using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldBuilderTests
{
    [Fact]
    public void CreateCanonicalWorldUsesTheSharedCatalog()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();

        Assert.Equal(
            new[]
            {
                ("dryfork", "Dry Fork", TownServices.None),
                ("emberfall", "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph),
                ("hardpan", "Hardpan", TownServices.None),
                ("holloway", "Holloway", TownServices.Doctor),
                ("openpass", "Open Pass", TownServices.None),
                ("pinecross", "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard),
                ("redmesa", "Red Mesa", TownServices.Supplies | TownServices.Telegraph),
                ("sagewell", "Sagewell", TownServices.Supplies | TownServices.Doctor),
            },
            SnapshotTowns(world));
        Assert.Equal(
            new[]
            {
                ("trail-hollow-sage", "holloway", "sagewell", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 3m),
                ("trail-pine-hardpan", "pinecross", "hardpan", TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 3m),
                ("trail-pine-hollow", "pinecross", "holloway", TrailRisk.Moderate, TrailTerrain.OpenRange, WaterFeature.Creek, 2m),
                ("trail-pine-openpass", "pinecross", "openpass", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m),
                ("trail-pine-red", "pinecross", "redmesa", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
                ("trail-red-dry", "redmesa", "dryfork", TrailRisk.High, TrailTerrain.OpenRange, WaterFeature.Creek, 5m),
                ("trail-red-ember", "redmesa", "emberfall", TrailRisk.High, TrailTerrain.OpenRange, WaterFeature.Creek, 5m),
                ("trail-red-sage", "redmesa", "sagewell", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 3m),
                ("trail-sage-ember", "sagewell", "emberfall", TrailRisk.Moderate, TrailTerrain.OpenRange, WaterFeature.Creek, 5m),
            },
            SnapshotTrails(world));
    }

    [Fact]
    public void CreateFrontierWorldUsesTheSharedCatalogAndFrontierOverlay()
    {
        var world = BuildSeedWorld(SeedWorldResolver.Resolve(CreateSeedCode(1, 1, 3, 0, tail: 17)));

        Assert.Equal(
            new[]
            {
                ("dryfork", "Dry Fork", TownServices.None),
                ("emberfall", "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph),
                ("hardpan", "Hardpan", TownServices.None),
                ("holloway", "Holloway", TownServices.Doctor | TownServices.NoticeBoard),
                ("openpass", "Open Pass", TownServices.None),
                ("pinecross", "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard),
                ("redmesa", "Red Mesa", TownServices.Supplies | TownServices.Telegraph),
                ("sagewell", "Sagewell", TownServices.Supplies | TownServices.Doctor),
            },
            SnapshotTowns(world));
        Assert.Equal(
            new[]
            {
                ("trail-hollow-sage", "holloway", "sagewell", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.River, 3m),
                ("trail-pine-hardpan", "pinecross", "hardpan", TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 3m),
                ("trail-pine-hollow", "pinecross", "holloway", TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring, 2m),
                ("trail-pine-openpass", "pinecross", "openpass", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m),
                ("trail-pine-red", "pinecross", "redmesa", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
                ("trail-red-dry", "redmesa", "dryfork", TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m),
                ("trail-red-ember", "redmesa", "emberfall", TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m),
                ("trail-red-sage", "redmesa", "sagewell", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.Creek, 3m),
                ("trail-sage-ember", "sagewell", "emberfall", TrailRisk.Moderate, TrailTerrain.Mountains, WaterFeature.Spring, 5m),
            },
            SnapshotTrails(world));
    }

    [Fact]
    public void CreateRailWorldUsesTheSharedCatalogAndRailOverlay()
    {
        var world = BuildSeedWorld(SeedWorldResolver.Resolve(CreateSeedCode(2, 1, 3, 0, tail: 19)));

        Assert.Equal(
            new[]
            {
                ("dryfork", "Dry Fork", TownServices.None),
                ("emberfall", "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph),
                ("hardpan", "Hardpan", TownServices.None),
                ("holloway", "Holloway", TownServices.Doctor),
                ("openpass", "Open Pass", TownServices.None),
                ("pinecross", "Pinecross", TownServices.Supplies | TownServices.Lodging),
                ("redmesa", "Red Mesa", TownServices.Supplies | TownServices.Telegraph | TownServices.NoticeBoard),
                ("sagewell", "Sagewell", TownServices.Supplies | TownServices.Doctor | TownServices.NoticeBoard),
            },
            SnapshotTowns(world));
        Assert.Equal(
            new[]
            {
                ("trail-hollow-sage", "holloway", "sagewell", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.River, 3m),
                ("trail-pine-hardpan", "pinecross", "hardpan", TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 3m),
                ("trail-pine-hollow", "pinecross", "holloway", TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring, 2m),
                ("trail-pine-openpass", "pinecross", "openpass", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m),
                ("trail-pine-red", "pinecross", "redmesa", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
                ("trail-red-dry", "redmesa", "dryfork", TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m),
                ("trail-red-ember", "redmesa", "emberfall", TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m),
                ("trail-red-sage", "redmesa", "sagewell", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.Creek, 3m),
                ("trail-sage-ember", "sagewell", "emberfall", TrailRisk.Moderate, TrailTerrain.Mountains, WaterFeature.Spring, 5m),
            },
            SnapshotTrails(world));
    }

    // --- Descriptor-based seed-derived town selection tests ---
    //
    // These tests build SeedWorld shapes using the resolver's own SelectTowns
    // method with fixed selection seeds. This is descriptor-based (the descriptor
    // is the town count + selection seed) and deterministic without treating raw
    // UUID strings as canonical fixtures.
    //
    // The UUID round-trip (CreateRepresentativeSeedCode) is tested separately
    // by CanonicalSeedWorldRoundTripsThroughAUuidShapedSeedCode in
    // SeedWorldResolverTests, which uses the canonical 8-town world where all
    // towns are selected (the only shape where the round-trip search space is
    // small enough to reliably find a match).

    /// <summary>
    /// Builds a SeedWorld with the given town count and selection seed using
    /// the resolver's own SelectTowns method. This produces shapes that are
    /// guaranteed reachable by the resolver.
    /// </summary>
    private static SeedWorld BuildSeedWorldWithCount(SeedWorldVariant variant, int townCount, ulong selectionSeed)
    {
        var selectedTownIds = SeedWorldResolver.SelectTowns(townCount, selectionSeed);
        var trails = SeedWorldResolver.BuildTrails(variant, selectedTownIds);
        return new SeedWorld(Guid.Empty, variant, selectedTownIds, trails, 1, 3, 0);
    }

    // 6-town worlds with different selection seeds
    private static readonly SeedWorld SixTownWorldA = BuildSeedWorldWithCount(SeedWorldVariant.Canonical, 6, 0);
    private static readonly SeedWorld SixTownWorldB = BuildSeedWorldWithCount(SeedWorldVariant.Canonical, 6, 1);

    // 7-town world
    private static readonly SeedWorld SevenTownWorld = BuildSeedWorldWithCount(SeedWorldVariant.Canonical, 7, 0);

    [Fact]
    public void DifferentSeedsCanProduceDifferentTownSelections()
    {
        // The seed deterministically derives which towns are selected from the catalog.
        // Different selection seeds produce different town selections (not just
        // different case/turf/cash outcomes).
        var selectionA = string.Join(",", SixTownWorldA.SelectedTownIds.OrderBy(id => id));
        var selectionB = string.Join(",", SixTownWorldB.SelectedTownIds.OrderBy(id => id));

        Assert.NotEqual(selectionA, selectionB);
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTrailSignatures()
    {
        // Different town selections produce different trail graphs.
        var trailsA = string.Join(",", SixTownWorldA.Trails.Select(t => t.Id).OrderBy(id => id));
        var trailsB = string.Join(",", SixTownWorldB.Trails.Select(t => t.Id).OrderBy(id => id));

        Assert.NotEqual(trailsA, trailsB);
    }

    [Fact]
    public void SameSeedProducesSameWorld()
    {
        // Same seed-world shape produces the same resolved world.
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(SevenTownWorld.SeedCode));
        var world1 = SeedWorldBuilder.CreateWorld(SevenTownWorld, source);
        var world2 = SeedWorldBuilder.CreateWorld(SevenTownWorld, source);
        Assert.Equal(world1.Towns.Count, world2.Towns.Count);
        Assert.Equal(world1.Trails.Count, world2.Trails.Count);
    }

    [Fact]
    public void SelectedStartingTownMustBeInGeneratedWorld()
    {
        // Starting town is NOT seed-owned but must be in the generated world.
        // StartingTownPolicy rejects a town that is not in the world.
        // SixTownWorldA has 6 towns (not all 8), so we can test with a
        // catalog town that was not selected.
        Assert.True(SixTownWorldA.SelectedTownIds.Count < SeedWorldCatalog.AllTowns.Count,
            "SixTownWorldA should produce fewer than 8 towns.");

        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(SixTownWorldA.SeedCode));
        var world = SeedWorldBuilder.CreateWorld(SixTownWorldA, source);
        var nonSelectedTown = SeedWorldCatalog.AllTowns.First(t => !SixTownWorldA.SelectedTownIds.Contains(t.Id));

        Assert.Throws<ArgumentException>(() =>
            StartingTownPolicy.ResolveStartingTown(world, new TownId(nonSelectedTown.Id)));
    }

    [Fact]
    public void BuilderCreatesWorldFromSeedWorldTemplate()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var source = new GameSetupDeterministicSource(seedWorld.SeedCodeText);
        var world = SeedWorldBuilder.CreateWorld(seedWorld, source);
        Assert.Equal(seedWorld.SelectedTownIds.Count, world.Towns.Count);
        Assert.Equal(seedWorld.Trails.Count, world.Trails.Count);
    }

    [Fact]
    public void StartingTownPolicyDefaultsToPinecrossForAllVariants()
    {
        // Starting town is NOT seed-owned. The safe default from StartingTownPolicy
        // is pinecross for all world variants — it is a fixed property of the world
        // catalog, not a hash of the seed code.
        var canonicalWorld = SeedWorldBuilder.CreateCanonicalWorld();
        Assert.Equal(new TownId("pinecross"), StartingTownPolicy.ResolveStartingTown(canonicalWorld, null));

        var frontierWorld = BuildSeedWorld(SeedWorldResolver.Resolve(CreateSeedCode(1, 1, 3, 0, tail: 17)));
        Assert.Equal(new TownId("pinecross"), StartingTownPolicy.ResolveStartingTown(frontierWorld, null));
    }

    [Fact]
    public void StartingTownPolicyAcceptsAnyValidTownChoice()
    {
        // The player can start in any town that exists in the generated world.
        var world = SeedWorldBuilder.CreateCanonicalWorld();
        var chosenTown = world.Towns.First(t => t.Id.Value != "pinecross");

        var resolved = StartingTownPolicy.ResolveStartingTown(world, chosenTown.Id);
        Assert.Equal(chosenTown.Id, resolved);
    }

    [Fact]
    public void StartingTownPolicyRejectsInvalidTownChoice()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();

        Assert.Throws<ArgumentException>(() =>
            StartingTownPolicy.ResolveStartingTown(world, new TownId("nonexistent-town")));
    }

    private static World BuildSeedWorld(SeedWorld seedWorld)
    {
        var seedCode = SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode == Guid.Empty
            ? SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld)
            : seedWorld.SeedCode);
        var source = new GameSetupDeterministicSource(seedCode);
        return SeedWorldBuilder.CreateWorld(seedWorld, source);
    }

    private static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong tail)
        => SeedWorldSeedCodeFactory.CreateSeedCode(worldVariant, accusationIndex, defaultCulpritIndex, cashBonus, tail);

    private static (string Id, string Name, TownServices Services)[] SnapshotTowns(World world)
        => world.Towns
            .OrderBy(town => town.Id.Value, StringComparer.OrdinalIgnoreCase)
            .Select(town => (town.Id.Value, town.Name, town.Services))
            .ToArray();

    private static (string Id, string FromTownId, string ToTownId, TrailRisk Risk, TrailTerrain Terrain, WaterFeature WaterFeature, decimal RideDayDistance)[] SnapshotTrails(World world)
        => world.Trails
            .OrderBy(trail => trail.Id.Value, StringComparer.OrdinalIgnoreCase)
            .Select(trail => (trail.Id.Value, trail.FromTownId.Value, trail.ToTownId.Value, trail.Risk, trail.Terrain, trail.WaterFeature, trail.RideDayDistance))
            .ToArray();
}
