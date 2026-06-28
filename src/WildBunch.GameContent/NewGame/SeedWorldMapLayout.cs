using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public sealed record SeedMapTown(string Id, string Name, TownServices Services, int X, int Y);

public sealed record SeedMapTrailEdge(string Id, string FromTownId, string ToTownId, decimal RideDayDistance);

public static class SeedWorldMapLayout
{
    private static readonly IReadOnlyDictionary<string, (int X, int Y)> TownCoordinates =
        new Dictionary<string, (int X, int Y)>
        {
            ["pinecross"] = (150, 500),
            ["redmesa"] = (450, 400),
            ["holloway"] = (300, 650),
            ["sagewell"] = (600, 550),
            ["dryfork"] = (700, 300),
            ["emberfall"] = (800, 500),
            ["hardpan"] = (100, 300),
            ["openpass"] = (80, 700),
            ["coppercreek"] = (120, 720)
        };

    public static IReadOnlyList<SeedMapTown> GetMapTowns()
    {
        var world = SeedWorldCatalog.CreateWorld(SeedWorldVariant.Canonical, GameSetupDeterministicLabels.WorldTownSetDefault);
        return world.Towns
            .Select(town =>
            {
                var (x, y) = TownCoordinates[town.Id.Value];
                return new SeedMapTown(town.Id.Value, town.Name, town.Services, x, y);
            })
            .ToArray();
    }

    public static IReadOnlyList<SeedMapTrailEdge> GetMapTrails()
    {
        var world = SeedWorldCatalog.CreateWorld(SeedWorldVariant.Canonical, GameSetupDeterministicLabels.WorldTownSetDefault);
        return world.Trails
            .Select(trail => new SeedMapTrailEdge(
                trail.Id.Value,
                trail.FromTownId.Value,
                trail.ToTownId.Value,
                trail.RideDayDistance))
            .ToArray();
    }
}
