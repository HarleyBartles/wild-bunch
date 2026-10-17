using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Domain.Tests.World;

public sealed class PathSegmentTests
{
    [Fact]
    public void PathSegment_StoresCoordinates()
    {
        var segment = PathSegment.Create(10, 20, 30, 40);
        Assert.Equal(10, segment.StartX);
        Assert.Equal(20, segment.StartY);
        Assert.Equal(30, segment.EndX);
        Assert.Equal(40, segment.EndY);
    }

    [Fact]
    public void PathSegment_WithCoordinateAtBoundary_CreatesSuccessfully()
    {
        var segment = PathSegment.Create(0, 0, 100, 100);
        Assert.Equal(0, segment.StartX);
        Assert.Equal(0, segment.StartY);
        Assert.Equal(100, segment.EndX);
        Assert.Equal(100, segment.EndY);
    }

    [Fact]
    public void PathSegment_WithNegativeStartX_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PathSegment.Create(-1, 50, 50, 50));
    }

    [Fact]
    public void PathSegment_WithNegativeStartY_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PathSegment.Create(50, -1, 50, 50));
    }

    [Fact]
    public void PathSegment_WithNegativeEndX_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PathSegment.Create(50, 50, -1, 50));
    }

    [Fact]
    public void PathSegment_WithNegativeEndY_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PathSegment.Create(50, 50, 50, -1));
    }

    [Fact]
    public void PathSegment_WithStartXExceedingMax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PathSegment.Create(101, 50, 50, 50));
    }

    [Fact]
    public void PathSegment_WithStartYExceedingMax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PathSegment.Create(50, 101, 50, 50));
    }

    [Fact]
    public void PathSegment_WithEndXExceedingMax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PathSegment.Create(50, 50, 101, 50));
    }

    [Fact]
    public void PathSegment_WithEndYExceedingMax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PathSegment.Create(50, 50, 50, 101));
    }

    [Theory]
    [InlineData(-1, 50, 50, 50)]
    [InlineData(101, 50, 50, 50)]
    [InlineData(50, -1, 50, 50)]
    [InlineData(50, 101, 50, 50)]
    [InlineData(50, 50, -1, 50)]
    [InlineData(50, 50, 101, 50)]
    [InlineData(50, 50, 50, -1)]
    [InlineData(50, 50, 50, 101)]
    public void PathSegment_DirectConstructionRejectsCoordinatesOutsideLogicalSurface(
        int startX, int startY, int endX, int endY)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PathSegment(startX, startY, endX, endY));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PathSegment_WithUpdateRejectsCoordinateOutsideLogicalSurface(int coordinate)
    {
        var segment = new PathSegment(10, 20, 30, 40);

        Assert.Throws<ArgumentOutOfRangeException>(() => coordinate switch
        {
            0 => segment with { StartX = 101 },
            1 => segment with { StartY = 101 },
            2 => segment with { EndX = 101 },
            3 => segment with { EndY = 101 },
            _ => throw new ArgumentOutOfRangeException(nameof(coordinate))
        });
    }
}
