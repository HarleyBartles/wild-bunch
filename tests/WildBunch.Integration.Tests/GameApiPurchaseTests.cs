using System.Net;
using System.Net.Http.Json;
using System.Linq;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiPurchaseTests
{
    [Fact]
    public async Task PostStoreBuySucceedsForCurrentTownOfferAndReturnsUpdatedState()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/hardpan/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Economy.StoreVendorType.GeneralStore, WildBunch.Domain.Inventory.ItemKind.Food, 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("Purchased 2 Food for $4.00.", result.Message);
        Assert.Equal(21m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(6, result.CurrentSession.Inventory.Items.Single(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Food).Quantity);
        Assert.Equal(createdSession.LogEntries.Count + 1, result.CurrentSession.LogEntries.Count);
        Assert.Equal(WildBunch.Domain.Game.GameLogEntryKind.Purchase, result.CurrentSession.LogEntries.Last().Kind);
    }

    [Fact]
    public async Task PostStoreBuyReturnsSuccessFalseWhenTownDoesNotMatchCurrentTown()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/quartzsite/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Economy.StoreVendorType.GeneralStore, WildBunch.Domain.Inventory.ItemKind.Food, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("You must be in that town to buy there.", result.Message);
        Assert.Equal("hardpan", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
    }

    [Fact]
    public async Task PostStoreBuyReturnsSuccessFalseForInsufficientCash()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/hardpan/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Economy.StoreVendorType.GeneralStore, WildBunch.Domain.Inventory.ItemKind.HorseFeed, 100));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("Not enough cash.", result.Message);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(3, result.CurrentSession.Inventory.Items.Single(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.HorseFeed).Quantity);
    }

    [Fact]
    public async Task PostStoreBuyReturnsSuccessFalseForUnavailableOffer()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        // Hardpan is Prosperous — it has a general store, stable, and gunsmith.
        // Revolver is sold by the gunsmith, not the stable. Requesting it from
        // the stable vendor triggers the "not available" path.
        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/hardpan/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Economy.StoreVendorType.Stable, WildBunch.Domain.Inventory.ItemKind.Revolver, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("That store offer is not available in this town.", result.Message);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(createdSession.LogEntries.Count, result.CurrentSession.LogEntries.Count);
    }
}
