using System.Net.Http.Json;
using WildBunch.Application.Dev.Models;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiHiddenTruthTests
{
    [Fact]
    public async Task DevTravelContextDoesNotLeakHiddenCulpritMarkers()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        Assert.NotNull(createdSession);

        // Start travel so the journey is active — discover destination dynamically
        var destinationTownId = scenario.DiscoverFirstConnectedTownId(createdSession);
        var travelResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession!.Id}/travel",
            new TravelRequest(destinationTownId));
        travelResponse.EnsureSuccessStatusCode();

        // The dev travel-context endpoint exposes journey internals + dev override state,
        // but must NOT leak hidden culprit truth. See BUNCH-89 and ADR-0007.
        var devContextResponse = await client.GetAsync($"/api/dev/sessions/{createdSession.Id}/travel-context");
        devContextResponse.EnsureSuccessStatusCode();
        var devContextPayload = await devContextResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Butch Cassidy", devContextPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sundance Kid", devContextPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Elzy Lay", devContextPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Kid Curry", devContextPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", devContextPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", devContextPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", devContextPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", devContextPayload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DevSaloonContextExposesTheSessionCulprit_WhilePlayerJournalReturnsOnlyUsefulPublicState()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        Assert.NotNull(createdSession);

        var devSaloonResponse = await client.GetAsync($"/api/dev/sessions/{createdSession!.Id}/saloon-context");
        devSaloonResponse.EnsureSuccessStatusCode();
        var devSaloonPayload = await devSaloonResponse.Content.ReadAsStringAsync();
        var devSaloonContext = await devSaloonResponse.Content.ReadFromJsonAsync<SaloonDevContextDto>();

        Assert.NotNull(devSaloonContext?.HiddenTruth);
        Assert.False(string.IsNullOrWhiteSpace(devSaloonContext.HiddenTruth.TrueCulpritId));
        Assert.False(string.IsNullOrWhiteSpace(devSaloonContext.HiddenTruth.TrueCulpritName));
        var trueCulprit = Assert.Single(devSaloonContext.Suspects, suspect => suspect.IsTrueCulprit);
        Assert.Equal(devSaloonContext.HiddenTruth.TrueCulpritId, trueCulprit.SuspectId);
        Assert.Equal(devSaloonContext.HiddenTruth.TrueCulpritName, trueCulprit.Name);
        Assert.Contains("\"hiddenTruth\"", devSaloonPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"isTrueCulprit\"", devSaloonPayload, StringComparison.OrdinalIgnoreCase);

        var journalResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal");
        journalResponse.EnsureSuccessStatusCode();
        var journalPayload = await journalResponse.Content.ReadAsStringAsync();
        var journal = await journalResponse.Content.ReadFromJsonAsync<JournalDto>();

        Assert.NotNull(journal);
        Assert.Equal(createdSession.Id, journal.Id);
        Assert.NotNull(journal.CurrentTown);
        Assert.DoesNotContain(devSaloonContext.HiddenTruth.TrueCulpritId, journalPayload, StringComparison.Ordinal);
        Assert.DoesNotContain("\"trueCulpritId\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(createdSession.Player.CurrentTownId, journal.CurrentTown.Id);
        Assert.Equal(createdSession.World.Towns.Single(town => town.Id == createdSession.Player.CurrentTownId).Name, journal.CurrentTown.Name);
        Assert.Equal(createdSession.CaseFile.OpeningLead, journal.CaseFile.OpeningLead);
        Assert.False(string.IsNullOrWhiteSpace(journal.CaseFile.CaseSummary));
    }
}
