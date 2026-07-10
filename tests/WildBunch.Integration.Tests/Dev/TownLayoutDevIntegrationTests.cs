using System.Net;
using System.Net.Http.Json;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Integration.Tests.TestInfrastructure;
using Xunit;

namespace WildBunch.Integration.Tests.Dev;

public sealed class TownLayoutDevIntegrationTests
{
    [Fact(Skip = "Set ConnectionStrings__WildBunchPostgresDb to run the PostgreSQL test lane.")]
    public async Task ThreePhaseFlow_PrepInjectStart_UsesDevLayoutSalts()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        // Phase 1: Prep
        var seedCode = "test-seed-code";
        var prepCommand = new
        {
            SeedCode = seedCode,
            Difficulty = "Standard",
            Entropy = "Classic"
        };
        var prepResponse = await client.PostAsJsonAsync("/api/dev/games/prep", prepCommand);
        Assert.Equal(HttpStatusCode.OK, prepResponse.StatusCode);

        var prepResult = await prepResponse.Content.ReadFromJsonAsync<PrepGameSessionResult>();
        Assert.NotNull(prepResult);
        var sessionId = prepResult!.GameSessionId;

        // Verify prepped session state
        var sessionResponse = await client.GetAsync($"/api/games/{sessionId}");
        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        var session = await sessionResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(session);
        Assert.Equal(GameStatus.Prepped, session!.Status);

        // Phase 2: Inject dev salts
        var devSalts = new TownLayoutSaltsDto("1.0.0", "dev-buildings", "dev-roads", "dev-dirt", "dev-props");
        var setSaltsResponse = await client.PostAsJsonAsync($"/api/dev/sessions/{sessionId}/town-layout/set-salts", devSalts);
        Assert.Equal(HttpStatusCode.NoContent, setSaltsResponse.StatusCode);

        // Verify dev salts were set via the dev-specific endpoint
        var saltsResponse = await client.GetAsync($"/api/dev/sessions/{sessionId}/town-layout/salts");
        Assert.Equal(HttpStatusCode.OK, saltsResponse.StatusCode);
        var saltsDto = await saltsResponse.Content.ReadFromJsonAsync<TownLayoutSaltsDto>();
        Assert.NotNull(saltsDto);
        Assert.Equal("1.0.0", saltsDto!.ResolverVersion);
        Assert.Equal("dev-buildings", saltsDto.BuildingsSalt);
        Assert.Equal("dev-roads", saltsDto.RoadsSalt);
        Assert.Equal("dev-dirt", saltsDto.DirtSalt);
        Assert.Equal("dev-props", saltsDto.PropsSalt);

        // Phase 3: Start
        var startResponse = await client.PostAsync($"/api/dev/games/{sessionId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        // Verify session is now active
        sessionResponse = await client.GetAsync($"/api/games/{sessionId}");
        session = await sessionResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(session);
        Assert.Equal(GameStatus.Active, session!.Status);
        Assert.NotNull(session.World);
        Assert.NotNull(session.CaseFile);
    }

    [Fact(Skip = "Set ConnectionStrings__WildBunchPostgresDb to run the PostgreSQL test lane.")]
    public async Task SetLayoutSalts_Returns400ForActiveSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        // Create and start a session via the normal flow
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();
        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        Assert.NotNull(createdSession);

        // Try to set dev salts on active session
        var devSalts = new TownLayoutSaltsDto("1.0.0", "dev-buildings", "dev-roads", "dev-dirt", "dev-props");
        var setSaltsResponse = await client.PostAsJsonAsync($"/api/dev/sessions/{createdSession!.Id}/town-layout/set-salts", devSalts);
        Assert.Equal(HttpStatusCode.BadRequest, setSaltsResponse.StatusCode);
    }

    [Fact(Skip = "Set ConnectionStrings__WildBunchPostgresDb to run the PostgreSQL test lane.")]
    public async Task GenerateRandomSalts_ReturnsValidSalts()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        // Create a prepped session first
        var seedCode = "test-seed-code";
        var prepCommand = new
        {
            SeedCode = seedCode,
            Difficulty = "Standard",
            Entropy = "Classic"
        };
        var prepResponse = await client.PostAsJsonAsync("/api/dev/games/prep", prepCommand);
        Assert.Equal(HttpStatusCode.OK, prepResponse.StatusCode);

        var prepResult = await prepResponse.Content.ReadFromJsonAsync<PrepGameSessionResult>();
        Assert.NotNull(prepResult);
        var sessionId = prepResult!.GameSessionId;

        var response = await client.PostAsync($"/api/dev/sessions/{sessionId}/town-layout/generate-random", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saltsDto = await response.Content.ReadFromJsonAsync<TownLayoutSaltsDto>();
        Assert.NotNull(saltsDto);
        Assert.NotNull(saltsDto!.ResolverVersion);
        Assert.NotNull(saltsDto.BuildingsSalt);
        Assert.NotNull(saltsDto.RoadsSalt);
        Assert.NotNull(saltsDto.DirtSalt);
        Assert.NotNull(saltsDto.PropsSalt);
    }
}
