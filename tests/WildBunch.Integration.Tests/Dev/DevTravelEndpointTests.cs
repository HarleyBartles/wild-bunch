using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Dev;

public sealed class DevTravelEndpointTests
{
    [Fact]
    public async Task GetTravelDevContext_Returns200_InDevEnvironment()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var (gameId, _) = await CreateSessionAndStartTravelAsync(client);

        var response = await client.GetAsync($"/api/dev/sessions/{gameId}/travel-context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var context = await response.Content.ReadFromJsonAsync<TravelDevContextDto>();
        Assert.NotNull(context);
        Assert.True(context!.HasActiveJourney);
        Assert.Equal("Active", context.JourneyStatus);
    }

    [Fact]
    public async Task GetTravelDevContext_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/travel-context");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTravelDevContext_Returns404_WhenSessionDoesNotExist()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/travel-context");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ForceTravelOverride_Returns204_AndForcesOverride()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var (gameId, _) = await CreateSessionAndStartTravelAsync(client);

        var forceResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/travel/force-override",
            new ForceTravelOverrideRequestDto(
                ForcedCategory: "Foe",
                FoeSpeed: 5,
                FoeFightStrength: 4,
                FoeMinimumBribe: 8m,
                EncounterMessage: "A hard-eyed rider blocks the trail."));

        Assert.Equal(HttpStatusCode.NoContent, forceResponse.StatusCode);

        // Verify the override is visible in the dev context
        var contextResponse = await client.GetAsync($"/api/dev/sessions/{gameId}/travel-context");
        var context = await contextResponse.Content.ReadFromJsonAsync<TravelDevContextDto>();
        Assert.NotNull(context!.PendingDevOverride);
        Assert.Equal("Foe", context.PendingDevOverride.ForcedCategory);
        Assert.Equal(5, context.PendingDevOverride.FoeProfile!.Speed);
    }

    [Fact]
    public async Task ForceTravelOverride_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{Guid.NewGuid()}/travel/force-override",
            new ForceTravelOverrideRequestDto("Foe", null, null, null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ClearTravelOverride_Returns204_AndClearsOverride()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var (gameId, _) = await CreateSessionAndStartTravelAsync(client);

        // Force first
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/travel/force-override",
            new ForceTravelOverrideRequestDto("Foe", 5, 4, 8m, null));

        // Clear
        var clearResponse = await client.PostAsync(
            $"/api/dev/sessions/{gameId}/travel/clear-override", content: null);

        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);

        // Verify cleared
        var contextResponse = await client.GetAsync($"/api/dev/sessions/{gameId}/travel-context");
        var context = await contextResponse.Content.ReadFromJsonAsync<TravelDevContextDto>();
        Assert.Null(context!.PendingDevOverride);
    }

    [Fact]
    public async Task PlayerFacingTravelContextPath_Returns404()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var (gameId, _) = await CreateSessionAndStartTravelAsync(client);

        // The player-facing travel-context path must not exist
        var response = await client.GetAsync($"/api/games/{gameId}/travel-context");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<(Guid gameId, GameTurnResultDto turnResult)> CreateSessionAndStartTravelAsync(
        HttpClient client)
    {
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var created = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        // Discover destination dynamically — no hardcoded town names
        var destinationTownId = scenario.DiscoverFirstConnectedTownId(created);
        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{created.Id}/travel",
            new TravelRequest(destinationTownId));
        travelResponse.EnsureSuccessStatusCode();
        var turnResult = await travelResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();
        Assert.NotNull(turnResult);
        Assert.True(turnResult!.Success);

        return (created.Id, turnResult);
    }
}
