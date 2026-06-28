using WildBunch.Application.Games.Commands;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;

namespace WildBunch.Application.Tests;

public sealed class StartNewGameHandlerTests
{
    [Fact]
    public async Task StartNewGameCreatesSessionSavesItAndReturnsDto()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartNewGameHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new StartNewGameCommand("Ranger Vale"));

        Assert.Equal("Ranger Vale", factory.RequestedPlayerNames.Single());
        Assert.Equal(WildBunch.Domain.Travel.GameDifficulty.Normal, factory.RequestedGameDifficulties.Single());
        Assert.Equal(WildBunch.Domain.Travel.GameEntropy.Standard, factory.RequestedEntropies.Single());
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal(factory.CreatedSession.Id.Value, result.Id);
        Assert.Equal("Ranger Vale", result.Player.Name);
        Assert.Equal(WildBunch.Domain.Game.GameStatus.Active, result.Status);
        Assert.Equal(WildBunch.Domain.Travel.GameDifficulty.Normal, result.GameDifficulty);
        Assert.Equal(WildBunch.Domain.Travel.GameEntropy.Standard, result.Entropy);
        Assert.Equal("dustvale", result.Player.CurrentTownId);
        Assert.NotEmpty(result.LogEntries);
        Assert.Contains(result.LogEntries, entry => entry.Kind == WildBunch.Domain.Game.GameLogEntryKind.Opening);
    }

    [Fact]
    public async Task StartNewGameForwardsSelectedGameDifficulty()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartNewGameHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        await handler.HandleAsync(new StartNewGameCommand("Ranger Vale", WildBunch.Domain.Travel.GameDifficulty.Easy));

        Assert.Equal(WildBunch.Domain.Travel.GameDifficulty.Easy, factory.RequestedGameDifficulties.Single());
    }

    [Fact]
    public async Task StartNewGameForwardsSelectedEntropy()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartNewGameHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        await handler.HandleAsync(new StartNewGameCommand("Ranger Vale", Entropy: WildBunch.Domain.Travel.GameEntropy.Boring));

        Assert.Equal(WildBunch.Domain.Travel.GameEntropy.Boring, factory.RequestedEntropies.Single());
    }

    [Fact]
    public async Task StartNewGameForwardsSetupSeedCode()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartNewGameHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        await handler.HandleAsync(new StartNewGameCommand("Ranger Vale", SetupSeedCode: "not-a-uuid"));

        Assert.Equal("not-a-uuid", factory.RequestedSetupSeedCodes.Single());
    }

    [Fact]
    public async Task StartNewGameForwardsStartingTownId()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartNewGameHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        await handler.HandleAsync(new StartNewGameCommand("Ranger Vale", StartingTownId: "redmesa"));

        Assert.Equal("redmesa", factory.RequestedStartingTownIds.Single());
    }

    [Fact]
    public async Task StartNewGameForwardsNullStartingTownIdByDefault()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartNewGameHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        await handler.HandleAsync(new StartNewGameCommand("Ranger Vale"));

        Assert.Null(factory.RequestedStartingTownIds.Single());
    }

    [Fact]
    public async Task StartNewGameReturnsDtoWithHudAndDiaryProjections()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartNewGameHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new StartNewGameCommand("Ranger Vale"));

        Assert.NotNull(result.HudProjection);
        Assert.Equal("Ranger Vale", result.HudProjection!.PlayerName);
        Assert.Equal(WildBunch.Domain.Game.GameStatus.Active, result.HudProjection.Status);
        Assert.NotNull(result.DiaryProjection);
        Assert.NotEmpty(result.DiaryProjection!.Entries);
        Assert.Equal(result.Id, result.HudProjection.SessionId);
        Assert.Equal(result.Id, result.DiaryProjection.SessionId);
    }
}
