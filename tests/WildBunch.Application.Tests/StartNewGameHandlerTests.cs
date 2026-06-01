using WildBunch.Application.Games.Commands;
using WildBunch.Application.Tests.TestDoubles;

namespace WildBunch.Application.Tests;

public sealed class StartNewGameHandlerTests
{
    [Fact]
    public async Task StartNewGameCreatesSessionSavesItAndReturnsDto()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartNewGameHandler(factory, repository, repository);

        var result = await handler.HandleAsync(new StartNewGameCommand("Ranger Vale"));

        Assert.Equal("Ranger Vale", factory.RequestedPlayerNames.Single());
        Assert.Equal(WildBunch.Domain.Travel.TravelDifficulty.Normal, factory.RequestedTravelDifficulties.Single());
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal(factory.CreatedSession.Id.Value, result.Id);
        Assert.Equal("Ranger Vale", result.Player.Name);
        Assert.Equal(WildBunch.Domain.Game.GameStatus.Active, result.Status);
        Assert.Equal(WildBunch.Domain.Travel.TravelDifficulty.Normal, result.TravelDifficulty);
        Assert.Equal("dustvale", result.Player.CurrentTownId);
        Assert.NotEmpty(result.LogEntries);
        Assert.Contains(result.LogEntries, entry => entry.Kind == WildBunch.Domain.Game.GameLogEntryKind.Opening);
    }

    [Fact]
    public async Task StartNewGameForwardsSelectedTravelDifficulty()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartNewGameHandler(factory, repository, repository);

        await handler.HandleAsync(new StartNewGameCommand("Ranger Vale", WildBunch.Domain.Travel.TravelDifficulty.Easy));

        Assert.Equal(WildBunch.Domain.Travel.TravelDifficulty.Easy, factory.RequestedTravelDifficulties.Single());
    }

    [Fact]
    public async Task StartNewGameForwardsSetupSeedCode()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartNewGameHandler(factory, repository, repository);

        await handler.HandleAsync(new StartNewGameCommand("Ranger Vale", SetupSeedCode: "not-a-uuid"));

        Assert.Equal("not-a-uuid", factory.RequestedSetupSeedCodes.Single());
    }
}
