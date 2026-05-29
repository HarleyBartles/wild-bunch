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

        if (Journey is not null && Journey.Status == JourneyStatus.Active)
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

        if (Journey.PendingEncounter is not null)
        {
            Journey.MarkInterrupted(Journey.PendingEncounter);
            var interruptedSnapshot = Journey.ToSnapshot();
            var interruptedMessage = Journey.PendingEncounter.Message;
            AddLogEntry(GameLogEntryKind.Travel, interruptedMessage);
            return new TravelJourneyStepResult(false, Journey.Status, interruptedMessage, interruptedMessage, 0, interruptedSnapshot);
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

        var horseWentFoot = false;
        var switchToFootAfterToday = false;

        if (Journey.TravelMode == TravelMode.Mounted)
        {
            if (!Journey.TryConsumeHorseFeed())
            {
                var nextHorseCondition = AdvanceHorseCondition(Player.Inventory.GetHorseCondition() ?? HorseCondition.Healthy);
                Player.Inventory.SetHorseCondition(nextHorseCondition);
                Journey.SetHorseCondition(nextHorseCondition);

                if (nextHorseCondition != HorseCondition.Healthy)
                {
                    switchToFootAfterToday = true;
                }
            }
            else
            {
                Player.Inventory.RemoveQuantity(ItemKind.HorseFeed, 1);
            }
        }

        Clock.Advance();
        var progress = Journey.AdvanceOneDay();
        if (switchToFootAfterToday)
        {
            Journey.RecalculatePacing(TravelMode.Foot);
            horseWentFoot = true;
        }
        PursuitState.IncreaseHeat(Math.Max(1, (int)Journey.Preview.RouteProfile.Risk));

        if (horseWentFoot)
        {
            AddLogEntry(GameLogEntryKind.Travel, "Your horse can no longer carry you, so you continue on foot.");
        }

        if (progress.Completed)
        {
            var destinationTownId = Journey.Preview.DestinationTownId;
            var destinationTownName = Journey.Preview.DestinationTownName;
            var heatIncrease = Math.Max(1, (int)Journey.Preview.RouteProfile.Risk);
            Journey.MarkCompleted();
            var completedSnapshot = Journey.ToSnapshot();
            Player.TravelTo(destinationTownId);
            AddLogEntry(
                GameLogEntryKind.Travel,
                $"You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s).");
            Journey = null;

            return new TravelJourneyStepResult(
                true,
                JourneyStatus.Completed,
                $"You reach {destinationTownName}.",
                $"You reach {destinationTownName} after {progress.DistanceTravelled} distance units.",
                heatIncrease,
                completedSnapshot);
        }

        var ongoingSnapshot = Journey.ToSnapshot();
        var ongoingMessage = $"One trail day passes. {Journey.RemainingDays} day(s) remain on the route.";
        AddLogEntry(GameLogEntryKind.Travel, ongoingMessage);

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            ongoingMessage,
            ongoingMessage,
            Math.Max(1, (int)Journey.Preview.RouteProfile.Risk),
            ongoingSnapshot);
    }

    public StorePurchaseResult Purchase(StoreOffer offer, int quantity)
    {
        ArgumentNullException.ThrowIfNull(offer);

        if (quantity < 1)
        {
            return StorePurchaseResult.Failed("Quantity must be at least 1.");
        }

        if (offer.ItemKind == ItemKind.Horse && offer.HorseCondition is null)
        {
            return StorePurchaseResult.Failed("Horse offers must define a horse condition.");
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
        Player.Inventory.AddItem(offer.ItemKind, quantity, offer.HorseCondition);
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

    private static HorseCondition AdvanceHorseCondition(HorseCondition currentHorseCondition)
        => currentHorseCondition switch
        {
            HorseCondition.Healthy => HorseCondition.Hungry,
            HorseCondition.Hungry => HorseCondition.Exhausted,
            HorseCondition.Exhausted => HorseCondition.Lame,
            HorseCondition.Lame => HorseCondition.Dead,
            HorseCondition.Dead => HorseCondition.Dead,
            _ => HorseCondition.Hungry
        };
}
