using WildBunch.Domain.World;

namespace WildBunch.Domain.Travel;

internal static class TrailEventCatalog
{
    public static JourneyTrailEventState? TryCreate(TravelJourney journey, TravelRulesProfile travelRulesProfile)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(travelRulesProfile);

        var routeProfile = journey.Preview.RouteProfile;

        if (routeProfile.Risk == TrailRisk.Low && routeProfile.WaterFeature == WaterFeature.Creek)
        {
            return JourneyTrailEventState.CreateLucky(
                JourneyTrailEventId.LuckyCoinCache,
                "Hidden coin cache",
                $"I spotted a hidden cache of trail coins and pocketed an extra ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                walletDelta: travelRulesProfile.LuckyTrailCoinReward);
        }

        if (travelRulesProfile.Difficulty == GameDifficulty.Easy && routeProfile.Risk == TrailRisk.Low && routeProfile.Terrain == TrailTerrain.OpenRange && routeProfile.WaterFeature == WaterFeature.None)
        {
            return JourneyTrailEventState.CreateLucky(
                JourneyTrailEventId.LuckyFoodCache,
                "Trail grub cache",
                $"I found a cache of jerky and trail biscuits and gained {travelRulesProfile.LuckyTrailFoodReward} food.",
                foodDelta: travelRulesProfile.LuckyTrailFoodReward);
        }

        if (travelRulesProfile.Difficulty == GameDifficulty.Easy && routeProfile.WaterFeature == WaterFeature.None && routeProfile.Terrain is TrailTerrain.Hills or TrailTerrain.Badlands)
        {
            return JourneyTrailEventState.CreateLucky(
                JourneyTrailEventId.LuckyWaterSeep,
                "Hidden water seep",
                $"I found a seep under the rocks and topped off my canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                canteenChargeDelta: travelRulesProfile.LuckyTrailWaterRecovery);
        }

        if (routeProfile.Risk == TrailRisk.Moderate && routeProfile.WaterFeature == WaterFeature.Spring)
        {
            return JourneyTrailEventState.CreateBadLuck(
                JourneyTrailEventId.BadLuckWashout,
                "Washed-out trail",
                $"A washout forced a detour and cost me {travelRulesProfile.BadLuckTrailDelayDays} extra delay day(s).",
                delayDays: travelRulesProfile.BadLuckTrailDelayDays);
        }

        if (travelRulesProfile.Difficulty == GameDifficulty.Hard && routeProfile.Terrain == TrailTerrain.Badlands && routeProfile.WaterFeature == WaterFeature.None && routeProfile.Risk != TrailRisk.High && journey.FoodRemaining > 0 && journey.AvailableCanteenCharges > 0)
        {
            return JourneyTrailEventState.CreateBadLuck(
                JourneyTrailEventId.BadLuckFoodLoss,
                "Dust-choked outfit",
                $"A dust storm stripped away {travelRulesProfile.BadLuckTrailFoodLoss} food and {travelRulesProfile.BadLuckTrailCanteenLoss} canteen charge(s).",
                foodDelta: -travelRulesProfile.BadLuckTrailFoodLoss,
                canteenChargeDelta: -travelRulesProfile.BadLuckTrailCanteenLoss,
                horseThirstDelta: travelRulesProfile.BadLuckTrailHorseThirst,
                delayDays: travelRulesProfile.BadLuckTrailDelayDays);
        }

        if (travelRulesProfile.Difficulty == GameDifficulty.Hard && journey.TravelMode == TravelMode.Mounted && journey.HorseState is not null && routeProfile.Terrain == TrailTerrain.Hills && routeProfile.WaterFeature == WaterFeature.River)
        {
            return JourneyTrailEventState.CreateBadLuck(
                JourneyTrailEventId.BadLuckSpookedHorse,
                "Spooked horse",
                "A sudden canyon echo spooked the horse and left it more exhausted.",
                horseExhaustionDelta: travelRulesProfile.BadLuckTrailHorseExhaustion);
        }

        return null;
    }
}
