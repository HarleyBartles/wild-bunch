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
        using var factory = new SqliteApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/games", new StartGameRequest("Ranger Vale"));
        var createdSession = await createResponse.Content.ReadFromJsonAsync<GameSessionDto>();

        Assert.NotNull(createdSession);

        var createPayload = await createResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"suspects\"", createPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", createPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", createPayload, StringComparison.OrdinalIgnoreCase);

        var journalResponse = await client.GetAsync($"/api/games/{createdSession!.Id}/journal");
        var journalPayload = await journalResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"suspects\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", journalPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", journalPayload, StringComparison.OrdinalIgnoreCase);

        var wantedPostersResponse = await client.PostAsync($"/api/games/{createdSession.Id}/wanted-posters/read", content: null);
        var wantedPostersPayload = await wantedPostersResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"suspects\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", wantedPostersPayload, StringComparison.OrdinalIgnoreCase);
    }
}
