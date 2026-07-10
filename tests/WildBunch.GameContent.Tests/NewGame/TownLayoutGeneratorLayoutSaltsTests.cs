using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class TownLayoutGeneratorLayoutSaltsTests
{
    [Fact]
    public void GenerateLayout_WithUsedLayoutSalts_PersistsSalts()
    {
        var townId = new TownId("town-1");
        var source = new GameSetupDeterministicSource("test-seed");
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");

        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph,
            TownProsperity.Prosperous,
            townId,
            0,
            source,
            layoutSalts: salts,
            BuildingLayoutPalette.NoSpurs_SpreadEvenly,
            "1.0.0",
            usedLayoutSalts: salts);

        Assert.Equal(salts, layout.LayoutSalts);
    }
}
