using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

internal static class ClusterPlacementGenerator
{
    private const int MapWidth = 800;
    private const int MapHeight = 500;
    private const int Padding = 50;
    private const double MinClusterCenterSeparation = 150.0;
    private const int MaxClusterCenterRetries = 10;
    private const double OutlierPlacementDistance = 150.0;

    public static (Dictionary<int, (int X, int Y)> Towns, Dictionary<int, int> ClusterAssignments, int? OutlierSlot) Place(
        SeedWorld seedWorld, GameSetupDeterministicSource source, GameEntropy entropy, SaltSource? saltSource)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(source);

        var clusterCenters = DeriveClusterCenters(seedWorld.ClusterCount, source);
        var clusterAssignments = AssignTownsToClusters(seedWorld.TownCount, seedWorld.ClusterCount, entropy, source, saltSource);
        var towns = PlaceTownsInClusters(seedWorld.TownCount, clusterCenters, clusterAssignments, entropy, source, saltSource);

        int? outlierSlot = null;
        if (seedWorld.OutlierSlotType == 1 && entropy != GameEntropy.Boring)
        {
            outlierSlot = seedWorld.TownCount;
            towns[outlierSlot.Value] = PlaceOutlierTown(towns, source, saltSource, entropy);
            clusterAssignments[outlierSlot.Value] = -1;
        }

        return (towns, clusterAssignments, outlierSlot);
    }

    private static List<(int X, int Y)> DeriveClusterCenters(int clusterCount, GameSetupDeterministicSource source)
    {
        var centers = new List<(int X, int Y)>(clusterCount);
        var usableWidth = MapWidth - 2 * Padding;
        var usableHeight = MapHeight - 2 * Padding;

        for (var i = 0; i < clusterCount; i++)
        {
            (int X, int Y) candidate = default;
            for (var retry = 0; retry <= MaxClusterCenterRetries; retry++)
            {
                var label = retry == 0 ? $"cluster-center-{i}" : $"cluster-center-{i}-retry-{retry}";
                var roll = source.Roll(label);
                var x = Padding + (int)(roll % (ulong)usableWidth);
                var y = Padding + (int)((roll >> 32) % (ulong)usableHeight);
                candidate = (x, y);

                if (IsFarEnoughFromExisting(candidate, centers)) break;
            }

            if (!IsFarEnoughFromExisting(candidate, centers) && centers.Count > 0)
                candidate = ClampToMinSeparation(candidate, centers);

            centers.Add(candidate);
        }

        return centers;
    }

    private static bool IsFarEnoughFromExisting((int X, int Y) candidate, List<(int X, int Y)> existing)
    {
        foreach (var c in existing)
        {
            var dx = c.X - candidate.X;
            var dy = c.Y - candidate.Y;
            if (Math.Sqrt(dx * dx + dy * dy) < MinClusterCenterSeparation)
                return false;
        }
        return true;
    }

    private static (int X, int Y) ClampToMinSeparation((int X, int Y) candidate, List<(int X, int Y)> existing)
    {
        var (nearestX, nearestY) = existing.OrderBy(c =>
        {
            var dx = c.X - candidate.X;
            var dy = c.Y - candidate.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }).First();

        var dx = candidate.X - nearestX;
        var dy = candidate.Y - nearestY;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance == 0)
            return (Math.Min(MapWidth - Padding, nearestX + (int)MinClusterCenterSeparation), nearestY);

        var scale = MinClusterCenterSeparation / distance;
        var x = (int)(nearestX + dx * scale);
        var y = (int)(nearestY + dy * scale);
        return (ClampToBounds(x, MapWidth), ClampToBounds(y, MapHeight));
    }

    private static Dictionary<int, int> AssignTownsToClusters(int townCount, int clusterCount, GameEntropy entropy,
        GameSetupDeterministicSource source, SaltSource? saltSource)
    {
        var assignments = new Dictionary<int, int>(townCount);
        if (entropy == GameEntropy.Boring)
        {
            for (var slot = 0; slot < townCount; slot++)
                assignments[slot] = slot % clusterCount;
            return assignments;
        }

        var salt = saltSource?.Salt ?? "default";
        for (var slot = 0; slot < townCount; slot++)
        {
            var baseCluster = slot % clusterCount;
            var roll = source.Roll($"town-cluster-{slot}-{salt}");
            var offset = (int)(roll % (ulong)clusterCount);
            assignments[slot] = (baseCluster + offset) % clusterCount;
        }
        return assignments;
    }

    private static Dictionary<int, (int X, int Y)> PlaceTownsInClusters(int townCount,
        List<(int X, int Y)> clusterCenters, Dictionary<int, int> clusterAssignments, GameEntropy entropy,
        GameSetupDeterministicSource source, SaltSource? saltSource)
    {
        var towns = new Dictionary<int, (int X, int Y)>(townCount);
        var salt = saltSource?.Salt ?? "default";

        for (var slot = 0; slot < townCount; slot++)
        {
            var clusterIndex = clusterAssignments[slot];
            var center = clusterCenters[clusterIndex];

            int xOffset, yOffset;
            if (entropy == GameEntropy.Boring)
            {
                var roll = source.Roll($"town-offset-{slot}");
                var angle = (roll % 360UL) * (Math.PI / 180.0);
                xOffset = (int)(60.0 * Math.Cos(angle));
                yOffset = (int)(60.0 * Math.Sin(angle));
            }
            else
            {
                var (minSpread, maxSpread) = entropy switch
                {
                    GameEntropy.Classic => (40, 80),
                    GameEntropy.Adventurous => (40, 120),
                    GameEntropy.Wild => (20, 160),
                    _ => (60, 60)
                };

                var roll = source.Roll($"town-offset-{slot}-{salt}");
                var angle = (roll % 360UL) * (Math.PI / 180.0);
                var spreadRange = (ulong)(maxSpread - minSpread + 1);
                var spread = minSpread + (int)((roll >> 32) % spreadRange);

                xOffset = (int)(spread * Math.Cos(angle));
                yOffset = (int)(spread * Math.Sin(angle));

                if (entropy == GameEntropy.Wild && (roll & 0x7UL) == 0x7UL)
                {
                    xOffset *= 2;
                    yOffset *= 2;
                }
            }

            towns[slot] = (ClampToBounds(center.X + xOffset, MapWidth), ClampToBounds(center.Y + yOffset, MapHeight));
        }

        return towns;
    }

    private static (int X, int Y) PlaceOutlierTown(Dictionary<int, (int X, int Y)> existingTowns,
        GameSetupDeterministicSource source, SaltSource? saltSource, GameEntropy entropy)
    {
        var nearest = existingTowns.Values.OrderBy(t =>
        {
            var dx = t.X - existingTowns[0].X;
            var dy = t.Y - existingTowns[0].Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }).First();

        var roll = source.Roll($"outlier-angle-{saltSource?.Salt ?? "default"}");
        var angle = (roll % 360UL) * (Math.PI / 180.0);
        var x = (int)(nearest.X + OutlierPlacementDistance * Math.Cos(angle));
        var y = (int)(nearest.Y + OutlierPlacementDistance * Math.Sin(angle));
        return (ClampToBounds(x, MapWidth), ClampToBounds(y, MapHeight));
    }

    private static int ClampToBounds(int value, int max) => Math.Max(0, Math.Min(max, value));
}
