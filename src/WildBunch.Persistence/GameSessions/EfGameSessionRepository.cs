using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence.Serialization;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Persistence.GameSessions;

public sealed class EfGameSessionRepository : IGameSessionRepository
{
    private const int SchemaVersion = 1;

    private readonly WildBunchDbContext _dbContext;
    private readonly GameSessionJsonSerializer _serializer;
    private readonly TravelDiaryDayProjector _travelDiaryDayProjector;
    private readonly PayloadUpcasterRegistry _eventUpcasters;
    private readonly PersistedPayloadLoader _payloadLoader;

    public EfGameSessionRepository(
        WildBunchDbContext dbContext,
        GameSessionJsonSerializer serializer,
        TravelDiaryDayProjector travelDiaryDayProjector,
        PayloadUpcasterRegistry eventUpcasters,
        PersistedPayloadLoader payloadLoader)
    {
        _dbContext = dbContext;
        _serializer = serializer;
        _travelDiaryDayProjector = travelDiaryDayProjector;
        _eventUpcasters = eventUpcasters;
        _payloadLoader = payloadLoader;
    }

    public async Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default)
        => await LoadAsync(id, cancellationToken).ConfigureAwait(false);

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
            var session = await LoadAsync(new GameSessionId(id), cancellationToken).ConfigureAwait(false);
            if (session is not null)
            {
                sessions.Add(session);
            }
        }
        return sessions;
    }

    /// <summary>
    /// Centralized load routing shared by <see cref="GetByIdAsync"/> and
    /// <see cref="GetByStatusAsync"/>. The snapshot is a shortcut cache, not a
    /// requirement — if the snapshot is stale or components are missing, the
    /// full replay path (<see cref="LoadFromEventsAsync"/>) is used.
    /// See ADR-0028 and the event sourcing integrity policy.
    /// </summary>
    private async Task<GameSession?> LoadAsync(GameSessionId id, CancellationToken cancellationToken)
    {
        // Check if the session exists and whether the snapshot is current.
        var envelope = await _dbContext.GameSessions.AsNoTracking()
            .SingleOrDefaultAsync(session => session.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (envelope is null)
        {
            return null;
        }

        // Full replay path: if the snapshot version doesn't match the stream version,
        // the snapshot is stale — use full replay. This is the event-sourcing-true path.
        if (envelope.SnapshotVersion != envelope.StreamVersion)
        {
            return await LoadFromEventsAsync(id, cancellationToken).ConfigureAwait(false);
        }

        // Missing-snapshot guard: the snapshot version matches the stream version, but the
        // component rows may be missing or corrupted. In that case the fast path
        // (LoadStoreAsync + ToAggregate) would throw on GetRequiredPayload for a
        // missing component. Fall back to the full replay path so the snapshot is
        // never a hard requirement. Check that all required components are present,
        // not just any row — partial corruption must also fall back.
        // See ADR-0028 and the event sourcing integrity policy.
        var requiredComponents = new[]
        {
            GameSessionComponentNames.Player,
            GameSessionComponentNames.World,
            GameSessionComponentNames.CaseFile,
            GameSessionComponentNames.Clock,
            GameSessionComponentNames.PursuitState
        };
        var presentComponentCount = await _dbContext.GameSessionComponents.AsNoTracking()
            .CountAsync(c => c.SessionId == id.Value && requiredComponents.Contains(c.ComponentName), cancellationToken)
            .ConfigureAwait(false);
        if (presentComponentCount < requiredComponents.Length)
        {
            return await LoadFromEventsAsync(id, cancellationToken).ConfigureAwait(false);
        }

        // Fast path: snapshot is current. Load from snapshot + replay post-snapshot events.
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
        entity.GameDifficulty = (int)session.GameDifficulty;
        entity.SeedCode = session.SeedCode;
        entity.SchemaVersion = SchemaVersion;

        // Append events to the event stream. For new sessions, store the full
        // event stream (AllEvents) so the session is replayable even when
        // MarkEventsCommitted was called before StoreAsync. For existing
        // sessions, store only the uncommitted delta.
        var eventsToStore = isNew ? session.AllEvents : session.UncommittedEvents;
        if (eventsToStore.Count > 0)
        {
            var nextSequence = entity.StreamVersion + 1;
            foreach (var e in eventsToStore)
            {
                var eventType = e.GetType().Name;
                _dbContext.StoredEvents.Add(new StoredEventEntity
                {
                    StreamId = entity.Id,
                    Sequence = nextSequence++,
                    EventId = Guid.NewGuid(),
                    OccurredAtUtc = now,
                    EventType = eventType,
                    PayloadJson = _serializer.SerializeEvent(e),
                    CorrelationId = correlationId,
                    SchemaVersion = _eventUpcasters.CurrentVersion(eventType)
                });
            }
            entity.StreamVersion = session.Version;
        }

        // The snapshot components are always written (below), so the snapshot
        // version must always reflect the session's current version — even when
        // no events were produced (e.g. StartPrepped sessions). Without this,
        // SnapshotVersion stays null and GetByIdAsync routes to LoadFromEventsAsync,
        // which returns null for sessions with no stored events.
        entity.SnapshotVersion = session.Version;

        // Stage snapshot upsert (cache)
        UpsertComponent(entity.Id, GameSessionComponentNames.Player, _serializer.SerializePlayer(session.Player), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.World, _serializer.SerializeWorld(session.World), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.CaseFile, _serializer.SerializeCaseFile(session.CaseFile), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.Clock, _serializer.SerializeClock(session.Clock), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.PursuitState, _serializer.SerializePursuitState(session.PursuitState), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.Setup, _serializer.SerializeSetup(session.GameEntropy), now);
        UpsertComponent(entity.Id, GameSessionComponentNames.SaltSource, _serializer.SerializeSaltSource(session.SaltSource), now);
        if (session.TownVisitStateOrNull is { } townVisitState)
        {
            UpsertComponent(entity.Id, GameSessionComponentNames.TownVisitState, _serializer.SerializeTownVisitState(townVisitState), now);
        }
        else
        {
            await RemoveComponentAsync(entity.Id, GameSessionComponentNames.TownVisitState, cancellationToken).ConfigureAwait(false);
        }
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

        var devLayoutSaltsJson = _serializer.SerializeDevLayoutSalts(session.DevLayoutSalts);
        if (devLayoutSaltsJson is null)
        {
            await RemoveComponentAsync(entity.Id, GameSessionComponentNames.DevLayoutSalts, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            UpsertComponent(entity.Id, GameSessionComponentNames.DevLayoutSalts, devLayoutSaltsJson, now);
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

        return _payloadLoader.LoadEvents(storedEvents);
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

        var diaryDayEntities = await _dbContext.GameSessionDiaryDays.AsNoTracking()
            .Where(day => day.SessionId == id.Value)
            .OrderBy(day => day.Sequence)
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

        var allEvents = _payloadLoader.LoadEvents(allStoredEvents);

        // Post-snapshot events for state replay (subset of allEvents).
        IReadOnlyList<IDomainEvent> postSnapshotEvents = Array.Empty<IDomainEvent>();
        if (envelope.SnapshotVersion < envelope.StreamVersion)
        {
            postSnapshotEvents = allEvents
                .Skip((int)envelope.SnapshotVersion)
                .ToArray();
        }

        var diaryDays = _payloadLoader.LoadDiaryDays(diaryDayEntities, allEvents);
        return new GameSessionStore(
            envelope,
            components,
            diaryDays,
            postSnapshotEvents,
            allEvents);
    }

    /// <summary>
    /// Loads a session by replaying all events from the stream through
    /// RehydrateFromEvents. This is the full replay path that proves the
    /// snapshot is not required. The world is reconstructed from the
    /// WorldGenerated event. Diary days are rebuilt via TravelDiaryDayProjector.
    /// See ADR-0028 and the event sourcing integrity policy.
    /// </summary>
    private async Task<GameSession?> LoadFromEventsAsync(GameSessionId id, CancellationToken cancellationToken)
    {
        var storedEvents = await _dbContext.StoredEvents.AsNoTracking()
            .Where(e => e.StreamId == id.Value)
            .OrderBy(e => e.Sequence)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (storedEvents.Length == 0)
        {
            return null;
        }

        var events = _payloadLoader.LoadEvents(storedEvents);

        // Rehydrate the aggregate from the full event stream via the shared
        // SessionRebuilder (also used by PersistedPayloadLoader's rebuild callback).
        var session = SessionRebuilder.RebuildFromEvents(id, events, _serializer);

        // Rebuild diary days via the projector.
        var diaryProjection = _travelDiaryDayProjector.Project(events);
        session.ReplaceTravelDiaryDays(diaryProjection.Days);

        // Set committed events for projection-backed read paths (JournalLogProjector etc.).
        session.SetCommittedEvents(events);

        return session;
    }

    private GameSession ToAggregate(GameSessionStore store)
    {
        var player = _serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player, _payloadLoader, store.AllEvents));
        var world = _serializer.DeserializeWorld(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.World, _payloadLoader, store.AllEvents));
        var caseFile = _serializer.DeserializeCaseFile(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.CaseFile, _payloadLoader, store.AllEvents));
        var clock = _serializer.DeserializeClock(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Clock, _payloadLoader, store.AllEvents));
        var pursuitState = _serializer.DeserializePursuitState(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.PursuitState, _payloadLoader, store.AllEvents));
        var entropyJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Setup, _payloadLoader, store.AllEvents);
        var entropy = entropyJson is null ? GameEntropy.Classic : _serializer.DeserializeSetup(entropyJson);
        var saltSourceJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.SaltSource, _payloadLoader, store.AllEvents);
        var saltSource = saltSourceJson is null ? SaltSource.CreateRuntime() : _serializer.DeserializeSaltSource(saltSourceJson);
        var townVisitStateJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.TownVisitState, _payloadLoader, store.AllEvents);
        var townVisitState = townVisitStateJson is null ? null : _serializer.DeserializeTownVisitState(townVisitStateJson);
        var journeyJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Journey, _payloadLoader, store.AllEvents);
        var journey = journeyJson is null ? null : _serializer.DeserializeJourneySnapshot(journeyJson);
        var completedJourneyHistoryJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.CompletedJourneyHistory, _payloadLoader, store.AllEvents);
        var completedJourneyHistory = completedJourneyHistoryJson is null
            ? Array.Empty<TravelJourneySnapshot>()
            : _serializer.DeserializeCompletedJourneyHistory(completedJourneyHistoryJson);
        var wantedSuspectPresenceLedgerJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.WantedSuspectPresenceLedger, _payloadLoader, store.AllEvents);
        var wantedSuspectPresenceEntries = wantedSuspectPresenceLedgerJson is null
            ? Array.Empty<WantedSuspectPresenceEntry>()
            : _serializer.DeserializeWantedSuspectPresenceLedger(wantedSuspectPresenceLedgerJson);
        var currentActionContextJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.CurrentActionContext, _payloadLoader, store.AllEvents);
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
        var devOverrideJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.PendingDevTravelOverride, _payloadLoader, store.AllEvents);
        var pendingDevOverride = _serializer.DeserializePendingDevTravelOverride(devOverrideJson);
        if (pendingDevOverride is not null)
        {
            session.RestorePendingDevTravelOverride(pendingDevOverride);
        }

        // Restore BountyLoop-owned state from snapshot (dev saloon override + unrelated
        // criminal ledger). The constructor builds a fresh BountyLoop; this overwrites
        // the owned state with persisted values. See BUNCH-90, BUNCH-107, BUNCH-112.
        var devSaloonOverrideJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.PendingDevSaloonOverride, _payloadLoader, store.AllEvents);
        var pendingDevSaloonOverride = _serializer.DeserializePendingDevSaloonOverride(devSaloonOverrideJson);

        var unrelatedCriminalLedgerJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.UnrelatedCriminalLedger, _payloadLoader, store.AllEvents);
        WildBunch.Domain.Cases.UnrelatedCriminalLedger? unrelatedCriminalLedger = null;
        if (unrelatedCriminalLedgerJson is not null)
        {
            unrelatedCriminalLedger = _serializer.DeserializeUnrelatedCriminalLedger(unrelatedCriminalLedgerJson);
        }

        if (pendingDevSaloonOverride is not null || unrelatedCriminalLedger is not null)
        {
            session.RestoreBountyLoopState(unrelatedCriminalLedger, pendingDevSaloonOverride);
        }

        // Restore dev layout salts from snapshot. If there are post-snapshot events,
        // ApplyCommittedEvents will overwrite this via Apply(DevLayoutSaltsForced).
        // When the snapshot is current, this restores the persisted dev salts. See BUNCH-147.
        var devLayoutSaltsJson = GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.DevLayoutSalts, _payloadLoader, store.AllEvents);
        var devLayoutSalts = _serializer.DeserializeDevLayoutSalts(devLayoutSaltsJson);
        if (devLayoutSalts is not null)
        {
            session.RestoreDevLayoutSalts(devLayoutSalts);
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
                ComponentVersion = ProjectionVersions.ForComponent(componentName),
                PayloadJson = payloadJson,
                UpdatedAtUtc = now
            });
            return;
        }

        component.ComponentVersion = ProjectionVersions.ForComponent(componentName);
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
            current.SchemaVersion = ProjectionVersions.DiaryDay;
        }

        for (var index = existing.Count; index < travelDiaryDays.Count; index++)
        {
            _dbContext.GameSessionDiaryDays.Add(new GameSessionDiaryDayEntity
            {
                SessionId = sessionId,
                Sequence = index,
                PayloadJson = _serializer.SerializeTravelDiaryDay(travelDiaryDays[index]),
                RecordedAtUtc = DateTime.UtcNow,
                SchemaVersion = ProjectionVersions.DiaryDay
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

    private sealed record GameSessionStore(
        GameSessionEntity Envelope,
        IReadOnlyDictionary<string, GameSessionComponentEntity> Components,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
        IReadOnlyList<IDomainEvent> PostSnapshotEvents,
        IReadOnlyList<IDomainEvent> AllEvents);
}
