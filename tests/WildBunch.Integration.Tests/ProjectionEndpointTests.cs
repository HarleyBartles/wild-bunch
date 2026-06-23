using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Projections;
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
        // Create a game
        var createResponse = await _client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(created);

        // Get HUD projection
        var hudResponse = await _client.GetAsync($"/api/games/{created!.Id}/projections/hud");
        Assert.Equal(HttpStatusCode.OK, hudResponse.StatusCode);
        var hud = await hudResponse.Content.ReadFromJsonAsync<HudProjection>();
        Assert.NotNull(hud);
        Assert.Equal("Ranger Vale", hud!.PlayerName);
        Assert.Equal(created.Id, hud.SessionId);
    }

    [Fact]
    public async Task GetDiaryProjection_ReturnsProjectionFromEventStream()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(created);

        var diaryResponse = await _client.GetAsync($"/api/games/{created!.Id}/projections/diary");
        Assert.Equal(HttpStatusCode.OK, diaryResponse.StatusCode);
        var diary = await diaryResponse.Content.ReadFromJsonAsync<DiaryProjection>();
        Assert.NotNull(diary);
        Assert.NotEmpty(diary!.Entries);
    }

    /// <summary>
    /// Proves the diary projection endpoint works after a persisted
    /// <see cref="WildBunch.Domain.Events.InvestigationPerformed"/> event.
    /// The DB-backed projection path loads the event stream via
    /// <see cref="WildBunch.Persistence.GameSessions.EfGameSessionRepository.GetEventStreamAsync"/>,
    /// which deserializes stored event rows through
    /// <see cref="WildBunch.Persistence.Serialization.GameSessionJsonSerializer"/>,
    /// then projects through <see cref="DiaryProjector"/>. Without the
    /// InvestigationPerformed mapping in the serializer, this test would throw
    /// "Unknown domain event type: InvestigationPerformed".
    /// </summary>
    [Fact]
    public async Task GetDiaryProjection_WorksAfterPersistedInvestigationEvent()
    {
        // 1. Create a game
        var createResponse = await _client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(created);

        // 2. Perform an investigation action via HTTP (produces + persists InvestigationPerformed)
        var investigateResponse = await _client.PostAsync(
            $"/api/games/{created!.Id}/investigations/local-gossip/gather", content: null);
        investigateResponse.EnsureSuccessStatusCode();

        // 3. Get diary projection — must deserialize the InvestigationPerformed event
        var diaryResponse = await _client.GetAsync($"/api/games/{created.Id}/projections/diary");
        Assert.Equal(HttpStatusCode.OK, diaryResponse.StatusCode);
        var diary = await diaryResponse.Content.ReadFromJsonAsync<DiaryProjection>();
        Assert.NotNull(diary);
        // GameStarted entry + InvestigationPerformed entry
        Assert.True(diary!.Entries.Count >= 2);
        Assert.Contains(diary.Entries, e => e.Summary.Contains("gossip", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAuditProjection_IsNotExposedOnPlayerFacingApi()
    {
        // Per ADR-0028 §10, full audit is a developer/replay surface, not player-facing.
        // The audit endpoint was removed from the normal game API route group.
        // This test proves the endpoint is no longer reachable.
        var createResponse = await _client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(created);

        var auditResponse = await _client.GetAsync($"/api/games/{created!.Id}/projections/audit");
        Assert.Equal(HttpStatusCode.NotFound, auditResponse.StatusCode);
    }

    [Fact]
    public async Task GetHudProjection_Returns404_WhenSessionDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/games/{Guid.NewGuid()}/projections/hud");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record GameSessionDto(Guid Id);
}
