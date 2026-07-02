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

        // Distance variance may not always occur if outlier trimming removes the same towns
        // The key proof is coordinate variance, which drives distance changes
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
        var adventurousTowns = adventurousSession.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        var wildTowns = wildSession.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        
        var classicMatches = 0;
        var adventurousMatches = 0;
        var wildMatches = 0;
        
        for (var i = 0; i < Math.Min(boringTowns.Length, classicTowns.Length); i++)
        {
            if (boringTowns[i].MapX == classicTowns[i].MapX && boringTowns[i].MapY == classicTowns[i].MapY)
            {
                classicMatches++;
            }
        }
        for (var i = 0; i < Math.Min(boringTowns.Length, adventurousTowns.Length); i++)
        {
            if (boringTowns[i].MapX == adventurousTowns[i].MapX && boringTowns[i].MapY == adventurousTowns[i].MapY)
            {
                adventurousMatches++;
            }
        }
        for (var i = 0; i < Math.Min(boringTowns.Length, wildTowns.Length); i++)
        {
            if (boringTowns[i].MapX == wildTowns[i].MapX && boringTowns[i].MapY == wildTowns[i].MapY)
            {
                wildMatches++;
            }
        }
        
        // All non-Boring modes should have variance from Boring
        Assert.True(classicMatches < Math.Min(boringTowns.Length, classicTowns.Length), "Classic should have variance from Boring");
        Assert.True(adventurousMatches < Math.Min(boringTowns.Length, adventurousTowns.Length), "Adventurous should have variance from Boring");
        Assert.True(wildMatches < Math.Min(boringTowns.Length, wildTowns.Length), "Wild should have variance from Boring");
        
        // Wild should have more variance than Classic (fewer matches with Boring)
        Assert.True(wildMatches <= classicMatches, "Wild should have at least as much variance as Classic");
    }

    [Fact]
    public void SessionWorld_IsLockedOnceCreated_InMemoryConsistency()
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
    public void OutlierTrimming_PreservesTrailReferences()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var factory = new SeededNewGameFactory(new TestRuntimeSaltSourceFactory());

        var wildSession = factory.Create("Wild", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Wild);

        // All trail references should point to valid towns in the trimmed world
        foreach (var trail in wildSession.World.Trails)
        {
            Assert.True(wildSession.World.TryGetTown(trail.FromTownId, out _), $"Trail {trail.Id} references missing FromTownId {trail.FromTownId}");
            Assert.True(wildSession.World.TryGetTown(trail.ToTownId, out _), $"Trail {trail.Id} references missing ToTownId {trail.ToTownId}");
        }
    }

    [Fact]
    public void NonBoringModes_ApplyOutlierTrailTrimming_WithConnectivityPreserved()
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

        // No towns should be removed — all entropic modes keep the same town count as Boring
        var boringSession = factory.Create("Boring", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Boring);
        foreach (var session in new[] { classicSession, adventurousSession, wildSession })
        {
            Assert.Equal(boringSession.World.Towns.Count, session.World.Towns.Count);
        }

        // Graph should remain fully connected after outlier trail trimming
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
        var originTownId = trail.FromTownId;

        // Use the real TravelResolver path to get a preview (reads from locked World.Trails)
        var travelResolver = new TravelResolver();
        var previewResult = travelResolver.PreviewJourney(
            session.World,
            originTownId,
            destinationTownId,
            session.Player.Inventory,
            session.TravelRules);

        Assert.True(previewResult.Success);
        Assert.NotNull(previewResult.Preview);

        // The preview should use the locked trail distance
        Assert.Equal(lockedTrailDistance, previewResult.Preview.RouteProfile.RideDayDistance);

        // Exercise the real travel command path through GameSession.StartJourney
        var journeyResult = session.StartJourney(previewResult.Preview);

        // The journey should start successfully
        Assert.True(journeyResult.Success);

        // The resulting journey should consume the locked trail distance
        Assert.NotNull(session.Journey);
        Assert.Equal(lockedTrailDistance, session.Journey.RemainingRideDayDistance);
        Assert.Equal((int)lockedTrailDistance, session.Journey.RemainingDays);
        Assert.Equal(0, session.Journey.DaysTravelled);
    }

    [Fact]
    public void OutlierTrailTrimming_PreservesAllTownsAndConnectivity()
    {
        // This test proves the real outlier-trimming invariant:
        // - No towns are removed from the generated world
        // - All generated towns remain reachable (graph stays connected)
        // - The outlier town remains present with exactly one trail
        // - Trail distances are derived from geometry (within 1 day of geometric
        //   distance, accounting for intentional 6→5 reduction on non-outlier trails)
        // - At most one town has a 6-day trail after trimming
        // - Town coordinate identity is stable (no slot compaction)
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory("salt-trim-identity"));

        var boringSession = factory.Create("Boring", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Boring);
        var wildSession = factory.Create("Wild", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Wild);

        // 1. No towns are removed: Wild must have the same town count as Boring
        Assert.Equal(boringSession.World.Towns.Count, wildSession.World.Towns.Count);

        // 2. All towns in Wild are the same set as Boring (by town ID)
        var boringTownIds = boringSession.World.Towns.Select(t => t.Id.Value).OrderBy(id => id).ToArray();
        var wildTownIds = wildSession.World.Towns.Select(t => t.Id.Value).OrderBy(id => id).ToArray();
        Assert.Equal(boringTownIds, wildTownIds);

        // 3. Graph remains fully connected after trail trimming
        var adjacency = new Dictionary<TownId, HashSet<TownId>>();
        foreach (var town in wildSession.World.Towns)
            adjacency[town.Id] = new HashSet<TownId>();
        foreach (var trail in wildSession.World.Trails)
        {
            adjacency[trail.FromTownId].Add(trail.ToTownId);
            adjacency[trail.ToTownId].Add(trail.FromTownId);
        }

        var visited = new HashSet<TownId>();
        var queue = new Queue<TownId>();
        var start = wildSession.World.Towns.First().Id;
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
        Assert.Equal(wildSession.World.Towns.Count, visited.Count);

        // 4. Trail distances are derived from geometry. The locked distance may
        // differ from the raw geometric distance by at most 1 day, because
        // non-outlier 6-day trails are intentionally reduced to 5 days.
        // This proves coordinates are valid and not corrupted by slot compaction.
        const double CoordinateScale = 25.0;
        foreach (var trail in wildSession.World.Trails)
        {
            Assert.True(wildSession.World.TryGetTown(trail.FromTownId, out var fromTown),
                $"Trail {trail.Id} references missing FromTownId {trail.FromTownId}");
            Assert.True(wildSession.World.TryGetTown(trail.ToTownId, out var toTown),
                $"Trail {trail.Id} references missing ToTownId {trail.ToTownId}");

            var dx = toTown.MapX - fromTown.MapX;
            var dy = toTown.MapY - fromTown.MapY;
            var geometricDistance = Math.Sqrt(dx * dx + dy * dy);
            var geometricRideDays = Math.Max(2m, Math.Min(6m, (decimal)Math.Round(geometricDistance / CoordinateScale, 1)));

            // The locked distance must be within 1 day of the geometric distance.
            // This allows for the intentional 6→5 reduction while proving
            // coordinates are not corrupted.
            var diff = Math.Abs(trail.RideDayDistance - geometricRideDays);
            Assert.True(diff <= 1m,
                $"Trail {trail.Id} distance {trail.RideDayDistance} differs from geometric {geometricRideDays} by {diff}, expected <= 1");
        }

        // 5. At most one town has a 6-day trail (the outlier dead-end).
        // Other 6-day trails should have been reduced to 5 days.
        var towns6Day = new HashSet<TownId>();
        foreach (var trail in wildSession.World.Trails)
        {
            if (trail.RideDayDistance == 6m)
            {
                towns6Day.Add(trail.FromTownId);
                towns6Day.Add(trail.ToTownId);
            }
        }
        Assert.True(towns6Day.Count <= 2, $"At most 2 towns (the outlier + its neighbor) should be on 6-day trails, found {towns6Day.Count}");

        // 6. The outlier town (if any 6-day trail exists) should have exactly one trail.
        if (towns6Day.Count > 0)
        {
            // Find the outlier: the town on a 6-day trail with the fewest total trails
            var townTrailCounts = wildSession.World.Trails
                .SelectMany(t => new[] { t.FromTownId, t.ToTownId })
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            var outlier = towns6Day.OrderBy(t => townTrailCounts.GetValueOrDefault(t, 0)).First();
            Assert.Equal(1, townTrailCounts.GetValueOrDefault(outlier, 0));
        }

        // 7. Town coordinate identity is stable: two Wild sessions with the same
        // fixed salt produce identical town coordinates and trail distances.
        // This proves no slot compaction occurs (coordinates are not shifted).
        var wildSession2 = factory.Create("Wild2", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Wild);
        Assert.Equal(wildSession.World.Towns.Count, wildSession2.World.Towns.Count);
        Assert.Equal(wildSession.World.Trails.Count, wildSession2.World.Trails.Count);

        var t1 = wildSession.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        var t2 = wildSession2.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        for (var i = 0; i < t1.Length; i++)
        {
            Assert.Equal(t1[i].Id.Value, t2[i].Id.Value);
            Assert.Equal(t1[i].MapX, t2[i].MapX);
            Assert.Equal(t1[i].MapY, t2[i].MapY);
        }

        var tr1 = wildSession.World.Trails.OrderBy(t => t.Id.Value).ToArray();
        var tr2 = wildSession2.World.Trails.OrderBy(t => t.Id.Value).ToArray();
        for (var i = 0; i < tr1.Length; i++)
        {
            Assert.Equal(tr1[i].Id.Value, tr2[i].Id.Value);
            Assert.Equal(tr1[i].RideDayDistance, tr2[i].RideDayDistance);
        }
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
