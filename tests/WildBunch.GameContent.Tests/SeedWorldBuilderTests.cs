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
        var world = BuildSeedWorld(SeedWorldResolver.Resolve(CreateSeedCode(1, 0, 1, 3, 0, tail: 17)));

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
        var world = BuildSeedWorld(SeedWorldResolver.Resolve(CreateSeedCode(2, 0, 1, 3, 0, tail: 19)));

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

    [Fact]
    public void DifferentTownSetKeysProduceDifferentWorldMaps()
    {
        // TownSetKey is a seed-owned map generation parameter. The alternate town set
        // replaces openpass with coppercreek and trail-pine-openpass with
        // trail-pine-coppercreek (different terrain, water, distance, and services).
        // This test proves that changing TownSetKey produces an observably different
        // generated world — not merely different case/turf/cash outcomes.
        var defaultWorld = BuildSeedWorld(new SeedWorld(
            Guid.Empty,
            SeedWorldVariant.Canonical,
            GameSetupDeterministicLabels.WorldTownSetDefault,
            AccusationIndex: 0,
            DefaultCulpritIndex: 3,
            CashBonus: 0));

        var alternateWorld = BuildSeedWorld(new SeedWorld(
            Guid.Empty,
            SeedWorldVariant.Canonical,
            GameSetupDeterministicLabels.WorldTownSetAlternate,
            AccusationIndex: 0,
            DefaultCulpritIndex: 3,
            CashBonus: 0));

        var defaultTowns = SnapshotTowns(defaultWorld);
        var alternateTowns = SnapshotTowns(alternateWorld);
        var defaultTrails = SnapshotTrails(defaultWorld);
        var alternateTrails = SnapshotTrails(alternateWorld);

        // Town sets differ: default has openpass, alternate has coppercreek.
        Assert.Contains(("openpass", "Open Pass", TownServices.None), defaultTowns);
        Assert.DoesNotContain(("openpass", "Open Pass", TownServices.None), alternateTowns);
        Assert.Contains(("coppercreek", "Copper Creek", TownServices.Supplies), alternateTowns);
        Assert.DoesNotContain(("coppercreek", "Copper Creek", TownServices.Supplies), defaultTowns);

        // Trail graphs differ: default has trail-pine-openpass, alternate has trail-pine-coppercreek.
        Assert.Contains(("trail-pine-openpass", "pinecross", "openpass", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m), defaultTrails);
        Assert.DoesNotContain(("trail-pine-openpass", "pinecross", "openpass", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m), alternateTrails);
        Assert.Contains(("trail-pine-coppercreek", "pinecross", "coppercreek", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.Spring, 4m), alternateTrails);
        Assert.DoesNotContain(("trail-pine-coppercreek", "pinecross", "coppercreek", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.Spring, 4m), defaultTrails);

        // Full snapshots are not equal.
        Assert.NotEqual(defaultTowns, alternateTowns);
        Assert.NotEqual(defaultTrails, alternateTrails);
    }

    [Fact]
    public void AlternateTownSetForCanonicalWorldHasCorrectSnapshot()
    {
        var world = BuildSeedWorld(new SeedWorld(
            Guid.Empty,
            SeedWorldVariant.Canonical,
            GameSetupDeterministicLabels.WorldTownSetAlternate,
            AccusationIndex: 0,
            DefaultCulpritIndex: 3,
            CashBonus: 0));

        Assert.Equal(
            new[]
            {
                ("coppercreek", "Copper Creek", TownServices.Supplies),
                ("dryfork", "Dry Fork", TownServices.None),
                ("emberfall", "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph),
                ("hardpan", "Hardpan", TownServices.None),
                ("holloway", "Holloway", TownServices.Doctor),
                ("pinecross", "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard),
                ("redmesa", "Red Mesa", TownServices.Supplies | TownServices.Telegraph),
                ("sagewell", "Sagewell", TownServices.Supplies | TownServices.Doctor),
            },
            SnapshotTowns(world));
        Assert.Equal(
            new[]
            {
                ("trail-hollow-sage", "holloway", "sagewell", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 3m),
                ("trail-pine-coppercreek", "pinecross", "coppercreek", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.Spring, 4m),
                ("trail-pine-hardpan", "pinecross", "hardpan", TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 3m),
                ("trail-pine-hollow", "pinecross", "holloway", TrailRisk.Moderate, TrailTerrain.OpenRange, WaterFeature.Creek, 2m),
                ("trail-pine-red", "pinecross", "redmesa", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
                ("trail-red-dry", "redmesa", "dryfork", TrailRisk.High, TrailTerrain.OpenRange, WaterFeature.Creek, 5m),
                ("trail-red-ember", "redmesa", "emberfall", TrailRisk.High, TrailTerrain.OpenRange, WaterFeature.Creek, 5m),
                ("trail-red-sage", "redmesa", "sagewell", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 3m),
                ("trail-sage-ember", "sagewell", "emberfall", TrailRisk.Moderate, TrailTerrain.OpenRange, WaterFeature.Creek, 5m),
            },
            SnapshotTrails(world));
    }

    [Fact]
    public void StartingTownPolicyDefaultsToPinecrossForAllVariants()
    {
        // Starting town is NOT seed-owned. The safe default from StartingTownPolicy
        // is pinecross for all world variants — it is a fixed property of the world
        // catalog, not a hash of the seed code.
        var canonicalWorld = SeedWorldBuilder.CreateCanonicalWorld();
        Assert.Equal(new TownId("pinecross"), StartingTownPolicy.ResolveStartingTown(canonicalWorld, null));

        var frontierWorld = BuildSeedWorld(SeedWorldResolver.Resolve(CreateSeedCode(1, 0, 1, 3, 0, tail: 17)));
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

    private static Guid CreateSeedCode(byte worldVariant, byte townSetKey, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong tail)
        => SeedWorldSeedCodeFactory.CreateSeedCode(worldVariant, townSetKey, accusationIndex, defaultCulpritIndex, cashBonus, tail);

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
