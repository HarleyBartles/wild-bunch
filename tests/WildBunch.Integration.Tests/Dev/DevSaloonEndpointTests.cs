using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Dev;

public sealed class DevSaloonEndpointTests
{
    [Fact]
    public async Task GetSaloonDevContext_Returns200_InDevEnvironment()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var gameId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/dev/sessions/{gameId}/saloon-context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var context = await response.Content.ReadFromJsonAsync<SaloonDevContextDto>();
        Assert.NotNull(context);
        Assert.Equal(gameId, context!.SessionId);
        Assert.False(context.SourceSpent);
        Assert.Null(context.ActiveSaloonPoi);
        Assert.Null(context.PendingDevOverride);
        // Hidden truth is exposed in dev context
        Assert.NotNull(context.HiddenTruth);
    }

    [Fact]
    public async Task GetSaloonDevContext_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/saloon-context");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSaloonDevContext_Returns404_WhenSessionDoesNotExist()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/saloon-context");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ForceSaloonOverride_Returns204_AndForcesOverride()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var gameId = await CreateSessionAsync(client);

        var forceResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/saloon/force-override",
            new ForceSaloonOverrideRequestDto(ForcedKind: "Citizen", ForcedSuspectId: null, ForcedCitizenRoleKey: null));

        Assert.Equal(HttpStatusCode.NoContent, forceResponse.StatusCode);

        // Verify the override is visible in the dev context
        var contextResponse = await client.GetAsync($"/api/dev/sessions/{gameId}/saloon-context");
        var context = await contextResponse.Content.ReadFromJsonAsync<SaloonDevContextDto>();
        Assert.NotNull(context!.PendingDevOverride);
        Assert.Equal("Citizen", context.PendingDevOverride.ForcedKind);
        Assert.Null(context.PendingDevOverride.ForcedSuspectId);
    }

    [Fact]
    public async Task ForceSaloonOverride_Returns400_WhenForcedKindIsMissing()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var gameId = await CreateSessionAsync(client);

        // Send with empty ForcedKind - the endpoint validates this and returns 400
        var forceResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/saloon/force-override",
            new ForceSaloonOverrideRequestDto(ForcedKind: "", ForcedSuspectId: null, ForcedCitizenRoleKey: null));

        Assert.Equal(HttpStatusCode.BadRequest, forceResponse.StatusCode);
    }

    [Fact]
    public async Task ForceSaloonOverride_Returns400_WhenForcedKindIsInvalid()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var gameId = await CreateSessionAsync(client);

        var forceResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/saloon/force-override",
            new ForceSaloonOverrideRequestDto(ForcedKind: "NotARealKind", ForcedSuspectId: null, ForcedCitizenRoleKey: null));

        Assert.Equal(HttpStatusCode.BadRequest, forceResponse.StatusCode);
    }

    [Fact]
    public async Task ForceSaloonOverride_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{Guid.NewGuid()}/saloon/force-override",
            new ForceSaloonOverrideRequestDto("Citizen", null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ClearSaloonOverride_Returns204_AndClearsOverride()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var gameId = await CreateSessionAsync(client);

        // Force first
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/saloon/force-override",
            new ForceSaloonOverrideRequestDto("Citizen", null, null));

        // Clear
        var clearResponse = await client.PostAsync(
            $"/api/dev/sessions/{gameId}/saloon/clear-override", content: null);

        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);

        // Verify cleared
        var contextResponse = await client.GetAsync($"/api/dev/sessions/{gameId}/saloon-context");
        var context = await contextResponse.Content.ReadFromJsonAsync<SaloonDevContextDto>();
        Assert.Null(context!.PendingDevOverride);
    }

    [Fact]
    public async Task ClearSaloonOverride_Returns204_WhenNoOverridePending()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var gameId = await CreateSessionAsync(client);

        // Clear without forcing first - idempotent no-op
        var clearResponse = await client.PostAsync(
            $"/api/dev/sessions/{gameId}/saloon/clear-override", content: null);

        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);
    }

    [Fact]
    public async Task ClearSaloonOverride_Returns404_WhenSessionDoesNotExist()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/dev/sessions/{Guid.NewGuid()}/saloon/clear-override", content: null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ClearSaloonOverride_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/dev/sessions/{Guid.NewGuid()}/saloon/clear-override", content: null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PlayerFacingSaloonContextPath_Returns404()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var gameId = await CreateSessionAsync(client);

        // The player-facing saloon-context path must not exist
        var response = await client.GetAsync($"/api/games/{gameId}/saloon-context");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<Guid> CreateSessionAsync(HttpClient client)
    {
        var scenario = BoringScenarioBuilder.StartingTownServicesOrWantedPosterReady();
        scenario.AssertReady();

        var created = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        return created.Id;
    }
}
