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

namespace WildBunch.Application.Tests;

public sealed class GetWorldMapHandlerTests
{
    [Fact]
    public async Task ReturnsAllSeededTownsAndTrails()
    {
        var (handler, sessionId) = CreateHandlerWithSession();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery(sessionId));
        Assert.Equal(8, result.Towns.Count);
        Assert.Equal(14, result.Trails.Count);
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
        return factory.Create(
            "Test Player",
            difficulty.Difficulty,
            seedWorld.SeedCode.ToString("D"),
            GameEntropy.Boring);
    }

    private sealed class TestFixedSaltSourceFactory : ISaltSourceFactory
    {
        public SaltSource Create(string? setupSeedCode, DomainGameDifficulty gameDifficulty)
            => SaltSource.CreateFixed("test-fixed-salt");
    }
}
