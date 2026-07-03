// tests/WildBunch.Domain.Tests/World/TrailGeometryTests.cs
using System.Numerics;
using WildBunch.Domain.World;
using Xunit;

public class TrailGeometryTests
{
    [Fact]
    public void CalculatePixelDistance_ReturnsCorrectDistance()
    {
        var from = new Vector2(0, 0);
        var to = new Vector2(100, 0);
        var distance = TrailGeometry.CalculatePixelDistance(from, to);
        Assert.Equal(100.0, distance, 0.01);
    }

    [Fact]
    public void LinesIntersect_DetectsCrossingLines()
    {
        var line1 = (From: new Vector2(0, 0), To: new Vector2(10, 10));
        var line2 = (From: new Vector2(0, 10), To: new Vector2(10, 0));
        Assert.True(TrailGeometry.LinesIntersect(line1.From, line1.To, line2.From, line2.To));
    }

    [Fact]
    public void LinesIntersect_ReturnsFalseForNonCrossingLines()
    {
        var line1 = (From: new Vector2(0, 0), To: new Vector2(10, 0));
        var line2 = (From: new Vector2(0, 5), To: new Vector2(10, 5));
        Assert.False(TrailGeometry.LinesIntersect(line1.From, line1.To, line2.From, line2.To));
    }

    [Fact]
    public void AreLinesParallel_DetectsParallelLines()
    {
        var line1 = (From: new Vector2(0, 0), To: new Vector2(10, 0));
        var line2 = (From: new Vector2(0, 5), To: new Vector2(10, 5));
        Assert.True(TrailGeometry.AreLinesParallel(line1.From, line1.To, line2.From, line2.To, threshold: 0.1));
    }

    [Fact]
    public void AreLinesParallel_ReturnsFalseForNonParallelLines()
    {
        var line1 = (From: new Vector2(0, 0), To: new Vector2(10, 0));
        var line2 = (From: new Vector2(0, 0), To: new Vector2(10, 10));
        Assert.False(TrailGeometry.AreLinesParallel(line1.From, line1.To, line2.From, line2.To, threshold: 0.1));
    }
}
