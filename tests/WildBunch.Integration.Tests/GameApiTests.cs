using System.Net;
using System.Net.Http.Json;
using WildBunch.Api;
using WildBunch.Api.Games;
using WildBunch.Application.Dev.Models;
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
        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);
        Assert.NotEqual(Guid.Empty, createdSession!.Id);
        Assert.Equal(WildBunch.Domain.Game.GameStatus.Active, createdSession.Status);

        var connectedTownIds = createdSession.World.Trails
            .Where(trail => trail.FromTownId == createdSession.Player.CurrentTownId || trail.ToTownId == createdSession.Player.CurrentTownId)
            .Select(trail => trail.FromTownId == createdSession.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
            .Distinct()
            .ToArray();

        Assert.True(connectedTownIds.Length >= 2, $"expected at least 2 connected towns from starting town, got {connectedTownIds.Length}");

        var previewResults = new List<TravelPreviewDto>();
        foreach (var destinationTownId in connectedTownIds)
        {
            var previewResponse = await client.GetAsync($"/api/games/{createdSession.Id}/travel/preview/{destinationTownId}");
            Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

            var previewResult = await previewResponse.Content.ReadFromJsonAsync<TravelPreviewResultDto>();
            Assert.NotNull(previewResult);
            Assert.True(previewResult!.Success);
            Assert.NotNull(previewResult.Preview);
            previewResults.Add(previewResult.Preview!);
        }

        Assert.True(previewResults.Count > 1);
        Assert.All(previewResults, preview => Assert.True(preview.BaselineRideDays > 0));
    }

    [Fact]
    public async Task GetGameByIdReturnsCreatedSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

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

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);
        var startingFood = createdSession!.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Food).Quantity;
        var startingHorseFeed = createdSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.HorseFeed).Quantity;

        // Discover the first connected town dynamically — no hardcoded town names.
        var startingTownId = createdSession.Player.CurrentTownId;
        var destinationTownId = createdSession.World.Trails
            .Where(trail => trail.FromTownId == startingTownId || trail.ToTownId == startingTownId)
            .Select(trail => trail.FromTownId == startingTownId ? trail.ToTownId : trail.FromTownId)
            .First();
        var destinationTownName = createdSession.World.Towns.First(town => town.Id == destinationTownId).Name;

        var previewResponse = await client.GetAsync($"/api/games/{createdSession.Id}/travel/preview/{destinationTownId}");

        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var preview = await previewResponse.Content.ReadFromJsonAsync<TravelPreviewResultDto>();

        Assert.NotNull(preview);
        Assert.True(preview!.Success);
        Assert.NotNull(preview.Preview);
        Assert.Equal(TravelMode.Mounted, preview.Preview!.TravelMode);
        Assert.True(preview.Preview.MountedTravelAvailable);
        Assert.True(preview.Preview.BaselineRideDays > 0, $"expected positive baseline ride days, got {preview.Preview.BaselineRideDays}");
        Assert.NotNull(preview.Preview.RouteProfile);

        // All downstream assertions derive from the preview's actual values — no hardcoded day counts or distances.
        var expectedDays = preview.Preview.BaselineRideDays;
        var rideDayDistance = preview.Preview.RideDayDistance;
        var requiredFood = preview.Preview.RequiredFood;
        var requiredCanteenCharges = preview.Preview.RequiredCanteenCharges;
        var canteenChargesPerDay = preview.Preview.CanteenChargesPerDay;
        Assert.Equal(expectedDays, preview.Preview.ExpectedDays);
        Assert.Equal(rideDayDistance, preview.Preview.RouteProfile.RideDayDistance);
        Assert.Equal(0, preview.Preview.RequiredHorseFeed);
        Assert.Equal(0, preview.Preview.DelayMarginDays);

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/travel",
            new TravelRequest(destinationTownId));

        Assert.Equal(HttpStatusCode.OK, travelResponse.StatusCode);

        var turnResult = await travelResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(turnResult);
        Assert.True(turnResult!.Success);
        Assert.Equal(JourneyStatus.Active, turnResult.JourneyStatus);
        Assert.NotNull(turnResult.Journey);
        Assert.Equal(TravelMode.Mounted, turnResult.Journey!.TravelMode);
        Assert.Equal(rideDayDistance, turnResult.Journey.RideDayDistance);
        Assert.Equal(expectedDays, turnResult.Journey.ExpectedDays);
        Assert.Equal(0, turnResult.Journey.DelayDays);
        Assert.Equal(startingTownId, turnResult.CurrentSession.Player.CurrentTownId);
        Assert.Equal(1, turnResult.CurrentSession.Clock.Day);
        Assert.Equal(0, turnResult.CurrentSession.Clock.Turn);
        Assert.NotNull(turnResult.CurrentSession.Journey);
        Assert.Equal(expectedDays, turnResult.CurrentSession.Journey!.RemainingDays);
        Assert.Equal(rideDayDistance, turnResult.CurrentSession.Journey.RemainingRideDayDistance);
        Assert.Equal(0, turnResult.CurrentSession.Journey.DaysTravelled);
        Assert.Equal(startingFood, turnResult.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Food).Quantity);
        Assert.Equal(startingHorseFeed, turnResult.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.HorseFeed).Quantity);

        var startingCanteenCharges = createdSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Canteen).CanteenState!.Charges;

        // Advance each ride day until the journey completes. The loop covers the active days
        // (days 1 through expectedDays-1); the final advance after the loop arrives at the destination.
        for (var day = 1; day < expectedDays; day++)
        {
            // Force Quiet days so the journey is not interrupted by seed-dependent encounters.
            await client.PostAsJsonAsync(
                $"/api/dev/sessions/{createdSession.Id}/travel/force-override",
                new ForceTravelOverrideRequestDto("Quiet", null, null, null, null));

            var advanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

            Assert.Equal(HttpStatusCode.OK, advanceResponse.StatusCode);

            var advance = await advanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

            Assert.NotNull(advance);
            Assert.True(advance!.Success);
            Assert.Equal(JourneyStatus.Active, advance.JourneyStatus);
            Assert.Equal(startingTownId, advance.CurrentSession.Player.CurrentTownId);
            Assert.Equal(day + 1, advance.CurrentSession.Clock.Day);
            Assert.Equal(0, advance.CurrentSession.Clock.Turn);
            Assert.NotNull(advance.CurrentSession.Journey);
            Assert.Equal(expectedDays - day, advance.CurrentSession.Journey!.RemainingDays);
            Assert.Equal(rideDayDistance - day, advance.CurrentSession.Journey.RemainingRideDayDistance);
            Assert.Equal(day, advance.CurrentSession.Journey.DaysTravelled);
            Assert.Equal(startingFood - day, advance.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Food).Quantity);
            Assert.Equal(startingHorseFeed, advance.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.HorseFeed).Quantity);
            Assert.Equal(startingCanteenCharges - (canteenChargesPerDay * day), advance.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Canteen).CanteenState!.Charges);

            // The first advance opens the travel diary with the journey's opening narration.
            if (day == 1)
            {
                Assert.NotNull(advance.TravelDiary);
                var openingDay = Assert.Single(advance.TravelDiary!.Days);
                Assert.NotNull(openingDay.OpeningNarration);
                Assert.Contains($"I set out for {destinationTownName}", openingDay.OpeningNarration, StringComparison.OrdinalIgnoreCase);
                Assert.Contains($"{preview.Preview.BaselineRideDays}-day", openingDay.OpeningNarration, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("by mounted travel", openingDay.OpeningNarration, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("without a horse", openingDay.OpeningNarration, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Final advance completes the journey, arriving at the discovered destination town.
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession.Id}/travel/force-override",
            new ForceTravelOverrideRequestDto("Quiet", null, null, null, null));

        var finalAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, finalAdvanceResponse.StatusCode);

        var finalAdvance = await finalAdvanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(finalAdvance);
        Assert.True(finalAdvance!.Success);
        Assert.Equal(JourneyStatus.Completed, finalAdvance.JourneyStatus);
        Assert.Equal(destinationTownId, finalAdvance.CurrentSession.Player.CurrentTownId);
        Assert.Equal(expectedDays + 1, finalAdvance.CurrentSession.Clock.Day);
        Assert.Equal(0, finalAdvance.CurrentSession.Clock.Turn);
        Assert.NotNull(finalAdvance.CurrentSession.Journey);
        Assert.Equal(0, finalAdvance.CurrentSession.Journey!.RemainingDays);
        Assert.Equal(0m, finalAdvance.CurrentSession.Journey.RemainingRideDayDistance);
        Assert.Equal(expectedDays, finalAdvance.CurrentSession.Journey.DaysTravelled);
        Assert.Equal(JourneyStatus.Completed, finalAdvance.CurrentSession.Journey!.Status);
        Assert.Equal(expectedDays, finalAdvance.TravelDiary!.Days.Count);
        Assert.Equal(JourneyStatus.Completed, finalAdvance.TravelDiary.Days[^1].Status);

        // Food consumption is one per ride day, capped at zero when supplies run out.
        var finalFoodItem = finalAdvance.CurrentSession.Inventory.Items.FirstOrDefault(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Food);
        var finalFoodQuantity = finalFoodItem?.Quantity ?? 0;
        Assert.Equal(Math.Max(0, startingFood - requiredFood), finalFoodQuantity);
        Assert.Equal(startingHorseFeed, finalAdvance.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.HorseFeed).Quantity);
        Assert.Equal(startingCanteenCharges - requiredCanteenCharges, finalAdvance.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Canteen).CanteenState!.Charges);

        var payload = await finalAdvanceResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Jonah Pike", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"money\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"supplies\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostGamesWithSubmittedSeedCodeExposesTheRedMesaToDryForkRoute()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.NoHorseFootTravelReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);

        // Discover the first connected town from the starting town — no hardcoded town names.
        var startingTownId = createdSession.Player.CurrentTownId;
        var firstDestination = createdSession.World.Trails
            .Where(trail => trail.FromTownId == startingTownId || trail.ToTownId == startingTownId)
            .Select(trail => trail.FromTownId == startingTownId ? trail.ToTownId : trail.FromTownId)
            .First();

        var firstPreviewResponse = await client.GetAsync($"/api/games/{createdSession.Id}/travel/preview/{firstDestination}");
        Assert.Equal(HttpStatusCode.OK, firstPreviewResponse.StatusCode);

        var firstPreviewResult = await firstPreviewResponse.Content.ReadFromJsonAsync<TravelPreviewResultDto>();
        Assert.NotNull(firstPreviewResult);
        scenario.Fixture.AssertTravelPreview(createdSession!, firstDestination, firstPreviewResult!);

        var travelToFirstDestinationResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/travel",
            new TravelRequest(firstDestination));

        Assert.Equal(HttpStatusCode.OK, travelToFirstDestinationResponse.StatusCode);

        var firstDestinationTurn = await travelToFirstDestinationResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(firstDestinationTurn);
        scenario.Fixture.AssertTravelTurn(createdSession!, firstDestination, firstDestinationTurn!, firstPreviewResult!);

        var arrivedFirstDestination = await AdvanceUntilTownAsync(client, createdSession.Id, firstDestination);

        Assert.Equal(firstDestination, arrivedFirstDestination.CurrentSession.Player.CurrentTownId);

        var acknowledgeFirstDestinationResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/arrival/acknowledge", content: null);

        Assert.Equal(HttpStatusCode.OK, acknowledgeFirstDestinationResponse.StatusCode);

        // Discover a connected town from the first destination (not the starting town we came from).
        var secondDestination = createdSession.World.Trails
            .Where(trail => trail.FromTownId == firstDestination || trail.ToTownId == firstDestination)
            .Select(trail => trail.FromTownId == firstDestination ? trail.ToTownId : trail.FromTownId)
            .Where(townId => townId != startingTownId)
            .First();

        var secondPreviewResponse = await client.GetAsync($"/api/games/{createdSession.Id}/travel/preview/{secondDestination}");
        Assert.Equal(HttpStatusCode.OK, secondPreviewResponse.StatusCode);

        var secondPreviewResult = await secondPreviewResponse.Content.ReadFromJsonAsync<TravelPreviewResultDto>();
        Assert.NotNull(secondPreviewResult);
        scenario.Fixture.AssertDryFootRoute(createdSession!, secondDestination, secondPreviewResult!);

        var onFootTravelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/travel",
            new TravelRequest(secondDestination));

        Assert.Equal(HttpStatusCode.OK, onFootTravelResponse.StatusCode);

        var onFootTurn = await onFootTravelResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(onFootTurn);
        scenario.Fixture.AssertDryFootRoute(createdSession!, secondDestination, onFootTurn!, secondPreviewResult!);
    }

    [Fact]
    public async Task HighRiskTravelCanPauseResolveAndResumeWithoutSkippingTheTrail()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.HighRiskFoeInterruptRoute();
        scenario.AssertReady();
        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);

        // Discover the first connected town from the starting town — no hardcoded town names.
        var startingTownId = createdSession.Player.CurrentTownId;
        var firstDestination = createdSession.World.Trails
            .Where(trail => trail.FromTownId == startingTownId || trail.ToTownId == startingTownId)
            .Select(trail => trail.FromTownId == startingTownId ? trail.ToTownId : trail.FromTownId)
            .First();

        var travelToFirstDestinationResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest(firstDestination));

        Assert.Equal(HttpStatusCode.OK, travelToFirstDestinationResponse.StatusCode);

        var firstDestinationTravel = await travelToFirstDestinationResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(firstDestinationTravel);
        Assert.True(firstDestinationTravel!.Success);
        Assert.Equal(JourneyStatus.Active, firstDestinationTravel.JourneyStatus);

        var completeLowRisk = await AdvanceUntilTownAsync(client, createdSession.Id, firstDestination);

        Assert.Equal(JourneyStatus.Completed, completeLowRisk.JourneyStatus);
        Assert.NotNull(completeLowRisk.CurrentSession.Journey);
        Assert.Equal(firstDestination, completeLowRisk.CurrentSession.Player.CurrentTownId);
        var firstDestinationArrivalDay = completeLowRisk.CurrentSession.Clock.Day;

        var acknowledgeResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/arrival/acknowledge", content: null);

        Assert.Equal(HttpStatusCode.OK, acknowledgeResponse.StatusCode);

        var acknowledged = await acknowledgeResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(acknowledged);
        Assert.True(acknowledged!.Success);
        Assert.Null(acknowledged.CurrentSession.Journey);
        Assert.Equal(firstDestination, acknowledged.CurrentSession.Player.CurrentTownId);

        var foodPurchaseResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/towns/{firstDestination}/store/buy",
            new BuyStoreItemRequest(
                WildBunch.Domain.Economy.StoreVendorType.GeneralStore,
                WildBunch.Domain.Inventory.ItemKind.Food,
                6));

        Assert.Equal(HttpStatusCode.OK, foodPurchaseResponse.StatusCode);

        var foodPurchase = await foodPurchaseResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(foodPurchase);
        Assert.True(foodPurchase!.Success);
        Assert.Equal(firstDestination, foodPurchase.CurrentSession.Player.CurrentTownId);

        // Discover a connected town from the first destination (not the starting town we came from).
        // The fixture's day-offset assertions require a 2-day journey, so query previews
        // to find a candidate with BaselineRideDays == 2.
        var secondDestinationCandidates = createdSession.World.Trails
            .Where(trail => trail.FromTownId == firstDestination || trail.ToTownId == firstDestination)
            .Select(trail => trail.FromTownId == firstDestination ? trail.ToTownId : trail.FromTownId)
            .Where(townId => townId != startingTownId)
            .Distinct()
            .ToArray();

        Assert.NotEmpty(secondDestinationCandidates);

        string? secondDestination = null;
        foreach (var candidateId in secondDestinationCandidates)
        {
            var candidatePreviewResponse = await client.GetAsync($"/api/games/{createdSession.Id}/travel/preview/{candidateId}");
            if (candidatePreviewResponse.StatusCode != HttpStatusCode.OK)
            {
                continue;
            }

            var candidatePreview = await candidatePreviewResponse.Content.ReadFromJsonAsync<TravelPreviewResultDto>();
            if (candidatePreview?.Success == true && candidatePreview.Preview?.BaselineRideDays == 2)
            {
                secondDestination = candidateId;
                break;
            }
        }

        // Fall back to the first candidate if no 2-day route is found.
        secondDestination ??= secondDestinationCandidates[0];

        var travelToSecondDestinationResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/travel",
            new TravelRequest(secondDestination));

        Assert.Equal(HttpStatusCode.OK, travelToSecondDestinationResponse.StatusCode);

        var secondDestinationTravel = await travelToSecondDestinationResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(secondDestinationTravel);
        Assert.True(secondDestinationTravel!.Success);
        Assert.Equal(JourneyStatus.Active, secondDestinationTravel.JourneyStatus);
        Assert.NotNull(secondDestinationTravel.Journey);
        Assert.Null(secondDestinationTravel.Journey!.PendingEncounter);
        Assert.Equal(0, secondDestinationTravel.CurrentSession.Journey!.DaysTravelled);

        // Force an NPC encounter so the test gets the expected encounter kind
        // regardless of the deterministic seed hash (BUNCH-104 enum rename shifted rolls).
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession.Id}/travel/force-override",
            new ForceTravelOverrideRequestDto("Npc", null, null, null, null));

        var firstAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, firstAdvanceResponse.StatusCode);

        var blockedAdvance = await firstAdvanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(blockedAdvance);
        Assert.False(blockedAdvance!.Success);
        Assert.Equal(JourneyStatus.Interrupted, blockedAdvance.JourneyStatus);
        Assert.NotNull(blockedAdvance.Journey);
        Assert.NotNull(blockedAdvance.Journey!.PendingEncounter);
        Assert.Equal("npc", blockedAdvance.Journey.PendingEncounter!.Kind);
        Assert.Equal(3, blockedAdvance.Journey.PendingEncounter.Choices.Count);
        Assert.Equal(new[] { "run", "fight", "bribe" }, blockedAdvance.Journey.PendingEncounter.Choices.Select(choice => choice.Id));
        Assert.Equal(10, blockedAdvance.CurrentSession.Inventory.Items.First(item => item.Kind == WildBunch.Domain.Inventory.ItemKind.Canteen).CanteenState!.Charges);
        Assert.Equal(firstDestinationArrivalDay + 1, blockedAdvance.CurrentSession.Clock.Day);
        Assert.Equal(0, blockedAdvance.CurrentSession.Clock.Turn);
        var secondDestinationPayload = await firstAdvanceResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("foeProfile", secondDestinationPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("minimumBribe", secondDestinationPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fightStrength", secondDestinationPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("resolutionAttempts", secondDestinationPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bribeOffersMade", secondDestinationPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cumulativeBribePaid", secondDestinationPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bribeLockedOut", secondDestinationPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chaseFatigue", secondDestinationPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("annoyance", secondDestinationPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shaken", secondDestinationPayload, StringComparison.OrdinalIgnoreCase);

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
        Assert.Equal(firstDestinationArrivalDay + 1, resolved.CurrentSession.Clock.Day);
        Assert.Equal(0, resolved.CurrentSession.Clock.Turn);

        var resumeAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, resumeAdvanceResponse.StatusCode);

        var resumeAdvance = await resumeAdvanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(resumeAdvance);
        Assert.NotNull(resumeAdvance!.CurrentSession.Journey);
        Assert.Equal(secondDestination, resumeAdvance.CurrentSession.Player.CurrentTownId);
        Assert.Equal(firstDestinationArrivalDay + 2, resumeAdvance.CurrentSession.Clock.Day);
        Assert.Equal(0, resumeAdvance.CurrentSession.Clock.Turn);

        scenario.Fixture.AssertHighRiskFoeInterruptRoute(createdSession!, secondDestinationTravel!, blockedAdvance!, resolved!, resumeAdvance!);
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
            new TravelRequest("any-town"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<GameTurnResultDto> AdvanceUntilTownAsync(HttpClient client, Guid gameId, string destinationTownId)
    {
        for (var step = 0; step < 20; step++)
        {
            // Force Quiet days so the journey is not interrupted by seed-dependent encounters.
            await client.PostAsJsonAsync(
                $"/api/dev/sessions/{gameId}/travel/force-override",
                new ForceTravelOverrideRequestDto("Quiet", null, null, null, null));

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
