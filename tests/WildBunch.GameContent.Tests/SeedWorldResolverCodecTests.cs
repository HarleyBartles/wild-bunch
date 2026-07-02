using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldResolverCodecTests
{
    [Fact]
    public void ResolverContractVersion_IsV12()
    {
        Assert.Equal("resolver-v12", SeedWorldResolver.ResolverContractVersion);
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
        Assert.Equal(original.MapLayoutPalette, resolved.MapLayoutPalette);
        Assert.Equal(original.HasOutlierSlot, resolved.HasOutlierSlot);
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
        
        // Should wrap to (15 + 5) % 11 = 20 % 11 = 9
        Assert.Equal(9, resolved.TownCount);
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
    public void MapLayoutPalette_ModuloWrapping_ClampsToDefinedRange()
    {
        // Create a seed with mapLayoutPalette value 7 (outside defined range 0-3)
        var bytes = new byte[16];
        var seedCode = new Guid(bytes);
        
        // Manually set bits 24-26 to 7 (binary 111) using proper bit manipulation
        // MapLayoutPalette is at bits 24-26 (3 bits)
        var low = BitConverter.ToUInt64(bytes, 0);
        low = (low & ~((0x7UL) << 24)) | ((7UL & 0x7UL) << 24);
        BitConverter.TryWriteBytes(bytes.AsSpan(0), low);
        seedCode = new Guid(bytes);

        var resolved = SeedWorldResolver.Resolve(seedCode);
        
        // Should wrap to 7 % 4 = 3 (DoubleLine)
        Assert.Equal(MapLayoutPalette.DoubleLine, resolved.MapLayoutPalette);
    }

    [Fact]
    public void HasOutlierSlot_BitEncoding_RoundTrip()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();

        // Test with HasOutlierSlot = false
        var withFalse = seedWorld with { HasOutlierSlot = false, TownCount = 5 };
        var seedCodeFalse = SeedWorldResolver.CreateRepresentativeSeedCode(withFalse);
        var resolvedFalse = SeedWorldResolver.Resolve(seedCodeFalse);
        Assert.False(resolvedFalse.HasOutlierSlot);

        // Test with HasOutlierSlot = true (requires town count < MaxTownCount)
        var withTrue = seedWorld with { HasOutlierSlot = true, TownCount = 5 };
        var seedCodeTrue = SeedWorldResolver.CreateRepresentativeSeedCode(withTrue);
        var resolvedTrue = SeedWorldResolver.Resolve(seedCodeTrue);
        Assert.True(resolvedTrue.HasOutlierSlot);
    }

    [Fact]
    public void HasOutlierSlot_Validation_RejectsMaxTownCount()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();

        // HasOutlierSlot = true with MaxTownCount should fail validation
        var invalid = seedWorld with { HasOutlierSlot = true, TownCount = SeedWorldResolver.MaxTownCount };
        var validation = SeedWorldResolver.Validate(invalid);

        Assert.False(validation.Success);
        Assert.Contains("Cannot have outlier slot when town count is at maximum", validation.ErrorMessage);
    }

    [Fact]
    public void HasOutlierSlot_BitPosition_Is27()
    {
        // Create a seed with HasOutlierSlot = true by setting bit 27
        var bytes = new byte[16];
        var seedCode = new Guid(bytes);

        var low = BitConverter.ToUInt64(bytes, 0);
        low |= 0x1UL << 27;
        BitConverter.TryWriteBytes(bytes.AsSpan(0), low);
        seedCode = new Guid(bytes);

        var resolved = SeedWorldResolver.Resolve(seedCode);
        Assert.True(resolved.HasOutlierSlot);
    }
}
