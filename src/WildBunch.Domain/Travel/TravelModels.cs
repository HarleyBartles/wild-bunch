using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Domain.Travel;

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
            snapshot.RouteProfile.ExpectedDays(TravelMode.Mounted),
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
        var warnings = TravelWarningFilter.Filter(
            Preview.Warnings,
            Preview.MountedTravelAvailable && (HorseState?.CanProvideMountedTravelFor(travelRulesProfile) ?? false));

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
            warnings);
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

public sealed record JourneyArrivalAcknowledgementResult(
    bool Success,
    string Message,
    TravelJourneySnapshot? Journey = null)
{
    public static JourneyArrivalAcknowledgementResult Failed(string message, TravelJourneySnapshot? journey = null)
        => new(false, message, journey);
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
                $"I spot a hidden cache of trail coins and pocket an extra ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                walletDelta: travelRulesProfile.LuckyTrailCoinReward);
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Easy && routeProfile.Risk == TrailRisk.Low && routeProfile.Terrain == TrailTerrain.OpenRange && routeProfile.WaterFeature == WaterFeature.None)
        {
            return JourneyTrailEventState.CreateLucky(
                JourneyTrailEventId.LuckyFoodCache,
                "Trail grub cache",
                $"I find a cache of jerky and trail biscuits and gain {travelRulesProfile.LuckyTrailFoodReward} food.",
                foodDelta: travelRulesProfile.LuckyTrailFoodReward);
        }

        if (travelRulesProfile.Difficulty == TravelDifficulty.Easy && routeProfile.WaterFeature == WaterFeature.None && routeProfile.Terrain is TrailTerrain.Hills or TrailTerrain.Badlands)
        {
            return JourneyTrailEventState.CreateLucky(
                JourneyTrailEventId.LuckyWaterSeep,
                "Hidden water seep",
                $"I find a seep under the rocks and top off my canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                canteenChargeDelta: travelRulesProfile.LuckyTrailWaterRecovery);
        }

        if (routeProfile.Risk == TrailRisk.Moderate && routeProfile.WaterFeature == WaterFeature.Spring)
        {
            return JourneyTrailEventState.CreateBadLuck(
                JourneyTrailEventId.BadLuckWashout,
                "Washed-out trail",
                $"A washout forces a detour and costs me {travelRulesProfile.BadLuckTrailDelayDays} extra delay day(s).",
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
        var baselineRideDays = routeProfile.ExpectedDays(TravelMode.Mounted);
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

        warnings = new List<string>(TravelWarningFilter.Filter(warnings, mountedTravelAvailable));

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
            baselineRideDays,
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
            $"Previewed {travelMode.ToString().ToLowerInvariant()} travel from {originTown.Name} to {destinationTown.Name}: {baselineRideDays} day ride estimate, {expectedDays} day(s) on the trail; {DescribeCanteenCoverage(routeProfile.WaterFeature, canteenChargesPerDay, canteenReserveCharges, delayMarginDays)}",
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
