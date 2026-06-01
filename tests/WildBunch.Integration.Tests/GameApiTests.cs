using System.Net;
using System.Net.Http.Json;
using WildBunch.Api;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiTests
{
    [Fact]
    public async Task PostGamesReturnsCreatedSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();
        var response = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(session);
        scenario.Fixture.AssertCreatedSession(session!);
        Assert.NotEqual(Guid.Empty, session!.Id);
        Assert.Equal(WildBunch.Domain.Game.GameStatus.Active, session.Status);

        var connectedTownIds = session.World.Trails
            .Where(trail => trail.FromTownId == session.Player.CurrentTownId || trail.ToTownId == session.Player.CurrentTownId)
            .Select(trail => trail.FromTownId == session.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
            .Distinct()
            .ToArray();

        Assert.Contains(connectedTownIds, townId => townId == "redmesa");
        Assert.Contains(connectedTownIds, townId => townId == "holloway");

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"money\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"supplies\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Jonah Pike", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Evan Quill", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tessa Wren", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspect-1\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspect-2\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspectCount\"", payload, StringComparison.OrdinalIgnoreCase);

        var previewResults = new List<TravelPreviewDto>();
        foreach (var destinationTownId in connectedTownIds)
        {
            var previewResponse = await client.GetAsync($"/api/games/{session.Id}/travel/preview/{destinationTownId}");
            Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

            var previewResult = await previewResponse.Content.ReadFromJsonAsync<TravelPreviewResultDto>();
            Assert.NotNull(previewResult);
            Assert.True(previewResult!.Success);
            Assert.NotNull(previewResult.Preview);
            previewResults.Add(previewResult.Preview!);
        }

        Assert.True(previewResults.Select(preview => preview.BaselineRideDays).Distinct().Count() > 1);
    }

    [Fact]
    public async Task GetGameByIdReturnsCreatedSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);

        var getResponse = await client.GetAsync($"/api/games/{createdSession!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedSession = await getResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(fetchedSession);
        Assert.Equal(createdSession.Id, fetchedSession!.Id);
        Assert.Equal(createdSession.Player.Name, fetchedSession.Player.Name);
        Assert.Equal(createdSession.Inventory.Wallet.Cash, fetchedSession.Inventory.Wallet.Cash);
        Assert.Equal(createdSession.CaseFile.OpeningLead, fetchedSession.CaseFile.OpeningLead);
        Assert.Empty(fetchedSession.CaseFile.DiscoveredSuspects);

        var payload = await getResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"money\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"supplies\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Jonah Pike", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspect-1\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspect-2\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TravelPreviewStartAndAdvanceFollowTheJourneyLoop()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);
        var startingFood = createdSession!.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Food).Quantity;
        var startingHorseFeed = createdSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.HorseFeed).Quantity;

        var previewResponse = await client.GetAsync($"/api/games/{createdSession.Id}/travel/preview/holloway");

        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var preview = await previewResponse.Content.ReadFromJsonAsync<TravelPreviewResultDto>();

        Assert.NotNull(preview);
        Assert.True(preview!.Success);
        Assert.NotNull(preview.Preview);
        Assert.Equal(TravelMode.Mounted, preview.Preview!.TravelMode);
        Assert.Equal(2m, preview.Preview.RideDayDistance);
        Assert.Equal(2, preview.Preview.BaselineRideDays);
        Assert.Equal(2, preview.Preview.ExpectedDays);
        Assert.True(preview.Preview.MountedTravelAvailable);
        Assert.Equal(0, preview.Preview.RequiredHorseFeed);
        Assert.Equal(2, preview.Preview.RequiredFood);
        Assert.Equal(0, preview.Preview.RequiredCanteenCharges);
        Assert.Equal(0, preview.Preview.CanteenChargesPerDay);
        Assert.Equal(0, preview.Preview.DelayMarginDays);
        Assert.NotNull(preview.Preview.RouteProfile);
        Assert.Equal(2m, preview.Preview.RouteProfile.RideDayDistance);
        Assert.Equal(WildBunch.Domain.World.TrailRisk.Moderate, preview.Preview.RouteProfile.Risk);
        Assert.Equal(WildBunch.Domain.World.TrailTerrain.OpenRange, preview.Preview.RouteProfile.Terrain);
        Assert.Equal(WildBunch.Domain.World.WaterFeature.Creek, preview.Preview.RouteProfile.WaterFeature);

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/travel",
            new TravelRequest("holloway"));

        Assert.Equal(HttpStatusCode.OK, travelResponse.StatusCode);

        var turnResult = await travelResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(turnResult);
        Assert.True(turnResult!.Success);
        Assert.Equal(JourneyStatus.Active, turnResult.JourneyStatus);
        Assert.NotNull(turnResult.Journey);
        Assert.Equal(TravelMode.Mounted, turnResult.Journey!.TravelMode);
        Assert.Equal(2m, turnResult.Journey.RideDayDistance);
        Assert.Equal(2, turnResult.Journey.ExpectedDays);
        Assert.Equal(0, turnResult.Journey.DelayDays);
        Assert.Equal("pinecross", turnResult.CurrentSession.Player.CurrentTownId);
        Assert.Equal(2, turnResult.CurrentSession.Clock.Day);
        Assert.Equal(0, turnResult.CurrentSession.Clock.Turn);
        Assert.NotNull(turnResult.CurrentSession.Journey);
        Assert.Equal(1, turnResult.CurrentSession.Journey!.RemainingDays);
        Assert.Equal(1m, turnResult.CurrentSession.Journey.RemainingRideDayDistance);
        Assert.Equal(startingFood - 1, turnResult.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Food).Quantity);
        Assert.Equal(startingHorseFeed, turnResult.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.HorseFeed).Quantity);
        Assert.Equal(10, turnResult.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Canteen).CanteenState!.Charges);
        Assert.NotNull(turnResult.TravelDiary);
        var openingDay = Assert.Single(turnResult.TravelDiary!.Days);
        Assert.NotNull(openingDay.OpeningNarration);
        Assert.Contains("I set out for Holloway", openingDay.OpeningNarration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"{preview.Preview.BaselineRideDays}-day", openingDay.OpeningNarration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("by mounted travel", openingDay.OpeningNarration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("without a horse", openingDay.OpeningNarration, StringComparison.OrdinalIgnoreCase);

        var firstAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, firstAdvanceResponse.StatusCode);

        var firstAdvance = await firstAdvanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(firstAdvance);
        Assert.True(firstAdvance!.Success);
        Assert.Equal(JourneyStatus.Completed, firstAdvance.JourneyStatus);
        Assert.Equal("holloway", firstAdvance.CurrentSession.Player.CurrentTownId);
        Assert.Equal(3, firstAdvance.CurrentSession.Clock.Day);
        Assert.Equal(0, firstAdvance.CurrentSession.Clock.Turn);
        Assert.NotNull(firstAdvance.CurrentSession.Journey);
        Assert.Equal(JourneyStatus.Completed, firstAdvance.CurrentSession.Journey!.Status);
        Assert.Equal(2, firstAdvance.TravelDiary!.Days.Count);
        Assert.Equal(JourneyStatus.Completed, firstAdvance.TravelDiary.Days[^1].Status);
        Assert.Equal(startingFood - 2, firstAdvance.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Food).Quantity);
        Assert.Equal(startingHorseFeed, firstAdvance.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.HorseFeed).Quantity);
        Assert.Equal(10, firstAdvance.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Canteen).CanteenState!.Charges);

        var payload = await firstAdvanceResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Jonah Pike", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"money\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"supplies\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostGamesWithSubmittedSeedCodeKeepsTheNoHorseOptionsAndExposesASixDayRoute()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.NoHorseFootTravelReady();
        scenario.AssertReady();

        var response = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdSession = await response.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);

        var connectedTownIds = createdSession.World.Trails
            .Where(trail => trail.FromTownId == createdSession.Player.CurrentTownId || trail.ToTownId == createdSession.Player.CurrentTownId)
            .Select(trail => trail.FromTownId == createdSession.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
            .Distinct()
            .ToArray();

        Assert.Contains(connectedTownIds, townId => townId == "redmesa");
        Assert.Contains(connectedTownIds, townId => townId == "sagewell");

        var redMesaPreviewResponse = await client.GetAsync($"/api/games/{createdSession.Id}/travel/preview/redmesa");
        Assert.Equal(HttpStatusCode.OK, redMesaPreviewResponse.StatusCode);

        var redMesaPreviewResult = await redMesaPreviewResponse.Content.ReadFromJsonAsync<TravelPreviewResultDto>();
        Assert.NotNull(redMesaPreviewResult);
        scenario.Fixture.AssertTravelPreview(createdSession!, "redmesa", redMesaPreviewResult!);

        var travelToRedMesaResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/travel",
            new TravelRequest("redmesa"));

        Assert.Equal(HttpStatusCode.OK, travelToRedMesaResponse.StatusCode);

        var redMesaTurn = await travelToRedMesaResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(redMesaTurn);
        scenario.Fixture.AssertTravelTurn(createdSession!, "redmesa", redMesaTurn!, redMesaPreviewResult!);

        var arrivedRedMesa = await AdvanceUntilTownAsync(client, createdSession.Id, "redmesa");

        Assert.Equal("redmesa", arrivedRedMesa.CurrentSession.Player.CurrentTownId);

        var acknowledgeRedMesaResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/arrival/acknowledge", content: null);

        Assert.Equal(HttpStatusCode.OK, acknowledgeRedMesaResponse.StatusCode);

        var dryForkPreviewResponse = await client.GetAsync($"/api/games/{createdSession.Id}/travel/preview/dryfork");
        Assert.Equal(HttpStatusCode.OK, dryForkPreviewResponse.StatusCode);

        var dryForkPreviewResult = await dryForkPreviewResponse.Content.ReadFromJsonAsync<TravelPreviewResultDto>();
        Assert.NotNull(dryForkPreviewResult);
        scenario.Fixture.AssertDryFootRoute(createdSession!, "dryfork", dryForkPreviewResult!);

        var destinationTownId = "dryfork";
        var onFootTravelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/travel",
            new TravelRequest(destinationTownId));

        Assert.Equal(HttpStatusCode.OK, onFootTravelResponse.StatusCode);

        var onFootTurn = await onFootTravelResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(onFootTurn);
        scenario.Fixture.AssertDryFootRoute(createdSession!, destinationTownId, onFootTurn!, dryForkPreviewResult!);
    }

    [Fact]
    public async Task HighRiskTravelCanPauseResolveAndResumeWithoutSkippingTheTrail()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.HighRiskFoeInterruptRoute();
        scenario.AssertReady();
        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);

        var travelToRedMesaResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest("redmesa"));

        Assert.Equal(HttpStatusCode.OK, travelToRedMesaResponse.StatusCode);

        var redMesaTravel = await travelToRedMesaResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(redMesaTravel);
        Assert.True(redMesaTravel!.Success);
        Assert.Equal(JourneyStatus.Active, redMesaTravel.JourneyStatus);

        var completeLowRisk = await AdvanceUntilTownAsync(client, createdSession.Id, "redmesa");

        Assert.Equal(JourneyStatus.Completed, completeLowRisk.JourneyStatus);
        Assert.NotNull(completeLowRisk.CurrentSession.Journey);
        Assert.Equal("redmesa", completeLowRisk.CurrentSession.Player.CurrentTownId);
        var redMesaArrivalDay = completeLowRisk.CurrentSession.Clock.Day;

        var acknowledgeResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/arrival/acknowledge", content: null);

        Assert.Equal(HttpStatusCode.OK, acknowledgeResponse.StatusCode);

        var acknowledged = await acknowledgeResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(acknowledged);
        Assert.True(acknowledged!.Success);
        Assert.Null(acknowledged.CurrentSession.Journey);
        Assert.Equal("redmesa", acknowledged.CurrentSession.Player.CurrentTownId);

        var foodPurchaseResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/towns/redmesa/store/buy",
            new BuyStoreItemRequest(
                WildBunch.Domain.Economy.StoreVendorType.GeneralStore,
                WildBunch.Domain.Inventory.ItemKind.Food,
                6));

        Assert.Equal(HttpStatusCode.OK, foodPurchaseResponse.StatusCode);

        var foodPurchase = await foodPurchaseResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(foodPurchase);
        Assert.True(foodPurchase!.Success);
        Assert.Equal("redmesa", foodPurchase.CurrentSession.Player.CurrentTownId);

        var travelToDryForkResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/travel",
            new TravelRequest("dryfork"));

        Assert.Equal(HttpStatusCode.OK, travelToDryForkResponse.StatusCode);

        var dryForkTravel = await travelToDryForkResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(dryForkTravel);
        Assert.False(dryForkTravel!.Success);
        Assert.Equal(JourneyStatus.Interrupted, dryForkTravel.JourneyStatus);
        Assert.NotNull(dryForkTravel.Journey);
        Assert.NotNull(dryForkTravel.Journey!.PendingEncounter);
        Assert.Equal("foe", dryForkTravel.Journey.PendingEncounter!.Kind);
        Assert.Equal(3, dryForkTravel.Journey.PendingEncounter.Choices.Count);
        Assert.Equal(new[] { "run", "fight", "bribe" }, dryForkTravel.Journey.PendingEncounter.Choices.Select(choice => choice.Id));
        Assert.Equal(10, dryForkTravel.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Canteen).CanteenState!.Charges);
        var dryForkPayload = await travelToDryForkResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("foeProfile", dryForkPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("minimumBribe", dryForkPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fightStrength", dryForkPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("resolutionAttempts", dryForkPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bribeOffersMade", dryForkPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cumulativeBribePaid", dryForkPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bribeLockedOut", dryForkPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chaseFatigue", dryForkPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("annoyance", dryForkPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shaken", dryForkPayload, StringComparison.OrdinalIgnoreCase);

        var blockedAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, blockedAdvanceResponse.StatusCode);

        var blockedAdvance = await blockedAdvanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(blockedAdvance);
        Assert.False(blockedAdvance!.Success);
        Assert.Equal(JourneyStatus.Interrupted, blockedAdvance.JourneyStatus);
        Assert.Equal(redMesaArrivalDay + 1, blockedAdvance.CurrentSession.Clock.Day);
        Assert.Equal(0, blockedAdvance.CurrentSession.Clock.Turn);

        var bribeAmount = blockedAdvance.CurrentSession.Inventory.Wallet.Cash;
        var resolveResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/travel/encounter/resolve",
            new { ChoiceId = "bribe", BribeAmount = bribeAmount });

        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);

        var resolved = await resolveResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(resolved);
        Assert.True(resolved!.Success);
        Assert.Equal(JourneyStatus.Active, resolved.JourneyStatus);
        Assert.NotNull(resolved.CurrentSession.Journey);
        Assert.Null(resolved.CurrentSession.Journey!.PendingEncounter);
        Assert.Equal(0, resolved.CurrentSession.Journey.DelayDays);
        Assert.True(resolved.CurrentSession.Journey.TravelMode is TravelMode.Foot or TravelMode.Mounted);
        Assert.Equal(redMesaArrivalDay + 1, resolved.CurrentSession.Clock.Day);
        Assert.Equal(0, resolved.CurrentSession.Clock.Turn);

        var resumeAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, resumeAdvanceResponse.StatusCode);

        var resumeAdvance = await resumeAdvanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(resumeAdvance);
        Assert.NotNull(resumeAdvance!.CurrentSession.Journey);
        Assert.Equal("redmesa", resumeAdvance.CurrentSession.Player.CurrentTownId);
        Assert.Equal(redMesaArrivalDay + 2, resumeAdvance.CurrentSession.Clock.Day);
        Assert.Equal(0, resumeAdvance.CurrentSession.Clock.Turn);

        scenario.Fixture.AssertHighRiskFoeInterruptRoute(createdSession!, dryForkTravel!, blockedAdvance!, resolved!, resumeAdvance!);
    }

    [Fact]
    public async Task GetMissingGameReturnsNotFound()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TravelMissingGameReturnsNotFound()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/games/{Guid.NewGuid()}/travel",
            new TravelRequest("dryfork"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<GameTurnResultDto> AdvanceUntilTownAsync(HttpClient client, Guid gameId, string destinationTownId)
    {
        for (var step = 0; step < 12; step++)
        {
            var advanceResponse = await client.PostAsync($"/api/games/{gameId}/travel/advance", content: null);

            Assert.Equal(HttpStatusCode.OK, advanceResponse.StatusCode);

            var advanceResult = await advanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();
            Assert.NotNull(advanceResult);

            if (!advanceResult!.Success && advanceResult.JourneyStatus == JourneyStatus.Interrupted)
            {
                var resolveResponse = await client.PostAsJsonAsync(
                    $"/api/games/{gameId}/travel/encounter/resolve",
                    new { ChoiceId = "run" });

                Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);

                var resolved = await resolveResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();
                Assert.NotNull(resolved);
                if (resolved.JourneyStatus == JourneyStatus.Completed && resolved.CurrentSession.Player.CurrentTownId == destinationTownId)
                {
                    return resolved;
                }
                continue;
            }

            Assert.True(advanceResult.Success);
            if (advanceResult.JourneyStatus == JourneyStatus.Completed && advanceResult.CurrentSession.Player.CurrentTownId == destinationTownId)
            {
                return advanceResult;
            }
        }

        throw new InvalidOperationException($"Travel to {destinationTownId} did not complete.");
    }
}
