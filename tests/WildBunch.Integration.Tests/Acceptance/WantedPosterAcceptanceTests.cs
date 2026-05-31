using System.Net;
using System.Net.Http.Json;
using WildBunch.Domain.Cases;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Acceptance;

public sealed class WantedPosterAcceptanceTests
{
    [Fact]
    public async Task PostReadWantedPostersUpdatesThePublicJournalAndKeepsHiddenTruthPrivate()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        var createdSession = await factory.SeedCanonicalSessionAsync();

        var response = await client.PostAsync($"/api/games/{createdSession.Id}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WantedPostersResultDto>();

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal(1, result.CurrentJournal.Clock.Turn);
        Assert.Single(result.CurrentJournal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Equal(4, result.CurrentJournal.CaseFile.KnownClues.Count);
        Assert.Contains(result.CurrentJournal.CaseFile.KnownClues, clue => clue.Kind == ClueKind.Alias);
        Assert.Equal(1, result.CurrentJournal.CaseFile.KillerReleaseState.Progress);
        Assert.False(result.CurrentJournal.CaseFile.KillerReleaseState.IsReleased);
        Assert.Contains(result.CurrentJournal.LogEntries, entry => entry.Kind == GameLogEntryKind.CaseUpdate);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"discoveredSuspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);

        var persistedSession = await factory.LoadSessionAsync(createdSession.Id);
        Assert.Equal(1, persistedSession.CaseFile.KillerReleaseState.Progress);
        Assert.Single(persistedSession.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Contains(persistedSession.LogEntries, entry => entry.Kind == GameLogEntryKind.CaseUpdate);
    }
}
