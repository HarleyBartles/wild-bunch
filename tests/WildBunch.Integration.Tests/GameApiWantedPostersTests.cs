using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiWantedPostersTests
{
    [Fact]
    public async Task PostReadWantedPostersSucceedsForCreatedGameAndUpdatesJournal()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var actionResponse = await client.PostAsync($"/api/games/{createdSession!.Id}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.OK, actionResponse.StatusCode);

        var actionResult = await actionResponse.Content.ReadFromJsonAsync<WantedPostersResultDto>();

        Assert.NotNull(actionResult);
        Assert.True(actionResult!.Success);
        Assert.Equal(1, actionResult.CurrentJournal.Clock.Turn);
        Assert.Equal(2, actionResult.CurrentJournal.LogEntries.Count);
        Assert.Single(actionResult.CurrentJournal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Single(actionResult.CurrentJournal.CaseFile.KnownClues, clue => clue.Id == "clue-public-1");
        Assert.Equal(1, actionResult.CurrentJournal.CaseFile.KillerReleaseState.Progress);
        Assert.False(actionResult.CurrentJournal.CaseFile.KillerReleaseState.IsReleased);

        var actionPayload = await actionResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"discoveredSuspects\"", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("suspect-1", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueculpritid\"", actionPayload, StringComparison.OrdinalIgnoreCase);

        var journalResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal");

        Assert.Equal(HttpStatusCode.OK, journalResponse.StatusCode);

        var journal = await journalResponse.Content.ReadFromJsonAsync<JournalDto>();

        Assert.NotNull(journal);
        Assert.Contains(journal!.CaseFile.KnownClues, clue => clue.Id == "clue-public-1");
        Assert.Equal(1, journal.CaseFile.KillerReleaseState.Progress);
        Assert.False(journal.CaseFile.KillerReleaseState.IsReleased);
        Assert.Single(journal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Contains(journal.LogEntries, entry => entry.Kind == GameLogEntryKind.CaseUpdate);

        var journalPayload = await journalResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"discoveredSuspects\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("suspect-1", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", journalPayload, StringComparison.OrdinalIgnoreCase);

        var secondReadResponse = await client.PostAsync($"/api/games/{createdSession.Id}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.OK, secondReadResponse.StatusCode);

        var secondRead = await secondReadResponse.Content.ReadFromJsonAsync<WantedPostersResultDto>();

        Assert.NotNull(secondRead);
        Assert.Equal(2, secondRead!.CurrentJournal.CaseFile.KillerReleaseState.Progress);
        Assert.Equal(5, secondRead.CurrentJournal.CaseFile.KnownClues.Count);
        Assert.Equal(2, secondRead.CurrentJournal.CaseFile.DiscoveredSuspects.Count);
        Assert.Contains(secondRead.CurrentJournal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Contains(secondRead.CurrentJournal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-2");
        Assert.Contains(secondRead.CurrentJournal.CaseFile.KnownClues, clue => clue.Id == "clue-public-1");
        Assert.Contains(secondRead.CurrentJournal.CaseFile.KnownClues, clue => clue.Id == "clue-public-2");

        var thirdReadResponse = await client.PostAsync($"/api/games/{createdSession.Id}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.OK, thirdReadResponse.StatusCode);

        var thirdRead = await thirdReadResponse.Content.ReadFromJsonAsync<WantedPostersResultDto>();

        Assert.NotNull(thirdRead);
        Assert.Equal(2, thirdRead!.CurrentJournal.CaseFile.KillerReleaseState.Progress);
    }

    [Fact]
    public async Task PostReadWantedPostersReturnsNotFoundForMissingGame()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/games/{Guid.NewGuid()}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostReadWantedPostersReturnsFailureWhenTownDoesNotSupportPosters()
    {
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest("holloway"));

        Assert.Equal(HttpStatusCode.OK, travelResponse.StatusCode);

        var firstAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, firstAdvanceResponse.StatusCode);

        var secondAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, secondAdvanceResponse.StatusCode);

        var response = await client.PostAsync($"/api/games/{createdSession.Id}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WantedPostersResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("holloway", result.CurrentJournal.CurrentTown.Id);
        Assert.Equal(2, result.CurrentJournal.Clock.Turn);
        Assert.Equal(4, result.CurrentJournal.LogEntries.Count);
        Assert.DoesNotContain(result.CurrentJournal.CaseFile.KnownClues, clue => clue.Id == "clue-public-1");
        Assert.Empty(result.CurrentJournal.CaseFile.DiscoveredSuspects);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"discoveredSuspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }
}
