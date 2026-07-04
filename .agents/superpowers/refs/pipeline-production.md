# Core Pipeline — Production Code Reference

## 1. TrailEdge

**File:** `src/WildBunch.GameContent/NewGame/TrailEdge.cs`

```csharp
namespace WildBunch.GameContent.NewGame;

internal sealed record TrailEdge(int FromSlot, int ToSlot, double PixelDistance)
{
    public (int Low, int High) OrderedSlots
        => FromSlot <= ToSlot ? (FromSlot, ToSlot) : (ToSlot, FromSlot);
}
```

---

## 2. ClusterPlacementGenerator

**File:** `src/WildBunch.GameContent/NewGame/ClusterPlacementGenerator.cs`

```csharp
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
```

---

## 3. TrailGraphGenerator

**File:** `src/WildBunch.GameContent/NewGame/TrailGraphGenerator.cs`

```csharp
using DelaunatorSharp;

namespace WildBunch.GameContent.NewGame;

internal static class TrailGraphGenerator
{
    private const double RedundantCorridorPerpendicularThreshold = 15.0;
    private const double CloseParallelAngleDegrees = 15.0;
    private const double CloseParallelSeparationPx = 30.0;

    public static IReadOnlyList<TrailEdge> Generate(SeedWorld seedWorld,
        Dictionary<int, (int X, int Y)> towns, Dictionary<int, int> clusterAssignments,
        GameSetupDeterministicSource source, GameEntropy entropy, SaltSource? saltSource)
    {
        var allEdges = ComputeAllEdges(towns);
        var delaunayEdges = ComputeDelaunayEdges(towns);
        var mst = ComputeMst(delaunayEdges, towns.Count);
        var candidates = delaunayEdges.Where(e => !mst.Any(m => m.OrderedSlots == e.OrderedSlots)).ToList();
        var extras = SelectExtras(seedWorld, entropy, saltSource, source, candidates);
        var accepted = mst.Concat(extras).ToList();
        accepted = ApplyRedundantCorridorFilter(accepted, towns);
        accepted = ApplyCloseParallelFilter(accepted, towns);
        accepted = RepairConnectivityIfNeeded(accepted, delaunayEdges, towns.Count);
        return accepted;
    }

    private static List<TrailEdge> ComputeAllEdges(Dictionary<int, (int X, int Y)> towns)
    {
        var edges = new List<TrailEdge>();
        var slots = towns.Keys.OrderBy(s => s).ToArray();
        for (var i = 0; i < slots.Length; i++)
        {
            for (var j = i + 1; j < slots.Length; j++)
            {
                var (a, b) = (slots[i], slots[j]);
                var dx = towns[a].X - towns[b].X;
                var dy = towns[a].Y - towns[b].Y;
                edges.Add(new TrailEdge(a, b, Math.Sqrt(dx * dx + dy * dy)));
            }
        }
        return edges;
    }

    private static List<TrailEdge> ComputeDelaunayEdges(Dictionary<int, (int X, int Y)> towns)
    {
        var slots = towns.Keys.OrderBy(s => s).ToArray();
        var points = new IPoint[slots.Length];
        for (var i = 0; i < slots.Length; i++)
        {
            var (x, y) = towns[slots[i]];
            points[i] = new Point(i, x, y);
        }

        var delaunator = new Delaunator(points);
        var edgeSet = new HashSet<(int, int)>();
        var edges = new List<TrailEdge>();

        for (var h = 0; h < delaunator.Triangles.Length; h++)
        {
            var opposite = delaunator.Halfedges[h];
            if (opposite != -1 && h > opposite) continue;

            var a = delaunator.Triangles[h];
            var b = delaunator.Triangles[h % 3 == 2 ? h - 2 : h + 1];

            var slotA = slots[a];
            var slotB = slots[b];
            var key = slotA < slotB ? (slotA, slotB) : (slotB, slotA);
            if (!edgeSet.Add(key)) continue;

            var dx = towns[slotA].X - towns[slotB].X;
            var dy = towns[slotA].Y - towns[slotB].Y;
            edges.Add(new TrailEdge(slotA, slotB, Math.Sqrt(dx * dx + dy * dy)));
        }

        return edges;
    }

    private static List<TrailEdge> ComputeMst(List<TrailEdge> edges, int townCount)
    {
        var sorted = edges.OrderBy(e => e.PixelDistance).ThenBy(e => e.OrderedSlots).ToList();
        var parent = new int[townCount + 1];
        for (var i = 0; i < parent.Length; i++) parent[i] = i;

        var mst = new List<TrailEdge>();
        foreach (var edge in sorted)
        {
            var rootA = Find(parent, edge.FromSlot);
            var rootB = Find(parent, edge.ToSlot);
            if (rootA == rootB) continue;
            parent[rootA] = rootB;
            mst.Add(edge);
            if (mst.Count == townCount - 1) break;
        }
        return mst;
    }

    private static int Find(int[] parent, int slot)
    {
        while (parent[slot] != slot)
        {
            parent[slot] = parent[parent[slot]];
            slot = parent[slot];
        }
        return slot;
    }

    private static List<TrailEdge> SelectExtras(SeedWorld seedWorld, GameEntropy entropy,
        SaltSource? saltSource, GameSetupDeterministicSource source, List<TrailEdge> candidates)
    {
        if (candidates.Count == 0) return new List<TrailEdge>();

        if (entropy == GameEntropy.Boring && seedWorld.GraphDensity == GraphDensity.Sparse)
            return new List<TrailEdge>();

        if (entropy == GameEntropy.Boring && seedWorld.GraphDensity == GraphDensity.Dense)
        {
            var medianDistance = candidates[candidates.Count / 2].PixelDistance;
            return candidates.Where(e => e.PixelDistance <= medianDistance).ToList();
        }

        var (minExtras, maxExtras) = (entropy, seedWorld.GraphDensity) switch
        {
            (GameEntropy.Classic, GraphDensity.Sparse) => (1, 2),
            (GameEntropy.Classic, GraphDensity.Dense) => (2, 4),
            (GameEntropy.Adventurous, GraphDensity.Sparse) => (1, 2),
            (GameEntropy.Adventurous, GraphDensity.Dense) => (3, 5),
            (GameEntropy.Wild, GraphDensity.Sparse) => (2, 3),
            (GameEntropy.Wild, GraphDensity.Dense) => (4, 6),
            _ => (0, 0)
        };

        var salt = saltSource?.Salt ?? "default";
        var count = minExtras + (int)(source.Roll($"extra-edges-count-{salt}") % (ulong)(maxExtras - minExtras + 1));
        count = Math.Min(count, candidates.Count);

        var selected = new List<TrailEdge>(count);
        var available = candidates.ToList();
        for (var i = 0; i < count && available.Count > 0; i++)
        {
            var roll = source.Roll($"extra-edge-{i}-{salt}");
            var index = (int)(roll % (ulong)available.Count);
            selected.Add(available[index]);
            available.RemoveAt(index);
        }
        return selected;
    }

    private static List<TrailEdge> ApplyRedundantCorridorFilter(List<TrailEdge> edges, Dictionary<int, (int X, int Y)> towns)
    {
        var accepted = edges.ToList();
        var slotPairs = new HashSet<(int, int)>(accepted.Select(e => e.OrderedSlots));

        foreach (var edge in edges.ToList())
        {
            var (a, c) = edge.OrderedSlots;
            foreach (var slot in towns.Keys)
            {
                if (slot == a || slot == c) continue;
                var (bToA, bToC) = (OrderedPair(slot, a), OrderedPair(slot, c));
                if (!slotPairs.Contains(bToA) || !slotPairs.Contains(bToC)) continue;

                if (PerpendicularDistance(towns[slot], towns[a], towns[c]) <= RedundantCorridorPerpendicularThreshold)
                {
                    accepted.Remove(edge);
                    slotPairs.Remove(edge.OrderedSlots);
                    break;
                }
            }
        }
        return accepted;
    }

    private static List<TrailEdge> ApplyCloseParallelFilter(List<TrailEdge> edges, Dictionary<int, (int X, int Y)> towns)
    {
        var accepted = new List<TrailEdge>();
        foreach (var edge in edges.OrderBy(e => e.PixelDistance))
        {
            var isParallel = accepted.Any(kept => AreCloseParallel(edge, kept, towns));
            if (!isParallel) accepted.Add(edge);
        }
        return accepted;
    }

    private static List<TrailEdge> RepairConnectivityIfNeeded(List<TrailEdge> edges, List<TrailEdge> delaunayEdges, int townCount)
    {
        while (true)
        {
            var adjacency = BuildAdjacency(edges, townCount);
            var unreachable = FindUnreachableTown(adjacency, townCount);
            if (unreachable < 0) break;

            var reconnect = delaunayEdges
                .Where(e => !edges.Any(x => x.OrderedSlots == e.OrderedSlots))
                .Where(e => e.FromSlot == unreachable || e.ToSlot == unreachable)
                .OrderBy(e => e.PixelDistance)
                .FirstOrDefault();

            if (reconnect == null) break;
            edges.Add(reconnect);
        }
        return edges;
    }

    private static Dictionary<int, HashSet<int>> BuildAdjacency(List<TrailEdge> edges, int townCount)
    {
        var adjacency = new Dictionary<int, HashSet<int>>();
        for (var i = 0; i < townCount; i++) adjacency[i] = new HashSet<int>();
        foreach (var edge in edges)
        {
            adjacency[edge.FromSlot].Add(edge.ToSlot);
            adjacency[edge.ToSlot].Add(edge.FromSlot);
        }
        return adjacency;
    }

    private static int FindUnreachableTown(Dictionary<int, HashSet<int>> adjacency, int townCount)
    {
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(0);
        visited.Add(0);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!adjacency.ContainsKey(current)) continue;
            foreach (var neighbor in adjacency[current])
                if (visited.Add(neighbor)) queue.Enqueue(neighbor);
        }
        for (var i = 0; i < townCount; i++)
            if (!visited.Contains(i)) return i;
        return -1;
    }

    private static double PerpendicularDistance((int X, int Y) point, (int X, int Y) a, (int X, int Y) c)
    {
        var acX = c.X - a.X;
        var acY = c.Y - a.Y;
        var apX = point.X - a.X;
        var apY = point.Y - a.Y;
        var cross = Math.Abs(acX * apY - acY * apX);
        var acLen = Math.Sqrt(acX * acX + acY * acY);
        return acLen > 0 ? cross / acLen : double.MaxValue;
    }

    private static bool AreCloseParallel(TrailEdge a, TrailEdge b, Dictionary<int, (int X, int Y)> towns)
    {
        var a1 = towns[a.FromSlot];
        var a2 = towns[a.ToSlot];
        var b1 = towns[b.FromSlot];
        var b2 = towns[b.ToSlot];

        var angleA = Math.Atan2(a2.Y - a1.Y, a2.X - a1.X);
        var angleB = Math.Atan2(b2.Y - b1.Y, b2.X - b1.X);
        var angleDiff = Math.Abs(angleA - angleB) * (180.0 / Math.PI);
        if (angleDiff > 180) angleDiff = 360 - angleDiff;
        if (angleDiff > CloseParallelAngleDegrees) return false;

        var midA = ((a1.X + a2.X) / 2.0, (a1.Y + a2.Y) / 2.0);
        var midB = ((b1.X + b2.X) / 2.0, (b1.Y + b2.Y) / 2.0);
        var sep = Math.Sqrt(Math.Pow(midA.Item1 - midB.Item1, 2) + Math.Pow(midA.Item2 - midB.Item2, 2));
        return sep < CloseParallelSeparationPx;
    }

    private static (int, int) OrderedPair(int a, int b) => a <= b ? (a, b) : (b, a);

    private sealed class Point : IPoint
    {
        public Point(int index, double x, double y)
        {
            Index = index;
            X = x;
            Y = y;
        }
        public int Index { get; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}
```

---

## 4. TerrainAssigner

**File:** `src/WildBunch.GameContent/NewGame/TerrainAssigner.cs`

```csharp
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class TerrainAssigner
{
    private const double CoordinateScale = 25.0; // 25px = 1 ride-day
    private const int MinNormalRideDays = 2;
    private const int MaxNormalRideDays = 5;
    private const int InterClusterShortDayThreshold = 4;

    public static IReadOnlyList<SeedWorldTrail> Assign(
        IReadOnlyList<TrailEdge> edges,
        Dictionary<int, (int X, int Y)> towns,
        Dictionary<int, int> clusterAssignments,
        SeedWorldVariant variant,
        IReadOnlyList<string> townIds,
        int? outlierSlot)
    {
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(towns);
        ArgumentNullException.ThrowIfNull(clusterAssignments);
        ArgumentNullException.ThrowIfNull(townIds);

        var trails = new List<SeedWorldTrail>(edges.Count);
        foreach (var edge in edges)
        {
            var fromId = townIds[edge.FromSlot];
            var toId = townIds[edge.ToSlot];
            var isOutlier = outlierSlot.HasValue && (edge.FromSlot == outlierSlot.Value || edge.ToSlot == outlierSlot.Value);
            var (terrain, water, risk, rideDays) = ClassifyEdge(
                edge, clusterAssignments, variant, isOutlier);

            var trailId = $"trail-{edge.FromSlot}-{edge.ToSlot}";
            trails.Add(new SeedWorldTrail(
                trailId, fromId, toId, risk, terrain, water, rideDays));
        }
        return trails;
    }

    private static (TrailTerrain Terrain, WaterFeature Water, TrailRisk Risk, decimal RideDays) ClassifyEdge(
        TrailEdge edge,
        Dictionary<int, int> clusterAssignments,
        SeedWorldVariant variant,
        bool isOutlier)
    {
        if (isOutlier)
        {
            return (TrailTerrain.Mountains, WaterFeature.None, TrailRisk.High, 6m);
        }

        var rawRideDays = edge.PixelDistance / CoordinateScale;
        var rideDays = Math.Clamp(
            Math.Round((decimal)rawRideDays, MidpointRounding.AwayFromZero),
            MinNormalRideDays, MaxNormalRideDays);

        var sameCluster = clusterAssignments.TryGetValue(edge.FromSlot, out var cA)
            && clusterAssignments.TryGetValue(edge.ToSlot, out var cB)
            && cA == cB && cA != -1;

        if (sameCluster)
        {
            var terrain = variant == SeedWorldVariant.Canonical ? TrailTerrain.OpenRange : TrailTerrain.Hills;
            return (terrain, WaterFeature.Creek, TrailRisk.Low, rideDays);
        }

        if (rideDays <= InterClusterShortDayThreshold)
        {
            var terrain = variant == SeedWorldVariant.Canonical ? TrailTerrain.Badlands : TrailTerrain.Hills;
            return (terrain, WaterFeature.None, TrailRisk.Moderate, rideDays);
        }

        return (TrailTerrain.Mountains, WaterFeature.None, TrailRisk.High, rideDays);
    }
}
```

---

## 5. OutlierGuarantee

**File:** `src/WildBunch.GameContent/NewGame/OutlierGuarantee.cs`

```csharp
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class OutlierGuarantee
{
    private const double OutlierTargetDistancePx = 150.0;

    public static (IReadOnlyList<SeedWorldTrail> Trails, Dictionary<int, (int X, int Y)> Towns) Enforce(
        IReadOnlyList<SeedWorldTrail> trails,
        Dictionary<int, (int X, int Y)> towns,
        int? outlierSlot,
        IReadOnlyList<string> townIds)
    {
        ArgumentNullException.ThrowIfNull(trails);
        ArgumentNullException.ThrowIfNull(towns);
        ArgumentNullException.ThrowIfNull(townIds);

        if (!outlierSlot.HasValue) return (trails, towns);

        var outlier = outlierSlot.Value;
        var incident = trails.Select((t, i) => (Index: i, Trail: t))
            .Where(x => x.Trail.FromTownId == townIds[outlier] || x.Trail.ToTownId == townIds[outlier])
            .ToList();

        if (incident.Count == 0) return (trails, towns);

        var kept = incident.OrderBy(x => x.Trail.RideDayDistance).First();
        var resultTrails = trails.Where((t, i) => i == kept.Index || !incident.Any(x => x.Index == i)).ToList();

        var connectedSlot = kept.Trail.FromTownId == townIds[outlier]
            ? townIds.IndexOf(kept.Trail.ToTownId)
            : townIds.IndexOf(kept.Trail.FromTownId);

        var adjustedTowns = new Dictionary<int, (int X, int Y)>(towns);
        var neighbor = towns[connectedSlot];
        var outlierPos = towns[outlier];
        var dx = outlierPos.X - neighbor.X;
        var dy = outlierPos.Y - neighbor.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (Math.Abs(distance - OutlierTargetDistancePx) > 0.5)
        {
            if (distance == 0)
            {
                adjustedTowns[outlier] = (neighbor.X + (int)OutlierTargetDistancePx, neighbor.Y);
            }
            else
            {
                var scale = OutlierTargetDistancePx / distance;
                adjustedTowns[outlier] = (
                    (int)(neighbor.X + dx * scale),
                    (int)(neighbor.Y + dy * scale));
            }
        }

        return (resultTrails, adjustedTowns);
    }
}
```

---

## 6. MapGenerator

**File:** `src/WildBunch.GameContent/NewGame/MapGenerator.cs`

```csharp
namespace WildBunch.GameContent.NewGame;

internal static class MapGenerator
{
    public static World Generate(SeedWorld seedWorld, GameSetupDeterministicSource source,
        GameEntropy entropy, SaltSource? saltSource)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(source);

        var townNames = SeedWorldCatalog.DeriveTownNames(
            seedWorld.WorldVariant,
            seedWorld.TownCount,
            seedWorld.AccusationIndex,
            seedWorld.DefaultCulpritIndex,
            seedWorld.CashBonus,
            seedWorld.ProsperityPalette,
            seedWorld.ServicesPalette);

        var placement = ClusterPlacementGenerator.Place(seedWorld, source, entropy, saltSource);
        var edges = TrailGraphGenerator.Generate(seedWorld, placement.Towns, placement.ClusterAssignments,
            source, entropy, saltSource);
        var townIds = townNames.Select(t => t.Id).ToArray();
        var trails = TerrainAssigner.Assign(edges, placement.Towns, placement.ClusterAssignments,
            seedWorld.WorldVariant, townIds, placement.OutlierSlot);
        var (enforcedTrails, adjustedTowns) = OutlierGuarantee.Enforce(
            trails, placement.Towns, placement.OutlierSlot, townIds);

        return SeedWorldCatalog.CreateWorld(
            seedWorld.WorldVariant,
            townNames,
            seedWorld.ServicesPalette,
            seedWorld.ProsperityPalette,
            enforcedTrails,
            townCoordinates: adjustedTowns,
            outlierSlot: placement.OutlierSlot,
            entropy,
            saltSource,
            seedWorld.SeedCode);
    }
}
```