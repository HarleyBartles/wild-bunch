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

    // Minimum pixel distance between any two placed towns. This must be at least
    // MinNormalRideDays * CoordinateScale (2 * 25 = 50) so that a 2-day trail is
    // genuinely 50px+ on the map and the visual ratio between 2-day and 5-day
    // trails is at least 2.5:1. Without this, towns packed within 20-40px of each
    // other get clamped up to "2 days" but look visually identical to 1-day gaps.
    private const double MinTownSeparation = 50.0;

    public static (Dictionary<int, (int X, int Y)> Towns, Dictionary<int, int> ClusterAssignments) Place(
        SeedWorld seedWorld, GameSetupDeterministicSource source, GameEntropy entropy, SaltSource? saltSource)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(source);

        var clusterCenters = DeriveClusterCenters(seedWorld.ClusterCount, source);
        var clusterAssignments = AssignTownsToClusters(seedWorld.TownCount, seedWorld.ClusterCount, entropy, source, saltSource);
        var towns = PlaceTownsInClusters(seedWorld.TownCount, clusterCenters, clusterAssignments, entropy, source, saltSource);

        return (towns, clusterAssignments);
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
                // Minimum spread is 50px so that intra-cluster town pairs are at
                // least MinTownSeparation apart from the cluster center, ensuring
                // 2-day trails are visually distinct from longer trails.
                var (minSpread, maxSpread) = entropy switch
                {
                    GameEntropy.Classic => (50, 100),
                    GameEntropy.Adventurous => (50, 140),
                    GameEntropy.Wild => (50, 180),
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

        // Post-placement separation pass: push apart any town pairs that are closer
        // than MinTownSeparation. This catches cases where clamping to map bounds
        // or the Wild entropy 2x multiplier squeezed towns together.
        EnforceMinTownSeparation(towns);

        return towns;
    }

    /// <summary>
    /// Iteratively pushes apart any town pairs closer than MinTownSeparation.
    /// Each pair is moved apart along the line connecting them, by half the
    /// shortfall each. Positions are re-clamped to map bounds after each pass.
    /// Repeats up to 10 times to resolve cascading overlaps.
    /// </summary>
    private static void EnforceMinTownSeparation(Dictionary<int, (int X, int Y)> towns)
    {
        const int maxPasses = 10;
        var slots = towns.Keys.OrderBy(s => s).ToArray();

        for (var pass = 0; pass < maxPasses; pass++)
        {
            var moved = false;
            for (var i = 0; i < slots.Length; i++)
            {
                for (var j = i + 1; j < slots.Length; j++)
                {
                    var a = towns[slots[i]];
                    var b = towns[slots[j]];
                    var dx = b.X - a.X;
                    var dy = b.Y - a.Y;
                    var dist = Math.Sqrt(dx * dx + dy * dy);

                    if (dist >= MinTownSeparation) continue;

                    // Push each town apart by half the shortfall along the connecting line.
                    // If both towns are at the same position, nudge apart along the X axis.
                    var shortfall = MinTownSeparation - dist;
                    if (dist < 0.01)
                    {
                        var nudge = (int)(shortfall / 2);
                        towns[slots[i]] = (ClampToBounds(a.X - nudge, MapWidth), a.Y);
                        towns[slots[j]] = (ClampToBounds(b.X + nudge, MapWidth), b.Y);
                    }
                    else
                    {
                        var push = shortfall / 2 / dist;
                        towns[slots[i]] = (
                            ClampToBounds((int)(a.X - dx * push), MapWidth),
                            ClampToBounds((int)(a.Y - dy * push), MapHeight));
                        towns[slots[j]] = (
                            ClampToBounds((int)(b.X + dx * push), MapWidth),
                            ClampToBounds((int)(b.Y + dy * push), MapHeight));
                    }
                    moved = true;
                }
            }
            if (!moved) break;
        }
    }

    private static int ClampToBounds(int value, int max) => Math.Max(0, Math.Min(max, value));
}
