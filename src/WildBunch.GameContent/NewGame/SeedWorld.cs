namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Seed-owned deterministic world/map layer decoded from the UUID seed code.
/// Owns generated world facts: world variant, town set key, accusation/default
/// culprit candidates, and seed-derived cash bonus.
/// Does NOT own selected difficulty, selected entropy, final starting town,
/// final horse/saddle/loadout, final health, or final resolved mystery truth
/// after entropy.
/// </summary>
public sealed record SeedWorld(
    Guid SeedCode,
    SeedWorldVariant WorldVariant,
    string TownSetKey,
    int AccusationIndex,
    int DefaultCulpritIndex,
    int CashBonus)
{
    public string SeedCodeText => SeedCode.ToString("D");
}

internal sealed record SeedWorldValidationResult(
    bool Success,
    string? ErrorMessage)
{
    public static SeedWorldValidationResult Ok()
        => new(true, null);

    public static SeedWorldValidationResult Failed(string errorMessage)
        => new(false, errorMessage);
}
