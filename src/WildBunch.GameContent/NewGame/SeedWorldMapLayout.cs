using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public sealed record SeedMapTown(string Id, string Name, TownServices Services, int X, int Y);

public sealed record SeedMapTrailEdge(string Id, string FromTownId, string ToTownId, decimal RideDayDistance);

public static class SeedWorldMapLayout
{
    private const int CenterX = 400;
    private const int CenterY = 250;
    private const int RingRadius = 200;

    public static IReadOnlyList<SeedMapTown> GetMapTowns(World world, MapLayoutPalette layout)
    {
        var towns = world.Towns.ToArray();
        return towns
            .Select((town, index) =>
            {
                var (x, y) = GetCoordinatesForSlot(index, towns.Length, layout);
                return new SeedMapTown(town.Id.Value, town.Name, town.Services, x, y);
            })
            .ToArray();
    }

    public static IReadOnlyList<SeedMapTown> GetMapTowns()
    {
        var world = SeedWorldCatalog.CreateCanonicalWorld();
        return GetMapTowns(world, MapLayoutPalette.HubAndSpoke);
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

    private static (int X, int Y) GetCoordinatesForSlot(int slotIndex, int totalTowns, MapLayoutPalette layout)
    {
        return layout switch
        {
            MapLayoutPalette.HubAndSpoke => GetHubAndSpokeCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.LinearChain => GetLinearChainCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.Ring => GetRingCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.DoubleLine => GetDoubleLineCoordinates(slotIndex, totalTowns),
            _ => throw new ArgumentOutOfRangeException(nameof(layout), $"Unknown map layout palette: {layout}")
        };
    }

    private static (int X, int Y) GetHubAndSpokeCoordinates(int slotIndex, int totalTowns)
    {
        if (slotIndex == 0) return (CenterX, CenterY);
        var angle = (slotIndex - 1) * (2.0 * Math.PI / Math.Max(1, totalTowns - 1));
        var x = (int)(CenterX + RingRadius * Math.Cos(angle));
        var y = (int)(CenterY + RingRadius * Math.Sin(angle));
        return (x, y);
    }

    private static (int X, int Y) GetLinearChainCoordinates(int slotIndex, int totalTowns)
    {
        var spacing = 60;
        var startX = CenterX - ((totalTowns - 1) * spacing) / 2;
        var x = startX + slotIndex * spacing;
        var y = CenterY;
        return (x, y);
    }

    private static (int X, int Y) GetRingCoordinates(int slotIndex, int totalTowns)
    {
        var angle = slotIndex * (2.0 * Math.PI / totalTowns);
        var x = (int)(CenterX + RingRadius * Math.Cos(angle));
        var y = (int)(CenterY + RingRadius * Math.Sin(angle));
        return (x, y);
    }

    private static (int X, int Y) GetDoubleLineCoordinates(int slotIndex, int totalTowns)
    {
        var spacing = 60;
        var mid = totalTowns / 2;
        var offset = 80; // Vertical offset between the two lines

        if (slotIndex < mid)
        {
            // Top line
            var startX = CenterX - ((mid - 1) * spacing) / 2;
            var x = startX + slotIndex * spacing;
            var y = CenterY - offset;
            return (x, y);
        }
        else
        {
            // Bottom line
            var startX = CenterX - ((totalTowns - mid - 1) * spacing) / 2;
            var x = startX + (slotIndex - mid) * spacing;
            var y = CenterY + offset;
            return (x, y);
        }
    }
}
