using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiWantedPostersTests
{
    [Fact]
    public async Task PostReadWantedPostersSucceedsForCreatedGameAndUpdatesJournal()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();
        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var actionResponse = await client.PostAsync($"/api/games/{createdSession!.Id}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.OK, actionResponse.StatusCode);

        var actionResult = await actionResponse.Content.ReadFromJsonAsync<WantedPostersResultDto>();

        Assert.NotNull(actionResult);
        Assert.True(actionResult!.Success);
        Assert.Equal(1, actionResult.CurrentJournal.Clock.Turn);
        Assert.Equal(2, actionResult.CurrentJournal.LogEntries.Count);
        Assert.Single(actionResult.CurrentJournal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Equal(2, actionResult.CurrentJournal.CaseFile.KnownClues.Count);
        Assert.Contains(actionResult.CurrentJournal.CaseFile.KnownClues, clue => clue.Kind == ClueKind.Alias);
        Assert.Single(actionResult.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Single(actionResult.WantedPosters);
        Assert.Equal("Butch Cassidy", actionResult.WantedPosters[0].TargetDisplayName);
        Assert.Equal("Raven-feather pin", actionResult.WantedPosters[0].QuickView.HeadlineFeatureOrDescriptor);
        Assert.Equal(2, actionResult.WantedPosters[0].Details.Features.Count);
        Assert.Equal(WantedPosterFeatureSalience.Headline, actionResult.WantedPosters[0].Details.Features[0].Salience);
        Assert.Equal(WantedPosterFeatureSalience.Supporting, actionResult.WantedPosters[0].Details.Features[1].Salience);
        Assert.Equal("gang-affiliated wanted criminal", actionResult.WantedPosters[0].PublicSafeClassification);
        Assert.Equal("The Wild Bunch trail is quiet.", actionResult.CurrentJournal.CaseFile.CaseState.StatusText);

        var actionPayload = await actionResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"discoveredSuspects\"", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("suspect-1", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"wantedPosters\"", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueculpritid\"", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", actionPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"targetKind\"", actionPayload, StringComparison.OrdinalIgnoreCase);

        var journalResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal");

        Assert.Equal(HttpStatusCode.OK, journalResponse.StatusCode);

        var journal = await journalResponse.Content.ReadFromJsonAsync<JournalDto>();

        Assert.NotNull(journal);
        Assert.Equal(2, journal!.CaseFile.KnownClues.Count);
        Assert.Contains(journal.CaseFile.KnownClues, clue => clue.Kind == ClueKind.Alias);
        Assert.Equal("The Wild Bunch trail is quiet.", journal.CaseFile.CaseState.StatusText);
        Assert.Single(journal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Single(journal.CaseFile.WantedPosters);
        Assert.Equal("Butch Cassidy", journal.CaseFile.WantedPosters[0].TargetDisplayName);
        Assert.Contains(journal.LogEntries, entry => entry.Kind == GameLogEntryKind.CaseUpdate);

        var journalPayload = await journalResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"discoveredSuspects\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("suspect-1", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"wantedPosters\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", journalPayload, StringComparison.OrdinalIgnoreCase);

        var secondReadResponse = await client.PostAsync($"/api/games/{createdSession.Id}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.OK, secondReadResponse.StatusCode);

        var secondRead = await secondReadResponse.Content.ReadFromJsonAsync<WantedPostersResultDto>();

        Assert.NotNull(secondRead);
        Assert.Equal("The Wild Bunch trail is quiet.", secondRead!.CurrentJournal.CaseFile.CaseState.StatusText);
        Assert.Equal(2, secondRead.CurrentJournal.CaseFile.KnownClues.Count);
        Assert.Single(secondRead.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Single(secondRead.CurrentJournal.CaseFile.DiscoveredSuspects);
        Assert.Contains(secondRead.CurrentJournal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Single(secondRead.WantedPosters);
        Assert.Equal("Butch Cassidy", secondRead.WantedPosters[0].TargetDisplayName);

        var thirdReadResponse = await client.PostAsync($"/api/games/{createdSession.Id}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.OK, thirdReadResponse.StatusCode);

        var thirdRead = await thirdReadResponse.Content.ReadFromJsonAsync<WantedPostersResultDto>();

        Assert.NotNull(thirdRead);
        Assert.Equal("The Wild Bunch trail is quiet.", thirdRead!.CurrentJournal.CaseFile.CaseState.StatusText);
        Assert.Equal(2, thirdRead.CurrentJournal.CaseFile.KnownClues.Count);
        Assert.Single(thirdRead.CurrentJournal.CaseFile.KnownWarrants);
    }

    [Fact]
    public async Task PostReadWantedPostersReturnsNotFoundForMissingGame()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/games/{Guid.NewGuid()}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostReadWantedPostersReturnsFailureWhenTownDoesNotSupportPosters()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();
        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest("holloway"));

        Assert.Equal(HttpStatusCode.OK, travelResponse.StatusCode);

        // Force Quiet days so the journey completes without seed-dependent encounter interruption.
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession.Id}/travel/force-override",
            new ForceTravelOverrideRequestDto("Quiet", null, null, null, null));

        var firstAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, firstAdvanceResponse.StatusCode);

        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession.Id}/travel/force-override",
            new ForceTravelOverrideRequestDto("Quiet", null, null, null, null));

        var secondAdvanceResponse = await client.PostAsync($"/api/games/{createdSession.Id}/travel/advance", content: null);

        Assert.Equal(HttpStatusCode.OK, secondAdvanceResponse.StatusCode);

        var response = await client.PostAsync($"/api/games/{createdSession.Id}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WantedPostersResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("holloway", result.CurrentJournal.CurrentTown.Id);
        Assert.Equal(3, result.CurrentJournal.Clock.Day);
        Assert.Equal(0, result.CurrentJournal.Clock.Turn);
        Assert.True(result.CurrentJournal.LogEntries.Count >= 4);
        Assert.Single(result.CurrentJournal.CaseFile.KnownClues);
        Assert.DoesNotContain(result.CurrentJournal.CaseFile.KnownClues, clue => clue.Kind == ClueKind.Alias);
        Assert.Empty(result.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Empty(result.WantedPosters);
        Assert.Empty(result.CurrentJournal.CaseFile.DiscoveredSuspects);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"discoveredSuspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"wantedPosters\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"targetKind\"", payload, StringComparison.OrdinalIgnoreCase);
    }
}
