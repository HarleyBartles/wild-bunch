using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
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
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var response = await client.GetAsync($"/api/games/{createdSession!.Id}/journal");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var journal = await response.Content.ReadFromJsonAsync<JournalDto>();

        Assert.NotNull(journal);
        Assert.Equal(createdSession.Id, journal!.Id);
        Assert.Equal(createdSession.Status, journal.Status);
        Assert.Equal(createdSession.Clock.Day, journal.Clock.Day);
        Assert.Equal(createdSession.Clock.Turn, journal.Clock.Turn);
        Assert.Equal("pinecross", journal.CurrentTown.Id);
        Assert.Equal("Pinecross", journal.CurrentTown.Name);
        Assert.Equal("Find the culprit before the law closes in.", journal.CaseFile.CaseSummary);
        Assert.Equal("A pale scar cuts across the left cheek.", journal.CaseFile.OpeningLead);
        Assert.False(journal.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(0, journal.CaseFile.KillerReleaseState.Progress);
        Assert.Empty(journal.CaseFile.DiscoveredSuspects);
        Assert.NotEmpty(journal.LogEntries);
        Assert.NotEmpty(journal.CaseFile.KnownClues);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Jonah Pike", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspectCount\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMissingGameJournalReturnsNotFound()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}/journal");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetJournalAfterTravelReflectsUpdatedState()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest("redmesa"));

        Assert.Equal(HttpStatusCode.OK, travelResponse.StatusCode);

        await AdvanceUntilTownAsync(client, createdSession.Id, "redmesa");

        var journalResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal");

        Assert.Equal(HttpStatusCode.OK, journalResponse.StatusCode);

        var journal = await journalResponse.Content.ReadFromJsonAsync<JournalDto>();

        Assert.NotNull(journal);
        Assert.Equal("redmesa", journal!.CurrentTown.Id);
        Assert.Equal(5, journal.Clock.Day);
        Assert.Equal(0, journal.Clock.Turn);
        Assert.Contains(journal.LogEntries, entry => entry.Kind == GameLogEntryKind.Travel);
        Assert.Equal("A pale scar cuts across the left cheek.", journal.CaseFile.OpeningLead);
        Assert.Empty(journal.CaseFile.DiscoveredSuspects);

        var payload = await journalResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Jonah Pike", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetJournalSupportsSkipAndTakeQueryParameters()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest("redmesa"));

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
