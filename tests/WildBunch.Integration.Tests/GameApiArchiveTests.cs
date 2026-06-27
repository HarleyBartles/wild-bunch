using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiArchiveTests
{
    [Fact]
    public async Task ArchiveGameReturnsArchivedSessionAndPersists()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(createdSession);
        Assert.Equal(GameStatus.Active, createdSession!.Status);

        var archiveResponse = await client.PostAsync($"/api/games/{createdSession.Id}/archive", content: null);

        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);

        var archiveResult = await archiveResponse.Content.ReadFromJsonAsync<ArchivePlaythroughResultDto>();
        Assert.NotNull(archiveResult);
        Assert.Equal(GameStatus.Archived, archiveResult!.Status);
        Assert.Equal("Ranger Vale", archiveResult.PlayerName);
        Assert.Equal(createdSession.Id, archiveResult.SessionId);

        var getResponse = await client.GetAsync($"/api/games/{createdSession.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedSession = await getResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(fetchedSession);
        Assert.Equal(createdSession.Id, fetchedSession!.Id);
        Assert.Equal(GameStatus.Archived, fetchedSession.Status);
    }

    [Fact]
    public async Task ArchiveGameReturnsNotFoundForMissingSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var archiveResponse = await client.PostAsync($"/api/games/{Guid.NewGuid()}/archive", content: null);

        Assert.Equal(HttpStatusCode.NotFound, archiveResponse.StatusCode);
    }

    [Fact]
    public async Task ArchiveGameReturnsConflictForDoubleArchive()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(createdSession);

        var firstArchive = await client.PostAsync($"/api/games/{createdSession!.Id}/archive", content: null);
        Assert.Equal(HttpStatusCode.OK, firstArchive.StatusCode);

        var secondArchive = await client.PostAsync($"/api/games/{createdSession.Id}/archive", content: null);

        Assert.Equal(HttpStatusCode.Conflict, secondArchive.StatusCode);
    }
}
