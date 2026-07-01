using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence.Serialization;

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

    public async Task<IReadOnlyList<GameSession>> GetByStatusAsync(GameStatus status, CancellationToken cancellationToken = default)
    {
        var sessionIds = await _dbContext.GameSessions.AsNoTracking()
            .Where(entity => entity.Status == status.ToString())
            .Select(entity => entity.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var sessions = new List<GameSession>(sessionIds.Length);
        foreach (var id in sessionIds)
        {
            var store = await LoadStoreAsync(new GameSessionId(id), cancellationToken).ConfigureAwait(false);
            if (store is not null)
            {
                sessions.Add(ToAggregate(store));
            }
        }
        return sessions;
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
        entity.GameDifficulty = (int)session.GameDifficulty;
        entity.SeedCode = session.SeedCode;
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
        UpsertComponent(entity.Id, GameSessionComponentNames.Setup, _serializer.SerializeSetup(session.GameEntropy), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.SaltSource, _serializer.SerializeSaltSource(session.SaltSource), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.TownVisitState, _serializer.SerializeTownVisitState(session.CurrentTownVisit), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.CurrentActionContext, _serializer.SerializeCurrentActionContext(session.CurrentActionContext, session.CurrentActionContextTownId), now);

        var devOverrideJson = _serializer.SerializePendingDevTravelOverride(session.PendingDevTravelOverride);
        if (devOverrideJson is null)
        {
            await RemoveComponentAsync(entity.Id, GameSessionComponentNames.PendingDevTravelOverride, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            UpsertComponent(entity.Id, GameSessionComponentNames.PendingDevTravelOverride, devOverrideJson, now);
        }

        var devSaloonOverrideJson = _serializer.SerializePendingDevSaloonOverride(session.PendingDevSaloonOverride);
        if (devSaloonOverrideJson is null)
        {
            await RemoveComponentAsync(entity.Id, GameSessionComponentNames.PendingDevSaloonOverride, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            UpsertComponent(entity.Id, GameSessionComponentNames.PendingDevSaloonOverride, devSaloonOverrideJson, now);
        }

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

        // Persist the UnrelatedCriminalLedger so active/taken-in/collected/retired
        // sets, gang parity, and next spawn index survive reload. Without this,
        // the ledger is reconstructed from a shrinking PublicWarrants pool (warrants
        // are removed by RevealWarrant on collection), which degrades the roster
        // below the 3x invariant. See BUNCH-107.
        UpsertComponent(entity.Id, GameSessionComponentNames.UnrelatedCriminalLedger, _serializer.SerializeUnrelatedCriminalLedger(session.UnrelatedCriminalLedger), now);

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

        var diaryDays = await _dbContext.GameSessionDiaryDays.AsNoTracking()
            .Where(day => day.SessionId == id.Value)
            .OrderBy(day => day.Sequence)
            .Select(day => day.PayloadJson)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        // Load all events for post-snapshot replay and projection-backed read paths.
        // After BUNCH-86, LogEntries are derived from the event stream via
        // JournalLogProjector, replacing the legacy log entries table.
        var allStoredEvents = await _dbContext.StoredEvents.AsNoTracking()
            .Where(e => e.StreamId == id.Value)
            .OrderBy(e => e.Sequence)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var allEvents = new IDomainEvent[allStoredEvents.Length];
        for (var i = 0; i < allStoredEvents.Length; i++)
        {
            allEvents[i] = _serializer.DeserializeEvent(allStoredEvents[i].EventType, allStoredEvents[i].PayloadJson);
        }

        // Post-snapshot events for state replay (subset of allEvents).
        IReadOnlyList<IDomainEvent> postSnapshotEvents = Array.Empty<IDomainEvent>();
        if (envelope.SnapshotVersion < envelope.StreamVersion)
        {
            postSnapshotEvents = allEvents
                .Skip((int)envelope.SnapshotVersion)
                .ToArray();
        }

        return new GameSessionStore(
            envelope,
            components,
            diaryDays.Select(_serializer.DeserializeTravelDiaryDay).ToArray(),
            postSnapshotEvents,
            allEvents);
    }

    private GameSession ToAggregate(GameSessionStore store)
    {
        var player = _serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player));
        var world = _serializer.DeserializeWorld(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.World));
        var caseFile = _serializer.DeserializeCaseFile(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.CaseFile));
        var clock = _serializer.DeserializeClock(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Clock));
        var pursuitState = _serializer.DeserializePursuitState(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.PursuitState));
        var entropyJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Setup);
        var entropy = entropyJson is null ? GameEntropy.Classic : _serializer.DeserializeSetup(entropyJson);
        var saltSource = _serializer.DeserializeSaltSource(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.SaltSource));
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
        TownActionContext currentActionContext;
        TownId? currentActionContextTownId;
        if (currentActionContextJson is null)
        {
            currentActionContext = TownActionContext.None;
            currentActionContextTownId = null;
        }
        else
        {
            (currentActionContext, currentActionContextTownId) = _serializer.DeserializeCurrentActionContext(currentActionContextJson);
        }

        var session = _serializer.RehydrateGameSession(
            store.Envelope.Id,
            Enum.Parse<GameStatus>(store.Envelope.Status, ignoreCase: false),
            (GameDifficulty)store.Envelope.GameDifficulty,
            entropy,
            player,
            world,
            caseFile,
            clock,
            pursuitState,
            saltSource,
            townVisitState,
            journey,
            completedJourneyHistory,
            wantedSuspectPresenceEntries,
            store.TravelDiaryDays);

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
            ? (int)store.Envelope.SnapshotVersion.GetValueOrDefault()
            : (int)store.Envelope.StreamVersion;
        GameSessionRehydrator.SetVersion(session, initialVersion);

        // Set SeedCode from snapshot as a cache. The true source of truth is the
        // GameStarted event, which will be applied during event replay if there are
        // post-snapshot events. When the snapshot is current, this restores the
        // persisted seed code. For setup-phase sessions (no GameStarted yet),
        // the seed code comes from the PlayerSetupCompleted event. See BUNCH-101.
        var seedCode = store.Envelope.SeedCode;
        if (seedCode is null)
        {
            var setupEvent = store.AllEvents.OfType<PlayerSetupCompleted>().FirstOrDefault();
            if (setupEvent is not null)
            {
                seedCode = setupEvent.SeedCode;
            }
        }
        GameSessionRehydrator.SetBackingField(session, "<SeedCode>k__BackingField", seedCode);

        // Set StartFlowPhase from the event stream. The Apply methods for
        // PlayerSetupCompleted, PrologueViewed, and GameStarted set this during
        // post-snapshot replay. When the snapshot is current (no post-snapshot
        // events), we derive it from the full event stream.
        if (!hasPostSnapshotEvents)
        {
            var derivedPhase = DeriveStartFlowPhase(store.AllEvents);
            GameSessionRehydrator.SetBackingField(session, "<StartFlowPhase>k__BackingField", derivedPhase);
        }

        // Set CurrentActionContext from snapshot. If there are post-snapshot events,
        // ApplyCommittedEvents will overwrite this via Apply(TownActionContextEntered).
        // When the snapshot is current, this restores the persisted context.
        GameSessionRehydrator.RestoreActionContextState(session, currentActionContext, currentActionContextTownId);

        // Set PendingDevTravelOverride from snapshot. If there are post-snapshot events,
        // ApplyCommittedEvents will overwrite this via Apply(DevTravelOverrideForced/Cleared/Consumed).
        // When the snapshot is current, this restores the persisted dev override. See BUNCH-89.
        var devOverrideJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.PendingDevTravelOverride);
        var pendingDevOverride = _serializer.DeserializePendingDevTravelOverride(devOverrideJson);
        if (pendingDevOverride is not null)
        {
            session.RestorePendingDevTravelOverride(pendingDevOverride);
        }

        // Restore BountyLoop-owned state from snapshot (dev saloon override + unrelated
        // criminal ledger). The constructor builds a fresh BountyLoop; this overwrites
        // the owned state with persisted values. See BUNCH-90, BUNCH-107, BUNCH-112.
        var devSaloonOverrideJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.PendingDevSaloonOverride);
        var pendingDevSaloonOverride = _serializer.DeserializePendingDevSaloonOverride(devSaloonOverrideJson);

        var unrelatedCriminalLedgerJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.UnrelatedCriminalLedger);
        WildBunch.Domain.Cases.UnrelatedCriminalLedger? unrelatedCriminalLedger = null;
        if (unrelatedCriminalLedgerJson is not null)
        {
            unrelatedCriminalLedger = _serializer.DeserializeUnrelatedCriminalLedger(unrelatedCriminalLedgerJson);
        }

        if (pendingDevSaloonOverride is not null || unrelatedCriminalLedger is not null)
        {
            session.RestoreBountyLoopState(unrelatedCriminalLedger, pendingDevSaloonOverride);
        }

        if (hasPostSnapshotEvents)
        {
            session.ApplyCommittedEvents(store.PostSnapshotEvents);
        }

        // Set committed events for projection-backed read paths (BUNCH-86).
        // AllEvents = committed + uncommitted, used by JournalLogProjector
        // to derive log entries from the event stream.
        session.SetCommittedEvents(store.AllEvents);

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

    private sealed record GameSessionStore(
        GameSessionEntity Envelope,
        IReadOnlyDictionary<string, GameSessionComponentEntity> Components,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
        IReadOnlyList<IDomainEvent> PostSnapshotEvents,
        IReadOnlyList<IDomainEvent> AllEvents);
}
