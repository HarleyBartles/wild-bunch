using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

[Obsolete("TrailDistanceSalter is no longer used in the setup pipeline - geometry-derived distances are now canonical")]
public sealed class TrailDistanceSalterTests
{
    [Fact]
    public void BoringEntropyPreservesBaselineDistances()
    {
        var world = BuildTestWorld();
        var entropy = EntropyPolicy.For(GameEntropy.Boring);
        var salt = SaltSource.CreateFixed("any-salt");

        var salted = TrailDistanceSalter.Apply(world, entropy, salt);

        foreach (var (original, saltedTrail) in world.Trails.Zip(salted.Trails))
        {
            Assert.Equal(original.RideDayDistance, saltedTrail.RideDayDistance);
        }
    }

    [Fact]
    public void ClassicEntropyStaysWithinPlusMinusOneOfBaseline()
    {
        var world = BuildTestWorld();
        var entropy = EntropyPolicy.For(GameEntropy.Classic);
        var salt = SaltSource.CreateFixed("classic-salt");

        var salted = TrailDistanceSalter.Apply(world, entropy, salt);

        foreach (var (original, saltedTrail) in world.Trails.Zip(salted.Trails))
        {
            var swing = saltedTrail.RideDayDistance - original.RideDayDistance;
            Assert.InRange(swing, -1m, 1m);
        }
    }

    [Fact]
    public void AdventurousEntropyStaysWithinPlusMinusTwoOfBaseline()
    {
        var world = BuildTestWorld();
        var entropy = EntropyPolicy.For(GameEntropy.Adventurous);
        var salt = SaltSource.CreateFixed("adventurous-salt");

        var salted = TrailDistanceSalter.Apply(world, entropy, salt);

        foreach (var (original, saltedTrail) in world.Trails.Zip(salted.Trails))
        {
            var swing = saltedTrail.RideDayDistance - original.RideDayDistance;
            Assert.InRange(swing, -2m, 2m);
        }
    }

    [Fact]
    public void WildEntropyStaysWithinPlusMinusThreeOfBaseline()
    {
        var world = BuildTestWorld();
        var entropy = EntropyPolicy.For(GameEntropy.Wild);
        var salt = SaltSource.CreateFixed("wild-salt");

        var salted = TrailDistanceSalter.Apply(world, entropy, salt);

        foreach (var (original, saltedTrail) in world.Trails.Zip(salted.Trails))
        {
            var swing = saltedTrail.RideDayDistance - original.RideDayDistance;
            Assert.InRange(swing, -3m, 3m);
        }
    }

    [Fact]
    public void SaltedDistanceNeverDropsBelowOne()
    {
        // Build a world with baseline distance 1 so any negative swing would hit the floor.
        var world = BuildTestWorld(baselineDistance: 1m);
        var entropy = EntropyPolicy.For(GameEntropy.Wild);
        var salt = SaltSource.CreateFixed("floor-test-salt");

        var salted = TrailDistanceSalter.Apply(world, entropy, salt);

        foreach (var trail in salted.Trails)
        {
            Assert.True(trail.RideDayDistance >= 1m);
        }
    }

    [Fact]
    public void SameSaltProducesSameDistances()
    {
        var world = BuildTestWorld();
        var entropy = EntropyPolicy.For(GameEntropy.Adventurous);
        var salt = SaltSource.CreateFixed("reproducible-salt");

        var saltedA = TrailDistanceSalter.Apply(world, entropy, salt);
        var saltedB = TrailDistanceSalter.Apply(world, entropy, salt);

        foreach (var (a, b) in saltedA.Trails.Zip(saltedB.Trails))
        {
            Assert.Equal(a.RideDayDistance, b.RideDayDistance);
        }
    }

    [Fact]
    public void DifferentSaltsCanProduceDifferentDistances()
    {
        var world = BuildTestWorld();
        var entropy = EntropyPolicy.For(GameEntropy.Adventurous);

        var saltedA = TrailDistanceSalter.Apply(world, entropy, SaltSource.CreateFixed("salt-a"));
        var saltedB = TrailDistanceSalter.Apply(world, entropy, SaltSource.CreateFixed("salt-b"));

        // With 14 trails and ±2 swing, it's virtually impossible for two different salts
        // to produce identical distances on every trail.
        var distancesA = string.Join(",", saltedA.Trails.Select(t => t.RideDayDistance));
        var distancesB = string.Join(",", saltedB.Trails.Select(t => t.RideDayDistance));
        Assert.NotEqual(distancesA, distancesB);
    }

    [Fact]
    public void TopologyIsPreserved()
    {
        var world = BuildTestWorld();
        var entropy = EntropyPolicy.For(GameEntropy.Wild);
        var salt = SaltSource.CreateFixed("topology-salt");

        var salted = TrailDistanceSalter.Apply(world, entropy, salt);

        Assert.Equal(world.Towns.Count, salted.Towns.Count);
        Assert.Equal(world.Trails.Count, salted.Trails.Count);

        foreach (var (original, saltedTrail) in world.Trails.Zip(salted.Trails))
        {
            Assert.Equal(original.Id, saltedTrail.Id);
            Assert.Equal(original.FromTownId, saltedTrail.FromTownId);
            Assert.Equal(original.ToTownId, saltedTrail.ToTownId);
            Assert.Equal(original.Risk, saltedTrail.Risk);
            Assert.Equal(original.Terrain, saltedTrail.Terrain);
            Assert.Equal(original.WaterFeature, saltedTrail.WaterFeature);
        }
    }

    /// <summary>
    /// Builds a small test world with 3 towns and 3 trails at a given baseline distance.
    /// </summary>
    private static World BuildTestWorld(decimal baselineDistance = 4m)
    {
        var towns = new[]
        {
            new Town(new TownId("town-a"), "Town A", TownServices.None, TownProsperity.Prosperous),
            new Town(new TownId("town-b"), "Town B", TownServices.None, TownProsperity.Prosperous),
            new Town(new TownId("town-c"), "Town C", TownServices.None, TownProsperity.Prosperous),
        };

        var trails = new[]
        {
            new Trail(new TrailId("trail-ab"), new TownId("town-a"), new TownId("town-b"), TrailRisk.Low, rideDayDistance: baselineDistance),
            new Trail(new TrailId("trail-bc"), new TownId("town-b"), new TownId("town-c"), TrailRisk.Moderate, rideDayDistance: baselineDistance),
            new Trail(new TrailId("trail-ac"), new TownId("town-a"), new TownId("town-c"), TrailRisk.Low, rideDayDistance: baselineDistance),
        };

        return new World(towns, trails);
    }
}
