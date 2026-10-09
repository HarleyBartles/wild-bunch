using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Projections;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
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

        var state = CreateReadState(store);

        return new GameSessionReadModel(
            store.Envelope.Id,
            state.Status,
            state.GameDifficulty,
            state.GameEntropy,
            state.StartFlowPhase,
            state.Player,
            state.World,
            state.CaseFile,
            state.Clock,
            state.PursuitState,
            state.TownVisitState,
            state.Journey,
            state.TravelDiaryDays,
            state.LogEntries);
    }

    private static StartFlowPhase DeriveStartFlowPhase(IReadOnlyList<IDomainEvent> events)
    {
        var hasGameStarted = false;
        var hasStartingTownSelected = false;
        var hasPrologueViewed = false;
        var hasSetupCompleted = false;

        foreach (var e in events)
        {
            if (e is GameStarted) hasGameStarted = true;
            else if (e is StartingTownSelected) hasStartingTownSelected = true;
            else if (e is PrologueViewed) hasPrologueViewed = true;
            else if (e is PlayerSetupCompleted) hasSetupCompleted = true;
        }

        if (hasGameStarted) return StartFlowPhase.GameStarted;
        if (hasStartingTownSelected) return StartFlowPhase.StartingTownSelected;
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

        var state = CreateReadState(store);
        var currentTown = state.Player.CurrentTownId is not null
            ? state.World.GetTown(state.Player.CurrentTownId.Value)
            : null;
        var logEntries = ApplySlice(state.LogEntries, skip, take);

        return new JournalSnapshot(
            store.Envelope.Id,
            state.Status,
            state.Clock.Day,
            state.Clock.Turn,
            currentTown?.Id,
            currentTown?.Name,
            state.CaseFile.Accusation is null ? null : state.CaseFile.Accusation.Value.Value,
            state.CaseFile.OpeningLead.Description,
            state.CaseFile.KillerReleaseState,
            "Find the culprit before the law closes in.",
            state.CaseFile.GetDiscoveredSuspects(),
            state.CaseFile.KnownClues.ToArray(),
            state.CaseFile.KnownWarrants.ToArray(),
            state.CaseFile.SheriffTurnInSettlements.ToArray(),
            logEntries);
    }

    private static IReadOnlyList<GameLogEntry> ApplySlice(IReadOnlyList<GameLogEntry> entries, int skip, int? take)
    {
        var query = entries.Skip(Math.Max(0, skip));
        return take.HasValue ? query.Take(Math.Max(0, take.Value)).ToArray() : query.ToArray();
    }

    private GameSessionReadState CreateReadState(GameSessionStore store)
    {
        var logEntries = new JournalLogProjector().Project(store.AllEvents);
        if (store.Envelope.SnapshotVersion != store.Envelope.StreamVersion)
        {
            return CreateReadStateFromEvents(store, logEntries);
        }

        if (!store.Components.ContainsKey(GameSessionComponentNames.Journey)
            && JourneyCacheRecovery.HasCurrentJourney(store.AllEvents))
        {
            return CreateReadStateFromEvents(store, logEntries);
        }

        try
        {
            var player = _serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player, _payloadLoader, store.AllEvents));

            var world = _serializer.DeserializeWorld(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.World, _payloadLoader, store.AllEvents));
            var entropyJson = GameSessionComponentPayloads.GetRequiredCachePayload(store.Components, GameSessionComponentNames.Setup, _payloadLoader, store.AllEvents);
            var entropy = _serializer.DeserializeSetup(entropyJson);
            var townVisitStateJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.TownVisitState, _payloadLoader, store.AllEvents);
            var townVisitState = player.CurrentTownId is not null
                ? (townVisitStateJson is null
                    ? new TownVisitState(player.CurrentTownId.Value)
                    : _serializer.DeserializeTownVisitState(townVisitStateJson))
                : null;
            var journeyJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Journey, _payloadLoader, store.AllEvents);

            return new GameSessionReadState(
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
                journeyJson is null ? null : _serializer.DeserializeJourneySnapshot(journeyJson),
                store.TravelDiaryDays,
                logEntries);
        }
        catch (InvalidComponentCacheShapeException)
        {
            return CreateReadStateFromEvents(store, logEntries);
        }
    }

    private GameSessionReadState CreateReadStateFromEvents(GameSessionStore store, IReadOnlyList<GameLogEntry> logEntries)
    {
        var session = SessionRebuilder.RebuildFromEvents(new GameSessionId(store.Envelope.Id), store.AllEvents, _serializer);
        return new GameSessionReadState(
            session.Status,
            session.GameDifficulty,
            session.GameEntropy,
            session.StartFlowPhase,
            session.Player,
            session.World,
            session.CaseFile,
            session.Clock,
            session.PursuitState,
            session.TownVisitStateOrNull,
            session.Journey?.ToSnapshot(session.TravelRules),
            store.TravelDiaryDays,
            logEntries);
    }

    private async Task<GameSessionStore?> LoadStoreAsync(
        WildBunchDbContext dbContext,
        GameSessionId id,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken)
            .ConfigureAwait(false);

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
        var diaryDayEntities = await dbContext.GameSessionDiaryDays.AsNoTracking()
            .Where(day => day.SessionId == id.Value)
            .OrderBy(day => day.Sequence)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        var domainEvents = _payloadLoader.LoadEvents(storedEvents);
        var diaryDays = _payloadLoader.LoadDiaryDays(
            diaryDayEntities,
            domainEvents,
            envelope.StreamVersion,
            envelope.TravelDiaryProjectionStreamVersion,
            envelope.TravelDiaryProjectionDayCount);

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

    private sealed record GameSessionReadState(
        GameStatus Status,
        GameDifficulty GameDifficulty,
        GameEntropy GameEntropy,
        StartFlowPhase StartFlowPhase,
        Player Player,
        World World,
        CaseFile CaseFile,
        GameClock Clock,
        PursuitState PursuitState,
        TownVisitState? TownVisitState,
        TravelJourneySnapshot? Journey,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
        IReadOnlyList<GameLogEntry> LogEntries);
}
