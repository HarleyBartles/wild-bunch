using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class GeometryPipelineTests
{
    private static GameSession CreateSessionThroughFullPipeline(GameEntropy entropy = GameEntropy.Boring)
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var difficulty = DifficultyEnvelope.For(GameDifficulty.Standard);
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());
        var (world, caseFile, seedCodeText, saltSource) = factory.ResolveWorld(
            "Test Player", difficulty.Difficulty, seedWorld.SeedCode.ToString("D"), entropy);
        var session = GameSession.StartSetup(
            "Test Player", world, caseFile, difficulty.Difficulty, entropy, seedCodeText, saltSource);
        return session;
    }

    [Fact]
    public void FullPipeline_ProducesWorldWithRealTrailsAndCoordinates()
    {
        var session = CreateSessionThroughFullPipeline();

        // 8 towns from canonical seed
        Assert.Equal(8, session.World.Towns.Count);

        // All towns have positive coordinates (clustered placement, not placeholder zeros)
        Assert.All(session.World.Towns, town =>
        {
            Assert.True(town.MapX > 0, $"Town {town.Name} has non-positive MapX: {town.MapX}");
            Assert.True(town.MapY > 0, $"Town {town.Name} has non-positive MapY: {town.MapY}");
        });

        // Non-empty trails (real MST graph, not stub linear chain)
        Assert.NotEmpty(session.World.Trails);

        // All trails have ride-day distances in 2-8 day range (honest 25px/day scale)
        Assert.All(session.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 8m));

        // All trail endpoints reference towns in the world
        var townIds = session.World.Towns.Select(t => t.Id).ToHashSet();
        Assert.All(session.World.Trails, trail =>
        {
            Assert.Contains(trail.FromTownId, townIds);
            Assert.Contains(trail.ToTownId, townIds);
        });
    }

    [Fact]
    public void FullPipeline_BoringMode_SameSeedProducesSameWorld()
    {
        var sessionA = CreateSessionThroughFullPipeline(GameEntropy.Boring);
        var sessionB = CreateSessionThroughFullPipeline(GameEntropy.Boring);

        var townsA = sessionA.World.Towns.ToArray();
        var townsB = sessionB.World.Towns.ToArray();

        Assert.Equal(townsA.Length, townsB.Length);
        for (var i = 0; i < townsA.Length; i++)
        {
            Assert.Equal(townsA[i].MapX, townsB[i].MapX);
            Assert.Equal(townsA[i].MapY, townsB[i].MapY);
        }
        Assert.Equal(sessionA.World.Trails.Count, sessionB.World.Trails.Count);
    }

    private sealed class TestFixedSaltSourceFactory : ISaltSourceFactory
    {
        public SaltSource Create(string? setupSeedCode, GameDifficulty gameDifficulty)
            => SaltSource.CreateFixed("test-fixed-salt");
    }
}
