using DelaunatorSharp;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

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
