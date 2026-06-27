using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Api.Games;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence;

namespace WildBunch.Integration.Tests;

/// <summary>
/// Integration tests proving the one-active-playthrough invariant is enforced at the
/// persistence level: after consecutive <c>POST /api/games</c> calls, exactly one Active
/// session exists in the persisted PostgreSQL store. The previously-created session is
/// archived (not deleted) with a <see cref="PlaythroughArchived"/> event carrying the
/// <c>superseded-by-new-playthrough</c> reason. See BUNCH-102.
/// </summary>
public sealed class OneActivePlaythroughInvariantTests
{
    [Fact]
    public async Task ConsecutiveGameCreatesLeaveExactlyOneActiveSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        // Create session A.
        var createAResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Ranger Vale"));
        Assert.Equal(HttpStatusCode.Created, createAResponse.StatusCode);
        var sessionA = await createAResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(sessionA);
        Assert.Equal(GameStatus.Active, sessionA!.Status);

        // Create session B — this must archive session A.
        var createBResponse = await client.PostAsJsonAsync("/api/games", scenario.CreateRequest("Trail Hand"));
        Assert.Equal(HttpStatusCode.Created, createBResponse.StatusCode);
        var sessionB = await createBResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(sessionB);
        Assert.Equal(GameStatus.Active, sessionB!.Status);

        // Assert against persisted PostgreSQL state: query the GameSessions table directly.
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();

        var activeRows = await dbContext.GameSessions
            .AsNoTracking()
            .Where(e => e.Status == GameStatus.Active.ToString())
            .ToArrayAsync();
        var archivedRows = await dbContext.GameSessions
            .AsNoTracking()
            .Where(e => e.Status == GameStatus.Archived.ToString())
            .ToArrayAsync();

        // Exactly one Active row (session B).
        Assert.Single(activeRows);
        Assert.Equal(sessionB.Id, activeRows[0].Id);

        // Session A is Archived (not deleted).
        var sessionARow = archivedRows.SingleOrDefault(r => r.Id == sessionA.Id);
        Assert.NotNull(sessionARow);
        Assert.Equal(GameStatus.Archived.ToString(), sessionARow!.Status);

        // Session A is still loadable by id via the API with Archived status.
        var getAResponse = await client.GetAsync($"/api/games/{sessionA.Id}");
        Assert.Equal(HttpStatusCode.OK, getAResponse.StatusCode);
        var fetchedA = await getAResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(fetchedA);
        Assert.Equal(sessionA.Id, fetchedA!.Id);
        Assert.Equal(GameStatus.Archived, fetchedA.Status);

        // Session B is loadable by id and still Active.
        var getBResponse = await client.GetAsync($"/api/games/{sessionB.Id}");
        Assert.Equal(HttpStatusCode.OK, getBResponse.StatusCode);
        var fetchedB = await getBResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(fetchedB);
        Assert.Equal(sessionB.Id, fetchedB!.Id);
        Assert.Equal(GameStatus.Active, fetchedB.Status);

        // The PlaythroughArchived event is in session A's event stream with the
        // invariant-driven archive reason.
        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var events = await repository.GetEventStreamAsync(new GameSessionId(sessionA.Id));
        var archivedEvent = events.OfType<PlaythroughArchived>().Single();
        Assert.Equal("superseded-by-new-playthrough", archivedEvent.ArchiveReason);
        Assert.Equal(GameStatus.Active, archivedEvent.StatusBeforeArchive);
    }
}
