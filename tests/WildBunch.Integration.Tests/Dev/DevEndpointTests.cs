using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Api.Games;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence;

namespace WildBunch.Integration.Tests.Dev;

public sealed class DevEndpointTests
{
    [Fact]
    public async Task GetSessionAudit_Returns200_WithAuditEntriesInDevEnvironment()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();

        var created = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        var auditResponse = await client.GetAsync($"/api/dev/sessions/{created.Id}/audit");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var payload = await auditResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"entries\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GameStarted", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSessionAudit_PreservesPersistedOccurrenceMetadataAcrossReads()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();

        var created = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        var route = $"/api/dev/sessions/{created.Id}/audit";
        var occurrenceBase = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        Dictionary<long, DateTime> expectedOccurrences;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
            var storedEvents = await dbContext.StoredEvents
                .Where(storedEvent => storedEvent.StreamId == created.Id)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .ToArrayAsync();

            Assert.NotEmpty(storedEvents);
            foreach (var storedEvent in storedEvents)
            {
                storedEvent.OccurredAtUtc = occurrenceBase.AddMinutes(storedEvent.Sequence);
            }

            await dbContext.SaveChangesAsync();
            expectedOccurrences = storedEvents.ToDictionary(
                storedEvent => storedEvent.Sequence,
                storedEvent => storedEvent.OccurredAtUtc);
        }

        var firstRead = await client.GetFromJsonAsync<SessionAuditDto>(route);
        var secondRead = await client.GetFromJsonAsync<SessionAuditDto>(route);

        Assert.NotNull(firstRead);
        Assert.NotNull(secondRead);
        Assert.NotEmpty(firstRead.Entries);
        Assert.Equal(expectedOccurrences.Keys.Order(), firstRead.Entries.Select(entry => entry.Sequence));
        foreach (var entry in firstRead.Entries)
        {
            Assert.Equal(expectedOccurrences[entry.Sequence], entry.OccurredAtUtc);
        }

        Assert.Equal(firstRead.Entries, secondRead.Entries);
    }

    [Fact]
    public async Task GetSessionAudit_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();

        // Even a valid session ID should be denied — the guard runs before the handler.
        var auditResponse = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/audit");
        Assert.Equal(HttpStatusCode.Forbidden, auditResponse.StatusCode);
    }

    [Fact]
    public async Task GetSessionAudit_Returns404_WhenSessionDoesNotExist()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var auditResponse = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/audit");
        Assert.Equal(HttpStatusCode.NotFound, auditResponse.StatusCode);
    }

    [Fact]
    public async Task PlayerFacingAuditPath_StillReturns404()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.StartingTownReady();
        scenario.AssertReady();

        var created = await client.CreateStartedGameAsync(scenario, "Ranger Vale");

        // The player-facing audit path must remain closed even though /api/dev/ exists.
        var playerAuditResponse = await client.GetAsync($"/api/games/{created.Id}/projections/audit");
        Assert.Equal(HttpStatusCode.NotFound, playerAuditResponse.StatusCode);
    }
}
