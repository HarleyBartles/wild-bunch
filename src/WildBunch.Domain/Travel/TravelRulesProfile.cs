using WildBunch.Domain.Inventory;

namespace WildBunch.Domain.Travel;

/// <summary>
/// Tuning knobs for travel difficulty. Heat-increase fields
/// (<see cref="TrailEventHeatIncrease"/>,
/// <see cref="EncounterRunMountedHeatIncrease"/>,
/// <see cref="EncounterRunFootHeatIncrease"/>,
/// <see cref="EncounterFightHeatIncrease"/>) are **dead/reserved** under
/// the current heat model (ADR-0029): heat is lawman pressure from time
/// spent in town, not trail danger, and trail events and trail encounters
/// do not affect heat. No code reads these fields. They are retained as
/// reserved knobs for a future lawman-pressure system that may reintroduce
/// heat changes from noisy/witnessed incidents, but they have no effect
/// today and should not be interpreted as active trail-heat mechanics.
/// </summary>
public sealed record TravelRulesProfile(
    TravelDifficulty Difficulty,
    int CanteenCapacity,
    int HorseHungerDeathThreshold,
    int HorseThirstDeathThreshold,
    int HorseExhaustionLameThreshold,
    int HorseExhaustionDeathThreshold,
    decimal MountedRideDayProgress,
    decimal FootRideDayProgress,
    int LuckyTrailCoinReward,
    int LuckyTrailFoodReward,
    int LuckyTrailWaterRecovery,
    int BadLuckTrailDelayDays,
    int BadLuckTrailFoodLoss,
    int BadLuckTrailCanteenLoss,
    int BadLuckTrailHorseExhaustion,
    int BadLuckTrailHorseThirst,
    int TrailEventHeatIncrease,
    int EncounterRunMountedHeatIncrease,
    int EncounterRunMountedHorseExhaustion,
    int EncounterRunFootHeatIncrease,
    int EncounterRunFootHealthLoss,
    decimal EncounterBribeCash,
    int EncounterFightAmmoHealthLoss,
    int EncounterFightUnarmedHealthLoss,
    int EncounterFightHeatIncrease)
{
    public static TravelRulesProfile Default { get; } = new(
        TravelDifficulty.Normal,
        CanteenCapacity: 10,
        HorseHungerDeathThreshold: 3,
        HorseThirstDeathThreshold: 2,
        HorseExhaustionLameThreshold: 3,
        HorseExhaustionDeathThreshold: 5,
        MountedRideDayProgress: 1m,
        FootRideDayProgress: 0.5m,
        LuckyTrailCoinReward: 3,
        LuckyTrailFoodReward: 1,
        LuckyTrailWaterRecovery: 1,
        BadLuckTrailDelayDays: 1,
        BadLuckTrailFoodLoss: 1,
        BadLuckTrailCanteenLoss: 1,
        BadLuckTrailHorseExhaustion: 1,
        BadLuckTrailHorseThirst: 1,
        TrailEventHeatIncrease: 1,
        EncounterRunMountedHeatIncrease: 1,
        EncounterRunMountedHorseExhaustion: 1,
        EncounterRunFootHeatIncrease: 2,
        EncounterRunFootHealthLoss: 5,
        EncounterBribeCash: 5m,
        EncounterFightAmmoHealthLoss: 5,
        EncounterFightUnarmedHealthLoss: 10,
        EncounterFightHeatIncrease: 1);

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
                LuckyTrailCoinReward: 4,
                LuckyTrailFoodReward: 2,
                LuckyTrailWaterRecovery: 2,
                BadLuckTrailDelayDays: 1,
                BadLuckTrailFoodLoss: 0,
                BadLuckTrailCanteenLoss: 0,
                BadLuckTrailHorseExhaustion: 1,
                BadLuckTrailHorseThirst: 0,
                TrailEventHeatIncrease: 1,
                EncounterRunMountedHeatIncrease: 0,
                EncounterRunMountedHorseExhaustion: 1,
                EncounterRunFootHeatIncrease: 1,
                EncounterRunFootHealthLoss: 2,
                EncounterBribeCash: 3m,
                EncounterFightAmmoHealthLoss: 3,
                EncounterFightUnarmedHealthLoss: 6,
                EncounterFightHeatIncrease: 1),
            TravelDifficulty.Hard => new TravelRulesProfile(
                TravelDifficulty.Hard,
                CanteenCapacity: 1,
                HorseHungerDeathThreshold: 2,
                HorseThirstDeathThreshold: 2,
                HorseExhaustionLameThreshold: 2,
                HorseExhaustionDeathThreshold: 4,
                MountedRideDayProgress: 0.75m,
                FootRideDayProgress: 0.5m,
                LuckyTrailCoinReward: 2,
                LuckyTrailFoodReward: 1,
                LuckyTrailWaterRecovery: 1,
                BadLuckTrailDelayDays: 2,
                BadLuckTrailFoodLoss: 1,
                BadLuckTrailCanteenLoss: 1,
                BadLuckTrailHorseExhaustion: 2,
                BadLuckTrailHorseThirst: 1,
                TrailEventHeatIncrease: 2,
                EncounterRunMountedHeatIncrease: 2,
                EncounterRunMountedHorseExhaustion: 2,
                EncounterRunFootHeatIncrease: 3,
                EncounterRunFootHealthLoss: 8,
                EncounterBribeCash: 8m,
                EncounterFightAmmoHealthLoss: 7,
                EncounterFightUnarmedHealthLoss: 12,
                EncounterFightHeatIncrease: 2),
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
