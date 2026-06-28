using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Seed-owned deterministic world/map layer decoded from the UUID seed code.
/// Owns generated world facts: world variant, selected town IDs, trail graph
/// with baseline terrain/water/distance, accusation/default culprit
/// candidates, and seed-derived cash bonus.
/// Does NOT own selected difficulty, selected entropy, final starting town,
/// final horse/saddle/loadout, final health, or final resolved mystery truth
/// after entropy.
///
/// Design boundary:
/// - Starting town is NOT seed-owned. StartingTownPolicy validates the
///   player's start choice against the generated world.
/// - SeedWorld owns the candidate/generated map: which towns are selected
///   and the trail graph between them with default terrain/water/distance.
/// - Same seed + same difficulty should produce the same resolved map.
/// - Difficulty may later influence map pressure/layout realization
///   (distance bands, terrain harshness, connectivity constraints)
///   downstream of the seed codec, not by hiding difficulty inside the seed.
/// - Longer term, SeedWorld + DifficultyEnvelope may produce the final
///   resolved world/map, while StartingTownPolicy validates the player's
///   start choice against that world.
/// </summary>
public sealed record SeedWorld(
    Guid SeedCode,
    SeedWorldVariant WorldVariant,
    IReadOnlyList<string> SelectedTownIds,
    IReadOnlyList<SeedWorldTrail> Trails,
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
