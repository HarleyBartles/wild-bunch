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
    private readonly List<GameLogEntry> _logEntries = [];
    private readonly List<TravelDiaryDayState> _travelDiaryDays = [];

    private GameSession(
        GameSessionId id,
        Player player,
        DomainWorld world,
        CaseFile caseFile,
        PursuitState pursuitState,
        GameClock clock,
        GameStatus status,
        TravelJourney? journey,
        TravelDifficulty travelDifficulty)
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

    public TravelRulesProfile TravelRules => TravelRulesProfile.For(TravelDifficulty);

    public IReadOnlyList<GameLogEntry> LogEntries => _logEntries;

    public IReadOnlyList<TravelDiaryDayState> TravelDiaryDays => _travelDiaryDays;

    public static GameSession StartNew(string playerName, DomainWorld world, CaseFile caseFile, TownId? startingTownId = null)
        => StartNew(playerName, world, caseFile, startingTownId, wallet: null, inventory: null, travelDifficulty: TravelDifficulty.Normal);

    public static GameSession StartNew(
        string playerName,
        DomainWorld world,
        CaseFile caseFile,
        TownId? startingTownId,
        WildBunch.Domain.Economy.Wallet? wallet,
        DomainInventory? inventory,
        TravelDifficulty travelDifficulty = TravelDifficulty.Normal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(caseFile);

        var resolvedTownId = startingTownId ?? world.Towns.First().Id;
        var startingTown = world.GetTown(resolvedTownId);
        var player = new Player(
            playerName,
            startingTown.Id,
            health: 100,
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
            travelDifficulty);

        session.AddLogEntry(GameLogEntryKind.Opening, $"The hunt begins in {startingTown.Name}.");
        return session;
    }

    public TravelJourneyStepResult StartJourney(TravelPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);

        if (Journey is not null)
        {
            return TravelJourneyStepResult.Failed("You are already on the trail.");
        }

        Journey = TravelJourney.Start(preview, BuildJourneyOpeningNarration(preview));
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
    {
        return AdvanceJourneyDayDeterministic();

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

        var startingTravelMode = Journey.TravelMode;
        var startingRideDayDistance = Journey.RemainingRideDayDistance;
        var startingDaysRemaining = Journey.RemainingDays;
        var startingHorseState = Player.Inventory.GetHorseState();
        var startingWallet = Player.Wallet.Cash;
        var startingFood = Player.Inventory.GetQuantity(ItemKind.Food);
        var startingHorseFeed = Player.Inventory.GetQuantity(ItemKind.HorseFeed);
        var startingCanteenCharges = Player.Inventory.GetCanteenState()?.Charges ?? 0;
        var startingHealth = Player.Health;
        var startingHorseHunger = startingHorseState?.Hunger ?? 0;
        var startingHorseThirst = startingHorseState?.Thirst ?? 0;
        var startingHorseExhaustion = startingHorseState?.Exhaustion ?? 0;
        var startingDelayDays = Journey.DelayDays;
        var startingHeat = PursuitState.Heat;

        var capabilities = new InventoryCapabilityResolver().Resolve(Player.Inventory, TravelRules);
        if (Journey.TravelMode == TravelMode.Mounted && !capabilities.MountedTravelAvailable)
        {
            Journey.RecalculatePacing(TravelMode.Foot);
        }

        if (Player.Inventory.GetQuantity(ItemKind.Food) < 1)
        {
            Journey.MarkFailed();
            var message = "The trail grinds to a halt when your food runs out.";
            AddLogEntry(GameLogEntryKind.Travel, message);
            var failedSnapshot = Journey.ToSnapshot(TravelRules);
            AppendTravelDiaryDay(CreateTravelDiaryDay(
                failedSnapshot,
                startingTravelMode,
                startingRideDayDistance,
                startingDaysRemaining,
                startingHorseState,
                startingWallet,
                startingFood,
                startingHorseFeed,
                startingCanteenCharges,
                startingHealth,
                startingHorseHunger,
                startingHorseThirst,
                startingHorseExhaustion,
                startingDelayDays,
                startingHeat));
            Journey = null;
            return new TravelJourneyStepResult(false, JourneyStatus.Failed, message, message, 0, failedSnapshot);
        }

        Player.Inventory.RemoveQuantity(ItemKind.Food, 1);
        Journey.ConsumeFood();

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

        Clock.Advance();
        var progress = Journey.AdvanceOneDay();
        PursuitState.IncreaseHeat(Math.Max(1, (int)Journey.Preview.RouteProfile.Risk));

        if (!progress.Completed)
        {
            var trailEvent = Journey.TryCreateTrailEvent(TravelRules);
            if (trailEvent is not null)
            {
                var trailEventApplication = ApplyTrailEvent(trailEvent);
                var eventSnapshot = Journey.ToSnapshot(TravelRules);
                var trailEventMessage = PrependHorseLossMessage(
                    CombineHorseLossMessage(horseLostMessage, trailEventApplication.HorseLossMessage),
                    trailEvent.Message);
                AddLogEntry(GameLogEntryKind.Travel, trailEventMessage);
                AppendTravelDiaryDay(CreateTravelDiaryDay(
                    eventSnapshot,
                    startingTravelMode,
                    startingRideDayDistance,
                    startingDaysRemaining,
                    startingHorseState,
                    startingWallet,
                    startingFood,
                    startingHorseFeed,
                    startingCanteenCharges,
                    startingHealth,
                    startingHorseHunger,
                    startingHorseThirst,
                    startingHorseExhaustion,
                    startingDelayDays,
                    startingHeat,
                    trailEvent));

                return new TravelJourneyStepResult(
                    true,
                    JourneyStatus.Active,
                    trailEventMessage,
                    trailEventMessage,
                    Math.Max(1, (int)Journey.Preview.RouteProfile.Risk) + trailEvent.HeatIncrease,
                    eventSnapshot,
                    trailEvent);
            }
        }

        var encounter = Journey.TryCreateEncounter(TravelRules);
        if (encounter is not null)
        {
            Journey.MarkInterrupted(encounter);
            var interruptedSnapshot = Journey.ToSnapshot(TravelRules);
            var encounterMessage = PrependHorseLossMessage(horseLostMessage, encounter.Message);
            AddLogEntry(GameLogEntryKind.Travel, encounterMessage);
            AppendTravelDiaryDay(CreateTravelDiaryDay(
                interruptedSnapshot,
                startingTravelMode,
                startingRideDayDistance,
                startingDaysRemaining,
                startingHorseState,
                startingWallet,
                startingFood,
                startingHorseFeed,
                startingCanteenCharges,
                startingHealth,
                startingHorseHunger,
                startingHorseThirst,
                startingHorseExhaustion,
                startingDelayDays,
                startingHeat,
                pendingEncounter: encounter));

            return new TravelJourneyStepResult(
                false,
                JourneyStatus.Interrupted,
                horseLostMessage.Length == 0
                    ? "Your journey is interrupted by a trail encounter."
                    : $"Your journey is interrupted by a trail encounter. {horseLostMessage}",
                encounterMessage,
                0,
                interruptedSnapshot);
        }

        if (progress.Completed)
        {
            var destinationTownId = Journey.Preview.DestinationTownId;
            var destinationTownName = Journey.Preview.DestinationTownName;
            var heatIncrease = Math.Max(1, (int)Journey.Preview.RouteProfile.Risk);
            Journey.MarkCompleted();
            Player.TravelTo(destinationTownId);
            var canteenState = Player.Inventory.GetCanteenState();
            if (canteenState is not null && canteenState.Charges < canteenState.Capacity)
            {
                var refilledCanteen = CanteenState.Full(canteenState.Capacity);
                Player.Inventory.SetCanteenState(refilledCanteen);
                Journey.SetCanteenCharges(refilledCanteen.Charges);
            }

            var completedSnapshot = Journey.ToSnapshot(TravelRules);
            var completionMessage = horseLostMessage.Length == 0
                ? $"You reach {destinationTownName}."
                : $"{horseLostMessage} You reach {destinationTownName}.";
            AddLogEntry(
                GameLogEntryKind.Travel,
                horseLostMessage.Length == 0
                    ? $"You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s)."
                    : $"{horseLostMessage} You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s).");
            AppendTravelDiaryDay(CreateTravelDiaryDay(
                completedSnapshot,
                startingTravelMode,
                startingRideDayDistance,
                startingDaysRemaining,
                startingHorseState,
                startingWallet,
                startingFood,
                startingHorseFeed,
                startingCanteenCharges,
                startingHealth,
                startingHorseHunger,
                startingHorseThirst,
                startingHorseExhaustion,
                startingDelayDays,
                startingHeat));
            Journey = null;

            return new TravelJourneyStepResult(
                true,
                JourneyStatus.Completed,
                completionMessage,
                horseLostMessage.Length == 0
                    ? $"You reach {destinationTownName} after {progress.RideDayDistanceTravelled:0.##} ride-day unit(s)."
                    : $"{horseLostMessage} You reach {destinationTownName} after {progress.RideDayDistanceTravelled:0.##} ride-day unit(s).",
                heatIncrease,
                completedSnapshot);
        }

        var ongoingSnapshot = Journey.ToSnapshot(TravelRules);
        var ongoingMessage = horseLostMessage.Length == 0
            ? $"One trail day passes. {ongoingSnapshot.RemainingRideDayDistance:0.##} ride-day unit(s) remain and {Journey.RemainingDays} day(s) remain on the route. {DescribeCanteenCoverage(ongoingSnapshot)}."
            : $"{horseLostMessage} One trail day passes on foot. {ongoingSnapshot.RemainingRideDayDistance:0.##} ride-day unit(s) remain and {Journey.RemainingDays} day(s) remain on the route. {DescribeCanteenCoverage(ongoingSnapshot)}.";
        AddLogEntry(GameLogEntryKind.Travel, ongoingMessage);
        AppendTravelDiaryDay(CreateTravelDiaryDay(
            ongoingSnapshot,
            startingTravelMode,
            startingRideDayDistance,
            startingDaysRemaining,
            startingHorseState,
            startingWallet,
            startingFood,
            startingHorseFeed,
            startingCanteenCharges,
            startingHealth,
            startingHorseHunger,
            startingHorseThirst,
            startingHorseExhaustion,
            startingDelayDays,
            startingHeat));

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            ongoingMessage,
            ongoingMessage,
            Math.Max(1, (int)Journey.Preview.RouteProfile.Risk),
            ongoingSnapshot);
    }

    private static string DescribeHorseLoss(HorseTravelState? horseState, TravelRulesProfile travelRulesProfile)
    {
        if (horseState is null)
        {
            return "Your horse can no longer carry you.";
        }

        if (horseState.IsDeadFor(travelRulesProfile))
        {
            return "Your horse dies on the trail.";
        }

        if (horseState.IsLameFor(travelRulesProfile))
        {
            return "Your horse goes lame and can no longer carry you.";
        }

        return "Your horse can no longer carry you.";
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

    private void UpdateOrAppendEncounterResolutionDiary(
        TravelJourneySnapshot journeySnapshot,
        JourneyEncounterState encounter,
        string resolvedChoiceId,
        string resolvedChoiceLabel,
        int healthDelta,
        decimal walletDelta,
        int ammoSpent,
        int heatIncrease,
        int horseExhaustionDelta,
        TravelMode startingTravelMode,
        decimal startingRideDayDistance,
        int startingDaysRemaining,
        HorseTravelState? startingHorseState,
        decimal startingWallet,
        int startingFood,
        int startingHorseFeed,
        int startingCanteenCharges,
        int startingHealth,
        int startingHorseHunger,
        int startingHorseThirst,
        int startingHorseExhaustion,
        int startingDelayDays,
        int startingHeat,
        bool continuedOnFoot)
    {
        var resolution = new TravelDiaryEncounterResolutionState(
            resolvedChoiceId,
            resolvedChoiceLabel,
            healthDelta,
            walletDelta,
            ammoSpent,
            heatIncrease,
            horseExhaustionDelta,
            continuedOnFoot);

        if (UpdateLatestTravelDiaryDay(day => day with
        {
            EndingTravelMode = journeySnapshot.TravelMode,
            RemainingRideDayDistance = journeySnapshot.RemainingRideDayDistance,
            RemainingDays = journeySnapshot.RemainingDays,
            HorseStateAfter = journeySnapshot.HorseState,
            Status = journeySnapshot.Status,
            EncounterResolution = resolution,
            HealthDelta = day.HealthDelta + healthDelta,
            WalletDelta = day.WalletDelta + walletDelta,
            AmmoSpent = day.AmmoSpent + ammoSpent,
            HeatIncrease = day.HeatIncrease + heatIncrease,
            HorseExhaustionDelta = day.HorseExhaustionDelta + horseExhaustionDelta
        }))
        {
            return;
        }

        AppendTravelDiaryDay(CreateTravelDiaryDay(
            journeySnapshot,
            startingTravelMode,
            startingRideDayDistance,
            startingDaysRemaining,
            startingHorseState,
            startingWallet,
            startingFood,
            startingHorseFeed,
            startingCanteenCharges,
            startingHealth,
            startingHorseHunger,
            startingHorseThirst,
            startingHorseExhaustion,
            startingDelayDays,
            startingHeat,
            pendingEncounter: encounter,
            encounterResolution: resolution,
            ammoSpent: ammoSpent));
    }

    private TravelDiaryDayState CreateTravelDiaryDay(
        TravelJourneySnapshot journeySnapshot,
        TravelMode startingTravelMode,
        decimal startingRideDayDistance,
        int startingDaysRemaining,
        HorseTravelState? startingHorseState,
        decimal startingWallet,
        int startingFood,
        int startingHorseFeed,
        int startingCanteenCharges,
        int startingHealth,
        int startingHorseHunger,
        int startingHorseThirst,
        int startingHorseExhaustion,
        int startingDelayDays,
        int startingHeat,
        JourneyTrailEventState? trailEvent = null,
        JourneyEncounterState? pendingEncounter = null,
        TravelDiaryEncounterResolutionState? encounterResolution = null,
        int ammoSpent = 0,
        IReadOnlyList<string>? entries = null)
    {
        var horseStateAfter = journeySnapshot.HorseState;
        var currentHorseState = Player.Inventory.GetHorseState();
        var currentFood = Player.Inventory.GetQuantity(ItemKind.Food);
        var currentHorseFeed = Player.Inventory.GetQuantity(ItemKind.HorseFeed);
        var currentCanteenCharges = Player.Inventory.GetCanteenState()?.Charges ?? 0;
        var openingNarration = startingDaysRemaining == journeySnapshot.ExpectedDays ? Journey?.OpeningNarration : null;
        var extraEntriesProvided = entries is not null && entries.Count > 0;
        var effectiveTrailEvent = extraEntriesProvided ? null : trailEvent;
        var effectivePendingEncounter = extraEntriesProvided ? null : pendingEncounter ?? journeySnapshot.PendingEncounter;
        var effectiveEncounterResolution = extraEntriesProvided ? null : encounterResolution;
        var journeyBeat = BuildJourneyBeat(journeySnapshot, effectiveTrailEvent, effectivePendingEncounter, effectiveEncounterResolution);
        var resourceBeat = BuildResourceBeat(
            journeySnapshot,
            currentFood,
            currentHorseFeed,
            startingCanteenCharges,
            currentCanteenCharges,
            effectiveTrailEvent,
            effectivePendingEncounter,
            effectiveEncounterResolution);

        var diaryEntries = BuildDefaultDiaryEntries(journeySnapshot, openingNarration, journeyBeat, resourceBeat, effectiveTrailEvent, effectivePendingEncounter, effectiveEncounterResolution);
        if (startingTravelMode == TravelMode.Mounted && journeySnapshot.TravelMode == TravelMode.Foot)
        {
            diaryEntries = diaryEntries.Append("I had to finish the trail on foot after the horse went lame.").ToArray();
        }
        if (entries is not null && entries.Count > 0)
        {
            diaryEntries = diaryEntries.Concat(entries).ToArray();
        }

        return new TravelDiaryDayState(
            journeySnapshot.DaysTravelled,
            journeySnapshot.OriginTownName,
            journeySnapshot.DestinationTownName,
            startingTravelMode,
            journeySnapshot.TravelMode,
            journeySnapshot.Status,
            startingRideDayDistance,
            journeySnapshot.RemainingRideDayDistance,
            startingDaysRemaining,
            journeySnapshot.RemainingDays,
            startingHorseState,
            horseStateAfter,
            trailEvent,
            pendingEncounter ?? journeySnapshot.PendingEncounter,
            encounterResolution,
            openingNarration,
            journeyBeat,
            resourceBeat,
            diaryEntries,
            Player.Health - startingHealth,
            Player.Wallet.Cash - startingWallet,
            Player.Inventory.GetQuantity(ItemKind.Food) - startingFood,
            Player.Inventory.GetQuantity(ItemKind.HorseFeed) - startingHorseFeed,
            currentCanteenCharges - startingCanteenCharges,
            ammoSpent,
            (currentHorseState?.Hunger ?? 0) - (startingHorseState?.Hunger ?? 0),
            (currentHorseState?.Thirst ?? 0) - (startingHorseState?.Thirst ?? 0),
            (currentHorseState?.Exhaustion ?? 0) - (startingHorseState?.Exhaustion ?? 0),
            journeySnapshot.DelayDays - startingDelayDays,
            PursuitState.Heat - startingHeat,
            journeySnapshot.Warnings);
    }

    private static IReadOnlyList<string> BuildDefaultDiaryEntries(
        TravelJourneySnapshot journeySnapshot,
        string? openingNarration,
        string? journeyBeat,
        string? resourceBeat,
        JourneyTrailEventState? trailEvent,
        JourneyEncounterState? pendingEncounter,
        TravelDiaryEncounterResolutionState? encounterResolution)
    {
        var entries = new List<string>();

        if (!string.IsNullOrWhiteSpace(openingNarration))
        {
            entries.Add(openingNarration!);
        }

        if (!string.IsNullOrWhiteSpace(journeyBeat))
        {
            entries.Add(journeyBeat!);
        }

        if (trailEvent is not null)
        {
            entries.Add(trailEvent.Message);
        }

        if (pendingEncounter is not null && encounterResolution is null)
        {
            entries.Add(pendingEncounter.Message);
        }

        if (encounterResolution is not null)
        {
            entries.Add(encounterResolution.ChoiceId switch
            {
                "run" => "I decided to run for it.",
                "fight" => "I decided to stand and fight.",
                "bribe" => "I decided to bribe my way through.",
                _ => $"I chose to {encounterResolution.ChoiceLabel.ToLowerInvariant()}."
            });
        }

        if (!string.IsNullOrWhiteSpace(resourceBeat))
        {
            entries.Add(resourceBeat!);
        }

        entries.Add(journeySnapshot.Status switch
        {
            JourneyStatus.Active => "The trail keeps stretching ahead.",
            JourneyStatus.Interrupted => "I am stuck until I decide how to answer the rider.",
            JourneyStatus.Completed => $"I made it to {journeySnapshot.DestinationTownName}.",
            JourneyStatus.Failed => "The trail gave out before I could finish it.",
            _ => "I am still on the trail."
        });

        return entries;
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

        var startingTravelMode = Journey.TravelMode;
        var startingRideDayDistance = Journey.RemainingRideDayDistance;
        var startingDaysRemaining = Journey.RemainingDays;
        var startingHorseState = Player.Inventory.GetHorseState();
        var startingWallet = Player.Wallet.Cash;
        var startingFood = Player.Inventory.GetQuantity(ItemKind.Food);
        var startingHorseFeed = Player.Inventory.GetQuantity(ItemKind.HorseFeed);
        var startingCanteenCharges = Player.Inventory.GetCanteenState()?.Charges ?? 0;
        var startingHealth = Player.Health;
        var startingHorseHunger = startingHorseState?.Hunger ?? 0;
        var startingHorseThirst = startingHorseState?.Thirst ?? 0;
        var startingHorseExhaustion = startingHorseState?.Exhaustion ?? 0;
        var startingDelayDays = Journey.DelayDays;
        var startingHeat = PursuitState.Heat;

        var capabilities = new InventoryCapabilityResolver().Resolve(Player.Inventory, TravelRules);
        if (Journey.TravelMode == TravelMode.Mounted && !capabilities.MountedTravelAvailable)
        {
            Journey.RecalculatePacing(TravelMode.Foot);
        }

        if (Player.Inventory.GetQuantity(ItemKind.Food) < 1)
        {
            Journey.MarkFailed();
            var message = "The trail grinds to a halt when your food runs out.";
            AddLogEntry(GameLogEntryKind.Travel, message);
            var failedSnapshot = Journey.ToSnapshot(TravelRules);
            AppendTravelDiaryDay(CreateTravelDiaryDay(
                failedSnapshot,
                startingTravelMode,
                startingRideDayDistance,
                startingDaysRemaining,
                startingHorseState,
                startingWallet,
                startingFood,
                startingHorseFeed,
                startingCanteenCharges,
                startingHealth,
                startingHorseHunger,
                startingHorseThirst,
                startingHorseExhaustion,
                startingDelayDays,
                startingHeat));
            Journey = null;
            return new TravelJourneyStepResult(false, JourneyStatus.Failed, message, message, 0, failedSnapshot);
        }

        Player.Inventory.RemoveQuantity(ItemKind.Food, 1);
        Journey.ConsumeFood();

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

        Clock.Advance();
        var progress = Journey.AdvanceOneDay();
        PursuitState.IncreaseHeat(Math.Max(1, (int)Journey.Preview.RouteProfile.Risk));

        var plan = TravelDayPlanGenerator.Generate(Journey, TravelRules);
        Journey.SetCurrentDayPlan(plan);

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
                var encounterMessage = PrependHorseLossMessage(horseLostMessage, pendingEncounter.Message);
                dayEntries.Add(encounterMessage);
                dayEntries.Add("I can run, fight, or bribe my way through.");
                AddLogEntry(GameLogEntryKind.Travel, encounterMessage);

                var interruptedSnapshot = Journey.ToSnapshot(TravelRules);
                AppendTravelDiaryDay(CreateTravelDiaryDay(
                    interruptedSnapshot,
                    startingTravelMode,
                    startingRideDayDistance,
                    startingDaysRemaining,
                    startingHorseState,
                    startingWallet,
                    startingFood,
                    startingHorseFeed,
                    startingCanteenCharges,
                    startingHealth,
                    startingHorseHunger,
                    startingHorseThirst,
                    startingHorseExhaustion,
                    startingDelayDays,
                    startingHeat,
                    pendingEncounter: pendingEncounter,
                    entries: dayEntries));

                return new TravelJourneyStepResult(
                    false,
                    JourneyStatus.Interrupted,
                    horseLostMessage.Length == 0
                        ? "Your journey is interrupted by a trail encounter."
                        : $"Your journey is interrupted by a trail encounter. {horseLostMessage}",
                    encounterMessage,
                    0,
                    interruptedSnapshot,
                    lastTrailEvent);
            }

            if (currentEncounter.TrailEvent is not null)
            {
                var trailEventApplication = ApplyTrailEvent(currentEncounter.TrailEvent);
                var trailEventMessage = PrependHorseLossMessage(
                    CombineHorseLossMessage(horseLostMessage, trailEventApplication.HorseLossMessage),
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
        var diaryEntries = dayEntries.Count == 0 ? null : dayEntries;

        if (progress.Completed)
        {
            var destinationTownId = Journey.Preview.DestinationTownId;
            var destinationTownName = Journey.Preview.DestinationTownName;
            var heatIncrease = Math.Max(1, (int)Journey.Preview.RouteProfile.Risk);
            Journey.MarkCompleted();
            Player.TravelTo(destinationTownId);
            var canteenState = Player.Inventory.GetCanteenState();
            if (canteenState is not null && canteenState.Charges < canteenState.Capacity)
            {
                var refilledCanteen = CanteenState.Full(canteenState.Capacity);
                Player.Inventory.SetCanteenState(refilledCanteen);
                Journey.SetCanteenCharges(refilledCanteen.Charges);
            }

            var completedSnapshot = Journey.ToSnapshot(TravelRules);
            var completionMessage = horseLostMessage.Length == 0
                ? $"You reach {destinationTownName}."
                : $"{horseLostMessage} You reach {destinationTownName}.";
            AddLogEntry(
                GameLogEntryKind.Travel,
                horseLostMessage.Length == 0
                    ? $"You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s)."
                    : $"{horseLostMessage} You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s).");
            AppendTravelDiaryDay(CreateTravelDiaryDay(
                completedSnapshot,
                startingTravelMode,
                startingRideDayDistance,
                startingDaysRemaining,
                startingHorseState,
                startingWallet,
                startingFood,
                startingHorseFeed,
                startingCanteenCharges,
                startingHealth,
                startingHorseHunger,
                startingHorseThirst,
                startingHorseExhaustion,
                startingDelayDays,
                startingHeat,
                trailEvent: lastTrailEvent,
                entries: diaryEntries));
            Journey.SetCurrentDayPlan(null);
            Journey = null;

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

        var ongoingMessage = horseLostMessage.Length == 0
            ? $"One trail day passes. {journeySnapshot.RemainingRideDayDistance:0.##} ride-day unit(s) remain and {Journey.RemainingDays} day(s) remain on the route. {DescribeCanteenCoverage(journeySnapshot)}."
            : $"{horseLostMessage} One trail day passes on foot. {journeySnapshot.RemainingRideDayDistance:0.##} ride-day unit(s) remain and {Journey.RemainingDays} day(s) remain on the route. {DescribeCanteenCoverage(journeySnapshot)}.";
        AddLogEntry(GameLogEntryKind.Travel, ongoingMessage);
        AppendTravelDiaryDay(CreateTravelDiaryDay(
            journeySnapshot,
            startingTravelMode,
            startingRideDayDistance,
            startingDaysRemaining,
            startingHorseState,
            startingWallet,
            startingFood,
            startingHorseFeed,
            startingCanteenCharges,
            startingHealth,
            startingHorseHunger,
            startingHorseThirst,
            startingHorseExhaustion,
            startingDelayDays,
            startingHeat,
            trailEvent: lastTrailEvent,
            entries: diaryEntries));
        Journey.SetCurrentDayPlan(null);

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            ongoingMessage,
            ongoingMessage,
            Math.Max(1, (int)Journey.Preview.RouteProfile.Risk),
            journeySnapshot,
            lastTrailEvent);
    }

    private JourneyEncounterResolutionResult ResolveJourneyEncounterDeterministic(string choiceId)
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
        if (!encounter.Choices.Any(choice => string.Equals(choice.Id, choiceId, StringComparison.OrdinalIgnoreCase)))
        {
            return JourneyEncounterResolutionResult.Failed("That is not a lawful way to answer this encounter.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }

        var startingTravelMode = Journey.TravelMode;
        var startingRideDayDistance = Journey.RemainingRideDayDistance;
        var startingDaysRemaining = Journey.RemainingDays;
        var startingHorseState = Player.Inventory.GetHorseState();
        var startingWallet = Player.Wallet.Cash;
        var startingFood = Player.Inventory.GetQuantity(ItemKind.Food);
        var startingHorseFeed = Player.Inventory.GetQuantity(ItemKind.HorseFeed);
        var startingCanteenCharges = Player.Inventory.GetCanteenState()?.Charges ?? 0;
        var startingHealth = Player.Health;
        var startingHorseHunger = startingHorseState?.Hunger ?? 0;
        var startingHorseThirst = startingHorseState?.Thirst ?? 0;
        var startingHorseExhaustion = startingHorseState?.Exhaustion ?? 0;
        var startingDelayDays = Journey.DelayDays;
        var startingHeat = PursuitState.Heat;
        var resolvedChoiceId = choiceId.Trim().ToLowerInvariant();
        var resolvedChoiceLabel = encounter.Choices.First(choice => string.Equals(choice.Id, resolvedChoiceId, StringComparison.OrdinalIgnoreCase)).Label;
        var dayEntries = new List<string>();

        switch (resolvedChoiceId)
        {
            case "run":
            {
                var isMountedEscape = Journey.TravelMode == TravelMode.Mounted;
                var heatIncrease = isMountedEscape ? TravelRules.EncounterRunMountedHeatIncrease : TravelRules.EncounterRunFootHeatIncrease;
                PursuitState.IncreaseHeat(heatIncrease);

                var horseLossMessage = isMountedEscape
                    ? ApplyEncounterHorsePressure(TravelRules.EncounterRunMountedHorseExhaustion)
                    : string.Empty;

                if (!isMountedEscape)
                {
                    Player.AdjustHealth(-TravelRules.EncounterRunFootHealthLoss);
                }

                Journey.ResumeFromEncounter();
                var runMessage = isMountedEscape
                    ? "You spur the horse and pull away."
                    : $"You run on foot and pull away, but it costs you {TravelRules.EncounterRunFootHealthLoss} health.";
                if (isMountedEscape && horseLossMessage.Length != 0)
                {
                    runMessage = $"{runMessage} You continue on foot.";
                }

                runMessage = PrependHorseLossMessage(horseLossMessage, runMessage);
                AddLogEntry(GameLogEntryKind.Travel, runMessage);
                dayEntries.Add("I decided to run for it.");
                dayEntries.Add(isMountedEscape ? "I got away on the horse." : "I got away on foot.");
                var resolution = new TravelDiaryEncounterResolutionState(
                    resolvedChoiceId,
                    resolvedChoiceLabel,
                    isMountedEscape ? 0 : -TravelRules.EncounterRunFootHealthLoss,
                    0m,
                    0,
                    heatIncrease,
                    isMountedEscape ? TravelRules.EncounterRunMountedHorseExhaustion : 0,
                    Journey.TravelMode == TravelMode.Foot);
                Journey.RecordCurrentDayEncounterResolution(resolution);
                Journey.AdvanceCurrentDayPlan();
                var continueResult = ContinueCurrentDayAfterEncounterResolution(
                    startingTravelMode,
                    startingRideDayDistance,
                    startingDaysRemaining,
                    startingHorseState,
                    startingWallet,
                    startingFood,
                    startingHorseFeed,
                    startingCanteenCharges,
                    startingHealth,
                    startingHorseHunger,
                    startingHorseThirst,
                    startingHorseExhaustion,
                    startingDelayDays,
                    startingHeat,
                    dayEntries,
                    resolution);
                return continueResult;
            }

            case "fight":
            {
                var hasFirearmAmmo = Player.Inventory.GetQuantity(ItemKind.RevolverAmmo) > 0 || Player.Inventory.GetQuantity(ItemKind.RifleAmmo) > 0;
                var hasKnife = Player.Inventory.HasItem(ItemKind.Knife);
                if (!hasFirearmAmmo && !hasKnife)
                {
                    return JourneyEncounterResolutionResult.Failed("You need a knife or firearm ammo to stand and fight.", Journey.Status, Journey.ToSnapshot(TravelRules));
                }

                var usedFirearm = hasFirearmAmmo && TrySpendFirearmAmmo();
                var fightHealthLoss = usedFirearm
                    ? TravelRules.EncounterFightAmmoHealthLoss
                    : TravelRules.EncounterFightUnarmedHealthLoss;

                Player.AdjustHealth(-fightHealthLoss);
                PursuitState.IncreaseHeat(TravelRules.EncounterFightHeatIncrease);
                Journey.ResumeFromEncounter();
                var fightMessage = usedFirearm
                    ? $"You spend a round and take {fightHealthLoss} health damage before forcing the rider off the trail."
                    : $"You fight with your knife and take {fightHealthLoss} health damage before forcing the rider off the trail.";
                AddLogEntry(GameLogEntryKind.Travel, fightMessage);
                dayEntries.Add(fightMessage);
                var resolution = new TravelDiaryEncounterResolutionState(
                    resolvedChoiceId,
                    resolvedChoiceLabel,
                    -fightHealthLoss,
                    0m,
                    usedFirearm ? 1 : 0,
                    TravelRules.EncounterFightHeatIncrease,
                    0,
                    Journey.TravelMode == TravelMode.Foot);
                Journey.RecordCurrentDayEncounterResolution(resolution);
                Journey.AdvanceCurrentDayPlan();
                return ContinueCurrentDayAfterEncounterResolution(
                    startingTravelMode,
                    startingRideDayDistance,
                    startingDaysRemaining,
                    startingHorseState,
                    startingWallet,
                    startingFood,
                    startingHorseFeed,
                    startingCanteenCharges,
                    startingHealth,
                    startingHorseHunger,
                    startingHorseThirst,
                    startingHorseExhaustion,
                    startingDelayDays,
                    startingHeat,
                    dayEntries,
                    resolution);
            }

            case "bribe":
            {
                var bribeAmount = TravelRules.EncounterBribeCash;
                if (!Player.Wallet.CanAfford(bribeAmount))
                {
                    return JourneyEncounterResolutionResult.Failed($"You need ${bribeAmount:0.00} to bribe your way through.", Journey.Status, Journey.ToSnapshot(TravelRules));
                }

                Player.SetWallet(Player.Wallet.Spend(bribeAmount));
                Journey.ResumeFromEncounter();
                var bribeMessage = $"You bribe the rider with ${bribeAmount:0.00} and continue on.";
                AddLogEntry(GameLogEntryKind.Travel, bribeMessage);
                dayEntries.Add(bribeMessage);
                var resolution = new TravelDiaryEncounterResolutionState(
                    resolvedChoiceId,
                    resolvedChoiceLabel,
                    0,
                    -bribeAmount,
                    0,
                    0,
                    0,
                    Journey.TravelMode == TravelMode.Foot);
                Journey.RecordCurrentDayEncounterResolution(resolution);
                Journey.AdvanceCurrentDayPlan();
                return ContinueCurrentDayAfterEncounterResolution(
                    startingTravelMode,
                    startingRideDayDistance,
                    startingDaysRemaining,
                    startingHorseState,
                    startingWallet,
                    startingFood,
                    startingHorseFeed,
                    startingCanteenCharges,
                    startingHealth,
                    startingHorseHunger,
                    startingHorseThirst,
                    startingHorseExhaustion,
                    startingDelayDays,
                    startingHeat,
                    dayEntries,
                    resolution);
            }

            default:
                return JourneyEncounterResolutionResult.Failed("That choice is not available for this encounter.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }
    }

    private JourneyEncounterResolutionResult ContinueCurrentDayAfterEncounterResolution(
        TravelMode startingTravelMode,
        decimal startingRideDayDistance,
        int startingDaysRemaining,
        HorseTravelState? startingHorseState,
        decimal startingWallet,
        int startingFood,
        int startingHorseFeed,
        int startingCanteenCharges,
        int startingHealth,
        int startingHorseHunger,
        int startingHorseThirst,
        int startingHorseExhaustion,
        int startingDelayDays,
        int startingHeat,
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
                UpdateLatestTravelDiaryDay(day => CreateTravelDiaryDay(
                    pendingSnapshot,
                    startingTravelMode,
                    startingRideDayDistance,
                    startingDaysRemaining,
                    startingHorseState,
                    startingWallet,
                    startingFood,
                    startingHorseFeed,
                    startingCanteenCharges,
                    startingHealth,
                    startingHorseHunger,
                    startingHorseThirst,
                    startingHorseExhaustion,
                    startingDelayDays,
                    startingHeat,
                    pendingEncounter: pendingEncounter,
                    encounterResolution: resolution,
                    entries: dayEntries));

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
        UpdateLatestTravelDiaryDay(day => CreateTravelDiaryDay(
            journeySnapshot,
            startingTravelMode,
            startingRideDayDistance,
            startingDaysRemaining,
            startingHorseState,
            startingWallet,
            startingFood,
            startingHorseFeed,
            startingCanteenCharges,
            startingHealth,
            startingHorseHunger,
            startingHorseThirst,
            startingHorseExhaustion,
            startingDelayDays,
            startingHeat,
            encounterResolution: resolution,
            entries: dayEntries));

        if (Journey.CurrentDayPlan?.IsComplete == true)
        {
            Journey.SetCurrentDayPlan(null);
        }

        if (Journey.RemainingDays == 0 && Journey.RemainingRideDayDistance == 0)
        {
            var destinationTownId = Journey.Preview.DestinationTownId;
            var destinationTownName = Journey.Preview.DestinationTownName;
            Journey.MarkCompleted();
            Player.TravelTo(destinationTownId);
            var canteenState = Player.Inventory.GetCanteenState();
            if (canteenState is not null && canteenState.Charges < canteenState.Capacity)
            {
                var refilledCanteen = CanteenState.Full(canteenState.Capacity);
                Player.Inventory.SetCanteenState(refilledCanteen);
                Journey.SetCanteenCharges(refilledCanteen.Charges);
            }

            var completedSnapshot = Journey.ToSnapshot(TravelRules);
            UpdateLatestTravelDiaryDay(day => CreateTravelDiaryDay(
                completedSnapshot,
                startingTravelMode,
                startingRideDayDistance,
                startingDaysRemaining,
                startingHorseState,
                startingWallet,
                startingFood,
                startingHorseFeed,
                startingCanteenCharges,
                startingHealth,
                startingHorseHunger,
                startingHorseThirst,
                startingHorseExhaustion,
                startingDelayDays,
                startingHeat,
                encounterResolution: resolution,
                entries: dayEntries));
            Journey = null;
            return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Completed, $"You clear the remaining trail and reach {destinationTownName}.", completedSnapshot);
        }

        Journey.ResumeFromEncounter();
        return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, "You push the rider behind you and keep moving.", journeySnapshot);
    }

    private static string BuildJourneyOpeningNarration(TravelPreview preview)
    {
        var travelMode = DescribeTravelMode(preview.TravelMode);
        var terrain = DescribeTerrain(preview.RouteProfile.Terrain);
        var risk = DescribeRisk(preview.RouteProfile.Risk);
        var waterPressure = preview.WaterSecure
            ? $"I have enough water for the base trail, though the canteen still needs watching on a {preview.ExpectedDays}-day run."
            : $"This dry trail will ask for {preview.CanteenChargesPerDay} canteen charge(s) a day, and I do not have much slack.";
        var foodPressure = preview.AvailableFood <= preview.ExpectedDays
            ? "My food is tight enough that I will notice every meal."
            : "My food should hold if the trail behaves itself.";
        var horsePressure = preview.HorseState is null
            ? "I am traveling without a horse, so the road will have to be enough."
            : preview.MountedTravelAvailable
                ? "The horse is fit enough to carry me for now."
                : "The horse is not fit for mounted travel, so I will need to mind the pace.";

        return $"I set out for {preview.DestinationTownName} on a {preview.ExpectedDays}-day {terrain} trail {travelMode}. {risk} {waterPressure} {foodPressure} {horsePressure}";
    }

    private static string BuildJourneyBeat(
        TravelJourneySnapshot journeySnapshot,
        JourneyTrailEventState? trailEvent,
        JourneyEncounterState? pendingEncounter,
        TravelDiaryEncounterResolutionState? encounterResolution)
    {
        if (pendingEncounter is not null && encounterResolution is null)
        {
            return pendingEncounter.Kind switch
            {
                "foe" => "A hard-eyed rider steps out from the brush and stops the day cold.",
                _ => "Something on the trail makes me stop and square up."
            };
        }

        if (encounterResolution is not null)
        {
            return encounterResolution.ChoiceId switch
            {
                "run" => "I put the bad moment behind me and keep moving.",
                "fight" => "I answer hard and keep the trail under my boot.",
                "bribe" => "I pay my way through and keep the dust moving.",
                _ => $"I answer by choosing to {encounterResolution.ChoiceLabel.ToLowerInvariant()}."
            };
        }

        if (trailEvent is not null)
        {
            return trailEvent.Id switch
            {
                JourneyTrailEventId.LuckyCoinCache => "The trail offers a little luck when I need it most.",
                JourneyTrailEventId.LuckyFoodCache => "I catch the smell of good luck and fresh grub on the wind.",
                JourneyTrailEventId.LuckyWaterSeep => "I follow a faint trace of damp earth and find a hidden seep.",
                JourneyTrailEventId.BadLuckWashout => "The trail caves in and makes me earn every mile.",
                JourneyTrailEventId.BadLuckFoodLoss => "The dust turns mean and I have to keep my temper in check.",
                JourneyTrailEventId.BadLuckSpookedHorse => "The horse flinches at the wrong sound and the day goes sideways.",
                _ => trailEvent.Message
            };
        }

        if (journeySnapshot.DaysTravelled % 6 == 0)
        {
            return "The trail goes quiet enough that I can hear leather creak and wind move through the brush.";
        }

        return journeySnapshot.RouteProfile.Terrain switch
        {
            TrailTerrain.OpenRange => journeySnapshot.TravelMode == TravelMode.Mounted
                ? "I cross open range with the horse moving steady under me."
                : "I walk the open range and let the horizon keep me honest.",
            TrailTerrain.Hills => journeySnapshot.TravelMode == TravelMode.Mounted
                ? "The hills make the horse work for every rise, but the miles still move."
                : "The hills keep asking for another climb, and I keep answering.",
            TrailTerrain.Badlands => "The badlands stay hard and dry, but the road still has to be followed.",
            TrailTerrain.Mountains => "The trail climbs hard, and I keep picking my way upward.",
            _ => "I keep moving and let the road tell me what kind of day it is."
        };
    }

    private static string? BuildResourceBeat(
        TravelJourneySnapshot journeySnapshot,
        int currentFood,
        int currentHorseFeed,
        int startingCanteenCharges,
        int currentCanteenCharges,
        JourneyTrailEventState? trailEvent,
        JourneyEncounterState? pendingEncounter,
        TravelDiaryEncounterResolutionState? encounterResolution)
    {
        var pieces = new List<string>();

        if (journeySnapshot.Status == JourneyStatus.Completed && currentCanteenCharges > startingCanteenCharges)
        {
            pieces.Add("Back in town, I refill the canteen to the brim.");
        }
        else if (!JourneyUpkeepRules.HasRouteWater(journeySnapshot.RouteProfile.WaterFeature))
        {
            if (currentCanteenCharges == 0)
            {
                pieces.Add("The canteen is dry, so every mile starts to matter.");
            }
            else if (currentCanteenCharges <= journeySnapshot.CanteenChargesPerDay)
            {
                pieces.Add("I am down to the last stretch of water in the canteen.");
            }
        }

        if (currentFood == 0)
        {
            pieces.Add("My food is gone, and the trail has turned mean.");
        }
        else if (currentFood == 1)
        {
            pieces.Add("My food is down to the last meal.");
        }

        if (currentHorseFeed == 0 && journeySnapshot.HorseState is not null)
        {
            pieces.Add("The horse feed is gone, so I have to watch the horse more closely.");
        }
        else if (currentHorseFeed == 1 && journeySnapshot.HorseState is not null)
        {
            pieces.Add("The horse feed is down to the last handful.");
        }

        if (pendingEncounter is not null && encounterResolution is null && journeySnapshot.Warnings.Count > 0)
        {
            pieces.Add("The route warnings stay in my head while I deal with the rider.");
        }

        return pieces.Count == 0 ? null : string.Join(" ", pieces);
    }

    private sealed record TrailEventApplicationResult(string HorseLossMessage);

    public JourneyEncounterResolutionResult ResolveJourneyEncounter(string choiceId)
    {
        return ResolveJourneyEncounterDeterministic(choiceId);

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
        if (!encounter.Choices.Any(choice => string.Equals(choice.Id, choiceId, StringComparison.OrdinalIgnoreCase)))
        {
            return JourneyEncounterResolutionResult.Failed("That is not a lawful way to answer this encounter.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }

        if (encounter.Kind != "foe")
        {
            return JourneyEncounterResolutionResult.Failed("That encounter cannot be resolved yet.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }

        var startingTravelMode = Journey.TravelMode;
        var startingRideDayDistance = Journey.RemainingRideDayDistance;
        var startingDaysRemaining = Journey.RemainingDays;
        var startingHorseState = Player.Inventory.GetHorseState();
        var startingWallet = Player.Wallet.Cash;
        var startingFood = Player.Inventory.GetQuantity(ItemKind.Food);
        var startingHorseFeed = Player.Inventory.GetQuantity(ItemKind.HorseFeed);
        var startingCanteenCharges = Player.Inventory.GetCanteenState()?.Charges ?? 0;
        var startingHealth = Player.Health;
        var startingHorseHunger = startingHorseState?.Hunger ?? 0;
        var startingHorseThirst = startingHorseState?.Thirst ?? 0;
        var startingHorseExhaustion = startingHorseState?.Exhaustion ?? 0;
        var startingDelayDays = Journey.DelayDays;
        var startingHeat = PursuitState.Heat;

        var resolvedChoiceId = choiceId.Trim().ToLowerInvariant();
        var resolvedChoiceLabel = encounter.Choices.First(choice => string.Equals(choice.Id, resolvedChoiceId, StringComparison.OrdinalIgnoreCase)).Label;

        switch (resolvedChoiceId)
        {
            case "run":
            {
                var isMountedEscape = Journey.TravelMode == TravelMode.Mounted;
                var heatIncrease = isMountedEscape ? TravelRules.EncounterRunMountedHeatIncrease : TravelRules.EncounterRunFootHeatIncrease;
                PursuitState.IncreaseHeat(heatIncrease);

                var horseLossMessage = isMountedEscape
                    ? ApplyEncounterHorsePressure(TravelRules.EncounterRunMountedHorseExhaustion)
                    : string.Empty;

                if (!isMountedEscape)
                {
                    Player.AdjustHealth(-TravelRules.EncounterRunFootHealthLoss);
                }

                Journey.ResumeFromEncounter();
                var runMessage = isMountedEscape
                    ? "You spur the horse and pull away."
                    : $"You run on foot and pull away, but it costs you {TravelRules.EncounterRunFootHealthLoss} health.";
                if (isMountedEscape && horseLossMessage.Length != 0)
                {
                    runMessage = $"{runMessage} You continue on foot.";
                }

                runMessage = PrependHorseLossMessage(horseLossMessage, runMessage);
                AddLogEntry(GameLogEntryKind.Travel, runMessage);
                var resolvedSnapshot = Journey.ToSnapshot(TravelRules);
                UpdateOrAppendEncounterResolutionDiary(
                    resolvedSnapshot,
                    encounter,
                    resolvedChoiceId,
                    resolvedChoiceLabel,
                    healthDelta: isMountedEscape ? 0 : -TravelRules.EncounterRunFootHealthLoss,
                    walletDelta: 0m,
                    ammoSpent: 0,
                    heatIncrease: heatIncrease,
                    horseExhaustionDelta: isMountedEscape ? TravelRules.EncounterRunMountedHorseExhaustion : 0,
                    startingTravelMode,
                    startingRideDayDistance,
                    startingDaysRemaining,
                    startingHorseState,
                    startingWallet,
                    startingFood,
                    startingHorseFeed,
                    startingCanteenCharges,
                    startingHealth,
                    startingHorseHunger,
                    startingHorseThirst,
                    startingHorseExhaustion,
                    startingDelayDays,
                    startingHeat,
                    continuedOnFoot: Journey.TravelMode == TravelMode.Foot);
                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, runMessage, resolvedSnapshot);
            }

            case "fight":
                var hasFirearmAmmo = Player.Inventory.GetQuantity(ItemKind.RevolverAmmo) > 0 || Player.Inventory.GetQuantity(ItemKind.RifleAmmo) > 0;
                var hasKnife = Player.Inventory.HasItem(ItemKind.Knife);
                if (!hasFirearmAmmo && !hasKnife)
                {
                    return JourneyEncounterResolutionResult.Failed("You need a knife or firearm ammo to stand and fight.", Journey.Status, Journey.ToSnapshot(TravelRules));
                }

                var usedFirearm = hasFirearmAmmo && TrySpendFirearmAmmo();
                var fightHealthLoss = usedFirearm
                    ? TravelRules.EncounterFightAmmoHealthLoss
                    : TravelRules.EncounterFightUnarmedHealthLoss;

                Player.AdjustHealth(-fightHealthLoss);
                PursuitState.IncreaseHeat(TravelRules.EncounterFightHeatIncrease);
                Journey.ResumeFromEncounter();
                var fightMessage = usedFirearm
                    ? $"You spend a round and take {fightHealthLoss} health damage before forcing the rider off the trail."
                    : $"You fight with your knife and take {fightHealthLoss} health damage before forcing the rider off the trail.";
                AddLogEntry(GameLogEntryKind.Travel, fightMessage);
                var fightResolvedSnapshot = Journey.ToSnapshot(TravelRules);
                UpdateOrAppendEncounterResolutionDiary(
                    fightResolvedSnapshot,
                    encounter,
                    resolvedChoiceId,
                    resolvedChoiceLabel,
                    healthDelta: -fightHealthLoss,
                    walletDelta: 0m,
                    ammoSpent: usedFirearm ? 1 : 0,
                    heatIncrease: TravelRules.EncounterFightHeatIncrease,
                    horseExhaustionDelta: 0,
                    startingTravelMode,
                    startingRideDayDistance,
                    startingDaysRemaining,
                    startingHorseState,
                    startingWallet,
                    startingFood,
                    startingHorseFeed,
                    startingCanteenCharges,
                    startingHealth,
                    startingHorseHunger,
                    startingHorseThirst,
                    startingHorseExhaustion,
                    startingDelayDays,
                    startingHeat,
                    continuedOnFoot: Journey.TravelMode == TravelMode.Foot);
                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, fightMessage, fightResolvedSnapshot);

            case "bribe":
                var bribeAmount = TravelRules.EncounterBribeCash;
                if (!Player.Wallet.CanAfford(bribeAmount))
                {
                    return JourneyEncounterResolutionResult.Failed($"You need ${bribeAmount:0.00} to bribe your way through.", Journey.Status, Journey.ToSnapshot(TravelRules));
                }

                Player.SetWallet(Player.Wallet.Spend(bribeAmount));
                Journey.ResumeFromEncounter();
                var bribeMessage = $"You bribe the rider with ${bribeAmount:0.00} and continue on.";
                AddLogEntry(GameLogEntryKind.Travel, bribeMessage);
                var bribeResolvedSnapshot = Journey.ToSnapshot(TravelRules);
                UpdateOrAppendEncounterResolutionDiary(
                    bribeResolvedSnapshot,
                    encounter,
                    resolvedChoiceId,
                    resolvedChoiceLabel,
                    healthDelta: 0,
                    walletDelta: -bribeAmount,
                    ammoSpent: 0,
                    heatIncrease: 0,
                    horseExhaustionDelta: 0,
                    startingTravelMode,
                    startingRideDayDistance,
                    startingDaysRemaining,
                    startingHorseState,
                    startingWallet,
                    startingFood,
                    startingHorseFeed,
                    startingCanteenCharges,
                    startingHealth,
                    startingHorseHunger,
                    startingHorseThirst,
                    startingHorseExhaustion,
                    startingDelayDays,
                    startingHeat,
                    continuedOnFoot: Journey.TravelMode == TravelMode.Foot);
                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, bribeMessage, bribeResolvedSnapshot);

            default:
                return JourneyEncounterResolutionResult.Failed("That choice is not available for this encounter.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }
    }

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

    public StorePurchaseResult Purchase(StoreOffer offer, int quantity)
    {
        ArgumentNullException.ThrowIfNull(offer);

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
        var currentTown = World.GetTown(Player.CurrentTownId);

        if ((currentTown.Services & TownServices.NoticeBoard) == 0)
        {
            return ReadWantedPostersResult.Failed("There are no wanted posters here.");
        }

        var clue = CaseFile.RevealNextPublicClue();

        if (clue is null)
        {
            RecordCaseUpdate("You study the wanted posters, but find nothing new.");
            return ReadWantedPostersResult.Succeeded("You study the wanted posters, but find nothing new.", sessionChanged: true);
        }

        RecordCaseUpdate($"You study the wanted posters and note a public lead: {clue.Description}.");
        return ReadWantedPostersResult.Succeeded("You study the wanted posters and uncover a public lead.", sessionChanged: true);
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

    private bool TrySpendFirearmAmmo()
    {
        var capabilities = new InventoryCapabilityResolver().Resolve(Player.Inventory);
        if (!capabilities.GunfightCapable)
        {
            return false;
        }

        if (Player.Inventory.GetQuantity(ItemKind.RevolverAmmo) > 0)
        {
            Player.Inventory.RemoveQuantity(ItemKind.RevolverAmmo, 1);
            return true;
        }

        if (Player.Inventory.GetQuantity(ItemKind.RifleAmmo) > 0)
        {
            Player.Inventory.RemoveQuantity(ItemKind.RifleAmmo, 1);
            return true;
        }

        return false;
    }

}
