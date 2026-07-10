using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Domain.Tests.World;

public sealed class TownLayoutLayoutSaltsTests
{
    [Fact]
    public void TownLayout_WithLayoutSalts_CreatesSuccessfully()
    {
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");
        var layout = new TownLayout(
            [],
            50,
            50,
            TownProsperity.Prosperous,
            [],
            null,
            "1.0.0",
            salts);

        Assert.Equal(salts, layout.LayoutSalts);
    }
}
