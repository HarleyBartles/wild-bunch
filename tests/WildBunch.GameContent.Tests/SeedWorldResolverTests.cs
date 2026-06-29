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
        var seedCode = SeedWorldResolver.FormatSeedCode(SeedSevenTowns);

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

    // Deterministic fixed seed GUIDs (new Guid(i, 0, 0, ...)) proven to produce
    // different town counts, selections, and trail graphs. See the seed survey
    // in the BUNCH-107 plan for the full mapping.
    private static readonly Guid SeedSixTowns = new(0x00000001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);   // Rail, 6 towns, 5 trails
    private static readonly Guid SeedEightTowns = new(0x00000002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);  // Rail, 8 towns, 9 trails
    private static readonly Guid SeedSevenTowns = new(0x00000003, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);  // Rail, 7 towns, 7 trails
    private static readonly Guid SeedCanonicalSeven = new(0x00000005, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0); // Canonical, 7 towns, 6 trails
    private static readonly Guid SeedCanonicalSix = new(0x00000014, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);  // Canonical, 6 towns, 5 trails
    private static readonly Guid SeedCanonicalEight = new(0x00000011, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0); // Canonical, 8 towns, 9 trails

    private static readonly Guid[] DeterministicSeeds =
    [
        SeedSixTowns, SeedEightTowns, SeedSevenTowns, SeedCanonicalSeven, SeedCanonicalSix, SeedCanonicalEight,
        new(0x00000004, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000006, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000007, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000008, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000009, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000a, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000b, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000c, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000d, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000e, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x0000000f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        new(0x00000010, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
    ];

    [Fact]
    public void ResolverDerivesTownCountFromSeed()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedSixTowns);
        Assert.InRange(seedWorld.SelectedTownIds.Count, 6, 8);
    }

    [Fact]
    public void ResolverAlwaysIncludesAnchorTowns()
    {
        foreach (var seed in DeterministicSeeds)
        {
            var seedWorld = SeedWorldResolver.Resolve(seed);
            Assert.Contains("pinecross", seedWorld.SelectedTownIds);
            Assert.Contains("redmesa", seedWorld.SelectedTownIds);
            Assert.Contains("holloway", seedWorld.SelectedTownIds);
        }
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTownCounts()
    {
        var counts = new HashSet<int>();
        foreach (var seed in DeterministicSeeds)
        {
            var seedWorld = SeedWorldResolver.Resolve(seed);
            counts.Add(seedWorld.SelectedTownIds.Count);
        }
        Assert.True(counts.Count >= 2, $"Expected at least 2 different town counts, got {counts.Count}: [{string.Join(", ", counts)}]");
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTownSelections()
    {
        var selections = new HashSet<string>();
        foreach (var seed in DeterministicSeeds)
        {
            var seedWorld = SeedWorldResolver.Resolve(seed);
            selections.Add(string.Join(",", seedWorld.SelectedTownIds.OrderBy(id => id)));
        }
        Assert.True(selections.Count >= 2, $"Expected at least 2 different town selections, got {selections.Count}");
    }

    [Fact]
    public void SameSeedProducesSameSeedWorld()
    {
        var seedWorldA = SeedWorldResolver.Resolve(SeedSevenTowns);
        var seedWorldB = SeedWorldResolver.Resolve(SeedSevenTowns);
        Assert.Equal(seedWorldA.SelectedTownIds, seedWorldB.SelectedTownIds);
        Assert.Equal(seedWorldA.Trails.Count, seedWorldB.Trails.Count);
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
