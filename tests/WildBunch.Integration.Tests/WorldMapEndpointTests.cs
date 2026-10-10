using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

        var payload = await response.Content.ReadAsStringAsync();
        var map = JsonSerializer.Deserialize<WorldMapDto>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(map);
        Assert.NotEmpty(map!.Towns);
        Assert.NotEmpty(map.Trails);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspectCount\"", payload, StringComparison.OrdinalIgnoreCase);
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
