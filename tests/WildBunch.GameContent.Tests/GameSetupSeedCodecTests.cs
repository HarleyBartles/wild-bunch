using WildBunch.Domain.Cases;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class GameSetupSeedCodecTests
{
    [Fact]
    public void SeedCodecRoundTripsAndDecodesDifficultyAndOptions()
    {
        var seed = GameSetupSeedCodec.WithOption(
            GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), WildBunch.Domain.Travel.TravelDifficulty.Hard),
            GameSetupOption.LoadoutProfile,
            (int)StartingLoadoutProfile.Stocked);
        seed = GameSetupSeedCodec.WithOption(seed, GameSetupOption.StartWithHorse, 0);
        seed = GameSetupSeedCodec.WithOption(seed, GameSetupOption.JourneyRandomness, 1);

        var seedCode = GameSetupSeedCodec.Encode(seed);
        var decoded = GameSetupSeedCodec.Decode(seedCode);

        Assert.True(decoded.Success);
        Assert.NotNull(decoded.Seed);
        Assert.Equal(seed.GeneratorVersion, decoded.Seed!.GeneratorVersion);
        Assert.Equal(seed.Difficulty, decoded.Seed.Difficulty);
        Assert.Equal(seed.Options, decoded.Seed.Options);
        Assert.Equal(seed.Entropy, decoded.Seed.Entropy);
        Assert.Equal(seedCode, GameSetupSeedCodec.Encode(decoded.Seed));
    }

    [Fact]
    public void EditingDifficultyOrOptionsRewritesTheEncodedSeed()
    {
        var canonical = GameSetupSeedCodec.CreateCanonicalSeed();
        var easySeed = GameSetupSeedCodec.WithDifficulty(canonical, WildBunch.Domain.Travel.TravelDifficulty.Easy);
        var noHorseSeed = GameSetupSeedCodec.WithOption(canonical, GameSetupOption.StartWithHorse, 0);
        var stockedSeed = GameSetupSeedCodec.WithOption(canonical, GameSetupOption.LoadoutProfile, (int)StartingLoadoutProfile.Stocked);
        var deterministicTravelSeed = GameSetupSeedCodec.WithOption(canonical, GameSetupOption.JourneyRandomness, 1);

        Assert.NotEqual(GameSetupSeedCodec.Encode(canonical), GameSetupSeedCodec.Encode(easySeed));
        Assert.NotEqual(GameSetupSeedCodec.Encode(canonical), GameSetupSeedCodec.Encode(noHorseSeed));
        Assert.NotEqual(GameSetupSeedCodec.Encode(canonical), GameSetupSeedCodec.Encode(stockedSeed));
        Assert.NotEqual(GameSetupSeedCodec.Encode(canonical), GameSetupSeedCodec.Encode(deterministicTravelSeed));
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(0x800000000000UL)]
    [InlineData(GameSetupSeed.CanonicalEntropyMaximum)]
    public void CanonicalEntropyValuesRoundTripIntoValidPackages(ulong entropy)
    {
        var seed = GameSetupSeedCodec.WithOption(
            GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), TravelDifficulty.Normal),
            GameSetupOption.LoadoutProfile,
            (int)StartingLoadoutProfile.Standard) with
        {
            Entropy = entropy
        };

        var seedCode = GameSetupSeedCodec.Encode(seed);
        var decoded = GameSetupSeedCodec.Decode(seedCode);

        Assert.True(decoded.Success);
        Assert.NotNull(decoded.Seed);
        Assert.Equal(entropy, decoded.Seed!.Entropy);

        var package = new GameSetupPackageBuilder().Build(seed);
        Assert.Equal(entropy, package.Seed.Entropy);
        Assert.NotNull(package.World);
        Assert.NotEmpty(package.World.Towns);
        Assert.NotEmpty(package.World.Trails);
        Assert.NotEmpty(package.StartingInventory.Items);
        Assert.NotNull(package.CaseFile.OpeningLead);
    }

    [Fact]
    public void RandomSeedGenerationStaysWithinTheCanonicalEntropyRange()
    {
        for (var index = 0; index < 200; index++)
        {
            var seed = GameSetupSeedCodec.GenerateRandom(GameSetupOptionsV1.Default, TravelDifficulty.Normal);

            Assert.InRange(seed.Entropy, 0UL, GameSetupSeed.CanonicalEntropyMaximum);

            var encoded = GameSetupSeedCodec.Encode(seed);
            var decoded = GameSetupSeedCodec.Decode(encoded);

            Assert.True(decoded.Success);
            Assert.NotNull(decoded.Seed);
            Assert.Equal(seed.Entropy, decoded.Seed!.Entropy);
        }
    }

    [Fact]
    public void OutOfRangeEntropyIsRejectedRatherThanSilentlyNormalized()
    {
        var invalidSeed = new GameSetupSeed(
            GameSetupSeedCodec.CurrentGeneratorVersion,
            TravelDifficulty.Normal,
            GameSetupOptionsV1.Default,
            GameSetupSeed.CanonicalEntropyMaximum + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => GameSetupSeedCodec.Encode(invalidSeed));

        var decodeResult = GameSetupSeedCodec.Decode("WB1-N-03-1000000000000-0000");
        Assert.False(decodeResult.Success);
        Assert.Equal("Seed entropy is invalid.", decodeResult.ErrorMessage);
    }

    [Theory]
    [InlineData("WB1-N-03-12345Z789ABC-0000")]
    [InlineData("WB1-N-03-123456789AB-0000")]
    [InlineData("WB1-N-03-123456789ABCD-0000")]
    public void MalformedEntropySeedStringsFailDecode(string seedCode)
    {
        var decoded = GameSetupSeedCodec.Decode(seedCode);

        Assert.False(decoded.Success);
        Assert.Equal("Seed entropy is invalid.", decoded.ErrorMessage);
    }

    [Fact]
    public void SameSeedProducesSameSetupAndDifferentSeedsCanVary()
    {
        var sameSeedCode = GameSetupSeedCodec.Encode(
            GameSetupSeedCodec.WithOption(
                GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), WildBunch.Domain.Travel.TravelDifficulty.Hard),
                GameSetupOption.LoadoutProfile,
                (int)StartingLoadoutProfile.Stocked));

        var otherSeedCode = GameSetupSeedCodec.Encode(
            GameSetupSeedCodec.WithOption(
                GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), WildBunch.Domain.Travel.TravelDifficulty.Hard),
                GameSetupOption.LoadoutProfile,
                (int)StartingLoadoutProfile.Light));

        var factory = new SeededNewGameFactory();
        var first = factory.Create("Ranger Vale", WildBunch.Domain.Travel.TravelDifficulty.Normal, sameSeedCode);
        var second = factory.Create("Ranger Vale", WildBunch.Domain.Travel.TravelDifficulty.Easy, sameSeedCode);
        var other = factory.Create("Ranger Vale", WildBunch.Domain.Travel.TravelDifficulty.Normal, otherSeedCode);

        Assert.Equal(WildBunch.Domain.Travel.TravelDifficulty.Hard, first.TravelDifficulty);
        Assert.Equal(BuildSignature(first), BuildSignature(second));
        Assert.NotEqual(BuildSignature(first), BuildSignature(other));
    }

    [Fact]
    public void GeneratedSetupKeepsCulpritAndClueLinksValid()
    {
        var seedCode = GameSetupSeedCodec.Encode(
            GameSetupSeedCodec.WithOption(
                GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), WildBunch.Domain.Travel.TravelDifficulty.Easy),
                GameSetupOption.LoadoutProfile,
                (int)StartingLoadoutProfile.Light));

        var factory = new SeededNewGameFactory();
        var session = factory.Create("Ranger Vale", setupSeedCode: seedCode);

        Assert.Contains(session.CaseFile.Suspects, suspect => suspect.Id.Equals(session.CaseFile.TrueCulpritId));
        foreach (var clue in session.CaseFile.KnownClues.Concat(session.CaseFile.PublicClues))
        {
            foreach (var linkedSuspectId in clue.LinkedSuspectIds)
            {
                Assert.Contains(session.CaseFile.Suspects, suspect => suspect.Id.Equals(linkedSuspectId));
            }
        }
    }

    private static string BuildSignature(WildBunch.Domain.Game.GameSession session)
        => string.Join(
            "|",
            session.TravelDifficulty,
            session.Player.CurrentTownId.Value,
            session.Player.Wallet.Cash,
            string.Join(",", session.World.Towns.Select(town => $"{town.Id.Value}:{town.Services}:{town.Name}")),
            string.Join(",", session.World.Trails.Select(trail => $"{trail.Id.Value}:{trail.FromTownId.Value}:{trail.ToTownId.Value}:{trail.Risk}:{trail.Terrain}:{trail.WaterFeature}:{trail.RideDayDistance}")),
            string.Join(",", session.Player.Inventory.Items.Select(item => $"{item.Kind}:{item.Quantity}:{item.HorseState?.Hunger ?? -1}:{item.HorseState?.Thirst ?? -1}:{item.HorseState?.Exhaustion ?? -1}:{item.CanteenState?.Charges ?? -1}:{item.CanteenState?.Capacity ?? -1}")),
            string.Join(",", session.CaseFile.Suspects.Select(suspect => suspect.Id.Value)),
            session.CaseFile.TrueCulpritId.Value,
            string.Join(",", session.CaseFile.KnownClues.Select(clue => $"{clue.Id.Value}:{clue.Kind}:{clue.Description}:{string.Join("/", clue.LinkedSuspectIds.Select(id => id.Value))}")),
            string.Join(",", session.CaseFile.PublicClues.Select(clue => $"{clue.Id.Value}:{clue.Kind}:{clue.Description}:{string.Join("/", clue.LinkedSuspectIds.Select(id => id.Value))}")));
}
