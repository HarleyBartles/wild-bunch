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
        Assert.Equal(descriptor.Entropy, resolved.Entropy);
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
        Assert.Equal(descriptorA.Entropy, descriptorB.Entropy);
        Assert.Equal(descriptorA.World, descriptorB.World);
        Assert.Equal(descriptorA.Player, descriptorB.Player);
        Assert.Equal(descriptorA.Case, descriptorB.Case);
    }

    [Fact]
    public void ExplicitSeedResolutionIgnoresRequestedDifficultyAndEntropy()
    {
        var descriptor = StartingWorldDescriptorResolver.CreateCanonicalDescriptor(
            GameDifficulty.Easy,
            GameEntropy.Boring);
        var seedCode = StartingWorldDescriptorResolver.FormatSeedCode(
            StartingWorldDescriptorResolver.CreateRepresentativeSeedCode(descriptor));

        var baseline = StartingWorldDescriptorResolver.Resolve(
            seedCode,
            GameDifficulty.Easy,
            GameEntropy.Boring);
        var challenged = StartingWorldDescriptorResolver.Resolve(
            seedCode,
            GameDifficulty.Challenging,
            GameEntropy.Wild);

        Assert.Equal(baseline, challenged);
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
            var seed = CreateSeedCode((byte)(index & 0x03), (byte)(index % 3), (byte)(index % 3), (byte)(index & 0x01), (byte)(index % 7), (byte)(index % 9), (byte)(index % 3), tail: (ulong)index << 16);
            var descriptor = StartingWorldDescriptorResolver.Resolve(seed);
            var validation = StartingWorldDescriptorResolver.Validate(descriptor);

            Assert.True(validation.Success, validation.ErrorMessage);
            Assert.Contains(descriptor.Entropy, Enum.GetValues<GameEntropy>());
            Assert.Contains(descriptor.World.Variant, Enum.GetValues<SeedWorldVariant>());
            Assert.Contains(descriptor.Player.LoadoutProfile, Enum.GetValues<StartingLoadoutProfile>());
            Assert.InRange(descriptor.Player.StartingCash, 10m, 40m);
            Assert.InRange(descriptor.Case.AccusationIndex, 0, 6);
        }
    }

    [Fact]
    public void GameEntropyStaysDescriptorLevelAndWildStaysLegal()
    {
        var wildSeed = CreateSeedCode(3, 0, 0, 0, 1, 8, 1, tail: 0);
        var descriptor = StartingWorldDescriptorResolver.Resolve(wildSeed);

        Assert.Equal(GameEntropy.Wild, descriptor.Entropy);
        Assert.Equal(GameSetupDeterministicLabels.WorldStartingTownHorse, descriptor.World.StartingTownSelectionKey);

        var validation = StartingWorldDescriptorResolver.Validate(descriptor);
        Assert.True(validation.Success, validation.ErrorMessage);
        Assert.Equal(1, descriptor.Case.AccusationIndex);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.World.StartingTownSelectionKey));
    }

    [Fact]
    public void NeighboringUuidEditsAvalancheAcrossDescriptorFields()
    {
        var seedA = Guid.ParseExact("00000000-0000-0000-0000-000000000000", "D");
        var seedB = Guid.ParseExact("00000000-0000-0000-0000-000000000001", "D");

        var descriptorA = StartingWorldDescriptorResolver.Resolve(seedA);
        var descriptorB = StartingWorldDescriptorResolver.Resolve(seedB);

        var differenceScore = 0;
        if (descriptorA.Difficulty != descriptorB.Difficulty) differenceScore++;
        if (descriptorA.Entropy != descriptorB.Entropy) differenceScore++;
        if (descriptorA.World != descriptorB.World) differenceScore++;
        if (descriptorA.Player != descriptorB.Player) differenceScore++;
        if (descriptorA.Case != descriptorB.Case) differenceScore++;

        Assert.True(differenceScore >= 4, $"Expected avalanche behavior, but only {differenceScore} descriptor surfaces changed.");
    }

    [Fact]
    public void InvalidUuidSeedCodesFailValidation()
    {
        Assert.False(StartingWorldDescriptorCodeValidator.TryValidate("not-a-uuid", out var errorMessage));
        Assert.Equal("Seed code must be a UUID-shaped string.", errorMessage);
        Assert.False(StartingWorldDescriptorResolver.TryParseSeedCode("WB1-N-03-000000000000-0000", out _));
    }

    private static Guid CreateSeedCode(byte byte0, byte byte1, byte byte2, byte byte3, byte byte4, byte byte5, byte byte6, ulong tail)
        => StartingWorldDescriptorSeedCodeFactory.CreateSeedCode(byte0, byte1, byte2, byte3, byte4, byte5, byte6, tail);
}
