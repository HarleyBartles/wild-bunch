using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Guardrail tests for <see cref="TravelTestSeedCatalog"/>.
/// Each test round-trips a seed world through CreateRepresentativeSeedCode -> Resolve
/// and verifies the codec produces the expected semantics. If the codec evolves and a
/// seed world no longer resolves to the expected world state, these tests fail and tell
/// you exactly which catalog entry is stale.
/// </summary>
public sealed class TravelTestSeedCatalogGuardrailTests
{
    [Fact]
    public void CanonicalMountedStandard_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.CanonicalMountedStandard);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Canonical, resolved.WorldVariant);
        Assert.Equal(8, resolved.TownCount);
        Assert.Equal(8, resolved.SelectedTownIds.Count);
    }

    [Fact]
    public void CanonicalMountedBoring_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.CanonicalMountedBoring);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Canonical, resolved.WorldVariant);
        Assert.Equal(8, resolved.TownCount);
    }

    [Fact]
    public void CanonicalFootBoringLight_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.CanonicalFootBoringLight);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Canonical, resolved.WorldVariant);
        Assert.Equal(8, resolved.TownCount);
    }

    [Fact]
    public void CanonicalMountedEasyStandard_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.CanonicalMountedEasyStandard);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Canonical, resolved.WorldVariant);
        Assert.Equal(8, resolved.TownCount);
    }

    [Fact]
    public void CanonicalMountedHardStandard_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.CanonicalMountedHardStandard);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Canonical, resolved.WorldVariant);
        Assert.Equal(8, resolved.TownCount);
    }

    [Fact]
    public void FrontierFootNormalFoe_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.FrontierFootNormalFoe);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Frontier, resolved.WorldVariant);
        Assert.Equal(8, resolved.TownCount);
    }

    [Fact]
    public void FrontierMountedHardNpc_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.FrontierMountedHardNpc);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Frontier, resolved.WorldVariant);
        Assert.Equal(8, resolved.TownCount);
    }

    [Fact]
    public void FrontierMountedNormalHighRisk_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.FrontierMountedNormalHighRisk);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Frontier, resolved.WorldVariant);
        Assert.Equal(8, resolved.TownCount);
    }

    [Fact]
    public void CanonicalWorld_StartsInFirstTown()
    {
        // Starting town is NOT seed-owned. The safe default from StartingTownPolicy
        // is the first town in the world (slot 0).
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedStandard);
        var firstTown = session.World.Towns.First().Id;
        Assert.Equal(firstTown, session.Player.CurrentTownId);
    }

    [Fact]
    public void CanonicalWorld_HasModerateBadlandsNoneRoute()
    {
        // Slot 0→6 is Moderate/Badlands/None (count >= 7). Start in slot 0's town.
        var world = SeedWorldBuilder.CreateCanonicalWorld();
        var startTown = TravelTestSeedCatalog.FindTownWithRoute(
            world, TrailRisk.Moderate, TrailTerrain.Badlands, WaterFeature.None);
        Assert.NotNull(startTown);

        var session = TravelTestSeedCatalog.CreateSession(
            TravelTestSeedCatalog.CanonicalMountedBoring, startTown!.Value.Value);
        var trail = TravelTestSeedCatalog.FindRouteFromCurrentTown(
            session, TrailRisk.Moderate, TrailTerrain.Badlands, WaterFeature.None);
        Assert.NotNull(trail);
    }

    [Fact]
    public void CanonicalWorld_HasLowOpenRangeCreekRoute()
    {
        // Slot 0→1 is Low/OpenRange/Creek in Canonical variant. Start in slot 0's town.
        var world = SeedWorldBuilder.CreateCanonicalWorld();
        var startTown = TravelTestSeedCatalog.FindTownWithRoute(
            world, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek);
        Assert.NotNull(startTown);

        var session = TravelTestSeedCatalog.CreateSession(
            TravelTestSeedCatalog.CanonicalMountedEasyStandard, startTown!.Value.Value);
        var trail = TravelTestSeedCatalog.FindRouteFromCurrentTown(
            session, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek);
        Assert.NotNull(trail);
    }

    [Fact]
    public void FrontierWorld_HasModerateHillsSpringRoute()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.FrontierFootNormalFoe);
        // Frontier variant: slot 0->2 trail is Moderate/Hills/Spring
        var trail = session.World.Trails.FirstOrDefault(t =>
            t.Risk == TrailRisk.Moderate && t.Terrain == TrailTerrain.Hills && t.WaterFeature == WaterFeature.Spring);
        Assert.NotNull(trail);
    }

    [Fact]
    public void FrontierWorld_HasHighBadlandsNoneRoute()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.FrontierMountedNormalHighRisk);
        // Frontier variant: slot 0->3 trail is High/Badlands/None
        var trail = session.World.Trails.FirstOrDefault(t =>
            t.Risk == TrailRisk.High && t.Terrain == TrailTerrain.Badlands && t.WaterFeature == WaterFeature.None);
        Assert.NotNull(trail);
    }

    [Fact]
    public void AllGangMembersAreCulpritEligible()
    {
        // The culprit is always a gang member, and any gang member can be the culprit.
        // This guardrail ensures no gang candidate is marked IsTrueCulpritEligible: false.
        var roster = CaseCharacterRoster.GangCandidatePool;
        Assert.All(roster, candidate => Assert.True(candidate.IsTrueCulpritEligible,
            $"Gang member '{candidate.Key}' is marked IsTrueCulpritEligible: false. " +
            "All gang members must be culprit-eligible. See AGENTS.md."));
    }
}
