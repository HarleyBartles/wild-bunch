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
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/pinecross/store/buy",
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
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/redmesa/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Economy.StoreVendorType.GeneralStore, WildBunch.Domain.Inventory.ItemKind.Food, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("You must be in that town to buy there.", result.Message);
        Assert.Equal("pinecross", result.CurrentSession.Player.CurrentTownId);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
    }

    [Fact]
    public async Task PostStoreBuyReturnsSuccessFalseForInsufficientCash()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/pinecross/store/buy",
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
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/pinecross/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Economy.StoreVendorType.Gunsmith, WildBunch.Domain.Inventory.ItemKind.RifleAmmo, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("That store offer is not available in this town.", result.Message);
        Assert.Equal(25m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(createdSession.LogEntries.Count, result.CurrentSession.LogEntries.Count);
    }
}
