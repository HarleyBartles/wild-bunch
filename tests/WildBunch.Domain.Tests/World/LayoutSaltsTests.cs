using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Domain.Tests.World;

public sealed class LayoutSaltsTests
{
    [Fact]
    public void LayoutSalts_CreatesWithAllFields()
    {
        var salts = new LayoutSalts("buildings-salt", "roads-salt", "dirt-salt", "props-salt");
        
        Assert.Equal("buildings-salt", salts.BuildingsSalt);
        Assert.Equal("roads-salt", salts.RoadsSalt);
        Assert.Equal("dirt-salt", salts.DirtSalt);
        Assert.Equal("props-salt", salts.PropsSalt);
    }

    [Fact]
    public void LayoutSalts_IsRecord()
    {
        var salts1 = new LayoutSalts("a", "b", "c", "d");
        var salts2 = new LayoutSalts("a", "b", "c", "d");
        
        Assert.Equal(salts1, salts2);
    }
}
