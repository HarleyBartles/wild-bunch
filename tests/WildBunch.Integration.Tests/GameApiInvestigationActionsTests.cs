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
    public async Task PostLocalGossipUpdatesTheJournalAndKeepsHiddenTruthPrivate()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);

        var gossipResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/local-gossip/gather", content: null);

        Assert.Equal(HttpStatusCode.OK, gossipResponse.StatusCode);

        var gossipResult = await gossipResponse.Content.ReadFromJsonAsync<InvestigationActionResultDto>();

        Assert.NotNull(gossipResult);
        Assert.True(gossipResult!.Success);
        Assert.Equal(1, gossipResult.CurrentJournal.Clock.Turn);
        Assert.Equal(2, gossipResult.CurrentJournal.CaseFile.KnownClues.Count);
        // Boring mode surfaces one of the authored LocalGossip clues. The opening lead is a
        // CulpritTrail, so this Whereabouts clue came from the retained saloon interaction.
        Assert.Contains(gossipResult.CurrentJournal.CaseFile.KnownClues, clue => clue.Kind == ClueKind.Whereabouts);

        var journalBeforeTelegraphRequest = await client.GetStringAsync($"/api/games/{createdSession.Id}/journal");
        var telegraphResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/telegraph-leads/follow", content: null);

        Assert.Equal(HttpStatusCode.NotFound, telegraphResponse.StatusCode);
        Assert.Equal(
            journalBeforeTelegraphRequest,
            await client.GetStringAsync($"/api/games/{createdSession.Id}/journal"));

        var payload = await gossipResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("notice-board/inspect")]
    [InlineData("local-records/check")]
    public async Task PostRetiredInvestigationActionsReturnsNotFoundWithoutChangingJournal(string actionPath)
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);
        await scenario.Fixture.AssertStartingTownReady(client, createdSession!.Id, createdSession!);

        var journalBefore = await client.GetStringAsync($"/api/games/{createdSession.Id}/journal");
        var response = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/{actionPath}", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(journalBefore, await client.GetStringAsync($"/api/games/{createdSession.Id}/journal"));
    }
}
