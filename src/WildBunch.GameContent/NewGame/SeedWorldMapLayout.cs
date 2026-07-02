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
        if (slotIndex == 0) return (CenterX, CenterY);

        // Four arms: 1=N, 2=E, 3=S, 4=W
        var armIndex = (slotIndex - 1) % 4;
        var armStep = (slotIndex - 1) / 4;
        var armLength = 180;
        var stepSize = 60;

        var angle = armIndex switch
        {
            0 => -Math.PI / 2,  // North
            1 => 0,              // East
            2 => Math.PI / 2,   // South
            3 => Math.PI,       // West
            _ => 0
        };

        var distance = armLength + (armStep * stepSize);
        var x = (int)(CenterX + distance * Math.Cos(angle));
        var y = (int)(CenterY + distance * Math.Sin(angle));
        return (x, y);
    }

    public static (int X, int Y) GetTreeCoordinates(int slotIndex, int totalTowns)
    {
        // Main trunk grows upward, branches extend sideways
        if (slotIndex == 0) return (CenterX, CenterY + 150);

        var trunkLength = Math.Min(4, totalTowns);
        if (slotIndex < trunkLength)
        {
            // Main trunk towns
            var y = CenterY + 150 - (slotIndex * 80);
            return (CenterX, y);
        }
        else
        {
            // Branch towns
            var branchIndex = slotIndex - trunkLength;
            var trunkSlot = (branchIndex % (trunkLength - 1)) + 1;
            var isLeft = branchIndex % 2 == 0;
            var trunkY = CenterY + 150 - (trunkSlot * 80);
            var xOffset = isLeft ? -100 : 100;
            return (CenterX + xOffset, trunkY);
        }
    }

    public static (int X, int Y) GetStarCoordinates(int slotIndex, int totalTowns)
    {
        if (slotIndex == 0) return (CenterX, CenterY);

        // Star pattern: evenly spaced around center
        var angle = (slotIndex - 1) * (2.0 * Math.PI / Math.Max(1, totalTowns - 1));
        var radius = 200;
        var x = (int)(CenterX + radius * Math.Cos(angle));
        var y = (int)(CenterY + radius * Math.Sin(angle));
        return (x, y);
    }

    public static (int X, int Y) GetClusterCoordinates(int slotIndex, int totalTowns)
    {
        // Three clusters arranged in a triangle
        var clusterIndex = slotIndex % 3;
        var clusterSize = (totalTowns + 2) / 3;
        var positionInCluster = slotIndex / 3;

        var clusterCenters = new (int X, int Y)[]
        {
            (CenterX - 150, CenterY - 100),
            (CenterX + 150, CenterY - 100),
            (CenterX, CenterY + 150)
        };

        var (cx, cy) = clusterCenters[clusterIndex];
        var offset = positionInCluster * 50;
        var x = cx + (clusterIndex == 0 ? offset : clusterIndex == 1 ? -offset : 0);
        var y = cy + (clusterIndex == 2 ? offset : 0);

        return (x, y);
    }

    public static (int X, int Y) GetMeshCoordinates(int slotIndex, int totalTowns)
    {
        // Arrange towns in a circle for the fully connected mesh
        var angle = slotIndex * (2.0 * Math.PI / Math.Max(1, totalTowns));
        var radius = 180;
        var x = (int)(CenterX + radius * Math.Cos(angle));
        var y = (int)(CenterY + radius * Math.Sin(angle));
        return (x, y);
    }

    public static (int X, int Y) GetGridCoordinates(int slotIndex, int totalTowns)
    {
        // 3x3 grid layout
        var col = slotIndex % 3;
        var row = slotIndex / 3;
        var spacing = 100;
        var startX = CenterX - spacing;
        var startY = CenterY - spacing;

        var x = startX + col * spacing;
        var y = startY + row * spacing;
        return (x, y);
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
