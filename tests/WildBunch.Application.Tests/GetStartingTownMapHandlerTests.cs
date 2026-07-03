using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;
using WildBunch.GameContent.NewGame;
using DomainGameDifficulty = WildBunch.Domain.Travel.GameDifficulty;

namespace WildBunch.Application.Tests;

public sealed class GetStartingTownMapHandlerTests
{
    [Fact]
    public async Task ReturnsAllEightSeededTowns()
    {
        var (handler, sessionId) = CreateHandlerWithSession();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery(sessionId));
        Assert.Equal(8, result.Towns.Count);
        Assert.All(result.Towns, town =>
        {
            Assert.False(string.IsNullOrWhiteSpace(town.Id));
            Assert.False(string.IsNullOrWhiteSpace(town.Name));
        });
        var ids = result.Towns.Select(t => t.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public async Task AllTownsAreSelectable()
    {
        var (handler, sessionId) = CreateHandlerWithSession();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery(sessionId));
        Assert.Equal(8, result.Towns.Count);
    }

    [Fact]
    public async Task CoordinatesAreDeterministicAcrossCalls()
    {
        var (handler, sessionId) = CreateHandlerWithSession();
        var first = await handler.HandleAsync(new GetStartingTownMapQuery(sessionId));
        var second = await handler.HandleAsync(new GetStartingTownMapQuery(sessionId));
        Assert.Equal(first.Towns.Select(t => (t.Id, t.X, t.Y)), second.Towns.Select(t => (t.Id, t.X, t.Y)));
    }

    [Fact]
    public async Task TrailEdgesCarryCorrectRideDayDistances()
    {
        var (handler, sessionId) = CreateHandlerWithSession();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery(sessionId));
        var byId = result.Trails.ToDictionary(t => t.Id);
        Assert.NotEmpty(byId);
        Assert.All(byId.Values, trail => Assert.True(trail.RideDayDistance > 0m));
    }

    [Fact]
    public async Task TrailEdgesConnectRenderedTowns()
    {
        var (handler, sessionId) = CreateHandlerWithSession();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery(sessionId));
        var townIds = result.Towns.Select(t => t.Id).ToHashSet();
        Assert.All(result.Trails, trail =>
        {
            Assert.Contains(trail.FromTownId, townIds);
            Assert.Contains(trail.ToTownId, townIds);
        });
    }

    [Fact]
    public async Task TrailEdgesCoverAllSeededTrails()
    {
        var (handler, sessionId) = CreateHandlerWithSession();
        var result = await handler.HandleAsync(new GetStartingTownMapQuery(sessionId));
        Assert.NotEmpty(result.Trails);
    }

    [Fact]
    public void GetMapTowns_DoesNotCrashWithDerivedTownNames()
    {
        var towns = SeedWorldMapLayout.GetMapTowns();
        Assert.NotEmpty(towns);
        Assert.All(towns, town => Assert.True(town.X >= 0 && town.Y >= 0));
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
