using WildBunch.Domain.Game;
using Xunit;

namespace WildBunch.Domain.Tests.Game;

public sealed class GameStatusTests
{
    [Fact]
    public void GameStatus_Prepped_Exists()
    {
        var status = GameStatus.Prepped;
        Assert.Equal(4, (int)status);
    }
}
