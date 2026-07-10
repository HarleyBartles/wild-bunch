using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using Xunit;

namespace WildBunch.Domain.Tests.Game;

public sealed class GameSessionStartPreppedTests
{
    [Fact]
    public void StartPrepped_CreatesMinimalSessionWithPreppedStatus()
    {
        var session = GameSession.StartPrepped("test-seed", GameDifficulty.Standard, GameEntropy.Classic);
        
        Assert.NotNull(session);
        Assert.Equal(GameStatus.Prepped, session.Status);
        Assert.Equal("test-seed", session.SeedCode);
        Assert.Equal(GameDifficulty.Standard, session.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, session.GameEntropy);
        Assert.Null(session.World);
        Assert.Null(session.CaseFile);
    }
}
