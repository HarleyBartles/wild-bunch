using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using WildBunch.Api;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiValidationTests
{
    [Fact]
    public async Task PostGamesWithBlankPlayerNameReturnsValidationProblem()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/games", new StartGameRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertValidationProblemAsync(response, "playerName");
    }

    [Fact]
    public async Task PostGamesWithInvalidSeedCodeReturnsValidationProblem()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale", SeedCode: "not-a-uuid"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertValidationProblemAsync(response, "seedCode");
    }

    [Fact]
    public async Task PostGamesWithMissingPlayerNameReturnsValidationProblem()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/games", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertValidationProblemAsync(response, "playerName");
    }

    [Fact]
    public async Task PostTravelWithBlankDestinationReturnsValidationProblem()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertValidationProblemAsync(response, "destinationTownId");
    }

    [Fact]
    public async Task PostTravelWithMissingDestinationReturnsValidationProblem()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertValidationProblemAsync(response, "destinationTownId");
    }

    [Fact]
    public async Task PostStoreBuyWithMissingFieldsReturnsValidationProblem()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/pinecross/store/buy",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertValidationProblemAsync(response, "vendorType", "itemKind");
    }

    [Fact]
    public async Task PostStoreBuyWithZeroQuantityReturnsValidationProblem()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/pinecross/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Economy.StoreVendorType.GeneralStore, WildBunch.Domain.Inventory.ItemKind.Food, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertValidationProblemAsync(response, "quantity");
    }

    [Fact]
    public async Task PostResolveJourneyEncounterWithMissingChoiceReturnsValidationProblem()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/games/{Guid.NewGuid()}/travel/encounter/resolve",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertValidationProblemAsync(response, "choiceId");
    }

    [Fact]
    public async Task GetMalformedGameIdReturnsNotFound()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/games/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetJournalWithInvalidPagingReturnsValidationProblem()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var negativeSkipResponse = await client.GetAsync($"/api/games/{createdSession!.Id}/journal?skip=-1");
        Assert.Equal(HttpStatusCode.BadRequest, negativeSkipResponse.StatusCode);
        await AssertValidationProblemAsync(negativeSkipResponse, "skip");

        var zeroTakeResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal?take=0");
        Assert.Equal(HttpStatusCode.BadRequest, zeroTakeResponse.StatusCode);
        await AssertValidationProblemAsync(zeroTakeResponse, "take");
    }

    [Fact]
    public async Task TravelToUnconnectedTownReturnsSuccessFalse()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest("nonexistent-town"));

        Assert.Equal(HttpStatusCode.OK, travelResponse.StatusCode);

        var turnResult = await travelResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(turnResult);
        Assert.False(turnResult!.Success);
        Assert.Equal("Destination town could not be found.", turnResult.Message);
        Assert.Equal("hardpan", turnResult.CurrentSession.Player.CurrentTownId);
        Assert.Equal(0, turnResult.CurrentSession.Clock.Turn);
        Assert.Equal(0, turnResult.CurrentSession.PursuitState.Heat);
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, params string[] expectedKeys)
    {
        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(validationProblem);
        Assert.Equal("One or more validation errors occurred.", validationProblem!.Title);
        foreach (var key in expectedKeys)
        {
            Assert.Contains(key, validationProblem.Errors.Keys);
            Assert.NotEmpty(validationProblem.Errors[key]);
        }
    }
}
