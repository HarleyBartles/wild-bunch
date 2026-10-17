using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Tests.Handlers;

public sealed class CompletePlayerSetupOperationTests
{
    [Fact]
    public async Task ExactRetryReturnsOriginalSessionWithoutAppendingEvents()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new CompletePlayerSetupHandler(new StubNewGameFactory(), repository, repository);
        var command = Command(Guid.NewGuid());

        var first = await handler.HandleAsync(command);
        var before = await repository.GetEventStreamAsync(new GameSessionId(first.Id));
        var retry = await handler.HandleAsync(command);
        var after = await repository.GetEventStreamAsync(new GameSessionId(first.Id));

        Assert.Equal(first.Id, retry.Id);
        Assert.Equal(before, after);
        Assert.Single(repository.Sessions);
        Assert.Equal(1, repository.CommitCalls);
    }

    [Fact]
    public async Task ReusingOperationIdWithDifferentInputConflictsWithoutChangingSession()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new CompletePlayerSetupHandler(new StubNewGameFactory(), repository, repository);
        var command = Command(Guid.NewGuid());
        var first = await handler.HandleAsync(command);
        var before = await repository.GetEventStreamAsync(new GameSessionId(first.Id));

        await Assert.ThrowsAsync<StartupOperationConflictException>(() => handler.HandleAsync(command with { PlayerName = "Different Name" }));

        var after = await repository.GetEventStreamAsync(new GameSessionId(first.Id));
        Assert.Equal(before, after);
        Assert.Single(repository.Sessions);
        Assert.Equal(1, repository.CommitCalls);
    }

    [Fact]
    public async Task NewSetupOperationWhilePlaythroughIsActiveConflictsWithoutArchivingIt()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new CompletePlayerSetupHandler(new StubNewGameFactory(), repository, repository);
        var first = await handler.HandleAsync(Command(Guid.NewGuid()));
        var firstSessionId = new GameSessionId(first.Id);
        var before = await repository.GetEventStreamAsync(firstSessionId);

        await Assert.ThrowsAsync<StartupOperationConflictException>(() => handler.HandleAsync(Command(Guid.NewGuid())));

        var after = await repository.GetEventStreamAsync(firstSessionId);
        Assert.Equal(before, after);
        Assert.Equal(GameStatus.Active, repository.Sessions.Single().Status);
        Assert.DoesNotContain(after, @event => @event is WildBunch.Domain.Events.PlaythroughArchived);
        Assert.Equal(1, repository.CommitCalls);
    }

    [Fact]
    public async Task NewSetupIsAllowedAfterConfirmedArchiveHasCommitted()
    {
        var repository = new InMemoryGameSessionRepository();
        var setupHandler = new CompletePlayerSetupHandler(new StubNewGameFactory(), repository, repository);
        var first = await setupHandler.HandleAsync(Command(Guid.NewGuid()));
        var archiveHandler = new ArchivePlaythroughHandler(repository, repository);
        await archiveHandler.HandleAsync(new ArchivePlaythroughCommand(new GameSessionId(first.Id), "player-start-over"));

        var replacement = await setupHandler.HandleAsync(Command(Guid.NewGuid()));

        Assert.NotEqual(first.Id, replacement.Id);
        Assert.Equal(2, repository.Sessions.Count);
        Assert.Equal(1, repository.Sessions.Count(session => session.Status == GameStatus.Active));
        Assert.Equal(1, repository.Sessions.Count(session => session.Status == GameStatus.Archived));
        Assert.Equal(3, repository.CommitCalls);
    }

    private static CompletePlayerSetupCommand Command(Guid operationId) => new()
    {
        OperationId = operationId,
        PlayerName = "Ranger Vale",
        GameDifficulty = GameDifficulty.Standard,
        GameEntropy = GameEntropy.Classic,
        SeedCode = "00000000-0000-0000-0000-000000000000"
    };
}
