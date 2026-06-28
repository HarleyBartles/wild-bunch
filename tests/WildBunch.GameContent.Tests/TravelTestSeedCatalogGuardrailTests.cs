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
        Assert.Equal(8, resolved.SelectedTownIds.Count);
    }

    [Fact]
    public void CanonicalMountedBoring_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.CanonicalMountedBoring);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Canonical, resolved.WorldVariant);
        Assert.Equal(8, resolved.SelectedTownIds.Count);
    }

    [Fact]
    public void CanonicalFootBoringLight_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.CanonicalFootBoringLight);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Canonical, resolved.WorldVariant);
        Assert.Equal(8, resolved.SelectedTownIds.Count);
    }

    [Fact]
    public void CanonicalMountedEasyStandard_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.CanonicalMountedEasyStandard);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Canonical, resolved.WorldVariant);
        Assert.Equal(8, resolved.SelectedTownIds.Count);
    }

    [Fact]
    public void CanonicalMountedHardStandard_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.CanonicalMountedHardStandard);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Canonical, resolved.WorldVariant);
        Assert.Equal(8, resolved.SelectedTownIds.Count);
    }

    [Fact]
    public void FrontierFootNormalFoe_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.FrontierFootNormalFoe);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Frontier, resolved.WorldVariant);
        Assert.Equal(8, resolved.SelectedTownIds.Count);
    }

    [Fact]
    public void FrontierMountedHardNpc_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.FrontierMountedHardNpc);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Frontier, resolved.WorldVariant);
        Assert.Equal(8, resolved.SelectedTownIds.Count);
    }

    [Fact]
    public void FrontierMountedNormalHighRisk_RoundTrips()
    {
        var seedCode = TravelTestSeedCatalog.ResolveSeedCode(TravelTestSeedCatalog.FrontierMountedNormalHighRisk);
        var resolved = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.Equal(SeedWorldVariant.Frontier, resolved.WorldVariant);
        Assert.Equal(8, resolved.SelectedTownIds.Count);
    }

    [Fact]
    public void CanonicalWorld_AlwaysStartsInPinecross()
    {
        // Starting town is NOT seed-owned. The safe default from StartingTownPolicy
        // is pinecross for all world variants.
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedStandard);
        Assert.Equal(new TownId("pinecross"), session.Player.CurrentTownId);
    }

    [Fact]
    public void CanonicalWorld_HasLowBadlandsNoneRoute_FromPinecross()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedBoring);
        var trail = TravelTestSeedCatalog.FindRouteFromCurrentTown(
            session, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None);
        Assert.NotNull(trail);
        Assert.Equal("trail-pine-hardpan", trail.Id.Value);
    }

    [Fact]
    public void CanonicalWorld_HasLowOpenRangeNoneRoute_FromPinecross()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedEasyStandard);
        var trail = TravelTestSeedCatalog.FindRouteFromCurrentTown(
            session, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None);
        Assert.NotNull(trail);
        Assert.Equal("trail-pine-openpass", trail.Id.Value);
    }

    [Fact]
    public void FrontierWorld_HasModerateHillsSpringRoute()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.FrontierFootNormalFoe);
        // Frontier variant: pinecross->holloway is Moderate/Hills/Spring
        var trail = session.World.Trails.FirstOrDefault(t =>
            t.Risk == TrailRisk.Moderate && t.Terrain == TrailTerrain.Hills && t.WaterFeature == WaterFeature.Spring);
        Assert.NotNull(trail);
        Assert.Equal("trail-pine-hollow", trail!.Id.Value);
    }

    [Fact]
    public void FrontierWorld_HasHighBadlandsNoneRoute()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.FrontierMountedNormalHighRisk);
        // Frontier variant: redmesa->dryfork is High/Badlands/None
        var trail = session.World.Trails.FirstOrDefault(t =>
            t.Risk == TrailRisk.High && t.Terrain == TrailTerrain.Badlands && t.WaterFeature == WaterFeature.None);
        Assert.NotNull(trail);
        Assert.Equal("trail-red-dry", trail!.Id.Value);
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
