using WildBunch.Domain.Inventory;

namespace WildBunch.Domain.Travel;

public sealed record TravelDiaryDayState(
    int DayNumber,
    string OriginTownName,
    string DestinationTownName,
    TravelMode StartingTravelMode,
    TravelMode EndingTravelMode,
    JourneyStatus Status,
    decimal StartingRideDayDistance,
    decimal RemainingRideDayDistance,
    int StartingDaysRemaining,
    int RemainingDays,
    HorseTravelState? HorseStateBefore,
    HorseTravelState? HorseStateAfter,
    JourneyTrailEventState? TrailEvent,
    JourneyEncounterState? PendingEncounter,
    TravelDiaryEncounterResolutionState? EncounterResolution,
    string? OpeningNarration,
    string? JourneyBeat,
    string? ResourceBeat,
    int HealthDelta,
    decimal WalletDelta,
    int FoodDelta,
    int HorseFeedDelta,
    int CanteenChargeDelta,
    int AmmoSpent,
    int HorseHungerDelta,
    int HorseThirstDelta,
    int HorseExhaustionDelta,
    int DelayDays,
    int HeatIncrease,
    IReadOnlyList<string> Warnings);

public sealed record TravelDiaryEncounterResolutionState(
    string ChoiceId,
    string ChoiceLabel,
    int HealthDelta,
    decimal WalletDelta,
    int AmmoSpent,
    int HeatIncrease,
    int HorseExhaustionDelta,
    bool ContinuedOnFoot);
