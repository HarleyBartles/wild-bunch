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

        var scenario = BoringScenarioBuilder.StartingTownServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownServices(client, createdSession!.Id, createdSession!);

        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/towns/{createdSession.Player.CurrentTownId}/store-offers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalog = await response.Content.ReadFromJsonAsync<TownStoreOffersDto>();

        Assert.NotNull(catalog);
        Assert.True(catalog!.Available);
        Assert.Equal(createdSession.Player.CurrentTownId, catalog.TownId);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == WildBunch.Domain.Economy.StoreVendorType.GeneralStore);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == WildBunch.Domain.Economy.StoreVendorType.Stable);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTownStoreOffersReturnsNotFoundForUnknownTown()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownServicesOrWantedPosterReady();
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

        var scenario = BoringScenarioBuilder.StartingTownServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownServices(client, createdSession!.Id, createdSession!);

        // Every town now has a prosperity-based store. Pick a town that is not
        // the current town — the catalog should still be available with general
        // store offers.
        var nonCurrentTownId = createdSession.World.Towns
            .Where(town => town.Id != createdSession.Player.CurrentTownId)
            .Select(town => town.Id)
            .First();
        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/towns/{nonCurrentTownId}/store-offers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalog = await response.Content.ReadFromJsonAsync<TownStoreOffersDto>();

        Assert.NotNull(catalog);
        Assert.True(catalog!.Available);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == WildBunch.Domain.Economy.StoreVendorType.GeneralStore);
    }
}
