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
        var sessionA = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        Assert.Equal(GameStatus.Active, sessionA.Status);

        // Create session B — this must archive session A.
        var sessionB = await client.CreateStartedGameAsync(scenario, "Trail Hand");
        Assert.Equal(GameStatus.Active, sessionB.Status);

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

    /// <summary>
    /// Falsification check: three consecutive <c>POST /api/games</c> calls must still leave
    /// exactly one Active session. Create A, create B (archives A), create C (archives B).
    /// Assert exactly one Active row (C), two Archived rows (A and B), and that each Archived
    /// row's event stream carries a <see cref="PlaythroughArchived"/> event with the
    /// <c>superseded-by-new-playthrough</c> reason. See BUNCH-102.
    /// </summary>
    [Fact]
    public async Task ThreeConsecutiveCreatesLeaveExactlyOneActiveSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        // Create session A.
        var sessionA = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        Assert.Equal(GameStatus.Active, sessionA.Status);

        // Create session B — this must archive session A.
        var sessionB = await client.CreateStartedGameAsync(scenario, "Trail Hand");
        Assert.Equal(GameStatus.Active, sessionB.Status);

        // Create session C — this must archive session B.
        var sessionC = await client.CreateStartedGameAsync(scenario, "Newcomer");
        Assert.Equal(GameStatus.Active, sessionC.Status);

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

        // Exactly one Active row (session C).
        Assert.Single(activeRows);
        Assert.Equal(sessionC.Id, activeRows[0].Id);

        // Exactly two Archived rows (sessions A and B).
        Assert.Equal(2, archivedRows.Length);
        var sessionARow = archivedRows.Single(r => r.Id == sessionA.Id);
        var sessionBRow = archivedRows.Single(r => r.Id == sessionB.Id);
        Assert.Equal(GameStatus.Archived.ToString(), sessionARow.Status);
        Assert.Equal(GameStatus.Archived.ToString(), sessionBRow.Status);

        // Each Archived row's event stream carries a PlaythroughArchived event with the
        // invariant-driven archive reason and the Active status it held before archiving.
        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();

        var eventsA = await repository.GetEventStreamAsync(new GameSessionId(sessionA.Id));
        var archivedEventA = eventsA.OfType<PlaythroughArchived>().Single();
        Assert.Equal("superseded-by-new-playthrough", archivedEventA.ArchiveReason);
        Assert.Equal(GameStatus.Active, archivedEventA.StatusBeforeArchive);

        var eventsB = await repository.GetEventStreamAsync(new GameSessionId(sessionB.Id));
        var archivedEventB = eventsB.OfType<PlaythroughArchived>().Single();
        Assert.Equal("superseded-by-new-playthrough", archivedEventB.ArchiveReason);
        Assert.Equal(GameStatus.Active, archivedEventB.StatusBeforeArchive);

        // The new Active session C has no PlaythroughArchived event in its stream.
        var eventsC = await repository.GetEventStreamAsync(new GameSessionId(sessionC.Id));
        Assert.DoesNotContain(eventsC, e => e is PlaythroughArchived);
    }

    /// <summary>
    /// Falsification check: archiving the active session and then creating a new one must not
    /// resurrect the archived session. Create A, archive A via
    /// <c>POST /api/games/{A.Id}/archive</c>, create B. Assert A is still Archived (no
    /// resurrection), B is Active, and exactly one Active row remains. See BUNCH-102.
    /// </summary>
    [Fact]
    public async Task ArchiveThenCreateDoesNotResurrectArchivedSession()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        // Create session A.
        var sessionA = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        Assert.Equal(GameStatus.Active, sessionA.Status);

        // Archive session A via the explicit archive endpoint (player-start-over reason).
        var archiveResponse = await client.PostAsync($"/api/games/{sessionA.Id}/archive", content: null);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);

        // Create session B — there are no Active sessions, so nothing should be archived,
        // and session A must remain Archived (no resurrection).
        var sessionB = await client.CreateStartedGameAsync(scenario, "Trail Hand");
        Assert.Equal(GameStatus.Active, sessionB.Status);

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

        // Exactly one Archived row (session A) — not resurrected.
        Assert.Single(archivedRows);
        Assert.Equal(sessionA.Id, archivedRows[0].Id);
        Assert.Equal(GameStatus.Archived.ToString(), archivedRows[0].Status);

        // Session A is still loadable by id via the API with Archived status (no resurrection).
        var getAResponse = await client.GetAsync($"/api/games/{sessionA.Id}");
        Assert.Equal(HttpStatusCode.OK, getAResponse.StatusCode);
        var fetchedA = await getAResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        Assert.NotNull(fetchedA);
        Assert.Equal(sessionA.Id, fetchedA!.Id);
        Assert.Equal(GameStatus.Archived, fetchedA.Status);

        // Session A's event stream carries exactly one PlaythroughArchived event, with the
        // player-start-over reason from the explicit archive call (not a second
        // superseded-by-new-playthrough event from the later create).
        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var eventsA = await repository.GetEventStreamAsync(new GameSessionId(sessionA.Id));
        var archivedEvent = eventsA.OfType<PlaythroughArchived>().Single();
        Assert.Equal("player-start-over", archivedEvent.ArchiveReason);
        Assert.Equal(GameStatus.Active, archivedEvent.StatusBeforeArchive);

        // Session B's event stream has no PlaythroughArchived event.
        var eventsB = await repository.GetEventStreamAsync(new GameSessionId(sessionB.Id));
        Assert.DoesNotContain(eventsB, e => e is PlaythroughArchived);
    }
}
