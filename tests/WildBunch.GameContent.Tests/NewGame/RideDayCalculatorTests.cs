// tests/WildBunch.GameContent.Tests/NewGame/RideDayCalculatorTests.cs
using WildBunch.GameContent.NewGame;
using Xunit;

public class RideDayCalculatorTests
{
    [Fact]
    public void CalculateRideDays_ConvertsPixelDistanceToRideDays()
    {
        const double CoordinateScale = 25.0; // 1 ride-day per 25 coordinate units
        var edge = new TrailEdgeCandidate(0, 1, 50.0); // 50 pixels = 2 ride days
        
        var rideDays = RideDayCalculator.CalculateRideDays(edge, CoordinateScale, outlierSlot: null);
        
        Assert.Equal(2m, rideDays);
    }

    [Fact]
    public void CalculateRideDays_ClampsToNormalRange()
    {
        const double CoordinateScale = 25.0;
        var shortEdge = new TrailEdgeCandidate(0, 1, 10.0); // Should clamp to 2
        var longEdge = new TrailEdgeCandidate(0, 1, 200.0); // Should clamp to 5
        
        var shortDays = RideDayCalculator.CalculateRideDays(shortEdge, CoordinateScale, outlierSlot: null);
        var longDays = RideDayCalculator.CalculateRideDays(longEdge, CoordinateScale, outlierSlot: null);
        
        Assert.Equal(2m, shortDays);
        Assert.Equal(5m, longDays);
    }

    [Fact]
    public void CalculateRideDays_OutlierTrailGetsSixDays()
    {
        const double CoordinateScale = 25.0;
        var edge = new TrailEdgeCandidate(0, 1, 50.0);
        
        var rideDays = RideDayCalculator.CalculateRideDays(edge, CoordinateScale, outlierSlot: 0);
        
        Assert.Equal(6m, rideDays);
    }
}
