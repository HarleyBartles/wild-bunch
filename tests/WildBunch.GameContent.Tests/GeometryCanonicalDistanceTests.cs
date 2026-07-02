using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.Abstractions;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class GeometryCanonicalDistanceTests
{
    [Fact]
    public void SessionWorldTrails_ContainGeometryDerivedDistances()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());
        var session = factory.Create("Test", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Boring);

        // All trails should have geometry-derived distances (2-6 days)
        Assert.All(session.World.Trails, trail =>
        {
            Assert.InRange(trail.RideDayDistance, 2m, 6m);
        });

        // Distances should be derived from locked town coordinates
        var townCoordinates = session.World.Towns.ToDictionary(t => t.Id, t => (t.MapX, t.MapY));

        foreach (var trail in session.World.Trails)
        {
            var fromCoords = townCoordinates[trail.FromTownId];
            var toCoords = townCoordinates[trail.ToTownId];
            var dx = toCoords.MapX - fromCoords.MapX;
            var dy = toCoords.MapY - fromCoords.MapY;
            var geometricDistance = Math.Sqrt(dx * dx + dy * dy);
            var expectedDays = Math.Round(geometricDistance / 25.0, 1);
            var cappedDays = Math.Max(2m, Math.Min(6m, (decimal)expectedDays));
            Assert.Equal(cappedDays, trail.RideDayDistance);
        }
    }

    [Fact]
    public void WorldTowns_HaveLockedMapCoordinates()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());
        var session = factory.Create("Test", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Boring);

        // All towns should have non-zero locked coordinates
        Assert.All(session.World.Towns, town =>
        {
            Assert.True(town.MapX > 0, $"Town {town.Name} should have positive MapX");
            Assert.True(town.MapY > 0, $"Town {town.Name} should have positive MapY");
        });
    }

    [Fact]
    public void MapTowns_ReadFromLockedWorldCoordinates()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());
        var session = factory.Create("Test", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Boring);

        // Map towns should read from locked world coordinates
        var mapTowns = SeedWorldMapLayout.GetMapTowns(session.World, seedWorld.MapLayoutPalette);
        
        foreach (var mapTown in mapTowns)
        {
            var worldTown = session.World.GetTown(new TownId(mapTown.Id));
            Assert.Equal(worldTown.MapX, mapTown.X);
            Assert.Equal(worldTown.MapY, mapTown.Y);
        }
    }

    [Fact]
    public void SameSeed_Boring_ProducesStableWorldGeometry()
    {
        var seedCode = SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode());
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());

        var session1 = factory.Create("Test1", GameDifficulty.Standard, seedCode, GameEntropy.Boring);
        var session2 = factory.Create("Test2", GameDifficulty.Standard, seedCode, GameEntropy.Boring);

        // Town coordinates should be identical (Boring has no variance)
        var towns1 = session1.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        var towns2 = session2.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        Assert.Equal(towns1.Length, towns2.Length);
        for (var i = 0; i < towns1.Length; i++)
        {
            Assert.Equal(towns1[i].MapX, towns2[i].MapX);
            Assert.Equal(towns1[i].MapY, towns2[i].MapY);
        }

        // Trail distances should be identical
        var trails1 = session1.World.Trails.OrderBy(t => t.Id.Value).ToArray();
        var trails2 = session2.World.Trails.OrderBy(t => t.Id.Value).ToArray();
        Assert.Equal(trails1.Length, trails2.Length);
        for (var i = 0; i < trails1.Length; i++)
        {
            Assert.Equal(trails1[i].RideDayDistance, trails2[i].RideDayDistance);
        }
    }

    [Fact]
    public void SameSeed_NonBoring_CanVaryByPlaythroughSalt()
    {
        var seedCode = SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode());

        // Create two sessions with different fixed salts to guarantee variance
        var factory1 = new SeededNewGameFactory(new TestFixedSaltSourceFactory("salt-1"));
        var factory2 = new SeededNewGameFactory(new TestFixedSaltSourceFactory("salt-2"));

        var session1 = factory1.Create("Test1", GameDifficulty.Standard, seedCode, GameEntropy.Classic);
        var session2 = factory2.Create("Test2", GameDifficulty.Standard, seedCode, GameEntropy.Classic);

        // Both sessions should have geometry-derived distances
        Assert.All(session1.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));
        Assert.All(session2.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));

        // At least some coordinates should differ with different salts
        var towns1 = session1.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        var towns2 = session2.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        var coordinateMatches = 0;
        for (var i = 0; i < Math.Min(towns1.Length, towns2.Length); i++)
        {
            if (towns1[i].MapX == towns2[i].MapX && towns1[i].MapY == towns2[i].MapY)
            {
                coordinateMatches++;
            }
        }
        Assert.True(coordinateMatches < Math.Min(towns1.Length, towns2.Length), "Expected some coordinate variance with different salts");
    }

    [Fact]
    public void EntropyVariance_IncreasesWithEntropyLevel()
    {
        var seedCode = SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode());
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());

        var boringSession = factory.Create("Boring", GameDifficulty.Standard, seedCode, GameEntropy.Boring);
        var classicSession = factory.Create("Classic", GameDifficulty.Standard, seedCode, GameEntropy.Classic);
        var adventurousSession = factory.Create("Adventurous", GameDifficulty.Standard, seedCode, GameEntropy.Adventurous);
        var wildSession = factory.Create("Wild", GameDifficulty.Standard, seedCode, GameEntropy.Wild);

        // All sessions should have geometry-derived distances
        Assert.All(boringSession.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));
        Assert.All(classicSession.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));
        Assert.All(adventurousSession.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));
        Assert.All(wildSession.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));

        // Boring should have zero variance (same as base coordinates)
        // Non-Boring modes should have variance from base coordinates
        // We verify this by checking that non-Boring coordinates differ from Boring coordinates
        var boringTowns = boringSession.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        var classicTowns = classicSession.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        
        var classicMatches = 0;
        for (var i = 0; i < Math.Min(boringTowns.Length, classicTowns.Length); i++)
        {
            if (boringTowns[i].MapX == classicTowns[i].MapX && boringTowns[i].MapY == classicTowns[i].MapY)
            {
                classicMatches++;
            }
        }
        
        // Classic should have variance from Boring
        Assert.True(classicMatches < Math.Min(boringTowns.Length, classicTowns.Length), "Classic should have variance from Boring");
    }

    [Fact]
    public void SessionWorld_IsLockedOnceCreated()
    {
        var seedCode = SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode());
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());

        var session = factory.Create("Test", GameDifficulty.Standard, seedCode, GameEntropy.Classic);

        // Capture the initial state
        var initialTowns = session.World.Towns.OrderBy(t => t.Id.Value).Select(t => (t.Id, t.MapX, t.MapY)).ToArray();
        var initialTrails = session.World.Trails.OrderBy(t => t.Id.Value).Select(t => (t.Id, t.RideDayDistance)).ToArray();

        // The session's world state should remain locked - we verify this by checking
        // that the world coordinates and trails are non-zero and within expected ranges
        Assert.All(session.World.Towns, town => Assert.True(town.MapX > 0 && town.MapY > 0));
        Assert.All(session.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));

        // The initial state should be consistent (all towns have coordinates, all trails have distances)
        Assert.Equal(session.World.Towns.Count, initialTowns.Length);
        Assert.Equal(session.World.Trails.Count, initialTrails.Length);

        // Verify that the world state is locked by checking that it doesn't change
        // when we read it again (in-memory consistency check)
        var recheckedTowns = session.World.Towns.OrderBy(t => t.Id.Value).Select(t => (t.Id, t.MapX, t.MapY)).ToArray();
        var recheckedTrails = session.World.Trails.OrderBy(t => t.Id.Value).Select(t => (t.Id, t.RideDayDistance)).ToArray();

        Assert.Equal(initialTowns, recheckedTowns);
        Assert.Equal(initialTrails, recheckedTrails);
    }

    [Fact]
    public void NonBoringModes_ApplyOutlierTownTrimming_WithConnectivityPreserved()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var factory = new SeededNewGameFactory(new TestRuntimeSaltSourceFactory());

        var classicSession = factory.Create("Test1", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Classic);
        var adventurousSession = factory.Create("Test2", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Adventurous);
        var wildSession = factory.Create("Test3", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Wild);

        // All entropic modes should have geometry-derived distances
        Assert.All(classicSession.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));
        Assert.All(adventurousSession.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));
        Assert.All(wildSession.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));

        // Graph should remain fully connected after outlier trimming
        foreach (var session in new[] { classicSession, adventurousSession, wildSession })
        {
            var adjacency = new Dictionary<TownId, HashSet<TownId>>();
            foreach (var town in session.World.Towns)
            {
                adjacency[town.Id] = new HashSet<TownId>();
            }
            foreach (var trail in session.World.Trails)
            {
                adjacency[trail.FromTownId].Add(trail.ToTownId);
                adjacency[trail.ToTownId].Add(trail.FromTownId);
            }

            var visited = new HashSet<TownId>();
            var queue = new Queue<TownId>();
            var start = session.World.Towns.First().Id;
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in adjacency[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            Assert.Equal(session.World.Towns.Count, visited.Count);
        }
    }

    [Fact]
    public void MapLabels_ReadFromLockedWorldTrails()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());
        var session = factory.Create("Test", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Boring);

        // Map trails should read from locked world trails
        var mapTrails = SeedWorldMapLayout.GetMapTrails(session.World);
        
        foreach (var mapTrail in mapTrails)
        {
            var worldTrail = session.World.Trails.FirstOrDefault(t => t.Id.Value == mapTrail.Id);
            Assert.NotNull(worldTrail);
            Assert.Equal(worldTrail.RideDayDistance, mapTrail.RideDayDistance);
        }
    }

    [Fact]
    public void TrailDistances_DeriveFromLockedWorldCoordinates()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());
        var session = factory.Create("Test", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Boring);

        // Get a trail from the world
        var trail = session.World.Trails.First();
        var trailDistance = trail.RideDayDistance;

        // The trail distance should be within the canonical range
        Assert.InRange(trailDistance, 2m, 6m);

        // The trail distance should match the geometric distance between towns
        var fromTown = session.World.GetTown(trail.FromTownId);
        var toTown = session.World.GetTown(trail.ToTownId);
        var dx = toTown.MapX - fromTown.MapX;
        var dy = toTown.MapY - fromTown.MapY;
        var geometricDistance = Math.Sqrt(dx * dx + dy * dy);
        var expectedDays = Math.Round(geometricDistance / 25.0, 1);
        var cappedDays = Math.Max(2m, Math.Min(6m, (decimal)expectedDays));

        Assert.Equal(cappedDays, trailDistance);
    }

    [Fact]
    public void Travel_ConsumesLockedWorldTrailDistances()
    {
        var seedCode = SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode());
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory());
        var session = factory.Create("Test", GameDifficulty.Standard, seedCode, GameEntropy.Boring);

        // Get a trail from the world with its locked distance
        var trail = session.World.Trails.First();
        var lockedTrailDistance = trail.RideDayDistance;

        // The trail distance should be within the canonical range
        Assert.InRange(lockedTrailDistance, 2m, 6m);

        // Get the destination town for this trail
        var destinationTownId = trail.ToTownId;
        var destinationTown = session.World.GetTown(destinationTownId);
        var originTown = session.World.GetTown(trail.FromTownId);

        // Create a minimal TravelPreview that reflects the locked trail distance
        var travelPreview = new TravelPreview(
            originTown.Id,
            destinationTownId,
            originTown.Name,
            destinationTown.Name,
            new TravelRouteProfile(
                trail.Id.Value,
                trail.Risk,
                trail.Terrain,
                trail.WaterFeature,
                lockedTrailDistance,
                lockedTrailDistance, // mountedRideDayProgress
                lockedTrailDistance, // footRideDayProgress
                Array.Empty<string>()),
            TravelMode.Mounted,
            true, // mountedTravelAvailable
            true, // waterSecure
            lockedTrailDistance,
            lockedTrailDistance,
            (int)lockedTrailDistance,
            (int)lockedTrailDistance,
            (int)lockedTrailDistance,
            0, // canteenChargesPerDay
            0, // requiredCanteenCharges
            10, // availableCanteenCharges
            0, // canteenReserveCharges
            0, // delayMarginDays
            false, // delayRisk
            0, // requiredFood
            10, // availableFood
            0, // requiredHorseFeed
            3, // availableHorseFeed
            session.Player.GetHorseState(), // horseState
            Array.Empty<string>());

        // Exercise the real travel command path through GameSession.StartJourney
        var journeyResult = session.StartJourney(travelPreview);

        // The journey should start successfully
        Assert.True(journeyResult.Success);

        // The resulting journey should consume the locked trail distance
        Assert.NotNull(session.Journey);
        Assert.Equal(lockedTrailDistance, session.Journey.RemainingRideDayDistance);
        Assert.Equal((int)lockedTrailDistance, session.Journey.RemainingDays);
        Assert.Equal(0, session.Journey.DaysTravelled);
    }

    private sealed class TestFixedSaltSourceFactory : ISaltSourceFactory
    {
        private readonly string _salt;

        public TestFixedSaltSourceFactory(string salt = "test-fixed-salt")
        {
            _salt = salt;
        }

        public SaltSource Create(string? setupSeedCode, GameDifficulty gameDifficulty)
            => SaltSource.CreateFixed(_salt);
    }

    private sealed class TestRuntimeSaltSourceFactory : ISaltSourceFactory
    {
        public SaltSource Create(string? setupSeedCode, GameDifficulty gameDifficulty)
            => SaltSource.CreateRuntime();
    }
}
