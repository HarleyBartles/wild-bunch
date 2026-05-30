using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
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
        Assert.NotEmpty(journal.LogEntries);
        Assert.NotEmpty(journal.CaseFile.KnownClues);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"suspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
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

        var advanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, advanceResponse.StatusCode);

        var journalResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal");

        Assert.Equal(HttpStatusCode.OK, journalResponse.StatusCode);

        var journal = await journalResponse.Content.ReadFromJsonAsync<JournalDto>();

        Assert.NotNull(journal);
        Assert.Equal("redmesa", journal!.CurrentTown.Id);
        Assert.Equal(1, journal.Clock.Turn);
        Assert.Contains(journal.LogEntries, entry => entry.Kind == GameLogEntryKind.Travel);
        Assert.Equal("A pale scar cuts across the left cheek.", journal.CaseFile.OpeningLead);

        var payload = await journalResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"suspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }
}
