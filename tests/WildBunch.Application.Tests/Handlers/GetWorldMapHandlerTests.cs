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
    public async Task ProjectsTheLoadedSetupWorldTownAndTrailFacts()
    {
        var (handler, session) = CreateHandlerWithSession();
        var result = await handler.HandleAsync(new GetWorldMapQuery(session.Id.Value));

        Assert.Equal(
            session.World.Towns.Select(town => (town.Id.Value, town.Name, town.MapX, town.MapY)),
            result.Towns.Select(town => (town.Id, town.Name, town.X, town.Y)));
        Assert.Equal(
            session.World.Trails.Select(trail => (
                trail.Id.Value,
                trail.FromTownId.Value,
                trail.ToTownId.Value,
                trail.RideDayDistance)),
            result.Trails.Select(trail => (
                trail.Id,
                trail.FromTownId,
                trail.ToTownId,
                trail.RideDayDistance)));
    }

    [Fact]
    public async Task ThrowsForMissingSession()
    {
        var repo = new InMemoryGameSessionRepository();
        var handler = new GetWorldMapHandler(repo);
        await Assert.ThrowsAsync<GameSessionNotFoundException>(() =>
            handler.HandleAsync(new GetWorldMapQuery(Guid.NewGuid())));
    }

    private static (GetWorldMapHandler Handler, GameSession Session) CreateHandlerWithSession()
    {
        var repo = new InMemoryGameSessionRepository();
        var session = CreateTestSession();
        repo.Seed(session);
        IGameSessionReadRepository readRepository = repo;
        return (new GetWorldMapHandler(readRepository), session);
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
        return session;
    }

    private sealed class TestFixedSaltSourceFactory : ISaltSourceFactory
    {
        public SaltSource Create(string? setupSeedCode, DomainGameDifficulty gameDifficulty)
            => SaltSource.CreateFixed("test-fixed-salt");
    }
}
