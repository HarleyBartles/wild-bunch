using WildBunch.Domain.Events;
using WildBunch.Domain.World;
using TownId = WildBunch.Domain.World.TownId;
using Xunit;

namespace WildBunch.Domain.Tests;

public sealed class StartingTownSelectedEventTests
{
    [Fact]
    public void StartingTownSelected_CarriesTownId()
    {
        var townId = new TownId("hardpan");
        var evt = new StartingTownSelected { StartingTownId = townId };
        Assert.Equal("hardpan", evt.StartingTownId.Value);
    }
}
