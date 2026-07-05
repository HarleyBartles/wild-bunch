using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Seed-owned deterministic world/map layer decoded from the UUID seed code.
/// Owns generated world facts: world variant, town count, services palette,
/// prosperity palette, cluster count, graph density,
/// accusation/default culprit candidates, and seed-derived cash bonus.
///
/// The seed encodes only: variant, townCount, servicesPalette,
/// prosperityPalette, clusterCount, graphDensity, accusationIndex, defaultCulpritIndex, cashBonus.
/// Town names, selected town IDs, and per-town services are derived from the
/// encoded fields via a deterministic shuffle of the name pool — they are
/// flavor, not encoded state. This means the catalog can grow to any size
/// without increasing UUID bandwidth.
///
/// Trails are NOT seed-owned. MapGenerator generates the trail graph at game
/// setup time from the geometric placement, not from the seed codec.
///
/// Does NOT own selected difficulty, selected entropy, final starting town,
/// final horse/saddle/loadout, final health, or final resolved mystery truth
/// after entropy.
///
/// Design boundary:
/// - Starting town is NOT seed-owned. StartingTownPolicy validates the
///   player's start choice against the generated world.
/// - SeedWorld owns the candidate/generated map shape: how many towns, what
///   services each slot has (via palette), what prosperity each slot has
///   (via palette), the cluster structure (via ClusterCount), the graph
///   density (via GraphDensity).
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
    int ClusterCount,
    GraphDensity GraphDensity,
    int AccusationIndex,
    int DefaultCulpritIndex,
    int CashBonus,
    int OutlierSlotType) // 0=no outlier, 1=simple outlier, 2-3 reserved
{
    public string SeedCodeText => SeedCode.ToString("D");

    /// <summary>
    /// Whether this seed world is the canonical shape (8 towns,
    /// Canonical variant, HubTelegraph services, UniformProsperous prosperity,
    /// single cluster, Sparse graph density, accusation index 1,
    /// default culprit index 3, zero cash bonus). Used by GameSetupResolver
    /// to select the canonical case file path.
    /// </summary>
    public bool IsCanonical =>
        WorldVariant == SeedWorldVariant.Canonical
            && TownCount == 8
            && ServicesPalette == ServicesPalette.HubTelegraph
            && ProsperityPalette == ProsperityPalette.UniformProsperous
            && ClusterCount == 1
            && GraphDensity == GraphDensity.Sparse
            && AccusationIndex == 1
            && DefaultCulpritIndex == 3
            && CashBonus == 0;

    /// <summary>
    /// Derives the selected town IDs for this seed world by running the
    /// deterministic name shuffle. This is a derived view, not encoded state.
    /// </summary>
    public IReadOnlyList<string> GetSelectedTownIds()
        => SeedWorldFactory.DeriveTownNames(
            WorldVariant, TownCount, AccusationIndex, DefaultCulpritIndex,
            CashBonus, ProsperityPalette, ServicesPalette)
            .Select(t => t.Id)
            .ToArray();
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
