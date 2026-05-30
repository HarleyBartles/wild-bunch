using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldBuilderTests
{
    [Fact]
    public void CreateCanonicalWorldUsesTheSharedCatalog()
    {
        var setup = SeedWorldBuilder.CreateCanonicalWorld();

        Assert.Equal(new TownId("pinecross"), setup.StartingTownId);
        Assert.Equal(
            new[]
            {
                ("dryfork", "Dry Fork", TownServices.None),
                ("emberfall", "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph),
                ("holloway", "Holloway", TownServices.Doctor),
                ("pinecross", "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard),
                ("redmesa", "Red Mesa", TownServices.Supplies | TownServices.Telegraph),
                ("sagewell", "Sagewell", TownServices.Supplies | TownServices.Doctor),
            },
            SnapshotTowns(setup.World));
        Assert.Equal(
            new[]
            {
                ("trail-hollow-sage", "holloway", "sagewell", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 3m),
                ("trail-pine-hollow", "pinecross", "holloway", TrailRisk.Moderate, TrailTerrain.OpenRange, WaterFeature.Creek, 2m),
                ("trail-pine-red", "pinecross", "redmesa", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
                ("trail-red-dry", "redmesa", "dryfork", TrailRisk.High, TrailTerrain.OpenRange, WaterFeature.Creek, 5m),
                ("trail-red-ember", "redmesa", "emberfall", TrailRisk.High, TrailTerrain.OpenRange, WaterFeature.Creek, 5m),
                ("trail-red-sage", "redmesa", "sagewell", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 3m),
                ("trail-sage-ember", "sagewell", "emberfall", TrailRisk.Moderate, TrailTerrain.OpenRange, WaterFeature.Creek, 5m),
            },
            SnapshotTrails(setup.World));
    }

    [Fact]
    public void CreateFrontierWorldUsesTheSharedCatalogAndFrontierOverlay()
    {
        var setup = FindSeedWorld(SeedWorldVariant.Frontier, startWithHorse: true);

        Assert.Equal(
            new[]
            {
                ("dryfork", "Dry Fork", TownServices.None),
                ("emberfall", "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph),
                ("holloway", "Holloway", TownServices.Doctor | TownServices.NoticeBoard),
                ("pinecross", "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard),
                ("redmesa", "Red Mesa", TownServices.Supplies | TownServices.Telegraph),
                ("sagewell", "Sagewell", TownServices.Supplies | TownServices.Doctor),
            },
            SnapshotTowns(setup.World));
        Assert.Equal(
            new[]
            {
                ("trail-hollow-sage", "holloway", "sagewell", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.River, 3m),
                ("trail-pine-hollow", "pinecross", "holloway", TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring, 2m),
                ("trail-pine-red", "pinecross", "redmesa", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
                ("trail-red-dry", "redmesa", "dryfork", TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m),
                ("trail-red-ember", "redmesa", "emberfall", TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m),
                ("trail-red-sage", "redmesa", "sagewell", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.Creek, 3m),
                ("trail-sage-ember", "sagewell", "emberfall", TrailRisk.Moderate, TrailTerrain.Mountains, WaterFeature.Spring, 5m),
            },
            SnapshotTrails(setup.World));

        Assert.Equal(
            new[] { "emberfall", "holloway", "pinecross", "redmesa", "sagewell" },
            GetStartingTownCandidateIds(setup.World));
        Assert.Contains(setup.StartingTownId.Value, GetStartingTownCandidateIds(setup.World));
    }

    [Fact]
    public void CreateRailWorldUsesTheSharedCatalogAndRailOverlay()
    {
        var setup = FindSeedWorld(SeedWorldVariant.Rail, startWithHorse: true);

        Assert.Equal(
            new[]
            {
                ("dryfork", "Dry Fork", TownServices.None),
                ("emberfall", "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph),
                ("holloway", "Holloway", TownServices.Doctor),
                ("pinecross", "Pinecross", TownServices.Supplies | TownServices.Lodging),
                ("redmesa", "Red Mesa", TownServices.Supplies | TownServices.Telegraph | TownServices.NoticeBoard),
                ("sagewell", "Sagewell", TownServices.Supplies | TownServices.Doctor | TownServices.NoticeBoard),
            },
            SnapshotTowns(setup.World));
        Assert.Equal(
            new[]
            {
                ("trail-hollow-sage", "holloway", "sagewell", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.River, 3m),
                ("trail-pine-hollow", "pinecross", "holloway", TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring, 2m),
                ("trail-pine-red", "pinecross", "redmesa", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
                ("trail-red-dry", "redmesa", "dryfork", TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m),
                ("trail-red-ember", "redmesa", "emberfall", TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m),
                ("trail-red-sage", "redmesa", "sagewell", TrailRisk.Low, TrailTerrain.Hills, WaterFeature.Creek, 3m),
                ("trail-sage-ember", "sagewell", "emberfall", TrailRisk.Moderate, TrailTerrain.Mountains, WaterFeature.Spring, 5m),
            },
            SnapshotTrails(setup.World));

        Assert.Equal(
            new[] { "emberfall", "pinecross", "redmesa", "sagewell" },
            GetStartingTownCandidateIds(setup.World));
        Assert.Contains(setup.StartingTownId.Value, GetStartingTownCandidateIds(setup.World));
    }

    [Fact]
    public void StartingTownSelectionStillUsesDifferentHorseAndFootLabels()
    {
        var (horseSetup, footSetup) = FindSameVariantPairWithDifferentStartingTowns();

        Assert.NotEqual(horseSetup.StartingTownId, footSetup.StartingTownId);
        Assert.Equal(GetStartingTownCandidateIds(horseSetup.World), GetStartingTownCandidateIds(footSetup.World));
        Assert.Contains(horseSetup.StartingTownId.Value, GetStartingTownCandidateIds(horseSetup.World));
        Assert.Contains(footSetup.StartingTownId.Value, GetStartingTownCandidateIds(footSetup.World));
    }

    private static SeedWorldSetup FindSeedWorld(SeedWorldVariant expectedVariant, bool startWithHorse)
    {
        for (ulong entropy = 0; entropy < 20_000; entropy++)
        {
            var setup = BuildSeedWorld(startWithHorse, entropy);
            if (expectedVariant == SeedWorldVariant.Frontier && IsFrontierWorld(setup.World))
            {
                return setup;
            }

            if (expectedVariant == SeedWorldVariant.Rail && IsRailWorld(setup.World))
            {
                return setup;
            }
        }

        throw new InvalidOperationException($"Could not find a {expectedVariant} seed world within the search range.");
    }

    private static (SeedWorldSetup Horse, SeedWorldSetup Foot) FindSameVariantPairWithDifferentStartingTowns()
    {
        for (ulong entropy = 0; entropy < 20_000; entropy++)
        {
            var horseSetup = BuildSeedWorld(startWithHorse: true, entropy);
            var footSetup = BuildSeedWorld(startWithHorse: false, entropy);

            var horseVariant = GetVariant(horseSetup.World);
            var footVariant = GetVariant(footSetup.World);
            if (horseVariant is null || footVariant is null || horseVariant != footVariant)
            {
                continue;
            }

            if (horseSetup.StartingTownId.Equals(footSetup.StartingTownId))
            {
                continue;
            }

            return (horseSetup, footSetup);
        }

        throw new InvalidOperationException("Could not find a seed that preserved the world variant while changing the starting-town label.");
    }

    private static SeedWorldSetup BuildSeedWorld(bool startWithHorse, ulong entropy)
    {
        var seed = new GameSetupSeed(
            GameSetupSeedCodec.CurrentGeneratorVersion,
            TravelDifficulty.Normal,
            new GameSetupOptionsV1(startWithHorse, StartingLoadoutProfile.Standard),
            entropy);
        var seedCode = GameSetupSeedCodec.Encode(seed);
        return SeedWorldBuilder.CreateWorld(seedCode, TravelRulesProfile.For(seed.Difficulty), seed.Options);
    }

    private static SeedWorldVariant? GetVariant(World world)
        => IsFrontierWorld(world) ? SeedWorldVariant.Frontier : IsRailWorld(world) ? SeedWorldVariant.Rail : null;

    private static bool IsFrontierWorld(World world)
        => world.GetTown(new TownId("holloway")).Services.HasFlag(TownServices.NoticeBoard)
            && !world.GetTown(new TownId("redmesa")).Services.HasFlag(TownServices.NoticeBoard);

    private static bool IsRailWorld(World world)
        => world.GetTown(new TownId("redmesa")).Services.HasFlag(TownServices.NoticeBoard)
            && !world.GetTown(new TownId("holloway")).Services.HasFlag(TownServices.NoticeBoard);

    private static string[] GetStartingTownCandidateIds(World world)
        => world.Towns
            .Where(town => (town.Services & TownServices.Supplies) != 0 || (town.Services & TownServices.NoticeBoard) != 0)
            .OrderBy(town => town.Name, StringComparer.OrdinalIgnoreCase)
            .Select(town => town.Id.Value)
            .ToArray();

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
