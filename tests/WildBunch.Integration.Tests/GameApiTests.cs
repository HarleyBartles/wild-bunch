using System.Net;
using System.Net.Http.Json;
using WildBunch.Api;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiTests
{
    [Fact]
    public async Task PostGamesReturnsCreatedSession()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session!.Id);
        Assert.Equal("Ranger Vale", session.Player.Name);
        Assert.Equal("pinecross", session.Player.CurrentTownId);
        Assert.Equal(WildBunch.Domain.Game.GameStatus.Active, session.Status);
        Assert.Equal(25m, session.Inventory.Wallet.Cash);
        Assert.Equal(8, session.Inventory.Items.Count);
        Assert.True(session.Inventory.Capabilities.MountedTravelAvailable);
        Assert.True(session.Inventory.Capabilities.GunfightCapable);
        Assert.False(session.Inventory.Capabilities.RifleUsable);
        Assert.Equal(6, session.World.Towns.Count);
        Assert.Equal(7, session.World.Trails.Count);
        Assert.Equal("A pale scar cuts across the left cheek.", session.CaseFile.OpeningLead);
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(0, session.CaseFile.KillerReleaseState.Progress);
        Assert.All(session.CaseFile.Suspects, suspect => Assert.NotEmpty(suspect.Profile.Aliases));
        Assert.NotEmpty(session.LogEntries);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"money\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"supplies\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetGameByIdReturnsCreatedSession()
    {
        using var factory = new SqliteApiFactory();
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
        Assert.Equal(createdSession.Inventory.Wallet.Cash, fetchedSession.Inventory.Wallet.Cash);
        Assert.Equal(createdSession.CaseFile.OpeningLead, fetchedSession.CaseFile.OpeningLead);

        var payload = await getResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"money\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"supplies\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostTravelToConnectedTownReturnsSuccessAndUpdatedState()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest("redmesa"));

        Assert.Equal(HttpStatusCode.OK, travelResponse.StatusCode);

        var turnResult = await travelResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(turnResult);
        Assert.True(turnResult!.Success);
        Assert.Equal("Travelled to redmesa.", turnResult.Message);
        Assert.Equal("redmesa", turnResult.CurrentSession.Player.CurrentTownId);
        Assert.Equal(25m, turnResult.CurrentSession.Inventory.Wallet.Cash);
        Assert.True(turnResult.CurrentSession.Inventory.Capabilities.MountedTravelAvailable);
        Assert.Equal(1, turnResult.CurrentSession.Clock.Turn);
        Assert.Equal(1, turnResult.CurrentSession.PursuitState.Heat);
        Assert.Equal(0, turnResult.CurrentSession.CaseFile.KillerReleaseState.Progress);

        var payload = await travelResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"money\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"supplies\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMissingGameReturnsNotFound()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TravelMissingGameReturnsNotFound()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/games/{Guid.NewGuid()}/travel",
            new TravelRequest("dryfork"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
