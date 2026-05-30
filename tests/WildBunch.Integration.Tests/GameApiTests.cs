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
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session!.Id);
        Assert.Equal("Ranger Vale", session.Player.Name);
        Assert.Equal("pinecross", session.Player.CurrentTownId);
        Assert.Equal(WildBunch.Domain.Game.GameStatus.Active, session.Status);
        Assert.Equal(25m, session.Inventory.Wallet.Cash);
        Assert.Equal(8, session.Inventory.Items.Count);
        Assert.True(session.Inventory.Capabilities.MountedTravelAvailable);
        Assert.True(session.Inventory.Capabilities.GunfightCapable);
        Assert.False(session.Inventory.Capabilities.RifleUsable);
        Assert.Equal(6, session.World.Towns.Count);
        Assert.Equal(7, session.World.Trails.Count);
        Assert.Equal("A pale scar cuts across the left cheek.", session.CaseFile.OpeningLead);
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(0, session.CaseFile.KillerReleaseState.Progress);
        Assert.Empty(session.CaseFile.DiscoveredSuspects);
        Assert.NotEmpty(session.LogEntries);

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
    }

    [Fact]
    public async Task GetGameByIdReturnsCreatedSession()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

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
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
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
        Assert.Equal(2, preview.Preview.ExpectedDays);
        Assert.True(preview.Preview.MountedTravelAvailable);
        Assert.Equal(0, preview.Preview.RequiredHorseFeed);
        Assert.Equal(2, preview.Preview.RequiredFood);
        Assert.Equal(0, preview.Preview.RequiredCanteenCharges);
        Assert.Equal(0, preview.Preview.CanteenChargesPerDay);
        Assert.Equal(0, preview.Preview.DelayMarginDays);
        Assert.NotNull(preview.Preview.RouteProfile);
        Assert.Equal(2m, preview.Preview.RouteProfile.RideDayDistance);

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
        Assert.Equal(1, turnResult.CurrentSession.Clock.Turn);
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

        var firstAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, firstAdvanceResponse.StatusCode);

        var firstAdvance = await firstAdvanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(firstAdvance);
        Assert.True(firstAdvance!.Success);
        Assert.Equal(JourneyStatus.Completed, firstAdvance.JourneyStatus);
        Assert.Equal("holloway", firstAdvance.CurrentSession.Player.CurrentTownId);
        Assert.Equal(2, firstAdvance.CurrentSession.Clock.Turn);
        Assert.Null(firstAdvance.CurrentSession.Journey);
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
    public async Task HighRiskTravelCanPauseResolveAndResumeWithoutSkippingTheTrail()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var travelToRedMesaResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest("redmesa"));

        Assert.Equal(HttpStatusCode.OK, travelToRedMesaResponse.StatusCode);

        var redMesaTravel = await travelToRedMesaResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(redMesaTravel);
        Assert.True(redMesaTravel!.Success);
        Assert.Equal(JourneyStatus.Active, redMesaTravel.JourneyStatus);

        var completeLowRiskResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, completeLowRiskResponse.StatusCode);

        var completeLowRisk = await completeLowRiskResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(completeLowRisk);
        Assert.True(completeLowRisk!.Success);
        Assert.Equal(JourneyStatus.Completed, completeLowRisk.JourneyStatus);
        Assert.Equal("redmesa", completeLowRisk.CurrentSession.Player.CurrentTownId);

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

        var blockedAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, blockedAdvanceResponse.StatusCode);

        var blockedAdvance = await blockedAdvanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(blockedAdvance);
        Assert.False(blockedAdvance!.Success);
        Assert.Equal(JourneyStatus.Interrupted, blockedAdvance.JourneyStatus);
        Assert.Equal(3, blockedAdvance.CurrentSession.Clock.Turn);

        var resolveResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/travel/encounter/resolve",
            new { ChoiceId = "run" });

        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);

        var resolved = await resolveResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(resolved);
        Assert.True(resolved!.Success);
        Assert.Equal(JourneyStatus.Active, resolved.JourneyStatus);
        Assert.NotNull(resolved.CurrentSession.Journey);
        Assert.Null(resolved.CurrentSession.Journey!.PendingEncounter);
        Assert.Equal(0, resolved.CurrentSession.Journey.DelayDays);
        Assert.Equal(TravelMode.Mounted, resolved.CurrentSession.Journey.TravelMode);

        var resumeAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, resumeAdvanceResponse.StatusCode);

        var resumeAdvance = await resumeAdvanceResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();

        Assert.NotNull(resumeAdvance);
        Assert.True(resumeAdvance!.Success);
        Assert.Equal(JourneyStatus.Active, resumeAdvance.JourneyStatus);
        Assert.NotNull(resumeAdvance.CurrentSession.Journey);
        Assert.Equal(0, resumeAdvance.CurrentSession.Clock.Turn);
        Assert.Equal(1, resumeAdvance.CurrentSession.Journey!.RemainingDays);
    }

    [Fact]
    public async Task GetMissingGameReturnsNotFound()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TravelMissingGameReturnsNotFound()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/games/{Guid.NewGuid()}/travel",
            new TravelRequest("dryfork"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
