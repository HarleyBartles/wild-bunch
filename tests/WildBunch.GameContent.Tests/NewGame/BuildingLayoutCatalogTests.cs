using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class BuildingLayoutCatalogTests
{
    [Fact]
    public void GetLayout_ReturnsCanonicalLayout()
    {
        // TODO: Task 2 will implement tile-based layout generation
        // For now, verify that the catalog returns a layout for the new palette
        var layout = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        Assert.NotNull(layout);
        Assert.NotEmpty(layout.BuildingPlacements);
    }

    [Fact]
    public void GetLayout_ReturnsLayoutForAllNewPalettes()
    {
        // TODO: Task 2 will implement tile-based layout generation
        // For now, verify that the catalog returns a layout for all new palettes
        var noSpurs = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.NoSpurs_SpreadEvenly);
        var oneSpur = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.OneSpurLeft_SpreadEvenly);
        var twoSpurs = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.TwoSpursLeftRight_SpreadEvenly);

        Assert.NotNull(noSpurs);
        Assert.NotNull(oneSpur);
        Assert.NotNull(twoSpurs);
    }
}
