// tests/WildBunch.GameContent.Tests/NewGame/TrailEdgeGeneratorTests.cs
using System.Linq;
using WildBunch.GameContent.NewGame;
using Xunit;

public class TrailEdgeGeneratorTests
{
    [Fact]
    public void GenerateCandidateEdges_CreatesAllPossibleEdges()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250),
            [2] = (400, 50)
        };

        var edges = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        
        // Should have 3 choose 2 = 3 edges
        Assert.Equal(3, edges.Count);
        
        // Verify all pairs are present
        Assert.Contains(edges, e => e.FromSlot == 0 && e.ToSlot == 1);
        Assert.Contains(edges, e => e.FromSlot == 0 && e.ToSlot == 2);
        Assert.Contains(edges, e => e.FromSlot == 1 && e.ToSlot == 2);
    }

    [Fact]
    public void GenerateCandidateEdges_CalculatesCorrectPixelDistances()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (0, 0),
            [1] = (100, 0)
        };

        var edges = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var edge = edges.First(e => e.FromSlot == 0 && e.ToSlot == 1);
        
        Assert.Equal(100.0, edge.PixelDistance, 0.01);
    }
}
