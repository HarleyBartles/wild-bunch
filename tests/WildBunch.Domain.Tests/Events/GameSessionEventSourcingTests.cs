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
    public void CanonicalStart_Produces_GameStarted_Event_As_Uncommitted()
    {
        var session = CreateSession();
        var gameStarted = session.UncommittedEvents.OfType<GameStarted>().Single();
        Assert.Equal("Ranger Vale", gameStarted.PlayerName);
        Assert.Equal(new TownId("pinecross"), gameStarted.StartingTownId);
        Assert.Equal("Pinecross", gameStarted.StartingTownName);
        Assert.Equal(1000, gameStarted.StartingHealth);
        Assert.Equal(25m, gameStarted.StartingWallet);
        Assert.Equal(GameDifficulty.Standard, gameStarted.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, gameStarted.GameEntropy);
    }

    [Fact]
    public void CanonicalStart_WithSeedCode_Produces_GameStarted_Event_WithSeedCode()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var seedCode = "test-seed-code-12345";

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic,
            seedCode, SaltSource.CreateRuntime());
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(new TownId("pinecross"));
        session.CompleteGameStart();

        var gameStarted = session.UncommittedEvents.OfType<GameStarted>().Single();
        Assert.Equal(seedCode, gameStarted.SeedCode);
    }

    [Fact]
    public void RehydrateFromEvents_Restores_SeedCode_From_GameStarted_Event()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var seedCode = "test-seed-code-67890";

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic,
            seedCode, SaltSource.CreateRuntime());
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(new TownId("pinecross"));
        session.CompleteGameStart();

        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            world,
            events);

        Assert.Equal(seedCode, rehydrated.SeedCode);
    }

    [Fact]
    public void RehydrateFromEvents_WithOldGameStarted_Handles_Null_SeedCode()
    {
        var world = CreateWorld();

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
            new[] { oldGameStarted });

        Assert.Null(rehydrated.SeedCode);
    }

    [Fact]
    public void CanonicalStart_Increments_Version_To_Six()
    {
        var session = CreateSession();
        Assert.Equal(6, session.Version);
    }

    [Fact]
    public void Purchase_Produces_StoreItemPurchased_Event_As_Uncommitted()
    {
        var session = CreateSession();
        session.MarkEventsCommitted();

        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);

        session.Purchase(offer, 3);

        // BUNCH-5: Purchase now enters Store context first, producing TownActionContextEntered + StoreItemPurchased
        Assert.Equal(2, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        var purchased = Assert.IsType<StoreItemPurchased>(session.UncommittedEvents[1]);
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
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);

        session.Purchase(offer, 2);

        // BUNCH-5: Purchase now enters Store context first, producing 2 events (TownActionContextEntered + StoreItemPurchased)
        Assert.Equal(versionBefore + 2, session.Version);
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
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);

        session.Purchase(offer, 3);
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            session.World,
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
        var foodOffer = resolver.Resolve(commandSession.World.GetTown(commandSession.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        var canteenOffer = resolver.Resolve(commandSession.World.GetTown(commandSession.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Canteen);

        commandSession.Purchase(foodOffer, 2);
        commandSession.Purchase(foodOffer, 1);
        var events = commandSession.UncommittedEvents.ToList();
        commandSession.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(
            commandSession.Id,
            commandSession.World,
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
        Assert.Throws<ArgumentException>(() =>
            GameSession.RehydrateFromEvents(
                GameSessionId.New(),
                world,
                Array.Empty<IDomainEvent>()));
    }

    [Fact]
    public void RehydrateFromEvents_Throws_When_First_Event_Is_Not_GameStarted()
    {
        var world = CreateWorld();
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
                events));
    }

    [Fact]
    public void RehydrateFromEvents_Reconstructs_Investigation_State()
    {
        // Create a session with a public clue. The factory calls MarkEventsCommitted(),
        // so we need to get the GameStarted event by creating a fresh session for events.
        // Approach: create the session, perform the investigation, collect ALL events
        // (GameStarted + CaseFileGenerated + InvestigationPerformed) by creating a
        // parallel session for events.
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.LocalGossip, "A dusty boot print.");

        // Perform investigation (produces InvestigationPerformed event)
        session.GatherLocalGossip();
        var investigationEvents = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        // Build the full event stream: GameStarted + CaseFileGenerated + InvestigationPerformed.
        // We reconstruct the GameStarted event from the session's initial state.
        // The CaseFileGenerated event carries the case file snapshot (including the
        // now-known clue) so the case file can be reconstructed during replay without
        // being passed in externally.
        var gameStartedEvent = new GameStarted
        {
            PlayerName = session.Player.Name,
            StartingTownId = session.Player.CurrentTownId!.Value,
            StartingTownName = session.World.GetTown(session.Player.CurrentTownId!.Value).Name,
            StartingHealth = session.Player.Health,
            StartingWallet = 25m,
            StartingInventoryItems = Array.Empty<InventoryItem>(),
            GameDifficulty = session.GameDifficulty,
            SaltSource = session.SaltSource,
            GameEntropy = session.GameEntropy
        };
        var caseFileEvent = new CaseFileGenerated
        {
            CaseFile = CaseFileSnapshot.FromDomain(session.CaseFile)
        };
        var allEvents = new List<IDomainEvent> { gameStartedEvent, caseFileEvent };
        allEvents.AddRange(investigationEvents);

        // Replay from events — the CaseFile is now reconstructed from the
        // CaseFileGenerated event in the stream, not passed in externally.
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            session.World,
            allEvents);

        // The replayed session must have discovered the clue from the event
        Assert.Equal(session.CaseFile.KnownClues.Count, rehydrated.CaseFile.KnownClues.Count);
        Assert.Equal(session.CaseFile.PublicClues.Count, rehydrated.CaseFile.PublicClues.Count);
        var revealedClueId = investigationEvents.OfType<InvestigationPerformed>().Single().ClueId!.Value;
        Assert.Contains(rehydrated.CaseFile.KnownClues, c => c.Id.Equals(revealedClueId));
        Assert.DoesNotContain(rehydrated.CaseFile.PublicClues, c => c.Id.Equals(revealedClueId));
    }

    [Fact]
    public void CanonicalStart_FullRoundTrip_Rehydrates_CompleteState_FromEvents()
    {
        // Create a session with a FULLY-populated CaseFile (all 14 fields non-empty)
        // through the canonical flow, WITHOUT committing so the real event stream
        // (6 setup events) can be collected. This is the definitive proof that the
        // event stream alone reconstructs complete state — including the original
        // PublicClues data-loss gap fixed in Plan 1d.
        var session = CreateSessionWithFullCaseFile();

        // Perform an operation to prove post-start events also survive replay
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        session.Purchase(offer, 1);

        // Collect ALL events (6 setup + operation events)
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        // Rehydrate from events alone — no external world/caseFile references
        // beyond the world placeholder. The placeholder must contain the starting
        // town (the constructor resolves it before WorldGenerated overwrites the
        // world), but it is intentionally different from the session's world so we
        // can prove Apply(WorldGenerated) restores the real world from the stream.
        var placeholderWorld = new DomainWorld(
            new[]
            {
                new Town(new TownId("current"), "PLACEHOLDER Town", TownServices.None),
                new Town(new TownId("connected"), "PLACEHOLDER Connected", TownServices.None)
            },
            new[] { new Trail(new TrailId("trail-1"), new TownId("current"), new TownId("connected"), TrailRisk.Low) });
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            placeholderWorld,
            events);

        // Prove the world was reconstructed from the WorldGenerated event, not the placeholder
        Assert.NotEqual("PLACEHOLDER Town", rehydrated.World.GetTown(session.Player.CurrentTownId!.Value).Name);
        Assert.Equal(session.World.GetTown(session.Player.CurrentTownId!.Value).Name,
            rehydrated.World.GetTown(session.Player.CurrentTownId!.Value).Name);

        // Prove full session state reconstruction
        Assert.Equal(session.Id, rehydrated.Id);
        Assert.Equal(session.Player.Name, rehydrated.Player.Name);
        Assert.Equal(session.Player.CurrentTownId, rehydrated.Player.CurrentTownId);
        Assert.Equal(session.Player.Health, rehydrated.Player.Health);
        Assert.Equal(session.Player.Wallet.Cash, rehydrated.Player.Wallet.Cash);
        Assert.Equal(session.GameDifficulty, rehydrated.GameDifficulty);
        Assert.Equal(session.GameEntropy, rehydrated.GameEntropy);
        Assert.Equal(session.SeedCode, rehydrated.SeedCode);
        Assert.Equal(session.Version, rehydrated.Version);
        Assert.Equal(StartFlowPhase.GameStarted, rehydrated.StartFlowPhase);

        // Prove ALL 14 CaseFile fields are reconstructed from CaseFileGenerated event
        // (not from external references). Each field is populated with a non-empty
        // value in the source session so a count of 0 == 0 cannot hide a data loss.

        // 1. Suspects
        Assert.Equal(session.CaseFile.Suspects.Count, rehydrated.CaseFile.Suspects.Count);
        Assert.Equal(session.CaseFile.Suspects.Select(s => s.Id.Value).ToArray(),
            rehydrated.CaseFile.Suspects.Select(s => s.Id.Value).ToArray());

        // 2. TrueCulpritId
        Assert.Equal(session.CaseFile.TrueCulpritId, rehydrated.CaseFile.TrueCulpritId);

        // 3. OpeningLead
        Assert.Equal(session.CaseFile.OpeningLead, rehydrated.CaseFile.OpeningLead);

        // 4. KnownClues
        Assert.Equal(session.CaseFile.KnownClues.Count, rehydrated.CaseFile.KnownClues.Count);
        Assert.Equal(session.CaseFile.KnownClues.Select(c => c.Id.Value).ToArray(),
            rehydrated.CaseFile.KnownClues.Select(c => c.Id.Value).ToArray());

        // 5. PublicClues (the original data-loss gap)
        Assert.Equal(session.CaseFile.PublicClues.Count, rehydrated.CaseFile.PublicClues.Count);
        Assert.Equal(session.CaseFile.PublicClues[0].Id, rehydrated.CaseFile.PublicClues[0].Id);
        Assert.Equal(session.CaseFile.PublicClues[0].Description, rehydrated.CaseFile.PublicClues[0].Description);

        // 6. Accusation
        Assert.Equal(session.CaseFile.Accusation, rehydrated.CaseFile.Accusation);

        // 7. DiscoveredSuspectIds
        Assert.Equal(session.CaseFile.DiscoveredSuspectIds.Count, rehydrated.CaseFile.DiscoveredSuspectIds.Count);
        Assert.Equal(session.CaseFile.DiscoveredSuspectIds.Select(s => s.Value).ToArray(),
            rehydrated.CaseFile.DiscoveredSuspectIds.Select(s => s.Value).ToArray());

        // 8. KillerReleaseThreshold
        Assert.Equal(session.CaseFile.KillerReleaseThreshold, rehydrated.CaseFile.KillerReleaseThreshold);

        // 9. KillerReleaseProgress
        Assert.Equal(session.CaseFile.KillerReleaseProgress, rehydrated.CaseFile.KillerReleaseProgress);

        // 10. KnownWarrants
        Assert.Equal(session.CaseFile.KnownWarrants.Count, rehydrated.CaseFile.KnownWarrants.Count);
        Assert.Equal(session.CaseFile.KnownWarrants.Select(w => w.Id.Value).ToArray(),
            rehydrated.CaseFile.KnownWarrants.Select(w => w.Id.Value).ToArray());

        // 11. PublicWarrants
        Assert.Equal(session.CaseFile.PublicWarrants.Count, rehydrated.CaseFile.PublicWarrants.Count);
        Assert.Equal(session.CaseFile.PublicWarrants.Select(w => w.Id.Value).ToArray(),
            rehydrated.CaseFile.PublicWarrants.Select(w => w.Id.Value).ToArray());

        // 12. SuspectTurfAssignments
        Assert.Equal(session.CaseFile.SuspectTurfAssignments.Count, rehydrated.CaseFile.SuspectTurfAssignments.Count);
        Assert.Equal(
            session.CaseFile.SuspectTurfAssignments.Select(a => (a.SuspectId.Value, a.TurfTownId.Value)).ToArray(),
            rehydrated.CaseFile.SuspectTurfAssignments.Select(a => (a.SuspectId.Value, a.TurfTownId.Value)).ToArray());

        // 13. WantedSuspectConfrontations
        Assert.Equal(session.CaseFile.WantedSuspectConfrontations.Count, rehydrated.CaseFile.WantedSuspectConfrontations.Count);
        Assert.Equal(
            session.CaseFile.WantedSuspectConfrontations.Select(c => (c.SuspectId.Value, c.Outcome)).ToArray(),
            rehydrated.CaseFile.WantedSuspectConfrontations.Select(c => (c.SuspectId.Value, c.Outcome)).ToArray());

        // 14. SheriffTurnInSettlements
        Assert.Equal(session.CaseFile.SheriffTurnInSettlements.Count, rehydrated.CaseFile.SheriffTurnInSettlements.Count);
        Assert.Equal(
            session.CaseFile.SheriffTurnInSettlements.Select(s => (s.SuspectId.Value, s.BountyAmount)).ToArray(),
            rehydrated.CaseFile.SheriffTurnInSettlements.Select(s => (s.SuspectId.Value, s.BountyAmount)).ToArray());

        // Prove operation events survived replay (post-start mutation)
        Assert.Equal(session.Player.Inventory.GetQuantity(DomainItemKind.Food),
            rehydrated.Player.Inventory.GetQuantity(DomainItemKind.Food));

        Assert.Empty(rehydrated.UncommittedEvents);
    }

    [Fact]
    public void NonFirstStartingTown_TownStates_Parity_Between_Live_And_Rehydrated()
    {
        // When the starting town differs from world.Towns.First(), the live session
        // must not have a phantom entry for the placeholder town in TownStates.
        // Apply(GameStarted) calls ReplacePlaceholderTown which removes the placeholder
        // entry and enters the actual starting town at visitNumber 1. The rehydrated
        // session's constructor sets _currentTown directly from the GameStarted event,
        // so it also has only the actual starting town. Both must match.
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var resolvedInventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        // Use the SECOND town (redmesa) as the starting town — not world.Towns.First()
        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic,
            "test-seed", SaltSource.CreateFixed("test-salt"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(new TownId("redmesa"));
        session.CompleteGameStart(Wallet.Starting(25m), resolvedInventory);

        // Collect all events (6 setup events)
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        // Rehydrate from events alone
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            world,
            events);

        // Assert TownStates parity — this will FAIL because the live session has a
        // phantom firstTown entry (pinecross@1) in addition to the actual starting
        // town (redmesa@1), while the rehydrated session only has redmesa@1.
        Assert.Equal(session.CurrentTownVisit.TownStates.Count, rehydrated.CurrentTownVisit.TownStates.Count);

        // Assert the same town IDs appear with the same visit numbers
        var liveTownStates = session.CurrentTownVisit.TownStates
            .Select(s => (s.TownId.Value, s.VisitNumber)).OrderBy(x => x.Value).ToArray();
        var rehydratedTownStates = rehydrated.CurrentTownVisit.TownStates
            .Select(s => (s.TownId.Value, s.VisitNumber)).OrderBy(x => x.Value).ToArray();
        Assert.Equal(liveTownStates, rehydratedTownStates);
    }

    /// <summary>
    /// Creates a session through the canonical start flow with a CaseFile that populates
    /// ALL 14 fields with non-empty values, WITHOUT calling MarkEventsCommitted so the
    /// real event stream (6 setup events) remains in <see cref="GameSession.UncommittedEvents"/>.
    /// Used by the full round-trip proof test.
    /// </summary>
    private static GameSession CreateSessionWithFullCaseFile()
    {
        var town = new Town(new TownId("current"), "Current Town", TownServices.Telegraph);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint",
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline",
                SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var knownClue = new Clue(
            new ClueId("clue-known-1"),
            ClueKind.Alias,
            "A known alias: Grey Jay.",
            new[] { new SuspectId("suspect-1") },
            InvestigationTargetKind.Suspected,
            InvestigationSourceKind.LocalGossip,
            source: "test source",
            context: "test context");

        var publicClue = new Clue(
            new ClueId("clue-public-1"),
            ClueKind.Alias,
            "A dusty boot print.",
            new[] { new SuspectId("suspect-1") },
            InvestigationTargetKind.Suspected,
            InvestigationSourceKind.LocalGossip,
            source: "test source",
            context: "test context");

        var warrantTerms = new WarrantTerms(
            WarrantDisposition.DeadOrAlive,
            2500m,
            new[] { "Red Wren" },
            new[] { "Pale scar across the left cheek" },
            "Dodge City Marshal",
            InvestigationTargetKind.GangMember,
            new[] { OutlawGangIds.WildBunch },
            OutlawGangIds.WildBunch,
            InvestigationSourceKind.SheriffWarrants);

        var knownWarrant = new Warrant(
            new WarrantId("warrant-known-1"),
            "Ira Flint",
            warrantTerms,
            "Wanted for a Wild Bunch robbery.");

        var publicWarrant = new Warrant(
            new WarrantId("warrant-public-1"),
            "Mira Cline",
            warrantTerms,
            "Wanted for stagecoach robbery.");

        var caseFile = new CaseFile(
            accusation: new SuspectId("suspect-2"),
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: new[] { knownClue },
            discoveredSuspectIds: new[] { new SuspectId("suspect-1") },
            publicClues: new[] { publicClue },
            killerReleaseThreshold: 3,
            killerReleaseProgress: 1,
            knownWarrants: new[] { knownWarrant },
            publicWarrants: new[] { publicWarrant },
            suspectTurfAssignments: new[] { new SuspectTurfAssignment(new SuspectId("suspect-1"), town.Id) },
            wantedSuspectConfrontations: new[]
            {
                new WantedSuspectConfrontationState(
                    new SuspectId("suspect-1"),
                    "Ira Flint",
                    WarrantDisposition.DeadOrAlive,
                    WantedSuspectConfrontationOutcome.Surrendered,
                    IsAlive: true,
                    IsSecured: true,
                    Day: 1,
                    Turn: 2)
            },
            sheriffTurnInSettlements: new[]
            {
                new SheriffTurnInSettlementState(
                    new SuspectId("suspect-1"),
                    "Ira Flint",
                    WarrantDisposition.DeadOrAlive,
                    IsAlive: true,
                    BountyAmount: 2500m,
                    Day: 1,
                    Turn: 3)
            });

        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 4),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1)
        });

        // Use the internal canonical factory (no MarkEventsCommitted) so the 6 setup
        // events remain in UncommittedEvents for the round-trip proof.
        return TestSessionFactory.StartGameCanonical(
            "Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory, GameDifficulty.Easy,
            SaltSource.CreateFixed(string.Empty));
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
        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic,
            "test-seed", SaltSource.CreateFixed("test-salt"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(new TownId("pinecross"));
        session.CompleteGameStart(wallet ?? Wallet.Starting(25m), resolvedInventory);
        return session;
    }

    private static DomainWorld CreateWorld()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Telegraph);
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
