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
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var scenario = ScenarioSeedCatalog.CanonicalPinecrossServices;
        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
        await scenario.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/towns/pinecross/store-offers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalog = await response.Content.ReadFromJsonAsync<TownStoreOffersDto>();

        Assert.NotNull(catalog);
        Assert.True(catalog!.Available);
        Assert.Equal("pinecross", catalog.TownId);
        Assert.Equal("Pinecross", catalog.TownName);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == WildBunch.Domain.Economy.StoreVendorType.GeneralStore);
        Assert.Contains(catalog.Offers, offer => offer.VendorType == WildBunch.Domain.Economy.StoreVendorType.Stable);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTownStoreOffersReturnsNotFoundForUnknownTown()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var scenario = ScenarioSeedCatalog.CanonicalPinecrossServices;
        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/towns/missing-town/store-offers");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTownStoreOffersReturnsUnavailableCatalogForTownWithoutStoreServices()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var scenario = ScenarioSeedCatalog.CanonicalPinecrossServices;
        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
        await scenario.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/towns/dryfork/store-offers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalog = await response.Content.ReadFromJsonAsync<TownStoreOffersDto>();

        Assert.NotNull(catalog);
        Assert.False(catalog!.Available);
        Assert.Empty(catalog.Offers);
    }
}
