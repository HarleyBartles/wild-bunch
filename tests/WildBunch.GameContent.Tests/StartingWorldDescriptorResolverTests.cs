using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class StartingWorldDescriptorResolverTests
{
    [Fact]
    public void CanonicalDescriptorRoundTripsThroughAUuidShapedSeedCode()
    {
        var descriptor = StartingWorldDescriptorResolver.CreateCanonicalDescriptor();

        var seedCode = StartingWorldDescriptorResolver.CreateRepresentativeSeedCode(descriptor);
        var resolved = StartingWorldDescriptorResolver.Resolve(seedCode);

        Assert.Equal(descriptor.Difficulty, resolved.Difficulty);
        Assert.Equal(descriptor.AdventureRandomnessPolicy, resolved.AdventureRandomnessPolicy);
        Assert.Equal(descriptor.World, resolved.World);
        Assert.Equal(descriptor.Player, resolved.Player);
        Assert.Equal(descriptor.Case, resolved.Case);
        Assert.Equal(seedCode, StartingWorldDescriptorResolver.CreateRepresentativeSeedCode(resolved));
    }

    [Fact]
    public void MultipleUuidSeedsCanResolveToTheSameDescriptor()
    {
        var seedA = CreateSeedCode(1, 0, 0, 0, 1, 0, 1, tail: 1);
        var seedB = CreateSeedCode(1, 0, 0, 0, 1, 0, 1, tail: 2);

        var descriptorA = StartingWorldDescriptorResolver.Resolve(seedA);
        var descriptorB = StartingWorldDescriptorResolver.Resolve(seedB);

        Assert.Equal(descriptorA.Difficulty, descriptorB.Difficulty);
        Assert.Equal(descriptorA.AdventureRandomnessPolicy, descriptorB.AdventureRandomnessPolicy);
        Assert.Equal(descriptorA.World, descriptorB.World);
        Assert.Equal(descriptorA.Player, descriptorB.Player);
        Assert.Equal(descriptorA.Case, descriptorB.Case);
    }

    [Fact]
    public void DescriptorValidationRejectsImpossibleManualEdits()
    {
        var valid = StartingWorldDescriptorResolver.CreateCanonicalDescriptor();

        var invalidLoadout = valid with
        {
            Player = valid.Player with
            {
                StartWithHorse = false,
                Loadout = valid.Player.Loadout with { IncludeHorse = true }
            }
        };

        var invalidCash = valid with
        {
            Player = valid.Player with { StartingCash = 999m }
        };

        var invalidAccusation = valid with
        {
            Case = valid.Case with { AccusationIndex = 42 }
        };

        Assert.False(StartingWorldDescriptorResolver.Validate(invalidLoadout).Success);
        Assert.False(StartingWorldDescriptorResolver.Validate(invalidCash).Success);
        Assert.False(StartingWorldDescriptorResolver.Validate(invalidAccusation).Success);
    }

    [Fact]
    public void AnyValidUuidResolvesToLegalDescriptorValues()
    {
        for (var index = 0; index < 64; index++)
        {
            var seed = CreateSeedCode((byte)(index & 0x03), (byte)(index % 3), (byte)(index % 3), (byte)(index & 0x01), (byte)((index % 7) + 1), (byte)(index % 9), (byte)(index % 3), tail: (ulong)index << 16);
            var descriptor = StartingWorldDescriptorResolver.Resolve(seed);
            var validation = StartingWorldDescriptorResolver.Validate(descriptor);

            Assert.True(validation.Success, validation.ErrorMessage);
            Assert.Contains(descriptor.AdventureRandomnessPolicy, Enum.GetValues<AdventureRandomnessPolicy>());
            Assert.Contains(descriptor.World.Variant, Enum.GetValues<SeedWorldVariant>());
            Assert.Contains(descriptor.Player.LoadoutProfile, Enum.GetValues<StartingLoadoutProfile>());
            Assert.InRange(descriptor.Player.StartingCash, 10m, 40m);
            Assert.InRange(descriptor.Case.AccusationIndex, 0, 6);
        }
    }

    [Fact]
    public void AdventureRandomnessPolicyStaysDescriptorLevelAndWildStaysLegal()
    {
        var wildSeed = CreateSeedCode(3, 0, 0, 0, 1, 8, 1, tail: 0);
        var descriptor = StartingWorldDescriptorResolver.Resolve(wildSeed);

        Assert.Equal(AdventureRandomnessPolicy.Wild, descriptor.AdventureRandomnessPolicy);
        Assert.Equal(GameSetupDeterministicLabels.WorldStartingTownHorse, descriptor.World.StartingTownSelectionKey);

        var validation = StartingWorldDescriptorResolver.Validate(descriptor);
        Assert.True(validation.Success, validation.ErrorMessage);
        Assert.Equal(1, descriptor.Case.AccusationIndex);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.World.StartingTownSelectionKey));
    }

    [Fact]
    public void InvalidUuidSeedCodesFailValidation()
    {
        Assert.False(StartingWorldDescriptorCodeValidator.TryValidate("not-a-uuid", out var errorMessage));
        Assert.Equal("Seed code must be a UUID-shaped string.", errorMessage);
    }

    private static Guid CreateSeedCode(byte byte0, byte byte1, byte byte2, byte byte3, byte byte4, byte byte5, byte byte6, ulong tail)
    {
        var bytes = new byte[16];
        bytes[0] = byte0;
        bytes[1] = byte1;
        bytes[2] = byte2;
        bytes[3] = byte3;
        bytes[4] = byte4;
        bytes[5] = byte5;
        bytes[6] = byte6;
        bytes[7] = (byte)(tail & 0xFF);
        bytes[8] = (byte)((tail >> 8) & 0xFF);
        bytes[9] = (byte)((tail >> 16) & 0xFF);
        bytes[10] = (byte)((tail >> 32) & 0xFF);
        bytes[11] = (byte)((tail >> 40) & 0xFF);
        bytes[12] = (byte)((tail >> 48) & 0xFF);
        bytes[13] = (byte)((tail >> 56) & 0xFF);
        return new Guid(bytes);
    }
}
