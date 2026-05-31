using WildBunch.Domain.Inventory;
using TownId = WildBunch.Domain.World.TownId;
using TrailRisk = WildBunch.Domain.World.TrailRisk;
using TrailTerrain = WildBunch.Domain.World.TrailTerrain;
using WaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Domain.Travel;

public enum TravelMode
{
    Mounted = 0,
    Foot = 1
}

public enum JourneyStatus
{
    Active = 0,
    Interrupted = 1,
    Completed = 2,
    Failed = 3
}

public sealed record TravelRouteProfile(
    string TrailId,
    TrailRisk Risk,
    TrailTerrain Terrain,
    WaterFeature WaterFeature,
    decimal RideDayDistance,
    decimal MountedRideDayProgress,
    decimal FootRideDayProgress,
    IReadOnlyList<string> Warnings)
{
    public int ExpectedDays(TravelMode mode)
        => CalculateRemainingDays(RideDayDistance, mode);

    public decimal DailyRideDayProgress(TravelMode mode)
        => mode == TravelMode.Mounted ? MountedRideDayProgress : FootRideDayProgress;

    public int CalculateRemainingDays(decimal remainingRideDayDistance, TravelMode mode)
    {
        if (remainingRideDayDistance <= 0)
        {
            return 0;
        }

        var dailyProgress = DailyRideDayProgress(mode);
        return Math.Max(1, (int)decimal.Ceiling(remainingRideDayDistance / dailyProgress));
    }
}

public sealed record TravelPreview(
    TownId OriginTownId,
    TownId DestinationTownId,
    string OriginTownName,
    string DestinationTownName,
    TravelRouteProfile RouteProfile,
    TravelMode TravelMode,
    bool MountedTravelAvailable,
    bool WaterSecure,
    decimal RideDayDistance,
    decimal RemainingRideDayDistance,
    int BaselineRideDays,
    int ExpectedDays,
    int RemainingDays,
    int CanteenChargesPerDay,
    int RequiredCanteenCharges,
    int AvailableCanteenCharges,
    int CanteenReserveCharges,
    int DelayMarginDays,
    bool DelayRisk,
    int RequiredFood,
    int AvailableFood,
    int RequiredHorseFeed,
    int AvailableHorseFeed,
    HorseTravelState? HorseState,
    IReadOnlyList<string> Warnings)
{
    public TravelJourney ToJourney()
        => new TravelJourney(this, 1);
}

public sealed record TravelJourneySnapshot(
    int JourneySequence,
    TownId OriginTownId,
    TownId DestinationTownId,
    string OriginTownName,
    string DestinationTownName,
    TravelRouteProfile RouteProfile,
    TravelMode TravelMode,
    JourneyStatus Status,
    bool MountedTravelAvailable,
    bool WaterSecure,
    decimal RideDayDistance,
    decimal RemainingRideDayDistance,
    int ExpectedDays,
    int RemainingDays,
    int CanteenChargesPerDay,
    int RequiredCanteenCharges,
    int AvailableCanteenCharges,
    int CanteenReserveCharges,
    int DelayMarginDays,
    bool DelayRisk,
    int RequiredFood,
    int AvailableFood,
    int RequiredHorseFeed,
    int AvailableHorseFeed,
    HorseTravelState? HorseState,
    string? OpeningNarration,
    int DaysTravelled,
    int DelayDays,
    TravelDayPlanState? CurrentDayPlan,
    JourneyEncounterState? PendingEncounter,
    IReadOnlyList<string> Warnings);
