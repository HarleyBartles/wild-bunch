using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WildBunch.Api;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;

namespace WildBunch.Integration.Tests;

public sealed class GameApiTests
{
    [Fact]
    public async Task PostGamesReturnsCreatedSession()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session!.Id);
        Assert.Equal("Ranger Vale", session.Player.Name);
        Assert.Equal("briar-glen", session.Player.CurrentTownId);
        Assert.Equal(WildBunch.Domain.Game.GameStatus.Active, session.Status);
        Assert.NotEmpty(session.LogEntries);
    }

    [Fact]
    public async Task GetGameByIdReturnsCreatedSession()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var getResponse = await client.GetAsync($"/api/games/{createdSession!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedSession = await getResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(fetchedSession);
        Assert.Equal(createdSession.Id, fetchedSession!.Id);
        Assert.Equal(createdSession.Player.Name, fetchedSession.Player.Name);
    }

    [Fact]
    public async Task PostTravelToConnectedTownReturnsSuccessAndUpdatedState()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest("cinder-ford"));

        Assert.Equal(HttpStatusCode.OK, travelResponse.StatusCode);

        var turnResult = await travelResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(turnResult);
        Assert.True(turnResult!.Success);
        Assert.Equal("Travelled to cinder-ford.", turnResult.Message);
        Assert.Equal("cinder-ford", turnResult.CurrentSession.Player.CurrentTownId);
        Assert.Equal(10, turnResult.CurrentSession.Player.Supplies);
        Assert.Equal(1, turnResult.CurrentSession.Clock.Turn);
        Assert.Equal(1, turnResult.CurrentSession.PursuitState.Heat);
    }

    [Fact]
    public async Task GetMissingGameReturnsNotFound()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TravelMissingGameReturnsNotFound()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/games/{Guid.NewGuid()}/travel",
            new TravelRequest("cinder-ford"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
