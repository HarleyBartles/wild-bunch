using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests;

public sealed class GetStartingTownsHandlerTests
{
    [Fact]
    public async Task ReturnsStartingTownCandidatesWithSuppliesOrNoticeBoard()
    {
        var handler = new GetStartingTownsHandler();
        var result = await handler.HandleAsync(new GetStartingTownsQuery());
        Assert.NotEmpty(result);
        Assert.All(result, town =>
        {
            var hasSupplies = (town.Services & TownServices.Supplies) != 0;
            var hasNoticeBoard = (town.Services & TownServices.NoticeBoard) != 0;
            Assert.True(hasSupplies || hasNoticeBoard, $"Town {town.Name} should have Supplies or NoticeBoard");
        });
    }

    [Fact]
    public async Task ReturnsKnownCanonicalTowns()
    {
        var handler = new GetStartingTownsHandler();
        var result = await handler.HandleAsync(new GetStartingTownsQuery());
        var ids = result.Select(t => t.Id).ToArray();
        // The canonical candidates with Supplies or NoticeBoard: pinecross, redmesa, sagewell, emberfall
        // (holloway has Doctor only in canonical — no Supplies/NoticeBoard; dryfork/hardpan/openpass have None)
        Assert.Contains("pinecross", ids);
        Assert.Contains("redmesa", ids);
        Assert.Contains("sagewell", ids);
        Assert.Contains("emberfall", ids);
    }

    [Fact]
    public async Task ExcludesTownsWithoutSuppliesOrNoticeBoard()
    {
        var handler = new GetStartingTownsHandler();
        var result = await handler.HandleAsync(new GetStartingTownsQuery());
        var ids = result.Select(t => t.Id).ToArray();
        // These towns have no Supplies or NoticeBoard in the canonical variant
        Assert.DoesNotContain("dryfork", ids);
        Assert.DoesNotContain("hardpan", ids);
        Assert.DoesNotContain("openpass", ids);
        // holloway has Doctor only in canonical (no Supplies/NoticeBoard)
        Assert.DoesNotContain("holloway", ids);
    }
}
