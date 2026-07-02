using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Seed-owned deterministic world/map layer decoded from the UUID seed code.
/// Owns generated world facts: world variant, town count, services palette,
/// prosperity palette, map layout palette, trail graph with baseline terrain/water/distance,
/// accusation/default culprit candidates, and seed-derived cash bonus.
///
/// The seed encodes only: variant, townCount, servicesPalette,
/// prosperityPalette, mapLayoutPalette, accusationIndex, defaultCulpritIndex, cashBonus.
/// Town names are derived from the encoded fields via a deterministic
/// shuffle of the name pool — they are flavor, not encoded state. This
/// means the catalog can grow to any size without increasing UUID bandwidth.
///
/// Does NOT own selected difficulty, selected entropy, final starting town,
/// final horse/saddle/loadout, final health, or final resolved mystery truth
/// after entropy.
///
/// Design boundary:
/// - Starting town is NOT seed-owned. StartingTownPolicy validates the
///   player's start choice against the generated world.
/// - SeedWorld owns the candidate/generated map: how many towns, what
///   services each slot has (via palette), what prosperity each slot has
///   (via palette), the map layout (via palette), and the trail graph between slots.
/// - Same seed + same difficulty should produce the same resolved map.
/// - Difficulty may later influence map pressure/layout realization
///   downstream of the seed codec, not by hiding difficulty inside the seed.
/// </summary>
public sealed record SeedWorld(
    Guid SeedCode,
    SeedWorldVariant WorldVariant,
    int TownCount,
    ServicesPalette ServicesPalette,
    ProsperityPalette ProsperityPalette,
    MapLayoutPalette MapLayoutPalette,
    int AccusationIndex,
    int DefaultCulpritIndex,
    int CashBonus,
    IReadOnlyList<string> SelectedTownIds,
    IReadOnlyDictionary<string, TownServices> TownServices,
    IReadOnlyList<SeedWorldTrail> Trails,
    int OutlierSlotType) // 0=no outlier, 1=simple outlier, 2-3 reserved
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
