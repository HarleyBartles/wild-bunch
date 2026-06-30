using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Actions;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiActionsTests
{
    [Fact]
    public async Task GetAvailableActionsReturnsExpectedActionsForCreatedGame()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/actions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actions = await response.Content.ReadFromJsonAsync<AvailableActionDto[]>();

        Assert.NotNull(actions);
        Assert.Contains(actions!, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(actions!, action => action.Kind == AvailableActionKind.ViewMap);
        Assert.Contains(actions!, action => action.Kind == AvailableActionKind.ViewJournal);
        Assert.Contains(actions!, action => action.Kind == AvailableActionKind.BuySupplies);
        Assert.Contains(actions!, action => action.Kind == AvailableActionKind.ReadWantedPosters);
        Assert.Contains(actions!, action => action.Kind == AvailableActionKind.InspectNoticeBoard);
        Assert.Contains(actions!, action => action.Kind == AvailableActionKind.CheckSheriffRecords);
        Assert.Contains(actions!, action => action.Kind == AvailableActionKind.GatherLocalGossip);
        Assert.Contains(actions!, action => action.Kind == AvailableActionKind.FollowTelegraphLeads);
        Assert.Contains(actions!, action => action.Kind == AvailableActionKind.SendTelegram);
    }

    [Fact]
    public async Task GetMissingGameActionsReturnsNotFound()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}/actions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
