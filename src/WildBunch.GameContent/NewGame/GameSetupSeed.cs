using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

internal enum GameSetupOption
{
    StartWithHorse = 0,
    LoadoutProfile = 1,
    JourneyRandomness = 2
}

internal enum StartingLoadoutProfile
{
    Standard = 0,
    Light = 1,
    Stocked = 2
}

internal sealed record GameSetupOptionsV1(
    bool StartWithHorse = true,
    StartingLoadoutProfile LoadoutProfile = StartingLoadoutProfile.Standard,
    TravelRandomnessMode JourneyRandomnessMode = TravelRandomnessMode.RuntimeSalted)
{
    public static GameSetupOptionsV1 Default { get; } = new();
}

internal sealed record GameSetupSeed(
    int GeneratorVersion,
    TravelDifficulty Difficulty,
    GameSetupOptionsV1 Options,
    ulong Entropy)
{
    public const ulong CanonicalEntropyMaximum = 0xFFFFFFFFFFFFUL;

    public bool IsCanonical => GeneratorVersion == GameSetupSeedCodec.CurrentGeneratorVersion
        && Options == GameSetupOptionsV1.Default
        && Entropy == 0;

    public bool IsCanonicalEntropy => Entropy <= CanonicalEntropyMaximum;
}

internal sealed record GameSetupSeedDecodeResult(
    bool Success,
    GameSetupSeed? Seed,
    string? ErrorMessage)
{
    public static GameSetupSeedDecodeResult Ok(GameSetupSeed seed)
        => new(true, seed, null);

    public static GameSetupSeedDecodeResult Failed(string errorMessage)
        => new(false, null, errorMessage);
}
