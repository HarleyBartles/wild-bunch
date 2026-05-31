using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiInvestigationActionsTests
{
    [Fact]
    public async Task PostInvestigationActionsUpdateTheJournalAndKeepHiddenTruthPrivate()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = ScenarioSeedCatalog.CanonicalPinecrossServices;
        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
        await scenario.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var noticeBoardResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/notice-board/inspect", content: null);

        Assert.Equal(HttpStatusCode.OK, noticeBoardResponse.StatusCode);

        var noticeBoardResult = await noticeBoardResponse.Content.ReadFromJsonAsync<InvestigationActionResultDto>();

        Assert.NotNull(noticeBoardResult);
        Assert.True(noticeBoardResult!.Success);
        Assert.Equal(1, noticeBoardResult.CurrentJournal.Clock.Turn);
        Assert.Equal(2, noticeBoardResult.CurrentJournal.LogEntries.Count);
        Assert.Single(noticeBoardResult.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Contains(noticeBoardResult.CurrentJournal.CaseFile.KnownWarrants, warrant => warrant.Summary.Contains("Wild Bunch", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(noticeBoardResult.CurrentJournal.CaseFile.DiscoveredSuspects);

        var sheriffRecordsResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/sheriff-records/check", content: null);

        Assert.Equal(HttpStatusCode.OK, sheriffRecordsResponse.StatusCode);

        var sheriffRecordsResult = await sheriffRecordsResponse.Content.ReadFromJsonAsync<InvestigationActionResultDto>();

        Assert.NotNull(sheriffRecordsResult);
        Assert.True(sheriffRecordsResult!.Success);
        Assert.Equal(2, sheriffRecordsResult.CurrentJournal.Clock.Turn);
        Assert.Equal(4, sheriffRecordsResult.CurrentJournal.CaseFile.KnownClues.Count);
        Assert.Single(sheriffRecordsResult.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Contains(sheriffRecordsResult.CurrentJournal.CaseFile.KnownClues, clue => clue.Kind == ClueKind.Record);

        var payload = await sheriffRecordsResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostInvestigationActionsReturnNotFoundForMissingGame()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/games/{Guid.NewGuid()}/investigations/notice-board/inspect", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
