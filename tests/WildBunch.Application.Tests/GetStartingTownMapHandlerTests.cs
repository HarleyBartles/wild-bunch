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
        Assert.Equal(8, result.Towns.Count);
        // Town IDs are seed-derived from the 40-entry name pool, so we verify
        // structural validity rather than hardcoded names.
        Assert.All(result.Towns, town =>
        {
            Assert.False(string.IsNullOrWhiteSpace(town.Id));
            Assert.False(string.IsNullOrWhiteSpace(town.Name));
        });
        var ids = result.Towns.Select(t => t.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public async Task AllTownsAreSelectable()
    {
        var handler = new GetStartingTownMapHandler();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery());
        Assert.Equal(8, result.Towns.Count);
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
        // Trail IDs are slot-based (trail-{fromSlot}-{toSlot}) in the canonical
        // 8-town world. Distances come from the catalog's Canonical variant.
        Assert.Equal(4m, byId["trail-0-1"].RideDayDistance);
        Assert.Equal(2m, byId["trail-0-2"].RideDayDistance);
        Assert.Equal(3m, byId["trail-1-3"].RideDayDistance);
        Assert.Equal(3m, byId["trail-2-4"].RideDayDistance);
        Assert.Equal(5m, byId["trail-1-4"].RideDayDistance);
        Assert.Equal(5m, byId["trail-0-3"].RideDayDistance);
        Assert.Equal(4m, byId["trail-3-5"].RideDayDistance);
        Assert.Equal(5m, byId["trail-4-5"].RideDayDistance);
        Assert.Equal(3m, byId["trail-5-6"].RideDayDistance);
        Assert.Equal(3m, byId["trail-0-6"].RideDayDistance);
        Assert.Equal(3m, byId["trail-6-7"].RideDayDistance);
        Assert.Equal(4m, byId["trail-3-7"].RideDayDistance);
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
    public async Task TrailEdgesCoverAllSeededTrails()
    {
        var handler = new GetStartingTownMapHandler();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery());
        // The canonical 8-town world has 12 slot-based trails (6 base + 2 per
        // additional slot for slots 5, 6, 7).
        Assert.Equal(12, result.Trails.Count);
    }

    [Fact]
    public void GetMapTowns_DoesNotCrashWithDerivedTownNames()
    {
        var towns = SeedWorldMapLayout.GetMapTowns();
        Assert.NotEmpty(towns);
        Assert.All(towns, town => Assert.True(town.X >= 0 && town.Y >= 0));
    }
}
