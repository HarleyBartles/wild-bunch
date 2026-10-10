using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;

namespace WildBunch.Application.Tests.Dev;

public sealed class GetSessionAuditHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAuditEntriesFromEventStream()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = new StubNewGameFactory().CreatedSession;
        repository.Seed(session);
        await repository.StoreAsync(session);
        await repository.CommitAsync();

        var handler = new GetSessionAuditHandler(repository, new FullAuditProjector());

        var result = await handler.HandleAsync(new GetSessionAuditQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.SessionId);
        Assert.NotEmpty(result.Entries);
        Assert.Contains(result.Entries, e => e.EventType == "GameStarted");
        Assert.All(result.Entries, entry => Assert.Equal(DateTime.UnixEpoch, entry.OccurredAtUtc));
        Assert.Equal(Enumerable.Range(1, result.Entries.Count).Select(sequence => (long)sequence), result.Entries.Select(entry => entry.Sequence));
    }

    [Fact]
    public async Task HandleAsync_ThrowsWhenSessionDoesNotExist()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new GetSessionAuditHandler(repository, new FullAuditProjector());

        await Assert.ThrowsAsync<GameSessionNotFoundException>(
            () => handler.HandleAsync(new GetSessionAuditQuery(Guid.NewGuid())));
    }
}
