using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;

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
        => Math.Clamp(CalculateRemainingDays(RideDayDistance, mode), 2, 6);

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
        => new TravelJourney(this);
}

public sealed record TravelJourneySnapshot(
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

public sealed record JourneyEncounterChoiceState(string Id, string Label);

public sealed record JourneyEncounterState(
    string Kind,
    string Message,
    IReadOnlyList<JourneyEncounterChoiceState> Choices)
{

    public static JourneyEncounterState CreateFoe(string message)
        => new(
            "foe",
            message,
            new[]
            {
                new JourneyEncounterChoiceState("run", "Run"),
                new JourneyEncounterChoiceState("fight", "Fight"),
                new JourneyEncounterChoiceState("bribe", "Bribe")
            });

    public static JourneyEncounterState CreateChoiceEncounter(
        string kind,
        string message,
        IReadOnlyList<JourneyEncounterChoiceState>? choices = null)
        => new(
            kind,
            message,
            choices ?? new[]
            {
                new JourneyEncounterChoiceState("run", "Run"),
                new JourneyEncounterChoiceState("fight", "Fight"),
                new JourneyEncounterChoiceState("bribe", "Bribe")
            });
}

public enum JourneyTrailEventKind
{
    Lucky = 0,
    BadLuck = 1
}

public enum JourneyTrailEventId
{
    LuckyCoinCache = 0,
    LuckyFoodCache = 1,
    LuckyWaterSeep = 2,
    BadLuckWashout = 3,
    BadLuckFoodLoss = 4,
    BadLuckDustStorm = 5,
    BadLuckSpookedHorse = 6
}

public sealed record JourneyTrailEventState(
    JourneyTrailEventId Id,
    JourneyTrailEventKind Kind,
    string Title,
    string Message,
    decimal WalletDelta,
    int FoodDelta,
    int CanteenChargeDelta,
    int HorseHungerDelta,
    int HorseThirstDelta,
    int HorseExhaustionDelta,
    int DelayDays,
    int HeatIncrease)
{
    public static JourneyTrailEventState CreateLucky(
        JourneyTrailEventId id,
        string title,
        string message,
        decimal walletDelta = 0m,
        int foodDelta = 0,
        int canteenChargeDelta = 0)
        => new(
            id,
            JourneyTrailEventKind.Lucky,
            title,
            message,
            WalletDelta: walletDelta,
            FoodDelta: foodDelta,
            CanteenChargeDelta: canteenChargeDelta,
            HorseHungerDelta: 0,
            HorseThirstDelta: 0,
            HorseExhaustionDelta: 0,
            DelayDays: 0,
            HeatIncrease: 0);

    public static JourneyTrailEventState CreateBadLuck(
        JourneyTrailEventId id,
        string title,
        string message,
        int foodDelta = 0,
        int canteenChargeDelta = 0,
        int horseHungerDelta = 0,
        int horseThirstDelta = 0,
        int horseExhaustionDelta = 0,
        int delayDays = 0,
        int heatIncrease = 0)
        => new(
            id,
            JourneyTrailEventKind.BadLuck,
            title,
            message,
            WalletDelta: 0m,
            FoodDelta: foodDelta,
            CanteenChargeDelta: canteenChargeDelta,
            HorseHungerDelta: horseHungerDelta,
            HorseThirstDelta: horseThirstDelta,
            HorseExhaustionDelta: horseExhaustionDelta,
            DelayDays: delayDays,
            HeatIncrease: heatIncrease);
}

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

internal static partial class TravelDayPlanGenerator
{
    private static readonly JourneyEncounterChoiceState[] DefaultEncounterChoices =
    {
        new("run", "Run"),
        new("fight", "Fight"),
        new("bribe", "Bribe")
    };

    public static TravelDayPlanState Generate(TravelJourney journey, TravelRulesProfile travelRulesProfile)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(travelRulesProfile);

        var dayNumber = journey.DaysTravelled;
        var baseSeed = ComposeSeed(journey, travelRulesProfile, dayNumber);
        var dayRoll = Roll(baseSeed, "day");
        var quietDay = dayRoll % 6 == 0;

        var encounterCountRoll = Roll(baseSeed, "count");
        var encounterCount = journey.Preview.RouteProfile.Risk switch
        {
            TrailRisk.High => 1,
            TrailRisk.Moderate => 1 + (int)(encounterCountRoll % 2),
            _ => 1
        };

        var encounters = new List<TravelDayEncounterState>(encounterCount);
        for (var slot = 0; slot < encounterCount; slot++)
        {
            var slotSeed = ComposeSeed(baseSeed, $"slot:{slot}");
            var category = SelectCategory(journey, travelRulesProfile, Roll(slotSeed, "category"), slot, quietDay);
            encounters.Add(CreateEncounter(journey, travelRulesProfile, dayNumber, slot, category, slotSeed));
        }

        return new TravelDayPlanState(dayNumber, encounters, CurrentEncounterIndex: 0, IsComplete: false);
    }

    private static string ComposeSeed(TravelJourney journey, TravelRulesProfile travelRulesProfile, int dayNumber)
        => string.Join(
            "|",
            journey.Preview.RouteProfile.TrailId,
            journey.Preview.OriginTownId.Value,
            journey.Preview.DestinationTownId.Value,
            journey.Preview.TravelMode,
            journey.Preview.RouteProfile.Risk,
            journey.Preview.RouteProfile.Terrain,
            journey.Preview.RouteProfile.WaterFeature,
            travelRulesProfile.Difficulty,
            dayNumber,
            journey.RemainingDays,
            journey.RemainingRideDayDistance,
            journey.AvailableCanteenCharges,
            journey.FoodRemaining,
            journey.HorseFeedRemaining,
            journey.HorseState?.Hunger ?? -1,
            journey.HorseState?.Thirst ?? -1,
            journey.HorseState?.Exhaustion ?? -1);

    private static string ComposeSeed(string seed, string suffix)
        => $"{seed}|{suffix}";

    private static ulong Roll(string seed, string label)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}|{label}"));
        return BitConverter.ToUInt64(bytes, 0);
    }

    private static TravelDayEncounterCategory SelectCategory(TravelJourney journey, TravelRulesProfile travelRulesProfile, ulong roll, int slotIndex, bool quietDay)
    {
        var routeProfile = journey.Preview.RouteProfile;

        if (routeProfile.Risk == TrailRisk.High)
        {
            return TravelDayEncounterCategory.Foe;
        }

        if (routeProfile.Risk == TrailRisk.Moderate && routeProfile.Terrain == TrailTerrain.Hills && routeProfile.WaterFeature == WaterFeature.River)
        {
            return TravelDayEncounterCategory.Quiet;
        }

        if (routeProfile.Risk == TrailRisk.Moderate && routeProfile.Terrain == TrailTerrain.OpenRange && routeProfile.WaterFeature == WaterFeature.Creek)
        {
            return TravelDayEncounterCategory.Quiet;
        }

        if (routeProfile.Risk == TrailRisk.Moderate && routeProfile.Terrain == TrailTerrain.Badlands && routeProfile.WaterFeature == WaterFeature.None)
        {
            return TravelDayEncounterCategory.Quiet;
        }

        if (routeProfile.Risk == TrailRisk.Low && routeProfile.Terrain == TrailTerrain.Hills && routeProfile.WaterFeature == WaterFeature.River && travelRulesProfile.Difficulty == TravelDifficulty.Normal)
        {
            return TravelDayEncounterCategory.Quiet;
        }

        if (routeProfile.Risk == TrailRisk.Low && routeProfile.WaterFeature == WaterFeature.Creek)
        {
            return TravelDayEncounterCategory.Lucky;
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Easy && routeProfile.Risk == TrailRisk.Low && routeProfile.Terrain == TrailTerrain.OpenRange && routeProfile.WaterFeature == WaterFeature.None)
        {
            return TravelDayEncounterCategory.Lucky;
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Normal && routeProfile.Risk == TrailRisk.Low && routeProfile.Terrain == TrailTerrain.OpenRange && routeProfile.WaterFeature == WaterFeature.None)
        {
            return TravelDayEncounterCategory.Lucky;
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Easy && routeProfile.WaterFeature == WaterFeature.None && routeProfile.Terrain is TrailTerrain.Hills or TrailTerrain.Badlands)
        {
            return TravelDayEncounterCategory.Lucky;
        }

        if (routeProfile.Risk == TrailRisk.Moderate && routeProfile.WaterFeature == WaterFeature.Spring)
        {
            return TravelDayEncounterCategory.Unlucky;
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Normal && routeProfile.Risk == TrailRisk.Low && routeProfile.Terrain == TrailTerrain.Badlands && routeProfile.WaterFeature == WaterFeature.None)
        {
            return TravelDayEncounterCategory.Quiet;
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Hard && routeProfile.Terrain == TrailTerrain.Badlands && routeProfile.WaterFeature == WaterFeature.None)
        {
            return TravelDayEncounterCategory.Unlucky;
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Hard && journey.TravelMode == TravelMode.Mounted && routeProfile.Terrain == TrailTerrain.Hills && routeProfile.WaterFeature == WaterFeature.River)
        {
            return TravelDayEncounterCategory.HorseTrouble;
        }

        if (quietDay)
        {
            return TravelDayEncounterCategory.Quiet;
        }

        var weightTable = BuildCategoryWeights(journey, travelRulesProfile);
        var totalWeight = weightTable.Sum(entry => entry.Weight);
        var pick = (int)(roll % (ulong)totalWeight);

        foreach (var entry in weightTable)
        {
            if (pick < entry.Weight)
            {
                return entry.Category;
            }

            pick -= entry.Weight;
        }

        return weightTable[0].Category;
    }

    private static IReadOnlyList<(TravelDayEncounterCategory Category, int Weight)> BuildCategoryWeights(TravelJourney journey, TravelRulesProfile travelRulesProfile)
    {
        var routeProfile = journey.Preview.RouteProfile;
        var weights = new List<(TravelDayEncounterCategory, int)>
        {
            (TravelDayEncounterCategory.Lucky, routeProfile.Risk == TrailRisk.Low ? 4 : 2),
            (TravelDayEncounterCategory.Unlucky, routeProfile.Risk == TrailRisk.High ? 4 : 2),
            (TravelDayEncounterCategory.Foe, routeProfile.Risk == TrailRisk.High ? 5 : routeProfile.Risk == TrailRisk.Moderate ? 3 : 1),
            (TravelDayEncounterCategory.Npc, 3),
            (TravelDayEncounterCategory.Environmental, routeProfile.WaterFeature == WaterFeature.None ? 4 : 2),
            (TravelDayEncounterCategory.Resource, journey.FoodRemaining <= 2 || journey.AvailableCanteenCharges <= 2 ? 4 : 2),
            (TravelDayEncounterCategory.HorseTrouble, journey.HorseState is null ? 1 : journey.HorseState.Exhaustion >= travelRulesProfile.HorseExhaustionLameThreshold - 1 ? 4 : 2)
        };

        return weights;
    }

    private static TravelDayEncounterState CreateEncounter(
        TravelJourney journey,
        TravelRulesProfile travelRulesProfile,
        int dayNumber,
        int slotIndex,
        TravelDayEncounterCategory category,
        string seed)
    {
        return category switch
        {
            TravelDayEncounterCategory.Lucky => CreateLuckyEncounter(journey, travelRulesProfile, dayNumber, slotIndex, seed),
            TravelDayEncounterCategory.Unlucky => CreateUnluckyEncounter(journey, travelRulesProfile, dayNumber, slotIndex, seed),
            TravelDayEncounterCategory.Foe => CreateChoiceEncounter(slotIndex, "foe", BuildFoeMessage(journey, dayNumber, slotIndex, seed)),
            TravelDayEncounterCategory.Npc => CreateChoiceEncounter(slotIndex, "npc", BuildNpcMessage(journey, dayNumber, slotIndex, seed)),
            TravelDayEncounterCategory.Environmental => CreateEnvironmentalEncounter(journey, travelRulesProfile, dayNumber, slotIndex, seed),
            TravelDayEncounterCategory.Resource => CreateResourceEncounter(journey, travelRulesProfile, dayNumber, slotIndex, seed),
            TravelDayEncounterCategory.HorseTrouble => CreateHorseTroubleEncounter(journey, travelRulesProfile, dayNumber, slotIndex, seed),
            _ => new TravelDayEncounterState(slotIndex, TravelDayEncounterCategory.Quiet, "Quiet trail", BuildQuietMessage(journey, dayNumber, slotIndex, seed), null, null, null)
        };
    }

    private static TravelDayEncounterState CreateLuckyEncounter(TravelJourney journey, TravelRulesProfile travelRulesProfile, int dayNumber, int slotIndex, string seed)
    {
        var routeProfile = journey.Preview.RouteProfile;
        var choice = routeProfile.Risk == TrailRisk.Low && routeProfile.WaterFeature == WaterFeature.Creek
            ? 0
            : travelRulesProfile.Difficulty == TravelDifficulty.Easy && routeProfile.Risk == TrailRisk.Low && routeProfile.Terrain == TrailTerrain.OpenRange && routeProfile.WaterFeature == WaterFeature.None
                ? 1
                : travelRulesProfile.Difficulty == TravelDifficulty.Easy && routeProfile.WaterFeature == WaterFeature.None && routeProfile.Terrain is TrailTerrain.Hills or TrailTerrain.Badlands
                    ? 2
                    : (int)(Roll(seed, "lucky") % 3);
        return choice switch
        {
            0 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Lucky,
                "Hidden coin cache",
                $"I found a hidden cache of trail coins and pocketed an extra ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyCoinCache,
                    "Hidden coin cache",
                    $"I found a hidden cache of trail coins and pocketed an extra ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                    walletDelta: travelRulesProfile.LuckyTrailCoinReward),
                null,
                null),
            1 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Lucky,
                "Trail grub cache",
                $"I found a cache of jerky and trail biscuits and gained {travelRulesProfile.LuckyTrailFoodReward} food.",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyFoodCache,
                    "Trail grub cache",
                    $"I found a cache of jerky and trail biscuits and gained {travelRulesProfile.LuckyTrailFoodReward} food.",
                    foodDelta: travelRulesProfile.LuckyTrailFoodReward),
                null,
                null),
            _ => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Lucky,
                "Hidden water seep",
                $"I found a seep under the rocks and topped off my canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyWaterSeep,
                    "Hidden water seep",
                    $"I found a seep under the rocks and topped off my canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                    canteenChargeDelta: travelRulesProfile.LuckyTrailWaterRecovery),
                null,
                null)
        };
    }

    private static TravelDayEncounterState CreateUnluckyEncounter(TravelJourney journey, TravelRulesProfile travelRulesProfile, int dayNumber, int slotIndex, string seed)
    {
        var routeProfile = journey.Preview.RouteProfile;
        var choice = routeProfile.Risk == TrailRisk.Moderate && routeProfile.WaterFeature == WaterFeature.Spring
            ? 0
            : travelRulesProfile.Difficulty == TravelDifficulty.Hard && routeProfile.Terrain == TrailTerrain.Badlands && routeProfile.WaterFeature == WaterFeature.None && routeProfile.Risk != TrailRisk.High
                ? 1
                : travelRulesProfile.Difficulty == TravelDifficulty.Hard && journey.TravelMode == TravelMode.Mounted && routeProfile.Terrain == TrailTerrain.Hills && routeProfile.WaterFeature == WaterFeature.River
                    ? 2
                    : (int)(Roll(seed, "unlucky") % 4);
        return choice switch
        {
            0 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Unlucky,
                "Washed-out trail",
                $"A washout forces a detour and costs me {travelRulesProfile.BadLuckTrailDelayDays} extra day(s).",
                JourneyTrailEventState.CreateBadLuck(
                    JourneyTrailEventId.BadLuckWashout,
                    "Washed-out trail",
                    $"A washout forces a detour and costs me {travelRulesProfile.BadLuckTrailDelayDays} extra day(s).",
                    delayDays: travelRulesProfile.BadLuckTrailDelayDays,
                    heatIncrease: travelRulesProfile.TrailEventHeatIncrease),
                null,
                null),
            1 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Unlucky,
                "Dust-choked outfit",
                $"A dust storm strips away {travelRulesProfile.BadLuckTrailFoodLoss} food and {travelRulesProfile.BadLuckTrailCanteenLoss} canteen charge(s).",
                JourneyTrailEventState.CreateBadLuck(
                    JourneyTrailEventId.BadLuckFoodLoss,
                    "Dust-choked outfit",
                    $"A dust storm strips away {travelRulesProfile.BadLuckTrailFoodLoss} food and {travelRulesProfile.BadLuckTrailCanteenLoss} canteen charge(s).",
                    foodDelta: -travelRulesProfile.BadLuckTrailFoodLoss,
                    canteenChargeDelta: -travelRulesProfile.BadLuckTrailCanteenLoss,
                    horseThirstDelta: travelRulesProfile.BadLuckTrailHorseThirst,
                    delayDays: travelRulesProfile.BadLuckTrailDelayDays,
                    heatIncrease: travelRulesProfile.TrailEventHeatIncrease),
                null,
                null),
            2 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Unlucky,
                "Spooked horse",
                "A sudden canyon echo spooks the horse and leaves it more exhausted.",
                JourneyTrailEventState.CreateBadLuck(
                    JourneyTrailEventId.BadLuckSpookedHorse,
                    "Spooked horse",
                    "A sudden canyon echo spooks the horse and leaves it more exhausted.",
                    horseExhaustionDelta: travelRulesProfile.BadLuckTrailHorseExhaustion,
                    heatIncrease: travelRulesProfile.TrailEventHeatIncrease),
                null,
                null),
            _ => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Unlucky,
                "Hard miles",
                "The trail goes mean and I have to earn every mile the hard way.",
                JourneyTrailEventState.CreateBadLuck(
                    JourneyTrailEventId.BadLuckDustStorm,
                    "Hard miles",
                    "The trail goes mean and I have to earn every mile the hard way.",
                    delayDays: 0,
                    heatIncrease: travelRulesProfile.TrailEventHeatIncrease),
                null,
                null)
        };
    }

    private static TravelDayEncounterState CreateEnvironmentalEncounter(TravelJourney journey, TravelRulesProfile travelRulesProfile, int dayNumber, int slotIndex, string seed)
    {
        var routeProfile = journey.Preview.RouteProfile;
        var title = routeProfile.Terrain switch
        {
            TrailTerrain.OpenRange => "Wide weather",
            TrailTerrain.Hills => "Crosswind",
            TrailTerrain.Badlands => "Rockfall",
            TrailTerrain.Mountains => "High pass",
            _ => "Trail weather"
        };

        var message = routeProfile.WaterFeature switch
        {
            WaterFeature.None => "The weather keeps the trail honest and the dust keeps my eyes narrowed.",
            WaterFeature.Creek or WaterFeature.Spring => "The water nearby keeps the trail a little friendlier, even when the wind starts to tug at me.",
            WaterFeature.River => "I follow the river cut and let the current shape the day.",
            _ => "The land keeps changing under me, and I stay alert."
        };

        return new TravelDayEncounterState(
            slotIndex,
            TravelDayEncounterCategory.Environmental,
            title,
            message,
            JourneyTrailEventState.CreateBadLuck(
                JourneyTrailEventId.BadLuckWashout,
                title,
                message,
                delayDays: 0,
                heatIncrease: Math.Max(1, travelRulesProfile.TrailEventHeatIncrease - 1)),
            null,
            null);
    }

    private static TravelDayEncounterState CreateResourceEncounter(TravelJourney journey, TravelRulesProfile travelRulesProfile, int dayNumber, int slotIndex, string seed)
    {
        var choice = (int)(Roll(seed, "resource") % 3);
        return choice switch
        {
            0 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Resource,
                "Trail grub",
                $"I find a little extra food and pick up {travelRulesProfile.LuckyTrailFoodReward} meal(s).",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyFoodCache,
                    "Trail grub",
                    $"I find a little extra food and pick up {travelRulesProfile.LuckyTrailFoodReward} meal(s).",
                    foodDelta: travelRulesProfile.LuckyTrailFoodReward),
                null,
                null),
            1 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Resource,
                "Water seep",
                $"I catch a seep in the rocks and top off the canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyWaterSeep,
                    "Water seep",
                    $"I catch a seep in the rocks and top off the canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                    canteenChargeDelta: travelRulesProfile.LuckyTrailWaterRecovery),
                null,
                null),
            _ => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Resource,
                "Coin cache",
                $"I uncover a hidden cache of trail coins and pocket ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyCoinCache,
                    "Coin cache",
                    $"I uncover a hidden cache of trail coins and pocket ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                    walletDelta: travelRulesProfile.LuckyTrailCoinReward),
                null,
                null)
        };
    }

    private static TravelDayEncounterState CreateHorseTroubleEncounter(TravelJourney journey, TravelRulesProfile travelRulesProfile, int dayNumber, int slotIndex, string seed)
    {
        var routeProfile = journey.Preview.RouteProfile;
        var message = journey.HorseState is null
            ? "The horse trouble never really starts because I am traveling without a horse."
            : routeProfile.Terrain switch
            {
                TrailTerrain.Badlands => "The horse picks a poor line through the bad ground and comes out more exhausted.",
                TrailTerrain.Hills => "The horse labors up the slope and pays for it in exhaustion.",
                TrailTerrain.Mountains => "The horse struggles on the climb and needs a steadier pace.",
                _ => "The horse takes a hard moment on the trail and I have to mind its pace."
            };

        return new TravelDayEncounterState(
            slotIndex,
            TravelDayEncounterCategory.HorseTrouble,
            "Horse trouble",
            message,
            JourneyTrailEventState.CreateBadLuck(
                JourneyTrailEventId.BadLuckSpookedHorse,
                "Horse trouble",
                message,
                horseExhaustionDelta: journey.HorseState is null ? 0 : Math.Max(1, travelRulesProfile.BadLuckTrailHorseExhaustion - 1),
                heatIncrease: 0),
            null,
            null);
    }

    private static TravelDayEncounterState CreateChoiceEncounter(int slotIndex, string kind, string message)
        => new(
            slotIndex,
            kind == "npc" ? TravelDayEncounterCategory.Npc : TravelDayEncounterCategory.Foe,
            kind == "npc" ? "Weathered stranger" : "Hard-eyed rider",
            message,
            null,
            JourneyEncounterState.CreateChoiceEncounter(kind, message, DefaultEncounterChoices),
            null);

    private static string BuildFoeMessage(TravelJourney journey, int dayNumber, int slotIndex, string seed)
        => journey.Preview.RouteProfile.Risk switch
        {
            TrailRisk.High => "A hard-eyed rider steps out from the brush and blocks my way.",
            TrailRisk.Moderate => "A wary trail rider cuts across my path and waits to see what I will do.",
            _ => "A stranger on the trail squares up and makes me choose how to answer."
        };

    private static string BuildNpcMessage(TravelJourney journey, int dayNumber, int slotIndex, string seed)
        => journey.Preview.RouteProfile.Terrain switch
        {
            TrailTerrain.OpenRange => "A weathered rider wants a word about the next water stop.",
            TrailTerrain.Hills => "A ranch hand asks if I have seen any trouble along the ridgeline.",
            TrailTerrain.Badlands => "A lone rider asks if I will trade news for water.",
            TrailTerrain.Mountains => "A pack rider wants to know if the pass ahead is still clear.",
            _ => "A stranger on the trail wants a moment of my time."
        };

    private static string BuildQuietMessage(TravelJourney journey, int dayNumber, int slotIndex, string seed)
        => journey.Preview.RouteProfile.Terrain switch
        {
            TrailTerrain.OpenRange => "The open range keeps its own counsel and lets me ride in silence.",
            TrailTerrain.Hills => "The hills answer only with wind and the horse's breath.",
            TrailTerrain.Badlands => "The badlands stay hard and dry, but the road remains clear.",
            TrailTerrain.Mountains => "The mountain trail stays empty enough for me to hear every hoofstep.",
            _ => "The trail goes quiet enough that I can hear leather creak and wind in the brush."
        };
}

public sealed class TravelJourney
{
    internal TravelJourney(TravelPreview preview, string? openingNarration = null)
    {
        Preview = preview;
        TravelMode = preview.TravelMode;
        Status = JourneyStatus.Active;
        RemainingRideDayDistance = preview.RemainingRideDayDistance;
        RemainingDays = preview.RemainingDays;
        FoodRemaining = preview.AvailableFood;
        HorseFeedRemaining = preview.AvailableHorseFeed;
        AvailableCanteenCharges = preview.AvailableCanteenCharges;
        HorseState = preview.HorseState;
        OpeningNarration = openingNarration;
    }

    public TravelPreview Preview { get; }

    public TravelMode TravelMode { get; private set; }

    public JourneyStatus Status { get; private set; }

    public decimal RemainingRideDayDistance { get; private set; }

    public int RemainingDays { get; private set; }

    public int DaysTravelled { get; private set; }

    public int DelayDays { get; private set; }

    public JourneyEncounterState? PendingEncounter { get; private set; }

    public TravelDayPlanState? CurrentDayPlan { get; private set; }

    public int FoodRemaining { get; private set; }

    public int HorseFeedRemaining { get; private set; }

    public int AvailableCanteenCharges { get; private set; }

    public HorseTravelState? HorseState { get; private set; }

    public string? OpeningNarration { get; private set; }

    public static TravelJourney Start(TravelPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return new TravelJourney(preview);
    }

    public static TravelJourney Start(TravelPreview preview, string? openingNarration)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return new TravelJourney(preview, openingNarration);
    }

    public static TravelJourney FromSnapshot(TravelJourneySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var preview = new TravelPreview(
            snapshot.OriginTownId,
            snapshot.DestinationTownId,
            snapshot.OriginTownName,
            snapshot.DestinationTownName,
            snapshot.RouteProfile,
            snapshot.TravelMode,
            snapshot.MountedTravelAvailable,
            snapshot.WaterSecure,
            snapshot.RideDayDistance,
            snapshot.RemainingRideDayDistance,
            snapshot.ExpectedDays,
            snapshot.RemainingDays,
            snapshot.CanteenChargesPerDay,
            snapshot.RequiredCanteenCharges,
            snapshot.AvailableCanteenCharges,
            snapshot.CanteenReserveCharges,
            snapshot.DelayMarginDays,
            snapshot.DelayRisk,
            snapshot.RequiredFood,
            snapshot.AvailableFood,
            snapshot.RequiredHorseFeed,
            snapshot.AvailableHorseFeed,
            snapshot.HorseState,
            snapshot.Warnings);

        var journey = new TravelJourney(preview)
        {
            TravelMode = snapshot.TravelMode,
            Status = snapshot.Status,
            RemainingRideDayDistance = snapshot.RemainingRideDayDistance,
            RemainingDays = snapshot.RemainingDays,
            DaysTravelled = snapshot.DaysTravelled,
            DelayDays = snapshot.DelayDays,
            CurrentDayPlan = snapshot.CurrentDayPlan,
            PendingEncounter = snapshot.PendingEncounter,
            FoodRemaining = snapshot.AvailableFood,
            HorseFeedRemaining = snapshot.AvailableHorseFeed,
            AvailableCanteenCharges = snapshot.AvailableCanteenCharges,
            HorseState = snapshot.HorseState,
            OpeningNarration = snapshot.OpeningNarration
        };

        return journey;
    }

    public void RecalculatePacing(TravelMode travelMode)
    {
        TravelMode = travelMode;
    }

    public JourneyProgress AdvanceOneDay()
    {
        if (Status != JourneyStatus.Active)
        {
            throw new InvalidOperationException("Journey is not active.");
        }

        var dailyProgress = Preview.RouteProfile.DailyRideDayProgress(TravelMode);

        RemainingRideDayDistance = Math.Max(0, RemainingRideDayDistance - dailyProgress);
        DaysTravelled++;
        RemainingDays = Math.Max(0, RemainingDays - 1);

        return new JourneyProgress(dailyProgress, RemainingDays == 0);
    }

    public void MarkCompleted()
    {
        Status = JourneyStatus.Completed;
        RemainingRideDayDistance = 0;
        RemainingDays = 0;
    }

    public void MarkInterrupted(JourneyEncounterState encounter)
    {
        ArgumentNullException.ThrowIfNull(encounter);

        Status = JourneyStatus.Interrupted;
        PendingEncounter = encounter;
    }

    public void ResumeFromEncounter()
    {
        Status = JourneyStatus.Active;
        PendingEncounter = null;
    }

    public void SetCurrentDayPlan(TravelDayPlanState? dayPlan)
    {
        CurrentDayPlan = dayPlan;
        PendingEncounter = dayPlan?.CurrentEncounter?.PendingEncounter;
    }

    public void AdvanceCurrentDayPlan()
    {
        if (CurrentDayPlan is null)
        {
            return;
        }

        var nextIndex = CurrentDayPlan.CurrentEncounterIndex + 1;
        CurrentDayPlan = CurrentDayPlan with
        {
            CurrentEncounterIndex = nextIndex,
            IsComplete = nextIndex >= CurrentDayPlan.Encounters.Count
        };
        PendingEncounter = CurrentDayPlan.CurrentEncounter?.PendingEncounter;
    }

    public void CompleteCurrentDayPlan()
    {
        if (CurrentDayPlan is null)
        {
            return;
        }

        CurrentDayPlan = CurrentDayPlan with
        {
            CurrentEncounterIndex = CurrentDayPlan.Encounters.Count,
            IsComplete = true
        };
        PendingEncounter = null;
    }

    public void RecordCurrentDayEncounterResolution(TravelDiaryEncounterResolutionState resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        if (CurrentDayPlan is null || CurrentDayPlan.CurrentEncounter is null)
        {
            return;
        }

        var updatedEncounters = CurrentDayPlan.Encounters
            .Select((encounter, index) => index == CurrentDayPlan.CurrentEncounterIndex
                ? encounter with { Resolution = resolution }
                : encounter)
            .ToArray();

        CurrentDayPlan = CurrentDayPlan with
        {
            Encounters = updatedEncounters
        };
    }

    public void MarkFailed()
    {
        Status = JourneyStatus.Failed;
    }

    public void AddDelayDays(int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Delay days cannot be negative.");
        }

        if (days == 0)
        {
            return;
        }

        DelayDays += days;
        if (RemainingRideDayDistance > 0)
        {
            RemainingDays += days;
        }
    }

    public void ConsumeFood()
    {
        if (FoodRemaining < 1)
        {
            throw new InvalidOperationException("Journey has no food remaining.");
        }

        FoodRemaining--;
    }

    public void AdjustFood(int quantity)
    {
        if (FoodRemaining + quantity < 0)
        {
            throw new InvalidOperationException("Journey has no food remaining.");
        }

        FoodRemaining += quantity;
    }

    public bool TryConsumeHorseFeed()
    {
        if (HorseFeedRemaining < 1)
        {
            return false;
        }

        HorseFeedRemaining--;
        return true;
    }

    public void ConsumeHorseFeed(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Horse feed quantity cannot be negative.");
        }

        if (quantity == 0)
        {
            return;
        }

        if (HorseFeedRemaining < quantity)
        {
            throw new InvalidOperationException("Journey has no horse feed remaining.");
        }

        HorseFeedRemaining -= quantity;
    }

    public void SetCanteenCharges(int charges)
    {
        if (charges < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(charges), "Canteen charges cannot be negative.");
        }

        AvailableCanteenCharges = charges;
    }

    public void SetHorseState(HorseTravelState? horseState)
    {
        HorseState = horseState;
    }

    private int CanteenChargesPerDay(TravelRulesProfile travelRulesProfile)
        => JourneyUpkeepRules.WaterChargesRequiredPerDay(HorseState, travelRulesProfile);

    public TravelJourneySnapshot ToSnapshot(TravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= TravelRulesProfile.Default;
        var canteenChargesPerDay = CanteenChargesPerDay(travelRulesProfile);
        var requiredCanteenCharges = RemainingRideDayDistance == 0 || JourneyUpkeepRules.HasRouteWater(Preview.RouteProfile.WaterFeature)
            ? 0
            : RemainingDays * canteenChargesPerDay;
        var waterSecure = JourneyUpkeepRules.HasRouteWater(Preview.RouteProfile.WaterFeature) || AvailableCanteenCharges >= requiredCanteenCharges;
        var canteenReserveCharges = AvailableCanteenCharges - requiredCanteenCharges;
        var delayMarginDays = canteenChargesPerDay == 0 ? 0 : Math.Max(0, canteenReserveCharges / canteenChargesPerDay);

        return new(
            Preview.OriginTownId,
            Preview.DestinationTownId,
            Preview.OriginTownName,
            Preview.DestinationTownName,
            Preview.RouteProfile,
            TravelMode,
            Status,
            Preview.MountedTravelAvailable && (HorseState?.CanProvideMountedTravelFor(travelRulesProfile) ?? false),
            waterSecure,
            Preview.RideDayDistance,
            RemainingRideDayDistance,
            Preview.ExpectedDays,
            RemainingDays,
            canteenChargesPerDay,
            requiredCanteenCharges,
            AvailableCanteenCharges,
            canteenReserveCharges,
            delayMarginDays,
            canteenChargesPerDay > 0 && canteenReserveCharges <= 0,
            Preview.RequiredFood,
            FoodRemaining,
            Preview.RequiredHorseFeed,
            HorseFeedRemaining,
            HorseState,
            OpeningNarration,
            DaysTravelled,
            DelayDays,
            CurrentDayPlan,
            PendingEncounter,
            Preview.Warnings);
    }

    public JourneyEncounterState? TryCreateEncounter(TravelRulesProfile? travelRulesProfile = null)
    {
        _ = travelRulesProfile;
        return CurrentDayPlan?.CurrentEncounter?.PendingEncounter;
    }

    public JourneyTrailEventState? TryCreateTrailEvent(TravelRulesProfile? travelRulesProfile = null)
    {
        _ = travelRulesProfile;
        return CurrentDayPlan?.CurrentEncounter?.TrailEvent;
    }
}

public sealed record JourneyProgress(decimal RideDayDistanceTravelled, bool Completed);

public sealed record TravelJourneyStepResult(
    bool Success,
    JourneyStatus Status,
    string Message,
    string LogMessage,
    int HeatIncrease,
    TravelJourneySnapshot? Journey = null,
    JourneyTrailEventState? TrailEvent = null)
{
    public static TravelJourneyStepResult Failed(string message)
        => new(false, JourneyStatus.Failed, message, message, 0);
}

public sealed record JourneyEncounterResolutionResult(
    bool Success,
    bool SessionChanged,
    JourneyStatus Status,
    string Message,
    TravelJourneySnapshot? Journey = null)
{
    public static JourneyEncounterResolutionResult Failed(string message, JourneyStatus status, TravelJourneySnapshot? journey = null)
        => new(false, false, status, message, journey);
}

public sealed record TravelPreviewResult(bool Success, string Message, TravelPreview? Preview)
{
    public static TravelPreviewResult Failed(string message) => new(false, message, null);
}

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
                $"You spot a hidden cache of trail coins and pocket an extra ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                walletDelta: travelRulesProfile.LuckyTrailCoinReward);
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Easy && routeProfile.Risk == TrailRisk.Low && routeProfile.Terrain == TrailTerrain.OpenRange && routeProfile.WaterFeature == WaterFeature.None)
        {
            return JourneyTrailEventState.CreateLucky(
                JourneyTrailEventId.LuckyFoodCache,
                "Trail grub cache",
                $"You find a cache of jerky and trail biscuits and gain {travelRulesProfile.LuckyTrailFoodReward} food.",
                foodDelta: travelRulesProfile.LuckyTrailFoodReward);
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Easy && routeProfile.WaterFeature == WaterFeature.None && routeProfile.Terrain is TrailTerrain.Hills or TrailTerrain.Badlands)
        {
            return JourneyTrailEventState.CreateLucky(
                JourneyTrailEventId.LuckyWaterSeep,
                "Hidden water seep",
                $"You find a seep under the rocks and top off your canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                canteenChargeDelta: travelRulesProfile.LuckyTrailWaterRecovery);
        }

        if (routeProfile.Risk == TrailRisk.Moderate && routeProfile.WaterFeature == WaterFeature.Spring)
        {
            return JourneyTrailEventState.CreateBadLuck(
                JourneyTrailEventId.BadLuckWashout,
                "Washed-out trail",
                $"A washout forces a detour and costs you {travelRulesProfile.BadLuckTrailDelayDays} extra delay day(s).",
                delayDays: travelRulesProfile.BadLuckTrailDelayDays,
                heatIncrease: travelRulesProfile.TrailEventHeatIncrease);
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Hard && routeProfile.Terrain == TrailTerrain.Badlands && routeProfile.WaterFeature == WaterFeature.None && routeProfile.Risk != TrailRisk.High && journey.FoodRemaining > 0 && journey.AvailableCanteenCharges > 0)
        {
            return JourneyTrailEventState.CreateBadLuck(
                JourneyTrailEventId.BadLuckFoodLoss,
                "Dust-choked outfit",
                $"A dust storm strips away {travelRulesProfile.BadLuckTrailFoodLoss} food and {travelRulesProfile.BadLuckTrailCanteenLoss} canteen charge(s).",
                foodDelta: -travelRulesProfile.BadLuckTrailFoodLoss,
                canteenChargeDelta: -travelRulesProfile.BadLuckTrailCanteenLoss,
                horseThirstDelta: travelRulesProfile.BadLuckTrailHorseThirst,
                delayDays: travelRulesProfile.BadLuckTrailDelayDays,
                heatIncrease: travelRulesProfile.TrailEventHeatIncrease);
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Hard && journey.TravelMode == TravelMode.Mounted && routeProfile.Terrain == TrailTerrain.Hills && routeProfile.WaterFeature == WaterFeature.River)
        {
            return JourneyTrailEventState.CreateBadLuck(
                JourneyTrailEventId.BadLuckSpookedHorse,
                "Spooked horse",
                "A sudden canyon echo spooks the horse and leaves it more exhausted.",
                horseExhaustionDelta: travelRulesProfile.BadLuckTrailHorseExhaustion,
                heatIncrease: travelRulesProfile.TrailEventHeatIncrease);
        }

        return null;
    }
}

public sealed class TravelResolver
{
    private static readonly InventoryCapabilityResolver CapabilityResolver = new();

    public TravelPreviewResult PreviewJourney(
        DomainWorld world,
        TownId currentTownId,
        TownId destinationTownId,
        DomainInventory inventory,
        TravelRulesProfile? travelRulesProfile = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(inventory);
        travelRulesProfile ??= TravelRulesProfile.Default;

        if (!world.TryGetTown(currentTownId, out var originTown))
        {
            return TravelPreviewResult.Failed("Current town could not be found.");
        }

        if (!world.TryGetTown(destinationTownId, out var destinationTown))
        {
            return TravelPreviewResult.Failed("Destination town could not be found.");
        }

        var trail = world.FindConnectedTrail(currentTownId, destinationTownId);
        if (trail is null)
        {
            return TravelPreviewResult.Failed("No trail connects those towns.");
        }

        var capabilities = CapabilityResolver.Resolve(inventory, travelRulesProfile);
        var mountedTravelAvailable = capabilities.MountedTravelAvailable;
        var travelMode = mountedTravelAvailable ? TravelMode.Mounted : TravelMode.Foot;
        var horseState = inventory.GetHorseState();
        var canteenState = inventory.GetCanteenState();
        var routeProfile = BuildRouteProfile(trail, travelRulesProfile);
        var rideDayDistance = routeProfile.RideDayDistance;
        var expectedDays = routeProfile.ExpectedDays(travelMode);
        var availableFood = inventory.GetQuantity(ItemKind.Food);
        var availableHorseFeed = inventory.GetQuantity(ItemKind.HorseFeed);
        var grazingAvailable = JourneyUpkeepRules.HasGrazing(routeProfile.Terrain);
        var routeWaterSecure = JourneyUpkeepRules.HasRouteWater(routeProfile.WaterFeature);
        var livingHorse = horseState is not null && !horseState.IsDeadFor(travelRulesProfile);
        var requiredFood = expectedDays;
        var requiredHorseFeed = livingHorse && !grazingAvailable ? expectedDays : 0;
        var canteenChargesPerDay = routeWaterSecure ? 0 : JourneyUpkeepRules.WaterChargesRequiredPerDay(horseState, travelRulesProfile);
        var requiredCanteenCharges = expectedDays * canteenChargesPerDay;
        var availableCanteenCharges = canteenState?.Charges ?? 0;
        var canteenReserveCharges = availableCanteenCharges - requiredCanteenCharges;
        var delayMarginDays = canteenChargesPerDay == 0 ? 0 : Math.Max(0, canteenReserveCharges / canteenChargesPerDay);
        var delayRisk = canteenChargesPerDay > 0 && canteenReserveCharges <= 0;
        var waterSecure = routeWaterSecure || availableCanteenCharges >= requiredCanteenCharges;
        var warnings = new List<string>(routeProfile.Warnings);

        if (!mountedTravelAvailable)
        {
            warnings.Add("Mounted travel is unavailable, so the route will continue on foot.");
        }

        if (livingHorse && !grazingAvailable)
        {
            warnings.Add("Poor grazing means the horse will rely on feed on this trail.");
        }

        if (availableFood < requiredFood)
        {
            warnings.Add("You do not have enough food to cover the full trail.");
        }

        if (availableHorseFeed < requiredHorseFeed)
        {
            warnings.Add("You do not have enough horse feed to keep the horse fed on this trail.");
        }

        if (!routeWaterSecure && livingHorse)
        {
            warnings.Add("This dry route needs two canteen charges per day to water both horse and rider.");
        }
        else if (!routeWaterSecure)
        {
            warnings.Add("This dry route needs one canteen charge per day for the rider.");
        }

        if (availableCanteenCharges < requiredCanteenCharges)
        {
            warnings.Add($"You are short by {Math.Abs(canteenReserveCharges)} canteen charge(s) for the base trail.");
        }
        else if (!routeWaterSecure && canteenReserveCharges == 0)
        {
            warnings.Add("Your canteen exactly covers the base trail, so any delay will need more water.");
        }
        else if (!routeWaterSecure)
        {
            warnings.Add($"Your canteen has {canteenReserveCharges} spare charge(s) and can absorb {delayMarginDays} delay day(s).");
        }

        var preview = new TravelPreview(
            currentTownId,
            destinationTownId,
            originTown!.Name,
            destinationTown!.Name,
            routeProfile,
            travelMode,
            mountedTravelAvailable,
            waterSecure,
            rideDayDistance,
            rideDayDistance,
            expectedDays,
            expectedDays,
            canteenChargesPerDay,
            requiredCanteenCharges,
            availableCanteenCharges,
            canteenReserveCharges,
            delayMarginDays,
            delayRisk,
            requiredFood,
            availableFood,
            requiredHorseFeed,
            availableHorseFeed,
            horseState,
            warnings);

        return new TravelPreviewResult(
            true,
            $"Previewed {travelMode.ToString().ToLowerInvariant()} travel from {originTown.Name} to {destinationTown.Name}: {rideDayDistance:0.##} ride-day unit(s), {expectedDays} day(s); {DescribeCanteenCoverage(routeProfile.WaterFeature, canteenChargesPerDay, canteenReserveCharges, delayMarginDays)}",
            preview);
    }

    private static string DescribeCanteenCoverage(
        WaterFeature waterFeature,
        int canteenChargesPerDay,
        int canteenReserveCharges,
        int delayMarginDays)
    {
        if (JourneyUpkeepRules.HasRouteWater(waterFeature))
        {
            return "Route water is secure, so no canteen reserve is required";
        }

        if (canteenChargesPerDay <= 0)
        {
            return "No canteen water is required on this trail";
        }

        if (canteenReserveCharges == 0)
        {
            return "The canteen exactly covers the base trail and has no reserve for delays";
        }

        if (canteenReserveCharges > 0)
        {
            return $"The canteen has {canteenReserveCharges} spare charge(s) and can absorb {delayMarginDays} delay day(s)";
        }

        return $"The canteen is short by {Math.Abs(canteenReserveCharges)} charge(s) for the base trail";
    }

    private static TravelRouteProfile BuildRouteProfile(Trail trail, TravelRulesProfile travelRulesProfile)
    {
        var warnings = new List<string>();

        if (trail.Risk >= TrailRisk.Moderate)
        {
            warnings.Add("Rough trail conditions may stress the horse.");
        }

        if (trail.WaterFeature == WaterFeature.None)
        {
            warnings.Add("Water is sparse along this trail.");
        }

        return new TravelRouteProfile(
            trail.Id.Value,
            trail.Risk,
            trail.Terrain,
            trail.WaterFeature,
            trail.RideDayDistance,
            travelRulesProfile.MountedRideDayProgress,
            travelRulesProfile.FootRideDayProgress,
            warnings);
    }
}
