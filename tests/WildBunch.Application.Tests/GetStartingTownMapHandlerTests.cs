using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Application.Tests;

public sealed class GetStartingTownMapHandlerTests
{
    [Fact]
    public async Task ReturnsAllEightSeededTowns()
    {
        var handler = new GetStartingTownMapHandler();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery());
        var ids = result.Towns.Select(t => t.Id).ToArray();
        Assert.Contains("pinecross", ids);
        Assert.Contains("redmesa", ids);
        Assert.Contains("holloway", ids);
        Assert.Contains("sagewell", ids);
        Assert.Contains("dryfork", ids);
        Assert.Contains("emberfall", ids);
        Assert.Contains("hardpan", ids);
        Assert.Contains("openpass", ids);
        Assert.Equal(8, result.Towns.Count);
    }

    [Fact]
    public async Task SelectableTownsMatchStartingTownCandidates()
    {
        var handler = new GetStartingTownMapHandler();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery());
        var candidateIds = StartingTownCatalog.GetStartingTownCandidates()
            .Select(t => t.Id.Value)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var selectableIds = result.Towns
            .Where(t => t.Selectable)
            .Select(t => t.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.Equal(candidateIds, selectableIds);
    }

    [Fact]
    public async Task SelectableTownsAreTheFourCanonicalCandidates()
    {
        var handler = new GetStartingTownMapHandler();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery());
        var selectableIds = result.Towns
            .Where(t => t.Selectable)
            .Select(t => t.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.Equal(new[] { "emberfall", "pinecross", "redmesa", "sagewell" }, selectableIds);
    }

    [Fact]
    public async Task SelectableTownIdsMatchGetStartingTownsHandlerResult()
    {
        var mapHandler = new GetStartingTownMapHandler();
        var mapResult = await mapHandler.HandleAsync(new GetStartingTownMapQuery());
        var townsHandler = new GetStartingTownsHandler();
        var townsResult = await townsHandler.HandleAsync(new GetStartingTownsQuery());
        var mapSelectableIds = mapResult.Towns
            .Where(t => t.Selectable)
            .Select(t => t.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var townIds = townsResult
            .Select(t => t.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.Equal(townIds, mapSelectableIds);
    }

    [Fact]
    public async Task CoordinatesAreDeterministicAcrossCalls()
    {
        var handler = new GetStartingTownMapHandler();
        var first = await handler.HandleAsync(new GetStartingTownMapQuery());
        var second = await handler.HandleAsync(new GetStartingTownMapQuery());
        Assert.Equal(first.Towns.Select(t => (t.Id, t.X, t.Y)), second.Towns.Select(t => (t.Id, t.X, t.Y)));
    }

    [Fact]
    public async Task TrailEdgesCarryCorrectRideDayDistances()
    {
        var handler = new GetStartingTownMapHandler();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery());
        var byId = result.Trails.ToDictionary(t => t.Id);
        Assert.Equal(4m, byId["trail-pine-red"].RideDayDistance);
        Assert.Equal(2m, byId["trail-pine-hollow"].RideDayDistance);
        Assert.Equal(3m, byId["trail-red-sage"].RideDayDistance);
        Assert.Equal(5m, byId["trail-red-dry"].RideDayDistance);
        Assert.Equal(3m, byId["trail-hollow-sage"].RideDayDistance);
        Assert.Equal(5m, byId["trail-sage-ember"].RideDayDistance);
        Assert.Equal(5m, byId["trail-red-ember"].RideDayDistance);
        Assert.Equal(3m, byId["trail-pine-hardpan"].RideDayDistance);
        Assert.Equal(3m, byId["trail-pine-openpass"].RideDayDistance);
    }

    [Fact]
    public async Task TrailEdgesConnectRenderedTowns()
    {
        var handler = new GetStartingTownMapHandler();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery());
        var townIds = result.Towns.Select(t => t.Id).ToHashSet();
        Assert.All(result.Trails, trail =>
        {
            Assert.Contains(trail.FromTownId, townIds);
            Assert.Contains(trail.ToTownId, townIds);
        });
    }

    [Fact]
    public async Task TrailEdgesCoverAllNineSeededTrails()
    {
        var handler = new GetStartingTownMapHandler();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery());
        Assert.Equal(9, result.Trails.Count);
    }
}
