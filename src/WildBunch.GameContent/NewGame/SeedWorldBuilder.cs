using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal sealed record SeedWorldSetup(World World, TownId StartingTownId);

internal static class SeedWorldBuilder
{
    public static SeedWorldSetup CreateCanonicalWorld()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);
        var sagewell = new Town(new TownId("sagewell"), "Sagewell", TownServices.Supplies | TownServices.Doctor);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var emberfall = new Town(new TownId("emberfall"), "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph);

        var world = new World(
            new[] { pinecross, redmesa, holloway, sagewell, dryfork, emberfall },
            new[]
            {
                new Trail(new TrailId("trail-pine-red"), pinecross.Id, redmesa.Id, TrailRisk.Low, rideDayDistance: 4m),
                new Trail(new TrailId("trail-pine-hollow"), pinecross.Id, holloway.Id, TrailRisk.Moderate),
                new Trail(new TrailId("trail-red-sage"), redmesa.Id, sagewell.Id, TrailRisk.Low, rideDayDistance: 3m),
                new Trail(new TrailId("trail-red-dry"), redmesa.Id, dryfork.Id, TrailRisk.High, rideDayDistance: 5m),
                new Trail(new TrailId("trail-hollow-sage"), holloway.Id, sagewell.Id, TrailRisk.Low, rideDayDistance: 3m),
                new Trail(new TrailId("trail-sage-ember"), sagewell.Id, emberfall.Id, TrailRisk.Moderate, rideDayDistance: 5m),
                new Trail(new TrailId("trail-red-ember"), redmesa.Id, emberfall.Id, TrailRisk.High, rideDayDistance: 5m)
            });

        return new SeedWorldSetup(world, pinecross.Id);
    }

    public static SeedWorldSetup CreateWorld(string seedCode, TravelRulesProfile travelRulesProfile, GameSetupOptionsV1 options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        ArgumentNullException.ThrowIfNull(options);

        return Roll(seedCode, "world-variant") % 2 == 0
            ? CreateFrontierVariant(seedCode, options)
            : CreateRailVariant(seedCode, options);
    }

    private static SeedWorldSetup CreateFrontierVariant(string seedCode, GameSetupOptionsV1 options)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor | TownServices.NoticeBoard);
        var sagewell = new Town(new TownId("sagewell"), "Sagewell", TownServices.Supplies | TownServices.Doctor);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var emberfall = new Town(new TownId("emberfall"), "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph);

        var world = new World(
            new[] { pinecross, redmesa, holloway, sagewell, dryfork, emberfall },
            new[]
            {
                new Trail(new TrailId("trail-pine-red"), pinecross.Id, redmesa.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
                new Trail(new TrailId("trail-pine-hollow"), pinecross.Id, holloway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring, 2m),
                new Trail(new TrailId("trail-red-sage"), redmesa.Id, sagewell.Id, TrailRisk.Low, TrailTerrain.Hills, WaterFeature.Creek, 3m),
                new Trail(new TrailId("trail-red-dry"), redmesa.Id, dryfork.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m),
                new Trail(new TrailId("trail-hollow-sage"), holloway.Id, sagewell.Id, TrailRisk.Low, TrailTerrain.Hills, WaterFeature.River, 3m),
                new Trail(new TrailId("trail-sage-ember"), sagewell.Id, emberfall.Id, TrailRisk.Moderate, TrailTerrain.Mountains, WaterFeature.Spring, 5m),
                new Trail(new TrailId("trail-hollow-ember"), holloway.Id, emberfall.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m)
            });

        return new SeedWorldSetup(world, PickStartingTown(seedCode, world, options));
    }

    private static SeedWorldSetup CreateRailVariant(string seedCode, GameSetupOptionsV1 options)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph | TownServices.NoticeBoard);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);
        var sagewell = new Town(new TownId("sagewell"), "Sagewell", TownServices.Supplies | TownServices.Doctor | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var emberfall = new Town(new TownId("emberfall"), "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph);

        var world = new World(
            new[] { pinecross, redmesa, holloway, sagewell, dryfork, emberfall },
            new[]
            {
                new Trail(new TrailId("trail-pine-red"), pinecross.Id, redmesa.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
                new Trail(new TrailId("trail-pine-hollow"), pinecross.Id, holloway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring, 2m),
                new Trail(new TrailId("trail-red-sage"), redmesa.Id, sagewell.Id, TrailRisk.Low, TrailTerrain.Hills, WaterFeature.Creek, 3m),
                new Trail(new TrailId("trail-red-dry"), redmesa.Id, dryfork.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m),
                new Trail(new TrailId("trail-hollow-sage"), holloway.Id, sagewell.Id, TrailRisk.Low, TrailTerrain.Hills, WaterFeature.River, 3m),
                new Trail(new TrailId("trail-sage-ember"), sagewell.Id, emberfall.Id, TrailRisk.Moderate, TrailTerrain.Mountains, WaterFeature.Spring, 5m),
                new Trail(new TrailId("trail-hollow-ember"), holloway.Id, emberfall.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 5m)
            });

        return new SeedWorldSetup(world, PickStartingTown(seedCode, world, options));
    }

    private static TownId PickStartingTown(string seedCode, World world, GameSetupOptionsV1 options)
    {
        var candidates = world.Towns
            .Where(town => (town.Services & TownServices.Supplies) != 0 || (town.Services & TownServices.NoticeBoard) != 0)
            .OrderBy(town => town.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidates.Length == 0)
        {
            return world.Towns.First().Id;
        }

        var label = options.StartWithHorse ? "starting-town-horse" : "starting-town-foot";
        var index = (int)(Roll(seedCode, label) % (ulong)candidates.Length);
        return candidates[index].Id;
    }

    private static ulong Roll(string seedCode, string label)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seedCode}|{label}"));
        return BitConverter.ToUInt64(bytes, 0);
    }
}
