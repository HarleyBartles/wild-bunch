using System.Net;
using System.Net.Http.Json;
using WildBunch.Application.Dev.Models;
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

        // BUNCH-107: Force a fixed salt so the wanted-poster resolver uses
        // boring-mode selection (deterministic). With Classic entropy, the salt
        // is Runtime (random), making warrant selection non-deterministic.
        // In boring mode: (townSlotIndex + visitCount) % eligibleCount = (0 + 1) % 27 = 1
        // → warrant at index 1 = "Sundance Kid" (a gang member warrant).
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession.Id}/session/lock-rng",
            new LockRngRequestDto(Salt: "test-salt-fixed"));

        var response = await client.PostAsync($"/api/games/{createdSession.Id}/wanted-posters/read", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WantedPostersResultDto>();

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal(1, result.CurrentJournal.Clock.Turn);
        Assert.Single(result.CurrentJournal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Equal(2, result.CurrentJournal.CaseFile.KnownClues.Count);
        Assert.Contains(result.CurrentJournal.CaseFile.KnownClues, clue => clue.Kind == ClueKind.Alias);
        Assert.Single(result.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Single(result.WantedPosters);
        Assert.Equal("Sundance Kid", result.WantedPosters[0].TargetDisplayName);
        Assert.Equal("The Wild Bunch trail is quiet.", result.CurrentJournal.CaseFile.CaseState.StatusText);
        Assert.Contains(result.CurrentJournal.LogEntries, entry => entry.Kind == GameLogEntryKind.CaseUpdate);
        Assert.Single(result.CurrentJournal.CaseFile.WantedPosters);
        Assert.Equal("Sundance Kid", result.CurrentJournal.CaseFile.WantedPosters[0].TargetDisplayName);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"discoveredSuspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"wantedPosters\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"targetKind\"", payload, StringComparison.OrdinalIgnoreCase);

        var persistedSession = await factory.LoadSessionAsync(createdSession.Id);
        Assert.Equal("The Wild Bunch trail is quiet.", persistedSession.CaseFile.CaseState.StatusText);
        Assert.Single(persistedSession.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Contains(persistedSession.LogEntries, entry => entry.Kind == GameLogEntryKind.CaseUpdate);
    }
}
