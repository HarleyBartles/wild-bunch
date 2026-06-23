using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Persistence.Serialization;

// LogEntries is [Obsolete] (projection-legacy per ADR-0028). The repository still
// persists and loads it for backward compatibility. Do not add new LogEntries consumers.
#pragma warning disable CS0618

namespace WildBunch.Persistence.GameSessions;

public sealed class EfGameSessionRepository : IGameSessionRepository
{
    private const int SchemaVersion = 1;

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

    public async Task StoreAsync(GameSession session, Guid? correlationId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var now = DateTime.UtcNow;
        var entity = await _dbContext.GameSessions.SingleOrDefaultAsync(existing => existing.Id == session.Id.Value, cancellationToken).ConfigureAwait(false);

        var isNew = entity is null;
        if (isNew)
        {
            entity = new GameSessionEntity
            {
                Id = session.Id.Value,
                CreatedAtUtc = now,
                SchemaVersion = SchemaVersion,
                StreamVersion = 0
            };
            _dbContext.GameSessions.Add(entity);
        }

        // Optimistic concurrency check: the persisted stream version must match
        // the session's committed version (Version - UncommittedEvents.Count).
        var expectedVersion = session.Version - session.UncommittedEvents.Count;
        if (!isNew && entity!.StreamVersion != expectedVersion)
        {
            throw new ConcurrencyException(session.Id, expectedVersion, (int)entity.StreamVersion);
        }

        entity!.UpdatedAtUtc = now;
        entity.Status = session.Status.ToString();
        entity.TravelDifficulty = (int)session.TravelDifficulty;
        entity.SchemaVersion = SchemaVersion;

        // Append uncommitted events to the event stream
        if (session.UncommittedEvents.Count > 0)
        {
            var nextSequence = entity.StreamVersion + 1;
            foreach (var e in session.UncommittedEvents)
            {
                _dbContext.StoredEvents.Add(new StoredEventEntity
                {
                    StreamId = entity.Id,
                    Sequence = nextSequence++,
                    EventId = Guid.NewGuid(),
                    OccurredAtUtc = now,
                    EventType = e.GetType().Name,
                    PayloadJson = _serializer.SerializeEvent(e),
                    CorrelationId = correlationId,
                    SchemaVersion = SchemaVersion
                });
            }
            entity.StreamVersion = session.Version;
            entity.SnapshotVersion = session.Version;
        }

        // Stage snapshot upsert (cache)
        UpsertComponent(entity.Id, GameSessionComponentNames.Player, _serializer.SerializePlayer(session.Player), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.World, _serializer.SerializeWorld(session.World), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.CaseFile, _serializer.SerializeCaseFile(session.CaseFile), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.Clock, _serializer.SerializeClock(session.Clock), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.PursuitState, _serializer.SerializePursuitState(session.PursuitState), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.Setup, _serializer.SerializeSetup(session.Entropy), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.TravelRandomness, _serializer.SerializeTravelRandomness(session.TravelRandomness), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.TownVisitState, _serializer.SerializeTownVisitState(session.CurrentTownVisit), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.CurrentActionContext, _serializer.SerializeCurrentActionContext(session.CurrentActionContext), now);

        if (session.Journey is null)
        {
            await RemoveComponentAsync(entity.Id, GameSessionComponentNames.Journey, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            UpsertComponent(entity.Id, GameSessionComponentNames.Journey, _serializer.SerializeJourneySnapshot(session.Journey.ToSnapshot(session.TravelRules)), now);
        }

        if (session.CompletedJourneyHistory.Count == 0)
        {
            await RemoveComponentAsync(entity.Id, GameSessionComponentNames.CompletedJourneyHistory, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            UpsertComponent(entity.Id, GameSessionComponentNames.CompletedJourneyHistory, _serializer.SerializeCompletedJourneyHistory(session.CompletedJourneyHistory), now);
        }

        if (session.WantedSuspectPresenceEntries.Count == 0)
        {
            await RemoveComponentAsync(entity.Id, GameSessionComponentNames.WantedSuspectPresenceLedger, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            UpsertComponent(entity.Id, GameSessionComponentNames.WantedSuspectPresenceLedger, _serializer.SerializeWantedSuspectPresenceLedger(session.WantedSuspectPresenceEntries), now);
        }

        await SyncLogEntriesAsync(entity.Id, session.LogEntries, cancellationToken).ConfigureAwait(false);
        await SyncDiaryDaysAsync(entity.Id, session.TravelDiaryDays, cancellationToken).ConfigureAwait(false);

        // NO SaveChangesAsync here — the UoW commits.
    }

    public async Task<IReadOnlyList<IDomainEvent>> GetEventStreamAsync(GameSessionId id, long fromVersion = 0, CancellationToken cancellationToken = default)
    {
        var storedEvents = await _dbContext.StoredEvents.AsNoTracking()
            .Where(e => e.StreamId == id.Value && e.Sequence > fromVersion)
            .OrderBy(e => e.Sequence)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (storedEvents.Length == 0)
        {
            return Array.Empty<IDomainEvent>();
        }

        var events = new IDomainEvent[storedEvents.Length];
        for (var i = 0; i < storedEvents.Length; i++)
        {
            events[i] = _serializer.DeserializeEvent(storedEvents[i].EventType, storedEvents[i].PayloadJson);
        }
        return events;
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

        // Load post-snapshot events when the snapshot is behind the stream version.
        // This implements the snapshot + replay load path from ADR-0028.
        IReadOnlyList<IDomainEvent> postSnapshotEvents = Array.Empty<IDomainEvent>();
        if (envelope.SnapshotVersion < envelope.StreamVersion)
        {
            var storedEvents = await _dbContext.StoredEvents.AsNoTracking()
                .Where(e => e.StreamId == id.Value && e.Sequence > envelope.SnapshotVersion)
                .OrderBy(e => e.Sequence)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            if (storedEvents.Length > 0)
            {
                var events = new IDomainEvent[storedEvents.Length];
                for (var i = 0; i < storedEvents.Length; i++)
                {
                    events[i] = _serializer.DeserializeEvent(storedEvents[i].EventType, storedEvents[i].PayloadJson);
                }
                postSnapshotEvents = events;
            }
        }

        return new GameSessionStore(
            envelope,
            components,
            logEntries,
            diaryDays.Select(_serializer.DeserializeTravelDiaryDay).ToArray(),
            postSnapshotEvents);
    }

    private GameSession ToAggregate(GameSessionStore store)
    {
        var player = _serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player));
        var world = _serializer.DeserializeWorld(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.World));
        var caseFile = _serializer.DeserializeCaseFile(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.CaseFile));
        var clock = _serializer.DeserializeClock(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Clock));
        var pursuitState = _serializer.DeserializePursuitState(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.PursuitState));
        var entropyJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Setup);
        var entropy = entropyJson is null ? AdventureRandomnessPolicy.Standard : _serializer.DeserializeSetup(entropyJson);
        var randomness = _serializer.DeserializeTravelRandomness(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.TravelRandomness));
        var townVisitStateJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.TownVisitState);
        var townVisitState = townVisitStateJson is null ? null : _serializer.DeserializeTownVisitState(townVisitStateJson);
        var journeyJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Journey);
        var journey = journeyJson is null ? null : _serializer.DeserializeJourneySnapshot(journeyJson);
        var completedJourneyHistoryJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.CompletedJourneyHistory);
        var completedJourneyHistory = completedJourneyHistoryJson is null
            ? Array.Empty<TravelJourneySnapshot>()
            : _serializer.DeserializeCompletedJourneyHistory(completedJourneyHistoryJson);
        var wantedSuspectPresenceLedgerJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.WantedSuspectPresenceLedger);
        var wantedSuspectPresenceEntries = wantedSuspectPresenceLedgerJson is null
            ? Array.Empty<WantedSuspectPresenceEntry>()
            : _serializer.DeserializeWantedSuspectPresenceLedger(wantedSuspectPresenceLedgerJson);
        var currentActionContextJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.CurrentActionContext);
        var currentActionContext = currentActionContextJson is null
            ? TownActionContext.None
            : _serializer.DeserializeCurrentActionContext(currentActionContextJson);

        var session = _serializer.RehydrateGameSession(
            store.Envelope.Id,
            Enum.Parse<GameStatus>(store.Envelope.Status, ignoreCase: false),
            (TravelDifficulty)store.Envelope.TravelDifficulty,
            entropy,
            player,
            world,
            caseFile,
            clock,
            pursuitState,
            randomness,
            townVisitState,
            journey,
            completedJourneyHistory,
            wantedSuspectPresenceEntries,
            store.TravelDiaryDays,
            store.LogEntries);

        // Set the aggregate version so that after any post-snapshot replay the
        // version equals StreamVersion. Each Apply call inside
        // ApplyCommittedEvents increments _version by 1, so we start from
        // SnapshotVersion (the snapshot's version) and replay
        // (StreamVersion - SnapshotVersion) events, ending at StreamVersion.
        // When the snapshot is current (SnapshotVersion == StreamVersion), there
        // are no post-snapshot events and SetVersion(StreamVersion) is correct.
        // See ADR-0028 §8 (Snapshots as cache) and §7 (Optimistic concurrency).
        var hasPostSnapshotEvents = store.PostSnapshotEvents.Count > 0;
        var initialVersion = hasPostSnapshotEvents
            ? (int)store.Envelope.SnapshotVersion
            : (int)store.Envelope.StreamVersion;
        GameSessionRehydrator.SetVersion(session, initialVersion);

        // Set CurrentActionContext from snapshot. If there are post-snapshot events,
        // ApplyCommittedEvents will overwrite this via Apply(TownActionContextEntered).
        // When the snapshot is current, this restores the persisted context.
        GameSessionRehydrator.SetCurrentActionContext(session, currentActionContext);

        if (hasPostSnapshotEvents)
        {
            session.ApplyCommittedEvents(store.PostSnapshotEvents);
        }

        return session;
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
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
        IReadOnlyList<IDomainEvent> PostSnapshotEvents);
}
