using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class TrailGraphGeneratorTests
{
    private static GameSetupDeterministicSource NewSource(Guid? seedCode = null)
        => new(SeedWorldResolver.FormatSeedCode(seedCode ?? SeedWorldResolver.CreateCanonicalSeedCode()));

    private static SeedWorld NewSeedWorld(int townCount = 8, int clusterCount = 2, GraphDensity density = GraphDensity.Sparse)
    {
        var base_ = SeedWorldResolver.CreateCanonicalSeedWorld();
        return base_ with { TownCount = townCount, ClusterCount = clusterCount, GraphDensity = density };
    }

    private static (Dictionary<int, (int X, int Y)> Towns, Dictionary<int, int> ClusterAssignments, int? OutlierSlot) PlaceTowns(
        SeedWorld seed, GameEntropy entropy, SaltSource? salt)
    {
        var source = NewSource();
        return ClusterPlacementGenerator.Place(seed, source, entropy, salt);
    }

    [Fact]
    public void Generate_ProducesConnectedGraph()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Sparse);
        var placement = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt"));
        var source = NewSource();

        var edges = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        var adjacency = BuildAdjacency(edges, seed.TownCount);
        Assert.True(IsConnected(adjacency, seed.TownCount), "Generated graph must be connected.");
    }

    [Fact]
    public void Generate_ProducesPlanarGraph_NoCrossings()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Dense);
        var placement = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt"));
        var source = NewSource();

        var edges = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.False(HasCrossing(edges, placement.Towns), "Generated graph must not have crossing edges.");
    }

    [Theory]
    [InlineData(GraphDensity.Sparse)]
    [InlineData(GraphDensity.Dense)]
    public void Generate_Boring_SameSeedProducesSameGraph(GraphDensity density)
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: density);
        var placementA = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt-a"));
        var placementB = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt-b"));
        var source = NewSource();

        var a = TrailGraphGenerator.Generate(seed, placementA.Towns, placementA.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt-a"));
        var b = TrailGraphGenerator.Generate(seed, placementB.Towns, placementB.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt-b"));

        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].OrderedSlots, b[i].OrderedSlots);
            Assert.Equal(a[i].PixelDistance, b[i].PixelDistance);
        }
    }

    [Fact]
    public void Generate_DenseHasAtLeastAsManyEdgesAsSparse()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Sparse);
        var placement = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt"));
        var source = NewSource();

        var sparseEdges = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        var denseSeed = seed with { GraphDensity = GraphDensity.Dense };
        var denseEdges = TrailGraphGenerator.Generate(denseSeed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.True(denseEdges.Count >= sparseEdges.Count,
            $"Dense ({denseEdges.Count} edges) should have at least as many edges as Sparse ({sparseEdges.Count} edges).");
    }

    [Fact]
    public void Generate_SparseBoring_IsExactlyMST()
    {
        // Sparse + Boring = MST only. MST of N nodes has N-1 edges.
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Sparse);
        var placement = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt"));
        var source = NewSource();

        var edges = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.Equal(seed.TownCount - 1, edges.Count);
    }

    [Fact]
    public void Generate_NonBoring_SameSeedSameSaltIsDeterministic()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Sparse);
        var placement = PlaceTowns(seed, GameEntropy.Classic, SaltSource.CreateFixed("deterministic-salt"));
        var source = NewSource();
        var salt = SaltSource.CreateFixed("deterministic-salt");

        var a = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Classic, salt);
        var b = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Classic, salt);

        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].OrderedSlots, b[i].OrderedSlots);
        }
    }



    private static Dictionary<int, HashSet<int>> BuildAdjacency(IReadOnlyList<TrailEdge> edges, int townCount)
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

    private static bool IsConnected(Dictionary<int, HashSet<int>> adjacency, int townCount)
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
            {
                if (visited.Add(neighbor)) queue.Enqueue(neighbor);
            }
        }
        return visited.Count == townCount;
    }

    private static bool HasCrossing(IReadOnlyList<TrailEdge> edges, Dictionary<int, (int X, int Y)> towns)
    {
        for (var i = 0; i < edges.Count; i++)
        {
            for (var j = i + 1; j < edges.Count; j++)
            {
                var a = edges[i];
                var b = edges[j];
                var sharedEndpoint = a.FromSlot == b.FromSlot || a.FromSlot == b.ToSlot
                    || a.ToSlot == b.FromSlot || a.ToSlot == b.ToSlot;
                if (sharedEndpoint) continue;

                var p1 = towns[a.FromSlot];
                var p2 = towns[a.ToSlot];
                var p3 = towns[b.FromSlot];
                var p4 = towns[b.ToSlot];

                if (SegmentsIntersect(p1, p2, p3, p4)) return true;
            }
        }
        return false;
    }

    private static bool SegmentsIntersect((int X, int Y) p1, (int X, int Y) p2, (int X, int Y) p3, (int X, int Y) p4)
    {
        var d1 = Sign(p3, p4, p1);
        var d2 = Sign(p3, p4, p2);
        var d3 = Sign(p1, p2, p3);
        var d4 = Sign(p1, p2, p4);
        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
            && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));

        static int Sign((int X, int Y) a, (int X, int Y) b, (int X, int Y) c)
            => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X) switch
            {
                > 0 => 1,
                < 0 => -1,
                _ => 0
            };
    }
}
