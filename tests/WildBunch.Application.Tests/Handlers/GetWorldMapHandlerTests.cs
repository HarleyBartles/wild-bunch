using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;
using WildBunch.GameContent.NewGame;
using DomainGameDifficulty = WildBunch.Domain.Travel.GameDifficulty;

namespace WildBunch.Application.Tests.Handlers;

public sealed class GetWorldMapHandlerTests
{
    [Fact]
    public async Task ReturnsAllSeededTownsAndTrails()
    {
        var (handler, sessionId) = CreateHandlerWithSession();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery(sessionId));
        Assert.Equal(8, result.Towns.Count);
        Assert.NotEmpty(result.Trails);
    }

    [Fact]
    public async Task ThrowsForMissingSession()
    {
        var repo = new InMemoryGameSessionRepository();
        var handler = new GetStartingTownMapHandler(repo);
        await Assert.ThrowsAsync<GameSessionNotFoundException>(() =>
            handler.HandleAsync(new GetStartingTownMapQuery(Guid.NewGuid())));
    }

    private static (GetStartingTownMapHandler Handler, Guid SessionId) CreateHandlerWithSession()
    {
        var repo = new InMemoryGameSessionRepository();
        var session = CreateTestSession();
        repo.Seed(session);
        return (new GetStartingTownMapHandler(repo), session.Id.Value);
    }

    private static GameSession CreateTestSession()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var difficulty = DifficultyEnvelope.For(DomainGameDifficulty.Standard);
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());
        var (world, caseFile, seedCodeText, saltSource) = factory.ResolveWorld(
            "Test Player", difficulty.Difficulty, seedWorld.SeedCode.ToString("D"), GameEntropy.Boring);
        var session = GameSession.StartSetup(
            "Test Player", world, caseFile, difficulty.Difficulty, GameEntropy.Boring, seedCodeText, saltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(world.Towns.First().Id);
        var (wallet, inventory) = factory.ResolveStartingResources(difficulty.Difficulty);
        session.CompleteGameStart(wallet, inventory);
        return session;
    }

    private sealed class TestFixedSaltSourceFactory : ISaltSourceFactory
    {
        public SaltSource Create(string? setupSeedCode, DomainGameDifficulty gameDifficulty)
            => SaltSource.CreateFixed("test-fixed-salt");
    }
}
