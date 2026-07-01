using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Application.Games.Models;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.GameSessions;

internal static class GameSessionReadStoreLoader
{
    public static async Task<GameSessionReadModel?> LoadGameSessionReadModelAsync(
        WildBunchDbContext dbContext,
        GameSessionJsonSerializer serializer,
        GameSessionId sessionId,
        CancellationToken cancellationToken)
    {
        var store = await LoadStoreAsync(dbContext, serializer, sessionId, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return null;
        }

        var player = serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player));
        var world = serializer.DeserializeWorld(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.World));
        var entropyJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Setup);
        var entropy = entropyJson is null ? GameEntropy.Classic : serializer.DeserializeSetup(entropyJson);
        var townVisitStateJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.TownVisitState);
        var townVisitState = townVisitStateJson is null
            ? new TownVisitState(player.CurrentTownId)
            : serializer.DeserializeTownVisitState(townVisitStateJson);

        return new GameSessionReadModel(
            store.Envelope.Id,
            Enum.Parse<GameStatus>(store.Envelope.Status, ignoreCase: false),
            (GameDifficulty)store.Envelope.GameDifficulty,
            entropy,
            DeriveStartFlowPhase(store.AllEvents),
            player,
            world,
            serializer.DeserializeCaseFile(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.CaseFile)),
            serializer.DeserializeClock(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Clock)),
            serializer.DeserializePursuitState(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.PursuitState)),
            townVisitState,
            GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Journey) is { } journeyJson
                ? serializer.DeserializeJourneySnapshot(journeyJson)
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

    public static async Task<JournalSnapshot?> LoadJournalSnapshotAsync(
        WildBunchDbContext dbContext,
        GameSessionJsonSerializer serializer,
        GameSessionId sessionId,
        int skip,
        int? take,
        CancellationToken cancellationToken)
    {
        var store = await LoadStoreAsync(dbContext, serializer, sessionId, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return null;
        }

        var player = serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player));
        var world = serializer.DeserializeWorld(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.World));
        var entropyJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Setup);
        var entropy = entropyJson is null ? GameEntropy.Classic : serializer.DeserializeSetup(entropyJson);
        var caseFile = serializer.DeserializeCaseFile(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.CaseFile));
        var currentTown = world.GetTown(player.CurrentTownId);
        var clock = serializer.DeserializeClock(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Clock));
        var logEntries = ApplySlice(new JournalLogProjector().Project(store.AllEvents), skip, take);

        return new JournalSnapshot(
            store.Envelope.Id,
            Enum.Parse<GameStatus>(store.Envelope.Status, ignoreCase: false),
            clock.Day,
            clock.Turn,
            currentTown.Id,
            currentTown.Name,
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

    private static async Task<GameSessionStore?> LoadStoreAsync(
        WildBunchDbContext dbContext,
        GameSessionJsonSerializer serializer,
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

        var domainEvents = new IDomainEvent[storedEvents.Length];
        for (var i = 0; i < storedEvents.Length; i++)
        {
            domainEvents[i] = serializer.DeserializeEvent(storedEvents[i].EventType, storedEvents[i].PayloadJson);
        }

        var diaryDays = await dbContext.GameSessionDiaryDays.AsNoTracking()
            .Where(day => day.SessionId == id.Value)
            .OrderBy(day => day.Sequence)
            .Select(day => day.PayloadJson)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return new GameSessionStore(
            envelope,
            components,
            diaryDays.Select(serializer.DeserializeTravelDiaryDay).ToArray(),
            domainEvents);
    }

    private sealed record GameSessionStore(
        GameSessionEntity Envelope,
        IReadOnlyDictionary<string, GameSessionComponentEntity> Components,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
        IReadOnlyList<IDomainEvent> AllEvents);
}
