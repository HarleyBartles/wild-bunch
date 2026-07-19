using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Application.Games.Models;
using WildBunch.Persistence.Serialization;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionReadStoreLoader
{
    private readonly PersistedPayloadLoader _payloadLoader;
    private readonly GameSessionJsonSerializer _serializer;

    public GameSessionReadStoreLoader(PersistedPayloadLoader payloadLoader, GameSessionJsonSerializer serializer)
    {
        _payloadLoader = payloadLoader;
        _serializer = serializer;
    }

    public async Task<GameSessionReadModel?> LoadGameSessionReadModelAsync(
        WildBunchDbContext dbContext,
        GameSessionId sessionId,
        CancellationToken cancellationToken)
    {
        var store = await LoadStoreAsync(dbContext, sessionId, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return null;
        }

        var player = _serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player, _payloadLoader, store.AllEvents));
        var world = _serializer.DeserializeWorld(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.World, _payloadLoader, store.AllEvents));
        var entropyJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Setup, _payloadLoader, store.AllEvents);
        var entropy = entropyJson is null ? GameEntropy.Classic : _serializer.DeserializeSetup(entropyJson);
        var townVisitStateJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.TownVisitState, _payloadLoader, store.AllEvents);
        var townVisitState = player.CurrentTownId is not null
            ? (townVisitStateJson is null
                ? new TownVisitState(player.CurrentTownId.Value)
                : _serializer.DeserializeTownVisitState(townVisitStateJson))
            : null;

        return new GameSessionReadModel(
            store.Envelope.Id,
            Enum.Parse<GameStatus>(store.Envelope.Status, ignoreCase: false),
            (GameDifficulty)store.Envelope.GameDifficulty,
            entropy,
            DeriveStartFlowPhase(store.AllEvents),
            player,
            world,
            _serializer.DeserializeCaseFile(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.CaseFile, _payloadLoader, store.AllEvents)),
            _serializer.DeserializeClock(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Clock, _payloadLoader, store.AllEvents)),
            _serializer.DeserializePursuitState(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.PursuitState, _payloadLoader, store.AllEvents)),
            townVisitState,
            GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Journey, _payloadLoader, store.AllEvents) is { } journeyJson
                ? _serializer.DeserializeJourneySnapshot(journeyJson)
                : null,
            store.TravelDiaryDays,
            new JournalLogProjector().Project(store.AllEvents));
    }

    private static StartFlowPhase DeriveStartFlowPhase(IReadOnlyList<IDomainEvent> events)
    {
        var hasGameStarted = false;
        var hasPrologueViewed = false;
        var hasSetupCompleted = false;

        foreach (var e in events)
        {
            if (e is GameStarted) hasGameStarted = true;
            else if (e is PrologueViewed) hasPrologueViewed = true;
            else if (e is PlayerSetupCompleted) hasSetupCompleted = true;
        }

        if (hasGameStarted) return StartFlowPhase.GameStarted;
        if (hasPrologueViewed) return StartFlowPhase.PrologueViewed;
        if (hasSetupCompleted) return StartFlowPhase.SetupComplete;
        return StartFlowPhase.NotStarted;
    }

    public async Task<JournalSnapshot?> LoadJournalSnapshotAsync(
        WildBunchDbContext dbContext,
        GameSessionId sessionId,
        int skip,
        int? take,
        CancellationToken cancellationToken)
    {
        var store = await LoadStoreAsync(dbContext, sessionId, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return null;
        }

        var player = _serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player, _payloadLoader, store.AllEvents));
        var world = _serializer.DeserializeWorld(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.World, _payloadLoader, store.AllEvents));
        var entropyJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Setup, _payloadLoader, store.AllEvents);
        var entropy = entropyJson is null ? GameEntropy.Classic : _serializer.DeserializeSetup(entropyJson);
        var caseFile = _serializer.DeserializeCaseFile(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.CaseFile, _payloadLoader, store.AllEvents));
        var currentTown = player.CurrentTownId is not null
            ? world.GetTown(player.CurrentTownId.Value)
            : null;
        var clock = _serializer.DeserializeClock(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Clock, _payloadLoader, store.AllEvents));
        var logEntries = ApplySlice(new JournalLogProjector().Project(store.AllEvents), skip, take);

        return new JournalSnapshot(
            store.Envelope.Id,
            Enum.Parse<GameStatus>(store.Envelope.Status, ignoreCase: false),
            clock.Day,
            clock.Turn,
            currentTown?.Id,
            currentTown?.Name,
            caseFile.Accusation is null ? null : caseFile.Accusation.Value.Value,
            caseFile.OpeningLead.Description,
            caseFile.KillerReleaseState,
            "Find the culprit before the law closes in.",
            caseFile.GetDiscoveredSuspects(),
            caseFile.KnownClues.ToArray(),
            caseFile.KnownWarrants.ToArray(),
            caseFile.SheriffTurnInSettlements.ToArray(),
            logEntries);
    }

    private static IReadOnlyList<GameLogEntry> ApplySlice(IReadOnlyList<GameLogEntry> entries, int skip, int? take)
    {
        var query = entries.Skip(Math.Max(0, skip));
        return take.HasValue ? query.Take(Math.Max(0, take.Value)).ToArray() : query.ToArray();
    }

    private async Task<GameSessionStore?> LoadStoreAsync(
        WildBunchDbContext dbContext,
        GameSessionId id,
        CancellationToken cancellationToken)
    {
        var envelope = await dbContext.GameSessions.AsNoTracking().SingleOrDefaultAsync(session => session.Id == id.Value, cancellationToken).ConfigureAwait(false);
        if (envelope is null)
        {
            return null;
        }

        var components = await dbContext.GameSessionComponents.AsNoTracking()
            .Where(component => component.SessionId == id.Value)
            .ToDictionaryAsync(component => component.ComponentName, cancellationToken)
            .ConfigureAwait(false);

        // BUNCH-84/BUNCH-86: LogEntries are derived from the event stream via
        // JournalLogProjector on demand at the call sites. The legacy log entries
        // table has been fully removed; both the read-store loader and the
        // command-load path now use projection.
        var storedEvents = await dbContext.StoredEvents.AsNoTracking()
            .Where(e => e.StreamId == id.Value)
            .OrderBy(e => e.Sequence)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var domainEvents = _payloadLoader.LoadEvents(storedEvents);

        var diaryDayEntities = await dbContext.GameSessionDiaryDays.AsNoTracking()
            .Where(day => day.SessionId == id.Value)
            .OrderBy(day => day.Sequence)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var diaryDays = _payloadLoader.LoadDiaryDays(diaryDayEntities, domainEvents);

        return new GameSessionStore(
            envelope,
            components,
            diaryDays,
            domainEvents);
    }

    private sealed record GameSessionStore(
        GameSessionEntity Envelope,
        IReadOnlyDictionary<string, GameSessionComponentEntity> Components,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
        IReadOnlyList<IDomainEvent> AllEvents);
}
