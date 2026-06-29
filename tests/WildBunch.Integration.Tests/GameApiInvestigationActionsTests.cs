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

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertPinecrossServices(client, createdSession!.Id, createdSession!);

        var noticeBoardResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/notice-board/inspect", content: null);

        Assert.Equal(HttpStatusCode.OK, noticeBoardResponse.StatusCode);

        var noticeBoardResult = await noticeBoardResponse.Content.ReadFromJsonAsync<InvestigationActionResultDto>();

        Assert.NotNull(noticeBoardResult);
        Assert.True(noticeBoardResult!.Success);
        Assert.Equal(1, noticeBoardResult.CurrentJournal.Clock.Turn);
        Assert.Equal(2, noticeBoardResult.CurrentJournal.LogEntries.Count);
        Assert.Empty(noticeBoardResult.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Empty(noticeBoardResult.CurrentJournal.CaseFile.DiscoveredSuspects);

        var localRecordsResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/local-records/check", content: null);

        Assert.Equal(HttpStatusCode.OK, localRecordsResponse.StatusCode);

        var localRecordsResult = await localRecordsResponse.Content.ReadFromJsonAsync<InvestigationActionResultDto>();

        Assert.NotNull(localRecordsResult);
        Assert.True(localRecordsResult!.Success);
        Assert.Equal(2, localRecordsResult.CurrentJournal.Clock.Turn);
        Assert.Equal(2, localRecordsResult.CurrentJournal.CaseFile.KnownClues.Count);
        Assert.Empty(localRecordsResult.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Contains(localRecordsResult.CurrentJournal.CaseFile.KnownClues, clue => clue.Kind == ClueKind.Record);

        var gossipResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/local-gossip/gather", content: null);

        Assert.Equal(HttpStatusCode.OK, gossipResponse.StatusCode);

        var gossipResult = await gossipResponse.Content.ReadFromJsonAsync<InvestigationActionResultDto>();

        Assert.NotNull(gossipResult);
        Assert.True(gossipResult!.Success);
        Assert.Equal(3, gossipResult.CurrentJournal.Clock.Turn);
        Assert.Equal(3, gossipResult.CurrentJournal.CaseFile.KnownClues.Count);
        // BUNCH-107: With DeterministicSaltSourceFactory producing a Fixed salt,
        // the GameSession passes null to the ClueSurfacingResolver (boring-mode path).
        // Boring-mode index = (townSlotIndex + visitCount) % eligibleCount = (0 + 1) % 2 = 1,
        // which surfaces the second LocalGossip-tagged clue ("Boot prints and a waystation
        // note..."). Both LocalGossip clues have Kind = Whereabouts, and no other known
        // clue at this point has that kind (opening lead = CulpritTrail, records = Record).
        Assert.Contains(gossipResult.CurrentJournal.CaseFile.KnownClues, clue => clue.Kind == ClueKind.Whereabouts);

        var telegraphResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/telegraph-leads/follow", content: null);

        Assert.Equal(HttpStatusCode.OK, telegraphResponse.StatusCode);

        var telegraphResult = await telegraphResponse.Content.ReadFromJsonAsync<InvestigationActionResultDto>();

        Assert.NotNull(telegraphResult);
        // BUNCH-107: Lost Canyon (the starting town) has Telegraph service
        // (HubTelegraph palette, slot 0). Following telegraph leads should
        // succeed and surface a new clue.
        Assert.True(telegraphResult!.Success);
        Assert.NotEqual("There is no telegraph office here.", telegraphResult.Message);

        var payload = await localRecordsResponse.Content.ReadAsStringAsync();
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
