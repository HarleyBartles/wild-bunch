using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Factory methods for deterministic travel test scenarios.
/// All scenarios use <see cref="TravelRandomnessState.CreateDeterministic"/>
/// for reproducible day plans and encounter generation. Each factory returns
/// the session alongside the resolved <see cref="TravelPreview"/> so callers
/// can decide whether to start the journey via <see cref="GameSession.StartJourney"/>.
/// </summary>
internal static class TravelTestFactory
{
    /// <summary>
    /// Creates a session with a short low-risk journey from Current Town to Connected Town.
    /// Inventory: 4 Food, 1 Canteen (full 10), 1 Horse (Healthy), 1 Saddle.
    /// Wallet: $25. TravelDifficulty: Easy.
    /// This reuses <see cref="TestSessionFactory.CreateDefault"/> world setup.
    /// </summary>
    internal static (GameSession session, TravelPreview preview) CreateEasyShortJourney()
    {
        var session = TestSessionFactory.CreateDefault();
        var preview = ResolvePreview(session, new TownId("connected"));
        return (session, preview);
    }

    /// <summary>
    /// Creates a session with a high-risk journey designed to trigger encounters.
    /// Uses TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, distance 6m.
    /// Inventory: 3 Food, 1 Canteen (full 2), 1 Horse (Healthy), 1 Saddle, 1 Knife.
    /// </summary>
    internal static (GameSession session, TravelPreview preview) CreateHighRiskJourney()
    {
        // Mirror the CreateHighRiskSession pattern from AdvanceTravelDayHandlerTests
        // but in the Domain test project (no handler, just session + preview).
        var pinecross = new Town(new TownId("pinecross"), "Pinecross",
            TownServices.NoticeBoard | TownServices.Telegraph | TownServices.Lodging);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id,
                    TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, rideDayDistance: 6m)
            });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 3),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(2)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1),
        });

        var caseFile = new CaseFile(
            accusation: null,
            suspects: Array.Empty<Suspect>(),
            trueCulpritId: new SuspectId("suspect-1"),
            knownClues: Array.Empty<Clue>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile,
            pinecross.Id, Wallet.Starting(25m), inventory,
            TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();

        var preview = ResolvePreview(session, dryfork.Id);
        return (session, preview);
    }

    /// <summary>
    /// Creates a session with a long low-risk journey designed to complete without interruption.
    /// Uses TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, distance 6m.
    /// Inventory: 8 Food, 1 Canteen (full 10), 1 Horse (Healthy), 1 Saddle.
    /// </summary>
    internal static (GameSession session, TravelPreview preview) CreateSixDayQuietJourney()
    {
        // Mirror the CreateSixDayQuietSession pattern from AdvanceTravelDayHandlerTests.
        var origin = new Town(new TownId("origin"), "Origin Town",
            TownServices.NoticeBoard | TownServices.Telegraph | TownServices.Lodging);
        var destination = new Town(new TownId("destination"), "Destination Town", TownServices.None);
        var world = new DomainWorld(
            new[] { origin, destination },
            new[]
            {
                new Trail(new TrailId("trail-long"), origin.Id, destination.Id,
                    TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, rideDayDistance: 6m)
            });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 8),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
        });

        var caseFile = new CaseFile(
            accusation: null,
            suspects: Array.Empty<Suspect>(),
            trueCulpritId: new SuspectId("suspect-1"),
            knownClues: Array.Empty<Clue>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile,
            origin.Id, Wallet.Starting(25m), inventory,
            TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();

        var preview = ResolvePreview(session, destination.Id);
        return (session, preview);
    }

    /// <summary>
    /// Resolves a deterministic <see cref="TravelPreview"/> for a journey from the session's
    /// current town to <paramref name="destinationId"/>. Throws if the preview cannot be resolved
    /// so test setup fails fast with a clear message rather than producing a null preview.
    /// </summary>
    private static TravelPreview ResolvePreview(GameSession session, TownId destinationId)
    {
        var resolver = new TravelResolver();
        var result = resolver.PreviewJourney(
            session.World,
            session.Player.CurrentTownId,
            destinationId,
            session.Player.Inventory,
            session.TravelRules);
        if (!result.Success || result.Preview is null)
        {
            throw new InvalidOperationException(
                $"Could not create journey preview: {result.Message}");
        }

        return result.Preview;
    }
}
