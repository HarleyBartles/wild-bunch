using WildBunch.Domain.World;
using WildBunch.Domain.Travel;
using WildBunch.Domain.Game;

namespace WildBunch.GameContent.NewGame;

public sealed record SeedMapTown(string Id, string Name, TownServices Services, int X, int Y);

public sealed record SeedMapTrailEdge(string Id, string FromTownId, string ToTownId, decimal RideDayDistance);

public static class SeedWorldMapLayout
{
    private const int CenterX = 400;
    private const int CenterY = 250;
    private const int RingRadius = 200;

    /// <summary>
    /// Derives a deterministic rotation (0-7, representing 0-315 degrees in 45-degree increments)
    /// from the seed code, entropy, and salt. Same seed + same entropy = same rotation.
    /// </summary>
    public static int DeriveRotation(Guid seedCode, GameEntropy entropy, SaltSource? saltSource)
    {
        var salt = saltSource?.Salt ?? "default";
        var hash = ComputeStableHash(seedCode.ToString("D"), entropy.ToString(), salt, "map-rotation");
        return (int)(hash % 8); // 0-7 = 8 rotation axes
    }

    /// <summary>
    /// Rotates coordinates around the center point by the specified number of 45-degree increments.
    /// </summary>
    public static (int X, int Y) RotateCoordinates(int x, int y, int rotation)
    {
        if (rotation == 0) return (x, y);

        // Convert rotation to radians (each step is 45 degrees = PI/4)
        var angle = rotation * (Math.PI / 4.0);

        // Translate to origin
        var dx = x - CenterX;
        var dy = y - CenterY;

        // Apply rotation matrix
        var rotatedX = dx * Math.Cos(angle) - dy * Math.Sin(angle);
        var rotatedY = dx * Math.Sin(angle) + dy * Math.Cos(angle);

        // Translate back from origin
        return ((int)(CenterX + rotatedX), (int)(CenterY + rotatedY));
    }

    /// <summary>
    /// Computes a stable hash from the given inputs using xorshift32.
    /// </summary>
    private static uint ComputeStableHash(params string[] inputs)
    {
        var hash = 0u;
        foreach (var input in inputs)
        {
            foreach (var c in input)
            {
                hash ^= (uint)c;
                hash = hash * 0x1000193u;
                hash ^= hash >> 16;
            }
        }
        return hash;
    }

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
            MapLayoutPalette.Tree => GetTreeCoordinates(slotIndex, totalTowns),
            MapLayoutPalette.Star => GetStarCoordinates(slotIndex, totalTowns),
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
