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
    IReadOnlyList<string> Entries,
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

public enum TravelDayEncounterCategory
{
    Quiet = 0,
    Lucky = 1,
    Unlucky = 2,
    Foe = 3,
    Npc = 4,
    Environmental = 5,
    Resource = 6,
    HorseTrouble = 7
}

public sealed record TravelDayEncounterState(
    int EncounterIndex,
    TravelDayEncounterCategory Category,
    string Title,
    string Message,
    JourneyTrailEventState? TrailEvent,
    JourneyEncounterState? PendingEncounter,
    TravelDiaryEncounterResolutionState? Resolution)
{
    public bool RequiresChoice => PendingEncounter is not null && Resolution is null;

    public bool IsQuiet => Category == TravelDayEncounterCategory.Quiet;
}

public sealed record TravelDayPlanState(
    int DayNumber,
    IReadOnlyList<TravelDayEncounterState> Encounters,
    int CurrentEncounterIndex,
    bool IsComplete)
{
    public TravelDayEncounterState? CurrentEncounter
        => CurrentEncounterIndex >= 0 && CurrentEncounterIndex < Encounters.Count
            ? Encounters[CurrentEncounterIndex]
            : null;

    public bool HasPendingChoice => CurrentEncounter?.RequiresChoice ?? false;
}
