using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Application.Tests.Games.Mapping;

public sealed class TownLayoutMapperLayoutSaltsTests
{
    [Fact]
    public void ToDto_WithLayoutSalts_MapsLayoutSalts()
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
        
        var dto = TownLayoutMapper.ToDto(layout);
        
        Assert.NotNull(dto);
        Assert.NotNull(dto.LayoutSalts);
        Assert.Equal("buildings", dto.LayoutSalts.BuildingsSalt);
    }
}
