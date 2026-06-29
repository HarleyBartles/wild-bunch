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
        Assert.True(context.SeedCodeRetained); // Seed code is now always retained for debugging
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
    public async Task LockRng_Returns204_AndReflectedInContext()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var lockResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/lock-rng",
            new LockRngRequestDto(Salt: "deadbeef"));
        Assert.Equal(HttpStatusCode.NoContent, lockResponse.StatusCode);

        var context = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
            .Content.ReadFromJsonAsync<SessionDevContextDto>();
        Assert.Equal("Fixed", context!.SaltPosture.Mode);
        Assert.Equal("deadbeef", context.SaltPosture.Salt);
    }

    [Fact]
    public async Task ClearRng_Returns204_AndRestoresRuntimeMode()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);
        await client.PostAsJsonAsync($"/api/dev/sessions/{gameId}/session/lock-rng", new LockRngRequestDto("deadbeef"));

        var clearResponse = await client.PostAsync($"/api/dev/sessions/{gameId}/session/clear-rng", null);
        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);

        var context = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
            .Content.ReadFromJsonAsync<SessionDevContextDto>();
        Assert.Equal("Runtime", context!.SaltPosture.Mode);
    }

    [Fact]
    public async Task PlayerGameDto_DoesNotContainDevSaltPosture()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var json = await (await client.GetAsync($"/api/games/{gameId}")).Content.ReadAsStringAsync();
        // Player DTO must not carry dev-only salt posture.
        Assert.DoesNotContain("saltPosture", json, StringComparison.OrdinalIgnoreCase);
    }

    // --- RNG mutation falsification proof (integration level) ---

    [Fact]
    public async Task LockRng_DoesNotMutatePlayerDto()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        // Capture player DTO before RNG lock
        var gameBefore = await (await client.GetAsync($"/api/games/{gameId}")).Content.ReadAsStringAsync();

        // Lock RNG
        await client.PostAsJsonAsync($"/api/dev/sessions/{gameId}/session/lock-rng", new LockRngRequestDto("lock-test"));

        // Capture player DTO after RNG lock
        var gameAfter = await (await client.GetAsync($"/api/games/{gameId}")).Content.ReadAsStringAsync();

        // Player-facing DTO must be unchanged by dev salt commands
        Assert.Equal(gameBefore, gameAfter);
    }

    [Fact]
    public async Task LockRng_WithNullSalt_GeneratesFixedSalt()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        // Post with null salt (omitted)
        var lockResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/lock-rng",
            new LockRngRequestDto(Salt: null));
        Assert.Equal(HttpStatusCode.NoContent, lockResponse.StatusCode);

        var context = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
            .Content.ReadFromJsonAsync<SessionDevContextDto>();
        Assert.Equal("Fixed", context!.SaltPosture.Mode);
        Assert.False(string.IsNullOrEmpty(context.SaltPosture.Salt));
    }

    [Fact]
    public async Task LockRng_WithEmptySalt_GeneratesFixedSalt()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var lockResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/lock-rng",
            new LockRngRequestDto(Salt: ""));
        Assert.Equal(HttpStatusCode.NoContent, lockResponse.StatusCode);

        var context = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
            .Content.ReadFromJsonAsync<SessionDevContextDto>();
        Assert.Equal("Fixed", context!.SaltPosture.Mode);
        Assert.False(string.IsNullOrEmpty(context.SaltPosture.Salt));
    }

    [Fact]
    public async Task LockRng_DoesNotMutateSessionDevContext_ExceptSaltPosture()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        // Capture session dev context before RNG lock
        var contextBefore = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
            .Content.ReadFromJsonAsync<SessionDevContextDto>();

        // Lock RNG
        await client.PostAsJsonAsync($"/api/dev/sessions/{gameId}/session/lock-rng", new LockRngRequestDto("test-salt"));

        // Capture session dev context after RNG lock
        var contextAfter = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
            .Content.ReadFromJsonAsync<SessionDevContextDto>();

        // Everything except SaltPosture must be unchanged
        Assert.Equal(contextBefore!.SessionId, contextAfter!.SessionId);
        Assert.Equal(contextBefore.Status, contextAfter.Status);
        Assert.Equal(contextBefore.GameDifficulty, contextAfter.GameDifficulty);
        Assert.Equal(contextBefore.GameEntropy, contextAfter.GameEntropy);
        Assert.Equal(contextBefore.CurrentTownId, contextAfter.CurrentTownId);
        Assert.Equal(contextBefore.CurrentTownName, contextAfter.CurrentTownName);
        Assert.Equal(contextBefore.CurrentActionContext, contextAfter.CurrentActionContext);
        Assert.Equal(contextBefore.HasActiveJourney, contextAfter.HasActiveJourney);
        // Salt posture DID change — that's the point
        Assert.Equal("Fixed", contextAfter.SaltPosture.Mode);
        Assert.Equal("test-salt", contextAfter.SaltPosture.Salt);
    }

    [Fact]
    public async Task ForceDifficulty_Returns204_AndReflectedInContext()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var forceResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/force-difficulty",
            new ForceDevDifficultyRequestDto(Difficulty: "Brutal"));
        Assert.Equal(HttpStatusCode.NoContent, forceResponse.StatusCode);

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

        var forceResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/force-difficulty",
            new ForceDevDifficultyRequestDto(Difficulty: "Nightmare"));
        Assert.Equal(HttpStatusCode.BadRequest, forceResponse.StatusCode);
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

        var setResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/set-entropy",
            new SetDevEntropyRequestDto { Entropy = "Wild" });
        Assert.Equal(HttpStatusCode.NoContent, setResponse.StatusCode);

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

        var setResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/set-entropy",
            new SetDevEntropyRequestDto { Entropy = "Chaotic" });
        Assert.Equal(HttpStatusCode.BadRequest, setResponse.StatusCode);
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
        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(created);
        return created!.Id;
    }
}
