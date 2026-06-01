using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

internal enum AdventureRandomnessPolicy
{
    Boring = 0,
    Standard = 1,
    Adventurous = 2,
    Wild = 3
}

internal enum StartingLoadoutProfile
{
    Standard = 0,
    Light = 1,
    Stocked = 2
}

internal sealed record StartingWorldDescriptor(
    Guid SeedCode,
    TravelDifficulty Difficulty,
    AdventureRandomnessPolicy AdventureRandomnessPolicy,
    StartingWorldDescriptorWorld World,
    StartingWorldDescriptorPlayer Player,
    StartingWorldDescriptorCase Case)
{
    public string SeedCodeText => SeedCode.ToString("D");
}

internal sealed record StartingWorldDescriptorWorld(
    SeedWorldVariant Variant,
    string StartingTownSelectionKey);

internal sealed record StartingWorldDescriptorPlayer(
    bool StartWithHorse,
    StartingLoadoutProfile LoadoutProfile,
    decimal StartingCash,
    StartingWorldDescriptorLoadout Loadout);

internal sealed record StartingWorldDescriptorLoadout(
    int Food,
    int HorseFeed,
    int RevolverAmmo,
    bool IncludeHorse,
    bool IncludeSaddle);

internal sealed record StartingWorldDescriptorCase(
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
