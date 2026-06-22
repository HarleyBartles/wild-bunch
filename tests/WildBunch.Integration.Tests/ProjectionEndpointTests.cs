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

    [Fact]
    public async Task GetAuditProjection_ReturnsProjectionFromEventStream()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(created);

        var auditResponse = await _client.GetAsync($"/api/games/{created!.Id}/projections/audit");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var audit = await auditResponse.Content.ReadFromJsonAsync<FullAuditProjection>();
        Assert.NotNull(audit);
        Assert.NotEmpty(audit!.Entries);
        Assert.Equal("GameStarted", audit.Entries[0].EventType);
    }

    [Fact]
    public async Task GetHudProjection_Returns404_WhenSessionDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/games/{Guid.NewGuid()}/projections/hud");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record GameSessionDto(Guid Id);
}
