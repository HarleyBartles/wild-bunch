using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class BuildingLayoutCatalogTests
{
    [Fact]
    public void GetPaletteSpec_ReturnsCorrectConfiguration()
    {
        var spec = BuildingLayoutCatalog.GetPaletteSpec(BuildingLayoutPalette.OneSpurLeft_SpreadEvenly);

        Assert.Equal(1, spec.SpurCount);
        Assert.Single(spec.SpurRows);
        Assert.Equal(4, spec.SpurRows[0]);
        Assert.Single(spec.SpurDirections);
        Assert.Equal(SpurDirection.West, spec.SpurDirections[0]);
        Assert.Equal(PlacementStrategy.SpreadEvenly, spec.PlacementStrategy);
    }

    [Fact]
    public void GetPaletteSpec_AllPalettesHaveValidConfiguration()
    {
        var palettes = Enum.GetValues<BuildingLayoutPalette>();

        foreach (BuildingLayoutPalette palette in palettes)
        {
            var spec = BuildingLayoutCatalog.GetPaletteSpec(palette);

            Assert.InRange(spec.SpurCount, 0, 2);
            Assert.Equal(spec.SpurCount, spec.SpurRows.Length);
            Assert.Equal(spec.SpurCount, spec.SpurDirections.Length);
        }
    }
}
