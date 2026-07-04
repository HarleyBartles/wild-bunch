using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

public sealed class GameApiHiddenTruthTests
{
    [Fact]
    public async Task PublicApiResponsesDoNotLeakHiddenCulpritMarkers()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        Assert.NotNull(createdSession);

        var journalResponse = await client.GetAsync($"/api/games/{createdSession!.Id}/journal");
        var journalPayload = await journalResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Butch Cassidy", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sundance Kid", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Elzy Lay", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Kid Curry", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", journalPayload, StringComparison.OrdinalIgnoreCase);

        var wantedPostersResponse = await client.PostAsync($"/api/games/{createdSession.Id}/wanted-posters/read", content: null);
        var wantedPostersPayload = await wantedPostersResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"trueCulpritId\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"targetKind\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"discoveredSuspects\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"wantedPosters\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);

        var noticeBoardResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/notice-board/inspect", content: null);
        var noticeBoardPayload = await noticeBoardResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"trueCulpritId\"", noticeBoardPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", noticeBoardPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", noticeBoardPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", noticeBoardPayload, StringComparison.OrdinalIgnoreCase);

        var sheriffRecordsResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/local-records/check", content: null);
        var sheriffRecordsPayload = await sheriffRecordsResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"trueCulpritId\"", sheriffRecordsPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", sheriffRecordsPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", sheriffRecordsPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", sheriffRecordsPayload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DevTravelContextDoesNotLeakHiddenCulpritMarkers()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

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
    public async Task DevSaloonContextDeliberatelyExposesHiddenTruth_AndPlayerApiDoesNot()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();
        scenario.AssertReady();

        var createdSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        Assert.NotNull(createdSession);

        // The dev saloon-context endpoint deliberately exposes hidden culprit truth
        // (TrueCulpritId, IsTrueCulprit, suspect eligibility) per ADR-0030 §7 and ADR-0032.
        // This is the first dev endpoint to exercise the player-vs-dev truth boundary.
        var devSaloonResponse = await client.GetAsync($"/api/dev/sessions/{createdSession!.Id}/saloon-context");
        devSaloonResponse.EnsureSuccessStatusCode();
        var devSaloonPayload = await devSaloonResponse.Content.ReadAsStringAsync();

        // Dev DTO MUST contain hidden truth markers
        Assert.Contains("\"trueCulpritId\"", devSaloonPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"isTrueCulprit\"", devSaloonPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"hiddenTruth\"", devSaloonPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"isEligibleSaloonPoi\"", devSaloonPayload, StringComparison.OrdinalIgnoreCase);

        // Player APIs MUST NOT contain hidden truth markers (re-verify the boundary)
        var journalResponse = await client.GetAsync($"/api/games/{createdSession.Id}/journal");
        var journalPayload = await journalResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"trueCulpritId\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", journalPayload, StringComparison.OrdinalIgnoreCase);
    }
}
