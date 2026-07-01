using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Game;
using Xunit;

namespace WildBunch.Application.Tests;

public class BeatLabelRendererTests
{
    [Theory]
    [InlineData(TimeOfDay.Morning, 1, "Morning of Day 1")]
    [InlineData(TimeOfDay.Afternoon, 1, "Afternoon of Day 1")]
    [InlineData(TimeOfDay.Evening, 2, "Evening of Day 2")]
    [InlineData(TimeOfDay.Night, 3, "Night of Day 3")]
    public void Render_ReturnsDiegeticBeatLabel(TimeOfDay timeOfDay, int day, string expected)
    {
        var result = BeatLabelRenderer.Render(timeOfDay, day);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Render_DoesNotIncludeRawTurnNumber()
    {
        var result = BeatLabelRenderer.Render(TimeOfDay.Morning, 1);
        Assert.DoesNotContain("turn", result.ToLowerInvariant());
        Assert.DoesNotContain("0", result);
    }
}
