using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Covers travel setup salt modes and runtime salt variance; generated difficulty
/// and entropy pressure behavior is owned by the Domain generator tests.
///
/// Entropy ladder (BUNCH-93):
///   Boring      — near-deterministic. SaltSourceMode.Fixed means no salt in the
///                 seed, so the same route/session inputs produce the same plan
///                 every time. No weight adjustment needed.
///   Classic     — standard gameplay. Runtime salt provides ordinary variance.
///   Adventurous — more swing than Classic (wider count spread, more lucky/unlucky).
///   Wild        — most volatile (biggest swings, most lucky/unlucky, least quiet).
///
/// Tests use the seed codec round-trip (SeedWorld -&gt; UUID via
/// CreateRepresentativeSeedCode) rather than stored UUIDs, so codec changes
/// don't break them. Tests establish fixed salt inputs at player genesis, while
/// the supported travel override isolates specific encounter scenarios.
///
/// See BUNCH-93.
/// </summary>
public sealed class TravelEntropyVarianceTests
{
    // --- Boring: near-deterministic via Fixed salt ---

    [Fact]
    public void BoringEntropy_UsesFixedSaltMode()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedBoring);

        Assert.Equal(SaltSourceMode.Fixed, session.SaltSource.Mode);
    }

    // --- Classic: runtime salt provides variance ---

    [Fact]
    public void ClassicEntropy_UsesRuntimeSaltMode()
    {
        var session = TravelTestSeedCatalog.CreateSessionWithRuntimeSalt(TravelTestSeedCatalog.CanonicalMountedStandard);

        Assert.Equal(SaltSourceMode.Runtime, session.SaltSource.Mode);
    }

    [Fact]
    public void ClassicEntropy_DifferentSessionsProduceDifferentSalt()
    {
        var session1 = TravelTestSeedCatalog.CreateSessionWithRuntimeSalt(TravelTestSeedCatalog.CanonicalMountedStandard);
        var session2 = TravelTestSeedCatalog.CreateSessionWithRuntimeSalt(TravelTestSeedCatalog.CanonicalMountedStandard);

        Assert.Equal(SaltSourceMode.Runtime, session1.SaltSource.Mode);
        Assert.NotEqual(session1.SaltSource.Salt, session2.SaltSource.Salt);
    }

    // --- Boring vs Classic: same seed, different salt mode ---

    [Fact]
    public void BoringAndClassic_SameSeedWorld_DifferentSaltMode()
    {
        // Both use the same canonical seed world, but Boring gets Fixed salt
        // and Classic gets Runtime salt. This is the core entropy distinction.
        var boringSession = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedBoring);
        var classicSession = TravelTestSeedCatalog.CreateSessionWithRuntimeSalt(TravelTestSeedCatalog.CanonicalMountedStandard);

        Assert.Equal(SaltSourceMode.Fixed, boringSession.SaltSource.Mode);
        Assert.Equal(SaltSourceMode.Runtime, classicSession.SaltSource.Mode);
    }

}
