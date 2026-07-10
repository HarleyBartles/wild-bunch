using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Application.Tests.Games.Commands;

public sealed class StartGameSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_StartsPreppedSession()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartGameSessionHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        // Create and store a prepped session
        var prepped = GameSession.StartPrepped("test-seed", GameDifficulty.Standard, GameEntropy.Classic);
        await repository.StoreAsync(prepped, Guid.NewGuid(), CancellationToken.None);
        await repository.CommitAsync(CancellationToken.None);
        prepped.MarkEventsCommitted();

        var command = new StartGameSessionCommand(prepped.Id.Value);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(GameStatus.Active, result.Status);
        Assert.Equal(GameDifficulty.Standard, result.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, result.GameEntropy);
    }

    [Fact]
    public async Task HandleAsync_ThrowsWhenSessionNotFound()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartGameSessionHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        var command = new StartGameSessionCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<GameSessionNotFoundException>(() =>
            handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ThrowsWhenSessionNotPrepped()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartGameSessionHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        // Create a session in Active status (not Prepped)
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.None);
        var world = new World(new[] { dustvale }, Array.Empty<Trail>());
        var activeSession = GameSession.StartSetup(
            "Player",
            world,
            new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"),
                CaseOpeningLead.Create("test"), Array.Empty<Clue>()),
            GameDifficulty.Standard,
            GameEntropy.Classic,
            "test-seed",
            SaltSource.CreateRuntime());
        activeSession.ViewPrologue("test-prologue");
        activeSession.SelectStartingTown(dustvale.Id);
        activeSession.CompleteGameStart(
            WildBunch.Domain.Economy.Wallet.Starting(25m),
            WildBunch.Domain.Inventory.Inventory.Empty());

        await repository.StoreAsync(activeSession, Guid.NewGuid(), CancellationToken.None);
        await repository.CommitAsync(CancellationToken.None);
        activeSession.MarkEventsCommitted();

        var command = new StartGameSessionCommand(activeSession.Id.Value);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_PassesDevLayoutSaltsToFactory()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartGameSessionHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        // Create a prepped session without dev layout salts
        var prepped = GameSession.StartPrepped("test-seed", GameDifficulty.Standard, GameEntropy.Classic);
        await repository.StoreAsync(prepped, Guid.NewGuid(), CancellationToken.None);
        await repository.CommitAsync(CancellationToken.None);
        prepped.MarkEventsCommitted();

        var command = new StartGameSessionCommand(prepped.Id.Value);
        await handler.HandleAsync(command, CancellationToken.None);

        // Verify the factory received null dev layout salts (since none were set)
        Assert.Single(factory.RequestedDevLayoutSalts);
        Assert.Null(factory.RequestedDevLayoutSalts[0]);
    }

    [Fact]
    public async Task HandleAsync_ReturnsDtoWithHudAndDiaryProjections()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new StartGameSessionHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        var prepped = GameSession.StartPrepped("test-seed", GameDifficulty.Standard, GameEntropy.Classic);
        await repository.StoreAsync(prepped, Guid.NewGuid(), CancellationToken.None);
        await repository.CommitAsync(CancellationToken.None);
        prepped.MarkEventsCommitted();

        var command = new StartGameSessionCommand(prepped.Id.Value);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.NotNull(result.HudProjection);
        Assert.NotNull(result.DiaryProjection);
        Assert.Equal(result.Id, result.HudProjection!.SessionId);
        Assert.Equal(result.Id, result.DiaryProjection!.SessionId);
    }
}
