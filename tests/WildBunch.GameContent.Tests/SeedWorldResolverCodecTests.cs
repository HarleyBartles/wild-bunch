using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldResolverCodecTests
{
    [Fact]
    public void ResolverContractVersion_IsV17()
    {
        Assert.Equal("resolver-v17", SeedWorldResolver.ResolverContractVersion);
    }

    [Fact]
    public void TownCount_Bounds_Are5To10()
    {
        Assert.Equal(5, SeedWorldResolver.MinTownCount);
        Assert.Equal(10, SeedWorldResolver.MaxTownCount);
    }

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
        Assert.Equal(original.ServicesPalette, resolved.ServicesPalette);
        Assert.Equal(original.ClusterCount, resolved.ClusterCount);
        Assert.Equal(original.GraphDensity, resolved.GraphDensity);
        Assert.Equal(original.OutlierSlotType, resolved.OutlierSlotType);
    }

    [Fact]
    public void TownCount_ModuloWrapping_ClampsToDefinedRange()
    {
        // Create a seed with townCount value 15 (outside defined range 5-10)
        var bytes = new byte[16];
        var seedCode = new Guid(bytes);
        
        // Set bits 14-17 to 15 (binary 1111)
        var low = BitConverter.ToUInt64(bytes, 0);
        low |= 0xFUL << 14;
        BitConverter.TryWriteBytes(bytes.AsSpan(0), low);
        seedCode = new Guid(bytes);

        var resolved = SeedWorldResolver.Resolve(seedCode);
        
        // Should wrap to 5 + ((15 + 5 - 5) % 6) = 5 + (15 % 6) = 5 + 3 = 8
        Assert.Equal(8, resolved.TownCount);
    }

    [Fact]
    public void ProsperityPalette_ModuloWrapping_ClampsToDefinedRange()
    {
        // Create a seed with prosperityPalette value 7 (within range 0-7)
        var bytes = new byte[16];
        var seedCode = new Guid(bytes);
        
        // Set bits 18-20 to 7
        var low = BitConverter.ToUInt64(bytes, 0);
        low |= 0x7UL << 18;
        BitConverter.TryWriteBytes(bytes.AsSpan(0), low);
        seedCode = new Guid(bytes);

        var resolved = SeedWorldResolver.Resolve(seedCode);
        
        // Should wrap to 7 % 8 = 7 (within range)
        Assert.Equal((ProsperityPalette)7, resolved.ProsperityPalette);
    }

    [Fact]
    public void ServicesPalette_ModuloWrapping_ClampsToDefinedRange()
    {
        // Create a seed with servicesPalette value 7 (within range 0-7)
        var bytes = new byte[16];
        var seedCode = new Guid(bytes);
        
        // Set bits 21-23 to 7
        var low = BitConverter.ToUInt64(bytes, 0);
        low |= 0x7UL << 21;
        BitConverter.TryWriteBytes(bytes.AsSpan(0), low);
        seedCode = new Guid(bytes);

        var resolved = SeedWorldResolver.Resolve(seedCode);
        
        // Should wrap to 7 % 8 = 7 (within range)
        Assert.Equal((ServicesPalette)7, resolved.ServicesPalette);
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

    [Fact]
    public void OutlierSlotType_BitPosition_Is27_28()
    {
        // Create a seed with OutlierSlotType = 1 by setting bits 27-28
        var bytes = new byte[16];
        var seedCode = new Guid(bytes);

        var low = BitConverter.ToUInt64(bytes, 0);
        low |= 0x1UL << 27; // Set bit 27
        BitConverter.TryWriteBytes(bytes.AsSpan(0), low);
        seedCode = new Guid(bytes);

        var resolved = SeedWorldResolver.Resolve(seedCode);
        Assert.Equal(1, resolved.OutlierSlotType);
    }
}
