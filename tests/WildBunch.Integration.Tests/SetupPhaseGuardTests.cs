using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

/// <summary>
/// Verifies that gameplay commands invoked during the setup phase return
/// 409 Conflict via the centralized SetupPhaseException → IExceptionHandler
/// mapping. The guard lives in GameSessionCommandHandler.ExecuteWithRetryAsync,
/// not in individual handlers or domain methods.
/// </summary>
public sealed class SetupPhaseGuardTests
{
    [Fact]
    public async Task TravelDuringSetupPhaseReturnsConflict()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownServicesOrWantedPosterReady();
        scenario.AssertReady();
        var setupSession = await client.CreateSetupOnlyGameAsync(scenario);

        // Pick any destination town from the generated world.
        var destinationTown = setupSession.World.Towns.Last().Id;

        var response = await client.PostAsJsonAsync(
            $"/api/games/{setupSession.Id}/travel",
            new TravelRequest(destinationTown));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseDuringSetupPhaseReturnsConflict()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownServicesOrWantedPosterReady();
        scenario.AssertReady();
        var setupSession = await client.CreateSetupOnlyGameAsync(scenario);

        var townId = setupSession.World.Towns.First().Id;

        var response = await client.PostAsJsonAsync(
            $"/api/games/{setupSession.Id}/towns/{townId}/store/buy",
            new BuyStoreItemRequest(
                StoreVendorType.GeneralStore,
                ItemKind.Food,
                1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task InvestigationActionDuringSetupPhaseReturnsConflict()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownServicesOrWantedPosterReady();
        scenario.AssertReady();
        var setupSession = await client.CreateSetupOnlyGameAsync(scenario);

        var response = await client.PostAsync(
            $"/api/games/{setupSession.Id}/investigations/notice-board/inspect",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PreviewTravelDuringSetupPhaseReturnsFailureDto()
    {
        // PreviewTravel is a query, not a command — it does not go through
        // ExecuteWithRetryAsync. It returns a failure DTO with Success=false
        // instead of throwing SetupPhaseException.
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownServicesOrWantedPosterReady();
        scenario.AssertReady();
        var setupSession = await client.CreateSetupOnlyGameAsync(scenario);

        var destinationTown = setupSession.World.Towns.Last().Id;

        var response = await client.GetAsync(
            $"/api/games/{setupSession.Id}/travel/preview/{destinationTown}");

        // Query handlers return 200 with a failure DTO, not 409.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = await response.Content.ReadFromJsonAsync<TravelPreviewResultDto>();
        Assert.NotNull(preview);
        Assert.False(preview!.Success);
    }

    [Fact]
    public async Task ArchiveDuringSetupPhaseSucceeds()
    {
        // Archive is a lifecycle command — it opts out of the setup-phase guard
        // via RequiresGameStarted => false. Archiving a setup-phase session is valid.
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownServicesOrWantedPosterReady();
        scenario.AssertReady();
        var setupSession = await client.CreateSetupOnlyGameAsync(scenario);

        var response = await client.PostAsync(
            $"/api/games/{setupSession.Id}/archive",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

