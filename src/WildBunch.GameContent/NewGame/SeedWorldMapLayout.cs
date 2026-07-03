using WildBunch.Domain.World;
using WildBunch.Domain.Travel;
using WildBunch.Domain.Game;

namespace WildBunch.GameContent.NewGame;

public sealed record SeedMapTown(string Id, string Name, TownServices Services, int X, int Y);

public sealed record SeedMapTrailEdge(string Id, string FromTownId, string ToTownId, decimal RideDayDistance);

public static class SeedWorldMapLayout
{
    public static IReadOnlyList<SeedMapTown> GetMapTowns(World world)
    {
        return world.Towns
            .Select(town => new SeedMapTown(town.Id.Value, town.Name, town.Services, town.MapX, town.MapY))
            .ToArray();
    }

    public static IReadOnlyList<SeedMapTown> GetMapTowns()
    {
        var world = SeedWorldCatalog.CreateCanonicalWorld();
        return GetMapTowns(world);
    }

    public static IReadOnlyList<SeedMapTrailEdge> GetMapTrails()
    {
        var world = SeedWorldCatalog.CreateCanonicalWorld();
        return GetMapTrails(world);
    }

    public static IReadOnlyList<SeedMapTrailEdge> GetMapTrails(World world)
    {
        return world.Trails
            .Select(trail => new SeedMapTrailEdge(
                trail.Id.Value,
                trail.FromTownId.Value,
                trail.ToTownId.Value,
                trail.RideDayDistance))
            .ToArray();
    }
}
