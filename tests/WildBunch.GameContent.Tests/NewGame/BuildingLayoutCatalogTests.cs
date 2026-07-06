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
}
