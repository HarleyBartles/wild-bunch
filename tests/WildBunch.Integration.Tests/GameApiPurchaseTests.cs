using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json;
using WildBunch.Application.Dev.Models;
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

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);

        var persistedBefore = await ReadPersistedSnapshotAsync(client, createdSession.Id);

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
        Assert.Equal(persistedBefore.Game.Inventory!.Wallet, result.CurrentSession.Inventory!.Wallet);
        AssertPersistedSnapshotUnchanged(persistedBefore, await ReadPersistedSnapshotAsync(client, createdSession.Id));
    }

    [Fact]
    public async Task PostStoreBuyReturnsSuccessFalseForInsufficientCash()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);

        var enterStoreResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/towns/{createdSession.Player.CurrentTownId}/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Inventory.ItemKind.Food, 1));
        Assert.Equal(HttpStatusCode.OK, enterStoreResponse.StatusCode);
        var enterStoreResult = await enterStoreResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();
        Assert.NotNull(enterStoreResult);
        Assert.True(enterStoreResult!.Success);

        var persistedBefore = await ReadPersistedSnapshotAsync(client, createdSession.Id);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/towns/{createdSession.Player.CurrentTownId}/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Inventory.ItemKind.HorseFeed, 100));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("Not enough cash.", result.Message);
        Assert.Equal(persistedBefore.Game.Inventory!.Wallet, result.CurrentSession.Inventory!.Wallet);
        Assert.Equal(3, result.CurrentSession.Inventory!.Items.Single(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.HorseFeed).Quantity);
        AssertPersistedSnapshotUnchanged(persistedBefore, await ReadPersistedSnapshotAsync(client, createdSession.Id));
    }

    [Fact]
    public async Task PostStoreBuyReturnsSuccessFalseForItemAbsentFromCurrentCatalog()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);
        var persistedBefore = await ReadPersistedSnapshotAsync(client, createdSession.Id);

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
        AssertPersistedSnapshotUnchanged(persistedBefore, await ReadPersistedSnapshotAsync(client, createdSession.Id));
    }

    private static async Task<PersistedSnapshot> ReadPersistedSnapshotAsync(HttpClient client, Guid gameId)
    {
        using var gameResponse = await client.GetAsync($"/api/games/{gameId}");
        Assert.Equal(HttpStatusCode.OK, gameResponse.StatusCode);
        var game = await gameResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(game);

        using var journalResponse = await client.GetAsync($"/api/games/{gameId}/journal");
        Assert.Equal(HttpStatusCode.OK, journalResponse.StatusCode);
        var journal = await journalResponse.Content.ReadFromJsonAsync<JournalDto>();
        Assert.NotNull(journal);

        using var auditResponse = await client.GetAsync($"/api/dev/sessions/{gameId}/audit");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var audit = await auditResponse.Content.ReadFromJsonAsync<SessionAuditDto>();
        Assert.NotNull(audit);

        return new PersistedSnapshot(game!, journal!, audit!);
    }

    private static void AssertPersistedSnapshotUnchanged(PersistedSnapshot before, PersistedSnapshot after)
    {
        Assert.Equal(before.Game.Status, after.Game.Status);
        Assert.Equal(before.Game.Player, after.Game.Player);
        Assert.Equal(before.Game.Clock, after.Game.Clock);
        Assert.Equal(before.Game.Inventory!.Wallet, after.Game.Inventory!.Wallet);
        Assert.Equal(before.Game.Inventory.HorseState, after.Game.Inventory.HorseState);
        Assert.Equal(before.Game.Inventory.CanteenState, after.Game.Inventory.CanteenState);
        Assert.Equal(before.Game.Inventory.Capabilities, after.Game.Inventory.Capabilities);
        Assert.Equal(
            before.Game.Inventory.Items.Select(item => (item.Kind, item.Quantity, item.HorseState, item.CanteenState)),
            after.Game.Inventory.Items.Select(item => (item.Kind, item.Quantity, item.HorseState, item.CanteenState)));
        Assert.Equal(
            before.Game.LogEntries.Select(entry => (entry.Kind, entry.Message, entry.Day, entry.Turn)),
            after.Game.LogEntries.Select(entry => (entry.Kind, entry.Message, entry.Day, entry.Turn)));
        Assert.Equal(before.Journal.Status, after.Journal.Status);
        Assert.Equal(before.Journal.Clock, after.Journal.Clock);
        Assert.Equal(before.Journal.CurrentTown, after.Journal.CurrentTown);
        Assert.Equal(
            before.Journal.LogEntries.Select(entry => (entry.Kind, entry.Message, entry.Day, entry.Turn)),
            after.Journal.LogEntries.Select(entry => (entry.Kind, entry.Message, entry.Day, entry.Turn)));
        Assert.Equal(
            before.Audit.Entries.Select(entry => (entry.Sequence, entry.EventType, entry.Summary, entry.OccurredAtUtc)),
            after.Audit.Entries.Select(entry => (entry.Sequence, entry.EventType, entry.Summary, entry.OccurredAtUtc)));
    }

    private sealed record PersistedSnapshot(GameSessionDto Game, JournalDto Journal, SessionAuditDto Audit);
}
