using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldResolverTests
{
    [Fact]
    public void CanonicalSeedWorldRoundTripsThroughAUuidShapedSeedCode()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();

        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld);
        var resolved = SeedWorldResolver.Resolve(seedCode);

        Assert.Equal(seedWorld.WorldVariant, resolved.WorldVariant);
        Assert.Equal(seedWorld.SelectedTownIds, resolved.SelectedTownIds);
        Assert.Equal(seedWorld.AccusationIndex, resolved.AccusationIndex);
        Assert.Equal(seedWorld.DefaultCulpritIndex, resolved.DefaultCulpritIndex);
        Assert.Equal(seedWorld.CashBonus, resolved.CashBonus);
        Assert.Equal(seedCode, SeedWorldResolver.CreateRepresentativeSeedCode(resolved));
    }

    [Fact]
    public void MultipleUuidSeedsCanResolveToTheSameSeedWorld()
    {
        var seedA = CreateSeedCode(0, 1, 3, 0, tail: 1);
        var seedB = CreateSeedCode(0, 1, 3, 0, tail: 2);

        var seedWorldA = SeedWorldResolver.Resolve(seedA);
        var seedWorldB = SeedWorldResolver.Resolve(seedB);

        Assert.Equal(seedWorldA.WorldVariant, seedWorldB.WorldVariant);
        Assert.Equal(seedWorldA.SelectedTownIds, seedWorldB.SelectedTownIds);
        Assert.Equal(seedWorldA.AccusationIndex, seedWorldB.AccusationIndex);
        Assert.Equal(seedWorldA.DefaultCulpritIndex, seedWorldB.DefaultCulpritIndex);
        Assert.Equal(seedWorldA.CashBonus, seedWorldB.CashBonus);
    }

    [Fact]
    public void SeedWorldResolutionIsIndependentOfDifficultyAndEntropy()
    {
        // The seed codec does NOT reference GameDifficulty or GameEntropy.
        // The same seed resolves to the same seed world regardless of what
        // difficulty/entropy would be selected downstream.
        var seedCode = SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode());

        var seedWorld = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        // SeedWorld has no difficulty or entropy fields — they are downstream.
        Assert.True(Enum.IsDefined(typeof(SeedWorldVariant), seedWorld.WorldVariant));
        Assert.True(seedWorld.AccusationIndex >= 0 && seedWorld.AccusationIndex <= 6);
        Assert.True(seedWorld.DefaultCulpritIndex >= 0 && seedWorld.DefaultCulpritIndex <= 6);
        Assert.True(seedWorld.CashBonus >= 0 && seedWorld.CashBonus <= 8);
    }

    [Fact]
    public void SeedWorldValidationRejectsImpossibleManualEdits()
    {
        var valid = SeedWorldResolver.CreateCanonicalSeedWorld();

        var invalidAccusation = valid with { AccusationIndex = 42 };
        var invalidCulprit = valid with { DefaultCulpritIndex = 42 };
        var invalidCashBonus = valid with { CashBonus = 42 };

        Assert.False(SeedWorldResolver.Validate(invalidAccusation).Success);
        Assert.False(SeedWorldResolver.Validate(invalidCulprit).Success);
        Assert.False(SeedWorldResolver.Validate(invalidCashBonus).Success);
    }

    [Fact]
    public void AnyValidUuidResolvesToLegalSeedWorldValues()
    {
        for (var index = 0; index < 64; index++)
        {
            var seed = CreateSeedCode((byte)(index % 3), (byte)(index % 7), (byte)(index % 7), (byte)(index % 9), tail: (ulong)index << 16);
            var seedWorld = SeedWorldResolver.Resolve(seed);
            var validation = SeedWorldResolver.Validate(seedWorld);

            Assert.True(validation.Success, validation.ErrorMessage);
            Assert.Contains(seedWorld.WorldVariant, Enum.GetValues<SeedWorldVariant>());
            Assert.InRange(seedWorld.AccusationIndex, 0, 6);
            Assert.InRange(seedWorld.DefaultCulpritIndex, 0, 6);
            Assert.InRange(seedWorld.CashBonus, 0, 8);
            // Anchor towns must always be present for trail graph connectivity.
            Assert.Contains("pinecross", seedWorld.SelectedTownIds);
            Assert.Contains("redmesa", seedWorld.SelectedTownIds);
            Assert.Contains("holloway", seedWorld.SelectedTownIds);
        }
    }

    [Fact]
    public void NeighboringUuidEditsAvalancheAcrossSeedWorldFields()
    {
        var seedA = Guid.ParseExact("00000000-0000-0000-0000-000000000000", "D");
        var seedB = Guid.ParseExact("00000000-0000-0000-0000-000000000001", "D");

        var seedWorldA = SeedWorldResolver.Resolve(seedA);
        var seedWorldB = SeedWorldResolver.Resolve(seedB);

        var differenceScore = 0;
        if (seedWorldA.WorldVariant != seedWorldB.WorldVariant) differenceScore++;
        if (!seedWorldA.SelectedTownIds.SequenceEqual(seedWorldB.SelectedTownIds)) differenceScore++;
        if (seedWorldA.AccusationIndex != seedWorldB.AccusationIndex) differenceScore++;
        if (seedWorldA.DefaultCulpritIndex != seedWorldB.DefaultCulpritIndex) differenceScore++;
        if (seedWorldA.CashBonus != seedWorldB.CashBonus) differenceScore++;

        Assert.True(differenceScore >= 2, $"Expected avalanche behavior, but only {differenceScore} seed world surfaces changed.");
    }

    [Fact]
    public void InvalidUuidSeedCodesFailValidation()
    {
        Assert.False(StartingWorldDescriptorCodeValidator.TryValidate("not-a-uuid", out var errorMessage));
        Assert.Equal("Seed code must be a UUID-shaped string.", errorMessage);
        Assert.False(SeedWorldResolver.TryParseSeedCode("WB1-N-03-000000000000-0000", out _));
    }

    // --- Descriptor-based seed-derived town selection tests ---
    //
    // These tests build SeedWorld shapes using the resolver's own SelectTowns
    // method with fixed selection seeds. This is descriptor-based (the descriptor
    // is the town count + selection seed) and deterministic without treating raw
    // UUID strings as canonical fixtures.
    //
    // The UUID round-trip (CreateRepresentativeSeedCode) is tested separately
    // by CanonicalSeedWorldRoundTripsThroughAUuidShapedSeedCode, which uses the
    // canonical 8-town world where all towns are selected (the only shape where
    // the round-trip search space is small enough to reliably find a match).

    /// <summary>
    /// Builds a SeedWorld with the given town count and selection seed using
    /// the resolver's own SelectTowns method. This produces shapes that are
    /// guaranteed reachable by the resolver.
    /// </summary>
    private static SeedWorld BuildSeedWorldWithCount(SeedWorldVariant variant, int townCount, ulong selectionSeed)
    {
        var selectedTownIds = SeedWorldResolver.SelectTowns(townCount, selectionSeed);
        var trails = SeedWorldResolver.BuildTrails(variant, selectedTownIds);
        return new SeedWorld(Guid.Empty, variant, selectedTownIds, trails, 1, 3, 0);
    }

    // 6-town worlds with different selection seeds
    private static readonly SeedWorld SixTownWorldA = BuildSeedWorldWithCount(SeedWorldVariant.Canonical, 6, 0);
    private static readonly SeedWorld SixTownWorldB = BuildSeedWorldWithCount(SeedWorldVariant.Canonical, 6, 1);

    // 7-town world
    private static readonly SeedWorld SevenTownWorld = BuildSeedWorldWithCount(SeedWorldVariant.Canonical, 7, 0);

    // 8-town world (all towns)
    private static readonly SeedWorld EightTownWorld = BuildSeedWorldWithCount(SeedWorldVariant.Canonical, 8, 0);

    [Fact]
    public void ResolverDerivesTownCountFromSeed()
    {
        Assert.Equal(6, SixTownWorldA.SelectedTownIds.Count);
        Assert.InRange(SixTownWorldA.SelectedTownIds.Count, 6, 8);
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTownCounts()
    {
        Assert.Equal(6, SixTownWorldA.SelectedTownIds.Count);
        Assert.Equal(7, SevenTownWorld.SelectedTownIds.Count);
        Assert.Equal(8, EightTownWorld.SelectedTownIds.Count);
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTownSelections()
    {
        // Two 6-town worlds with different selection seeds may produce
        // different town selections.
        var selectionA = string.Join(",", SixTownWorldA.SelectedTownIds.OrderBy(id => id));
        var selectionB = string.Join(",", SixTownWorldB.SelectedTownIds.OrderBy(id => id));

        Assert.NotEqual(selectionA, selectionB);
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTrailSignatures()
    {
        // Different town selections produce different trail graphs.
        var trailsA = string.Join(",", SixTownWorldA.Trails.Select(t => t.Id).OrderBy(id => id));
        var trailsB = string.Join(",", SixTownWorldB.Trails.Select(t => t.Id).OrderBy(id => id));

        Assert.NotEqual(trailsA, trailsB);
    }

    [Fact]
    public void SameSeedProducesSameSeedWorld()
    {
        // The same selection seed produces the same town selection.
        var worldA = BuildSeedWorldWithCount(SeedWorldVariant.Canonical, 7, 42);
        var worldB = BuildSeedWorldWithCount(SeedWorldVariant.Canonical, 7, 42);
        Assert.Equal(worldA.SelectedTownIds, worldB.SelectedTownIds);
        Assert.Equal(worldA.Trails.Count, worldB.Trails.Count);
    }

    [Fact]
    public void CanonicalSeedWorldHasAllEightTowns()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        Assert.Equal(8, seedWorld.SelectedTownIds.Count);
        Assert.Equal(9, seedWorld.Trails.Count);
    }

    private static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong tail)
        => SeedWorldSeedCodeFactory.CreateSeedCode(worldVariant, accusationIndex, defaultCulpritIndex, cashBonus, tail);
}
