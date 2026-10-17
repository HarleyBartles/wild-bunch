using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Covers the salt posture selected by Boring and Classic entropy during real game setup.
/// </summary>
public sealed class TravelEntropyVarianceTests
{
    // --- Boring: near-deterministic via Fixed salt ---

    [Fact]
    public void BoringEntropy_UsesFixedSaltMode()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalBoring);

        Assert.Equal(SaltSourceMode.Fixed, session.SaltSource.Mode);
    }

    // --- Classic: runtime salt provides variance ---

    [Fact]
    public void ClassicEntropy_UsesRuntimeSaltMode()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalClassic);

        Assert.Equal(SaltSourceMode.Runtime, session.SaltSource.Mode);
    }

    [Fact]
    public void ClassicEntropy_DifferentSessionsProduceDifferentSalt()
    {
        var session1 = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalClassic);
        var session2 = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalClassic);

        Assert.Equal(SaltSourceMode.Runtime, session1.SaltSource.Mode);
        Assert.NotEqual(session1.SaltSource.Salt, session2.SaltSource.Salt);
    }

    // --- Boring vs Classic: same seed, different salt mode ---

    [Fact]
    public void BoringAndClassic_SameSeedWorld_DifferentSaltMode()
    {
        // Both use the same canonical seed world, but Boring gets Fixed salt
        // and Classic gets Runtime salt. This is the core entropy distinction.
        var boringSession = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalBoring);
        var classicSession = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalClassic);

        Assert.Equal(SaltSourceMode.Fixed, boringSession.SaltSource.Mode);
        Assert.Equal(SaltSourceMode.Runtime, classicSession.SaltSource.Mode);
        Assert.Equal(TownSignature(boringSession), TownSignature(classicSession));
    }

    private static string TownSignature(GameSession session)
        => string.Join(
            "|",
            session.World.Towns
                .OrderBy(town => town.Id.Value, StringComparer.Ordinal)
                .Select(town => $"{town.Id.Value}:{town.Name}:{town.Prosperity}"));
}
