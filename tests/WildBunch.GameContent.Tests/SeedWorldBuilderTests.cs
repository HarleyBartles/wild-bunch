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
        var setup = BuildSeedWorld(StartingWorldDescriptorResolver.Resolve(CreateSeedCode(1, 1, 0, 0, 1, 0, 1, tail: 17)));

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
        var setup = BuildSeedWorld(StartingWorldDescriptorResolver.Resolve(CreateSeedCode(1, 2, 0, 0, 1, 0, 1, tail: 19)));

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
        var horseDescriptor = StartingWorldDescriptorResolver.CreateCanonicalDescriptor() with
        {
            World = new StartingWorldDescriptorWorld(SeedWorldVariant.Frontier, GameSetupDeterministicLabels.WorldStartingTownHorse),
            Player = StartingWorldDescriptorResolver.CreateCanonicalDescriptor().Player with
            {
                StartWithHorse = true,
                Loadout = StartingWorldDescriptorResolver.CreateCanonicalDescriptor().Player.Loadout with
                {
                    IncludeHorse = true,
                    IncludeSaddle = true
                }
            }
        };

        var footDescriptor = horseDescriptor with
        {
            World = horseDescriptor.World with { StartingTownSelectionKey = GameSetupDeterministicLabels.WorldStartingTownFoot },
            Player = horseDescriptor.Player with
            {
                StartWithHorse = false,
                Loadout = horseDescriptor.Player.Loadout with
                {
                    IncludeHorse = false,
                    IncludeSaddle = false
                }
            }
        };

        var horseSetup = BuildSeedWorld(horseDescriptor);
        var footSetup = BuildSeedWorld(footDescriptor);

        Assert.NotEqual(horseSetup.StartingTownId, footSetup.StartingTownId);
        Assert.Equal(GetStartingTownCandidateIds(horseSetup.World), GetStartingTownCandidateIds(footSetup.World));
        Assert.Contains(horseSetup.StartingTownId.Value, GetStartingTownCandidateIds(horseSetup.World));
        Assert.Contains(footSetup.StartingTownId.Value, GetStartingTownCandidateIds(footSetup.World));
    }

    private static SeedWorldSetup BuildSeedWorld(StartingWorldDescriptor descriptor)
        => SeedWorldBuilder.CreateWorld(StartingWorldGenerationPlan.Create(descriptor));

    private static Guid CreateSeedCode(byte byte0, byte byte1, byte byte2, byte byte3, byte byte4, byte byte5, byte byte6, ulong tail)
        => StartingWorldDescriptorSeedCodeFactory.CreateSeedCode(byte0, byte1, byte2, byte3, byte4, byte5, byte6, tail);

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
