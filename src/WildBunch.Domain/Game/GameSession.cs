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

        Journey = TravelJourney.Start(preview);
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
            var completedSnapshot = Journey.ToSnapshot(TravelRules);
            Player.TravelTo(destinationTownId);
            var completionMessage = horseLostMessage.Length == 0
                ? $"You reach {destinationTownName}."
                : $"{horseLostMessage} You reach {destinationTownName}.";
            AddLogEntry(
                GameLogEntryKind.Travel,
                horseLostMessage.Length == 0
                    ? $"You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s)."
                    : $"{horseLostMessage} You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s).");
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

    private sealed record TrailEventApplicationResult(string HorseLossMessage);

    public JourneyEncounterResolutionResult ResolveJourneyEncounter(string choiceId)
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

        if (encounter.Kind != "foe")
        {
            return JourneyEncounterResolutionResult.Failed("That encounter cannot be resolved yet.", Journey.Status, Journey.ToSnapshot(TravelRules));
        }

        switch (choiceId.Trim().ToLowerInvariant())
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
                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, runMessage, Journey.ToSnapshot(TravelRules));
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
                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, fightMessage, Journey.ToSnapshot(TravelRules));

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
                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, bribeMessage, Journey.ToSnapshot(TravelRules));

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
