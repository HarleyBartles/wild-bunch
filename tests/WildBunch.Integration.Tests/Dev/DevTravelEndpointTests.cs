using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Dev;
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

    [Theory]
    [InlineData("", "ForcedCategory is required.")]
    [InlineData("Npc", "Invalid ForcedCategory value.")]
    public async Task ForceTravelOverride_Returns400_ForBlankOrRetiredCategory_WithoutChangingPersistedState(
        string forcedCategory,
        string expectedError)
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var (gameId, _) = await CreateSessionAndStartTravelAsync(client);
        var sessionBefore = await ReadGameAsync(client, gameId);
        var travelContextBefore = await ReadTravelContextAsync(client, gameId);
        var auditBefore = await ReadAuditAsync(client, gameId);
        Assert.NotNull(sessionBefore);
        Assert.NotNull(travelContextBefore);
        Assert.NotNull(auditBefore);

        var response = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/travel/force-override",
            new ForceTravelOverrideRequestDto(forcedCategory, null, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expectedError, await response.Content.ReadFromJsonAsync<string>());

        var sessionAfter = await ReadGameAsync(client, gameId);
        var travelContextAfter = await ReadTravelContextAsync(client, gameId);
        var auditAfter = await ReadAuditAsync(client, gameId);

        AssertSessionStateUnchanged(sessionBefore!, sessionAfter!);
        Assert.Equal(travelContextBefore, travelContextAfter);
        Assert.NotNull(auditAfter);
        Assert.Equal(
            auditBefore!.Entries.Select(entry => (entry.Sequence, entry.EventType, entry.Summary, entry.OccurredAtUtc)),
            auditAfter!.Entries.Select(entry => (entry.Sequence, entry.EventType, entry.Summary, entry.OccurredAtUtc)));
        Assert.DoesNotContain(auditAfter.Entries, entry => entry.EventType == "DevTravelOverrideForced");
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

    private static async Task<GameSessionDto?> ReadGameAsync(HttpClient client, Guid gameId)
    {
        using var response = await client.GetAsync($"/api/games/{gameId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<GameSessionDto>();
    }

    private static async Task<TravelDevContextDto?> ReadTravelContextAsync(HttpClient client, Guid gameId)
    {
        using var response = await client.GetAsync($"/api/dev/sessions/{gameId}/travel-context");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<TravelDevContextDto>();
    }

    private static async Task<SessionAuditDto?> ReadAuditAsync(HttpClient client, Guid gameId)
    {
        using var response = await client.GetAsync($"/api/dev/sessions/{gameId}/audit");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SessionAuditDto>();
    }

    private static void AssertSessionStateUnchanged(GameSessionDto before, GameSessionDto after)
    {
        Assert.Equal(before.Status, after.Status);
        Assert.Equal(before.Player, after.Player);
        Assert.Equal(before.Clock, after.Clock);
        Assert.Equal(before.PursuitState, after.PursuitState);
        Assert.Equal(before.Journey?.Status, after.Journey?.Status);
        Assert.Equal(before.Journey?.DaysTravelled, after.Journey?.DaysTravelled);
        Assert.Equal(before.Journey?.RemainingDays, after.Journey?.RemainingDays);
        Assert.Equal(before.Journey?.PendingEncounter?.Kind, after.Journey?.PendingEncounter?.Kind);
        Assert.Equal(before.Journey?.PendingEncounter?.Message, after.Journey?.PendingEncounter?.Message);
        Assert.NotNull(before.Inventory);
        Assert.NotNull(after.Inventory);
        Assert.Equal(before.Inventory!.Wallet, after.Inventory!.Wallet);
        Assert.Equal(before.Inventory.HorseState, after.Inventory.HorseState);
        Assert.Equal(before.Inventory.CanteenState, after.Inventory.CanteenState);
        Assert.Equal(before.Inventory.Capabilities, after.Inventory.Capabilities);
        Assert.Equal(
            before.Inventory.Items.Select(item => (item.Kind, item.Quantity, item.HorseState, item.CanteenState)),
            after.Inventory.Items.Select(item => (item.Kind, item.Quantity, item.HorseState, item.CanteenState)));
        Assert.Equal(
            before.LogEntries.Select(entry => (entry.Kind, entry.Message, entry.Day, entry.Turn)),
            after.LogEntries.Select(entry => (entry.Kind, entry.Message, entry.Day, entry.Turn)));
    }
}
