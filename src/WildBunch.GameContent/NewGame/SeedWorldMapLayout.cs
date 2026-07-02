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
            .Select(town =>
            {
                return new SeedMapTown(town.Id.Value, town.Name, town.Services, town.MapX, town.MapY);
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

    public static (int X, int Y) GetCoordinatesForSlot(int slotIndex, int totalTowns, MapLayoutPalette layout)
    {
        return layout switch
        {
            MapLayoutPalette.HubAndSpoke => GetHubAndSpokeCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.DoubleLine => GetDoubleLineCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.XShaped => GetXShapedCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.Tree => GetTreeCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.Star => GetStarCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.Cluster => GetClusterCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.Mesh => GetMeshCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.Grid => GetGridCoordinates(slotIndex, totalTowns),
            _ => throw new ArgumentOutOfRangeException(nameof(layout), $"Unknown map layout palette: {layout}")
        };
    }

    public static (int X, int Y) GetHubAndSpokeCoordinates(int slotIndex, int totalTowns)
    {
        if (slotIndex == 0) return (CenterX, CenterY);
        var angle = (slotIndex - 1) * (2.0 * Math.PI / Math.Max(1, totalTowns - 1));
        var x = (int)(CenterX + RingRadius * Math.Cos(angle));
        var y = (int)(CenterY + RingRadius * Math.Sin(angle));
        return (x, y);
    }

    public static (int X, int Y) GetXShapedCoordinates(int slotIndex, int totalTowns)
    {
        // TODO: Implement XShaped coordinates
        throw new NotImplementedException("XShaped coordinates not yet implemented");
    }

    public static (int X, int Y) GetTreeCoordinates(int slotIndex, int totalTowns)
    {
        // TODO: Implement Tree coordinates
        throw new NotImplementedException("Tree coordinates not yet implemented");
    }

    public static (int X, int Y) GetStarCoordinates(int slotIndex, int totalTowns)
    {
        // TODO: Implement Star coordinates
        throw new NotImplementedException("Star coordinates not yet implemented");
    }

    public static (int X, int Y) GetClusterCoordinates(int slotIndex, int totalTowns)
    {
        // TODO: Implement Cluster coordinates
        throw new NotImplementedException("Cluster coordinates not yet implemented");
    }

    public static (int X, int Y) GetMeshCoordinates(int slotIndex, int totalTowns)
    {
        // TODO: Implement Mesh coordinates
        throw new NotImplementedException("Mesh coordinates not yet implemented");
    }

    public static (int X, int Y) GetGridCoordinates(int slotIndex, int totalTowns)
    {
        // TODO: Implement Grid coordinates
        throw new NotImplementedException("Grid coordinates not yet implemented");
    }

    public static (int X, int Y) GetDoubleLineCoordinates(int slotIndex, int totalTowns)
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
