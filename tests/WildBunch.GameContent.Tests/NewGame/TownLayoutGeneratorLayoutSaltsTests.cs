using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class TownLayoutGeneratorLayoutSaltsTests
{
    [Fact]
    public void GenerateLayout_WithLayoutSalts_PersistsSalts()
    {
        var townId = new TownId("town-1");
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");
        var layoutSource = new LayoutDeterministicSource("test-seed", townId, 0, "1.0.0", salts);

        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph,
            TownProsperity.Prosperous,
            townId,
            0,
            layoutSource,
            BuildingLayoutPalette.NoSpurs_SpreadEvenly,
            "1.0.0");

        Assert.Equal(salts, layout.LayoutSalts);
    }
}
