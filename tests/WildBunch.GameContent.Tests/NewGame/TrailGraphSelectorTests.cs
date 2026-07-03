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
