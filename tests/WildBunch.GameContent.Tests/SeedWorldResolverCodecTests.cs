using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldResolverCodecTests
{
    [Fact]
    public void ResolverContractVersion_IsV11()
    {
        Assert.Equal("resolver-v11", SeedWorldResolver.ResolverContractVersion);
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
}
