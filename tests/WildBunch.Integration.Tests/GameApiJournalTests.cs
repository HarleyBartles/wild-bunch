using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiJournalTests
{
    [Fact]
    public async Task GetJournalReturnsExpectedDataForCreatedGame()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);

        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/journal");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var journal = await response.Content.ReadFromJsonAsync<JournalDto>();

        Assert.NotNull(journal);
        Assert.Equal(createdSession.Id, journal!.Id);
        Assert.Equal(createdSession.Status, journal.Status);
        Assert.Equal(createdSession.Clock.Day, journal.Clock.Day);
        Assert.Equal(createdSession.Clock.Turn, journal.Clock.Turn);
        Assert.Equal("hardpan", journal.CurrentTown.Id);
        Assert.Equal("Hardpan", journal.CurrentTown.Name);
        Assert.Equal("Find the culprit before the law closes in.", journal.CaseFile.CaseSummary);
        Assert.Equal("The culprit has a scar on the left cheek.", journal.CaseFile.OpeningLead);
        Assert.Equal("The Wild Bunch trail is quiet.", journal.CaseFile.CaseState.StatusText);
        Assert.Empty(journal.CaseFile.DiscoveredSuspects);
        Assert.NotEmpty(journal.LogEntries);
        Assert.NotEmpty(journal.CaseFile.KnownClues);
        Assert.Contains(journal.CaseFile.KnownClues, clue => clue.SourceLabel is not null);
        Assert.Contains(journal.CaseFile.KnownClues, clue => clue.Anchors.Subjects.Count > 0 || clue.Anchors.Locations.Count > 0 || clue.Anchors.Times.Count > 0 || clue.Anchors.Directions.Count > 0);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Jonah Pike", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspectCount\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMissingGameJournalReturnsNotFound()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}/journal");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetJournalAfterPurchaseIncludesPurchaseLogEntry()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        Assert.NotNull(createdSession);

        await scenario.Fixture.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var buyResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/towns/hardpan/store/buy",
            new BuyStoreItemRequest(WildBunch.Domain.Economy.StoreVendorType.GeneralStore, WildBunch.Domain.Inventory.ItemKind.Food, 2));
        var buyResult = await buyResponse.Content.ReadFromJsonAsync<GameTurnResultDto>();
        Assert.NotNull(buyResult);
        Assert.True(buyResult!.Success);

        var journalResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal");
        Assert.Equal(HttpStatusCode.OK, journalResponse.StatusCode);

        var journal = await journalResponse.Content.ReadFromJsonAsync<JournalDto>();
        Assert.NotNull(journal);
        Assert.Contains(journal!.LogEntries, entry => entry.Kind == GameLogEntryKind.Purchase);
        var purchaseEntry = journal.LogEntries.Single(entry => entry.Kind == GameLogEntryKind.Purchase);
        Assert.Equal("Purchased 2 Food for $4.00.", purchaseEntry.Message);
    }

    [Fact]
    public async Task GetJournalAfterTravelReflectsUpdatedState()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);

        // Get a connected town dynamically
        var connectedTownIds = createdSession.World.Trails
            .Where(trail => trail.FromTownId == createdSession.Player.CurrentTownId || trail.ToTownId == createdSession.Player.CurrentTownId)
            .Select(trail => trail.FromTownId == createdSession.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
            .Distinct()
            .ToArray();

        Assert.True(connectedTownIds.Length > 0, "Expected at least one connected town");
        var destinationTownId = connectedTownIds.First();

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest(destinationTownId));

        Assert.Equal(HttpStatusCode.OK, travelResponse.StatusCode);

        await AdvanceUntilTownAsync(client, createdSession.Id, destinationTownId);

        var journalResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal");

        Assert.Equal(HttpStatusCode.OK, journalResponse.StatusCode);

        var journal = await journalResponse.Content.ReadFromJsonAsync<JournalDto>();

        Assert.NotNull(journal);
        Assert.Equal(destinationTownId, journal!.CurrentTown.Id);
        Assert.Equal(6, journal.Clock.Day);
        Assert.Equal(0, journal.Clock.Turn);
        Assert.Contains(journal.LogEntries, entry => entry.Kind == GameLogEntryKind.Travel);
        Assert.Equal("The culprit has a scar on the left cheek.", journal.CaseFile.OpeningLead);
        Assert.Equal("The Wild Bunch trail is quiet.", journal.CaseFile.CaseState.StatusText);
        Assert.Empty(journal.CaseFile.DiscoveredSuspects);

        var payload = await journalResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Jonah Pike", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetJournalSupportsSkipAndTakeQueryParameters()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        scenario.Fixture.AssertCreatedSession(createdSession!);

        // Get a connected town dynamically
        var connectedTownIds = createdSession.World.Trails
            .Where(trail => trail.FromTownId == createdSession.Player.CurrentTownId || trail.ToTownId == createdSession.Player.CurrentTownId)
            .Select(trail => trail.FromTownId == createdSession.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
            .Distinct()
            .ToArray();

        Assert.True(connectedTownIds.Length > 0, "Expected at least one connected town");
        var destinationTownId = connectedTownIds.First();

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest(destinationTownId));

        Assert.Equal(HttpStatusCode.OK, travelResponse.StatusCode);

        var fullResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal");
        Assert.Equal(HttpStatusCode.OK, fullResponse.StatusCode);
        var fullJournal = await fullResponse.Content.ReadFromJsonAsync<JournalDto>();
        Assert.NotNull(fullJournal);
        Assert.True(fullJournal!.LogEntries.Count >= 2);

        var pagedResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal?skip=1&take=1");
        Assert.Equal(HttpStatusCode.OK, pagedResponse.StatusCode);

        var pagedJournal = await pagedResponse.Content.ReadFromJsonAsync<JournalDto>();
        Assert.NotNull(pagedJournal);
        Assert.Single(pagedJournal!.LogEntries);
        Assert.Equal(fullJournal.LogEntries[1].Message, pagedJournal.LogEntries[0].Message);
        Assert.Equal(fullJournal.LogEntries[1].Kind, pagedJournal.LogEntries[0].Kind);
    }

    private static async Task AdvanceUntilTownAsync(HttpClient client, Guid gameId, string destinationTownId)
    {
        for (var step = 0; step < 12; step++)
        {
            // Force a Quiet day so the journey is not interrupted by seed-dependent encounters.
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
                Assert.True(resolved!.Success);
                if (resolved.JourneyStatus == JourneyStatus.Completed && resolved.CurrentSession.Player.CurrentTownId == destinationTownId)
                {
                    return;
                }

                Assert.Equal(JourneyStatus.Active, resolved.JourneyStatus);
                continue;
            }

            Assert.True(advanceResult.Success);
            if (advanceResult.JourneyStatus == JourneyStatus.Completed && advanceResult.CurrentSession.Player.CurrentTownId == destinationTownId)
            {
                return;
            }
        }

        throw new InvalidOperationException($"Travel to {destinationTownId} did not complete.");
    }
}
