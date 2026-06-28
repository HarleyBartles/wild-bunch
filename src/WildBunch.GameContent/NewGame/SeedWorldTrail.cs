using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// A trail definition held in the <see cref="SeedWorld"/> template.
/// Carries the seed-owned default terrain, water feature, and baseline
/// ride-day distance. Difficulty may later modify these values
/// downstream of the seed codec (e.g. terrain harshness, distance bands).
/// </summary>
public sealed record SeedWorldTrail(
    string Id,
    string FromTownId,
    string ToTownId,
    TrailRisk Risk,
    TrailTerrain Terrain,
    WaterFeature WaterFeature,
    decimal RideDayDistance);
