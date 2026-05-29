using WildBunch.Domain.Inventory;

namespace WildBunch.Domain.Travel;

public sealed record TravelRulesProfile(
    TravelDifficulty Difficulty,
    int CanteenCapacity,
    int HorseHungerDeathThreshold,
    int HorseThirstDeathThreshold,
    int HorseExhaustionLameThreshold,
    int HorseExhaustionDeathThreshold,
    decimal MountedRideDayProgress,
    decimal FootRideDayProgress,
    int FirstEncounterDay,
    int FirstTrailEventDay)
{
    public static TravelRulesProfile Default { get; } = new(
        TravelDifficulty.Normal,
        CanteenCapacity: 2,
        HorseHungerDeathThreshold: 3,
        HorseThirstDeathThreshold: 2,
        HorseExhaustionLameThreshold: 3,
        HorseExhaustionDeathThreshold: 5,
        MountedRideDayProgress: 1m,
        FootRideDayProgress: 0.5m,
        FirstEncounterDay: 1,
        FirstTrailEventDay: 1);

    public static TravelRulesProfile For(TravelDifficulty difficulty)
        => difficulty switch
        {
            TravelDifficulty.Normal => Default,
            TravelDifficulty.Easy => new TravelRulesProfile(
                TravelDifficulty.Easy,
                CanteenCapacity: 10,
                HorseHungerDeathThreshold: 4,
                HorseThirstDeathThreshold: 3,
                HorseExhaustionLameThreshold: 4,
                HorseExhaustionDeathThreshold: 7,
                MountedRideDayProgress: 1.5m,
                FootRideDayProgress: 0.75m,
                FirstEncounterDay: 1,
                FirstTrailEventDay: 1),
            TravelDifficulty.Hard => new TravelRulesProfile(
                TravelDifficulty.Hard,
                CanteenCapacity: 1,
                HorseHungerDeathThreshold: 2,
                HorseThirstDeathThreshold: 2,
                HorseExhaustionLameThreshold: 2,
                HorseExhaustionDeathThreshold: 4,
                MountedRideDayProgress: 0.75m,
                FootRideDayProgress: 0.5m,
                FirstEncounterDay: 1,
                FirstTrailEventDay: 1),
            _ => Default
        };

    public bool IsHorseDead(HorseTravelState horseState)
        => horseState.Hunger >= HorseHungerDeathThreshold
            || horseState.Thirst >= HorseThirstDeathThreshold
            || horseState.Exhaustion >= HorseExhaustionDeathThreshold;

    public bool IsHorseLame(HorseTravelState horseState)
        => !IsHorseDead(horseState) && horseState.Exhaustion >= HorseExhaustionLameThreshold;

    public bool CanProvideMountedTravel(HorseTravelState? horseState)
        => horseState is not null && !IsHorseDead(horseState) && !IsHorseLame(horseState);
}
