using System.Diagnostics.CodeAnalysis;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;
using WildBunch.Domain.WantedPosters;

// GameSession contains both migrated (event-sourced) and non-migrated (direct-mutation)
// flows per ADR-0028. The non-migrated flows still call AddLogEntry, which is [Obsolete]
// (projection-legacy). These call sites are known legacy and will be migrated flow-by-flow
// in follow-up issues. Do not add new AddLogEntry call sites; use typed domain events instead.
#pragma warning disable CS0618

namespace WildBunch.Domain.Game;

// Event-sourced flows (migrated): StartNew, Purchase.
// Direct-mutation flows (not-yet-migrated): all others.
// See ADR-0028 and follow-up issues for the migration path.
// Do not add new direct-mutation command methods; use the event-sourced pattern.

/// <summary>
/// Mutable live play-state aggregate root.
/// Command handlers load and persist this root through <see cref="WildBunch.Application.Abstractions.IGameSessionRepository"/>.
/// </summary>
public sealed partial class GameSession : WildBunch.Domain.IAggregateRoot
{
    private const string JourneyModalBlockMessage = "Finish the current journey before taking that action.";
    private const decimal CitizenDeclarationFine = 10m;

    private readonly List<GameLogEntry> _logEntries = [];
    private readonly List<TravelDiaryDayState> _travelDiaryDays = [];
    private readonly List<TravelJourneySnapshot> _completedJourneyHistory = [];
    private readonly WantedSuspectPresenceLedger _wantedSuspectPresenceLedger;
    private int _nextJourneySequence = 1;
    private readonly TownAggregate _currentTown;
    private readonly BountyLoopCoordinator _bountyLoopCoordinator;

    private readonly List<IDomainEvent> _uncommittedEvents = [];
    private readonly List<IDomainEvent> _committedEvents = [];
    private int _version;

    private GameSession(
        GameSessionId id,
        Player player,
        DomainWorld world,
        CaseFile caseFile,
        PursuitState pursuitState,
        GameClock clock,
        GameStatus status,
        TravelJourney? journey,
        TravelDifficulty travelDifficulty,
        TravelRandomnessState travelRandomness,
        AdventureRandomnessPolicy entropy,
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
        Journey = journey;
        TravelDifficulty = travelDifficulty;
        Entropy = entropy;
        TravelRandomness = travelRandomness;
        _currentTown = new TownAggregate(World.GetTown(player.CurrentTownId), currentTownVisit ?? new TownVisitState(player.CurrentTownId));
        if (!_currentTown.VisitState.TownId.Equals(player.CurrentTownId))
        {
            _currentTown.EnterTown(World.GetTown(player.CurrentTownId));
        }

        _currentTown.PrimeCurrentTown();
        _bountyLoopCoordinator = new BountyLoopCoordinator(this);

        if (completedJourneyHistory is not null)
        {
            _completedJourneyHistory.AddRange(completedJourneyHistory);
        }

        _wantedSuspectPresenceLedger = new WantedSuspectPresenceLedger(wantedSuspectPresenceEntries);

        _nextJourneySequence = CalculateNextJourneySequence(journey, _completedJourneyHistory);
    }

    public GameSessionId Id { get; }

    public GameStatus Status { get; private set; }

    public Player Player { get; private set; }

    public DomainWorld World { get; }

    public CaseFile CaseFile { get; }

    public PursuitState PursuitState { get; }

    public GameClock Clock { get; }

    public TravelJourney? Journey { get; private set; }

    public TravelDifficulty TravelDifficulty { get; private set; }

    public AdventureRandomnessPolicy Entropy { get; private set; }

    public TravelRandomnessState TravelRandomness { get; private set; }

    public TownAggregate CurrentTown => _currentTown;

    public TownVisitState CurrentTownVisit => _currentTown.VisitState;

    public TravelRulesProfile TravelRules => TravelRulesProfile.For(TravelDifficulty);

    [Obsolete("LogEntries is projection-legacy per ADR-0028. Derive diary/audit from typed domain events via IDomainEventProjector instead.")]
    public IReadOnlyList<GameLogEntry> LogEntries => _logEntries;

    public IReadOnlyList<TravelDiaryDayState> TravelDiaryDays => _travelDiaryDays;

    public IReadOnlyList<TravelJourneySnapshot> CompletedJourneyHistory => _completedJourneyHistory;

    public IReadOnlyList<WantedSuspectPresenceEntry> WantedSuspectPresenceEntries => _wantedSuspectPresenceLedger.Entries;

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
    public TownActionContext CurrentActionContext { get; private set; } = TownActionContext.None;

    /// <summary>
    /// The town in which the <see cref="CurrentActionContext"/> was entered. A context is
    /// scoped to its town: entering <see cref="TownActionContext.Saloon"/> in Town A does not
    /// suppress time advancement when entering Saloon in Town B. Event-sourced alongside
    /// <see cref="CurrentActionContext"/> via <see cref="Apply(TownActionContextEntered)"/>.
    /// </summary>
    public TownId? CurrentActionContextTownId { get; private set; }

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
        if (context == TownActionContext.None)
        {
            return false;
        }

        // Same context only suppresses time advancement if it was entered in the same town.
        if (context == CurrentActionContext && CurrentTown.TownId.Equals(CurrentActionContextTownId))
        {
            return false;
        }

        // Compute resulting clock state (do NOT mutate Clock directly — Apply does that).
        var newTurn = Clock.Turn + 1;
        var newDay = Clock.Day;
        if (newTurn >= 4)
        {
            newDay++;
            newTurn = 0;
        }

        var e = new TownActionContextEntered
        {
            Context = context,
            TownId = CurrentTown.TownId,
            Day = newDay,
            Turn = newTurn,
            TimeOfDay = (TimeOfDay)newTurn
        };
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
        if (CurrentActionContext != TownActionContext.Saloon)
        {
            return false;
        }

        if (CurrentActionContextTownId is null || !CurrentActionContextTownId.Equals(CurrentTown.TownId))
        {
            return false;
        }

        var activeSaloonPoiId = CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId;
        return activeSaloonPoiId is not null && activeSaloonPoiId.Equals(targetSuspectId);
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
        CurrentActionContext = e.Context;
        CurrentActionContextTownId = e.TownId;
        Clock.Set(e.Day, e.Turn);
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="SaloonPersonOfInterestSpotted"/> event to mutate session state.
    /// This is the event-sourced mutation path for the saloon look-around flow: it marks the
    /// saloon source as spent, appends the case-update log entry (if RecordLog), and sets the
    /// active saloon person of interest. Clock advancement is handled by EnterActionContext.
    /// See ADR-0028 and BUNCH-80.
    /// </summary>
    private void Apply(SaloonPersonOfInterestSpotted e)
    {
        CurrentTown.CheckSource(e.SourceKind);

        if (e.RecordLog)
        {
            RecordCaseUpdate(e.Message);
        }

        if (e.SuspectId is not null && e.Descriptor is not null)
        {
            CurrentTownVisit.CurrentTownState.SetActiveSaloonPersonOfInterest(e.SuspectId.Value, e.Descriptor);
        }
        else if (e.Descriptor is not null)
        {
            CurrentTownVisit.CurrentTownState.SetActiveSaloonCitizenPersonOfInterest(e.Descriptor);
        }

        _version++;
    }

    /// <summary>
    /// Applies a <see cref="WantedSuspectConfronted"/> event to mutate session state.
    /// This is the event-sourced mutation path for the wanted-suspect confrontation flow:
    /// it appends the case-update log entry, records the confrontation state (for non-abandoned
    /// outcomes), and updates the wanted-suspect presence ledger. Clock advancement is handled
    /// by EnterActionContext. The Clock.Turn + 1 offset is removed — confrontation state
    /// records Clock.Turn directly. See ADR-0028 and BUNCH-80.
    /// </summary>
    private void Apply(WantedSuspectConfronted e)
    {
        RecordCaseUpdate(e.Message);

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
            UpdateWantedSuspectPresence(e.TargetSuspectId, e.Choice);
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

        _version++;
    }

    /// <summary>
    /// Applies a <see cref="SaloonPersonOfInterestConfronted"/> event to mutate session state.
    /// This is the event-sourced mutation path for the saloon person confrontation flow:
    /// it clears the active saloon person of interest and optionally fines the player.
    /// No RecordCaseUpdate call — log entries come from delegated WantedSuspectConfronted
    /// events. Clock advancement is handled by EnterActionContext (already in Saloon context).
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
    /// Records a travel-kind log entry. This is the travel equivalent of
    /// <see cref="RecordCaseUpdate"/> and is called from the travel Apply methods
    /// so that diary/audit accumulation stays consistent between command and replay paths.
    /// See ADR-0028 and BUNCH-83.
    /// </summary>
    private void RecordTravelUpdate(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            AddLogEntry(GameLogEntryKind.Travel, message);
    }

    /// <summary>
    /// Applies a <see cref="JourneyStarted"/> event. JourneySnapshot is ABSOLUTE —
    /// Apply sets <see cref="Journey"/> from it. See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void Apply(JourneyStarted e)
    {
        Journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
        _nextJourneySequence = e.JourneySnapshot.JourneySequence + 1;
        _travelDiaryDays.Clear();
        RecordTravelUpdate(e.DiaryMessage);
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="TravelDayAdvanced"/> event. Day is ABSOLUTE — Apply calls
    /// <see cref="GameClock.Set"/>. JourneySnapshot is ABSOLUTE. HealthDelta is ADDITIVE.
    /// PursuitHeat is ABSOLUTE — Apply calls <see cref="PursuitState.SetHeat"/>.
    /// AdditionalDiaryMessages are logged via RecordTravelUpdate before DiaryMessage.
    /// See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void Apply(TravelDayAdvanced e)
    {
        Clock.Set(e.Day, turn: 0);
        Journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
        if (e.HealthDelta != 0)
            Player.AdjustHealth(e.HealthDelta);
        PursuitState.SetHeat(e.PursuitHeat);
        // Player food/canteen/horse feed/horse state are set ABSOLUTE from the journey
        // snapshot. On the command path, direct mutations in PrepareTravelDayAdvance
        // already set these values, so these are no-ops. On replay, they set the
        // correct values from the snapshot. See ADR-0028 and BUNCH-83.
        SyncPlayerFromJourneySnapshot(e.JourneySnapshot);
        foreach (var narration in e.AdditionalDiaryMessages)
            RecordTravelUpdate(narration);
        RecordTravelUpdate(e.DiaryMessage);
        if (!string.IsNullOrEmpty(e.HorseLostMessage))
            RecordTravelUpdate(e.HorseLostMessage);
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
        Journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
        Player.SetCash(e.WalletCash);
        PursuitState.SetHeat(e.PursuitHeat);
        SyncPlayerFromJourneySnapshot(e.JourneySnapshot);
        RecordTravelUpdate(e.DiaryMessage);
        if (!string.IsNullOrEmpty(e.HorseLostMessage))
            RecordTravelUpdate(e.HorseLostMessage);
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
        Journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
        Player.SetHealth(e.PlayerHealth);
        Player.SetCash(e.WalletCash);
        if (e.AmmoSpent > 0)
            SpendFirearmAmmo(e.AmmoSpent);
        if (e.StolenItemKind is { } kind && e.StolenItemQuantity > 0)
            Player.RemoveQuantity(kind, e.StolenItemQuantity);
        PursuitState.SetHeat(e.PursuitHeat);
        foreach (var narration in e.AdditionalDiaryMessages)
            RecordTravelUpdate(narration);
        RecordTravelUpdate(e.DiaryMessage);
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
        Journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
        Player.TravelTo(e.DestinationTownId);
        RefreshTownVisit(e.DestinationTownId);
        RefillCanteenAfterArrival();
        RecordTravelUpdate(e.DiaryMessage);
        _version++;
    }

    /// <summary>
    /// Applies a <see cref="JourneyArrivalAcknowledged"/> event. Apply archives the
    /// current journey into <see cref="CompletedJourneyHistory"/> and clears the active
    /// journey. See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void Apply(JourneyArrivalAcknowledged e)
    {
        _completedJourneyHistory.Add(e.JourneySnapshot);
        Journey = null;
        RecordTravelUpdate(e.DiaryMessage);
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

    public static GameSession StartNew(string playerName, DomainWorld world, CaseFile caseFile, TownId? startingTownId = null)
        => StartNew(playerName, world, caseFile, startingTownId, wallet: null, inventory: null, travelDifficulty: TravelDifficulty.Normal);

    public static GameSession StartNew(
        string playerName,
        DomainWorld world,
        CaseFile caseFile,
        TownId? startingTownId,
        WildBunch.Domain.Economy.Wallet? wallet,
        DomainInventory? inventory,
        TravelDifficulty travelDifficulty = TravelDifficulty.Normal,
        TravelRandomnessState? travelRandomness = null,
        AdventureRandomnessPolicy entropy = AdventureRandomnessPolicy.Standard)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(caseFile);

        var resolvedTownId = startingTownId ?? world.Towns.First().Id;
        var startingTown = world.GetTown(resolvedTownId);
        var resolvedTravelRandomness = travelRandomness ?? TravelRandomnessState.CreateRuntimeSalted();
        var resolvedWallet = wallet ?? WildBunch.Domain.Economy.Wallet.Starting(25m);
        var resolvedInventory = inventory ?? DomainInventory.Empty();
        var startingHealth = StartingHealthFor(travelDifficulty);

        // Build the typed domain event from the resolved starting values.
        var e = new GameStarted
        {
            PlayerName = playerName,
            StartingTownId = startingTown.Id,
            StartingTownName = startingTown.Name,
            StartingHealth = startingHealth,
            StartingWallet = resolvedWallet.Cash,
            StartingInventoryItems = resolvedInventory.Items.ToArray(),
            Difficulty = travelDifficulty,
            TravelRandomness = resolvedTravelRandomness,
            Entropy = entropy
        };

        // Construct a placeholder session (like RehydrateFromEvents).
        // Apply(GameStarted) is the single mutation path — it sets Player,
        // Status, TravelDifficulty, TravelRandomness, and Entropy from the event.
        // The constructor only sets world/caseFile/clock/pursuit references that
        // are external inputs, not event-derived state.
        var placeholderPlayer = new Player(
            playerName,
            startingTown.Id,
            health: startingHealth,
            resolvedWallet,
            resolvedInventory);

        var session = new GameSession(
            GameSessionId.New(),
            placeholderPlayer,
            world,
            caseFile,
            new PursuitState(),
            new GameClock(),
            GameStatus.Active,
            journey: null,
            travelDifficulty,
            resolvedTravelRandomness,
            entropy,
            currentTownVisit: null,
            Array.Empty<TravelJourneySnapshot>(),
            Array.Empty<WantedSuspectPresenceEntry>());

        // Apply the event through the single mutation path (same as replay).
        session.Apply(e);
        session._uncommittedEvents.Add(e);

        return session;
    }

    private static int StartingHealthFor(TravelDifficulty travelDifficulty)
        => travelDifficulty switch
        {
            TravelDifficulty.Easy => 1250,
            TravelDifficulty.Hard => 800,
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
        TravelDifficulty = e.Difficulty;
        TravelRandomness = e.TravelRandomness;
        Entropy = e.Entropy;
        AddLogEntry(GameLogEntryKind.Opening, $"The hunt begins in {e.StartingTownName}.");
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
        var quantityLabel = e.Quantity == 1 ? e.DisplayName : $"{e.Quantity} {e.DisplayName}";
        AddLogEntry(GameLogEntryKind.Purchase, $"Purchased {quantityLabel} for ${e.TotalPrice:0.00}.");
        _version++;
    }

    /// <summary>
    /// Applies an <see cref="InvestigationPerformed"/> event to mutate session state.
    /// This is the event-sourced mutation path for investigation flows: it marks the
    /// investigation source as spent for the current visit, advances the clock, reveals
    /// the clue and/or warrant carried by the event, and appends the case-update log
    /// entry. See ADR-0028.
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

        RecordCaseUpdate(e.Message);

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
        => _wantedSuspectPresenceLedger.GetState(suspectId);

    public bool TryGetWantedSuspectPresenceState(SuspectId suspectId, out WantedSuspectPresenceState state)
        => _wantedSuspectPresenceLedger.TryGetState(suspectId, out state);

    public void SetWantedSuspectPresenceState(SuspectId suspectId, WantedSuspectPresenceState state)
        => _wantedSuspectPresenceLedger.SetState(suspectId, state);

    public TravelJourneyStepResult StartJourney(TravelPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);

        if (Journey is not null)
        {
            return TravelJourneyStepResult.Failed("You are already on the trail.");
        }

        var newJourney = TravelJourney.Start(preview, _nextJourneySequence, BuildJourneyOpeningNarration(preview));
        var startMessage = $"You set out from {preview.OriginTownName} toward {preview.DestinationTownName} {DescribeTravelMode(preview.TravelMode)}. The route is {preview.RideDayDistance:0.##} ride-day unit(s) and should take {preview.ExpectedDays} day(s). {DescribeCanteenCoverage(preview)}.";
        ProduceEvent(new JourneyStarted
        {
            JourneySnapshot = newJourney.ToSnapshot(TravelRules),
            DiaryMessage = startMessage
        });

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            startMessage,
            startMessage,
            0,
            Journey!.ToSnapshot(TravelRules));
    }

    public TravelJourneyStepResult AdvanceJourneyDay()
        => AdvanceJourneyDayDeterministic();

    private static string DescribeHorseLoss(HorseTravelState? horseState, TravelRulesProfile travelRulesProfile)
    {
        if (horseState is null)
        {
            return "Your horse could no longer carry you.";
        }

        if (horseState.IsDeadFor(travelRulesProfile))
        {
            return "Your horse died on the trail.";
        }

        if (horseState.IsLameFor(travelRulesProfile))
        {
            return "Your horse went lame and could no longer carry you.";
        }

        return "Your horse could no longer carry you.";
    }

    private static string DescribeTerrain(TrailTerrain terrain)
        => terrain switch
        {
            TrailTerrain.OpenRange => "open-range",
            TrailTerrain.Hills => "hill country",
            TrailTerrain.Badlands => "badlands",
            TrailTerrain.Mountains => "mountain",
            _ => "trail"
        };

    private static string DescribeRisk(TrailRisk risk)
        => risk switch
        {
            TrailRisk.Low => "The route looks steady enough for now.",
            TrailRisk.Moderate => "The route has some teeth, so I will keep my eyes open.",
            TrailRisk.High => "The route looks rough enough to demand respect.",
            _ => "The route is hard to read."
        };

    private static string PrependHorseLossMessage(string horseLossMessage, string message)
        => horseLossMessage.Length == 0 ? message : $"{horseLossMessage} {message}";

    private string ApplyEncounterHorsePressure(int exhaustionIncrease)
    {
        if (exhaustionIncrease <= 0)
        {
            return string.Empty;
        }

        var horseState = Player.GetHorseState();
        if (horseState is null)
        {
            return string.Empty;
        }

        var nextHorseState = horseState.IncreaseExhaustion(exhaustionIncrease);
        Player.SetHorseState(nextHorseState);
        Journey!.SetHorseState(nextHorseState);

        if (Journey.TravelMode == TravelMode.Mounted && !nextHorseState.CanProvideMountedTravelFor(TravelRules))
        {
            Journey.RecalculatePacing(TravelMode.Foot);
            return DescribeHorseLoss(nextHorseState, TravelRules);
        }

        return string.Empty;
    }

    private TrailEventApplicationResult ApplyTrailEvent(
        JourneyTrailEventState trailEvent,
        string prefixHorseLostMessage,
        string encounterMessage)
    {
        ArgumentNullException.ThrowIfNull(trailEvent);

        // Direct player mutations (same as pre-migration code).
        // Apply(TrailEventApplied) sets wallet/heat ABSOLUTE and syncs food/canteen/horse
        // from the journey snapshot, so command-path and replay converge.
        if (trailEvent.WalletDelta != 0m)
        {
            Player.AdjustCash(trailEvent.WalletDelta);
        }

        if (trailEvent.FoodDelta != 0)
        {
            ApplyFoodDelta(trailEvent.FoodDelta);
            Journey!.AdjustFood(trailEvent.FoodDelta);
        }

        if (trailEvent.CanteenChargeDelta != 0)
        {
            var canteenState = Player.GetCanteenState();
            if (canteenState is not null)
            {
                var nextCanteenState = canteenState.AdjustCharges(trailEvent.CanteenChargeDelta);
                Player.SetCanteenState(nextCanteenState);
                Journey!.SetCanteenCharges(nextCanteenState.Charges);
            }
        }

        if (trailEvent.HorseHungerDelta != 0 || trailEvent.HorseThirstDelta != 0 || trailEvent.HorseExhaustionDelta != 0)
        {
            var horseState = Player.GetHorseState();
            if (horseState is not null)
            {
                horseState = ApplyHorseDelta(horseState, trailEvent);
                Player.SetHorseState(horseState);
                Journey!.SetHorseState(horseState);
            }
        }

        // Trail-event heat guard: stays for future noisy/witnessed trail events.
        // Current bad-luck events (washout, dust storm, spooked horse, hard miles)
        // carry HeatIncrease=0 because they are private hardship, not noisy/visible.
        // See ADR-0029.
        if (trailEvent.HeatIncrease != 0)
        {
            PursuitState.IncreaseHeat(trailEvent.HeatIncrease);
        }

        if (trailEvent.DelayDays != 0)
        {
            Journey!.AddDelayDays(trailEvent.DelayDays);
        }

        var horseLossMessage = string.Empty;
        TravelMode? travelModeChangedTo = null;
        if (Journey!.TravelMode == TravelMode.Mounted && Player.GetHorseState()?.CanProvideMountedTravelFor(TravelRules) == false)
        {
            horseLossMessage = DescribeHorseLoss(Player.GetHorseState(), TravelRules);
            Journey.RecalculatePacing(TravelMode.Foot);
            travelModeChangedTo = TravelMode.Foot;
        }

        var fullDiaryMessage = PrependHorseLossMessage(
            CombineHorseLossMessage(prefixHorseLostMessage, horseLossMessage),
            encounterMessage);

        var postEventSnapshot = Journey.ToSnapshot(TravelRules);
        ProduceEvent(new TrailEventApplied
        {
            JourneySnapshot = postEventSnapshot,
            TrailEventKind = trailEvent.Kind,
            TrailEventId = trailEvent.Id,
            WalletDelta = trailEvent.WalletDelta,
            WalletCash = Player.Wallet.Cash,
            FoodDelta = trailEvent.FoodDelta,
            CanteenChargeDelta = trailEvent.CanteenChargeDelta,
            HorseHungerDelta = trailEvent.HorseHungerDelta,
            HorseThirstDelta = trailEvent.HorseThirstDelta,
            HorseExhaustionDelta = trailEvent.HorseExhaustionDelta,
            DelayDays = trailEvent.DelayDays,
            HeatIncrease = trailEvent.HeatIncrease,
            PursuitHeat = PursuitState.Heat,
            TravelModeChangedTo = travelModeChangedTo,
            DiaryMessage = fullDiaryMessage,
            HorseLostMessage = horseLossMessage
        });

        return new TrailEventApplicationResult(horseLossMessage);
    }

    private static HorseTravelState ApplyHorseDelta(HorseTravelState horseState, JourneyTrailEventState trailEvent)
    {
        var nextHorseState = horseState;

        if (trailEvent.HorseHungerDelta > 0)
        {
            nextHorseState = nextHorseState.IncreaseHunger(trailEvent.HorseHungerDelta);
        }
        else if (trailEvent.HorseHungerDelta < 0)
        {
            nextHorseState = nextHorseState.RecoverHunger(Math.Abs(trailEvent.HorseHungerDelta));
        }

        if (trailEvent.HorseThirstDelta > 0)
        {
            nextHorseState = nextHorseState.IncreaseThirst(trailEvent.HorseThirstDelta);
        }
        else if (trailEvent.HorseThirstDelta < 0)
        {
            nextHorseState = nextHorseState.RecoverThirst(Math.Abs(trailEvent.HorseThirstDelta));
        }

        if (trailEvent.HorseExhaustionDelta > 0)
        {
            nextHorseState = nextHorseState.IncreaseExhaustion(trailEvent.HorseExhaustionDelta);
        }

        return nextHorseState;
    }

    private static string CombineHorseLossMessage(string primaryHorseLossMessage, string secondaryHorseLossMessage)
        => primaryHorseLossMessage.Length == 0
            ? secondaryHorseLossMessage
            : secondaryHorseLossMessage.Length == 0
                ? primaryHorseLossMessage
                : $"{primaryHorseLossMessage} {secondaryHorseLossMessage}";

    private TravelJourneySnapshot CompleteJourneyAtDestination()
    {
        if (Journey is null)
        {
            throw new InvalidOperationException("A journey is required to complete arrival handling.");
        }

        // Journey-internal state transition only. Player town/canteen mutations
        // flow through Apply(JourneyCompleted) so command and replay paths match.
        Journey.MarkCompleted();
        return Journey.ToSnapshot(TravelRules);
    }

    private void RefreshTownVisit(TownId townId)
    {
        var currentTown = World.GetTown(townId);
        _currentTown.EnterTown(currentTown);
        // The action context is scoped to the current town. When the town changes
        // (including a round-trip back to the same town), the context resets so that
        // re-entering a location in the new town advances time. This is a direct
        // mutation because travel/journey is not yet event-sourced; the context
        // reset is a side effect of the town change, not a gameplay event.
        // See BUNCH-80 review feedback on town-scoped CurrentActionContext.
        ResetActionContextForTownChange();
    }

    /// <summary>
    /// Resets <see cref="CurrentActionContext"/> and <see cref="CurrentActionContextTownId"/>
    /// to None/null. Called by <see cref="RefreshTownVisit"/> when the current town changes.
    /// Also available for test helpers that simulate town changes via
    /// <see cref="TownVisitState.Reset"/> directly.
    /// </summary>
    internal void ResetActionContextForTownChange()
    {
        CurrentActionContext = TownActionContext.None;
        CurrentActionContextTownId = null;
    }

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

    private TravelResourceSnapshot CaptureTravelResources()
        => TravelResourceSnapshotFactory.Capture(Player, PursuitState);

    private void AppendTravelDiaryDay(
        TravelJourneySnapshot journeySnapshot,
        TravelDiaryBaselineState startingState,
        JourneyTrailEventState? trailEvent = null,
        JourneyEncounterState? pendingEncounter = null,
        TravelDiaryEncounterResolutionState? encounterResolution = null,
        IReadOnlyList<string>? entries = null)
    {
        AppendTravelDiaryDay(TravelDiaryDayFactory.Create(
            journeySnapshot,
            startingState,
            CaptureTravelResources(),
            trailEvent: trailEvent,
            pendingEncounter: pendingEncounter,
            encounterResolution: encounterResolution,
            entries: entries));
    }

    private TravelDayAdvanceState PrepareTravelDayAdvance()
    {
        var startingResources = CaptureTravelResources();
        var startingState = new TravelDiaryBaselineState(
            Journey!.TravelMode,
            Journey.RemainingRideDayDistance,
            Journey.RemainingDays,
            Journey.DelayDays,
            startingResources);

        var capabilities = Player.GetCapabilities(TravelRules);
        if (Journey.TravelMode == TravelMode.Mounted && !capabilities.MountedTravelAvailable)
        {
            Journey.RecalculatePacing(TravelMode.Foot);
        }

        // Player food consumption — direct mutation (same as pre-migration code).
        // Apply(TravelDayAdvanced) sets player state ABSOLUTE from the journey snapshot,
        // so command-path direct mutations and replay-path event applications converge.
        if (Player.GetQuantity(ItemKind.Food) > 0)
        {
            Player.RemoveQuantity(ItemKind.Food, 1);
            Journey.ConsumeFood();
        }

        var upkeep = JourneyUpkeepRules.ApplyDailyUpkeep(
            Journey.Preview.RouteProfile.Terrain,
            Journey.Preview.RouteProfile.WaterFeature,
            Player.GetHorseState(),
            Player.GetCanteenState(),
            Player.GetQuantity(ItemKind.HorseFeed),
            TravelRules);

        if (upkeep.HorseFeedConsumed > 0)
        {
            Player.RemoveQuantity(ItemKind.HorseFeed, upkeep.HorseFeedConsumed);
            Journey.ConsumeHorseFeed(upkeep.HorseFeedConsumed);
        }

        if (upkeep.CanteenState is not null)
        {
            Player.SetCanteenState(upkeep.CanteenState);
            Journey.SetCanteenCharges(upkeep.CanteenState.Charges);
        }
        else
        {
            Journey.SetCanteenCharges(0);
        }

        if (upkeep.HorseState is not null)
        {
            Player.SetHorseState(upkeep.HorseState);
            Journey.SetHorseState(upkeep.HorseState);
        }

        var horseLostMessage = string.Empty;
        if (upkeep.MountedTravelLost && Journey.TravelMode == TravelMode.Mounted)
        {
            horseLostMessage = DescribeHorseLoss(upkeep.HorseState, TravelRules);
            Journey.RecalculatePacing(TravelMode.Foot);
        }

        // Clock advance is ABSOLUTE via the event (Day = Clock.Day + 1).
        // The clock is advanced directly here so trail event log entries capture the
        // correct day. Apply(TravelDayAdvanced) sets the clock ABSOLUTE for replay.
        // On the command path, Clock.Set(e.Day, 0) is a no-op (clock already at newDay).
        var newDay = Clock.Day + 1;
        Clock.AdvanceTravelDay();
        var progress = Journey.AdvanceOneDay();

        // Heat is future lawman pressure, not trail danger — travel no longer
        // raises heat from route risk. See ADR-0029.
        var generationContext = CreateTravelDayGenerationContext(TravelDayPlanGenerator.CurrentVersion);
        Journey.SetCurrentDayPlan(TravelDayPlanGenerator.Generate(generationContext));

        return new TravelDayAdvanceState(
            startingState,
            horseLostMessage,
            progress,
            newDay,
            PursuitState.Heat);
    }

    private TravelJourneyStepResult HandleInterruptedTravelDay(
        TravelDayAdvanceState travelDay,
        JourneyEncounterState pendingEncounter,
        JourneyTrailEventState? lastTrailEvent,
        List<string> dayEntries,
        List<string> narrationMessages)
    {
        var horseLostMessage = travelDay.HorseLostMessage;
        var encounterMessage = PrependHorseLossMessage(horseLostMessage, pendingEncounter.Message);
        dayEntries.Add(encounterMessage);
        dayEntries.Add("I could run, fight, or bribe my way through.");

        var interruptedSnapshot = Journey!.ToSnapshot(TravelRules);
        ProduceEvent(new TravelDayAdvanced
        {
            Day = travelDay.NewDay,
            JourneySnapshot = interruptedSnapshot,
            HealthDelta = 0,
            PursuitHeat = travelDay.PursuitHeat,

            DayOutcome = TravelDayOutcome.Interrupted,
            DiaryMessage = encounterMessage,
            HorseLostMessage = horseLostMessage,
            AdditionalDiaryMessages = narrationMessages
        });

        AppendTravelDiaryDay(
            interruptedSnapshot,
            travelDay.StartingState,
            pendingEncounter: pendingEncounter,
            entries: dayEntries);

        return new TravelJourneyStepResult(
            false,
            Journey.Status,
            horseLostMessage.Length == 0
                ? "Your journey is interrupted by a trail encounter."
                : $"Your journey is interrupted by a trail encounter. {horseLostMessage}",
            encounterMessage,
            0,
            interruptedSnapshot,
            lastTrailEvent);
    }

    private TravelJourneyStepResult HandleCompletedTravelDay(
        TravelDayAdvanceState travelDay,
        JourneyTrailEventState? lastTrailEvent,
        List<string> dayEntries,
        List<string> narrationMessages,
        JourneyProgress progress)
    {
        var horseLostMessage = travelDay.HorseLostMessage;
        var destinationTownName = Journey!.Preview.DestinationTownName;
        var destinationTownId = Journey.Preview.DestinationTownId;
        var completedSnapshot = CompleteJourneyAtDestination();
        var completionMessage = horseLostMessage.Length == 0
            ? $"You reach {destinationTownName}."
            : $"{horseLostMessage} You reach {destinationTownName}.";
        var arrivalMessage = horseLostMessage.Length == 0
            ? $"You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s)."
            : $"{horseLostMessage} You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s).";

        ProduceEvent(new TravelDayAdvanced
        {
            Day = travelDay.NewDay,
            JourneySnapshot = completedSnapshot,
            HealthDelta = 0,
            PursuitHeat = travelDay.PursuitHeat,

            DayOutcome = TravelDayOutcome.Completed,
            DiaryMessage = arrivalMessage,
            HorseLostMessage = horseLostMessage,
            AdditionalDiaryMessages = narrationMessages
        });

        ProduceEvent(new JourneyCompleted
        {
            JourneySnapshot = completedSnapshot,
            DestinationTownId = destinationTownId,
            DestinationTownName = destinationTownName,
            DiaryMessage = string.Empty
        });

        AppendTravelDiaryDay(
            completedSnapshot,
            travelDay.StartingState,
            trailEvent: lastTrailEvent,
            entries: dayEntries.Count == 0 ? null : dayEntries);
        Journey!.SetCurrentDayPlan(null);

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Completed,
            completionMessage,
            horseLostMessage.Length == 0
                ? $"You reach {destinationTownName} after {progress.RideDayDistanceTravelled:0.##} ride-day unit(s)."
                : $"{horseLostMessage} You reach {destinationTownName} after {progress.RideDayDistanceTravelled:0.##} ride-day unit(s).",
            travelDay.PursuitHeat,
            completedSnapshot,
            lastTrailEvent);
    }

    private TravelJourneyStepResult HandleOngoingTravelDay(
        TravelDayAdvanceState travelDay,
        JourneyTrailEventState? lastTrailEvent,
        List<string> dayEntries,
        List<string> narrationMessages,
        TravelJourneySnapshot journeySnapshot)
    {
        var horseLostMessage = travelDay.HorseLostMessage;
        var ongoingMessage = horseLostMessage.Length == 0
            ? $"One trail day passes. {journeySnapshot.RemainingRideDayDistance:0.##} ride-day unit(s) remain and {Journey!.RemainingDays} day(s) remain on the route. {DescribeCanteenCoverage(journeySnapshot)}."
            : $"{horseLostMessage} One trail day passes on foot. {journeySnapshot.RemainingRideDayDistance:0.##} ride-day unit(s) remain and {Journey!.RemainingDays} day(s) remain on the route. {DescribeCanteenCoverage(journeySnapshot)}.";

        ProduceEvent(new TravelDayAdvanced
        {
            Day = travelDay.NewDay,
            JourneySnapshot = journeySnapshot,
            HealthDelta = 0,
            PursuitHeat = travelDay.PursuitHeat,

            DayOutcome = TravelDayOutcome.Ongoing,
            DiaryMessage = ongoingMessage,
            HorseLostMessage = horseLostMessage,
            AdditionalDiaryMessages = narrationMessages
        });

        AppendTravelDiaryDay(
            journeySnapshot,
            travelDay.StartingState,
            trailEvent: lastTrailEvent,
            entries: dayEntries.Count == 0 ? null : dayEntries);
        Journey!.SetCurrentDayPlan(null);

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            ongoingMessage,
            ongoingMessage,
            travelDay.PursuitHeat,
            journeySnapshot,
            lastTrailEvent);
    }

    private TravelJourneyStepResult AdvanceJourneyDayDeterministic()
    {
        if (Journey is null)
        {
            return TravelJourneyStepResult.Failed("No active journey is underway.");
        }

        if (Journey.PendingEncounter is not null)
        {
            var encounterMessage = Journey.PendingEncounter.Message;
            return new TravelJourneyStepResult(
                false,
                Journey.Status,
                "Resolve the pending encounter before you continue on the trail.",
                encounterMessage,
                0,
                Journey.ToSnapshot(TravelRules));
        }

        if (Journey.Status != JourneyStatus.Active)
        {
            return new TravelJourneyStepResult(
                false,
                Journey.Status,
                "The journey is not active.",
                "The journey is not active.",
                0,
                Journey.ToSnapshot(TravelRules));
        }

        var travelDay = PrepareTravelDayAdvance();
        var dayEntries = new List<string>();
        var narrationMessages = new List<string>();
        JourneyTrailEventState? lastTrailEvent = null;

        while (Journey.CurrentDayPlan is not null && !Journey.CurrentDayPlan.IsComplete)
        {
            var currentEncounter = Journey.CurrentDayPlan.CurrentEncounter;
            if (currentEncounter is null)
            {
                break;
            }

            if (currentEncounter.RequiresChoice)
            {
                var pendingEncounter = currentEncounter.PendingEncounter!;
                Journey.MarkInterrupted(pendingEncounter);
                return HandleInterruptedTravelDay(
                    travelDay,
                    pendingEncounter,
                    lastTrailEvent,
                    dayEntries,
                    narrationMessages);
            }

            if (currentEncounter.TrailEvent is not null)
            {
                var trailEventApplication = ApplyTrailEvent(
                    currentEncounter.TrailEvent,
                    travelDay.HorseLostMessage,
                    currentEncounter.Message);
                var trailEventMessage = PrependHorseLossMessage(
                    CombineHorseLossMessage(travelDay.HorseLostMessage, trailEventApplication.HorseLossMessage),
                    currentEncounter.Message);
                dayEntries.Add(trailEventMessage);
                lastTrailEvent = currentEncounter.TrailEvent;
            }
            else if (!string.IsNullOrWhiteSpace(currentEncounter.Message))
            {
                dayEntries.Add(currentEncounter.Message);
                narrationMessages.Add(currentEncounter.Message);
            }

            Journey.AdvanceCurrentDayPlan();
        }

        var journeySnapshot = Journey.ToSnapshot(TravelRules);
        return travelDay.Progress.Completed
            ? HandleCompletedTravelDay(travelDay, lastTrailEvent, dayEntries, narrationMessages, travelDay.Progress)
            : HandleOngoingTravelDay(travelDay, lastTrailEvent, dayEntries, narrationMessages, journeySnapshot);
    }

    public JourneyArrivalAcknowledgementResult AcknowledgeJourneyArrival()
    {
        if (Journey is null)
        {
            return JourneyArrivalAcknowledgementResult.Failed("No completed journey is waiting to be acknowledged.");
        }

        if (Journey.Status != JourneyStatus.Completed)
        {
            return JourneyArrivalAcknowledgementResult.Failed("The journey is not ready to be acknowledged.", Journey.ToSnapshot(TravelRules));
        }

        var completedSnapshot = Journey.ToSnapshot(TravelRules);
        var arrivalMessage = $"You step into {completedSnapshot.DestinationTownName} and put the trail behind you.";
        ProduceEvent(new JourneyArrivalAcknowledged
        {
            JourneySequence = completedSnapshot.JourneySequence,
            JourneySnapshot = completedSnapshot,
            DiaryMessage = string.Empty
        });

        return new JourneyArrivalAcknowledgementResult(
            true,
            arrivalMessage,
            completedSnapshot);
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
        var recentTrailEventKinds = _travelDiaryDays
            .Select(day => day.TrailEvent?.Kind)
            .Where(kind => kind is not null)
            .Select(kind => kind!.Value)
            .TakeLast(3)
            .ToArray();
        var recentTrailEventIds = _travelDiaryDays
            .Select(day => day.TrailEvent?.Id)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .TakeLast(3)
            .ToArray();
        var recentEncounterCategories = _travelDiaryDays
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
            CreateHeatBand(PursuitState.Heat),
            CreateWalletBand(Player.Wallet.Cash, TravelRules),
            recentTrailEventKinds,
            recentTrailEventIds,
            recentEncounterCategories,
            HasHorse: horseState is not null && !horseState.IsDeadFor(TravelRules),
            TravelRandomness.Mode,
            TravelRandomness.Salt,
            Entropy);
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

    private static HorseConditionBand CreateHorseConditionBand(HorseTravelState? horseState)
        => CreateHorseConditionBand(horseState, TravelRulesProfile.Default);

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

    private static PursuitHeatBand CreateHeatBand(int heat)
    {
        if (heat <= 0)
        {
            return PursuitHeatBand.Calm;
        }

        if (heat <= 2)
        {
            return PursuitHeatBand.Wary;
        }

        if (heat <= 4)
        {
            return PursuitHeatBand.Hot;
        }

        return PursuitHeatBand.Hunted;
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

    private JourneyEncounterResolutionResult ResolveJourneyEncounterDeterministic(
        string choiceId,
        int? bulletSpend,
        decimal? bribeAmount,
        ulong? forcedRoll = null)
    {
        if (Journey is null)
        {
            return JourneyEncounterResolutionResult.Failed("No active journey is underway.", JourneyStatus.Failed);
        }

        if (Journey.PendingEncounter is null)
        {
            return JourneyEncounterResolutionResult.Failed("There is no pending encounter to resolve.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }

        if (Journey.Status != JourneyStatus.Interrupted)
        {
            return JourneyEncounterResolutionResult.Failed("The encounter is not waiting to be resolved.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }

        if (string.IsNullOrWhiteSpace(choiceId))
        {
            return JourneyEncounterResolutionResult.Failed("Choose how you want to answer the encounter.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }

        var encounter = Journey.PendingEncounter;
        var currentDayEncounter = Journey.CurrentDayPlan?.CurrentEncounter?.PendingEncounter;
        if (encounter is null)
        {
            encounter = currentDayEncounter;
        }
        else if (encounter.FoeProfile is null && currentDayEncounter?.FoeProfile is not null)
        {
            encounter = currentDayEncounter;
        }

        if (encounter is not null && !ReferenceEquals(encounter, Journey.PendingEncounter))
        {
            Journey.UpdatePendingEncounter(encounter);
        }

        var startingState = RebuildCurrentTravelDiaryBaselineState();
        if (encounter is null)
        {
            return JourneyEncounterResolutionResult.Failed("The encounter is not waiting to be resolved.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }

        if (encounter.Kind == "foe" && encounter.FoeProfile is null)
        {
            encounter = RecoverFoeProfile(encounter);
            Journey.UpdatePendingEncounter(encounter);
        }

        var resolvedChoiceId = choiceId.Trim().ToLowerInvariant();
        if (resolvedChoiceId == "bribe" && encounter.HiddenState?.BribeLockedOut == true)
        {
            return JourneyEncounterResolutionResult.Failed("The rider will not take any more money.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }

        if (!encounter.Choices.Any(choice => string.Equals(choice.Id, choiceId, StringComparison.OrdinalIgnoreCase)))
        {
            return JourneyEncounterResolutionResult.Failed("That is not a lawful way to answer this encounter.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }

        var resolvedChoiceLabel = encounter.Choices.First(choice => string.Equals(choice.Id, resolvedChoiceId, StringComparison.OrdinalIgnoreCase)).Label;
        var hiddenState = encounter.HiddenState ?? new JourneyEncounterHiddenState();
        var resolutionAttemptIndex = resolvedChoiceId switch
        {
            "bribe" => hiddenState.BribeOffersMade + 1,
            "run" => hiddenState.ChaseFatigue + 1,
            "fight" => 1 + hiddenState.Annoyance + (hiddenState.Shaken ? 1 : 0),
            _ => encounter.ResolutionAttempts + 1
        };
        var rollSeed = JourneyEncounterResolutionEngine.ComposeRollSeed(
            encounter,
            resolvedChoiceId,
            resolutionAttemptIndex,
            string.Join(
                "|",
                Journey.TravelMode,
                Journey.RemainingRideDayDistance,
                Journey.RemainingDays,
                Player.Health,
                Player.Wallet.Cash,
                Player.GetQuantity(ItemKind.RevolverAmmo),
                Player.GetQuantity(ItemKind.RifleAmmo),
                PursuitState.Heat));
        var roll = forcedRoll ?? JourneyEncounterResolutionEngine.Roll(rollSeed, "resolution");
        var dayEntries = new List<string>();

        switch (resolvedChoiceId)
        {
            case "run":
            {
                var plan = JourneyEncounterResolutionEngine.ResolveRun(
                    encounter,
                    Journey.TravelMode,
                    Player.GetHorseState(),
                    Player.Health,
                    TravelRules,
                    roll);

                if (plan.HealthDelta != 0)
                {
                    Player.AdjustHealth(plan.HealthDelta);
                }

                var horseLossMessage = plan.HorseExhaustionDelta > 0
                    ? ApplyEncounterHorsePressure(plan.HorseExhaustionDelta)
                    : string.Empty;

                // Encounter run: visible/noisy incident that draws future lawman attention. See ADR-0029.
                PursuitState.IncreaseHeat(plan.HeatIncrease);

                var runMessage = PrependHorseLossMessage(horseLossMessage, plan.Message);
                dayEntries.Add(plan.Message);

                if (!plan.Resolved)
                {
                    Journey.UpdatePendingEncounter(plan.UpdatedEncounter);
                    PersistLatestTravelDiaryDay(startingState, dayEntries, Journey.PendingEncounter);
                    ProduceEvent(new JourneyEncounterResolved
                    {
                        ChoiceId = resolvedChoiceId,
                        ChoiceLabel = resolvedChoiceLabel,
                        Resolved = false,
                        PlayerHealth = Player.Health,
                        WalletCash = Player.Wallet.Cash,
                        AmmoSpent = 0,
                        StolenItemKind = null,
                        StolenItemQuantity = 0,
                        PursuitHeat = PursuitState.Heat,
                        HorseExhaustionDelta = plan.HorseExhaustionDelta,
                        ContinuedOnFoot = Journey.TravelMode == TravelMode.Foot,
                        JourneySnapshot = Journey.ToSnapshot(TravelRules),
                        DiaryMessage = runMessage,
                        DayCompleted = false,
                        JourneyCompleted = false
                    });
                    return new JourneyEncounterResolutionResult(false, true, Journey.Status, runMessage, Journey.ToSnapshot(TravelRules));
                }

                Journey.ResumeFromEncounter();
                var resolution = new TravelDiaryEncounterResolutionState(
                    resolvedChoiceId,
                    resolvedChoiceLabel,
                    plan.HealthDelta,
                    0m,
                    0,
                    plan.HeatIncrease,
                    plan.HorseExhaustionDelta,
                    Journey.TravelMode == TravelMode.Foot);
                Journey.RecordCurrentDayEncounterResolution(resolution);
                Journey.AdvanceCurrentDayPlan();
                var resolutionResult = ContinueCurrentDayAfterEncounterResolution(
                    encounter,
                    startingState,
                    dayEntries,
                    resolution);
                ProduceEncounterResolvedEvent(resolvedChoiceId, resolvedChoiceLabel, 0, null, 0, plan.HorseExhaustionDelta, resolutionResult);
                return resolutionResult;
            }

            case "fight":
            {
                var availableRevolverAmmo = Player.GetQuantity(ItemKind.RevolverAmmo);
                var availableRifleAmmo = Player.GetQuantity(ItemKind.RifleAmmo);
                var availableAmmo = availableRevolverAmmo + availableRifleAmmo;
                var hasKnife = Player.HasItem(ItemKind.Knife);
                if (availableAmmo == 0 && !hasKnife)
                {
                    return JourneyEncounterResolutionResult.Failed("You need a knife or firearm ammo to stand and fight.", Journey.Status, Journey.ToSnapshot(TravelRules));
                }

                var plan = JourneyEncounterResolutionEngine.ResolveFight(
                    encounter,
                    Player.Health,
                    TravelRules,
                    availableAmmo,
                    hasKnife,
                    bulletSpend,
                    roll);

                if (plan.AmmoSpent > 0)
                {
                    SpendFirearmAmmo(plan.AmmoSpent);
                }

                if (plan.HealthDelta != 0)
                {
                    Player.AdjustHealth(plan.HealthDelta);
                }

                // Encounter fight: visible/noisy incident that draws future lawman attention. See ADR-0029.
                if (plan.HeatIncrease != 0)
                {
                    PursuitState.IncreaseHeat(plan.HeatIncrease);
                }

                dayEntries.Add(plan.Message);

                if (!plan.Resolved)
                {
                    Journey.UpdatePendingEncounter(plan.UpdatedEncounter);
                    PersistLatestTravelDiaryDay(startingState, dayEntries, Journey.PendingEncounter);
                    ProduceEvent(new JourneyEncounterResolved
                    {
                        ChoiceId = resolvedChoiceId,
                        ChoiceLabel = resolvedChoiceLabel,
                        Resolved = false,
                        PlayerHealth = Player.Health,
                        WalletCash = Player.Wallet.Cash,
                        AmmoSpent = plan.AmmoSpent,
                        StolenItemKind = null,
                        StolenItemQuantity = 0,
                        PursuitHeat = PursuitState.Heat,
                        HorseExhaustionDelta = plan.HorseExhaustionDelta,
                        ContinuedOnFoot = plan.ContinuedOnFoot,
                        JourneySnapshot = Journey.ToSnapshot(TravelRules),
                        DiaryMessage = plan.Message,
                        DayCompleted = false,
                        JourneyCompleted = false
                    });
                    return new JourneyEncounterResolutionResult(false, true, Journey.Status, plan.Message, Journey.ToSnapshot(TravelRules));
                }

                Journey.ResumeFromEncounter();
                var resolution = new TravelDiaryEncounterResolutionState(
                    resolvedChoiceId,
                    resolvedChoiceLabel,
                    plan.HealthDelta,
                    0m,
                    plan.AmmoSpent,
                    plan.HeatIncrease,
                    plan.HorseExhaustionDelta,
                    plan.ContinuedOnFoot);
                Journey.RecordCurrentDayEncounterResolution(resolution);
                Journey.AdvanceCurrentDayPlan();
                var resolutionResult = ContinueCurrentDayAfterEncounterResolution(
                    encounter,
                    startingState,
                    dayEntries,
                    resolution);
                ProduceEncounterResolvedEvent(resolvedChoiceId, resolvedChoiceLabel, plan.AmmoSpent, null, 0, plan.HorseExhaustionDelta, resolutionResult);
                return resolutionResult;
            }

            case "bribe":
            {
                var bribeOffer = bribeAmount ?? TravelRules.EncounterBribeCash;
                if (bribeOffer < 0m)
                {
                    bribeOffer = 0m;
                }

                if (!Player.CanAfford(bribeOffer))
                {
                    return JourneyEncounterResolutionResult.Failed($"You need ${bribeOffer:0.00} to bribe your way through.", Journey.Status, Journey.ToSnapshot(TravelRules));
                }

                var availableFood = Player.GetQuantity(ItemKind.Food);
                var availableHorseFeed = Player.GetQuantity(ItemKind.HorseFeed);
                var availableRevolverAmmo = Player.GetQuantity(ItemKind.RevolverAmmo);
                var availableRifleAmmo = Player.GetQuantity(ItemKind.RifleAmmo);
                var plan = JourneyEncounterResolutionEngine.ResolveBribe(
                    encounter,
                    Player.Wallet.Cash,
                    TravelRules,
                    bribeOffer,
                    availableFood,
                    availableHorseFeed,
                    availableRevolverAmmo,
                    availableRifleAmmo,
                    roll);

                if (plan.WalletDelta != 0m)
                {
                    Player.AdjustCash(plan.WalletDelta);
                }

                if (plan.StolenItemKind is not null && plan.StolenItemQuantity > 0)
                {
                    Player.RemoveQuantity(plan.StolenItemKind.Value, plan.StolenItemQuantity);
                }

                if (plan.HealthDelta != 0)
                {
                    Player.AdjustHealth(plan.HealthDelta);
                }

                // Encounter bribe: visible/noisy incident that draws future lawman attention. See ADR-0029.
                if (plan.HeatIncrease != 0)
                {
                    PursuitState.IncreaseHeat(plan.HeatIncrease);
                }

                dayEntries.Add(plan.Message);

                var retaliated = !plan.Resolved && (plan.HealthDelta < 0 || plan.StolenItemKind is not null || plan.WalletDelta < -bribeOffer);
                if (!plan.Resolved && !retaliated)
                {
                    Journey.UpdatePendingEncounter(plan.UpdatedEncounter);
                    PersistLatestTravelDiaryDay(startingState, dayEntries, Journey.PendingEncounter);
                    ProduceEvent(new JourneyEncounterResolved
                    {
                        ChoiceId = resolvedChoiceId,
                        ChoiceLabel = resolvedChoiceLabel,
                        Resolved = false,
                        PlayerHealth = Player.Health,
                        WalletCash = Player.Wallet.Cash,
                        AmmoSpent = 0,
                        StolenItemKind = plan.StolenItemKind,
                        StolenItemQuantity = plan.StolenItemQuantity,
                        PursuitHeat = PursuitState.Heat,
                        HorseExhaustionDelta = plan.HorseExhaustionDelta,
                        ContinuedOnFoot = Journey.TravelMode == TravelMode.Foot,
                        JourneySnapshot = Journey.ToSnapshot(TravelRules),
                        DiaryMessage = plan.Message,
                        DayCompleted = false,
                        JourneyCompleted = false
                    });
                    return new JourneyEncounterResolutionResult(false, true, Journey.Status, plan.Message, Journey.ToSnapshot(TravelRules));
                }

                Journey.ResumeFromEncounter();
                var resolution = new TravelDiaryEncounterResolutionState(
                    resolvedChoiceId,
                    resolvedChoiceLabel,
                    plan.HealthDelta,
                    plan.WalletDelta,
                    0,
                    plan.HeatIncrease,
                    plan.HorseExhaustionDelta,
                    Journey.TravelMode == TravelMode.Foot);
                Journey.RecordCurrentDayEncounterResolution(resolution);
                Journey.AdvanceCurrentDayPlan();
                var resolutionResult = ContinueCurrentDayAfterEncounterResolution(
                    encounter,
                    startingState,
                    dayEntries,
                    resolution);
                ProduceEncounterResolvedEvent(resolvedChoiceId, resolvedChoiceLabel, 0, plan.StolenItemKind, plan.StolenItemQuantity, plan.HorseExhaustionDelta, resolutionResult);
                return retaliated ? resolutionResult with { Success = false } : resolutionResult;
            }

            default:
                return JourneyEncounterResolutionResult.Failed("That choice is not available for this encounter.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }
    }

    /// <summary>
    /// Produces a JourneyEncounterResolved event after ContinueCurrentDayAfterEncounterResolution.
    /// The journey snapshot captures the final state (interrupted by next encounter, active, or completed).
    /// PursuitHeat is ABSOLUTE. HealthDelta/WalletDelta/AmmoSpent/StolenItem are ADDITIVE.
    /// See ADR-0028 and BUNCH-83.
    /// </summary>
    private void ProduceEncounterResolvedEvent(
        string choiceId,
        string choiceLabel,
        int ammoSpent,
        WildBunch.Domain.Inventory.ItemKind? stolenItemKind,
        int stolenItemQuantity,
        int horseExhaustionDelta,
        JourneyEncounterResolutionResult resolutionResult)
    {
        var journeyCompleted = resolutionResult.Status == JourneyStatus.Completed;
        var dayCompleted = journeyCompleted || (Journey?.CurrentDayPlan?.IsComplete ?? false);
        ProduceEvent(new JourneyEncounterResolved
        {
            ChoiceId = choiceId,
            ChoiceLabel = choiceLabel,
            Resolved = true,
            PlayerHealth = Player.Health,
            WalletCash = Player.Wallet.Cash,
            AmmoSpent = ammoSpent,
            StolenItemKind = stolenItemKind,
            StolenItemQuantity = stolenItemQuantity,
            PursuitHeat = PursuitState.Heat,
            HorseExhaustionDelta = horseExhaustionDelta,
            ContinuedOnFoot = Journey?.TravelMode == TravelMode.Foot,
            JourneySnapshot = Journey?.ToSnapshot(TravelRules) ?? resolutionResult.Journey!,
            DiaryMessage = resolutionResult.Message,
            DayCompleted = dayCompleted,
            JourneyCompleted = journeyCompleted,
            AdditionalDiaryMessages = resolutionResult.AdditionalDiaryMessages ?? []
        });
    }

    private JourneyEncounterResolutionResult ContinueCurrentDayAfterEncounterResolution(
        JourneyEncounterState resolvedEncounter,
        TravelDiaryBaselineState startingState,
        List<string> dayEntries,
        TravelDiaryEncounterResolutionState resolution)
    {
        var narrationMessages = new List<string>();
        while (Journey is not null && Journey.CurrentDayPlan is not null && !Journey.CurrentDayPlan.IsComplete)
        {
            var currentEncounter = Journey.CurrentDayPlan.CurrentEncounter;
            if (currentEncounter is null)
            {
                break;
            }

            if (currentEncounter.RequiresChoice)
            {
                var pendingEncounter = currentEncounter.PendingEncounter!;
                Journey.MarkInterrupted(pendingEncounter);
                var pendingMessage = pendingEncounter.Message;
                dayEntries.Add(pendingMessage);

                var pendingSnapshot = Journey.ToSnapshot(TravelRules);
                var pendingResources = TravelResourceSnapshotFactory.Capture(Player, PursuitState);
                PersistLatestTravelDiaryDay(
                    startingState,
                    dayEntries,
                    pendingEncounter,
                    resolution,
                    pendingSnapshot,
                    pendingResources);

                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Interrupted, pendingMessage, pendingSnapshot, narrationMessages);
            }

            if (currentEncounter.TrailEvent is not null)
            {
                var trailEventApplication = ApplyTrailEvent(
                    currentEncounter.TrailEvent,
                    prefixHorseLostMessage: string.Empty,
                    currentEncounter.Message);
                var encounterMessage = PrependHorseLossMessage(trailEventApplication.HorseLossMessage, currentEncounter.Message);
                dayEntries.Add(encounterMessage);
            }
            else if (!string.IsNullOrWhiteSpace(currentEncounter.Message))
            {
                dayEntries.Add(currentEncounter.Message);
                narrationMessages.Add(currentEncounter.Message);
            }

            Journey.AdvanceCurrentDayPlan();
        }

        var journeySnapshot = Journey!.ToSnapshot(TravelRules);
        var currentResources = TravelResourceSnapshotFactory.Capture(Player, PursuitState);
        PersistLatestTravelDiaryDay(
            startingState,
            dayEntries,
            resolvedEncounter,
            resolution,
            journeySnapshot,
            currentResources);

        if (Journey.CurrentDayPlan?.IsComplete == true)
        {
            Journey.SetCurrentDayPlan(null);
        }

        if (Journey.RemainingDays == 0 && Journey.RemainingRideDayDistance == 0)
        {
            var destinationTownName = Journey.Preview.DestinationTownName;
            var destinationTownId = Journey.Preview.DestinationTownId;
            var completedSnapshot = CompleteJourneyAtDestination();
            ProduceEvent(new JourneyCompleted
            {
                JourneySnapshot = completedSnapshot,
                DestinationTownId = destinationTownId,
                DestinationTownName = destinationTownName,
                DiaryMessage = string.Empty
            });
            currentResources = TravelResourceSnapshotFactory.Capture(Player, PursuitState);
            PersistLatestTravelDiaryDay(
                startingState,
                Array.Empty<string>(),
                resolvedEncounter,
                resolution,
                completedSnapshot,
                currentResources);
            return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Completed, $"You clear the remaining trail and reach {destinationTownName}.", completedSnapshot, narrationMessages);
        }

        Journey.ResumeFromEncounter();
        return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, "You push the rider behind you and keep moving.", journeySnapshot, narrationMessages);
    }

    private TravelDiaryBaselineState RebuildCurrentTravelDiaryBaselineState()
    {
        if (_travelDiaryDays.Count == 0)
        {
            throw new InvalidOperationException("There is no travel diary day to resume from.");
        }

        if (Journey is null)
        {
            throw new InvalidOperationException("A journey is required to rebuild the travel diary baseline.");
        }

        var latestDay = _travelDiaryDays[^1];
        var startingResources = new TravelResourceSnapshot(
            latestDay.HorseStateBefore,
            latestDay.CurrentWallet - latestDay.WalletDelta,
            latestDay.CurrentFood - latestDay.FoodDelta,
            latestDay.CurrentHorseFeed - latestDay.HorseFeedDelta,
            latestDay.CurrentCanteenCharges - latestDay.CanteenChargeDelta,
            latestDay.CurrentAmmo + latestDay.AmmoSpent,
            latestDay.CurrentHealth - latestDay.HealthDelta,
            latestDay.CurrentHeat - latestDay.HeatIncrease);

        return new TravelDiaryBaselineState(
            latestDay.StartingTravelMode,
            latestDay.StartingRideDayDistance,
            latestDay.StartingDaysRemaining,
            Journey.DelayDays - latestDay.DelayDays,
            startingResources);
    }

    private JourneyEncounterState RecoverFoeProfile(JourneyEncounterState encounter)
    {
        if (Journey is null)
        {
            throw new InvalidOperationException("A journey is required to recover foe profile data.");
        }

        var context = CreateTravelDayGenerationContext(TravelDayPlanGenerator.CurrentVersion);
        var fallbackSeed = string.Join(
            "|",
            Journey.Preview.RouteProfile.TrailId,
            Journey.DaysTravelled,
            Journey.DelayDays,
            Journey.TravelMode,
            Journey.RemainingRideDayDistance,
            Journey.RemainingDays,
            Player.Health,
            Player.Wallet.Cash,
            PursuitState.Heat,
            TravelRandomness.Salt,
            encounter.Message);

        var foeProfile = JourneyEncounterResolutionEngine.CreateFoeProfile(context, TravelRules, fallbackSeed);
        return encounter with { FoeProfile = foeProfile };
    }

    private static string BuildJourneyOpeningNarration(TravelPreview preview)
    {
        var baselineRidePhrase = $"{preview.BaselineRideDays}-day {DescribeTerrain(preview.RouteProfile.Terrain)} ride";
        var travelMode = DescribeTravelMode(preview.TravelMode);
        var risk = DescribeRisk(preview.RouteProfile.Risk);
        var waterPressure = preview.WaterSecure
            ? $"I had enough water for the base trail, though the canteen still needed watching on a {preview.ExpectedDays}-day run."
            : $"This dry trail asked for {preview.CanteenChargesPerDay} canteen charge(s) a day, and I did not have much slack.";
        var foodPressure = preview.AvailableFood <= preview.ExpectedDays
            ? "My food was tight enough that I noticed every meal."
            : "My food should have held if the trail behaved itself.";
        var horsePressure = preview.HorseState is null
            ? "I was traveling without a horse, so the road had to be enough."
            : preview.MountedTravelAvailable
                ? "My horse was fit enough to carry me for now."
                : "My horse was not fit for mounted travel, so I needed to mind the pace.";

        var openingSentence = preview.TravelMode == TravelMode.Foot
            ? preview.ExpectedDays != preview.BaselineRideDays
                ? $"I set out for {preview.DestinationTownName} on a {baselineRidePhrase}, but without a horse it would take {preview.ExpectedDays} days on foot."
                : $"I set out for {preview.DestinationTownName} on a {baselineRidePhrase} on foot."
            : $"I set out for {preview.DestinationTownName} on a {baselineRidePhrase} {travelMode}.";

        return $"{openingSentence} {risk} {waterPressure} {foodPressure} {horsePressure}";
    }

    private sealed record TrailEventApplicationResult(string HorseLossMessage);

    private sealed record TravelDayAdvanceState(
        TravelDiaryBaselineState StartingState,
        string HorseLostMessage,
        JourneyProgress Progress,
        int NewDay,
        int PursuitHeat);

    public JourneyEncounterResolutionResult ResolveJourneyEncounter(string choiceId)
        => ResolveJourneyEncounter(choiceId, bulletSpend: null, bribeAmount: null, forcedRoll: null);

    public JourneyEncounterResolutionResult ResolveJourneyEncounter(string choiceId, int? bulletSpend, decimal? bribeAmount)
        => ResolveJourneyEncounter(choiceId, bulletSpend, bribeAmount, forcedRoll: null);

    internal JourneyEncounterResolutionResult ResolveJourneyEncounter(
        string choiceId,
        int? bulletSpend,
        decimal? bribeAmount,
        ulong? forcedRoll)
        => ResolveJourneyEncounterDeterministic(choiceId, bulletSpend, bribeAmount, forcedRoll);

    private static string DescribeTravelMode(TravelMode travelMode)
        => travelMode == TravelMode.Mounted ? "by mounted travel" : "on foot";

    private static string DescribeCanteenCoverage(TravelPreview preview)
        => DescribeCanteenCoverage(preview.RouteProfile.WaterFeature, preview.CanteenChargesPerDay, preview.CanteenReserveCharges, preview.DelayMarginDays);

    private static string DescribeCanteenCoverage(TravelJourneySnapshot snapshot)
        => DescribeCanteenCoverage(snapshot.RouteProfile.WaterFeature, snapshot.CanteenChargesPerDay, snapshot.CanteenReserveCharges, snapshot.DelayMarginDays);

    private static string DescribeCanteenCoverage(
        WaterFeature waterFeature,
        int canteenChargesPerDay,
        int canteenReserveCharges,
        int delayMarginDays)
    {
        if (JourneyUpkeepRules.HasRouteWater(waterFeature))
        {
            return "Route water is secure, so no canteen reserve is required";
        }

        if (canteenChargesPerDay <= 0)
        {
            return "No canteen water is required on this trail";
        }

        if (canteenReserveCharges == 0)
        {
            return "The canteen exactly covers the base trail and has no reserve for delays";
        }

        if (canteenReserveCharges > 0)
        {
            return $"The canteen has {canteenReserveCharges} spare charge(s) and can absorb {delayMarginDays} delay day(s)";
        }

        return $"The canteen is short by {Math.Abs(canteenReserveCharges)} charge(s) for the base trail";
    }

    private void ArchiveCompletedJourney(TravelJourneySnapshot completedJourney)
    {
        _completedJourneyHistory.Add(completedJourney);
    }

    private static int CalculateNextJourneySequence(TravelJourney? journey, IReadOnlyList<TravelJourneySnapshot> completedJourneyHistory)
    {
        var maxSequence = journey?.JourneySequence ?? 0;

        if (completedJourneyHistory.Count > 0)
        {
            maxSequence = Math.Max(maxSequence, completedJourneyHistory.Max(history => history.JourneySequence));
        }

        return Math.Max(1, maxSequence + 1);
    }

    public StorePurchaseResult Purchase(StoreOffer offer, int quantity)
    {
        ArgumentNullException.ThrowIfNull(offer);

        if (IsJourneyModal())
        {
            return StorePurchaseResult.Failed(JourneyModalBlockMessage);
        }

        if (quantity < 1)
        {
            return StorePurchaseResult.Failed("Quantity must be at least 1.");
        }

        if (offer.ItemKind == ItemKind.Horse && quantity != 1)
        {
            return StorePurchaseResult.Failed("Horse items must have a quantity of 1.");
        }

        if (quantity != 1 && !IsStackableItemKind(offer.ItemKind))
        {
            return StorePurchaseResult.Failed($"{offer.ItemKind} does not stack.");
        }

        var totalPrice = offer.Price * quantity;
        if (!Player.CanAfford(totalPrice))
        {
            return StorePurchaseResult.Failed("Not enough cash.");
        }

        if (!CanPurchaseInventoryItem(offer, quantity, out var inventoryFailureMessage))
        {
            return StorePurchaseResult.Failed(inventoryFailureMessage);
        }

        // Produce typed domain event and apply it (event-sourced mutation path)
        var e = new StoreItemPurchased
        {
            TownId = CurrentTown.TownId,
            ItemKind = offer.ItemKind,
            DisplayName = offer.DisplayName,
            Quantity = quantity,
            UnitPrice = offer.Price,
            TotalPrice = totalPrice,
            WalletAfter = Player.Wallet.Cash - totalPrice
        };
        Apply(e);
        _uncommittedEvents.Add(e);

        var quantityLabel = quantity == 1 ? offer.DisplayName : $"{quantity} {offer.DisplayName}";
        return StorePurchaseResult.Succeeded($"Purchased {quantityLabel} for ${totalPrice:0.00}.");
    }

    public ReadWantedPostersResult ReadWantedPosters()
    {
        if (IsJourneyModal())
        {
            return ReadWantedPostersResult.Failed(JourneyModalBlockMessage);
        }

        if (!CurrentTown.SupportsWantedPosters)
        {
            return ReadWantedPostersResult.Failed("There are no wanted posters here.");
        }

        EnterActionContext(TownActionContext.SheriffOffice);

        if (CurrentTownVisit.WantedPostersSpent)
        {
            var msg = "You study the wanted posters again, but find nothing new.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.SheriffWarrants,
                TownId = CurrentTown.TownId,
                Message = msg
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return ReadWantedPostersResult.Succeeded(msg, sessionChanged: true);
        }

        var warrant = CaseFile.PeekNextPublicWarrant(InvestigationSourceKind.SheriffWarrants);
        var clue = CaseFile.PeekNextPublicClue(publicClue =>
            IsPlayerKnownClue(publicClue)
            && publicClue.SourceKind == InvestigationSourceKind.SheriffWarrants);

        if (warrant is null && clue is null)
        {
            var msg = "You study the wanted posters, but find nothing new.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.SheriffWarrants,
                TownId = CurrentTown.TownId,
                Message = msg
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return ReadWantedPostersResult.Succeeded(msg, sessionChanged: true);
        }

        if (warrant is not null && clue is not null)
        {
            var msg = $"You study the wanted posters and copy down a wanted notice for {warrant.TargetName}, noting a public lead: {DescribeClueLead(clue.Description)}.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.SheriffWarrants,
                TownId = CurrentTown.TownId,
                Message = msg,
                ClueId = clue?.Id,
                WarrantId = warrant?.Id
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return ReadWantedPostersResult.Succeeded("You study the wanted posters and uncover a wanted notice and a public lead.", sessionChanged: true);
        }

        if (warrant is not null)
        {
            var msg = $"You study the wanted posters and copy down a wanted notice for {warrant.TargetName}.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.SheriffWarrants,
                TownId = CurrentTown.TownId,
                Message = msg,
                WarrantId = warrant?.Id
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return ReadWantedPostersResult.Succeeded(msg, sessionChanged: true);
        }

        var clueOnlyMsg = $"You study the wanted posters and note a public lead: {DescribeClueLead(clue!.Description)}.";
        var clueOnlyEvent = new InvestigationPerformed
        {
            SourceKind = InvestigationSourceKind.SheriffWarrants,
            TownId = CurrentTown.TownId,
            Message = clueOnlyMsg,
            ClueId = clue?.Id
        };
        Apply(clueOnlyEvent);
        _uncommittedEvents.Add(clueOnlyEvent);
        return ReadWantedPostersResult.Succeeded("You study the wanted posters and uncover a public lead.", sessionChanged: true);
    }

    public CaseInvestigationResult LookAroundSaloon()
    {
        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        if (!CurrentTown.IsAvailable(InvestigationSourceKind.SaloonLookAround))
        {
            return CaseInvestigationResult.Failed("There is no saloon here.");
        }

        // Enter saloon context AFTER availability check, BEFORE local action resolution.
        // Emits TownActionContextEntered event if context changed (advances turn).
        // If no saloon exists, we already returned above — no context event, no turn advance.
        EnterActionContext(TownActionContext.Saloon);

        if (CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround))
        {
            var repeatMessage = "You look around the saloon again, but nobody of interest is here.";
            var repeatEvent = new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = CurrentTown.TownId,
                Message = repeatMessage,
                RecordLog = true
            };
            ProduceEvent(repeatEvent);
            return CaseInvestigationResult.Succeeded(repeatMessage, sessionChanged: true);
        }

        if (TryGetConfrontableSaloonPersonOfInterestCandidateInTown(out var suspect))
        {
            var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, CaseFile);
            var spotMessage = $"You look around the saloon and spot {descriptor}.";
            var spotEvent = new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = CurrentTown.TownId,
                Message = spotMessage,
                SuspectId = suspect.Id,
                Descriptor = descriptor,
                PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
                RecordLog = true
            };
            ProduceEvent(spotEvent);
            return CaseInvestigationResult.Succeeded(spotMessage, sessionChanged: true);
        }

        var citizenDescriptor = DescribeTownCitizen(CurrentTown);
        var citizenMessage = $"You look around the saloon and spot {citizenDescriptor}.";
        var citizenEvent = new SaloonPersonOfInterestSpotted
        {
            SourceKind = InvestigationSourceKind.SaloonLookAround,
            TownId = CurrentTown.TownId,
            Message = citizenMessage,
            Descriptor = citizenDescriptor,
            PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
            RecordLog = false
        };
        ProduceEvent(citizenEvent);
        return CaseInvestigationResult.Succeeded(citizenMessage, sessionChanged: true);
    }

    public SaloonPersonOfInterestConfrontationResult ConfrontSaloonPersonOfInterest(string? declaredWantedIdentityHandle = null)
        => _bountyLoopCoordinator.ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle);

    public WantedSuspectConfrontationResult ConfrontSaloonWantedSuspect(string? declaredWantedIdentityHandle = null)
        => _bountyLoopCoordinator.ConfrontSaloonWantedSuspect(declaredWantedIdentityHandle);

    public WantedSuspectConfrontationResult ResolveWantedSuspectConfrontation(
        SuspectId targetSuspectId,
        WantedSuspectConfrontationChoice choice,
        string? declaredWantedIdentityHandle = null)
        => _bountyLoopCoordinator.ResolveWantedSuspectConfrontation(targetSuspectId, choice, declaredWantedIdentityHandle);

    private void UpdateWantedSuspectPresence(SuspectId suspectId, WantedSuspectConfrontationChoice choice)
    {
        var nextPresenceState = choice switch
        {
            WantedSuspectConfrontationChoice.Surrendered => WantedSuspectPresenceState.SecuredAlive,
            WantedSuspectConfrontationChoice.Fled => WantedSuspectPresenceState.GoneToGround,
            WantedSuspectConfrontationChoice.Killed => WantedSuspectPresenceState.SecuredDead,
            _ => WantedSuspectPresenceState.Unavailable
        };

        if (nextPresenceState != WantedSuspectPresenceState.Unavailable)
        {
            SetWantedSuspectPresenceState(suspectId, nextPresenceState);
        }
    }

    public SheriffTurnInResult AssessSheriffTurnIn(SuspectId targetSuspectId, bool isAlive)
        => _bountyLoopCoordinator.AssessSheriffTurnIn(targetSuspectId, isAlive);

    public SheriffTurnInResult SettleSheriffTurnIn(SuspectId targetSuspectId, bool isAlive)
        => _bountyLoopCoordinator.SettleSheriffTurnIn(targetSuspectId, isAlive);

    public CaseInvestigationResult FollowTelegraphLeads()
    {
        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        if (!CurrentTown.IsAvailable(InvestigationSourceKind.TelegraphLead))
        {
            return CaseInvestigationResult.Failed("There is no telegraph office here.");
        }

        EnterActionContext(TownActionContext.TelegraphOffice);

        if (CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead))
        {
            var msg = "You ask after telegraph leads again, but no new wire has come in.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.TelegraphLead,
                TownId = CurrentTown.TownId,
                Message = msg
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return CaseInvestigationResult.Succeeded(msg, sessionChanged: true);
        }

        var clue = CaseFile.PeekNextPublicClue(c => IsPlayerKnownClue(c) && c.SourceKind == InvestigationSourceKind.TelegraphLead);

        if (clue is null)
        {
            var msg = "You follow the telegraph leads, but find nothing new.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.TelegraphLead,
                TownId = CurrentTown.TownId,
                Message = msg
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return CaseInvestigationResult.Succeeded(msg, sessionChanged: true);
        }

        var foundMsg = $"You follow the telegraph leads and uncover a public lead: {DescribeClueLead(clue.Description)}.";
        var foundEvent = new InvestigationPerformed
        {
            SourceKind = InvestigationSourceKind.TelegraphLead,
            TownId = CurrentTown.TownId,
            Message = foundMsg,
            ClueId = clue?.Id
        };
        Apply(foundEvent);
        _uncommittedEvents.Add(foundEvent);
        return CaseInvestigationResult.Succeeded("You follow the telegraph leads and uncover a public lead.", sessionChanged: true);
    }

    public CaseInvestigationResult GatherLocalGossip()
    {
        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        EnterActionContext(TownActionContext.Saloon);

        if (CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalGossip))
        {
            var msg = "You ask around again, but hear nothing new.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.LocalGossip,
                TownId = CurrentTown.TownId,
                Message = msg
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return CaseInvestigationResult.Succeeded(msg, sessionChanged: true);
        }

        var clue = CaseFile.PeekNextPublicClue(c => IsPlayerKnownClue(c) && c.SourceKind == InvestigationSourceKind.LocalGossip);

        if (clue is null)
        {
            var msg = "You ask around for local gossip, but hear nothing new.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.LocalGossip,
                TownId = CurrentTown.TownId,
                Message = msg
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return CaseInvestigationResult.Succeeded(msg, sessionChanged: true);
        }

        var foundMsg = $"You ask around for local gossip and uncover a public lead: {DescribeClueLead(clue.Description)}.";
        var foundEvent = new InvestigationPerformed
        {
            SourceKind = InvestigationSourceKind.LocalGossip,
            TownId = CurrentTown.TownId,
            Message = foundMsg,
            ClueId = clue?.Id
        };
        Apply(foundEvent);
        _uncommittedEvents.Add(foundEvent);
        return CaseInvestigationResult.Succeeded("You ask around for local gossip and uncover a public lead.", sessionChanged: true);
    }

    public CaseInvestigationResult InspectNoticeBoard()
    {
        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        EnterActionContext(TownActionContext.TownSquare);

        if (CurrentTownVisit.IsSpent(InvestigationSourceKind.NoticeBoard))
        {
            var msg = "You inspect the notice board again, but nothing new has been posted.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.NoticeBoard,
                TownId = CurrentTown.TownId,
                Message = msg
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return CaseInvestigationResult.Succeeded(msg, sessionChanged: true);
        }

        var clue = CaseFile.PeekNextPublicClue(c => c.SourceKind == InvestigationSourceKind.NoticeBoard);

        if (clue is null)
        {
            var msg = "You inspect the notice board, but find nothing new.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.NoticeBoard,
                TownId = CurrentTown.TownId,
                Message = msg
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return CaseInvestigationResult.Succeeded(msg, sessionChanged: true);
        }

        var foundMsg = $"You inspect the notice board and uncover a civic notice: {DescribeClueLead(clue.Description)}.";
        var foundEvent = new InvestigationPerformed
        {
            SourceKind = InvestigationSourceKind.NoticeBoard,
            TownId = CurrentTown.TownId,
            Message = foundMsg,
            ClueId = clue?.Id
        };
        Apply(foundEvent);
        _uncommittedEvents.Add(foundEvent);
        return CaseInvestigationResult.Succeeded("You inspect the notice board and uncover a civic notice.", sessionChanged: true);
    }

    public CaseInvestigationResult CheckSheriffRecords()
    {
        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        EnterActionContext(TownActionContext.SheriffOffice);

        if (CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalRecords))
        {
            var msg = "You check the local records again, but find nothing new.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.LocalRecords,
                TownId = CurrentTown.TownId,
                Message = msg
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return CaseInvestigationResult.Succeeded(msg, sessionChanged: true);
        }

        var clue = CaseFile.PeekNextPublicClue(c => IsPlayerKnownClue(c) && c.SourceKind == InvestigationSourceKind.LocalRecords);

        if (clue is null)
        {
            var msg = "You check the local records, but find nothing new.";
            var e = new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.LocalRecords,
                TownId = CurrentTown.TownId,
                Message = msg
            };
            Apply(e);
            _uncommittedEvents.Add(e);
            return CaseInvestigationResult.Succeeded(msg, sessionChanged: true);
        }

        var foundMsg = $"You check the local records and uncover a public lead: {DescribeClueLead(clue.Description)}.";
        var foundEvent = new InvestigationPerformed
        {
            SourceKind = InvestigationSourceKind.LocalRecords,
            TownId = CurrentTown.TownId,
            Message = foundMsg,
            ClueId = clue?.Id
        };
        Apply(foundEvent);
        _uncommittedEvents.Add(foundEvent);
        return CaseInvestigationResult.Succeeded("You check the local records and uncover a public lead.", sessionChanged: true);
    }

    public void RecordCaseUpdate(string message)
    {
        AddLogEntry(GameLogEntryKind.CaseUpdate, message);
    }

    public void AppendTravelDiaryDay(TravelDiaryDayState travelDiaryDay)
    {
        ArgumentNullException.ThrowIfNull(travelDiaryDay);
        _travelDiaryDays.Add(travelDiaryDay);
    }

    public bool UpdateLatestTravelDiaryDay(Func<TravelDiaryDayState, TravelDiaryDayState> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (_travelDiaryDays.Count == 0)
        {
            return false;
        }

        var lastIndex = _travelDiaryDays.Count - 1;
        _travelDiaryDays[lastIndex] = update(_travelDiaryDays[lastIndex]);
        return true;
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

    private bool TryGetKnownWarrantForSuspect(SuspectId suspectId, [NotNullWhen(true)] out Warrant? warrant)
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

    private bool PersistLatestTravelDiaryDay(
        TravelDiaryBaselineState startingState,
        IReadOnlyList<string> newEntries,
        JourneyEncounterState? pendingEncounter = null,
        TravelDiaryEncounterResolutionState? encounterResolution = null,
        TravelJourneySnapshot? journeySnapshot = null,
        TravelResourceSnapshot? currentResources = null,
        JourneyTrailEventState? trailEvent = null)
    {
        ArgumentNullException.ThrowIfNull(startingState);
        ArgumentNullException.ThrowIfNull(newEntries);

        if (_travelDiaryDays.Count == 0)
        {
            return false;
        }

        journeySnapshot ??= Journey?.ToSnapshot(TravelRules);
        if (journeySnapshot is null)
        {
            return false;
        }

        currentResources ??= TravelResourceSnapshotFactory.Capture(Player, PursuitState);
        var combinedEntries = _travelDiaryDays[^1].Entries.Concat(newEntries).ToArray();

        return UpdateLatestTravelDiaryDay(day => TravelDiaryDayFactory.Create(
            journeySnapshot,
            startingState,
            currentResources,
            trailEvent: trailEvent,
            pendingEncounter: pendingEncounter,
            encounterResolution: encounterResolution,
            entries: combinedEntries));
    }

    public void ReplaceTravelDiaryDays(IReadOnlyList<TravelDiaryDayState> travelDiaryDays)
    {
        ArgumentNullException.ThrowIfNull(travelDiaryDays);

        _travelDiaryDays.Clear();
        _travelDiaryDays.AddRange(travelDiaryDays);
    }

    [Obsolete("AddLogEntry is projection-legacy per ADR-0028. Use typed domain events and IDomainEventProjector instead.")]
    private void AddLogEntry(GameLogEntryKind kind, string message)
    {
        _logEntries.Add(new GameLogEntry(kind, message, Clock.Day, Clock.Turn));
    }

    private static string DescribeClueLead(string description)
        => description.Trim().TrimEnd('.', '!', '?');

    private static string DescribeTownCitizen(TownAggregate town)
        => $"a town clerk from {town.TownName}";

    private static bool IsPlayerKnownClue(Clue clue)
    {
        ArgumentNullException.ThrowIfNull(clue);

        if (clue.Kind is ClueKind.Warrant or ClueKind.Alias or ClueKind.IdentityFact or ClueKind.CulpritTrail)
        {
            return true;
        }

        return clue.Anchors.Subjects.Any(subject =>
            !string.IsNullOrWhiteSpace(subject.Alias)
            || !string.IsNullOrWhiteSpace(subject.Feature));
    }

    private bool TryGetConfrontableSaloonPersonOfInterestCandidateInTown(out Suspect suspect)
    {
        foreach (var candidate in CaseFile.Suspects)
        {
            if (!IsEligibleSaloonPersonOfInterestCandidate(candidate))
            {
                continue;
            }

            suspect = candidate;
            return true;
        }

        suspect = null!;
        return false;
    }

    private bool IsEligibleSaloonPersonOfInterestCandidate(Suspect suspect)
    {
        ArgumentNullException.ThrowIfNull(suspect);

        if (suspect.Id.Equals(CaseFile.TrueCulpritId))
        {
            return false;
        }

        if (!TryGetKnownWarrantForSuspect(suspect.Id, out _))
        {
            return true;
        }

        if (!_wantedSuspectPresenceLedger.TryGetState(suspect.Id, out var presenceState))
        {
            return false;
        }

        return presenceState is WantedSuspectPresenceState.AvailableInTown or WantedSuspectPresenceState.GoneToGround;
    }

    private static WantedSuspectConfrontationResult ResolveSaloonPersonOfInterestCompatibilityResult(SaloonPersonOfInterestConfrontationResult result)
        => result.ToWantedSuspectResult();

    private bool IsJourneyModal()
        => Journey is not null;

    private bool CanPurchaseInventoryItem(StoreOffer offer, int quantity, out string failureMessage)
    {
        if (quantity < 1)
        {
            failureMessage = "Quantity must be at least 1.";
            return false;
        }

        if (offer.ItemKind == ItemKind.Horse)
        {
            if (quantity != 1)
            {
                failureMessage = "Horse items must have a quantity of 1.";
                return false;
            }

            if (Player.HasItem(ItemKind.Horse))
            {
                failureMessage = "Horse already exists in inventory.";
                return false;
            }

            failureMessage = string.Empty;
            return true;
        }

        if (quantity != 1 && !IsStackableItemKind(offer.ItemKind))
        {
            failureMessage = $"{offer.ItemKind} does not stack.";
            return false;
        }

        if (!IsStackableItemKind(offer.ItemKind) && Player.HasItem(offer.ItemKind))
        {
            failureMessage = $"{offer.ItemKind} already exists in inventory.";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static bool IsStackableItemKind(ItemKind kind)
        => kind is ItemKind.Food or ItemKind.HorseFeed or ItemKind.RevolverAmmo or ItemKind.RifleAmmo;

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
