using WildBunch.Domain;
using WildBunch.Domain.Game;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionAggregateRootTests
{
    [Fact]
    public void GameSessionIsMarkedAsTheMutableAggregateRoot()
    {
        Assert.True(typeof(IAggregateRoot).IsAssignableFrom(typeof(GameSession)));
    }
}
