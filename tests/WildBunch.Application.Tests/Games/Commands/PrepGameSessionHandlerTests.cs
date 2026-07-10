using WildBunch.Application.Games.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using Xunit;

namespace WildBunch.Application.Tests.Games.Commands;

public sealed class PrepGameSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesPreppedSession()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new PrepGameSessionHandler(repository, repository);

        var command = new PrepGameSessionCommand("test-seed", GameDifficulty.Standard, GameEntropy.Classic);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.NotNull(result.GameSessionId);
        var sessionIdGuid = Guid.Parse(result.GameSessionId);
        Assert.NotEqual(Guid.Empty, sessionIdGuid);

        // Verify the session was stored
        var sessionId = new GameSessionId(sessionIdGuid);
        var session = await repository.GetByIdAsync(sessionId, CancellationToken.None);
        Assert.NotNull(session);
        Assert.Equal(GameStatus.Prepped, session.Status);
        Assert.Equal("test-seed", session.SeedCode);
        Assert.Equal(GameDifficulty.Standard, session.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, session.GameEntropy);
    }
}
