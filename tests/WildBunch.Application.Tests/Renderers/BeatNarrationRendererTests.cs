using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Game;
using Xunit;

namespace WildBunch.Application.Tests.Renderers;

public class BeatNarrationRendererTests
{
    [Theory]
    [InlineData(TimeOfDay.Morning, TownActionContext.Saloon, "Tumbleweed", "You spent the morning at the saloon in Tumbleweed")]
    [InlineData(TimeOfDay.Afternoon, TownActionContext.SheriffOffice, "Dust Creek", "You spent the afternoon at the sheriff's office in Dust Creek")]
    [InlineData(TimeOfDay.Evening, TownActionContext.TelegraphOffice, "Ridge Pass", "You spent the evening at the telegraph office in Ridge Pass")]
    [InlineData(TimeOfDay.Night, TownActionContext.TownSquare, "Silverton", "You spent the night at the town square in Silverton")]
    public void Render_ReturnsDiegeticNarration(TimeOfDay timeOfDay, TownActionContext context, string townName, string expected)
    {
        var result = BeatNarrationRenderer.Render(timeOfDay, context, townName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Render_DoesNotIncludeRawTurnOrDayNumbers()
    {
        var result = BeatNarrationRenderer.Render(TimeOfDay.Morning, TownActionContext.Saloon, "Tumbleweed");
        Assert.DoesNotContain("turn", result.ToLowerInvariant());
        Assert.DoesNotContain("day 0", result.ToLowerInvariant());
    }
}
