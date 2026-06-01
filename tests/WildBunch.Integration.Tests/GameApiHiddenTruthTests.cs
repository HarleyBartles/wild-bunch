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

        var createResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var createPayload = await createResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Butch Cassidy", createPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sundance Kid", createPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Elzy Lay", createPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Kid Curry", createPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", createPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", createPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", createPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", createPayload, StringComparison.OrdinalIgnoreCase);

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
        Assert.Contains("\"discoveredSuspects\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);

        var noticeBoardResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/notice-board/inspect", content: null);
        var noticeBoardPayload = await noticeBoardResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"trueCulpritId\"", noticeBoardPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", noticeBoardPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", noticeBoardPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", noticeBoardPayload, StringComparison.OrdinalIgnoreCase);

        var sheriffRecordsResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/sheriff-records/check", content: null);
        var sheriffRecordsPayload = await sheriffRecordsResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"trueCulpritId\"", sheriffRecordsPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", sheriffRecordsPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", sheriffRecordsPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", sheriffRecordsPayload, StringComparison.OrdinalIgnoreCase);
    }
}
