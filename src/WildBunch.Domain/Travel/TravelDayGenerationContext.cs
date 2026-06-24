using TownId = WildBunch.Domain.World.TownId;
using TrailRisk = WildBunch.Domain.World.TrailRisk;
using TrailTerrain = WildBunch.Domain.World.TrailTerrain;
using WaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Domain.Travel;

public enum TravelPressureBand
{
    None = 0,
    Low = 1,
    Moderate = 2,
    High = 3,
    Critical = 4
}

public enum HorseConditionBand
{
    None = 0,
    Sound = 1,
    Worn = 2,
    Lame = 3,
    Critical = 4
}

/// <summary>
/// Banded view of <see cref="WildBunch.Domain.Game.PursuitState.Heat"/> used
/// by the travel day plan generator and encounter engine. Higher bands mean
/// more lawman attention is following the player, which draws tougher/more-
/// greedy trail foes. This is future lawman pressure, not trail danger.
/// See ADR-0029.
/// </summary>
public enum PursuitHeatBand
{
    Calm = 0,
    Wary = 1,
    Hot = 2,
    Hunted = 3
}

public enum WalletBand
{
    Broke = 0,
    Tight = 1,
    Steady = 2,
    Comfortable = 3,
    Flush = 4
}

public sealed record TravelDayGenerationContext(
    int GeneratorVersion,
    string? GameSeed,
    string? ScenarioProfileId,
    string TrailId,
    TownId OriginTownId,
    TownId DestinationTownId,
    int DayNumber,
    TravelMode TravelMode,
    TrailRisk Risk,
    TrailTerrain Terrain,
    WaterFeature WaterFeature,
    TravelDifficulty Difficulty,
    int RemainingDays,
    decimal RemainingRideDayDistance,
    TravelPressureBand FoodPressure,
    TravelPressureBand CanteenPressure,
    TravelPressureBand HorseFeedPressure,
    HorseConditionBand HorseConditionBand,
    PursuitHeatBand PursuitHeatBand,
    WalletBand WalletBand,
    IReadOnlyList<JourneyTrailEventKind> RecentTrailEventKinds,
    IReadOnlyList<JourneyTrailEventId> RecentTrailEventIds,
    IReadOnlyList<TravelDayEncounterCategory> RecentEncounterCategories,
    bool HasHorse,
    TravelRandomnessMode RandomnessMode,
    string RandomnessSalt)
{
    public bool WaterSecure => WaterFeature is WaterFeature.Creek or WaterFeature.River or WaterFeature.Spring;

    public bool IsMounted => TravelMode == TravelMode.Mounted;
}
