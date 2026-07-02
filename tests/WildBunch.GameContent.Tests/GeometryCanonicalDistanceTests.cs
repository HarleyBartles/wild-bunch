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
    public void OutlierTrimming_MiddleSlotRemoval_PreservesCoordinateIdentity()
    {
        // This test catches the identity bug where trimming a middle town slot
        // causes later towns to receive the wrong coordinates. The invariant:
        // each trail's locked RideDayDistance must be consistent with the
        // geometric distance between its endpoint town coordinates.
        // If coordinates are misaligned after trimming, recomputing the
        // geometric distance from town (MapX, MapY) will not match the locked
        // trail distance.
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var factory = new SeededNewGameFactory(new TestFixedSaltSourceFactory("salt-trim-identity"));

        // Create a Wild session (with trimming) using a fixed salt
        var wildSession = factory.Create("Wild", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Wild);

        // For every trail in the Wild session, verify that the locked
        // RideDayDistance is geometrically consistent with the endpoint
        // coordinates. This catches the coordinate-shift bug: if a middle
        // slot was trimmed and coordinates were compacted incorrectly,
        // the recomputed geometric distance will not match the locked distance.
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
            var recomputedRideDays = Math.Round(geometricDistance / CoordinateScale, 1);
            var expectedDistance = Math.Max(2m, Math.Min(6m, (decimal)recomputedRideDays));

            // The locked trail distance must match the geometric distance
            // computed from the town coordinates. A mismatch proves the
            // coordinate-to-town mapping was corrupted by trimming.
            Assert.Equal(expectedDistance, trail.RideDayDistance);
        }

        // Additionally verify that two Wild sessions with the same fixed salt
        // produce identical town coordinates and trail distances. This proves
        // that trimming is deterministic and preserves coordinate identity
        // across repeated sessions with the same inputs.
        var wildSession2 = factory.Create("Wild2", GameDifficulty.Standard, SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode), GameEntropy.Wild);

        Assert.Equal(wildSession.World.Towns.Count, wildSession2.World.Towns.Count);
        var towns1 = wildSession.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        var towns2 = wildSession2.World.Towns.OrderBy(t => t.Id.Value).ToArray();
        for (var i = 0; i < towns1.Length; i++)
        {
            Assert.Equal(towns1[i].Id.Value, towns2[i].Id.Value);
            Assert.Equal(towns1[i].MapX, towns2[i].MapX);
            Assert.Equal(towns1[i].MapY, towns2[i].MapY);
        }

        var trails1 = wildSession.World.Trails.OrderBy(t => t.Id.Value).ToArray();
        var trails2 = wildSession2.World.Trails.OrderBy(t => t.Id.Value).ToArray();
        Assert.Equal(trails1.Length, trails2.Length);
        for (var i = 0; i < trails1.Length; i++)
        {
            Assert.Equal(trails1[i].Id.Value, trails2[i].Id.Value);
            Assert.Equal(trails1[i].RideDayDistance, trails2[i].RideDayDistance);
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
