using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Domain.Tests.World;

public sealed class PathSegmentTests
{
    [Fact]
    public void PathSegment_StoresCoordinates()
    {
        var segment = new PathSegment(10, 20, 30, 40);
        Assert.Equal(10, segment.StartX);
        Assert.Equal(20, segment.StartY);
        Assert.Equal(30, segment.EndX);
        Assert.Equal(40, segment.EndY);
    }
}
