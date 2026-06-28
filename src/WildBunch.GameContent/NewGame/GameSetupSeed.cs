using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

public enum StartingLoadoutProfile
{
    Standard = 0,
    Light = 1,
    Stocked = 2
}

public sealed record StartingWorldDescriptor(
    Guid SeedCode,
    GameDifficulty GameDifficulty,
    GameEntropy GameEntropy,
    StartingWorldDescriptorWorld World,
    StartingWorldDescriptorPlayer Player,
    StartingWorldDescriptorCase Case)
{
    public string SeedCodeText => SeedCode.ToString("D");
}

public sealed record StartingWorldDescriptorWorld(
    SeedWorldVariant Variant,
    string StartingTownSelectionKey);

public sealed record StartingWorldDescriptorPlayer(
    bool StartWithHorse,
    StartingLoadoutProfile LoadoutProfile,
    decimal StartingCash,
    StartingWorldDescriptorLoadout Loadout);

public sealed record StartingWorldDescriptorLoadout(
    int Food,
    int HorseFeed,
    int RevolverAmmo,
    bool IncludeHorse,
    bool IncludeSaddle);

public sealed record StartingWorldDescriptorCase(
    int AccusationIndex);

internal sealed record StartingWorldDescriptorValidationResult(
    bool Success,
    string? ErrorMessage)
{
    public static StartingWorldDescriptorValidationResult Ok()
        => new(true, null);

    public static StartingWorldDescriptorValidationResult Failed(string errorMessage)
        => new(false, errorMessage);
}
