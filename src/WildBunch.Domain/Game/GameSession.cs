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
        TravelJourney? journey)
    {
        Id = id;
        Player = player;
        World = world;
        CaseFile = caseFile;
        PursuitState = pursuitState;
        Clock = clock;
        Status = status;
        Journey = journey;
    }

    public GameSessionId Id { get; }

    public GameStatus Status { get; private set; }

    public Player Player { get; private set; }

    public DomainWorld World { get; }

    public CaseFile CaseFile { get; }

    public PursuitState PursuitState { get; }

    public GameClock Clock { get; }

    public TravelJourney? Journey { get; private set; }

    public IReadOnlyList<GameLogEntry> LogEntries => _logEntries;

    public static GameSession StartNew(string playerName, DomainWorld world, CaseFile caseFile, TownId? startingTownId = null)
        => StartNew(playerName, world, caseFile, startingTownId, wallet: null, inventory: null);

    public static GameSession StartNew(
        string playerName,
        DomainWorld world,
        CaseFile caseFile,
        TownId? startingTownId,
        WildBunch.Domain.Economy.Wallet? wallet,
        DomainInventory? inventory)
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
            journey: null);

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
        AddLogEntry(
            GameLogEntryKind.Travel,
            $"You set out from {preview.OriginTownName} toward {preview.DestinationTownName}.");

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            $"You set out from {preview.OriginTownName} toward {preview.DestinationTownName}.",
            $"You set out from {preview.OriginTownName} toward {preview.DestinationTownName}.",
            0,
            Journey.ToSnapshot());
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
                Journey.ToSnapshot());
        }

        if (Journey.Status != JourneyStatus.Active)
        {
            return new TravelJourneyStepResult(
                false,
                Journey.Status,
                "The journey is not active.",
                "The journey is not active.",
                0,
                Journey.ToSnapshot());
        }

        var capabilities = new InventoryCapabilityResolver().Resolve(Player.Inventory);
        if (Journey.TravelMode == TravelMode.Mounted && !capabilities.MountedTravelAvailable)
        {
            Journey.RecalculatePacing(TravelMode.Foot);
        }

        if (Player.Inventory.GetQuantity(ItemKind.Food) < 1)
        {
            Journey.MarkFailed();
            var message = "The trail grinds to a halt when your food runs out.";
            AddLogEntry(GameLogEntryKind.Travel, message);
            var failedSnapshot = Journey.ToSnapshot();
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
            Player.Inventory.GetQuantity(ItemKind.HorseFeed));

        if (upkeep.HorseFeedConsumed > 0)
        {
            Player.Inventory.RemoveQuantity(ItemKind.HorseFeed, upkeep.HorseFeedConsumed);
            Journey.ConsumeHorseFeed(upkeep.HorseFeedConsumed);
        }

        if (upkeep.CanteenState is not null)
        {
            Player.Inventory.SetCanteenState(upkeep.CanteenState);
        }

        if (upkeep.HorseState is not null)
        {
            Player.Inventory.SetHorseState(upkeep.HorseState);
            Journey.SetHorseState(upkeep.HorseState);
        }

        var horseLostMessage = string.Empty;
        if (upkeep.MountedTravelLost && Journey.TravelMode == TravelMode.Mounted)
        {
            horseLostMessage = DescribeHorseLoss(upkeep.HorseState);
            Journey.RecalculatePacing(TravelMode.Foot);
        }

        Clock.Advance();
        var progress = Journey.AdvanceOneDay();
        PursuitState.IncreaseHeat(Math.Max(1, (int)Journey.Preview.RouteProfile.Risk));

        if (!progress.Completed)
        {
            var trailEvent = Journey.TryCreateTrailEvent();
            if (trailEvent is not null)
            {
                ApplyTrailEvent(trailEvent);
                var eventSnapshot = Journey.ToSnapshot();
                var trailEventMessage = PrependHorseLossMessage(horseLostMessage, trailEvent.Message);
                AddLogEntry(GameLogEntryKind.Travel, trailEventMessage);

                return new TravelJourneyStepResult(
                    true,
                    JourneyStatus.Active,
                    trailEventMessage,
                    trailEventMessage,
                    Math.Max(1, (int)Journey.Preview.RouteProfile.Risk),
                    eventSnapshot);
            }
        }

        var encounter = Journey.TryCreateEncounter();
        if (encounter is not null)
        {
            Journey.MarkInterrupted(encounter);
            var interruptedSnapshot = Journey.ToSnapshot();
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
            var completedSnapshot = Journey.ToSnapshot();
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
                    ? $"You reach {destinationTownName} after {progress.DistanceTravelled} distance units."
                    : $"{horseLostMessage} You reach {destinationTownName} after {progress.DistanceTravelled} distance units.",
                heatIncrease,
                completedSnapshot);
        }

        var ongoingSnapshot = Journey.ToSnapshot();
        var ongoingMessage = horseLostMessage.Length == 0
            ? $"One trail day passes. {Journey.RemainingDays} day(s) remain on the route."
            : $"{horseLostMessage} One trail day passes on foot. {Journey.RemainingDays} day(s) remain on the route.";
        AddLogEntry(GameLogEntryKind.Travel, ongoingMessage);

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            ongoingMessage,
            ongoingMessage,
            Math.Max(1, (int)Journey.Preview.RouteProfile.Risk),
            ongoingSnapshot);
    }

    private static string DescribeHorseLoss(HorseTravelState? horseState)
    {
        if (horseState is null)
        {
            return "Your horse can no longer carry you.";
        }

        if (horseState.IsDead)
        {
            return "Your horse dies on the trail.";
        }

        if (horseState.IsLame)
        {
            return "Your horse goes lame and can no longer carry you.";
        }

        return "Your horse can no longer carry you.";
    }

    private static string PrependHorseLossMessage(string horseLossMessage, string message)
        => horseLossMessage.Length == 0 ? message : $"{horseLossMessage} {message}";

    private void ApplyTrailEvent(JourneyTrailEventState trailEvent)
    {
        ArgumentNullException.ThrowIfNull(trailEvent);

        switch (trailEvent.Kind)
        {
            case JourneyTrailEventKind.Lucky:
                Player.SetWallet(Player.Wallet.Adjust(3m));
                break;

            case JourneyTrailEventKind.BadLuck:
                Journey!.AddDelayDays(1);
                break;

            default:
                throw new InvalidOperationException("Unknown trail event kind.");
        }
    }

    public JourneyEncounterResolutionResult ResolveJourneyEncounter(string choiceId)
    {
        if (Journey is null)
        {
            return JourneyEncounterResolutionResult.Failed("No active journey is underway.", JourneyStatus.Failed);
        }

        if (Journey.PendingEncounter is null)
        {
            return JourneyEncounterResolutionResult.Failed("There is no pending encounter to resolve.", Journey.Status, Journey.ToSnapshot());
        }

        if (Journey.Status != JourneyStatus.Interrupted)
        {
            return JourneyEncounterResolutionResult.Failed("The encounter is not waiting to be resolved.", Journey.Status, Journey.ToSnapshot());
        }

        if (string.IsNullOrWhiteSpace(choiceId))
        {
            return JourneyEncounterResolutionResult.Failed("Choose how you want to answer the encounter.", Journey.Status, Journey.ToSnapshot());
        }

        var encounter = Journey.PendingEncounter;
        if (!encounter.Choices.Any(choice => string.Equals(choice.Id, choiceId, StringComparison.OrdinalIgnoreCase)))
        {
            return JourneyEncounterResolutionResult.Failed("That is not a lawful way to answer this encounter.", Journey.Status, Journey.ToSnapshot());
        }

        if (encounter.Kind != "foe")
        {
            return JourneyEncounterResolutionResult.Failed("That encounter cannot be resolved yet.", Journey.Status, Journey.ToSnapshot());
        }

        switch (choiceId.Trim().ToLowerInvariant())
        {
            case "run":
                Journey.AddDelayDays(1);
                PursuitState.IncreaseHeat(1);
                Journey.ResumeFromEncounter();
                AddLogEntry(GameLogEntryKind.Travel, "You pull away under a cloud of dust and regain the trail after a delay.");
                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, "You run the gauntlet and keep riding.", Journey.ToSnapshot());

            case "fight":
                if (!TrySpendFirearmAmmo())
                {
                    return JourneyEncounterResolutionResult.Failed("You need firearm ammo to stand and fight.", Journey.Status, Journey.ToSnapshot());
                }

                Player.AdjustHealth(-5);
                PursuitState.IncreaseHeat(1);
                Journey.ResumeFromEncounter();
                AddLogEntry(GameLogEntryKind.Travel, "You break the encounter with gun smoke and keep moving.");
                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, "You fight through the ambush and keep moving.", Journey.ToSnapshot());

            case "bribe":
                const decimal bribeAmount = 5m;
                if (!Player.Wallet.CanAfford(bribeAmount))
                {
                    return JourneyEncounterResolutionResult.Failed("You do not have enough cash to bribe your way through.", Journey.Status, Journey.ToSnapshot());
                }

                Player.SetWallet(Player.Wallet.Spend(bribeAmount));
                Journey.ResumeFromEncounter();
                AddLogEntry(GameLogEntryKind.Travel, $"You pay ${bribeAmount:0.00} to clear the road and keep riding.");
                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, $"You bribe the rider with ${bribeAmount:0.00} and continue on.", Journey.ToSnapshot());

            default:
                return JourneyEncounterResolutionResult.Failed("That choice is not recognized.", Journey.Status, Journey.ToSnapshot());
        }
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
