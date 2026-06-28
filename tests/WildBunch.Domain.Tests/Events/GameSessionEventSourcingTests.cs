using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Domain.Tests.Events;

public class GameSessionEventSourcingTests
{
    [Fact]
    public void StartNew_Produces_GameStarted_Event_As_Uncommitted()
    {
        var session = CreateSession();
        var single = Assert.Single(session.UncommittedEvents);
        var gameStarted = Assert.IsType<GameStarted>(single);
        Assert.Equal("Ranger Vale", gameStarted.PlayerName);
        Assert.Equal(new TownId("pinecross"), gameStarted.StartingTownId);
        Assert.Equal("Pinecross", gameStarted.StartingTownName);
        Assert.Equal(1000, gameStarted.StartingHealth);
        Assert.Equal(25m, gameStarted.StartingWallet);
        Assert.Equal(GameDifficulty.Standard, gameStarted.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, gameStarted.GameEntropy);
    }

    [Fact]
    public void StartNew_WithSeedCode_Produces_GameStarted_Event_WithSeedCode()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var seedCode = "test-seed-code-12345";

        var session = GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            new TownId("pinecross"),
            wallet: null,
            inventory: null,
            GameDifficulty.Standard,
            SaltSource.CreateRuntime(),
            GameEntropy.Classic,
            seedCode);

        var single = Assert.Single(session.UncommittedEvents);
        var gameStarted = Assert.IsType<GameStarted>(single);
        Assert.Equal(seedCode, gameStarted.SeedCode);
    }

    [Fact]
    public void RehydrateFromEvents_Restores_SeedCode_From_GameStarted_Event()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var seedCode = "test-seed-code-67890";

        var session = GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            new TownId("pinecross"),
            wallet: null,
            inventory: null,
            GameDifficulty.Standard,
            SaltSource.CreateRuntime(),
            GameEntropy.Classic,
            seedCode);

        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            world,
            caseFile,
            events);

        Assert.Equal(seedCode, rehydrated.SeedCode);
    }

    [Fact]
    public void RehydrateFromEvents_WithOldGameStarted_Handles_Null_SeedCode()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();

        // Simulate an old GameStarted event without SeedCode
        var oldGameStarted = new GameStarted
        {
            PlayerName = "Ranger Vale",
            StartingTownId = new TownId("pinecross"),
            StartingTownName = "Pinecross",
            StartingHealth = 1000,
            StartingWallet = 25m,
            StartingInventoryItems = Array.Empty<InventoryItem>(),
            GameDifficulty = GameDifficulty.Standard,
            SaltSource = SaltSource.CreateRuntime(),
            GameEntropy = GameEntropy.Classic,
            SeedCode = null // Old event without seed code
        };

        var rehydrated = GameSession.RehydrateFromEvents(
            GameSessionId.New(),
            world,
            caseFile,
            new[] { oldGameStarted });

        Assert.Null(rehydrated.SeedCode);
    }

    [Fact]
    public void StartNew_Increments_Version_To_One()
    {
        var session = CreateSession();
        Assert.Equal(1, session.Version);
    }

    [Fact]
    public void Purchase_Produces_StoreItemPurchased_Event_As_Uncommitted()
    {
        var session = CreateSession();
        session.MarkEventsCommitted();

        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);

        session.Purchase(offer, 3);

        var single = Assert.Single(session.UncommittedEvents);
        var purchased = Assert.IsType<StoreItemPurchased>(single);
        Assert.Equal(new TownId("pinecross"), purchased.TownId);
        Assert.Equal(DomainItemKind.Food, purchased.ItemKind);
        Assert.Equal(3, purchased.Quantity);
        Assert.Equal(2m, purchased.UnitPrice);
        Assert.Equal(6m, purchased.TotalPrice);
        Assert.Equal(19m, purchased.WalletAfter);
    }

    [Fact]
    public void Purchase_Increments_Version()
    {
        var session = CreateSession();
        var versionBefore = session.Version;
        session.MarkEventsCommitted();

        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);

        session.Purchase(offer, 2);

        Assert.Equal(versionBefore + 1, session.Version);
    }

    [Fact]
    public void MarkEventsCommitted_Clears_Uncommitted_Events_Without_Changing_State()
    {
        var session = CreateSession();
        var walletBefore = session.Player.Wallet.Cash;
        var versionBefore = session.Version;

        session.MarkEventsCommitted();

        Assert.Empty(session.UncommittedEvents);
        Assert.Equal(versionBefore, session.Version);
        Assert.Equal(walletBefore, session.Player.Wallet.Cash);
    }

    [Fact]
    public void RehydrateFromEvents_Reconstructs_State_From_GameStarted_Only()
    {
        var session = CreateSession();
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            session.World,
            session.CaseFile,
            events);

        Assert.Equal(session.Id, rehydrated.Id);
        Assert.Equal(session.Player.Name, rehydrated.Player.Name);
        Assert.Equal(session.Player.CurrentTownId, rehydrated.Player.CurrentTownId);
        Assert.Equal(session.Player.Health, rehydrated.Player.Health);
        Assert.Equal(session.Player.Wallet.Cash, rehydrated.Player.Wallet.Cash);
        Assert.Equal(session.GameDifficulty, rehydrated.GameDifficulty);
        Assert.Equal(session.GameEntropy, rehydrated.GameEntropy);
        Assert.Equal(session.Version, rehydrated.Version);
        Assert.Empty(rehydrated.UncommittedEvents);
    }

    [Fact]
    public void RehydrateFromEvents_Reconstructs_State_From_GameStarted_And_Purchase()
    {
        var session = CreateSession();
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);

        session.Purchase(offer, 3);
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            session.World,
            session.CaseFile,
            events);

        Assert.Equal(session.Player.Wallet.Cash, rehydrated.Player.Wallet.Cash);
        Assert.Equal(session.Player.Inventory.GetQuantity(DomainItemKind.Food), rehydrated.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(session.Version, rehydrated.Version);
        Assert.Empty(rehydrated.UncommittedEvents);
    }

    [Fact]
    public void RehydrateFromEvents_Replay_Matches_Command_Path_State()
    {
        // The core Event Sourcing proof: replay produces the same state as the command path
        var commandSession = CreateSession();
        var resolver = new TownStoreCatalogResolver();
        var foodOffer = resolver.Resolve(commandSession.World.GetTown(commandSession.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        var canteenOffer = resolver.Resolve(commandSession.World.GetTown(commandSession.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Canteen);

        commandSession.Purchase(foodOffer, 2);
        commandSession.Purchase(foodOffer, 1);
        var events = commandSession.UncommittedEvents.ToList();
        commandSession.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(
            commandSession.Id,
            commandSession.World,
            commandSession.CaseFile,
            events);

        // State equality proof
        Assert.Equal(commandSession.Player.Wallet.Cash, rehydrated.Player.Wallet.Cash);
        Assert.Equal(commandSession.Player.Inventory.GetQuantity(DomainItemKind.Food), rehydrated.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(commandSession.Player.Inventory.GetQuantity(DomainItemKind.Canteen), rehydrated.Player.Inventory.GetQuantity(DomainItemKind.Canteen));
        Assert.Equal(commandSession.Version, rehydrated.Version);
    }

    [Fact]
    public void RehydrateFromEvents_Throws_On_Empty_Event_Stream()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        Assert.Throws<ArgumentException>(() =>
            GameSession.RehydrateFromEvents(
                GameSessionId.New(),
                world,
                caseFile,
                Array.Empty<IDomainEvent>()));
    }

    [Fact]
    public void RehydrateFromEvents_Throws_When_First_Event_Is_Not_GameStarted()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var events = new IDomainEvent[]
        {
            new StoreItemPurchased
            {
                TownId = new TownId("pinecross"),
                ItemKind = DomainItemKind.Food,
                DisplayName = "Food",
                Quantity = 1,
                UnitPrice = 2m,
                TotalPrice = 2m,
                WalletAfter = 23m
            }
        };
        Assert.Throws<ArgumentException>(() =>
            GameSession.RehydrateFromEvents(
                GameSessionId.New(),
                world,
                caseFile,
                events));
    }

    [Fact]
    public void RehydrateFromEvents_Throws_On_Unknown_Event_Type()
    {
        var session = CreateSession();
        var events = new IDomainEvent[]
        {
            new GameStarted
            {
                PlayerName = "Ranger Vale",
                StartingTownId = new TownId("pinecross"),
                StartingTownName = "Pinecross",
                StartingHealth = 1000,
                StartingWallet = 25m,
                StartingInventoryItems = Array.Empty<InventoryItem>(),
                GameDifficulty = GameDifficulty.Standard,
                SaltSource = SaltSource.CreateFixed("test"),
                GameEntropy = GameEntropy.Classic,
                SeedCode = null
            },
            new UnknownTestEvent()
        };
        Assert.Throws<InvalidOperationException>(() =>
            GameSession.RehydrateFromEvents(
                GameSessionId.New(),
                CreateWorld(),
                CreateCaseFile(),
                events));
    }

    [Fact]
    public void RehydrateFromEvents_Reconstructs_Investigation_State()
    {
        // Create a session with a public clue. The factory calls MarkEventsCommitted(),
        // so we need to get the GameStarted event by creating a fresh session for events.
        // Approach: create the session, perform the investigation, collect ALL events
        // (GameStarted + InvestigationPerformed) by creating a parallel session for events.
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.LocalGossip, "A dusty boot print.");

        // Perform investigation (produces InvestigationPerformed event)
        session.GatherLocalGossip();
        var investigationEvents = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        // Build the full event stream: GameStarted + InvestigationPerformed
        // We reconstruct the GameStarted event from the session's initial state
        var gameStartedEvent = new GameStarted
        {
            PlayerName = session.Player.Name,
            StartingTownId = session.Player.CurrentTownId,
            StartingTownName = session.World.GetTown(session.Player.CurrentTownId).Name,
            StartingHealth = session.Player.Health,
            StartingWallet = 25m,
            StartingInventoryItems = Array.Empty<InventoryItem>(),
            GameDifficulty = session.GameDifficulty,
            SaltSource = session.SaltSource,
            GameEntropy = session.GameEntropy
        };
        var allEvents = new List<IDomainEvent> { gameStartedEvent };
        allEvents.AddRange(investigationEvents);

        // Create a FRESH baseline CaseFile with the same public clue (not yet revealed)
        var freshBaselineCaseFile = TestSessionFactory.CreateBaselineCaseFileFor(session);

        // Replay from events into the FRESH baseline
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            session.World,
            freshBaselineCaseFile,
            allEvents);

        // The replayed session must have discovered the clue from the event
        Assert.Equal(session.CaseFile.KnownClues.Count, rehydrated.CaseFile.KnownClues.Count);
        Assert.Equal(session.CaseFile.PublicClues.Count, rehydrated.CaseFile.PublicClues.Count);
        var revealedClueId = investigationEvents.OfType<InvestigationPerformed>().Single().ClueId!.Value;
        Assert.Contains(rehydrated.CaseFile.KnownClues, c => c.Id.Equals(revealedClueId));
        Assert.DoesNotContain(rehydrated.CaseFile.PublicClues, c => c.Id.Equals(revealedClueId));
    }

    private sealed record UnknownTestEvent : IDomainEvent;

    private static GameSession CreateSession(
        Wallet? wallet = null,
        DomainInventory? inventory = null)
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var resolvedInventory = inventory ?? new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });
        return GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), wallet ?? Wallet.Starting(25m), resolvedInventory);
    }

    private static DomainWorld CreateWorld()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        return new DomainWorld(
            new[] { pinecross, redmesa },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low)
            });
    }

    private static CaseFile CreateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };
        return new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
    }
}
