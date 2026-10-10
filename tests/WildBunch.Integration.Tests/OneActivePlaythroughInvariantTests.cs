using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Api.Games;
using WildBunch.Application.Abstractions;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence;

namespace WildBunch.Integration.Tests;

/// <summary>
/// The single-active-playthrough rule rejects a second setup until the current playthrough
/// is explicitly archived. Creating a replacement must never silently mutate the old stream.
/// </summary>
public sealed class OneActivePlaythroughInvariantTests
{
    [Fact]
    public async Task ConcurrentDistinctSetupsCreateAtMostOneActivePlaythrough()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();
        var firstOperation = Guid.NewGuid();
        var secondOperation = Guid.NewGuid();

        var responses = await Task.WhenAll(
            client.PostAsJsonWithStartupOperationAsync(
                "/api/games/setup",
                scenario.CreateRequest("Ranger Vale"),
                firstOperation),
            client.PostAsJsonWithStartupOperationAsync(
                "/api/games/setup",
                scenario.CreateRequest("Trail Hand"),
                secondOperation));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        var sessions = await dbContext.GameSessions.AsNoTracking().ToArrayAsync();
        Assert.Single(sessions);
        Assert.Equal(GameStatus.Active.ToString(), sessions[0].Status);
    }

    [Fact]
    public async Task SetupWithActivePlaythroughIsRejectedWithoutArchivingIt()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var activeSession = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        var secondSetup = await client.PostAsJsonWithStartupOperationAsync(
            "/api/games/setup",
            new SetupGameRequest(
                "Trail Hand",
                scenario.Fixture.GameDifficulty,
                scenario.Fixture.SeedCode,
                scenario.Fixture.GameEntropy));

        Assert.Equal(HttpStatusCode.Conflict, secondSetup.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        var sessions = await dbContext.GameSessions.AsNoTracking().ToArrayAsync();
        Assert.Single(sessions);
        Assert.Equal(activeSession.Id, sessions[0].Id);
        Assert.Equal(GameStatus.Active.ToString(), sessions[0].Status);

        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var events = await repository.GetEventStreamAsync(new GameSessionId(activeSession.Id));
        Assert.DoesNotContain(events, gameEvent => gameEvent is PlaythroughArchived);
    }

    [Fact]
    public async Task ExplicitArchiveAllowsNewPlaythroughWithoutResurrectingOldOne()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var scenario = BoringScenarioBuilder.MountedTravelReady();
        scenario.AssertReady();

        var previous = await client.CreateStartedGameAsync(scenario, "Ranger Vale");
        var archiveResponse = await client.PostAsync($"/api/games/{previous.Id}/archive", content: null);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);

        var next = await client.CreateStartedGameAsync(scenario, "Trail Hand");
        Assert.Equal(GameStatus.Active, next.Status);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        var activeRows = await dbContext.GameSessions
            .AsNoTracking()
            .Where(session => session.Status == GameStatus.Active.ToString())
            .ToArrayAsync();
        var archivedRows = await dbContext.GameSessions
            .AsNoTracking()
            .Where(session => session.Status == GameStatus.Archived.ToString())
            .ToArrayAsync();

        Assert.Collection(activeRows, row => Assert.Equal(next.Id, row.Id));
        Assert.Collection(archivedRows, row => Assert.Equal(previous.Id, row.Id));

        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var previousEvents = await repository.GetEventStreamAsync(new GameSessionId(previous.Id));
        var archiveEvent = Assert.Single(previousEvents.OfType<PlaythroughArchived>());
        Assert.Equal("player-start-over", archiveEvent.ArchiveReason);
        Assert.Equal(GameStatus.Active, archiveEvent.StatusBeforeArchive);

        var nextEvents = await repository.GetEventStreamAsync(new GameSessionId(next.Id));
        Assert.DoesNotContain(nextEvents, gameEvent => gameEvent is PlaythroughArchived);
    }
}
