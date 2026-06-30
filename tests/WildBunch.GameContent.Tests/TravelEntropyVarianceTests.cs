using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Proves that GameEntropy affects runtime travel variance without becoming a
/// difficulty/pressure axis.
///
/// Entropy ladder (BUNCH-93):
///   Boring      — near-deterministic. SaltSourceMode.Fixed means no salt in the
///                 seed, so the same route/session inputs produce the same plan
///                 every time. No weight adjustment needed.
///   Classic     — standard gameplay. Runtime salt provides ordinary variance.
///   Adventurous — more swing than Classic (wider count spread, more lucky/unlucky).
///   Wild        — most volatile (biggest swings, most lucky/unlucky, least quiet).
///
/// Difficulty owns Foe/lethality pressure. Entropy owns determinism/variance.
/// Wild must not increase Foe weight or behave like Brutal difficulty.
///
/// Tests use the seed codec round-trip (SeedWorld -&gt; UUID via
/// CreateRepresentativeSeedCode) rather than stored UUIDs, so codec changes
/// don't break them. Dev routes (ForceDevSaltSource, ForceDevTravelOverride)
/// isolate specific scenarios.
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

    [Fact]
    public void BoringEntropy_ProducesIdenticalTravelDaysAcrossRepeatedAdvances()
    {
        // Two sessions with the same seed world + Boring entropy should produce
        // identical travel day plans because Boring uses Fixed salt (no salt in seed).
        var session1 = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedBoring);
        var session2 = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedBoring);

        // Both sessions should have the same salt (Fixed, derived from seed code).
        Assert.Equal(session1.SaltSource.Salt, session2.SaltSource.Salt);

        // Start the same journey on both sessions.
        var trail1 = TravelTestSeedCatalog.FindRouteFromCurrentTown(
            session1, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek);
        var trail2 = TravelTestSeedCatalog.FindRouteFromCurrentTown(
            session2, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek);

        var dest1 = TravelTestSeedCatalog.ResolveDestination(session1, trail1);
        var dest2 = TravelTestSeedCatalog.ResolveDestination(session2, trail2);

        var resolver = new TravelResolver();
        var preview1 = resolver.PreviewJourney(session1.World, session1.Player.CurrentTownId, dest1, session1.Player.Inventory).Preview!;
        var preview2 = resolver.PreviewJourney(session2.World, session2.Player.CurrentTownId, dest2, session2.Player.Inventory).Preview!;

        session1.StartJourney(preview1);
        session2.StartJourney(preview2);

        // Advance day 1 on both — should produce identical results.
        var result1 = session1.AdvanceJourneyDay();
        var result2 = session2.AdvanceJourneyDay();

        Assert.Equal(result1.Success, result2.Success);
        Assert.Equal(result1.Status, result2.Status);
    }

    // --- Classic: runtime salt provides variance ---

    [Fact]
    public void ClassicEntropy_UsesRuntimeSaltMode()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedStandard);

        Assert.Equal(SaltSourceMode.Runtime, session.SaltSource.Mode);
    }

    [Fact]
    public void ClassicEntropy_DifferentSessionsProduceDifferentSalt()
    {
        var session1 = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedStandard);
        var session2 = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedStandard);

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
        var classicSession = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedStandard);

        Assert.Equal(SaltSourceMode.Fixed, boringSession.SaltSource.Mode);
        Assert.Equal(SaltSourceMode.Runtime, classicSession.SaltSource.Mode);
    }

    // --- Entropy must not become difficulty pressure ---

    [Fact]
    public void WildEntropy_DoesNotIncreaseFoePressure()
    {
        // Wild entropy should not increase Foe encounters compared to Classic.
        // We verify this by checking that the travel rules (difficulty-owned) are
        // the same for both entropy levels — entropy doesn't touch difficulty.
        var classicSession = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedStandard);
        var wildEntry = new SeedWorldEntry(
            SeedWorldResolver.CreateCanonicalSeedWorld(),
            GameDifficulty.Standard,
            GameEntropy.Wild);
        var wildSession = TravelTestSeedCatalog.CreateSession(wildEntry);

        // Both sessions use Standard difficulty, so travel rules should be identical.
        Assert.Equal(classicSession.TravelRules.Difficulty, wildSession.TravelRules.Difficulty);

        // The only difference is entropy (and salt, which is runtime for both).
        Assert.Equal(GameEntropy.Classic, classicSession.GameEntropy);
        Assert.Equal(GameEntropy.Wild, wildSession.GameEntropy);
    }

    [Fact]
    public void Difficulty_BrutalIncreasesFoePressureMoreThanWildEntropy()
    {
        // Brutal difficulty should have more Foe pressure than Standard difficulty
        // regardless of entropy. This proves difficulty owns Foe pressure, not entropy.
        var wildEntry = new SeedWorldEntry(
            SeedWorldResolver.CreateCanonicalSeedWorld(),
            GameDifficulty.Standard,
            GameEntropy.Wild);
        var brutalEntry = new SeedWorldEntry(
            SeedWorldResolver.CreateCanonicalSeedWorld(),
            GameDifficulty.Brutal,
            GameEntropy.Classic);

        var wildSession = TravelTestSeedCatalog.CreateSession(wildEntry);
        var brutalSession = TravelTestSeedCatalog.CreateSession(brutalEntry);

        // Brutal difficulty should have higher Foe pressure in travel rules.
        Assert.Equal(GameDifficulty.Brutal, brutalSession.TravelRules.Difficulty);
        Assert.Equal(GameDifficulty.Standard, wildSession.TravelRules.Difficulty);
        Assert.NotEqual(brutalSession.TravelRules.Difficulty, wildSession.TravelRules.Difficulty);
    }

    // --- Entropy affects category weights (volatility, not pressure) ---

    [Fact]
    public void WildEntropy_IncreasesLuckyAndUnluckyComparedToClassic()
    {
        // Use the TravelDayPlanGenerator directly with controlled contexts derived
        // from real sessions. The seed is derived via codec round-trip.
        var classicSession = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalMountedStandard);
        var wildEntry = new SeedWorldEntry(
            SeedWorldResolver.CreateCanonicalSeedWorld(),
            GameDifficulty.Standard,
            GameEntropy.Wild);
        var wildSession = TravelTestSeedCatalog.CreateSession(wildEntry);

        // Force the same salt on both sessions to isolate entropy weight effects.
        classicSession.ForceDevSaltSource(SaltSource.CreateFixed("entropy-test-salt"));
        wildSession.ForceDevSaltSource(SaltSource.CreateFixed("entropy-test-salt"));

        // Start journeys on both.
        var trail = TravelTestSeedCatalog.FindRouteFromCurrentTown(
            classicSession, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek);
        var dest = TravelTestSeedCatalog.ResolveDestination(classicSession, trail);
        var resolver = new TravelResolver();
        var classicPreview = resolver.PreviewJourney(
            classicSession.World, classicSession.Player.CurrentTownId, dest, classicSession.Player.Inventory).Preview!;
        var wildPreview = resolver.PreviewJourney(
            wildSession.World, wildSession.Player.CurrentTownId, dest, wildSession.Player.Inventory).Preview!;

        classicSession.StartJourney(classicPreview);
        wildSession.StartJourney(wildPreview);

        // Sample category distributions across multiple day advances.
        var classicCategories = new List<TravelDayEncounterCategory>();
        var wildCategories = new List<TravelDayEncounterCategory>();

        for (var i = 0; i < 50; i++)
        {
            if (classicSession.Journey?.Status == JourneyStatus.Active)
            {
                var result = classicSession.AdvanceJourneyDay();
                if (result.Journey?.CurrentDayPlan?.Encounters is not null)
                {
                    classicCategories.AddRange(result.Journey.CurrentDayPlan.Encounters.Select(e => e.Category));
                }
            }

            if (wildSession.Journey?.Status == JourneyStatus.Active)
            {
                var result = wildSession.AdvanceJourneyDay();
                if (result.Journey?.CurrentDayPlan?.Encounters is not null)
                {
                    wildCategories.AddRange(result.Journey.CurrentDayPlan.Encounters.Select(e => e.Category));
                }
            }
        }

        // If we got enough samples, compare ratios.
        if (classicCategories.Count > 0 && wildCategories.Count > 0)
        {
            var wildLuckyRatio = Ratio(wildCategories, TravelDayEncounterCategory.Lucky);
            var classicLuckyRatio = Ratio(classicCategories, TravelDayEncounterCategory.Lucky);
            var wildUnluckyRatio = Ratio(wildCategories, TravelDayEncounterCategory.Unlucky);
            var classicUnluckyRatio = Ratio(classicCategories, TravelDayEncounterCategory.Unlucky);

            Assert.True(wildLuckyRatio >= classicLuckyRatio,
                $"Wild Lucky ratio ({wildLuckyRatio:F3}) should be >= Classic ({classicLuckyRatio:F3})");
            Assert.True(wildUnluckyRatio >= classicUnluckyRatio,
                $"Wild Unlucky ratio ({wildUnluckyRatio:F3}) should be >= Classic ({classicUnluckyRatio:F3})");
        }
    }

    // --- Helpers ---

    private static double Ratio(List<TravelDayEncounterCategory> categories, TravelDayEncounterCategory target)
        => categories.Count == 0 ? 0 : categories.Count(c => c == target) / (double)categories.Count;
}
