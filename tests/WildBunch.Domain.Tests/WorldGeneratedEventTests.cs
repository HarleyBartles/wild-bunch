using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using SaltSource = WildBunch.Domain.Game.SaltSource;
using Xunit;

namespace WildBunch.Domain.Tests;

public sealed class WorldGeneratedEventTests
{
    [Fact]
    public void WorldGenerated_CarriesWorldSnapshotThatReconstructsToIdenticalWorld()
    {
        var town = new Town(new TownId("t1"), "Test Town", TownServices.Telegraph, TownProsperity.Boomtown, MapX: 100, MapY: 200, IsOutlier: false);
        var trail = new Trail(new TrailId("trail-0-1"), new TownId("t1"), new TownId("t2"), TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m);
        var world = new DomainWorld(new[] { town }, new[] { trail });

        var snapshot = WorldSnapshot.FromDomain(world);
        var caseFileSnapshot = new WildBunch.Domain.Cases.CaseFileSnapshot(
            Array.Empty<WildBunch.Domain.Cases.SuspectSnapshot>(),
            "placeholder",
            WildBunch.Domain.Cases.CaseOpeningLead.Create("placeholder"),
            Array.Empty<WildBunch.Domain.Cases.ClueSnapshot>(),
            Array.Empty<WildBunch.Domain.Cases.ClueSnapshot>(),
            null,
            Array.Empty<string>(),
            0,
            0,
            Array.Empty<WildBunch.Domain.Cases.WarrantSnapshot>(),
            Array.Empty<WildBunch.Domain.Cases.WarrantSnapshot>(),
            Array.Empty<WildBunch.Domain.Cases.SuspectTurfAssignmentSnapshot>(),
            Array.Empty<WildBunch.Domain.Cases.WantedSuspectConfrontationSnapshot>(),
            Array.Empty<WildBunch.Domain.Cases.SheriffTurnInSettlementSnapshot>());
        var evt = new WorldGenerated
        {
            SeedCode = "test-seed",
            SaltSource = SaltSource.CreateFixed("test-salt"),
            GameEntropy = GameEntropy.Boring,
            World = snapshot,
            CaseFile = caseFileSnapshot
        };

        var reconstructed = evt.World.ToDomain();

        Assert.Single(reconstructed.Towns);
        var reconstructedTown = reconstructed.Towns.First();
        Assert.Equal("t1", reconstructedTown.Id.Value);
        Assert.Equal("Test Town", reconstructedTown.Name);
        Assert.Equal(TownServices.Telegraph, reconstructedTown.Services);
        Assert.Equal(TownProsperity.Boomtown, reconstructedTown.Prosperity);
        Assert.Equal(100, reconstructedTown.MapX);
        Assert.Equal(200, reconstructedTown.MapY);
        Assert.False(reconstructedTown.IsOutlier);
    }

    [Fact]
    public void WorldGenerated_PreservesIsOutlierFlag()
    {
        var outlier = new Town(new TownId("t-outlier"), "Outlier Town", TownServices.None, TownProsperity.Poor, MapX: 500, MapY: 500, IsOutlier: true);
        var world = new DomainWorld(new[] { outlier }, Array.Empty<Trail>());

        var snapshot = WorldSnapshot.FromDomain(world);
        var reconstructed = snapshot.ToDomain();

        Assert.True(reconstructed.Towns.First().IsOutlier);
    }
}
