using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Abstractions;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.GameSessions;

public sealed class EfGameSessionRepository : IGameSessionRepository
{
    private const int SchemaVersion = 1;
    private const string PlayerComponentName = "player";
    private const string WorldComponentName = "world";
    private const string CaseFileComponentName = "caseFile";
    private const string ClockComponentName = "clock";
    private const string PursuitStateComponentName = "pursuitState";
    private const string TravelRandomnessComponentName = "travelRandomness";
    private const string JourneyComponentName = "journey";

    private readonly WildBunchDbContext _dbContext;
    private readonly GameSessionJsonSerializer _serializer;

    public EfGameSessionRepository(WildBunchDbContext dbContext, GameSessionJsonSerializer serializer)
    {
        _dbContext = dbContext;
        _serializer = serializer;
    }

    public async Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(id, cancellationToken).ConfigureAwait(false);
        return store is null ? null : ToAggregate(store);
    }

    public async Task SaveAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var entity = await _dbContext.GameSessions.SingleOrDefaultAsync(existing => existing.Id == session.Id.Value, cancellationToken).ConfigureAwait(false);

        if (entity is null)
        {
            entity = new GameSessionEntity
            {
                Id = session.Id.Value,
                CreatedAtUtc = now,
                SchemaVersion = SchemaVersion
            };
            _dbContext.GameSessions.Add(entity);
        }

        entity.UpdatedAtUtc = now;
        entity.Status = session.Status.ToString();
        entity.TravelDifficulty = (int)session.TravelDifficulty;
        entity.SchemaVersion = SchemaVersion;

        UpsertComponent(entity.Id, PlayerComponentName, _serializer.SerializePlayer(session.Player), now);
        UpsertComponent(entity.Id, WorldComponentName, _serializer.SerializeWorld(session.World), now);
        UpsertComponent(entity.Id, CaseFileComponentName, _serializer.SerializeCaseFile(session.CaseFile), now);
        UpsertComponent(entity.Id, ClockComponentName, _serializer.SerializeClock(session.Clock), now);
        UpsertComponent(entity.Id, PursuitStateComponentName, _serializer.SerializePursuitState(session.PursuitState), now);
        UpsertComponent(entity.Id, TravelRandomnessComponentName, _serializer.SerializeTravelRandomness(session.TravelRandomness), now);

        if (session.Journey is null)
        {
            await RemoveComponentAsync(entity.Id, JourneyComponentName, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            UpsertComponent(entity.Id, JourneyComponentName, _serializer.SerializeJourneySnapshot(session.Journey.ToSnapshot(session.TravelRules)), now);
        }

        await SyncLogEntriesAsync(entity.Id, session.LogEntries, cancellationToken).ConfigureAwait(false);
        await SyncDiaryDaysAsync(entity.Id, session.TravelDiaryDays, cancellationToken).ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<GameSessionStore?> LoadStoreAsync(GameSessionId id, CancellationToken cancellationToken)
    {
        var envelope = await _dbContext.GameSessions.AsNoTracking().SingleOrDefaultAsync(session => session.Id == id.Value, cancellationToken).ConfigureAwait(false);
        if (envelope is null)
        {
            return null;
        }

        var components = await _dbContext.GameSessionComponents.AsNoTracking()
            .Where(component => component.SessionId == id.Value)
            .ToDictionaryAsync(component => component.ComponentName, cancellationToken)
            .ConfigureAwait(false);

        var logEntries = await _dbContext.GameSessionLogEntries.AsNoTracking()
            .Where(entry => entry.SessionId == id.Value)
            .OrderBy(entry => entry.Sequence)
            .Select(entry => new GameLogEntry(entry.Kind, entry.Message, entry.Day, entry.Turn))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var diaryDays = await _dbContext.GameSessionDiaryDays.AsNoTracking()
            .Where(day => day.SessionId == id.Value)
            .OrderBy(day => day.Sequence)
            .Select(day => day.PayloadJson)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return new GameSessionStore(
            envelope,
            components,
            logEntries,
            diaryDays.Select(_serializer.DeserializeTravelDiaryDay).ToArray());
    }

    private GameSession ToAggregate(GameSessionStore store)
    {
        var player = _serializer.DeserializePlayer(store.Components[PlayerComponentName].PayloadJson);
        var world = _serializer.DeserializeWorld(store.Components[WorldComponentName].PayloadJson);
        var caseFile = _serializer.DeserializeCaseFile(store.Components[CaseFileComponentName].PayloadJson);
        var clock = _serializer.DeserializeClock(store.Components[ClockComponentName].PayloadJson);
        var pursuitState = _serializer.DeserializePursuitState(store.Components[PursuitStateComponentName].PayloadJson);
        var randomness = _serializer.DeserializeTravelRandomness(store.Components[TravelRandomnessComponentName].PayloadJson);
        var journey = store.Components.TryGetValue(JourneyComponentName, out var journeyComponent)
            ? _serializer.DeserializeJourneySnapshot(journeyComponent.PayloadJson)
            : null;

        return _serializer.RehydrateGameSession(
            store.Envelope.Id,
            Enum.Parse<GameStatus>(store.Envelope.Status, ignoreCase: false),
            (TravelDifficulty)store.Envelope.TravelDifficulty,
            player,
            world,
            caseFile,
            clock,
            pursuitState,
            randomness,
            journey,
            store.TravelDiaryDays,
            store.LogEntries);
    }

    private void UpsertComponent(Guid sessionId, string componentName, string payloadJson, DateTime now)
    {
        var component = _dbContext.GameSessionComponents.Local.FirstOrDefault(item => item.SessionId == sessionId && item.ComponentName == componentName)
            ?? _dbContext.GameSessionComponents.SingleOrDefault(item => item.SessionId == sessionId && item.ComponentName == componentName);

        if (component is null)
        {
            _dbContext.GameSessionComponents.Add(new GameSessionComponentEntity
            {
                SessionId = sessionId,
                ComponentName = componentName,
                ComponentVersion = SchemaVersion,
                PayloadJson = payloadJson,
                UpdatedAtUtc = now
            });
            return;
        }

        component.ComponentVersion = SchemaVersion;
        component.PayloadJson = payloadJson;
        component.UpdatedAtUtc = now;
    }

    private async Task RemoveComponentAsync(Guid sessionId, string componentName, CancellationToken cancellationToken)
    {
        var component = await _dbContext.GameSessionComponents.SingleOrDefaultAsync(item => item.SessionId == sessionId && item.ComponentName == componentName, cancellationToken).ConfigureAwait(false);
        if (component is not null)
        {
            _dbContext.GameSessionComponents.Remove(component);
        }
    }

    private async Task SyncLogEntriesAsync(Guid sessionId, IReadOnlyList<GameLogEntry> logEntries, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.GameSessionLogEntries
            .Where(entry => entry.SessionId == sessionId)
            .OrderBy(entry => entry.Sequence)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var commonCount = Math.Min(existing.Count, logEntries.Count);
        for (var index = 0; index < commonCount; index++)
        {
            var current = existing[index];
            var desired = logEntries[index];
            if (current.Kind != desired.Kind || current.Message != desired.Message || current.Day != desired.Day || current.Turn != desired.Turn)
            {
                current.Kind = desired.Kind;
                current.Message = desired.Message;
                current.Day = desired.Day;
                current.Turn = desired.Turn;
            }
        }

        for (var index = existing.Count; index < logEntries.Count; index++)
        {
            var desired = logEntries[index];
            _dbContext.GameSessionLogEntries.Add(new GameSessionLogEntryEntity
            {
                SessionId = sessionId,
                Sequence = index,
                Kind = desired.Kind,
                Message = desired.Message,
                Day = desired.Day,
                Turn = desired.Turn
            });
        }

        for (var index = logEntries.Count; index < existing.Count; index++)
        {
            _dbContext.GameSessionLogEntries.Remove(existing[index]);
        }
    }

    private async Task SyncDiaryDaysAsync(Guid sessionId, IReadOnlyList<TravelDiaryDayState> travelDiaryDays, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.GameSessionDiaryDays
            .Where(day => day.SessionId == sessionId)
            .OrderBy(day => day.Sequence)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var commonCount = Math.Min(existing.Count, travelDiaryDays.Count);
        for (var index = 0; index < commonCount; index++)
        {
            var current = existing[index];
            var desiredJson = _serializer.SerializeTravelDiaryDay(travelDiaryDays[index]);
            if (!string.Equals(current.PayloadJson, desiredJson, StringComparison.Ordinal))
            {
                current.PayloadJson = desiredJson;
                current.RecordedAtUtc = DateTime.UtcNow;
            }
        }

        for (var index = existing.Count; index < travelDiaryDays.Count; index++)
        {
            _dbContext.GameSessionDiaryDays.Add(new GameSessionDiaryDayEntity
            {
                SessionId = sessionId,
                Sequence = index,
                PayloadJson = _serializer.SerializeTravelDiaryDay(travelDiaryDays[index]),
                RecordedAtUtc = DateTime.UtcNow
            });
        }

        for (var index = travelDiaryDays.Count; index < existing.Count; index++)
        {
            _dbContext.GameSessionDiaryDays.Remove(existing[index]);
        }
    }

    private sealed record GameSessionStore(
        GameSessionEntity Envelope,
        IReadOnlyDictionary<string, GameSessionComponentEntity> Components,
        IReadOnlyList<GameLogEntry> LogEntries,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays);
}
