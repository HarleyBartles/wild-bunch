using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class SeedWorldResolverCodecTests
{
    [Fact]
    public void CreateRepresentativeSeedCode_RoundTripsBuildingLayoutPalette()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld() with
        {
            BuildingLayoutPalette = BuildingLayoutPalette.NoSpurs_ClusterMiddle,
        };
        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld);
        var expectedSeedCode = Guid.Parse("2020c0c4-0000-0000-0000-000000000000");

        Assert.Equal(expectedSeedCode, seedCode);
        Assert.Equal(
            BuildingLayoutPalette.NoSpurs_ClusterMiddle,
            SeedWorldResolver.Resolve(expectedSeedCode).BuildingLayoutPalette);
    }

    [Fact]
    public void Resolve_DecodesAll16PaletteValues()
    {
        // Test that all 16 palette values (0-15) decode correctly
        for (var i = 0; i < 16; i++)
        {
            var seedCode = Guid.Empty;
            var bytes = seedCode.ToByteArray();
            var low = BitConverter.ToUInt64(bytes, 0);

            // Encode palette value at bits 29-32
            var encodedLow = (low & ~(0xFUL << 29)) | ((ulong)i << 29);
            var encodedBytes = new byte[16];
            BitConverter.TryWriteBytes(encodedBytes.AsSpan(0), encodedLow);
            BitConverter.TryWriteBytes(encodedBytes.AsSpan(8), BitConverter.ToUInt64(bytes, 8));
            var encodedSeedCode = new Guid(encodedBytes);

            var decoded = SeedWorldResolver.Resolve(encodedSeedCode);
            var decodedValue = (int)decoded.BuildingLayoutPalette;

            Assert.Equal(i, decodedValue);
        }
    }
}
