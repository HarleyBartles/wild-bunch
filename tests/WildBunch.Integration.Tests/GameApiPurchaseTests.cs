using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiPurchaseTests
{
    [Fact]
    public async Task GetGameOmitsRetiredRifleCapabilityFromInventoryContract()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();
        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        using var response = await client.GetAsync($"/api/games/{createdSession!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var capabilities = document.RootElement.GetProperty("inventory").GetProperty("capabilities");

        Assert.False(capabilities.TryGetProperty("rifleUsable", out _));
    }

    [Fact]
    public async Task PostStoreBuySucceedsForCurrentTownOfferAndReturnsUpdatedState()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/{createdSession.Player.CurrentTownId}/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Inventory.ItemKind.Food, 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("Purchased 2 Food for $4.00.", result.Message);
        Assert.Equal(21m, result.CurrentSession.Inventory!.Wallet.Cash);
        Assert.Equal(6, result.CurrentSession.Inventory!.Items.Single(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Food).Quantity);
        Assert.Equal(createdSession.LogEntries.Count + 1, result.CurrentSession.LogEntries.Count);
        Assert.Equal(WildBunch.Domain.Game.GameLogEntryKind.Purchase, result.CurrentSession.LogEntries.Last().Kind);
    }

    [Fact]
    public async Task PostStoreBuyReturnsSuccessFalseWhenTownDoesNotMatchCurrentTown()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);

        // Pick a town that is not the current town to trigger the mismatch path.
        var nonCurrentTownId = createdSession.World.Towns
            .Where(town => town.Id != createdSession.Player.CurrentTownId)
            .Select(town => town.Id)
            .First();

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/{nonCurrentTownId}/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Inventory.ItemKind.Food, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("You must be in that town to buy there.", result.Message);
        Assert.Equal(createdSession.Player.CurrentTownId, result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(25m, result.CurrentSession.Inventory!.Wallet.Cash);
    }

    [Fact]
    public async Task PostStoreBuyReturnsSuccessFalseForInsufficientCash()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/{createdSession.Player.CurrentTownId}/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Inventory.ItemKind.HorseFeed, 100));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("Not enough cash.", result.Message);
        Assert.Equal(25m, result.CurrentSession.Inventory!.Wallet.Cash);
        Assert.Equal(3, result.CurrentSession.Inventory!.Items.Single(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.HorseFeed).Quantity);
    }

    [Fact]
    public async Task PostStoreBuyReturnsSuccessFalseForItemAbsentFromCurrentCatalog()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);

        // Former persisted item-kind values are not valid store purchases.
        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/{createdSession.Player.CurrentTownId}/store/buy",
            new BuyStoreItemRequest((WildBunch.Domain.Inventory.ItemKind)8, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("That item is not offered at this store.", result.Message);
        Assert.Equal(25m, result.CurrentSession.Inventory!.Wallet.Cash);
        Assert.Equal(createdSession.Inventory!.Items, result.CurrentSession.Inventory.Items);
        Assert.Equal(createdSession.LogEntries.Count, result.CurrentSession.LogEntries.Count);
    }
}
