using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;
using WildBunch.Domain.WantedPosters;

namespace WildBunch.Domain.Game;

/// <summary>
/// Mutable live play-state aggregate root.
/// Command handlers load and persist this root through <see cref="WildBunch.Application.Abstractions.IGameSessionRepository"/>.
/// </summary>
public sealed class GameSession : WildBunch.Domain.IAggregateRoot
{
    private const string JourneyModalBlockMessage = "Finish the current journey before taking that action.";

    private readonly List<GameLogEntry> _logEntries = [];
    private readonly List<TravelDiaryDayState> _travelDiaryDays = [];
    private readonly List<TravelJourneySnapshot> _completedJourneyHistory = [];
    private int _nextJourneySequence = 1;
    private readonly TownAggregate _currentTown;

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
        IReadOnlyList<TravelJourneySnapshot>? completedJourneyHistory)
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

        if (completedJourneyHistory is not null)
        {
            _completedJourneyHistory.AddRange(completedJourneyHistory);
        }

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

    public IReadOnlyList<GameLogEntry> LogEntries => _logEntries;

    public IReadOnlyList<TravelDiaryDayState> TravelDiaryDays => _travelDiaryDays;

    public IReadOnlyList<TravelJourneySnapshot> CompletedJourneyHistory => _completedJourneyHistory;

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
        var player = new Player(
            playerName,
            startingTown.Id,
            health: StartingHealthFor(travelDifficulty),
            wallet ?? WildBunch.Domain.Economy.Wallet.Starting(25m),
            inventory ?? DomainInventory.Empty());
        var session = new GameSession(
            GameSessionId.New(),
            player,
            world,
            caseFile,
            new PursuitState(),
            new GameClock(),
            GameStatus.Active,
            journey: null,
            travelDifficulty,
            travelRandomness ?? TravelRandomnessState.CreateRuntimeSalted(),
            entropy,
            currentTownVisit: null,
            Array.Empty<TravelJourneySnapshot>());

        session.AddLogEntry(GameLogEntryKind.Opening, $"The hunt begins in {startingTown.Name}.");
        return session;
    }

    private static int StartingHealthFor(TravelDifficulty travelDifficulty)
        => travelDifficulty switch
        {
            TravelDifficulty.Easy => 1250,
            TravelDifficulty.Hard => 800,
            _ => 1000
        };

    public TravelJourneyStepResult StartJourney(TravelPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);

        if (Journey is not null)
        {
            return TravelJourneyStepResult.Failed("You are already on the trail.");
        }

        Journey = TravelJourney.Start(preview, _nextJourneySequence++, BuildJourneyOpeningNarration(preview));
        _travelDiaryDays.Clear();
        var startMessage = $"You set out from {preview.OriginTownName} toward {preview.DestinationTownName} {DescribeTravelMode(preview.TravelMode)}. The route is {preview.RideDayDistance:0.##} ride-day unit(s) and should take {preview.ExpectedDays} day(s). {DescribeCanteenCoverage(preview)}.";
        AddLogEntry(
            GameLogEntryKind.Travel,
            startMessage);

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            startMessage,
            startMessage,
            0,
            Journey.ToSnapshot(TravelRules));
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

        var horseState = Player.Inventory.GetHorseState();
        if (horseState is null)
        {
            return string.Empty;
        }

        var nextHorseState = horseState.IncreaseExhaustion(exhaustionIncrease);
        Player.Inventory.SetHorseState(nextHorseState);
        Journey!.SetHorseState(nextHorseState);

        if (Journey.TravelMode == TravelMode.Mounted && !nextHorseState.CanProvideMountedTravelFor(TravelRules))
        {
            Journey.RecalculatePacing(TravelMode.Foot);
            return DescribeHorseLoss(nextHorseState, TravelRules);
        }

        return string.Empty;
    }

    private TrailEventApplicationResult ApplyTrailEvent(JourneyTrailEventState trailEvent)
    {
        ArgumentNullException.ThrowIfNull(trailEvent);

        if (trailEvent.WalletDelta != 0m)
        {
            Player.SetWallet(Player.Wallet.Adjust(trailEvent.WalletDelta));
        }

        if (trailEvent.FoodDelta != 0)
        {
            if (trailEvent.FoodDelta > 0)
            {
                Player.Inventory.AddItem(ItemKind.Food, trailEvent.FoodDelta);
                Journey!.AdjustFood(trailEvent.FoodDelta);
            }
            else
            {
                var foodLoss = Math.Abs(trailEvent.FoodDelta);
                Player.Inventory.RemoveQuantity(ItemKind.Food, foodLoss);
                Journey!.AdjustFood(trailEvent.FoodDelta);
            }
        }

        if (trailEvent.CanteenChargeDelta != 0)
        {
            var canteenState = Player.Inventory.GetCanteenState();
            if (canteenState is not null)
            {
                var nextCanteenState = canteenState.AdjustCharges(trailEvent.CanteenChargeDelta);
                Player.Inventory.SetCanteenState(nextCanteenState);
                Journey!.SetCanteenCharges(nextCanteenState.Charges);
            }
        }

        if (trailEvent.HorseHungerDelta != 0 || trailEvent.HorseThirstDelta != 0 || trailEvent.HorseExhaustionDelta != 0)
        {
            var horseState = Player.Inventory.GetHorseState();
            if (horseState is not null)
            {
                horseState = ApplyHorseDelta(horseState, trailEvent);
                Player.Inventory.SetHorseState(horseState);
                Journey!.SetHorseState(horseState);
            }
        }

        if (trailEvent.DelayDays != 0)
        {
            Journey!.AddDelayDays(trailEvent.DelayDays);
        }

        if (trailEvent.HeatIncrease != 0)
        {
            PursuitState.IncreaseHeat(trailEvent.HeatIncrease);
        }

        var horseLossMessage = string.Empty;
        if (Journey!.TravelMode == TravelMode.Mounted && Player.Inventory.GetHorseState()?.CanProvideMountedTravelFor(TravelRules) == false)
        {
            horseLossMessage = DescribeHorseLoss(Player.Inventory.GetHorseState(), TravelRules);
            Journey.RecalculatePacing(TravelMode.Foot);
        }

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

        Journey.MarkCompleted();
        Player.TravelTo(Journey.Preview.DestinationTownId);
        RefreshTownVisit(Journey.Preview.DestinationTownId);
        RefillCanteenAfterArrival();
        return Journey.ToSnapshot(TravelRules);
    }

    private void RefreshTownVisit(TownId townId)
    {
        var currentTown = World.GetTown(townId);
        _currentTown.EnterTown(currentTown);
    }

    private void RefillCanteenAfterArrival()
    {
        var canteenState = Player.Inventory.GetCanteenState();
        if (canteenState is null || canteenState.Charges >= canteenState.Capacity)
        {
            return;
        }

        var refilledCanteen = CanteenState.Full(canteenState.Capacity);
        Player.Inventory.SetCanteenState(refilledCanteen);
        Journey!.SetCanteenCharges(refilledCanteen.Charges);
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

        var capabilities = new InventoryCapabilityResolver().Resolve(Player.Inventory, TravelRules);
        if (Journey.TravelMode == TravelMode.Mounted && !capabilities.MountedTravelAvailable)
        {
            Journey.RecalculatePacing(TravelMode.Foot);
        }

        if (Player.Inventory.GetQuantity(ItemKind.Food) > 0)
        {
            Player.Inventory.RemoveQuantity(ItemKind.Food, 1);
            Journey.ConsumeFood();
        }
        else
        {
            AddLogEntry(GameLogEntryKind.Travel, "My food is gone, but the trail keeps moving.");
        }

        var upkeep = JourneyUpkeepRules.ApplyDailyUpkeep(
            Journey.Preview.RouteProfile.Terrain,
            Journey.Preview.RouteProfile.WaterFeature,
            Player.Inventory.GetHorseState(),
            Player.Inventory.GetCanteenState(),
            Player.Inventory.GetQuantity(ItemKind.HorseFeed),
            TravelRules);

        if (upkeep.HorseFeedConsumed > 0)
        {
            Player.Inventory.RemoveQuantity(ItemKind.HorseFeed, upkeep.HorseFeedConsumed);
            Journey.ConsumeHorseFeed(upkeep.HorseFeedConsumed);
        }

        if (upkeep.CanteenState is not null)
        {
            Player.Inventory.SetCanteenState(upkeep.CanteenState);
            Journey.SetCanteenCharges(upkeep.CanteenState.Charges);
        }
        else
        {
            Journey.SetCanteenCharges(0);
        }

        if (upkeep.HorseState is not null)
        {
            Player.Inventory.SetHorseState(upkeep.HorseState);
            Journey.SetHorseState(upkeep.HorseState);
        }

        var horseLostMessage = string.Empty;
        if (upkeep.MountedTravelLost && Journey.TravelMode == TravelMode.Mounted)
        {
            horseLostMessage = DescribeHorseLoss(upkeep.HorseState, TravelRules);
            Journey.RecalculatePacing(TravelMode.Foot);
        }

        Clock.AdvanceTravelDay();
        var progress = Journey.AdvanceOneDay();
        PursuitState.IncreaseHeat(Math.Max(1, (int)Journey.Preview.RouteProfile.Risk));

        var generationContext = CreateTravelDayGenerationContext(TravelDayPlanGenerator.CurrentVersion);
        Journey.SetCurrentDayPlan(TravelDayPlanGenerator.Generate(generationContext));

        return new TravelDayAdvanceState(startingState, horseLostMessage, progress);
    }

    private TravelJourneyStepResult HandleInterruptedTravelDay(
        TravelDiaryBaselineState startingState,
        string horseLostMessage,
        JourneyEncounterState pendingEncounter,
        JourneyTrailEventState? lastTrailEvent,
        List<string> dayEntries)
    {
        var encounterMessage = PrependHorseLossMessage(horseLostMessage, pendingEncounter.Message);
        dayEntries.Add(encounterMessage);
        dayEntries.Add("I could run, fight, or bribe my way through.");
        AddLogEntry(GameLogEntryKind.Travel, encounterMessage);

        var interruptedSnapshot = Journey!.ToSnapshot(TravelRules);
        AppendTravelDiaryDay(
            interruptedSnapshot,
            startingState,
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
        TravelDiaryBaselineState startingState,
        string horseLostMessage,
        JourneyTrailEventState? lastTrailEvent,
        List<string> dayEntries,
        JourneyProgress progress)
    {
        var destinationTownName = Journey!.Preview.DestinationTownName;
        var heatIncrease = Math.Max(1, (int)Journey.Preview.RouteProfile.Risk);
        var completedSnapshot = CompleteJourneyAtDestination();
        var completionMessage = horseLostMessage.Length == 0
            ? $"You reach {destinationTownName}."
            : $"{horseLostMessage} You reach {destinationTownName}.";
        AddLogEntry(
            GameLogEntryKind.Travel,
            horseLostMessage.Length == 0
                ? $"You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s)."
                : $"{horseLostMessage} You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s).");

        AppendTravelDiaryDay(
            completedSnapshot,
            startingState,
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
            heatIncrease,
            completedSnapshot,
            lastTrailEvent);
    }

    private TravelJourneyStepResult HandleOngoingTravelDay(
        TravelDiaryBaselineState startingState,
        string horseLostMessage,
        JourneyTrailEventState? lastTrailEvent,
        List<string> dayEntries,
        TravelJourneySnapshot journeySnapshot)
    {
        var ongoingMessage = horseLostMessage.Length == 0
            ? $"One trail day passes. {journeySnapshot.RemainingRideDayDistance:0.##} ride-day unit(s) remain and {Journey!.RemainingDays} day(s) remain on the route. {DescribeCanteenCoverage(journeySnapshot)}."
            : $"{horseLostMessage} One trail day passes on foot. {journeySnapshot.RemainingRideDayDistance:0.##} ride-day unit(s) remain and {Journey!.RemainingDays} day(s) remain on the route. {DescribeCanteenCoverage(journeySnapshot)}.";
        AddLogEntry(GameLogEntryKind.Travel, ongoingMessage);

        AppendTravelDiaryDay(
            journeySnapshot,
            startingState,
            trailEvent: lastTrailEvent,
            entries: dayEntries.Count == 0 ? null : dayEntries);
        Journey!.SetCurrentDayPlan(null);

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            ongoingMessage,
            ongoingMessage,
            Math.Max(1, (int)Journey.Preview.RouteProfile.Risk),
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
            AddLogEntry(GameLogEntryKind.Travel, encounterMessage);
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
                    travelDay.StartingState,
                    travelDay.HorseLostMessage,
                    pendingEncounter,
                    lastTrailEvent,
                    dayEntries);
            }

            if (currentEncounter.TrailEvent is not null)
            {
                var trailEventApplication = ApplyTrailEvent(currentEncounter.TrailEvent);
                var trailEventMessage = PrependHorseLossMessage(
                    CombineHorseLossMessage(travelDay.HorseLostMessage, trailEventApplication.HorseLossMessage),
                    currentEncounter.Message);
                dayEntries.Add(trailEventMessage);
                AddLogEntry(GameLogEntryKind.Travel, trailEventMessage);
                lastTrailEvent = currentEncounter.TrailEvent;
            }
            else if (!string.IsNullOrWhiteSpace(currentEncounter.Message))
            {
                dayEntries.Add(currentEncounter.Message);
                AddLogEntry(GameLogEntryKind.Travel, currentEncounter.Message);
            }

            Journey.AdvanceCurrentDayPlan();
        }

        var journeySnapshot = Journey.ToSnapshot(TravelRules);
        return travelDay.Progress.Completed
            ? HandleCompletedTravelDay(travelDay.StartingState, travelDay.HorseLostMessage, lastTrailEvent, dayEntries, travelDay.Progress)
            : HandleOngoingTravelDay(travelDay.StartingState, travelDay.HorseLostMessage, lastTrailEvent, dayEntries, journeySnapshot);
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
        ArchiveCompletedJourney(completedSnapshot);
        Journey = null;

        return new JourneyArrivalAcknowledgementResult(
            true,
            $"You step into {completedSnapshot.DestinationTownName} and put the trail behind you.",
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
        var horseState = Player.Inventory.GetHorseState();
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
            TravelRandomness.Salt);
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
                Player.Inventory.GetQuantity(ItemKind.RevolverAmmo),
                Player.Inventory.GetQuantity(ItemKind.RifleAmmo),
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
                    Player.Inventory.GetHorseState(),
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

                PursuitState.IncreaseHeat(plan.HeatIncrease);

                var runMessage = PrependHorseLossMessage(horseLossMessage, plan.Message);
                AddLogEntry(GameLogEntryKind.Travel, runMessage);
                dayEntries.Add(plan.Message);

                if (!plan.Resolved)
                {
                    Journey.UpdatePendingEncounter(plan.UpdatedEncounter);
                    PersistLatestTravelDiaryDay(startingState, dayEntries, Journey.PendingEncounter);
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
                return resolutionResult;
            }

            case "fight":
            {
                var availableRevolverAmmo = Player.Inventory.GetQuantity(ItemKind.RevolverAmmo);
                var availableRifleAmmo = Player.Inventory.GetQuantity(ItemKind.RifleAmmo);
                var availableAmmo = availableRevolverAmmo + availableRifleAmmo;
                var hasKnife = Player.Inventory.HasItem(ItemKind.Knife);
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

                if (plan.HeatIncrease != 0)
                {
                    PursuitState.IncreaseHeat(plan.HeatIncrease);
                }

                AddLogEntry(GameLogEntryKind.Travel, plan.Message);
                dayEntries.Add(plan.Message);

                if (!plan.Resolved)
                {
                    Journey.UpdatePendingEncounter(plan.UpdatedEncounter);
                    PersistLatestTravelDiaryDay(startingState, dayEntries, Journey.PendingEncounter);
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
                return resolutionResult;
            }

            case "bribe":
            {
                var bribeOffer = bribeAmount ?? TravelRules.EncounterBribeCash;
                if (bribeOffer < 0m)
                {
                    bribeOffer = 0m;
                }

                if (!Player.Wallet.CanAfford(bribeOffer))
                {
                    return JourneyEncounterResolutionResult.Failed($"You need ${bribeOffer:0.00} to bribe your way through.", Journey.Status, Journey.ToSnapshot(TravelRules));
                }

                var availableFood = Player.Inventory.GetQuantity(ItemKind.Food);
                var availableHorseFeed = Player.Inventory.GetQuantity(ItemKind.HorseFeed);
                var availableRevolverAmmo = Player.Inventory.GetQuantity(ItemKind.RevolverAmmo);
                var availableRifleAmmo = Player.Inventory.GetQuantity(ItemKind.RifleAmmo);
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
                    Player.SetWallet(Player.Wallet.Adjust(plan.WalletDelta));
                }

                if (plan.StolenItemKind is not null && plan.StolenItemQuantity > 0)
                {
                    Player.Inventory.RemoveQuantity(plan.StolenItemKind.Value, plan.StolenItemQuantity);
                }

                if (plan.HealthDelta != 0)
                {
                    Player.AdjustHealth(plan.HealthDelta);
                }

                if (plan.HeatIncrease != 0)
                {
                    PursuitState.IncreaseHeat(plan.HeatIncrease);
                }

                AddLogEntry(GameLogEntryKind.Travel, plan.Message);
                dayEntries.Add(plan.Message);

                var retaliated = !plan.Resolved && (plan.HealthDelta < 0 || plan.StolenItemKind is not null || plan.WalletDelta < -bribeOffer);
                if (!plan.Resolved && !retaliated)
                {
                    Journey.UpdatePendingEncounter(plan.UpdatedEncounter);
                    PersistLatestTravelDiaryDay(startingState, dayEntries, Journey.PendingEncounter);
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
                return retaliated ? resolutionResult with { Success = false } : resolutionResult;
            }

            default:
                return JourneyEncounterResolutionResult.Failed("That choice is not available for this encounter.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }
    }

    private JourneyEncounterResolutionResult ContinueCurrentDayAfterEncounterResolution(
        JourneyEncounterState resolvedEncounter,
        TravelDiaryBaselineState startingState,
        List<string> dayEntries,
        TravelDiaryEncounterResolutionState resolution)
    {
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
                AddLogEntry(GameLogEntryKind.Travel, pendingMessage);
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

                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Interrupted, pendingMessage, pendingSnapshot);
            }

            if (currentEncounter.TrailEvent is not null)
            {
                var trailEventApplication = ApplyTrailEvent(currentEncounter.TrailEvent);
                var encounterMessage = PrependHorseLossMessage(trailEventApplication.HorseLossMessage, currentEncounter.Message);
                AddLogEntry(GameLogEntryKind.Travel, encounterMessage);
                dayEntries.Add(encounterMessage);
            }
            else if (!string.IsNullOrWhiteSpace(currentEncounter.Message))
            {
                AddLogEntry(GameLogEntryKind.Travel, currentEncounter.Message);
                dayEntries.Add(currentEncounter.Message);
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
            var completedSnapshot = CompleteJourneyAtDestination();
            currentResources = TravelResourceSnapshotFactory.Capture(Player, PursuitState);
            PersistLatestTravelDiaryDay(
                startingState,
                Array.Empty<string>(),
                resolvedEncounter,
                resolution,
                completedSnapshot,
                currentResources);
            return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Completed, $"You clear the remaining trail and reach {destinationTownName}.", completedSnapshot);
        }

        Journey.ResumeFromEncounter();
        return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, "You push the rider behind you and keep moving.", journeySnapshot);
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
        JourneyProgress Progress);

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
        if (!Player.Wallet.CanAfford(totalPrice))
        {
            return StorePurchaseResult.Failed("Not enough cash.");
        }

        if (!CanPurchaseInventoryItem(offer, quantity, out var inventoryFailureMessage))
        {
            return StorePurchaseResult.Failed(inventoryFailureMessage);
        }

        var nextWallet = Player.Wallet.Spend(totalPrice);
        Player.Inventory.AddItem(offer.ItemKind, quantity);
        Player.SetWallet(nextWallet);

        var quantityLabel = quantity == 1 ? offer.DisplayName : $"{quantity} {offer.DisplayName}";
        AddLogEntry(GameLogEntryKind.Purchase, $"Purchased {quantityLabel} for ${totalPrice:0.00}.");
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

        if (CurrentTown.CheckWantedPosters() == TownSourceCheckOutcome.RepeatNoNewInfo)
        {
            RecordCaseUpdate("You study the wanted posters again, but find nothing new.");
            return ReadWantedPostersResult.Succeeded("You study the wanted posters again, but find nothing new.", sessionChanged: true);
        }

        var warrant = CaseFile.RevealNextPublicWarrant(InvestigationSourceKind.SheriffWarrants);
        var clue = CaseFile.RevealNextPublicClue(publicClue =>
            IsPlayerKnownClue(publicClue)
            && publicClue.SourceKind == InvestigationSourceKind.SheriffWarrants);

        if (warrant is null && clue is null)
        {
            RecordCaseUpdate("You study the wanted posters, but find nothing new.");
            return ReadWantedPostersResult.Succeeded("You study the wanted posters, but find nothing new.", sessionChanged: true);
        }

        if (warrant is not null && clue is not null)
        {
            RecordCaseUpdate($"You study the wanted posters and copy down a wanted notice for {warrant.TargetName}, noting a public lead: {DescribeClueLead(clue.Description)}.");
            return ReadWantedPostersResult.Succeeded("You study the wanted posters and uncover a wanted notice and a public lead.", sessionChanged: true);
        }

        if (warrant is not null)
        {
            RecordCaseUpdate($"You study the wanted posters and copy down a wanted notice for {warrant.TargetName}.");
            return ReadWantedPostersResult.Succeeded($"You study the wanted posters and copy down a wanted notice for {warrant.TargetName}.", sessionChanged: true);
        }

        RecordCaseUpdate($"You study the wanted posters and note a public lead: {DescribeClueLead(clue!.Description)}.");
        return ReadWantedPostersResult.Succeeded("You study the wanted posters and uncover a public lead.", sessionChanged: true);
    }

    public SheriffTurnInResult AssessSheriffTurnIn(SuspectId targetSuspectId, bool isAlive)
    {
        if (IsJourneyModal())
        {
            return SheriffTurnInResult.Rejected(JourneyModalBlockMessage);
        }

        var targetSuspect = CaseFile.Suspects.FirstOrDefault(suspect => suspect.Id.Equals(targetSuspectId));
        if (targetSuspect is null)
        {
            return isAlive
                ? SheriffTurnInResult.WrongPersonAlive("That person is not part of this case.")
                : SheriffTurnInResult.WrongPersonDead("That person is not part of this case.");
        }

        var warrant = CaseFile.KnownWarrants.FirstOrDefault(candidate => MatchesKnownWarrant(candidate, targetSuspect));
        if (warrant is null)
        {
            return isAlive
                ? SheriffTurnInResult.WrongPersonAlive($"There is no wanted notice for {targetSuspect.Name}.", targetSuspect.Name)
                : SheriffTurnInResult.WrongPersonDead($"There is no wanted notice for {targetSuspect.Name}.", targetSuspect.Name);
        }

        if (isAlive)
        {
            return SheriffTurnInResult.AcceptedAlive(
                warrant.TargetName,
                warrant.Terms.Disposition,
                warrant.Terms.BountyAmount,
                $"You bring in {warrant.TargetName} alive under a {DescribeWarrantDisposition(warrant.Terms.Disposition)} warrant.");
        }

        if (warrant.Terms.Disposition == WarrantDisposition.DeadOrAlive)
        {
            return SheriffTurnInResult.AcceptedDead(
                warrant.TargetName,
                warrant.Terms.Disposition,
                warrant.Terms.BountyAmount,
                $"You turn in the body of {warrant.TargetName} under a {DescribeWarrantDisposition(warrant.Terms.Disposition)} warrant.");
        }

        return SheriffTurnInResult.Rejected(
            $"The warrant for {warrant.TargetName} requires an alive turn-in.",
            warrant.TargetName,
            warrant.Terms.Disposition,
            warrant.Terms.BountyAmount);
    }

    public CaseInvestigationResult FollowTelegraphLeads()
    {
        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        var telegraphLeadSource = CurrentTown.GetRequiredSourceDefinition(InvestigationSourceKind.TelegraphLead);

        if (!CurrentTown.IsAvailable(InvestigationSourceKind.TelegraphLead))
        {
            return CaseInvestigationResult.Failed("There is no telegraph office here.");
        }

        if (CurrentTown.CheckSource(telegraphLeadSource) == TownSourceCheckOutcome.RepeatNoNewInfo)
        {
            RecordCaseUpdate("You ask after telegraph leads again, but no new wire has come in.");
            return CaseInvestigationResult.Succeeded("You ask after telegraph leads again, but no new wire has come in.", sessionChanged: true);
        }

        var clue = CaseFile.RevealNextPublicClue(clue => IsPlayerKnownClue(clue) && clue.SourceKind == InvestigationSourceKind.TelegraphLead);

        if (clue is null)
        {
            RecordCaseUpdate("You follow the telegraph leads, but find nothing new.");
            return CaseInvestigationResult.Succeeded("You follow the telegraph leads, but find nothing new.", sessionChanged: true);
        }

        RecordCaseUpdate($"You follow the telegraph leads and uncover a public lead: {DescribeClueLead(clue.Description)}.");
        return CaseInvestigationResult.Succeeded("You follow the telegraph leads and uncover a public lead.", sessionChanged: true);
    }

    public CaseInvestigationResult GatherLocalGossip()
    {
        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        var localGossipSource = CurrentTown.GetRequiredSourceDefinition(InvestigationSourceKind.LocalGossip);

        if (CurrentTown.CheckSource(localGossipSource) == TownSourceCheckOutcome.RepeatNoNewInfo)
        {
            RecordCaseUpdate("You ask around again, but hear nothing new.");
            return CaseInvestigationResult.Succeeded("You ask around again, but hear nothing new.", sessionChanged: true);
        }

        var clue = CaseFile.RevealNextPublicClue(clue => IsPlayerKnownClue(clue) && clue.SourceKind == InvestigationSourceKind.LocalGossip);

        if (clue is null)
        {
            RecordCaseUpdate("You ask around for local gossip, but hear nothing new.");
            return CaseInvestigationResult.Succeeded("You ask around for local gossip, but hear nothing new.", sessionChanged: true);
        }

        RecordCaseUpdate($"You ask around for local gossip and uncover a public lead: {DescribeClueLead(clue.Description)}.");
        return CaseInvestigationResult.Succeeded("You ask around for local gossip and uncover a public lead.", sessionChanged: true);
    }

    public CaseInvestigationResult InspectNoticeBoard()
    {
        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        var noticeBoardSource = CurrentTown.GetRequiredSourceDefinition(InvestigationSourceKind.NoticeBoard);

        if (CurrentTown.CheckSource(noticeBoardSource) == TownSourceCheckOutcome.RepeatNoNewInfo)
        {
            RecordCaseUpdate("You inspect the notice board again, but nothing new has been posted.");
            return CaseInvestigationResult.Succeeded("You inspect the notice board again, but nothing new has been posted.", sessionChanged: true);
        }

        var clue = CaseFile.RevealNextPublicClue(InvestigationSourceKind.NoticeBoard);

        if (clue is null)
        {
            RecordCaseUpdate("You inspect the notice board, but find nothing new.");
            return CaseInvestigationResult.Succeeded("You inspect the notice board, but find nothing new.", sessionChanged: true);
        }

        RecordCaseUpdate($"You inspect the notice board and uncover a civic notice: {DescribeClueLead(clue.Description)}.");
        return CaseInvestigationResult.Succeeded("You inspect the notice board and uncover a civic notice.", sessionChanged: true);
    }

    public CaseInvestigationResult CheckSheriffRecords()
    {
        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        var sheriffRecordsSource = CurrentTown.GetRequiredSourceDefinition(InvestigationSourceKind.LocalRecords);

        if (CurrentTown.CheckSource(sheriffRecordsSource) == TownSourceCheckOutcome.RepeatNoNewInfo)
        {
            RecordCaseUpdate("You check the local records again, but find nothing new.");
            return CaseInvestigationResult.Succeeded("You check the local records again, but find nothing new.", sessionChanged: true);
        }

        var clue = CaseFile.RevealNextPublicClue(clue => IsPlayerKnownClue(clue) && clue.SourceKind == InvestigationSourceKind.LocalRecords);

        if (clue is null)
        {
            RecordCaseUpdate("You check the local records, but find nothing new.");
            return CaseInvestigationResult.Succeeded("You check the local records, but find nothing new.", sessionChanged: true);
        }

        RecordCaseUpdate($"You check the local records and uncover a public lead: {DescribeClueLead(clue.Description)}.");
        return CaseInvestigationResult.Succeeded("You check the local records and uncover a public lead.", sessionChanged: true);
    }

    public void RecordCaseUpdate(string message, bool advanceClock = true)
    {
        if (advanceClock)
        {
            Clock.Advance();
        }

        AddLogEntry(GameLogEntryKind.CaseUpdate, message);
    }

    public void CompleteCase(string message)
    {
        Status = GameStatus.Completed;
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

    private static string DescribeWarrantDisposition(WarrantDisposition disposition)
        => disposition switch
        {
            WarrantDisposition.AliveOnly => "alive-only",
            WarrantDisposition.DeadOrAlive => "dead-or-alive",
            _ => $"disposition {disposition}"
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

    private void AddLogEntry(GameLogEntryKind kind, string message)
    {
        _logEntries.Add(new GameLogEntry(kind, message, Clock.Day, Clock.Turn));
    }

    private static string DescribeClueLead(string description)
        => description.Trim().TrimEnd('.', '!', '?');

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

            if (Player.Inventory.HasItem(ItemKind.Horse))
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

        if (!IsStackableItemKind(offer.ItemKind) && Player.Inventory.HasItem(offer.ItemKind))
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

        var availableRevolverAmmo = Player.Inventory.GetQuantity(ItemKind.RevolverAmmo);
        var availableRifleAmmo = Player.Inventory.GetQuantity(ItemKind.RifleAmmo);
        var availableAmmo = availableRevolverAmmo + availableRifleAmmo;
        if (availableAmmo <= 0)
        {
            return 0;
        }

        var bulletsToSpend = Math.Min(Math.Clamp(requestedBullets, 1, 6), availableAmmo);
        var spent = 0;

        while (spent < bulletsToSpend && Player.Inventory.GetQuantity(ItemKind.RevolverAmmo) > 0)
        {
            Player.Inventory.RemoveQuantity(ItemKind.RevolverAmmo, 1);
            spent++;
        }

        while (spent < bulletsToSpend && Player.Inventory.GetQuantity(ItemKind.RifleAmmo) > 0)
        {
            Player.Inventory.RemoveQuantity(ItemKind.RifleAmmo, 1);
            spent++;
        }

        return spent;
    }

}
