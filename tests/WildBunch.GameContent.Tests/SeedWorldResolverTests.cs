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
        var seedCode = SeedWorldResolver.FormatSeedCode(Guid.NewGuid());

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

    [Fact]
    public void ResolverDerivesTownCountFromSeed()
    {
        var seedWorld = SeedWorldResolver.Resolve(Guid.NewGuid());
        Assert.InRange(seedWorld.SelectedTownIds.Count, 6, 8);
    }

    [Fact]
    public void ResolverAlwaysIncludesAnchorTowns()
    {
        for (var i = 0; i < 32; i++)
        {
            var seedWorld = SeedWorldResolver.Resolve(Guid.NewGuid());
            Assert.Contains("pinecross", seedWorld.SelectedTownIds);
            Assert.Contains("redmesa", seedWorld.SelectedTownIds);
            Assert.Contains("holloway", seedWorld.SelectedTownIds);
        }
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTownCounts()
    {
        var counts = new HashSet<int>();
        for (var i = 0; i < 128; i++)
        {
            var seedWorld = SeedWorldResolver.Resolve(Guid.NewGuid());
            counts.Add(seedWorld.SelectedTownIds.Count);
        }
        Assert.True(counts.Count >= 2, $"Expected at least 2 different town counts, got {counts.Count}");
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTownSelections()
    {
        var selections = new HashSet<string>();
        for (var i = 0; i < 128; i++)
        {
            var seedWorld = SeedWorldResolver.Resolve(Guid.NewGuid());
            selections.Add(string.Join(",", seedWorld.SelectedTownIds.OrderBy(id => id)));
        }
        Assert.True(selections.Count >= 2, $"Expected at least 2 different town selections, got {selections.Count}");
    }

    [Fact]
    public void SameSeedProducesSameSeedWorld()
    {
        var seed = Guid.NewGuid();
        var seedWorldA = SeedWorldResolver.Resolve(seed);
        var seedWorldB = SeedWorldResolver.Resolve(seed);
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
