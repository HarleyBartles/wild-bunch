using System.Net;
using System.Net.Http.Json;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class StartingTownMapEndpointTests
{
    private static readonly string[] SeededTownIds =
    [
        "lostcanyon",
        "goldgulch",
        "redmesa",
        "tumbleweed",
        "quartzsite",
        "emberfall",
        "rattlesnake",
        "boulderwash"
    ];

    [Fact]
    public async Task GetStartingTownMapReturnsOkWithTownsAndTrails()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/games/starting-town-map");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var map = await response.Content.ReadFromJsonAsync<StartingTownMapDto>();

        Assert.NotNull(map);
        Assert.NotEmpty(map!.Towns);
        Assert.NotEmpty(map.Trails);
    }

    [Fact]
    public async Task GetStartingTownMapReturnsAllEightSeededTowns()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/games/starting-town-map");
        var map = await response.Content.ReadFromJsonAsync<StartingTownMapDto>();

        Assert.NotNull(map);
        var townIds = map!.Towns.Select(town => town.Id).ToArray();
        Assert.Equal(8, townIds.Length);
        foreach (var seededTownId in SeededTownIds)
        {
            Assert.Contains(seededTownId, townIds);
        }
    }

    [Fact]
    public async Task GetStartingTownMapReturnsAllTownsAsSelectable()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/games/starting-town-map");
        var map = await response.Content.ReadFromJsonAsync<StartingTownMapDto>();

        Assert.NotNull(map);
        Assert.Equal(8, map!.Towns.Count);
    }

    [Fact]
    public async Task GetStartingTownMapReturnsTrailsWithRideDayDistances()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/games/starting-town-map");
        var map = await response.Content.ReadFromJsonAsync<StartingTownMapDto>();

        Assert.NotNull(map);
        Assert.Equal(12, map.Trails.Count);
        Assert.All(map!.Trails, trail =>
        {
            Assert.False(string.IsNullOrWhiteSpace(trail.Id));
            Assert.False(string.IsNullOrWhiteSpace(trail.FromTownId));
            Assert.False(string.IsNullOrWhiteSpace(trail.ToTownId));
            Assert.True(trail.RideDayDistance > 0m);
        });
    }

    [Fact]
    public async Task GetStartingTownMapDoesNotExposeHiddenTruthFields()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/games/starting-town-map");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspectCount\"", payload, StringComparison.OrdinalIgnoreCase);
    }
}
