using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Dev;

public sealed class DevEndpointTests
{
    [Fact]
    public async Task GetSessionAudit_Returns200_WithAuditEntriesInDevEnvironment()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(created);

        var auditResponse = await client.GetAsync($"/api/dev/sessions/{created!.Id}/audit");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var payload = await auditResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"entries\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GameStarted", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSessionAudit_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();

        // Even a valid session ID should be denied — the guard runs before the handler.
        var auditResponse = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/audit");
        Assert.Equal(HttpStatusCode.Forbidden, auditResponse.StatusCode);
    }

    [Fact]
    public async Task GetSessionAudit_Returns404_WhenSessionDoesNotExist()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var auditResponse = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/audit");
        Assert.Equal(HttpStatusCode.NotFound, auditResponse.StatusCode);
    }

    [Fact]
    public async Task PlayerFacingAuditPath_StillReturns404()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(created);

        // The player-facing audit path must remain closed even though /api/dev/ exists.
        var playerAuditResponse = await client.GetAsync($"/api/games/{created!.Id}/projections/audit");
        Assert.Equal(HttpStatusCode.NotFound, playerAuditResponse.StatusCode);
    }
}
