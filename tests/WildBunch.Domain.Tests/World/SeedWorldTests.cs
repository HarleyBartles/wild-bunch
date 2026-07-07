using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.Domain.Tests.World;

public sealed class SeedWorldTests
{
    [Fact]
    public void SeedWorld_WithBuildingLayoutPalette_StoresValue()
    {
        var seedWorld = new SeedWorld(
            Guid.NewGuid(),
            SeedWorldVariant.Canonical,
            5,
            ServicesPalette.HubTelegraph,
            ProsperityPalette.UniformProsperous,
            1,
            GraphDensity.Sparse,
            0,
            0,
            0,
            0,
            BuildingLayoutPalette: BuildingLayoutPalette.NoSpurs_SpreadEvenly);

        Assert.Equal(BuildingLayoutPalette.NoSpurs_SpreadEvenly, seedWorld.BuildingLayoutPalette);
    }
}
