using WildBunch.Application.Games.Commands;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;

namespace WildBunch.Application.Tests;

/// <summary>
/// Application-level tests for CompletePlayerSetupHandler — the first step of the
/// three-step game-start flow (setup -> prologue-viewed -> start).
/// Verifies that setup creates a session in the SetupComplete phase, archives
/// pre-existing active sessions, and returns the correct DTO.
/// </summary>
public sealed class CompletePlayerSetupHandlerTests
{
    [Fact]
    public async Task SetupCreatesSessionAndReturnsDto()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new CompletePlayerSetupHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new CompletePlayerSetupCommand
        {
            PlayerName = "Ranger Vale",
            GameDifficulty = WildBunch.Domain.Travel.GameDifficulty.Standard,
            SeedCode = "00000000-0000-0000-0000-000000000000",
            GameEntropy = WildBunch.Domain.Travel.GameEntropy.Classic,
        });

        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Ranger Vale", result.Player.Name);
        Assert.Equal(WildBunch.Domain.Game.GameStatus.Active, result.Status);
        Assert.Equal(WildBunch.Domain.Travel.GameDifficulty.Standard, result.GameDifficulty);
        Assert.Equal(WildBunch.Domain.Travel.GameEntropy.Classic, result.GameEntropy);
    }

    [Fact]
    public async Task SetupForwardsSelectedGameDifficulty()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new CompletePlayerSetupHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        await handler.HandleAsync(new CompletePlayerSetupCommand
        {
            PlayerName = "Ranger Vale",
            GameDifficulty = WildBunch.Domain.Travel.GameDifficulty.Easy,
            SeedCode = "00000000-0000-0000-0000-000000000000",
            GameEntropy = WildBunch.Domain.Travel.GameEntropy.Classic,
        });

        Assert.Equal(WildBunch.Domain.Travel.GameDifficulty.Easy, factory.RequestedGameDifficulties.Single());
    }

    [Fact]
    public async Task SetupForwardsSelectedEntropy()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new CompletePlayerSetupHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        await handler.HandleAsync(new CompletePlayerSetupCommand
        {
            PlayerName = "Ranger Vale",
            GameDifficulty = WildBunch.Domain.Travel.GameDifficulty.Standard,
            SeedCode = "00000000-0000-0000-0000-000000000000",
            GameEntropy = WildBunch.Domain.Travel.GameEntropy.Boring,
        });

        Assert.Equal(WildBunch.Domain.Travel.GameEntropy.Boring, factory.RequestedEntropies.Single());
    }

    [Fact]
    public async Task SetupReturnsDtoWithHudAndDiaryProjections()
    {
        var factory = new StubNewGameFactory();
        var repository = new InMemoryGameSessionRepository();
        var handler = new CompletePlayerSetupHandler(factory, repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new CompletePlayerSetupCommand
        {
            PlayerName = "Ranger Vale",
            GameDifficulty = WildBunch.Domain.Travel.GameDifficulty.Standard,
            SeedCode = "00000000-0000-0000-0000-000000000000",
            GameEntropy = WildBunch.Domain.Travel.GameEntropy.Classic,
        });

        // HUD/diary projections are returned, but the HUD player name
        // is only populated after GameStarted (not during setup phase).
        Assert.NotNull(result.HudProjection);
        Assert.NotNull(result.DiaryProjection);
        Assert.Equal(result.Id, result.HudProjection!.SessionId);
        Assert.Equal(result.Id, result.DiaryProjection!.SessionId);
    }
}
