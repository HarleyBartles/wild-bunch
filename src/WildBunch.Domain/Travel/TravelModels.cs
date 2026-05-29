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
    int DaysTravelled,
    int DelayDays,
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
}

public enum JourneyTrailEventKind
{
    Lucky = 0,
    BadLuck = 1
}

public sealed record JourneyTrailEventState(
    JourneyTrailEventKind Kind,
    string Message)
{
    public static JourneyTrailEventState CreateLucky(string message)
        => new(JourneyTrailEventKind.Lucky, message);

    public static JourneyTrailEventState CreateBadLuck(string message)
        => new(JourneyTrailEventKind.BadLuck, message);
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

public sealed class TravelJourney
{
    internal TravelJourney(TravelPreview preview)
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
    }

    public TravelPreview Preview { get; }

    public TravelMode TravelMode { get; private set; }

    public JourneyStatus Status { get; private set; }

    public decimal RemainingRideDayDistance { get; private set; }

    public int RemainingDays { get; private set; }

    public int DaysTravelled { get; private set; }

    public int DelayDays { get; private set; }

    public JourneyEncounterState? PendingEncounter { get; private set; }

    public int FoodRemaining { get; private set; }

    public int HorseFeedRemaining { get; private set; }

    public int AvailableCanteenCharges { get; private set; }

    public HorseTravelState? HorseState { get; private set; }

    public static TravelJourney Start(TravelPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return new TravelJourney(preview);
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
            PendingEncounter = snapshot.PendingEncounter,
            FoodRemaining = snapshot.AvailableFood,
            HorseFeedRemaining = snapshot.AvailableHorseFeed,
            AvailableCanteenCharges = snapshot.AvailableCanteenCharges,
            HorseState = snapshot.HorseState
        };

        return journey;
    }

    public void RecalculatePacing(TravelMode travelMode)
    {
        TravelMode = travelMode;
        RemainingDays = RemainingRideDayDistance == 0
            ? 0
            : Preview.RouteProfile.CalculateRemainingDays(RemainingRideDayDistance, TravelMode) + DelayDays;
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
        RemainingDays = RemainingRideDayDistance == 0
            ? 0
            : Preview.RouteProfile.CalculateRemainingDays(RemainingRideDayDistance, TravelMode) + DelayDays;

        return new JourneyProgress(dailyProgress, RemainingRideDayDistance == 0);
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
            DaysTravelled,
            DelayDays,
            PendingEncounter,
            Preview.Warnings);
    }

    public JourneyEncounterState? TryCreateEncounter(TravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= TravelRulesProfile.Default;

        if (Status != JourneyStatus.Active || PendingEncounter is not null)
        {
            return null;
        }

        if (Preview.RouteProfile.Risk == TrailRisk.High && DaysTravelled == travelRulesProfile.FirstEncounterDay)
        {
            return JourneyEncounterState.CreateFoe("A hard-eyed trail rider steps out from the brush and blocks your way.");
        }

        return null;
    }

    public JourneyTrailEventState? TryCreateTrailEvent(TravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= TravelRulesProfile.Default;

        if (Status != JourneyStatus.Active || PendingEncounter is not null || RemainingRideDayDistance == 0)
        {
            return null;
        }

        if (DaysTravelled != travelRulesProfile.FirstTrailEventDay)
        {
            return null;
        }

        return Preview.RouteProfile.Risk switch
        {
            TrailRisk.Low when Preview.RouteProfile.WaterFeature == WaterFeature.Creek => JourneyTrailEventState.CreateLucky("You spot a hidden cache of trail coins and pocket an extra $3.00."),
            TrailRisk.Moderate when Preview.RouteProfile.WaterFeature == WaterFeature.Spring => JourneyTrailEventState.CreateBadLuck("A washout forces a detour and costs you one extra delay day."),
            _ => null
        };
    }
}

public sealed record JourneyProgress(decimal RideDayDistanceTravelled, bool Completed);

public sealed record TravelJourneyStepResult(
    bool Success,
    JourneyStatus Status,
    string Message,
    string LogMessage,
    int HeatIncrease,
    TravelJourneySnapshot? Journey = null)
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
            $"Previewed trail from {originTown.Name} to {destinationTown.Name}.",
            preview);
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
