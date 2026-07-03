// tests/WildBunch.GameContent.Tests/NewGame/TrailEdgeFilterTests.cs
using WildBunch.GameContent.NewGame;
using Xunit;

public class TrailEdgeFilterTests
{
    [Fact]
    public void FilterCrossingEdges_RemovesEdgesThatCrossExistingTrails()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (0, 0),
            [1] = (10, 10),
            [2] = (0, 10),
            [3] = (10, 0)
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var accepted = new List<TrailEdgeCandidate>
        {
            candidates.First(e => e.FromSlot == 0 && e.ToSlot == 3) // (0,0) to (10,0)
        };

        var filtered = TrailEdgeFilter.FilterCrossingEdges(candidates, accepted, coordinates);
        
        // Edge (0,10) to (10,0) should be removed as it crosses (0,0) to (10,0)
        var crossingEdge = filtered.FirstOrDefault(e => e.FromSlot == 2 && e.ToSlot == 3);
        Assert.Null(crossingEdge);
    }

    [Fact]
    public void FilterParallelCorridors_RemovesCloselyParallelEdges()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (0, 0),
            [1] = (100, 0),
            [2] = (0, 10),
            [3] = (100, 10)
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var accepted = new List<TrailEdgeCandidate>
        {
            candidates.First(e => e.FromSlot == 0 && e.ToSlot == 1) // (0,0) to (100,0)
        };

        var filtered = TrailEdgeFilter.FilterParallelCorridors(candidates, accepted, coordinates, threshold: 0.1);
        
        // Edge (2,10) to (3,10) should be removed as it's parallel to (0,0) to (100,0)
        var parallelEdge = filtered.FirstOrDefault(e => e.FromSlot == 2 && e.ToSlot == 3);
        Assert.Null(parallelEdge);
    }

    [Fact]
    public void FilterRedundantRoutes_RemovesDirectEdgesWhenIndirectRouteExists()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (0, 0),
            [1] = (50, 0),
            [2] = (100, 0)
        };

        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(coordinates);
        var accepted = new List<TrailEdgeCandidate>
        {
            candidates.First(e => e.FromSlot == 0 && e.ToSlot == 1),
            candidates.First(e => e.FromSlot == 1 && e.ToSlot == 2)
        };

        var filtered = TrailEdgeFilter.FilterRedundantRoutes(candidates, accepted, coordinates);
        
        // Edge (0,0) to (100,0) should be removed as 0->1->2 already exists
        var redundantEdge = filtered.FirstOrDefault(e => e.FromSlot == 0 && e.ToSlot == 2);
        Assert.Null(redundantEdge);
    }
}
