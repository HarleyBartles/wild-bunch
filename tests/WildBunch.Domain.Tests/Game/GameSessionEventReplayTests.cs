using System;
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

namespace WildBunch.Domain.Tests.Game;

/// <summary>
/// Event replay parity tests for the town hub layout feature (BUNCH-130).
/// The command path mutates state directly; the replay path reconstructs state
/// from the event stream via <see cref="GameSession.RehydrateFromEvents"/>. Per the
/// architecture guardrails, command-path and replay-path state must converge.
///
/// The world is created during game setup (command path) and reconstructed from
/// <see cref="WorldGenerated"/> during replay. <see cref="WorldGenerated"/> carries a
/// <see cref="WorldSnapshot"/> which round-trips <see cref="Town.Layout"/> through
/// <see cref="TownSnapshot.FromDomain"/>/<see cref="TownSnapshot.ToDomain"/> (Task 3).
/// These tests verify that layouts survive the round-trip — guarding against a future
/// regression where someone breaks the <see cref="TownSnapshot"/> round-trip.
/// </summary>
public sealed class GameSessionEventReplayTests
{
    [Fact]
    public void RehydrateFromEvents_Preserves_Town_Layouts_Across_Replay()
    {
        // Command path: build a world with layouts and start a game through the
        // canonical flow. The world is constructed with hand-crafted layouts (the
        // MapGenerator that produces layouts lives in GameContent, which is internal
        // and not referenced by Domain.Tests). The parity concern under test is the
        // WorldSnapshot round-trip, not the generation pipeline.
        var world = CreateWorldWithLayouts();
        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(ItemKind.Food, 1),
            new DomainInventoryItem(ItemKind.Canteen, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic,
            "test-seed", SaltSource.CreateFixed("test-salt"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(new TownId("pinecross"));
        session.CompleteGameStart(Wallet.Starting(25m), inventory);

        // Collect the full event stream (6 setup events, including WorldGenerated).
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        // Sanity: the command-path world has towns with non-null layouts.
        Assert.NotEmpty(session.World.Towns);
        Assert.All(session.World.Towns, town => Assert.NotNull(town.Layout));

        // Replay path: rehydrate from the event stream alone. A placeholder world is
        // passed to the constructor (the starting town must exist in it), but
        // Apply(WorldGenerated) overwrites it with the world reconstructed from the
        // WorldSnapshot carried by the event.
        var placeholderWorld = new DomainWorld(
            new[]
            {
                new Town(new TownId("pinecross"), "PLACEHOLDER", TownServices.None),
                new Town(new TownId("redmesa"), "PLACEHOLDER", TownServices.None)
            },
            new[] { new Trail(new TrailId("trail-1"), new TownId("pinecross"), new TownId("redmesa"), TrailRisk.Low) });

        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            placeholderWorld,
            events);

        // Parity proof: every town in the replayed world has a non-null layout that
        // matches the command-path world's layout.
        Assert.Equal(session.World.Towns.Count, rehydrated.World.Towns.Count);
        foreach (var commandTown in session.World.Towns)
        {
            var replayTown = rehydrated.World.GetTown(commandTown.Id);
            Assert.NotNull(replayTown.Layout);
            Assert.NotNull(commandTown.Layout);
            Assert.Equal(commandTown.Layout, replayTown.Layout);
        }

        // Version and event-state parity as a baseline guard.
        Assert.Equal(session.Version, rehydrated.Version);
        Assert.Empty(rehydrated.UncommittedEvents);
    }

    [Fact]
    public void RehydrateFromEvents_Layout_Parity_Holds_After_Post_Start_Mutation()
    {
        // The layout is world state set during setup; a post-start command (Purchase)
        // must not disturb layout parity. This mirrors the existing
        // RehydrateFromEvents_Replay_Matches_Command_Path_State proof but asserts the
        // world (with layouts) survives alongside the player-state mutation.
        var world = CreateWorldWithLayouts();
        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(ItemKind.Food, 1),
            new DomainInventoryItem(ItemKind.Canteen, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic,
            "test-seed", SaltSource.CreateFixed("test-salt"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(new TownId("pinecross"));
        session.CompleteGameStart(Wallet.Starting(25m), inventory);

        // Perform a post-start mutation (Purchase enters Store context first).
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == ItemKind.Food);
        session.Purchase(offer, 1);

        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            world,
            events);

        // Player-state parity (the existing proof).
        Assert.Equal(session.Player.Wallet.Cash, rehydrated.Player.Wallet.Cash);
        Assert.Equal(session.Version, rehydrated.Version);

        // Layout parity survives the post-start mutation.
        foreach (var commandTown in session.World.Towns)
        {
            var replayTown = rehydrated.World.GetTown(commandTown.Id);
            Assert.NotNull(replayTown.Layout);
            Assert.NotNull(commandTown.Layout);
            Assert.Equal(commandTown.Layout, replayTown.Layout);
        }
    }

    private static DomainWorld CreateWorldWithLayouts()
    {
        var pinecrossLayout = new TownLayout(
            new[]
            {
                new BuildingPlacement(BuildingKind.Store, 10, 20, BuildingView.FrontOblique),
                new BuildingPlacement(BuildingKind.Sheriff, 30, 12, BuildingView.FrontOblique),
                new BuildingPlacement(BuildingKind.Saloon, 50, 18, BuildingView.FrontOblique),
                new BuildingPlacement(BuildingKind.Trailhead, 5, 40, BuildingView.FrontOblique),
                new BuildingPlacement(BuildingKind.Telegraph, 22, 32, BuildingView.FrontOblique)
            },
            PlayerSpawnX: 50,
            PlayerSpawnY: 35,
            TownProsperity.Prosperous,
            Array.Empty<PathSegment>(),
            new int[10, 10]);

        var redmesaLayout = new TownLayout(
            new[]
            {
                new BuildingPlacement(BuildingKind.Store, 11, 21, BuildingView.FrontOblique),
                new BuildingPlacement(BuildingKind.Sheriff, 31, 13, BuildingView.FrontOblique),
                new BuildingPlacement(BuildingKind.Saloon, 51, 19, BuildingView.FrontOblique),
                new BuildingPlacement(BuildingKind.Trailhead, 6, 41, BuildingView.FrontOblique)
            },
            PlayerSpawnX: 52,
            PlayerSpawnY: 36,
            TownProsperity.Poor,
            Array.Empty<PathSegment>(),
            new int[10, 10]);

        var pinecross = new Town(
            new TownId("pinecross"),
            "Pinecross",
            TownServices.Telegraph,
            TownProsperity.Prosperous,
            MapX: 100,
            MapY: 100,
            IsOutlier: false,
            Layout: pinecrossLayout);

        var redmesa = new Town(
            new TownId("redmesa"),
            "Red Mesa",
            TownServices.None,
            TownProsperity.Poor,
            MapX: 400,
            MapY: 300,
            IsOutlier: false,
            Layout: redmesaLayout);

        return new DomainWorld(
            new[] { pinecross, redmesa },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low) });
    }

    private static CaseFile CreateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Ira Flint",
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
                SuspectStatus.AtLarge)
        };
        return new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
    }
}
