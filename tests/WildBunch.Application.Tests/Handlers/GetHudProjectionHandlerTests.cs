using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests.Handlers;

public sealed class GetHudProjectionHandlerTests
{
    [Fact]
    public async Task HandleAsync_ThrowsWhenSessionIsMissing()
    {
        var handler = new GetHudProjectionHandler(
            new InMemoryGameSessionRepository(),
            new HudProjector());

        await Assert.ThrowsAsync<GameSessionNotFoundException>(
            () => handler.HandleAsync(new GetHudProjectionQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNoProjectionBeforeGameStarts()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(started: false);
        repository.Seed(session);
        var handler = new GetHudProjectionHandler(repository, new HudProjector());

        var projection = await handler.HandleAsync(new GetHudProjectionQuery(session.Id.Value));

        Assert.Null(projection);
        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public async Task HandleAsync_ProjectsStartedSessionFromEventsWithoutWriting()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(started: true);
        repository.Seed(session);
        var handler = new GetHudProjectionHandler(repository, new HudProjector());

        var projection = await handler.HandleAsync(new GetHudProjectionQuery(session.Id.Value));

        Assert.NotNull(projection);
        Assert.Equal(session.Id.Value, projection!.SessionId);
        Assert.Equal("Ranger Vale", projection.PlayerName);
        Assert.True(projection.Health > 0);
        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
        Assert.Equal(0, session.Clock.Turn);
    }

    private static GameSession CreateSession(bool started)
    {
        var startedSession = new StubNewGameFactory().CreatedSession;
        if (started)
        {
            return startedSession;
        }

        var session = GameSession.StartSetup(
            "Ranger Vale",
            startedSession.World,
            startedSession.CaseFile,
            startedSession.GameDifficulty,
            startedSession.GameEntropy,
            startedSession.SeedCode!,
            startedSession.SaltSource);

        session.ViewPrologue("test-prologue-descriptor");
        return session;
    }
}
