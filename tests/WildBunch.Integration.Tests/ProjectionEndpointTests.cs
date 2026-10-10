using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class ProjectionEndpointTests : IClassFixture<PostgreSqlApiFactory>
{
    private readonly PostgreSqlApiFactory _factory;
    private readonly HttpClient _client;

    public ProjectionEndpointTests(PostgreSqlApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHudProjection_ReturnsProjectionFromEventStream()
    {
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();
        var created = await _client.CreateStartedGameAsync(scenario);

        // Get HUD projection
        var hudResponse = await _client.GetAsync($"/api/games/{created.Id}/projections/hud");
        Assert.Equal(HttpStatusCode.OK, hudResponse.StatusCode);
        var hud = await hudResponse.Content.ReadFromJsonAsync<HudProjection>();
        Assert.NotNull(hud);
        Assert.Equal("Ranger Vale", hud!.PlayerName);
        Assert.Equal(created.Id, hud.SessionId);
    }

    [Fact]
    public async Task GetAuditProjection_IsNotExposedOnPlayerFacingApi()
    {
        // Per ADR-0028 §10, full audit is a developer/replay surface, not player-facing.
        // The audit endpoint was removed from the normal game API route group.
        // This test proves the endpoint is no longer reachable.
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();
        var created = await _client.CreateStartedGameAsync(scenario);

        var auditResponse = await _client.GetAsync($"/api/games/{created.Id}/projections/audit");
        Assert.Equal(HttpStatusCode.NotFound, auditResponse.StatusCode);
    }

    [Fact]
    public async Task GetHudProjection_Returns404_WhenSessionDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/games/{Guid.NewGuid()}/projections/hud");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Regression: CompleteGameStartHandler previously fetched the event stream
    /// and built HUD/diary projections INSIDE the ExecuteWithRetryAsync lambda,
    /// before the just-emitted GameStarted event was stored. The returned DTO's
    /// HudProjection was missing player name, wallet, inventory, current town,
    /// and health. Fix: follow TravelToTownHandler pattern — run
    /// ExecuteWithRetryAsync first, then fetch/project after events are committed.
    /// </summary>
    [Fact]
    public async Task CompleteGameStart_ReturnedDto_HudProjectionIncludesGameStartedState()
    {
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        // Step 1: setup
        var setupResponse = await _client.CreateSetupOnlyGameAsync(scenario);
        Assert.Equal("Ranger Vale", setupResponse.Player.Name);
        Assert.Null(setupResponse.Player.CurrentTownId);
        Assert.Null(setupResponse.Player.Health);
        Assert.Null(setupResponse.Inventory);
        Assert.Null(setupResponse.HudProjection);

        var setupHudResponse = await _client.GetAsync($"/api/games/{setupResponse.Id}/projections/hud");
        Assert.Equal(HttpStatusCode.NoContent, setupHudResponse.StatusCode);
        Assert.Empty(await setupHudResponse.Content.ReadAsStringAsync());

        // Step 2: view prologue
        var prologueResponse = await _client.PostAsync(
            $"/api/games/{setupResponse.Id}/prologue-viewed", content: null);
        prologueResponse.EnsureSuccessStatusCode();

        // Step 3: complete game start — the returned DTO must include HUD
        // projection with GameStarted state (player name, wallet, town, health).
        var townId = setupResponse.World.Towns.First().Id;
        var startResponse = await _client.PostAsJsonAsync(
            $"/api/games/{setupResponse.Id}/start",
            new StartGameWithTownRequest(townId));
        startResponse.EnsureSuccessStatusCode();
        var startedSession = await startResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        ArgumentNullException.ThrowIfNull(startedSession);

        // The HUD projection must reflect the just-emitted GameStarted event.
        Assert.NotNull(startedSession!.HudProjection);
        Assert.Equal("Ranger Vale", startedSession.HudProjection!.PlayerName);
        Assert.True(startedSession.HudProjection.Health > 0, "Health should be set from GameStarted");
        Assert.True(startedSession.HudProjection.WalletCash > 0, "Wallet should be set from GameStarted");
        Assert.Equal(townId, startedSession.HudProjection.CurrentTownId.Value);
    }

    /// <summary>
    /// The prologue response represents the committed setup phase without
    /// manufacturing a gameplay HUD before GameStarted.
    /// </summary>
    [Fact]
    public async Task ViewPrologue_ReturnedDtoOmitsHudUntilGameStarts()
    {
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        // Step 1: setup
        var setupResponse = await _client.CreateSetupOnlyGameAsync(scenario);

        // Step 2: view prologue — the response retains setup state but no gameplay HUD.
        var prologueResponse = await _client.PostAsync(
            $"/api/games/{setupResponse.Id}/prologue-viewed", content: null);
        prologueResponse.EnsureSuccessStatusCode();
        var prologueSession = await prologueResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        ArgumentNullException.ThrowIfNull(prologueSession);

        Assert.Null(prologueSession!.HudProjection);
        Assert.Null(prologueSession.Inventory);
        Assert.Null(prologueSession.Player.Health);
        Assert.Equal(StartFlowPhase.PrologueViewed, prologueSession.StartFlowPhase);
    }
}
