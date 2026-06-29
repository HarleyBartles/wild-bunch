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

    // Deterministic fixed seed GUIDs proven to produce different town counts,
    // selections, and trail graphs. See SeedWorldResolverTests for the full set.
    private static readonly Guid SeedSixTowns = new(0x00000001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);   // Rail, 6 towns, 5 trails
    private static readonly Guid SeedEightTowns = new(0x00000002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);  // Rail, 8 towns, 9 trails
    private static readonly Guid SeedSevenTowns = new(0x00000003, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);  // Rail, 7 towns, 7 trails
    private static readonly Guid SeedCanonicalSeven = new(0x00000005, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0); // Canonical, 7 towns, 6 trails

    private static readonly Guid[] DeterministicSeeds =
    [
        SeedSixTowns, SeedEightTowns, SeedSevenTowns, SeedCanonicalSeven,
        new(0x00000004, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000006, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000007, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000008, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000009, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000a, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000b, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000c, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000d, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000e, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000010, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000011, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000014, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
    ];

    [Fact]
    public void DifferentSeedsCanProduceDifferentTownSelections()
    {
        // The seed deterministically derives which towns are selected from the catalog.
        // Different fixed seeds produce different town selections (not just different
        // case/turf/cash outcomes).
        var selections = new HashSet<string>();
        foreach (var seed in DeterministicSeeds)
        {
            var seedWorld = SeedWorldResolver.Resolve(seed);
            selections.Add(string.Join(",", seedWorld.SelectedTownIds.OrderBy(id => id)));
        }
        Assert.True(selections.Count >= 2, $"Expected at least 2 different town selections, got {selections.Count}");
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTrailSignatures()
    {
        // Different town selections produce different trail graphs.
        var signatures = new HashSet<string>();
        foreach (var seed in DeterministicSeeds)
        {
            var seedWorld = SeedWorldResolver.Resolve(seed);
            var sig = string.Join(",", seedWorld.Trails.Select(t => t.Id).OrderBy(id => id));
            signatures.Add(sig);
        }
        Assert.True(signatures.Count >= 2, $"Expected at least 2 different trail signatures, got {signatures.Count}");
    }

    [Fact]
    public void SameSeedProducesSameWorld()
    {
        // Same seed + same difficulty should produce the same resolved map.
        var seed = SeedSevenTowns;
        var seedWorld = SeedWorldResolver.Resolve(seed);
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seed));
        var world1 = SeedWorldBuilder.CreateWorld(seedWorld, source);
        var world2 = SeedWorldBuilder.CreateWorld(seedWorld, source);
        Assert.Equal(world1.Towns.Count, world2.Towns.Count);
        Assert.Equal(world1.Trails.Count, world2.Trails.Count);
    }

    [Fact]
    public void SelectedStartingTownMustBeInGeneratedWorld()
    {
        // Starting town is NOT seed-owned but must be in the generated world.
        // StartingTownPolicy rejects a town that is not in the world.
        // SeedSixTowns produces 6 towns (not all 8), so we can test with a
        // catalog town that was not selected.
        var seedWorld = SeedWorldResolver.Resolve(SeedSixTowns);
        Assert.True(seedWorld.SelectedTownIds.Count < SeedWorldCatalog.AllTowns.Count,
            "SeedSixTowns should produce fewer than 8 towns.");

        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode));
        var world = SeedWorldBuilder.CreateWorld(seedWorld, source);
        var nonSelectedTown = SeedWorldCatalog.AllTowns.First(t => !seedWorld.SelectedTownIds.Contains(t.Id));

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
