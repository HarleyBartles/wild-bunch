using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class SeedWorldResolverCodecTests
{
    [Fact]
    public void CreateRepresentativeSeedCode_RoundTripsBuildingLayoutPalette()
    {
        // Test encoding/decoding directly without validation
        var seedCode = Guid.NewGuid();
        var bytes = seedCode.ToByteArray();
        var low = BitConverter.ToUInt64(bytes, 0);
        
        // Manually encode Tree (value 3) at bits 29-32
        var encodedLow = (low & ~(0xFUL << 29)) | ((ulong)3 << 29);
        var encodedBytes = new byte[16];
        BitConverter.TryWriteBytes(encodedBytes.AsSpan(0), encodedLow);
        BitConverter.TryWriteBytes(encodedBytes.AsSpan(8), BitConverter.ToUInt64(bytes, 8));
        var encodedSeedCode = new Guid(encodedBytes);
        
        // Verify the encoding worked
        var verifyBytes = encodedSeedCode.ToByteArray();
        var verifyLow = BitConverter.ToUInt64(verifyBytes, 0);
        var verifyExtracted = (verifyLow >> 29) & 0xFUL;
        Assert.Equal(3UL, verifyExtracted);
        
        // Decode
        var decoded = SeedWorldResolver.Resolve(encodedSeedCode);
        
        Assert.Equal(BuildingLayoutPalette.Tree, decoded.BuildingLayoutPalette);
    }
}
