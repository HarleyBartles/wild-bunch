using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class BuildingLayoutCatalogTests
{
    [Fact]
    public void GetLayout_ReturnsCanonicalLayout()
    {
        var layout = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.HubAndSpoke);
        
        Assert.NotNull(layout);
        Assert.NotEmpty(layout.BuildingPlacements);
        Assert.True(layout.SpurCount >= 1 && layout.SpurCount <= 2);
    }

    [Fact]
    public void GetLayout_DifferentPalettesHaveDifferentBuildingCounts()
    {
        var hubAndSpoke = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.HubAndSpoke);
        var linearChain = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.LinearChain);
        var doubleLine = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.DoubleLine);
        var tree = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.Tree);
        var star = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.Star);
        var xShaped = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.XShaped);
        var cluster = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.Cluster);
        var grid = BuildingLayoutCatalog.GetLayout(BuildingLayoutPalette.Grid);

        // All palettes should have 5 building placements (Store, Sheriff, Saloon, Telegraph, Trailhead)
        Assert.Equal(5, hubAndSpoke.BuildingPlacements.Length);
        Assert.Equal(5, linearChain.BuildingPlacements.Length);
        Assert.Equal(5, doubleLine.BuildingPlacements.Length);
        Assert.Equal(5, tree.BuildingPlacements.Length);
        Assert.Equal(5, star.BuildingPlacements.Length);
        Assert.Equal(5, xShaped.BuildingPlacements.Length);
        Assert.Equal(5, cluster.BuildingPlacements.Length);
        Assert.Equal(5, grid.BuildingPlacements.Length);

        // But they should have different spur counts
        Assert.Equal(1, hubAndSpoke.SpurCount);
        Assert.Equal(1, linearChain.SpurCount);
        Assert.Equal(2, doubleLine.SpurCount);
        Assert.Equal(2, tree.SpurCount);
        Assert.Equal(4, star.SpurCount);
        Assert.Equal(4, xShaped.SpurCount);
        Assert.Equal(2, cluster.SpurCount);
        Assert.Equal(2, grid.SpurCount);
    }
}
