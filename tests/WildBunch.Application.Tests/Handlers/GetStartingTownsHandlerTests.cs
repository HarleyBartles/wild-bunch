using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Application.Tests.Handlers;

public sealed class GetStartingTownsHandlerTests
{
    [Fact]
    public async Task ReturnsAllCanonicalTownsAsStartingCandidates()
    {
        var handler = new GetStartingTownsHandler();
        var result = await handler.HandleAsync(new GetStartingTownsQuery());
        var canonicalTowns = StartingTownCatalog.GetStartingTownCandidates();

        Assert.NotEmpty(result);
        Assert.Equal(canonicalTowns.Count, result.Count);
    }

    [Fact]
    public async Task ReturnsKnownCanonicalTowns()
    {
        var handler = new GetStartingTownsHandler();
        var result = await handler.HandleAsync(new GetStartingTownsQuery());
        var ids = result.Select(t => t.Id).ToArray();
        var canonicalIds = StartingTownCatalog.GetStartingTownCandidates()
            .Select(t => t.Id.Value)
            .ToArray();

        Assert.All(canonicalIds, id => Assert.Contains(id, ids));
    }

    [Fact]
    public async Task ReturnsExactlyTheCanonicalWorldTowns()
    {
        var handler = new GetStartingTownsHandler();
        var result = await handler.HandleAsync(new GetStartingTownsQuery());
        var ids = result.Select(t => t.Id).ToHashSet();
        var canonicalIds = StartingTownCatalog.GetStartingTownCandidates()
            .Select(t => t.Id.Value)
            .ToHashSet();

        Assert.Equal(canonicalIds, ids);
    }
}
