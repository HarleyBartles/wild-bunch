using System.Net;
using System.Net.Http.Json;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class WorldMapEndpointTests
{
    [Fact]
    public async Task GetWorldMapReturnsOkWithTownsAndTrails()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/games/{sessionId}/world-map");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var map = await response.Content.ReadFromJsonAsync<StartingTownMapDto>();
        Assert.NotNull(map);
        Assert.NotEmpty(map!.Towns);
        Assert.NotEmpty(map.Trails);
    }

    [Fact]
    public async Task GetWorldMapReturnsSameShapeAsStartingTownMap()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var sessionId = await CreateSessionAsync(client);

        var worldMapResponse = await client.GetAsync($"/api/games/{sessionId}/world-map");
        var startingTownMapResponse = await client.GetAsync($"/api/games/{sessionId}/starting-town-map");

        Assert.Equal(HttpStatusCode.OK, worldMapResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, startingTownMapResponse.StatusCode);

        var worldMap = await worldMapResponse.Content.ReadFromJsonAsync<StartingTownMapDto>();
        var startingTownMap = await startingTownMapResponse.Content.ReadFromJsonAsync<StartingTownMapDto>();

        Assert.NotNull(worldMap);
        Assert.NotNull(startingTownMap);
        Assert.Equal(startingTownMap!.Towns.Count, worldMap!.Towns.Count);
        Assert.Equal(startingTownMap.Trails.Count, worldMap.Trails.Count);
    }

    [Fact]
    public async Task GetWorldMapReturnsNotFoundForMissingSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}/world-map");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<Guid> CreateSessionAsync(HttpClient client)
    {
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var response = await client.PostAsJsonAsync("/api/games/setup", scenario.CreateRequest("Ranger Vale"));
        var session = await response.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(session);
        return session!.Id;
    }
}
