using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiStoreOffersTests
{
    [Fact]
    public async Task GetTownStoreOffersReturnsTownCatalogForCurrentTown()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);

        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/towns/{createdSession.Player.CurrentTownId}/store-offers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalog = await response.Content.ReadFromJsonAsync<TownStoreOffersDto>();

        Assert.NotNull(catalog);
        Assert.Equal(createdSession.Player.CurrentTownId, catalog.TownId);
        Assert.Contains(catalog.Offers, offer => offer.ItemKind == WildBunch.Domain.Inventory.ItemKind.Food);
        Assert.Contains(catalog.Offers, offer => offer.ItemKind == WildBunch.Domain.Inventory.ItemKind.Horse);
        Assert.Equal(catalog.Offers.Count, catalog.Offers.Select(offer => offer.ItemKind).Distinct().Count());

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTownStoreOffersReturnsNotFoundForUnknownTown()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);

        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/towns/missing-town/store-offers");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTownStoreOffersReturnsAvailableCatalogForNonCurrentTown()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);

        // Catalog reads are available for towns other than the player's current town.
        var nonCurrentTownId = createdSession.World.Towns
            .Where(town => town.Id != createdSession.Player.CurrentTownId)
            .Select(town => town.Id)
            .First();
        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/towns/{nonCurrentTownId}/store-offers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalog = await response.Content.ReadFromJsonAsync<TownStoreOffersDto>();

        Assert.NotNull(catalog);
        Assert.NotEmpty(catalog!.Offers);
    }
}
