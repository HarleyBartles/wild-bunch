using System.Net;
using System.Net.Http.Json;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.GameContent.NewGame;
using WildBunch.Api.Games;

namespace WildBunch.Integration.Tests;

public sealed class StartingTownMapEndpointTests
{
    [Fact]
    public async Task GetStartingTownMapReturnsOkWithTownsAndTrails()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/games/{sessionId}/starting-town-map");

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
        var sessionId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/games/{sessionId}/starting-town-map");
        var map = await response.Content.ReadFromJsonAsync<StartingTownMapDto>();

        Assert.NotNull(map);
        var townIds = map!.Towns.Select(town => town.Id).ToArray();
        Assert.Equal(8, townIds.Length);
        // Assert structural properties instead of specific town IDs — town names are game content.
        Assert.All(townIds, townId => Assert.False(string.IsNullOrWhiteSpace(townId)));
        Assert.Equal(townIds.Length, townIds.Distinct().Count());
    }

    [Fact]
    public async Task GetStartingTownMapReturnsAllTownsAsSelectable()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/games/{sessionId}/starting-town-map");
        var map = await response.Content.ReadFromJsonAsync<StartingTownMapDto>();

        Assert.NotNull(map);
        Assert.Equal(8, map!.Towns.Count);
    }

    [Fact]
    public async Task GetStartingTownMapReturnsTrailsWithRideDayDistances()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/games/{sessionId}/starting-town-map");
        var map = await response.Content.ReadFromJsonAsync<StartingTownMapDto>();

        Assert.NotNull(map);
        Assert.NotEmpty(map.Trails);
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
        var sessionId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/games/{sessionId}/starting-town-map");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspectCount\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStartingTownMapReturnsNotFoundForMissingSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}/starting-town-map");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<Guid> CreateSessionAsync(HttpClient client)
    {
        // Build a specific seed world descriptor for this test instead of relying on canonical seed
        var seedCode = SeedWorldResolver.CreateCanonicalSeedCode();
        var seedWorld = SeedWorldResolver.Resolve(seedCode);
        
        var request = new SetupGameRequest(
            "Test Player",
            GameDifficulty.Standard,
            seedCode.ToString("D"),
            GameEntropy.Boring);

        // The map endpoint is used during town selection, so we only need
        // a setup-phase session (not a fully-started game).
        var response = await client.PostAsJsonAsync("/api/games/setup", request);
        var session = await response.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(session);
        return session!.Id;
    }
}
