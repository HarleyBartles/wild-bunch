using System.Diagnostics.CodeAnalysis;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;
using WildBunch.Domain.WantedPosters;

namespace WildBunch.Domain.Game;

// All flows are event-sourced per ADR-0028. Log/journal reads derive from the typed
// domain event stream via JournalLogProjector / GameSessionLogProjection.
// Do not add new direct-mutation command methods; use the event-sourced pattern.

/// <summary>
/// Mutable live play-state aggregate root.
/// Command handlers load and persist this root through <see cref="WildBunch.Application.Abstractions.IGameSessionRepository"/>.
/// </summary>
public sealed partial class GameSession : WildBunch.Domain.IAggregateRoot
{
    private const string JourneyModalBlockMessage = "Finish the current journey before taking that action.";
    private const string ArchivedBlockMessage = "This playthrough is archived.";
    private const decimal CitizenDeclarationFine = 10m;

    private TownAggregate? _currentTown;
    private readonly BountyLoop _bountyLoop;
    private readonly JourneyLoop _journeyLoop;
    private readonly ActionContextTracker _actionContextTracker = new();
    private readonly InvestigationLoop _investigationLoop = new();
    private readonly StoreLoop _storeLoop = new();

    private readonly List<IDomainEvent> _uncommittedEvents = [];
    private readonly List<IDomainEvent> _committedEvents = [];
    private int _version;
    private TownId? _selectedStartingTownId;

    private GameSession(
        GameSessionId id,
        Player player,
        DomainWorld world,
        CaseFile caseFile,
        PursuitState pursuitState,
        GameClock clock,
        GameStatus status,
        TravelJourney? journey,
        GameDifficulty gameDifficulty,
        SaltSource saltSource,
        GameEntropy gameEntropy,
        TownVisitState? currentTownVisit,
        IReadOnlyList<TravelJourneySnapshot>? completedJourneyHistory,
        IReadOnlyList<WantedSuspectPresenceEntry>? wantedSuspectPresenceEntries)
    {
        Id = id;
        Player = player;
        World = world;
        CaseFile = caseFile;
        PursuitState = pursuitState;
        Clock = clock;
        Status = status;
        GameDifficulty = gameDifficulty;
        GameEntropy = gameEntropy;
        SaltSource = saltSource;
        SeedCode = null; // Set by Apply(GameStarted) during event replay
        // During setup phase (StartSetup, RehydrateFromEvents), currentTownVisit is null
        // and the player's CurrentTownId is null. Defer TownAggregate creation
        // until Apply(GameStarted) when the real starting town is known. For snapshot-based
        // loads, currentTownVisit is non-null and we create _currentTown immediately.
        if (currentTownVisit is not null && player.CurrentTownId is not null)
        {
            _currentTown = new TownAggregate(World.GetTown(player.CurrentTownId.Value), currentTownVisit);
            if (!_currentTown.VisitState.TownId.Equals(player.CurrentTownId))
            {
                _currentTown.EnterTown(World.GetTown(player.CurrentTownId.Value));
            }
            _currentTown.PrimeCurrentTown();
        }

        // BUNCH-107: unrelated criminal parity ledger. Built from the case file's
        // unrelated-criminal warrants (the 21-strong pool) and the gang roster size.
        // The active pool starts at gang parity; gang take-ins (replayed via
        // SheriffTurnInSettled) drop the parity target and despawn excess. The
        // unrelated-criminal turn-in flow (SettleUnrelatedCriminalTurnIn /
        // UnrelatedCriminalTurnInSettled) records take-ins and spawns replacements;
        // the ledger itself is the parity source of truth.
        // During prepped phase (StartPrepped), caseFile is null; use a no-op ledger.
        var unrelatedCriminalLedger = caseFile is not null
            ? BuildUnrelatedCriminalLedger(caseFile)
            : new UnrelatedCriminalLedger(gangMemberCount: 0, poolSize: 0);

        _bountyLoop = new BountyLoop(wantedSuspectPresenceEntries, unrelatedCriminalLedger);

        _journeyLoop = new JourneyLoop(journey, completedJourneyHistory);
    }

    public GameSessionId Id { get; }

    /// <summary>
    /// Restores BountyLoop-owned state from a persisted snapshot. Called by the
    /// rehydration path after the constructor builds a fresh BountyLoop. The
    /// presence ledger is already constructed from constructor inputs; this
    /// restores the unrelated-criminal ledger and pending dev saloon override.
    /// See BUNCH-112.
    /// </summary>
    internal void RestoreBountyLoopState(
        WildBunch.Domain.Cases.UnrelatedCriminalLedger? unrelatedCriminalLedger,
        DevSaloonOverride? pendingDevSaloonOverride)
    {
        if (unrelatedCriminalLedger is not null)
        {
            _bountyLoop.RestoreUnrelatedCriminalLedger(unrelatedCriminalLedger);
        }
        if (pendingDevSaloonOverride is not null)
        {
            _bountyLoop.RestorePendingDevSaloonOverride(pendingDevSaloonOverride);
        }
    }

    /// <summary>
    /// Restores the pending dev travel override during snapshot rehydration.
    /// The override lives on <see cref="JourneyLoop"/> after BUNCH-119.
    /// </summary>
    internal void RestorePendingDevTravelOverride(DevTravelOverride? overrideValue)
    {
        _journeyLoop.RestorePendingDevTravelOverride(overrideValue);
    }

    /// <summary>
    /// Restores ActionContextTracker-owned state from a persisted snapshot. Called by the
    /// rehydration path after the constructor builds a fresh ActionContextTracker.
    /// See BUNCH-120.
    /// </summary>
    internal void RestoreActionContextState(TownActionContext context, TownId? townId)
    {
        _actionContextTracker.RestoreState(context, townId);
    }

    /// <summary>
    /// Restores dev layout salts during snapshot rehydration.
    /// The salts are reconstructed from event replay via Apply(DevLayoutSaltsForced).
    /// Both paths (snapshot load + event replay) must produce the same values.
    /// See BUNCH-147.
    /// </summary>
    internal void RestoreDevLayoutSalts(WildBunch.Domain.World.LayoutSalts layoutSalts)
    {
        DevLayoutSalts = layoutSalts;
    }

    public GameStatus Status { get; private set; }

    /// <summary>
    /// Tracks progress through the start game flow (setup → prologue → map selection → game started).
    /// This is persisted via domain events (PlayerSetupCompleted, PrologueViewed) and
    /// allows the frontend to resume from the correct step after a refresh.
    /// </summary>
    public StartFlowPhase StartFlowPhase { get; private set; } = StartFlowPhase.NotStarted;

    public Player Player { get; private set; }

    public DomainWorld World { get; private set; } = null!;

    public CaseFile CaseFile { get; private set; } = null!;

    public PursuitState PursuitState { get; }

    public GameClock Clock { get; }

    public TravelJourney? Journey => _journeyLoop.Journey;

    public GameDifficulty GameDifficulty { get; private set; }

    public GameEntropy GameEntropy { get; private set; }

    public SaltSource SaltSource { get; private set; }

    public string? SeedCode { get; private set; }

    /// <summary>
    /// Dev-controlled layout salts for town hub layout generation.
    /// When set, these salts override the derived layout salts for reproducible
    /// layout generation. Dev-only state. See BUNCH-147.
    /// </summary>
    public LayoutSalts? DevLayoutSalts { get; private set; }

    public TownAggregate CurrentTown => _currentTown
        ?? throw new InvalidOperationException("No town has been selected yet. The current town is only available after GameStarted.");

    public TownVisitState CurrentTownVisit => _currentTown?.VisitState
        ?? throw new InvalidOperationException("No town has been selected yet. The current town visit is only available after GameStarted.");

    /// <summary>
    /// Null-safe accessor for the town visit state. Returns null during the setup phase
    /// (before GameStarted). Used by the persistence layer to avoid snapshotting a
    /// phantom town visit state for setup-phase sessions.
    /// </summary>
    internal TownVisitState? TownVisitStateOrNull => _currentTown?.VisitState;

    public TravelRulesProfile TravelRules => TravelRulesProfile.For(GameDifficulty);

    /// <summary>
    /// Pending dev override for the next travel-day generation. Dev-only state.
    /// Consumed once by the next AdvanceJourneyDay. See BUNCH-89.
    /// </summary>
    internal DevTravelOverride? PendingDevTravelOverride => _journeyLoop.PendingDevTravelOverride;

    /// <summary>
    /// Pending dev override for the next saloon look-around. Dev-only state.
    /// Consumed once by the next LookAroundSaloon. See BUNCH-90.
    /// </summary>
    internal DevSaloonOverride? PendingDevSaloonOverride => _bountyLoop.PendingDevSaloonOverride;

    public IReadOnlyList<TravelDiaryDayState> TravelDiaryDays => _journeyLoop.TravelDiaryDays;

    public IReadOnlyList<TravelJourneySnapshot> CompletedJourneyHistory => _journeyLoop.CompletedJourneyHistory;

    public IReadOnlyList<WantedSuspectPresenceEntry> WantedSuspectPresenceEntries => _bountyLoop.PresenceEntries;

    /// <summary>
    /// Unrelated criminal parity ledger (BUNCH-107). Tracks the active pool of
    /// unrelated wanted criminals and keeps it at parity with the number of gang
    /// members still available to surface. Read-only view; mutations flow through
    /// <see cref="Apply(SheriffTurnInSettled)"/> (gang take-ins) and
    /// <see cref="Apply(UnrelatedCriminalTurnInSettled)"/> (unrelated-criminal take-ins).
    /// </summary>
    public UnrelatedCriminalLedger UnrelatedCriminalLedger => _bountyLoop.UnrelatedCriminalLedger;

    /// <summary>
    /// Events produced by command methods but not yet committed to the event stream.
    /// The handler collects these before calling <see cref="MarkEventsCommitted"/>.
    /// </summary>
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    /// <summary>
    /// Events committed to the event stream that were used to load/replay this session.
    /// Set by the repository during load. Used by <see cref="AllEvents"/> for projection.
    /// </summary>
    internal IReadOnlyList<IDomainEvent> CommittedEvents => _committedEvents;

    /// <summary>
    /// The full event stream: committed events (from load) followed by uncommitted
    /// events (from the current command). This is the projection source for
    /// projection-backed read paths (JournalLogProjector). See ADR-0028 and BUNCH-86.
    /// </summary>
    public IReadOnlyList<IDomainEvent> AllEvents
    {
        get
        {
            if (_uncommittedEvents.Count == 0)
            {
                return _committedEvents;
            }

            var combined = new List<IDomainEvent>(_committedEvents.Count + _uncommittedEvents.Count);
            combined.AddRange(_committedEvents);
            combined.AddRange(_uncommittedEvents);
            return combined;
        }
    }

    /// <summary>
    /// Sets the committed events loaded from the event stream. Called by the
    /// repository during load. See ADR-0028 and BUNCH-86.
    /// </summary>
    internal void SetCommittedEvents(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        _committedEvents.Clear();
        _committedEvents.AddRange(events);
    }

    /// <summary>
    /// Number of events applied (committed + uncommitted). Used for optimistic concurrency.
    /// </summary>
    public int Version => _version;

    /// <summary>
    /// Transfers uncommitted events to committed events after the event store has
    /// committed them. This keeps <see cref="AllEvents"/> correct for projection-backed
    /// read paths that run after commit (e.g., in-memory test doubles). The real
    /// repository re-sets committed events from the event store on next load.
    /// State is unchanged.
    /// </summary>
    internal void MarkEventsCommitted()
    {
        _committedEvents.AddRange(_uncommittedEvents);
        _uncommittedEvents.Clear();
    }

    /// <summary>
    /// The action context the player is currently in within the current town.
    /// Event-sourced: mutated only by <see cref="Apply(TownActionContextEntered)"/> via
    /// <see cref="EnterActionContext"/>. Persisted in the session snapshot and reconstructed
    /// from event replay. See ADR-0028 and BUNCH-80 clock/turn correction.
    /// </summary>
    public TownActionContext CurrentActionContext => _actionContextTracker.CurrentActionContext;

    /// <summary>
    /// The town in which the <see cref="CurrentActionContext"/> was entered. A context is
    /// scoped to its town: entering <see cref="TownActionContext.Saloon"/> in Town A does not
    /// suppress time advancement when entering Saloon in Town B. Event-sourced alongside
    /// <see cref="CurrentActionContext"/> via <see cref="Apply(TownActionContextEntered)"/>.
    /// </summary>
    public TownId? CurrentActionContextTownId => _actionContextTracker.CurrentActionContextTownId;

    /// <summary>
    /// Enters an action context within the current town. If the context is different from the
    /// current one, or if the current town differs from the town the current context was entered
    /// in, emits a <see cref="TownActionContextEntered"/> event that advances the turn and
    /// records the resulting context/town/clock state. If the same context in the same town, no
    /// event and no turn advance. <see cref="TownActionContext.None"/> never produces an event.
    /// This is event-sourced: the event carries the resulting Day/Turn/TimeOfDay/TownId so replay
    /// reconstructs the exact same state. <see cref="EnterActionContext"/> does NOT call
    /// <see cref="GameClock.Advance"/> directly — <see cref="Apply(TownActionContextEntered)"/>
    /// sets the clock from the event via <see cref="GameClock.Set"/>.
    /// </summary>
    public bool EnterActionContext(TownActionContext context)
    {
        var inputs = new ActionContextEnterInputs(Clock, PursuitState, CurrentTown.TownId);
        var e = _actionContextTracker.EnterActionContext(context, inputs);
        if (e is null)
        {
            return false;
        }

        ProduceEvent(e);
        return true;
    }

    /// <summary>
    /// Named predicate expressing the invariant for direct wanted-suspect confrontation:
    /// confrontation itself does not advance time and is only valid when the player is
    /// already in an appropriate active POI/location context. For this first version the
    /// only supported confrontation context is the saloon POI loop, which requires:
    /// <list type="number">
    /// <item><see cref="CurrentActionContext"/> is <see cref="TownActionContext.Saloon"/>.</item>
    /// <item>The saloon context was entered in the current town
    /// (<see cref="CurrentActionContextTownId"/> matches <see cref="CurrentTown"/>).</item>
    /// <item>The current town visit has an active saloon person of interest matching
    /// <paramref name="targetSuspectId"/>.</item>
    /// </list>
    /// Future non-saloon POI locations should extend this helper rather than weakening the
    /// call-site check to "any non-None context." See BUNCH-80 review feedback.
    /// </summary>
    public bool CanConfrontWantedSuspectInCurrentContext(SuspectId targetSuspectId)
    {
        var activeSaloonPoiId = CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId;
        var inputs = new CanConfrontInContextInputs(targetSuspectId, CurrentTown.TownId, activeSaloonPoiId);
        return _actionContextTracker.CanConfrontWantedSuspectInCurrentContext(inputs);
    }

    /// <summary>
    /// Produces a typed domain event: applies it through the single mutation path (Apply)
    /// and adds it to <see cref="UncommittedEvents"/>. Used by command methods and
    /// <see cref="EnterActionContext"/>. This is the canonical event-sourcing produce step.
    /// </summary>
    internal void ProduceEvent<T>(T e) where T : IDomainEvent
    {
        ApplyProducedEvent(e);
        _uncommittedEvents.Add(e);
    }

    /// <summary>
    /// Dispatches a produced event to the appropriate Apply overload. Mirrors the replay
    /// dispatcher in <see cref="GameSessionEventReplay.ApplyEvent"/> so command-path and
    /// replay-path mutation stay identical. Throws for unknown event types.
    /// </summary>
    private void ApplyProducedEvent(IDomainEvent e)
    {
        switch (e)
        {
            case GameStarted gs:
                Apply(gs);
                break;
            case PlaythroughArchived pa:
                Apply(pa);
                break;
            case StoreItemPurchased p:
                Apply(p);
                break;
            case InvestigationPerformed ip:
                Apply(ip);
                break;
            case TownActionContextEntered tc:
                Apply(tc);
                break;
            case SaloonPersonOfInterestSpotted sp:
                Apply(sp);
                break;
            case WantedSuspectConfronted wc:
                Apply(wc);
                break;
            case SheriffTurnInSettled ts:
                Apply(ts);
                break;
            case UnrelatedCriminalTurnInSettled ucts:
                Apply(ucts);
                break;
            case SaloonPersonOfInterestConfronted sc:
                Apply(sc);
                break;
            case JourneyStarted js:
                Apply(js);
                break;
            case TravelDayAdvanced tda:
                Apply(tda);
                break;
            case TrailEventApplied tea:
                Apply(tea);
                break;
            case JourneyEncounterResolved jer:
                Apply(jer);
                break;
            case JourneyCompleted jc:
                Apply(jc);
                break;
            case JourneyArrivalAcknowledged jaa:
                Apply(jaa);
                break;
            case DevTravelOverrideForced dtf:
                Apply(dtf);
                break;
            case DevTravelOverrideCleared dtc:
                Apply(dtc);
                break;
            case DevTravelOverrideConsumed dtc2:
                Apply(dtc2);
                break;
            case DevSaloonOverrideForced dsf:
                Apply(dsf);
                break;
            case DevSaloonOverrideCleared dsc:
                Apply(dsc);
                break;
            case DevSaloonOverrideConsumed dsc2:
                Apply(dsc2);
                break;
            case DevSaltSourceForced dsf:
                Apply(dsf);
                break;
            case DevSaltSourceCleared dsc:
                Apply(dsc);
                break;
            case DevLayoutSaltsForced dlsf:
                Apply(dlsf);
                break;
            case DevDifficultyForced ddf:
                Apply(ddf);
                break;
            case DevEntropyChanged dec:
                Apply(dec);
                break;
            case WorldGenerated wg:
                Apply(wg);
                break;
            default:
                throw new InvalidOperationException($"Unknown domain event type: {e.GetType().Name}");
        }
    }

    /// <summary>
    /// Applies a <see cref="TownActionContextEntered"/> event to mutate session state.
    /// This is the event-sourced mutation path for the clock/context correction: it sets both
    /// <see cref="CurrentActionContext"/> and <see cref="Clock"/> from the event so that
    /// command execution and replay produce identical state. See ADR-0028 and BUNCH-80.
    /// </summary>
    private void Apply(TownActionContextEntered e)
    {
        _actionContextTracker.Apply(e);
        Clock.Set(e.Day, e.Turn);
        PursuitState.SetHeat(e.PursuitHeat);
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="SaloonPersonOfInterestSpotted"/> event to mutate session state.
    /// This is the event-sourced mutation path for the saloon look-around flow: it marks the
    /// saloon source as spent and sets the active saloon person of interest. Clock advancement
    /// is handled by EnterActionContext. Log/journal entries are projected from the event
    /// stream via JournalLogProjector. See ADR-0028 and BUNCH-80.
    /// </summary>
    private void Apply(SaloonPersonOfInterestSpotted e)
    {
        CurrentTown.CheckSource(e.SourceKind);

        if (e.SuspectId is not null && e.Descriptor is not null)
        {
            CurrentTownVisit.CurrentTownState.SetActiveSaloonPersonOfInterest(e.SuspectId.Value, e.Descriptor);
        }
        else if (e.Descriptor is not null)
        {
            CurrentTownVisit.CurrentTownState.SetActiveSaloonCitizenPersonOfInterest(e.Descriptor, e.CitizenRole);
        }

        _version++;
    }

    /// <summary>
    /// Applies a <see cref="WantedSuspectConfronted"/> event to mutate session state.
    /// This is the event-sourced mutation path for the wanted-suspect confrontation flow:
    /// it records the confrontation state (for non-abandoned outcomes) and updates the
    /// wanted-suspect presence ledger. Clock advancement is handled by EnterActionContext.
    /// Log/journal entries are projected from the event stream via JournalLogProjector.
    /// The Clock.Turn + 1 offset is removed — confrontation state records Clock.Turn directly.
    /// See ADR-0028 and BUNCH-80.
    /// </summary>
    private void Apply(WantedSuspectConfronted e)
    {
        if (e.Outcome is not WantedSuspectConfrontationOutcome.Abandoned)
        {
            var confrontationState = new WantedSuspectConfrontationState(
                e.TargetSuspectId,
                e.TargetName,
                e.Disposition,
                e.Outcome,
                e.IsAlive,
                e.IsSecured,
                Clock.Day,
                Clock.Turn);
            CaseFile.RecordWantedSuspectConfrontationState(confrontationState);
            _bountyLoop.Apply(e);
        }

        _version++;
    }

    /// <summary>
    /// Applies a <see cref="SheriffTurnInSettled"/> event to mutate session state.
    /// This is the event-sourced mutation path for the sheriff turn-in flow: it adjusts
    /// the player's wallet by the bounty amount and records the settlement state.
    /// Clock advancement is handled by EnterActionContext(SheriffOffice).
    /// See ADR-0028 and BUNCH-80.
    /// </summary>
    private void Apply(SheriffTurnInSettled e)
    {
        Player.AdjustCash(e.BountyAmount);

        var settlementState = new SheriffTurnInSettlementState(
            e.TargetSuspectId, e.TargetName, e.Disposition,
            e.IsAlive, e.BountyAmount, e.Day, e.Turn);
        CaseFile.RecordSheriffTurnInSettlementState(settlementState);

        // BUNCH-107: a gang member taken in reduces the unrelated-criminal parity
        // target. The ledger despawns excess unrelated criminals (preferring ones
        // the player has not collected a warrant for) to maintain parity. The
        // despawned warrants are retired from the surfacing pool.
        _bountyLoop.Apply(e);

        _version++;
    }

    /// <summary>
    /// Applies an <see cref="UnrelatedCriminalTurnInSettled"/> event to mutate session state.
    /// Pays the bounty, records the take-in on the ledger (which may spawn a replacement),
    /// and marks the warrant as collected. See BUNCH-107.
    /// </summary>
    private void Apply(UnrelatedCriminalTurnInSettled e)
    {
        Player.AdjustCash(e.BountyAmount);

        _bountyLoop.Apply(e);

        _version++;
    }

    /// <summary>
    /// Applies a <see cref="SaloonPersonOfInterestConfronted"/> event to mutate session state.
    /// This is the event-sourced mutation path for the saloon person confrontation flow:
    /// it clears the active saloon person of interest and optionally fines the player.
    /// Log entries come from delegated WantedSuspectConfronted events via JournalLogProjector.
    /// Clock advancement is handled by EnterActionContext (already in Saloon context).
    /// See ADR-0028 and BUNCH-80.
    /// </summary>
    private void Apply(SaloonPersonOfInterestConfronted e)
    {
        CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();

        if (e.FineAmount is { } fine && fine > 0m)
        {
            Player.AdjustCash(-fine);
        }

        _version++;
    }

    /// <summary>
    /// Applies a <see cref="JourneyStarted"/> event. JourneySnapshot is ABSOLUTE —
    /// JourneyLoop.Apply sets the active journey from it. GameSession sets pursuit
    /// heat. See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void Apply(JourneyStarted e)
    {
        _journeyLoop.Apply(e);
        PursuitState.SetHeat(e.PursuitHeat);
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="TravelDayAdvanced"/> event. Day is ABSOLUTE — Apply calls
    /// <see cref="GameClock.Set"/>. JourneySnapshot is ABSOLUTE. HealthDelta is ADDITIVE.
    /// PursuitHeat is ABSOLUTE — Apply calls <see cref="PursuitState.SetHeat"/>.
    /// AdditionalDiaryMessages and DiaryMessage are projected from the event stream
    /// via JournalLogProjector on read paths. See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void Apply(TravelDayAdvanced e)
    {
        Clock.Set(e.Day, turn: 0);
        _journeyLoop.Apply(e);
        if (e.HealthDelta != 0)
            Player.AdjustHealth(e.HealthDelta);
        PursuitState.SetHeat(e.PursuitHeat);
        // Player food/canteen/horse feed/horse state are set ABSOLUTE from the journey
        // snapshot. On the command path, JourneyLoop.PrepareTravelDayAdvance
        // already set these values, so these are no-ops. On replay, they set the
        // correct values from the snapshot. See ADR-0028 and BUNCH-83.
        SyncPlayerFromJourneySnapshot(e.JourneySnapshot);
        _version++;
    }

    /// <summary>
    /// Sets player food, canteen, horse feed, and horse state to match the journey
    /// snapshot. Used by Apply(TravelDayAdvanced) to converge command and replay paths.
    /// On the command path, these are no-ops (player state already matches).
    /// On replay, they set the correct values from the authoritative snapshot.
    /// </summary>
    private void SyncPlayerFromJourneySnapshot(TravelJourneySnapshot snapshot)
    {
        var foodDelta = snapshot.AvailableFood - Player.GetQuantity(ItemKind.Food);
        if (foodDelta != 0)
            ApplyFoodDelta(foodDelta);

        var canteen = Player.GetCanteenState();
        if (canteen is not null && canteen.Charges != snapshot.AvailableCanteenCharges)
            Player.SetCanteenState(new CanteenState(
                Math.Min(canteen.Capacity, snapshot.AvailableCanteenCharges),
                canteen.Capacity));

        var horseFeedDelta = snapshot.AvailableHorseFeed - Player.GetQuantity(ItemKind.HorseFeed);
        if (horseFeedDelta != 0)
        {
            if (horseFeedDelta > 0)
                Player.AddItem(ItemKind.HorseFeed, horseFeedDelta);
            else
                Player.RemoveQuantity(ItemKind.HorseFeed, -horseFeedDelta);
        }

        if (snapshot.HorseState is { } horseState)
            Player.SetHorseState(horseState);
    }

    /// <summary>
    /// Applies a <see cref="TrailEventApplied"/> event. JourneySnapshot is ABSOLUTE.
    /// WalletCash and PursuitHeat are ABSOLUTE — Apply sets player wallet and pursuit heat directly.
    /// Food/canteen/horse are synced ABSOLUTE from the journey snapshot.
    /// Horse/delay/mode fields are informational (journey snapshot is the source of truth).
    /// See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void Apply(TrailEventApplied e)
    {
        _journeyLoop.Apply(e);
        Player.SetCash(e.WalletCash);
        PursuitState.SetHeat(e.PursuitHeat);
        SyncPlayerFromJourneySnapshot(e.JourneySnapshot);
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="JourneyEncounterResolved"/> event. JourneySnapshot is ABSOLUTE.
    /// PlayerHealth and WalletCash are ABSOLUTE — Apply sets them from the event.
    /// AmmoSpent and StolenItem are ADDITIVE — Apply applies them to the player.
    /// PursuitHeat is ABSOLUTE — Apply sets pursuit heat from it.
    /// See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void Apply(JourneyEncounterResolved e)
    {
        _journeyLoop.Apply(e);
        Player.SetHealth(e.PlayerHealth);
        Player.SetCash(e.WalletCash);
        if (e.AmmoSpent > 0)
            SpendFirearmAmmo(e.AmmoSpent);
        if (e.StolenItemKind is { } kind && e.StolenItemQuantity > 0)
            Player.RemoveQuantity(kind, e.StolenItemQuantity);
        PursuitState.SetHeat(e.PursuitHeat);
        SyncPlayerFromJourneySnapshot(e.JourneySnapshot);
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="JourneyCompleted"/> event. DestinationTownId is ABSOLUTE —
    /// Apply sets player town. JourneySnapshot is ABSOLUTE. Arrival side effects
    /// (town visit refresh, canteen refill) are deterministic and applied here so
    /// command and replay paths stay identical. See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void Apply(JourneyCompleted e)
    {
        _journeyLoop.Apply(e);
        Player.TravelTo(e.DestinationTownId);
        RefreshTownVisit(e.DestinationTownId);
        RefillCanteenAfterArrival();
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="JourneyArrivalAcknowledged"/> event. JourneyLoop.Apply
    /// archives the current journey into <see cref="CompletedJourneyHistory"/> and
    /// clears the active journey. See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void Apply(JourneyArrivalAcknowledged e)
    {
        _journeyLoop.Apply(e);
        _version++;
    }

    /// <summary>
    /// Applies a DevTravelOverrideForced event. JourneyLoop.Apply sets the pending
    /// dev override. Dev-only event — does not affect gameplay state directly.
    /// See BUNCH-89.
    /// </summary>
    internal void Apply(DevTravelOverrideForced e)
    {
        _journeyLoop.Apply(e);
        _version++;
    }

    /// <summary>
    /// Applies a DevTravelOverrideCleared event. JourneyLoop.Apply clears the
    /// pending dev override. Dev-only event. See BUNCH-89.
    /// </summary>
    internal void Apply(DevTravelOverrideCleared e)
    {
        _journeyLoop.Apply(e);
        _version++;
    }

    /// <summary>
    /// Applies a DevTravelOverrideConsumed event. JourneyLoop.Apply clears the
    /// pending dev override. This is the replay-safe consumption path: replaying
    /// Forced -> Consumed -> TravelDayAdvanced reconstructs the correct final state
    /// with no pending override. Dev-only event — not a gameplay outcome. See BUNCH-89.
    /// </summary>
    internal void Apply(DevTravelOverrideConsumed e)
    {
        _journeyLoop.Apply(e);
        _version++;
    }

    /// <summary>
    /// Applies a DevSaloonOverrideForced event. Sets the pending dev saloon override.
    /// Dev-only event - does not affect gameplay state directly. See BUNCH-90.
    /// </summary>
    internal void Apply(DevSaloonOverrideForced e)
    {
        _bountyLoop.Apply(e);
        _version++;
    }

    /// <summary>
    /// Applies a DevSaloonOverrideCleared event. Clears the pending dev saloon override.
    /// Dev-only event. See BUNCH-90.
    /// </summary>
    internal void Apply(DevSaloonOverrideCleared e)
    {
        _bountyLoop.Apply(e);
        _version++;
    }

    /// <summary>
    /// Applies a DevSaloonOverrideConsumed event. Clears the pending dev saloon override.
    /// This is the replay-safe consumption path: replaying Forced -> Consumed ->
    /// SaloonPersonOfInterestSpotted reconstructs the correct final state with no
    /// pending override. Dev-only event - not a gameplay outcome. See BUNCH-90.
    /// </summary>
    internal void Apply(DevSaloonOverrideConsumed e)
    {
        _bountyLoop.Apply(e);
        _version++;
    }

    /// <summary>
    /// Applies a DevSaltSourceForced event. Replaces the RNG salt posture with the
    /// forced fixed salt source. Dev-only event — does not affect gameplay state
    /// directly. The salt source is persisted in the session snapshot, so
    /// rehydration after a salt change requires no new persistence shape.
    /// See BUNCH-101.
    /// </summary>
    internal void Apply(DevSaltSourceForced e)
    {
        SaltSource = e.ForcedSaltSource;
        _version++;
    }

    /// <summary>
    /// Applies a DevSaltSourceCleared event. Restores runtime RNG.
    /// Dev-only event. See BUNCH-101.
    /// </summary>
    internal void Apply(DevSaltSourceCleared e)
    {
        SaltSource = SaltSource.CreateRuntime();
        _version++;
    }

    /// <summary>
    /// Applies a DevLayoutSaltsForced event. Sets the dev layout salts for town layout generation.
    /// Dev-only event. See BUNCH-147.
    /// </summary>
    internal void Apply(DevLayoutSaltsForced e)
    {
        DevLayoutSalts = e.DevLayoutSalts;
        _version++;
    }

    /// <summary>
    /// Applies a DevDifficultyForced event. Changes the session difficulty,
    /// which changes the derived TravelRules profile. Dev-only event — does
    /// not affect starting health/cash or any other gameplay state directly.
    /// See BUNCH-94.
    /// </summary>
    internal void Apply(DevDifficultyForced e)
    {
        GameDifficulty = e.ForcedDifficulty;
        _version++;
    }

    /// <summary>
    /// Applies a DevEntropyChanged event. Changes the session entropy,
    /// which affects travel variance going forward. Dev-only event — does
    /// not affect past travel outcomes or hidden truth.
    /// See BUNCH-93.
    /// </summary>
    internal void Apply(DevEntropyChanged e)
    {
        GameEntropy = e.NewEntropy;
        _version++;
    }

    /// <summary>
    /// Adjusts player food by a signed delta. Positive deltas add food; negative
    /// deltas remove food. Used by travel Apply methods for additive food deltas.
    /// </summary>
    private void ApplyFoodDelta(int foodDelta)
    {
        if (foodDelta > 0)
            Player.AddItem(ItemKind.Food, foodDelta);
        else if (foodDelta < 0)
            Player.RemoveQuantity(ItemKind.Food, -foodDelta);
    }

    /// <summary>
    /// Adjusts player canteen charges by a signed delta. Used by travel Apply methods
    /// for additive canteen charge deltas.
    /// </summary>
    private void ApplyCanteenChargeDelta(int canteenChargeDelta)
    {
        var canteen = Player.GetCanteenState();
        if (canteen is null)
            return;
        Player.SetCanteenState(canteen.AdjustCharges(canteenChargeDelta));
    }

    /// <summary>
    /// Starts a new game session in the setup-complete phase (after player has completed initial setup).
    /// This creates a session that has PlayerSetupCompleted and WorldGenerated applied but not yet GameStarted.
    /// The session can be advanced to GameStarted by calling CompleteGameStart().
    /// </summary>
    public static GameSession StartSetup(
        string playerName,
        DomainWorld world,
        CaseFile caseFile,
        GameDifficulty gameDifficulty,
        GameEntropy gameEntropy,
        string seedCode,
        SaltSource saltSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(caseFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);

        var setupEvent = new PlayerSetupCompleted
        {
            PlayerName = playerName,
            GameDifficulty = gameDifficulty,
            GameEntropy = gameEntropy,
            SeedCode = seedCode
        };

        var caseFileSnapshot = WildBunch.Domain.Cases.CaseFileSnapshot.FromDomain(caseFile);
        var worldEvent = new WorldGenerated
        {
            SeedCode = seedCode,
            SaltSource = saltSource,
            GameEntropy = gameEntropy,
            World = WorldSnapshot.FromDomain(world),
            CaseFile = caseFileSnapshot
        };

        var placeholderPlayer = new Player(
            playerName,
            currentTownId: null,
            health: StartingHealthFor(gameDifficulty),
            WildBunch.Domain.Economy.Wallet.Starting(25m),
            DomainInventory.Empty());

        var session = new GameSession(
            GameSessionId.New(),
            placeholderPlayer,
            world,
            caseFile,
            new PursuitState(),
            new GameClock(),
            GameStatus.Active,
            journey: null,
            gameDifficulty,
            saltSource,
            gameEntropy,
            currentTownVisit: null,
            Array.Empty<TravelJourneySnapshot>(),
            Array.Empty<WantedSuspectPresenceEntry>());

        session.Apply(setupEvent);
        session._uncommittedEvents.Add(setupEvent);
        session.Apply(worldEvent);
        session._uncommittedEvents.Add(worldEvent);

        var caseFileEvent = new CaseFileGenerated
        {
            CaseFile = CaseFileSnapshot.FromDomain(caseFile)
        };

        session.Apply(caseFileEvent);
        session._uncommittedEvents.Add(caseFileEvent);

        return session;
    }

    /// <summary>
    /// Creates a minimal game session in the prepped phase (before world generation).
    /// The session has seed, difficulty, and entropy but no world yet.
    /// Used for the multi-phase setup flow where dev injections happen before world generation.
    /// </summary>
    public static GameSession StartPrepped(
        string seedCode,
        GameDifficulty gameDifficulty,
        GameEntropy gameEntropy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);

        var placeholderPlayer = new Player(
            "Prepped",
            currentTownId: null,
            health: 1000,
            WildBunch.Domain.Economy.Wallet.Starting(0m),
            DomainInventory.Empty());

        var session = new GameSession(
            GameSessionId.New(),
            placeholderPlayer,
            world: null,
            caseFile: null,
            new PursuitState(),
            new GameClock(),
            GameStatus.Prepped,
            journey: null,
            gameDifficulty,
            SaltSource.CreateRuntime(),
            gameEntropy,
            currentTownVisit: null,
            Array.Empty<TravelJourneySnapshot>(),
            Array.Empty<WantedSuspectPresenceEntry>());

        session.SeedCode = seedCode;

        return session;
    }

    /// <summary>
    /// Records the player's starting town choice. Emits StartingTownSelected.
    /// Must be called after ViewPrologue and before CompleteGameStart.
    /// </summary>
    public void SelectStartingTown(TownId startingTownId)
    {
        ArgumentNullException.ThrowIfNull(startingTownId);

        if (StartFlowPhase == StartFlowPhase.StartingTownSelected)
            return; // Idempotent

        if (StartFlowPhase != StartFlowPhase.PrologueViewed)
            throw new InvalidOperationException("Cannot select starting town before viewing the prologue.");

        var town = World.GetTown(startingTownId);

        var e = new StartingTownSelected
        {
            StartingTownId = startingTownId
        };

        Apply(e);
        _uncommittedEvents.Add(e);
    }

    /// <summary>
    /// Completes the game start by emitting GameStarted.
    /// This transitions the session from StartingTownSelected to GameStarted.
    /// The wallet and inventory come from the difficulty envelope (application layer concern).
    /// </summary>
    public void CompleteGameStart(
        WildBunch.Domain.Economy.Wallet? wallet = null,
        DomainInventory? inventory = null)
    {
        if (StartFlowPhase == StartFlowPhase.GameStarted)
            return;

        if (StartFlowPhase != StartFlowPhase.StartingTownSelected)
            throw new InvalidOperationException("Cannot complete game start before selecting a starting town.");

        var startingTownId = _selectedStartingTownId
            ?? throw new InvalidOperationException("No starting town selected.");
        var startingTown = World.GetTown(startingTownId);
        var startingHealth = StartingHealthFor(GameDifficulty);
        var resolvedWallet = wallet ?? WildBunch.Domain.Economy.Wallet.Starting(25m);
        var resolvedInventory = inventory ?? DomainInventory.Empty();

        var e = new GameStarted
        {
            PlayerName = Player.Name,
            StartingTownId = startingTown.Id,
            StartingTownName = startingTown.Name,
            StartingHealth = startingHealth,
            StartingWallet = resolvedWallet.Cash,
            StartingInventoryItems = resolvedInventory.Items.ToArray(),
            GameDifficulty = GameDifficulty,
            SaltSource = SaltSource,
            GameEntropy = GameEntropy,
            SeedCode = SeedCode
        };

        Apply(e);
        _uncommittedEvents.Add(e);
    }

    /// <summary>
    /// Records that the player has viewed the prologue and the starting clue was revealed.
    /// This emits a <see cref="PrologueViewed"/> event and advances the start flow phase.
    /// </summary>
    public void ViewPrologue(string revealedSuspectIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revealedSuspectIdentifier);

        if (StartFlowPhase == StartFlowPhase.PrologueViewed)
        {
            return; // Already viewed — idempotent
        }

        if (StartFlowPhase == StartFlowPhase.NotStarted)
        {
            throw new InvalidOperationException("Cannot view prologue before setup is complete.");
        }

        if (StartFlowPhase == StartFlowPhase.GameStarted)
        {
            throw new InvalidOperationException("Cannot view prologue after the game has started.");
        }

        var e = new PrologueViewed
        {
            RevealedSuspectIdentifier = revealedSuspectIdentifier
        };

        Apply(e);
        _uncommittedEvents.Add(e);
    }

    /// <summary>
    /// Archives this playthrough: marks the session <see cref="GameStatus.Archived"/>
    /// and emits a <see cref="PlaythroughArchived"/> event carrying a snapshot of the
    /// player's last position (town, day, turn) and the status before archive. Archive
    /// is a lifecycle mutation, not a deletion — the session remains queryable. See BUNCH-102.
    /// </summary>
    /// <param name="archiveReason">Caller-supplied reason recorded on the event (e.g. "start-over").</param>
    /// <param name="archivedAtUtc">Optional archive timestamp; defaults to <see cref="DateTime.UtcNow"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when the session is already archived.</exception>
    public void ArchivePlaythrough(string archiveReason, DateTime? archivedAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archiveReason);

        if (Status == GameStatus.Archived)
        {
            throw new InvalidOperationException("Cannot archive a playthrough that is already archived.");
        }

        var e = new PlaythroughArchived
        {
            ArchivedAtUtc = archivedAtUtc ?? DateTime.UtcNow,
            ArchiveReason = archiveReason,
            PlayerName = Player.Name,
            LastTownId = IsSetupPhase ? null : CurrentTown.TownId,
            LastTownName = IsSetupPhase ? null : CurrentTown.TownName,
            Day = Clock.Day,
            Turn = Clock.Turn.ToString(),
            StatusBeforeArchive = Status
        };

        ProduceEvent(e);
    }

    private static int StartingHealthFor(GameDifficulty gameDifficulty)
        => gameDifficulty switch
        {
            GameDifficulty.Easy => 1250,
            GameDifficulty.Challenging => 800,
            GameDifficulty.Brutal => 600,
            _ => 1000
        };

    /// <summary>
    /// Applies a <see cref="GameStarted"/> event to mutate session state.
    /// This is the event-sourced mutation path for the start-new-game flow.
    /// </summary>
    private void Apply(GameStarted e)
    {
        var inventory = new DomainInventory(e.StartingInventoryItems);
        Player = new Player(
            e.PlayerName,
            e.StartingTownId,
            health: e.StartingHealth,
            WildBunch.Domain.Economy.Wallet.Starting(e.StartingWallet),
            inventory);
        Status = GameStatus.Active;
        GameDifficulty = e.GameDifficulty;
        SaltSource = e.SaltSource;
        GameEntropy = e.GameEntropy;
        SeedCode = e.SeedCode;
        StartFlowPhase = StartFlowPhase.GameStarted;
        // Create _currentTown now that the real starting town is known.
        // During setup phase (StartSetup, RehydrateFromEvents), _currentTown was
        // left null because the player hadn't selected a town yet. For snapshot-based
        // loads, _currentTown was already created by the constructor.
        if (_currentTown is null)
        {
            _currentTown = new TownAggregate(World.GetTown(e.StartingTownId), new TownVisitState(e.StartingTownId));
            _currentTown.PrimeCurrentTown();
        }
        else if (!_currentTown.TownId.Equals(e.StartingTownId))
        {
            _currentTown.EnterTown(World.GetTown(e.StartingTownId));
        }
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="PlayerSetupCompleted"/> event to mutate session state.
    /// This marks the transition from "no game" to "setup complete, ready to view prologue".
    /// Sets SeedCode, GameDifficulty, and GameEntropy from the event so that
    /// setup-phase sessions have these values available before GameStarted is emitted.
    /// </summary>
    private void Apply(PlayerSetupCompleted e)
    {
        StartFlowPhase = StartFlowPhase.SetupComplete;
        SeedCode = e.SeedCode;
        GameDifficulty = e.GameDifficulty;
        GameEntropy = e.GameEntropy;
        Player = new Player(
            e.PlayerName,
            currentTownId: null,
            health: Player.Health,
            Player.Wallet,
            Player.Inventory);
        _version++;
    }

    private void Apply(WorldGenerated e)
    {
        World = e.World.ToDomain();
        CaseFile = e.CaseFile.ToDomain();
        SaltSource = e.SaltSource;
        GameEntropy = e.GameEntropy;
        _version++;
    }

    private void Apply(CaseFileGenerated e)
    {
        CaseFile = e.CaseFile.ToDomain();
        _version++;
    }

    private void Apply(StartingTownSelected e)
    {
        StartFlowPhase = StartFlowPhase.StartingTownSelected;
        _selectedStartingTownId = e.StartingTownId;
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="PrologueViewed"/> event to mutate session state.
    /// This marks the transition from "setup complete" to "ready to select starting town".
    /// </summary>
    private void Apply(PrologueViewed e)
    {
        StartFlowPhase = StartFlowPhase.PrologueViewed;
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="PlaythroughArchived"/> event to mutate session state.
    /// This is the event-sourced mutation path for the archive flow: it sets
    /// <see cref="Status"/> to <see cref="GameStatus.Archived"/>. The event carries
    /// the pre-archive status and last-position snapshot as decision data; the
    /// snapshot is not re-applied to live state (archive is terminal for play).
    /// See ADR-0028 and BUNCH-102.
    /// </summary>
    private void Apply(PlaythroughArchived e)
    {
        Status = GameStatus.Archived;
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="StoreItemPurchased"/> event to mutate session state.
    /// This is the event-sourced mutation path for the purchase flow.
    /// </summary>
    private void Apply(StoreItemPurchased e)
    {
        Player.SpendCash(e.TotalPrice);
        Player.AddItem(e.ItemKind, e.Quantity);
        _version++;
    }

    /// <summary>
    /// Applies an <see cref="InvestigationPerformed"/> event to mutate session state.
    /// This is the event-sourced mutation path for investigation flows: it marks the
    /// investigation source as spent for the current visit and reveals the clue and/or
    /// warrant carried by the event. Clock advancement is handled by EnterActionContext.
    /// Log/journal entries are projected from the event stream via JournalLogProjector.
    /// See ADR-0028.
    /// </summary>
    private void Apply(InvestigationPerformed e)
    {
        if (e.SourceKind == InvestigationSourceKind.SheriffWarrants)
        {
            CurrentTown.CheckWantedPosters();
        }
        else
        {
            CurrentTown.CheckSource(e.SourceKind);
        }

        if (e.ClueId is not null)
        {
            CaseFile.RevealClueById(e.ClueId.Value);
        }

        if (e.WarrantId is not null)
        {
            CaseFile.RevealWarrantById(e.WarrantId.Value);
        }

        _version++;
    }

    public WantedSuspectPresenceState GetWantedSuspectPresenceState(SuspectId suspectId)
        => _bountyLoop.GetWantedSuspectPresenceState(suspectId);

    public bool TryGetWantedSuspectPresenceState(SuspectId suspectId, out WantedSuspectPresenceState state)
        => _bountyLoop.TryGetWantedSuspectPresenceState(suspectId, out state);

    public void SetWantedSuspectPresenceState(SuspectId suspectId, WantedSuspectPresenceState state)
        => _bountyLoop.SetWantedSuspectPresenceState(suspectId, state);

    public TravelJourneyStepResult StartJourney(TravelPreview preview)
    {
        if (IsArchived)
        {
            return TravelJourneyStepResult.Failed(ArchivedBlockMessage);
        }

        ArgumentNullException.ThrowIfNull(preview);

        var context = new StartJourneyContext(preview, _journeyLoop.NextJourneySequence, TravelRules);
        var result = _journeyLoop.StartJourney(context);
        foreach (var e in result.Events)
        {
            ProduceEvent(e);
        }
        return result.Result;
    }

    public TravelJourneyStepResult AdvanceJourneyDay()
    {
        if (IsArchived)
        {
            return TravelJourneyStepResult.Failed(ArchivedBlockMessage);
        }

        // Advance clock only when the journey will actually advance (not blocked
        // by a pending encounter or inactive status). The clock advance must happen
        // before trail events are produced so the event's Day field captures the
        // correct day. See ADR-0028 and BUNCH-83.
        if (Journey is not null && Journey.PendingEncounter is null && Journey.Status == JourneyStatus.Active)
        {
            Clock.AdvanceTravelDay();
        }

        var caps = Player.GetCapabilities(TravelRules);
        var context = new AdvanceJourneyDayContext(
            TravelRules,
            SaltSource.Salt,
            SaltSource.Mode,
            GameEntropy,
            Clock.Day,
            PursuitState.Heat,
            new PlayerCapabilities(caps.MountedTravelAvailable, caps.FirearmThreatAvailable),
            Player.GetQuantity(ItemKind.Food),
            Player.GetQuantity(ItemKind.HorseFeed),
            Player.GetCanteenState(),
            Player.GetHorseState(),
            Player.Wallet.Cash,
            Player.Health,
            Player.GetQuantity(ItemKind.RevolverAmmo) + Player.GetQuantity(ItemKind.RifleAmmo));

        var result = _journeyLoop.AdvanceJourneyDay(context);
        foreach (var e in result.Events)
        {
            ProduceEvent(e);
        }
        return result.Result;
    }

    /// <summary>
    /// Dev command: forces the next travel-day generation to use the given override.
    /// Produces a DevTravelOverrideForced event. The override is consumed once by
    /// the next AdvanceJourneyDay. See BUNCH-89.
    /// </summary>
    public void ForceDevTravelOverride(DevTravelOverride overrideValue)
    {
        ArgumentNullException.ThrowIfNull(overrideValue);
        var context = new ForceDevTravelOverrideContext(overrideValue);
        var result = _journeyLoop.ForceDevTravelOverride(context);
        foreach (var e in result.Events)
        {
            ProduceEvent(e);
        }
    }

    /// <summary>
    /// Dev command: clears any pending travel override.
    /// Produces a DevTravelOverrideCleared event. See BUNCH-89.
    /// </summary>
    public void ClearDevTravelOverride()
    {
        var result = _journeyLoop.ClearDevTravelOverride();
        foreach (var e in result.Events)
        {
            ProduceEvent(e);
        }
    }

    /// <summary>
    /// Dev command: forces the next saloon look-around to use the given override.
    /// Produces a DevSaloonOverrideForced event. The override is consumed once by
    /// the next LookAroundSaloon. Validates suspect eligibility at force time.
    /// See BUNCH-90 and BUNCH-106 realignment.
    /// </summary>
    public void ForceDevSaloonOverride(DevSaloonOverride overrideValue)
    {
        ArgumentNullException.ThrowIfNull(overrideValue);
        if (IsJourneyModal())
        {
            throw new InvalidOperationException("Cannot force a saloon override while a journey is active.");
        }

        var context = new DevSaloonOverrideContext(
            overrideValue,
            CaseFile.Suspects,
            CaseFile.TrueCulpritId,
            CaseFile.KillerReleaseState,
            CitizenCast.Roles.Select(r => r.Key).ToList());

        var result = _bountyLoop.ForceDevSaloonOverride(context);
        foreach (var e in result.Events)
        {
            ProduceEvent(e);
        }
    }

    /// <summary>
    /// Dev command: clears any pending saloon override.
    /// Produces a DevSaloonOverrideCleared event. See BUNCH-90.
    /// </summary>
    public void ClearDevSaloonOverride()
    {
        if (_bountyLoop.PendingDevSaloonOverride is null)
        {
            return; // No-op if nothing to clear - idempotent
        }

        var result = _bountyLoop.ClearDevSaloonOverride();
        foreach (var e in result.Events)
        {
            ProduceEvent(e);
        }
    }

    /// <summary>
    /// Dev command: locks the RNG to a fixed salt for reproducible playtesting.
    /// Sets up reproducibility state; does not force any encounter outcome.
    /// Per dev-overlay doctrine §1 (state/action boundary). See BUNCH-101.
    /// </summary>
    public void ForceDevSaltSource(SaltSource saltSource)
    {
        ArgumentNullException.ThrowIfNull(saltSource);
        if (saltSource.Mode != SaltSourceMode.Fixed)
        {
            throw new ArgumentException("ForceDevSaltSource requires a Fixed salt source.", nameof(saltSource));
        }

        ProduceEvent(new DevSaltSourceForced
        {
            ForcedSaltSource = saltSource
        });
    }

    /// <summary>
    /// Dev command: restores runtime RNG. See BUNCH-101.
    /// </summary>
    public void ClearDevSaltSource()
    {
        ProduceEvent(new DevSaltSourceCleared());
    }

    /// <summary>
    /// Dev command: forces the session difficulty to a new value for playtesting.
    /// Changes the travel rules profile going forward. Does not retroactively
    /// change starting health/cash (those were set at game start).
    /// Per dev-overlay doctrine §1 (state/action boundary). See BUNCH-94.
    /// </summary>
    public void ForceDevDifficulty(GameDifficulty difficulty)
    {
        if (!Enum.IsDefined(typeof(GameDifficulty), difficulty))
        {
            throw new ArgumentException("Invalid game difficulty value.", nameof(difficulty));
        }

        ProduceEvent(new DevDifficultyForced
        {
            ForcedDifficulty = difficulty
        });
    }

    /// <summary>
    /// Dev command: forces the session entropy to a new value for playtesting.
    /// Changes the travel variance profile going forward. Does not retroactively
    /// change past travel outcomes or hidden truth.
    /// Per dev-overlay doctrine §1 (state/action boundary). See BUNCH-93.
    /// </summary>
    public void SetDevEntropy(GameEntropy entropy)
    {
        if (!Enum.IsDefined(typeof(GameEntropy), entropy))
        {
            throw new ArgumentException("Invalid game entropy value.", nameof(entropy));
        }

        ProduceEvent(new DevEntropyChanged
        {
            NewEntropy = entropy
        });
    }

    /// <summary>
    /// Dev command: forces layout salts for town hub layout generation.
    /// Stores dev-controlled layout salts for reproducible layout generation.
    /// Per dev-overlay doctrine §1 (state/action boundary). See BUNCH-147.
    /// </summary>
    public void SetDevLayoutSalts(LayoutSalts layoutSalts)
    {
        ArgumentNullException.ThrowIfNull(layoutSalts);
        ProduceEvent(new DevLayoutSaltsForced(layoutSalts));
    }

    /// <summary>
    /// Transitions a prepped session to active phase by generating the world
    /// with the provided world, case file, and salt source. Used by the three-phase
    /// dev-enabled action pattern (prep → inject dev salts → start). See BUNCH-147.
    /// </summary>
    public void StartFromPrepped(
        DomainWorld world,
        CaseFile caseFile,
        string seedCodeText,
        SaltSource saltSource)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(caseFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCodeText);
        ArgumentNullException.ThrowIfNull(saltSource);

        if (Status != GameStatus.Prepped)
        {
            throw new InvalidOperationException("Session must be in Prepped status to start from prepped.");
        }

        World = world;
        CaseFile = caseFile;
        SeedCode = seedCodeText;
        SaltSource = saltSource;
        Status = GameStatus.Active;

        var caseFileSnapshot = WildBunch.Domain.Cases.CaseFileSnapshot.FromDomain(caseFile);
        var worldEvent = new WorldGenerated
        {
            SeedCode = seedCodeText,
            SaltSource = saltSource,
            GameEntropy = GameEntropy,
            World = WorldSnapshot.FromDomain(world),
            CaseFile = caseFileSnapshot
        };
        ProduceEvent(worldEvent);
    }

    private void RefreshTownVisit(TownId townId)
    {
        if (_currentTown is null)
        {
            throw new InvalidOperationException("Cannot refresh town visit before the game has started.");
        }

        var currentTown = World.GetTown(townId);
        _currentTown.EnterTown(currentTown);
        // The action context is scoped to the current town. When the town changes
        // (including a round-trip back to the same town), the context resets so that
        // re-entering a location in the new town advances time. The context reset is
        // a side effect of the town change, not a gameplay event.
        // See BUNCH-80 review feedback on town-scoped CurrentActionContext.
        ResetActionContextForTownChange();
    }

    /// <summary>
    /// Resets <see cref="CurrentActionContext"/> and <see cref="CurrentActionContextTownId"/>
    /// to None/null. Called by <see cref="RefreshTownVisit"/> when the current town changes.
    /// Also available for test helpers that simulate town changes via
    /// <see cref="TownVisitState.Reset"/> directly.
    /// </summary>
    internal void ResetActionContextForTownChange() => _actionContextTracker.Reset();

    private void RefillCanteenAfterArrival()
    {
        var canteenState = Player.GetCanteenState();
        if (canteenState is null || canteenState.Charges >= canteenState.Capacity)
        {
            return;
        }

        var refilledCanteen = CanteenState.Full(canteenState.Capacity);
        Player.SetCanteenState(refilledCanteen);
    }

    public JourneyArrivalAcknowledgementResult AcknowledgeJourneyArrival()
    {
        if (IsArchived)
        {
            return JourneyArrivalAcknowledgementResult.Failed(ArchivedBlockMessage);
        }

        var context = new AcknowledgeJourneyArrivalContext(TravelRules);
        var result = _journeyLoop.AcknowledgeJourneyArrival(context);
        foreach (var e in result.Events)
        {
            ProduceEvent(e);
        }
        return result.Result;
    }

    internal TravelDayGenerationContext CreateTravelDayGenerationContext(
        int generatorVersion = TravelDayPlanGenerator.CurrentVersion,
        string? gameSeed = null,
        string? scenarioProfileId = null)
    {
        if (Journey is null)
        {
            throw new InvalidOperationException("No active journey is underway.");
        }

        var routeProfile = Journey.Preview.RouteProfile;
        var horseState = Player.GetHorseState();
        var travelDiaryDays = _journeyLoop.TravelDiaryDays;
        var recentTrailEventKinds = travelDiaryDays
            .Select(day => day.TrailEvent?.Kind)
            .Where(kind => kind is not null)
            .Select(kind => kind!.Value)
            .TakeLast(3)
            .ToArray();
        var recentTrailEventIds = travelDiaryDays
            .Select(day => day.TrailEvent?.Id)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .TakeLast(3)
            .ToArray();
        var recentEncounterCategories = travelDiaryDays
            .Select(day => day.PendingEncounter?.Kind switch
            {
                "foe" => TravelDayEncounterCategory.Foe,
                "npc" => TravelDayEncounterCategory.Npc,
                _ => (TravelDayEncounterCategory?)null
            })
            .Where(category => category is not null)
            .Select(category => category!.Value)
            .TakeLast(3)
            .ToArray();

        return new TravelDayGenerationContext(
            generatorVersion,
            gameSeed,
            scenarioProfileId,
            routeProfile.TrailId,
            Journey.Preview.OriginTownId,
            Journey.Preview.DestinationTownId,
            Journey.DaysTravelled,
            Journey.TravelMode,
            routeProfile.Risk,
            routeProfile.Terrain,
            routeProfile.WaterFeature,
            TravelRules.Difficulty,
            Journey.RemainingDays,
            Journey.RemainingRideDayDistance,
            CreateFoodPressureBand(Journey.FoodRemaining, Journey.RemainingDays),
            CreateCanteenPressureBand(Journey.AvailableCanteenCharges, Journey.RemainingDays, Journey.Preview.RouteProfile.WaterFeature, horseState, TravelRules),
            CreateHorseFeedPressureBand(Journey.HorseFeedRemaining, Journey.RemainingDays, Journey.Preview.RouteProfile.Terrain, horseState),
            CreateHorseConditionBand(horseState, TravelRules),
            CreateWalletBand(Player.Wallet.Cash, TravelRules),
            recentTrailEventKinds,
            recentTrailEventIds,
            recentEncounterCategories,
            HasHorse: horseState is not null && !horseState.IsDeadFor(TravelRules),
            SaltSource.Mode,
            SaltSource.Salt,
            GameEntropy);
    }

    private static TravelPressureBand CreateFoodPressureBand(int foodRemaining, int remainingDays)
    {
        if (foodRemaining <= 0)
        {
            return TravelPressureBand.Critical;
        }

        if (foodRemaining == 1)
        {
            return TravelPressureBand.High;
        }

        if (foodRemaining <= remainingDays)
        {
            return TravelPressureBand.Moderate;
        }

        if (foodRemaining <= remainingDays + 1)
        {
            return TravelPressureBand.Low;
        }

        return TravelPressureBand.None;
    }

    private static TravelPressureBand CreateCanteenPressureBand(
        int availableCanteenCharges,
        int remainingDays,
        WaterFeature waterFeature,
        HorseTravelState? horseState,
        TravelRulesProfile travelRulesProfile)
    {
        if (JourneyUpkeepRules.HasRouteWater(waterFeature))
        {
            return TravelPressureBand.None;
        }

        var chargesPerDay = JourneyUpkeepRules.WaterChargesRequiredPerDay(horseState, travelRulesProfile);
        var requiredCharges = remainingDays * chargesPerDay;
        var reserveCharges = availableCanteenCharges - requiredCharges;

        if (reserveCharges < 0)
        {
            return TravelPressureBand.Critical;
        }

        if (reserveCharges == 0)
        {
            return TravelPressureBand.High;
        }

        if (reserveCharges <= chargesPerDay)
        {
            return TravelPressureBand.Moderate;
        }

        if (reserveCharges <= chargesPerDay * 2)
        {
            return TravelPressureBand.Low;
        }

        return TravelPressureBand.None;
    }

    private static TravelPressureBand CreateHorseFeedPressureBand(
        int horseFeedRemaining,
        int remainingDays,
        TrailTerrain terrain,
        HorseTravelState? horseState)
    {
        if (horseState is null || JourneyUpkeepRules.HasGrazing(terrain))
        {
            return TravelPressureBand.None;
        }

        if (horseFeedRemaining <= 0)
        {
            return TravelPressureBand.Critical;
        }

        if (horseFeedRemaining == 1)
        {
            return TravelPressureBand.High;
        }

        if (horseFeedRemaining <= remainingDays)
        {
            return TravelPressureBand.Moderate;
        }

        if (horseFeedRemaining <= remainingDays + 1)
        {
            return TravelPressureBand.Low;
        }

        return TravelPressureBand.None;
    }

    private static HorseConditionBand CreateHorseConditionBand(HorseTravelState? horseState, TravelRulesProfile travelRulesProfile)
    {
        if (horseState is null)
        {
            return HorseConditionBand.None;
        }

        if (horseState.IsDeadFor(travelRulesProfile))
        {
            return HorseConditionBand.Critical;
        }

        if (horseState.IsLameFor(travelRulesProfile))
        {
            return HorseConditionBand.Lame;
        }

        if (horseState.Exhaustion >= 2 || horseState.Hunger >= 2 || horseState.Thirst >= 1)
        {
            return HorseConditionBand.Worn;
        }

        return HorseConditionBand.Sound;
    }

    private static WalletBand CreateWalletBand(decimal cash, TravelRulesProfile travelRulesProfile)
    {
        if (cash <= 0m)
        {
            return WalletBand.Broke;
        }

        if (cash < travelRulesProfile.EncounterBribeCash)
        {
            return WalletBand.Tight;
        }

        if (cash < travelRulesProfile.EncounterBribeCash * 2)
        {
            return WalletBand.Steady;
        }

        if (cash < travelRulesProfile.EncounterBribeCash * 4)
        {
            return WalletBand.Comfortable;
        }

        return WalletBand.Flush;
    }

    public JourneyEncounterResolutionResult ResolveJourneyEncounter(string choiceId)
        => ResolveJourneyEncounter(choiceId, bulletSpend: null, bribeAmount: null, forcedRoll: null);

    public JourneyEncounterResolutionResult ResolveJourneyEncounter(string choiceId, int? bulletSpend, decimal? bribeAmount)
        => ResolveJourneyEncounter(choiceId, bulletSpend, bribeAmount, forcedRoll: null);

    internal JourneyEncounterResolutionResult ResolveJourneyEncounter(
        string choiceId,
        int? bulletSpend,
        decimal? bribeAmount,
        ulong? forcedRoll)
    {
        if (IsArchived)
        {
            return JourneyEncounterResolutionResult.Failed(ArchivedBlockMessage, JourneyStatus.Failed);
        }

        var caps = Player.GetCapabilities(TravelRules);
        var context = new ResolveJourneyEncounterContext(
            TravelRules,
            SaltSource.Salt,
            SaltSource.Mode,
            GameEntropy,
            Clock.Day,
            PursuitState.Heat,
            new PlayerCapabilities(caps.MountedTravelAvailable, caps.FirearmThreatAvailable),
            Player.GetQuantity(ItemKind.Food),
            Player.GetQuantity(ItemKind.HorseFeed),
            Player.GetCanteenState(),
            Player.GetHorseState(),
            Player.Wallet.Cash,
            Player.Health,
            choiceId,
            bulletSpend,
            bribeAmount,
            forcedRoll,
            Player.GetQuantity(ItemKind.RevolverAmmo),
            Player.GetQuantity(ItemKind.RifleAmmo),
            Player.HasItem(ItemKind.Knife));

        var result = _journeyLoop.ResolveJourneyEncounter(context);
        foreach (var e in result.Events)
        {
            ProduceEvent(e);
        }
        return result.Result;
    }

    /// <summary>
    /// Builds the <see cref="UnrelatedCriminalLedger"/> from the case file's
    /// unrelated-criminal warrants and gang roster size, then replays gang take-ins
    /// already recorded on the case file (as <see cref="CaseFile.SheriffTurnInSettlements"/>)
    /// so the ledger's gang-side parity matches the persisted state on snapshot load.
    /// Returns a degenerate empty ledger (gang count 0) when the case file has no
    /// unrelated warrants or when the roster does not satisfy the 3x redundancy
    /// invariant, so the parity system is a safe no-op for test/seed case files
    /// that omit the full unrelated pool. See BUNCH-107.
    /// </summary>
    private static UnrelatedCriminalLedger BuildUnrelatedCriminalLedger(CaseFile caseFile)
    {
        ArgumentNullException.ThrowIfNull(caseFile);

        var unrelatedWarrantIds = caseFile.PublicWarrants
            .Where(warrant => warrant.Terms.TargetKind == InvestigationTargetKind.UnrelatedWantedCriminal)
            .Select(warrant => warrant.Id)
            .ToArray();

        var gangMemberCount = caseFile.Suspects.Count;

        // The parity system only activates when the full unrelated roster is present
        // (at least 3x gang size). Partial test fixtures fall back to a no-op ledger.
        if (gangMemberCount == 0 || unrelatedWarrantIds.Length < gangMemberCount * 3)
        {
            return new UnrelatedCriminalLedger(gangMemberCount: 0, poolSize: 0);
        }

        var ledger = new UnrelatedCriminalLedger(gangMemberCount, unrelatedWarrantIds);

        // Replay gang take-ins already persisted on the case file so the ledger's
        // gang-side parity matches the snapshot. Post-snapshot SheriffTurnInSettled
        // events are replayed separately via Apply(SheriffTurnInSettled).
        var persistedGangTakeIns = Math.Min(caseFile.SheriffTurnInSettlements.Count, gangMemberCount);
        for (var i = 0; i < persistedGangTakeIns; i++)
        {
            ledger.RecordGangMemberTakenIn();
        }

        return ledger;
    }

    public StorePurchaseResult Purchase(StoreOffer offer, int quantity)
    {
        if (IsArchived)
        {
            return StorePurchaseResult.Failed(ArchivedBlockMessage);
        }

        ArgumentNullException.ThrowIfNull(offer);

        if (IsJourneyModal())
        {
            return StorePurchaseResult.Failed(JourneyModalBlockMessage);
        }

        EnterActionContext(TownActionContext.Store);

        var context = new StorePurchaseContext(
            offer,
            quantity,
            CurrentTown.TownId,
            Player.Wallet.Cash,
            Player.CanAfford,
            Player.HasItem);
        var outcome = _storeLoop.Purchase(context);
        if (!outcome.Success)
        {
            return StorePurchaseResult.Failed(outcome.Message);
        }

        ProduceEvent(outcome.Event!);
        return StorePurchaseResult.Succeeded(outcome.Message);
    }

    public ReadWantedPostersResult ReadWantedPosters()
    {
        if (IsArchived)
        {
            return ReadWantedPostersResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return ReadWantedPostersResult.Failed(JourneyModalBlockMessage);
        }

        EnterActionContext(TownActionContext.SheriffOffice);

        var boringSalt = SaltSource.Mode == SaltSourceMode.Fixed ? null : SaltSource;
        var context = new InvestigationContext(
            CaseFile,
            CurrentTownSlotIndex,
            CurrentTownVisitCount,
            boringSalt,
            RetiredWarrantIds,
            CurrentTown.TownId,
            CurrentTown.TownName,
            BeatNarration: null,
            IsSourceSpent: CurrentTownVisit.WantedPostersSpent,
            IsSourceAvailable: true);
        var outcome = _investigationLoop.ReadWantedPosters(context);
        ProduceEvent(outcome.Event);
        return ReadWantedPostersResult.Succeeded(outcome.DisplayMessage, sessionChanged: true);
    }

    public CaseInvestigationResult LookAroundSaloon()
    {
        if (IsArchived)
        {
            return CaseInvestigationResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        // Enter saloon context BEFORE local action resolution.
        // Emits TownActionContextEntered event if context changed (advances turn).
        var beatSpent = Clock.TimeOfDay;
        EnterActionContext(TownActionContext.Saloon);
        var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.Saloon, CurrentTown.TownName);

        var eligibleSuspects = CaseFile.Suspects.Where(IsEligibleSaloonPersonOfInterestCandidate).ToList();
        var context = new SaloonLookAroundContext(
            CurrentTown.TownId,
            Clock.Day,
            Clock.Turn,
            CurrentTownVisit.CurrentTownState.VisitNumber,
            SaltSource.Salt,
            eligibleSuspects,
            CaseFile.KnownWarrants,
            CitizenCast.Roles.Count,
            CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround),
            _bountyLoop.PendingDevSaloonOverride,
            CollectSuspectFeatureDescriptions(),
            (townId, day, turn, visit, features) => CitizenCast.Select(townId, day, turn, visit, features),
            (roleKey, features) => CitizenCast.SelectByRoleKey(roleKey, features),
            encounter => CitizenCast.ResolveDescriptor(encounter));

        var result = _bountyLoop.LookAroundSaloon(context);
        foreach (var e in result.Events)
        {
            ProduceEvent(e);
        }
        return result.Result with { BeatNarration = beatNarration };
    }

    public SaloonPersonOfInterestConfrontationResult ConfrontSaloonPersonOfInterest(string? declaredWantedIdentityHandle = null)
    {
        if (IsArchived)
        {
            return SaloonPersonOfInterestConfrontationResult.Rejected(ArchivedBlockMessage, declaredWantedIdentityHandle);
        }

        var context = new SaloonConfrontationContext(
            CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId,
            CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor,
            CurrentTownVisit.CurrentTownState.ResolveActiveSaloonPersonOfInterestKind(),
            CurrentTownVisit.CurrentTownState.ActiveSaloonCitizenRole,
            CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId is { } poiId
                ? GetWantedSuspectPresenceState(poiId)
                : null,
            CaseFile.Suspects,
            CaseFile.KnownWarrants,
            CaseFile.WantedSuspectConfrontations.ToDictionary(s => s.SuspectId),
            Player.GetCapabilities(TravelRules).FirearmThreatAvailable,
            Player.Wallet.Cash,
            CitizenDeclarationFine,
            IsJourneyModal(),
            JourneyModalBlockMessage,
            Clock.Day,
            Clock.Turn,
            declaredWantedIdentityHandle);

        var outcome = _bountyLoop.ConfrontSaloonPersonOfInterest(context);

        // Produce pre-settlement events.
        foreach (var e in outcome.Events)
        {
            ProduceEvent(e);
        }

        // If there's a settlement request, orchestrate EnterActionContext + SettleSheriffTurnIn
        // between the pre-settlement events and the final SaloonPersonOfInterestConfronted event.
        if (outcome.SettlementRequest is { } request)
        {
            var settlementResult = SettleSheriffTurnIn(request.TargetSuspectId, request.IsAlive);
            if (!settlementResult.Success)
            {
                ProduceEvent(new SaloonPersonOfInterestConfronted
                {
                    Message = settlementResult.Message,
                    DeclaredWantedIdentityHandle = request.DeclaredWantedIdentityHandle,
                    TargetName = request.WarrantTargetName,
                    PersonOfInterestKind = request.PersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                    Outcome = SaloonPersonOfInterestConfrontationOutcome.Rejected
                });
                return SaloonPersonOfInterestConfrontationResult.Rejected(
                    settlementResult.Message,
                    request.DeclaredWantedIdentityHandle,
                    request.WarrantTargetName,
                    settlementResult.Disposition,
                    sessionChanged: true,
                    personOfInterestKind: request.PersonOfInterestKind);
            }

            var settlementMessage = $"{request.ArmedWantedMessage} The sheriff pays you ${settlementResult.BountyAmount:0.00}.";
            ProduceEvent(new SaloonPersonOfInterestConfronted
            {
                Message = settlementMessage,
                DeclaredWantedIdentityHandle = request.DeclaredWantedIdentityHandle,
                TargetSuspectId = request.TargetSuspectId,
                TargetName = request.WarrantTargetName,
                PersonOfInterestKind = request.PersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                Outcome = SaloonPersonOfInterestConfrontationOutcome.Surrendered,
                IsAlive = true,
                IsSecured = true
            });
            return SaloonPersonOfInterestConfrontationResult.FromWantedSuspectResult(
                WantedSuspectConfrontationResult.Surrendered(
                    request.DeclaredWantedIdentityHandle,
                    request.WarrantTargetName,
                    settlementResult.Disposition ?? WarrantDisposition.AliveOnly,
                    request.ArmedWantedMessage)) with
            {
                Message = settlementMessage
            };
        }

        return outcome.Result;
    }

    public WantedSuspectConfrontationResult ConfrontSaloonWantedSuspect(string? declaredWantedIdentityHandle = null)
    {
        if (IsArchived)
        {
            return WantedSuspectConfrontationResult.Rejected(ArchivedBlockMessage, declaredWantedIdentityHandle);
        }

        var activeSaloonSuspectId = CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId;
        if (activeSaloonSuspectId is null)
        {
            return WantedSuspectConfrontationResult.Rejected(
                "There is no wanted suspect waiting in the saloon.",
                declaredWantedIdentityHandle);
        }

        var targetSuspect = CaseFile.Suspects.FirstOrDefault(s => s.Id.Equals(activeSaloonSuspectId));
        if (targetSuspect is null)
        {
            return WantedSuspectConfrontationResult.Rejected(
                "That person is not part of this case.",
                declaredWantedIdentityHandle);
        }

        if (!TryGetKnownWarrantForSuspect(targetSuspect.Id, out _))
        {
            ProduceEvent(new SaloonPersonOfInterestConfronted
            {
                Message = $"There is no wanted notice for {targetSuspect.Name}.",
                DeclaredWantedIdentityHandle = declaredWantedIdentityHandle,
                TargetSuspectId = targetSuspect.Id,
                TargetName = targetSuspect.Name,
                PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
                Outcome = SaloonPersonOfInterestConfrontationOutcome.Rejected
            });
            return WantedSuspectConfrontationResult.Rejected(
                $"There is no wanted notice for {targetSuspect.Name}.",
                declaredWantedIdentityHandle,
                targetSuspect.Name,
                sessionChanged: true);
        }

        // Delegate to ConfrontSaloonPersonOfInterest which now orchestrates through _bountyLoop.
        return ResolveSaloonPersonOfInterestCompatibilityResult(
            ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle));
    }

    public WantedSuspectConfrontationResult ResolveWantedSuspectConfrontation(
        SuspectId targetSuspectId,
        WantedSuspectConfrontationChoice choice,
        string? declaredWantedIdentityHandle = null)
    {
        if (IsArchived)
        {
            return WantedSuspectConfrontationResult.Rejected(ArchivedBlockMessage, declaredWantedIdentityHandle);
        }

        var context = new WantedSuspectConfrontationContext(
            targetSuspectId,
            choice,
            declaredWantedIdentityHandle,
            CanConfrontWantedSuspectInCurrentContext(targetSuspectId),
            IsJourneyModal(),
            JourneyModalBlockMessage,
            CaseFile.Suspects,
            CaseFile.KnownWarrants,
            CaseFile.WantedSuspectConfrontations.ToDictionary(s => s.SuspectId));

        var result = _bountyLoop.ResolveWantedSuspectConfrontation(context);
        foreach (var e in result.Events)
        {
            ProduceEvent(e);
        }
        return result.Result;
    }

    public SheriffTurnInResult AssessSheriffTurnIn(SuspectId targetSuspectId, bool isAlive)
    {
        if (IsArchived)
        {
            return SheriffTurnInResult.Rejected(ArchivedBlockMessage);
        }

        var context = new SheriffTurnInContext(
            targetSuspectId,
            isAlive,
            IsJourneyModal(),
            JourneyModalBlockMessage,
            CaseFile.Suspects,
            CaseFile.KnownWarrants,
            CaseFile.WantedSuspectConfrontations.ToDictionary(s => s.SuspectId),
            CaseFile.SheriffTurnInSettlements,
            Clock.Day,
            Clock.Turn);

        return _bountyLoop.AssessSheriffTurnIn(context);
    }

    public SheriffTurnInResult SettleSheriffTurnIn(SuspectId targetSuspectId, bool isAlive)
    {
        if (IsArchived)
        {
            return SheriffTurnInResult.Rejected(ArchivedBlockMessage);
        }

        // Enter SheriffOffice context BEFORE assessment. This emits a TownActionContextEntered
        // event if the context changed (advances turn). Even rejected turn-ins produce the
        // context event — going to the sheriff's office takes time regardless of outcome.
        var contextChanged = EnterActionContext(TownActionContext.SheriffOffice);

        var context = new SheriffTurnInContext(
            targetSuspectId,
            isAlive,
            IsJourneyModal(),
            JourneyModalBlockMessage,
            CaseFile.Suspects,
            CaseFile.KnownWarrants,
            CaseFile.WantedSuspectConfrontations.ToDictionary(s => s.SuspectId),
            CaseFile.SheriffTurnInSettlements,
            Clock.Day,
            Clock.Turn);

        var assessment = _bountyLoop.AssessSheriffTurnIn(context);
        if (!assessment.Success)
        {
            return contextChanged ? assessment.WithSessionChanged() : assessment;
        }

        if (!_bountyLoop.TryCreateSettlementState(
                context,
                assessment,
                out var settlementState,
                out var rejectionResult))
        {
            return contextChanged ? rejectionResult.WithSessionChanged() : rejectionResult;
        }

        ProduceEvent(new SheriffTurnInSettled
        {
            TargetSuspectId = targetSuspectId,
            TargetName = assessment.TargetName!,
            Disposition = assessment.Disposition!.Value,
            IsAlive = isAlive,
            BountyAmount = settlementState.BountyAmount,
            Message = assessment.Message!,
            Day = settlementState.Day,
            Turn = settlementState.Turn
        });

        return assessment with { SessionChanged = true };
    }

    /// <summary>
    /// Settles the turn-in of an unrelated wanted criminal to the sheriff. The player
    /// declares the warrant (collected from a wanted poster). If the criminal is active
    /// in the <see cref="UnrelatedCriminalLedger"/>, the sheriff pays the bounty, the
    /// ledger records the take-in (spawning a replacement when parity allows), and the
    /// warrant is marked as collected. No confrontation step is required — the player
    /// brings the criminal in directly. See BUNCH-107.
    /// </summary>
    public SheriffTurnInResult SettleUnrelatedCriminalTurnIn(WarrantId warrantId, bool isAlive)
    {
        if (IsArchived)
        {
            return SheriffTurnInResult.Rejected(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return SheriffTurnInResult.Rejected(JourneyModalBlockMessage);
        }

        var contextChanged = EnterActionContext(TownActionContext.SheriffOffice);

        var warrant = CaseFile.KnownWarrants.FirstOrDefault(w => w.Id.Equals(warrantId));
        if (warrant is null)
        {
            return contextChanged
                ? SheriffTurnInResult.Rejected($"You don't have a wanted notice for that person.").WithSessionChanged()
                : SheriffTurnInResult.Rejected($"You don't have a wanted notice for that person.");
        }

        if (warrant.Terms.TargetKind != InvestigationTargetKind.UnrelatedWantedCriminal)
        {
            return contextChanged
                ? SheriffTurnInResult.Rejected($"{warrant.TargetName} is not an unrelated criminal.").WithSessionChanged()
                : SheriffTurnInResult.Rejected($"{warrant.TargetName} is not an unrelated criminal.");
        }

        if (!_bountyLoop.UnrelatedCriminalLedger.IsSurfacingEligible(warrantId))
        {
            return contextChanged
                ? SheriffTurnInResult.Rejected($"{warrant.TargetName} is no longer an active criminal.").WithSessionChanged()
                : SheriffTurnInResult.Rejected($"{warrant.TargetName} is no longer an active criminal.");
        }

        if (!isAlive && warrant.Terms.Disposition == WarrantDisposition.AliveOnly)
        {
            return contextChanged
                ? SheriffTurnInResult.Rejected($"The warrant for {warrant.TargetName} requires an alive turn-in.", warrant.TargetName, warrant.Terms.Disposition, warrant.Terms.BountyAmount).WithSessionChanged()
                : SheriffTurnInResult.Rejected($"The warrant for {warrant.TargetName} requires an alive turn-in.", warrant.TargetName, warrant.Terms.Disposition, warrant.Terms.BountyAmount);
        }

        var message = isAlive
            ? $"You bring in {warrant.TargetName} alive under a {DescribeWarrantDisposition(warrant.Terms.Disposition)} warrant."
            : $"You turn in the body of {warrant.TargetName} under a {DescribeWarrantDisposition(warrant.Terms.Disposition)} warrant.";

        var settledEvent = new UnrelatedCriminalTurnInSettled
        {
            WarrantId = warrantId,
            TargetName = warrant.TargetName,
            Disposition = warrant.Terms.Disposition,
            IsAlive = isAlive,
            BountyAmount = warrant.Terms.BountyAmount,
            Message = message,
            Day = Clock.Day,
            Turn = Clock.Turn
        };
        ProduceEvent(settledEvent);

        var result = isAlive
            ? SheriffTurnInResult.AcceptedAlive(warrant.TargetName, warrant.Terms.Disposition, warrant.Terms.BountyAmount, message)
            : SheriffTurnInResult.AcceptedDead(warrant.TargetName, warrant.Terms.Disposition, warrant.Terms.BountyAmount, message);

        return result with { SessionChanged = true };
    }

    public CaseInvestigationResult FollowTelegraphLeads()
    {
        if (IsArchived)
        {
            return CaseInvestigationResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        if (!CurrentTown.IsAvailable(InvestigationSourceKind.TelegraphLead))
        {
            return CaseInvestigationResult.Failed("There is no telegraph office here.");
        }

        var beatSpent = Clock.TimeOfDay;
        EnterActionContext(TownActionContext.TelegraphOffice);
        var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.TelegraphOffice, CurrentTown.TownName);

        var boringSalt = SaltSource.Mode == SaltSourceMode.Fixed ? null : SaltSource;
        var context = new InvestigationContext(
            CaseFile,
            CurrentTownSlotIndex,
            CurrentTownVisitCount,
            boringSalt,
            RetiredWarrantIds,
            CurrentTown.TownId,
            CurrentTown.TownName,
            beatNarration,
            IsSourceSpent: CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead),
            IsSourceAvailable: true);
        var outcome = _investigationLoop.FollowTelegraphLeads(context);
        ProduceEvent(outcome.Event);
        return CaseInvestigationResult.Succeeded(outcome.DisplayMessage, sessionChanged: true, beatNarration: beatNarration);
    }

    public CaseInvestigationResult GatherLocalGossip()
    {
        if (IsArchived)
        {
            return CaseInvestigationResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        var beatSpent = Clock.TimeOfDay;
        EnterActionContext(TownActionContext.Saloon);
        var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.Saloon, CurrentTown.TownName);

        var boringSalt = SaltSource.Mode == SaltSourceMode.Fixed ? null : SaltSource;
        var context = new InvestigationContext(
            CaseFile,
            CurrentTownSlotIndex,
            CurrentTownVisitCount,
            boringSalt,
            RetiredWarrantIds,
            CurrentTown.TownId,
            CurrentTown.TownName,
            beatNarration,
            IsSourceSpent: CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalGossip),
            IsSourceAvailable: true);
        var outcome = _investigationLoop.GatherLocalGossip(context);
        ProduceEvent(outcome.Event);
        return CaseInvestigationResult.Succeeded(outcome.DisplayMessage, sessionChanged: true, beatNarration: beatNarration);
    }

    public CaseInvestigationResult InspectNoticeBoard()
    {
        if (IsArchived)
        {
            return CaseInvestigationResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        var beatSpent = Clock.TimeOfDay;
        EnterActionContext(TownActionContext.TownSquare);
        var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.TownSquare, CurrentTown.TownName);

        var context = new InvestigationContext(
            CaseFile,
            CurrentTownSlotIndex,
            CurrentTownVisitCount,
            SaltSource: null,
            RetiredWarrantIds,
            CurrentTown.TownId,
            CurrentTown.TownName,
            beatNarration,
            IsSourceSpent: CurrentTownVisit.IsSpent(InvestigationSourceKind.NoticeBoard),
            IsSourceAvailable: true);
        var outcome = _investigationLoop.InspectNoticeBoard(context);
        ProduceEvent(outcome.Event);
        return CaseInvestigationResult.Succeeded(outcome.DisplayMessage, sessionChanged: true, beatNarration: beatNarration);
    }

    public CaseInvestigationResult CheckSheriffRecords()
    {
        if (IsArchived)
        {
            return CaseInvestigationResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        var beatSpent = Clock.TimeOfDay;
        EnterActionContext(TownActionContext.SheriffOffice);
        var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.SheriffOffice, CurrentTown.TownName);

        var context = new InvestigationContext(
            CaseFile,
            CurrentTownSlotIndex,
            CurrentTownVisitCount,
            SaltSource: null,
            RetiredWarrantIds,
            CurrentTown.TownId,
            CurrentTown.TownName,
            beatNarration,
            IsSourceSpent: CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalRecords),
            IsSourceAvailable: true);
        var outcome = _investigationLoop.CheckSheriffRecords(context);
        ProduceEvent(outcome.Event);
        return CaseInvestigationResult.Succeeded(outcome.DisplayMessage, sessionChanged: true, beatNarration: beatNarration);
    }

    public void AppendTravelDiaryDay(TravelDiaryDayState travelDiaryDay)
    {
        ArgumentNullException.ThrowIfNull(travelDiaryDay);
        _journeyLoop.AppendTravelDiaryDay(travelDiaryDay);
    }

    public bool UpdateLatestTravelDiaryDay(Func<TravelDiaryDayState, TravelDiaryDayState> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        return _journeyLoop.UpdateLatestTravelDiaryDay(update);
    }

    private static bool MatchesKnownWarrant(Warrant warrant, Suspect targetSuspect)
    {
        ArgumentNullException.ThrowIfNull(warrant);
        ArgumentNullException.ThrowIfNull(targetSuspect);

        if (string.Equals(warrant.TargetName, targetSuspect.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return warrant.Terms.KnownAliases.Any(alias => string.Equals(alias, targetSuspect.Name, StringComparison.OrdinalIgnoreCase));
    }

    internal bool TryGetKnownWarrantForSuspect(SuspectId suspectId, [NotNullWhen(true)] out Warrant? warrant)
    {
        var targetSuspect = CaseFile.Suspects.FirstOrDefault(suspect => suspect.Id.Equals(suspectId));
        if (targetSuspect is null)
        {
            warrant = null;
            return false;
        }

        warrant = CaseFile.KnownWarrants.FirstOrDefault(candidate => MatchesKnownWarrant(candidate, targetSuspect));
        return warrant is not null;
    }

    private static string DescribeWarrantDisposition(WarrantDisposition disposition)
        => disposition switch
        {
            WarrantDisposition.AliveOnly => "alive-only",
            WarrantDisposition.DeadOrAlive => "dead-or-alive",
            _ => $"disposition {disposition}"
        };

    private static string DescribeConfrontationNarration(
        string targetName,
        WantedSuspectConfrontationChoice choice,
        string? declaredWantedIdentityHandle = null)
        => choice switch
        {
            WantedSuspectConfrontationChoice.Surrendered => declaredWantedIdentityHandle is null
                ? $"You confront {targetName} and bring them in alive."
                : $"You confront {targetName} as {declaredWantedIdentityHandle} and bring them in alive.",
            WantedSuspectConfrontationChoice.Fled => declaredWantedIdentityHandle is null
                ? $"You confront {targetName}, but they get away."
                : $"You confront {targetName} as {declaredWantedIdentityHandle}, but they get away.",
            WantedSuspectConfrontationChoice.Killed => declaredWantedIdentityHandle is null
                ? $"You confront {targetName} and secure the body."
                : $"You confront {targetName} as {declaredWantedIdentityHandle} and secure the body.",
            WantedSuspectConfrontationChoice.Abandoned => declaredWantedIdentityHandle is null
                ? $"You back away before confronting {targetName}."
                : $"You back away before confronting {targetName} as {declaredWantedIdentityHandle}.",
            _ => declaredWantedIdentityHandle is null
                ? $"You confront {targetName}."
                : $"You confront {targetName} as {declaredWantedIdentityHandle}."
        };

    public void ReplaceTravelDiaryDays(IReadOnlyList<TravelDiaryDayState> travelDiaryDays)
    {
        ArgumentNullException.ThrowIfNull(travelDiaryDays);

        _journeyLoop.RestoreTravelDiaryDays(travelDiaryDays);
    }

    private IReadOnlyList<string> CollectSuspectFeatureDescriptions()
        => CaseFile.Suspects
            .SelectMany(s => s.Profile.IdentifyingFacts)
            .Where(f => f.IsPrimary)
            .Select(f => f.Language.HasForm)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// The current town's slot index — its position in <see cref="World"/>'s town list.
    /// Used by the investigation resolvers to vary which warrant/clue surfaces per town.
    /// </summary>
    private int CurrentTownSlotIndex
    {
        get
        {
            var slot = 0;
            foreach (var town in World.Towns)
            {
                if (town.Id.Equals(CurrentTown.TownId))
                {
                    return slot;
                }

                slot++;
            }

            return 0;
        }
    }

    /// <summary>
    /// The visit count for the current town (1-based). Used by the investigation
    /// resolvers to vary which warrant/clue surfaces per visit.
    /// </summary>
    private int CurrentTownVisitCount => CurrentTownVisit.CurrentTownState.VisitNumber;

    /// <summary>
    /// The set of retired and taken-in warrant IDs from the BountyLoop's unrelated-criminal
    /// ledger. Passed to InvestigationLoop via the context record so the wanted-poster
    /// resolver can skip already-resolved warrants. See BUNCH-120.
    /// </summary>
    private IReadOnlySet<WarrantId> RetiredWarrantIds
        => _bountyLoop.UnrelatedCriminalLedger.RetiredWarrantIds
            .Concat(_bountyLoop.UnrelatedCriminalLedger.TakenInCriminalIds)
            .ToHashSet();

    /// <summary>
    /// A suspect is eligible as a saloon POI candidate if they are not the unreleased
    /// true killer. Any non-culprit suspect can walk into any saloon — no town presence,
    /// warrant, or poster state gates. The true killer is gated out until the killer-release
    /// gate opens. See BUNCH-106 realignment.
    /// </summary>
    internal bool IsEligibleSaloonPersonOfInterestCandidate(Suspect suspect)
        => _bountyLoop.IsEligibleSaloonPersonOfInterestCandidate(suspect, CaseFile.TrueCulpritId, CaseFile.KillerReleaseState);

    /// <summary>
    /// Dev-only: describes why a suspect is ineligible as a saloon POI candidate.
    /// Returns null if the suspect is eligible. The only ineligibility reason is
    /// being the unreleased true killer. See BUNCH-90 and BUNCH-106 realignment.
    /// </summary>
    internal string? GetSaloonPoiIneligibilityReason(Suspect suspect)
        => _bountyLoop.GetSaloonPoiIneligibilityReason(suspect, CaseFile.TrueCulpritId, CaseFile.KillerReleaseState);

    private static WantedSuspectConfrontationResult ResolveSaloonPersonOfInterestCompatibilityResult(SaloonPersonOfInterestConfrontationResult result)
        => result.ToWantedSuspectResult();

    private bool IsJourneyModal()
        => Journey is not null;

    private bool IsArchived => Status == GameStatus.Archived;

    /// <summary>
    /// True when the session has not yet reached GameStarted. Gameplay commands
    /// are blocked while this is true. Exposed so the command-handler pipeline
    /// can enforce the setup-phase invariant centrally without each gameplay
    /// domain method repeating the guard. See ADR-0028 and the architecture
    /// guardrails for the inversion pattern.
    /// </summary>
    public bool IsSetupPhase => StartFlowPhase < StartFlowPhase.GameStarted;

    private int SpendFirearmAmmo(int requestedBullets)
    {
        if (requestedBullets <= 0)
        {
            requestedBullets = 1;
        }

        var availableRevolverAmmo = Player.GetQuantity(ItemKind.RevolverAmmo);
        var availableRifleAmmo = Player.GetQuantity(ItemKind.RifleAmmo);
        var availableAmmo = availableRevolverAmmo + availableRifleAmmo;
        if (availableAmmo <= 0)
        {
            return 0;
        }

        var bulletsToSpend = Math.Min(Math.Clamp(requestedBullets, 1, 6), availableAmmo);
        var spent = 0;

        while (spent < bulletsToSpend && Player.GetQuantity(ItemKind.RevolverAmmo) > 0)
        {
            Player.RemoveQuantity(ItemKind.RevolverAmmo, 1);
            spent++;
        }

        while (spent < bulletsToSpend && Player.GetQuantity(ItemKind.RifleAmmo) > 0)
        {
            Player.RemoveQuantity(ItemKind.RifleAmmo, 1);
            spent++;
        }

        return spent;
    }

}

