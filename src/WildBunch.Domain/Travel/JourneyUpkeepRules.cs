using WildBunch.Domain.Inventory;
using TrailTerrain = WildBunch.Domain.World.TrailTerrain;
using WaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Domain.Travel;

public sealed record JourneyDailyUpkeepResult(
    HorseTravelState? HorseState,
    CanteenState? CanteenState,
    int HorseFeedConsumed,
    bool MountedTravelLost);

public static class JourneyUpkeepRules
{
    public static bool HasGrazing(TrailTerrain terrain)
        => terrain is TrailTerrain.OpenRange or TrailTerrain.Hills;

    public static bool HasRouteWater(WaterFeature waterFeature)
        => waterFeature is WaterFeature.Creek or WaterFeature.River or WaterFeature.Spring;

    public static int ExhaustionIncrease(TrailTerrain terrain)
        => terrain switch
        {
            TrailTerrain.OpenRange => 0,
            TrailTerrain.Hills => 1,
            TrailTerrain.Badlands => 1,
            TrailTerrain.Mountains => 2,
            _ => 1
        };

    public static int WaterChargesRequiredPerDay(HorseTravelState? horseState, TravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= TravelRulesProfile.Default;
        return horseState is not null && !horseState.IsDeadFor(travelRulesProfile) ? 2 : 1;
    }

    public static JourneyDailyUpkeepResult ApplyDailyUpkeep(
        TrailTerrain terrain,
        WaterFeature waterFeature,
        HorseTravelState? horseState,
        CanteenState? canteenState,
        int horseFeedAvailable,
        TravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= TravelRulesProfile.Default;
        var grazingAvailable = HasGrazing(terrain);
        var routeWaterSecure = HasRouteWater(waterFeature);
        var nextHorseState = horseState;
        var nextCanteenState = canteenState;
        var horseFeedConsumed = 0;
        var livingHorse = horseState is not null && !horseState.IsDeadFor(travelRulesProfile);

        if (livingHorse)
        {
            nextHorseState = grazingAvailable
                ? horseState!.RecoverHunger(1)
                : horseFeedAvailable > 0
                    ? horseState!.RecoverHunger(1)
                    : horseState!.IncreaseHunger(1);

            if (routeWaterSecure)
            {
                nextHorseState = nextHorseState.RecoverThirst(1);
            }
            else if (nextCanteenState?.Charges >= 2)
            {
                nextCanteenState = nextCanteenState.Consume(2);
                nextHorseState = nextHorseState.RecoverThirst(1);
            }
            else if (nextCanteenState?.Charges >= 1)
            {
                nextCanteenState = nextCanteenState.Consume(1);
                nextHorseState = nextHorseState.IncreaseThirst(1);
            }
            else
            {
                nextHorseState = nextHorseState.IncreaseThirst(1);
            }

            nextHorseState = nextHorseState.IncreaseExhaustion(ExhaustionIncrease(terrain));

            if (!grazingAvailable && horseFeedAvailable > 0)
            {
                horseFeedConsumed = 1;
            }
        }
        else if (!routeWaterSecure && nextCanteenState?.Charges >= 1)
        {
            nextCanteenState = nextCanteenState.Consume(1);
        }

        return new JourneyDailyUpkeepResult(
            nextHorseState,
            nextCanteenState,
            horseFeedConsumed,
            livingHorse && nextHorseState is not null && !nextHorseState.CanProvideMountedTravelFor(travelRulesProfile));
    }
}
