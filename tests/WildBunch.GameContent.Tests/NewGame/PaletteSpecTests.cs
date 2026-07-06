using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class PaletteSpecTests
{
    [Fact]
    public void PaletteSpec_StoresSpurConfiguration()
    {
        var spec = new PaletteSpec(
            SpurCount: 1,
            SpurRows: new[] { 4 },
            SpurDirections: new[] { SpurDirection.East },
            PlacementStrategy: PlacementStrategy.SpreadEvenly);
        
        Assert.Equal(1, spec.SpurCount);
        Assert.Single(spec.SpurRows);
        Assert.Equal(4, spec.SpurRows[0]);
        Assert.Single(spec.SpurDirections);
        Assert.Equal(SpurDirection.East, spec.SpurDirections[0]);
        Assert.Equal(PlacementStrategy.SpreadEvenly, spec.PlacementStrategy);
    }
}
