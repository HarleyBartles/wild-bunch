using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldResolverCodecTests
{
    [Fact]
    public void RoundTrip_PreservesTownCount()
    {
        for (var townCount = SeedWorldResolver.MinTownCount; townCount <= SeedWorldResolver.MaxTownCount; townCount++)
        {
            var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
            var modified = seedWorld with { TownCount = townCount };
            var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(modified);
            var resolved = SeedWorldResolver.Resolve(seedCode);

            Assert.Equal(townCount, resolved.TownCount);
        }
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = SeedWorldResolver.CreateCanonicalSeedWorld();
        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(original);
        var resolved = SeedWorldResolver.Resolve(seedCode);

        Assert.Equal(original.WorldVariant, resolved.WorldVariant);
        Assert.Equal(original.TownCount, resolved.TownCount);
        Assert.Equal(original.AccusationIndex, resolved.AccusationIndex);
        Assert.Equal(original.DefaultCulpritIndex, resolved.DefaultCulpritIndex);
        Assert.Equal(original.CashBonus, resolved.CashBonus);
        Assert.Equal(original.ProsperityPalette, resolved.ProsperityPalette);
        Assert.Equal(original.ReservedTownNameDerivationBits, resolved.ReservedTownNameDerivationBits);
        Assert.Equal(original.ClusterCount, resolved.ClusterCount);
        Assert.Equal(original.GraphDensity, resolved.GraphDensity);
        Assert.Equal(original.OutlierSlotType, resolved.OutlierSlotType);
    }

    [Fact]
    public void Resolve_DecodesFixedSeedWorldFixture()
    {
        var seedCode = Guid.Parse("2020c0c4-0000-0000-0000-000000000000");
        var expected = new SeedWorld(
            seedCode,
            SeedWorldVariant.Canonical,
            8,
            1,
            ProsperityPalette.UniformProsperous,
            1,
            GraphDensity.Sparse,
            1,
            3,
            0,
            0,
            BuildingLayoutPalette.NoSpurs_ClusterMiddle);

        Assert.Equal(expected, SeedWorldResolver.Resolve(seedCode));
    }

    [Fact]
    public void ReservedTownNameDerivationBits_RoundTripAcrossEncodedRange()
    {
        for (var bits = 0; bits < 8; bits++)
        {
            var original = SeedWorldResolver.CreateCanonicalSeedWorld() with
            {
                ReservedTownNameDerivationBits = bits,
            };
            var resolved = SeedWorldResolver.Resolve(SeedWorldResolver.CreateRepresentativeSeedCode(original));

            Assert.Equal(bits, resolved.ReservedTownNameDerivationBits);
        }
    }

    [Fact]
    public void OutlierSlotType_BitEncoding_RoundTrip()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();

        // Test with OutlierSlotType = 0 (no outlier)
        var withZero = seedWorld with { OutlierSlotType = 0, TownCount = 5 };
        var seedCodeZero = SeedWorldResolver.CreateRepresentativeSeedCode(withZero);
        var resolvedZero = SeedWorldResolver.Resolve(seedCodeZero);
        Assert.Equal(0, resolvedZero.OutlierSlotType);

        // Test with OutlierSlotType = 1 (simple outlier, requires town count < MaxTownCount)
        var withOne = seedWorld with { OutlierSlotType = 1, TownCount = 5 };
        var seedCodeOne = SeedWorldResolver.CreateRepresentativeSeedCode(withOne);
        var resolvedOne = SeedWorldResolver.Resolve(seedCodeOne);
        Assert.Equal(1, resolvedOne.OutlierSlotType);
    }

    [Fact]
    public void OutlierSlotType_Validation_RejectsMaxTownCount()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();

        // OutlierSlotType > 0 with MaxTownCount should fail validation
        var invalid = seedWorld with { OutlierSlotType = 1, TownCount = SeedWorldResolver.MaxTownCount };
        var validation = SeedWorldResolver.Validate(invalid);

        Assert.False(validation.Success);
        Assert.Contains("Cannot have outlier slot when town count is at maximum", validation.ErrorMessage);
    }

    [Fact]
    public void OutlierSlotType_Validation_RejectsInvalidRange()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();

        // OutlierSlotType > 1 should fail validation (2-3 are reserved)
        var invalid = seedWorld with { OutlierSlotType = 2 };
        var validation = SeedWorldResolver.Validate(invalid);

        Assert.False(validation.Success);
        Assert.Contains("Outlier slot type must be 0 (no outlier) or 1 (simple outlier)", validation.ErrorMessage);
    }

}
