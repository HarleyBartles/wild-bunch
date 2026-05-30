using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

internal enum GameSetupOption
{
    StartWithHorse = 0,
    LoadoutProfile = 1
}

internal enum StartingLoadoutProfile
{
    Standard = 0,
    Light = 1,
    Stocked = 2
}

internal sealed record GameSetupOptionsV1(
    bool StartWithHorse = true,
    StartingLoadoutProfile LoadoutProfile = StartingLoadoutProfile.Standard)
{
    public static GameSetupOptionsV1 Default { get; } = new();
}

internal sealed record GameSetupSeed(
    int GeneratorVersion,
    TravelDifficulty Difficulty,
    GameSetupOptionsV1 Options,
    ulong Entropy)
{
    public bool IsCanonical => GeneratorVersion == GameSetupSeedCodec.CurrentGeneratorVersion
        && Options == GameSetupOptionsV1.Default
        && Entropy == 0;
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
