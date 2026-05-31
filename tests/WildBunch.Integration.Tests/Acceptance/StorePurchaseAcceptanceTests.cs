using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Acceptance;

public sealed class StorePurchaseAcceptanceTests
{
    [Fact]
    public async Task PostStoreBuyConsumesCashAndPersistsTheNewInventoryTotals()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        var createdSession = await factory.SeedCanonicalSessionAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/towns/pinecross/store/buy",
            new BuyStoreItemRequest(StoreVendorType.GeneralStore, ItemKind.Food, 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("Purchased 2 Food for $4.00.", result.Message);
        Assert.Equal(21m, result.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(6, result.CurrentSession.Inventory.Items.Single(item => item.Kind == ItemKind.Food).Quantity);
        Assert.Equal(GameLogEntryKind.Purchase, result.CurrentSession.LogEntries.Last().Kind);
        Assert.Equal(createdSession.LogEntries.Count + 1, result.CurrentSession.LogEntries.Count);

        var persistedSession = await factory.LoadSessionAsync(createdSession.Id);
        Assert.Equal(21m, persistedSession.Inventory.Wallet.Cash);
        Assert.Equal(6, persistedSession.Inventory.Items.Single(item => item.Kind == ItemKind.Food).Quantity);
        Assert.Equal(GameLogEntryKind.Purchase, persistedSession.LogEntries.Last().Kind);
    }
}

