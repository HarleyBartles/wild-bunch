using System.Net;
using System.Net.Http.Json;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Dev;

public sealed class DevSessionEndpointTests
{
    [Fact]
    public async Task GetSessionDevContext_Returns200_InDevEnvironment()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/dev/sessions/{gameId}/session-context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var context = await response.Content.ReadFromJsonAsync<SessionDevContextDto>();
        Assert.NotNull(context);
        Assert.Equal(gameId, context!.SessionId);
        Assert.NotNull(context.SaltPosture);
        Assert.Equal("Fixed", context.SaltPosture.Mode);
        Assert.False(string.IsNullOrWhiteSpace(context.SaltPosture.Salt));
        Assert.True(context.SeedCodeRetained);
    }

    [Fact]
    public async Task GetSessionDevContext_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/session-context");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSessionDevContext_Returns404_WhenSessionDoesNotExist()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/session-context");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PlayerGameDto_DoesNotContainDevSaltPosture()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var json = await (await client.GetAsync($"/api/games/{gameId}")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("saltPosture", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ForceDifficulty_Returns204_AndReflectedInContext()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/force-difficulty",
            new ForceDevDifficultyRequestDto(Difficulty: "Brutal"));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var context = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
            .Content.ReadFromJsonAsync<SessionDevContextDto>();
        Assert.Equal("Brutal", context!.GameDifficulty);
    }

    [Fact]
    public async Task ForceDifficulty_Returns400_ForInvalidDifficulty()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/force-difficulty",
            new ForceDevDifficultyRequestDto(Difficulty: "Nightmare"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForceDifficulty_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{Guid.NewGuid()}/session/force-difficulty",
            new ForceDevDifficultyRequestDto(Difficulty: "Brutal"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetEntropy_Returns204_AndReflectedInContext()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/set-entropy",
            new SetDevEntropyRequestDto { Entropy = "Wild" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var context = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
            .Content.ReadFromJsonAsync<SessionDevContextDto>();
        Assert.Equal("Wild", context!.GameEntropy);
    }

    [Fact]
    public async Task SetEntropy_Returns400_ForInvalidEntropy()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/set-entropy",
            new SetDevEntropyRequestDto { Entropy = "Chaotic" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetEntropy_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{Guid.NewGuid()}/session/set-entropy",
            new SetDevEntropyRequestDto { Entropy = "Wild" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<Guid> CreateSessionAsync(HttpClient client)
    {
        var scenario = BoringScenarioBuilder.StartingTownServicesOrWantedPosterReady();
        scenario.AssertReady();

        var created = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        return created.Id;
    }
}
