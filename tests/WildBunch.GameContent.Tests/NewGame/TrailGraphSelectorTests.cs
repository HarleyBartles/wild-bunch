// tests/WildBunch.GameContent.Tests/NewGame/TrailGraphSelectorTests.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;
using Xunit;

public class TrailGraphSelectorTests
{
    [Fact]
    public void SelectConnectedGraph_ProducesConnectedGraph()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250),
            [2] = (400, 50)
        };

        var townNames = new List<TownNameEntry>
        {
            new(0, "town-0", "Town 0"),
            new(1, "town-1", "Town 1"),
            new(2, "town-2", "Town 2")
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var selected = TrailGraphSelector.SelectConnectedGraph(
            candidates,
            coordinates,
            townNames,
            coordinates.Count,
            GameEntropy.Boring,
            null,
            source);

        Assert.True(IsConnected(selected, coordinates.Count));
    }

    [Fact]
    public void SelectConnectedGraph_IsDeterministicForBoringMode()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250),
            [2] = (400, 50)
        };

        var townNames = new List<TownNameEntry>
        {
            new(0, "town-0", "Town 0"),
            new(1, "town-1", "Town 1"),
            new(2, "town-2", "Town 2")
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var selected1 = TrailGraphSelector.SelectConnectedGraph(
            candidates,
            coordinates,
            townNames,
            coordinates.Count,
            GameEntropy.Boring,
            null,
            source);
        
        var selected2 = TrailGraphSelector.SelectConnectedGraph(
            candidates,
            coordinates,
            townNames,
            coordinates.Count,
            GameEntropy.Boring,
            null,
            source);

        Assert.Equal(selected1.Count, selected2.Count);
        foreach (var edge in selected1)
        {
            Assert.Contains(selected2, e => e.FromSlot == edge.FromSlot && e.ToSlot == edge.ToSlot);
        }
    }

    [Fact]
    public void SelectConnectedGraph_AvoidsCrossingEdgesWhenAlternativeExists()
    {
        // Create a diamond pattern where crossing is avoidable
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),  // Center
            [1] = (200, 100),  // Top-left
            [2] = (600, 100),  // Top-right
            [3] = (200, 400),  // Bottom-left
            [4] = (600, 400)   // Bottom-right
        };

        var townNames = new List<TownNameEntry>
        {
            new(0, "town-0", "Town 0"),
            new(1, "town-1", "Town 1"),
            new(2, "town-2", "Town 2"),
            new(3, "town-3", "Town 3"),
            new(4, "town-4", "Town 4")
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var selected = TrailGraphSelector.SelectConnectedGraph(
            candidates,
            coordinates,
            townNames,
            coordinates.Count,
            GameEntropy.Boring,
            null,
            source);

        // Verify no edges cross
        for (var i = 0; i < selected.Count; i++)
        {
            for (var j = i + 1; j < selected.Count; j++)
            {
                Assert.False(TrailEdgeFilter.EdgesCross(selected[i], selected[j], coordinates),
                    $"Edges {selected[i].FromSlot}-{selected[i].ToSlot} and {selected[j].FromSlot}-{selected[j].ToSlot} should not cross");
            }
        }

        Assert.True(IsConnected(selected, coordinates.Count));
    }

    [Fact]
    public void SelectConnectedGraph_AvoidsParallelCorridorsWhenAlternativeExists()
    {
        // Create towns in a line where parallel corridors are avoidable
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (100, 250),
            [1] = (300, 250),
            [2] = (500, 250),
            [3] = (700, 250)
        };

        var townNames = new List<TownNameEntry>
        {
            new(0, "town-0", "Town 0"),
            new(1, "town-1", "Town 1"),
            new(2, "town-2", "Town 2"),
            new(3, "town-3", "Town 3")
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var selected = TrailGraphSelector.SelectConnectedGraph(
            candidates,
            coordinates,
            townNames,
            coordinates.Count,
            GameEntropy.Boring,
            null,
            source);

        // Verify no parallel corridors
        for (var i = 0; i < selected.Count; i++)
        {
            for (var j = i + 1; j < selected.Count; j++)
            {
                Assert.False(TrailEdgeFilter.AreParallelCorridors(selected[i], selected[j], coordinates),
                    $"Edges {selected[i].FromSlot}-{selected[i].ToSlot} and {selected[j].FromSlot}-{selected[j].ToSlot} should not be parallel");
            }
        }

        Assert.True(IsConnected(selected, coordinates.Count));
    }

    [Fact]
    public void SelectConnectedGraph_AvoidsRedundantRoutesWhenIndirectRouteExists()
    {
        // Create a triangle where adding the third edge would be redundant
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (200, 100),
            [2] = (600, 100)
        };

        var townNames = new List<TownNameEntry>
        {
            new(0, "town-0", "Town 0"),
            new(1, "town-1", "Town 1"),
            new(2, "town-2", "Town 2")
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var selected = TrailGraphSelector.SelectConnectedGraph(
            candidates,
            coordinates,
            townNames,
            coordinates.Count,
            GameEntropy.Boring,
            null,
            source);

        // For a triangle, we should get exactly 2 edges (minimum spanning tree)
        // The third edge would be redundant since it creates an indirect route
        Assert.Equal(2, selected.Count);
        Assert.True(IsConnected(selected, coordinates.Count));
    }

    [Fact]
    public void SelectConnectedGraph_FailsLoudlyWhenCannotConnectAllTowns()
    {
        // Create a scenario where connectivity is impossible
        // This is a degenerate case - in practice town placement should prevent this
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (100, 100),
            [1] = (900, 100)  // Very far apart
        };

        var townNames = new List<TownNameEntry>
        {
            new(0, "town-0", "Town 0"),
            new(1, "town-1", "Town 1")
        };

        // Manually create candidates with no connecting edges
        var candidates = new List<TrailEdgeCandidate>();
        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            TrailGraphSelector.SelectConnectedGraph(
                candidates,
                coordinates,
                townNames,
                coordinates.Count,
                GameEntropy.Boring,
                null,
                source);
        });

        Assert.Contains("Failed to build connected trail graph", exception.Message);
    }

    private bool IsConnected(IReadOnlyList<TrailEdgeCandidate> edges, int townCount)
    {
        if (townCount == 0) return true;
        
        var adjacency = new Dictionary<int, List<int>>();
        for (var i = 0; i < townCount; i++)
        {
            adjacency[i] = new List<int>();
        }

        foreach (var edge in edges)
        {
            adjacency[edge.FromSlot].Add(edge.ToSlot);
            adjacency[edge.ToSlot].Add(edge.FromSlot);
        }

        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(0);
        visited.Add(0);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in adjacency[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return visited.Count == townCount;
    }
}
