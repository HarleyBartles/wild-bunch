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
    int TotalDistance,
    int MountedDailyProgress,
    int FootDailyProgress,
    IReadOnlyList<string> Warnings)
{
    public int ExpectedDays(TravelMode mode)
        => CalculateRemainingDays(TotalDistance, mode);

    public int CalculateRemainingDays(int remainingDistance, TravelMode mode)
    {
        var dailyProgress = mode == TravelMode.Mounted ? MountedDailyProgress : FootDailyProgress;
        return Math.Max(1, (int)Math.Ceiling((double)remainingDistance / dailyProgress));
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
    int TotalDistance,
    int RemainingDistance,
    int ExpectedDays,
    int RemainingDays,
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
    int TotalDistance,
    int RemainingDistance,
    int ExpectedDays,
    int RemainingDays,
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

    public static int WaterChargesRequiredPerDay(HorseTravelState? horseState)
        => horseState is not null && !horseState.IsDead ? 2 : 1;

    public static JourneyDailyUpkeepResult ApplyDailyUpkeep(
        TrailTerrain terrain,
        WaterFeature waterFeature,
        HorseTravelState? horseState,
        CanteenState? canteenState,
        int horseFeedAvailable)
    {
        var grazingAvailable = HasGrazing(terrain);
        var routeWaterSecure = HasRouteWater(waterFeature);
        var nextHorseState = horseState;
        var nextCanteenState = canteenState;
        var horseFeedConsumed = 0;
        var livingHorse = horseState is not null && !horseState.IsDead;

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
            livingHorse && nextHorseState is not null && !nextHorseState.CanProvideMountedTravel);
    }
}

public sealed class TravelJourney
{
    internal TravelJourney(TravelPreview preview)
    {
        Preview = preview;
        TravelMode = preview.TravelMode;
        Status = JourneyStatus.Active;
        RemainingDistance = preview.RemainingDistance;
        RemainingDays = preview.RemainingDays;
        FoodRemaining = preview.AvailableFood;
        HorseFeedRemaining = preview.AvailableHorseFeed;
        HorseState = preview.HorseState;
    }

    public TravelPreview Preview { get; }

    public TravelMode TravelMode { get; private set; }

    public JourneyStatus Status { get; private set; }

    public int RemainingDistance { get; private set; }

    public int RemainingDays { get; private set; }

    public int DaysTravelled { get; private set; }

    public int DelayDays { get; private set; }

    public JourneyEncounterState? PendingEncounter { get; private set; }

    public int FoodRemaining { get; private set; }

    public int HorseFeedRemaining { get; private set; }

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
            snapshot.TotalDistance,
            snapshot.RemainingDistance,
            snapshot.ExpectedDays,
            snapshot.RemainingDays,
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
            RemainingDistance = snapshot.RemainingDistance,
            RemainingDays = snapshot.RemainingDays,
            DaysTravelled = snapshot.DaysTravelled,
            DelayDays = snapshot.DelayDays,
            PendingEncounter = snapshot.PendingEncounter,
            FoodRemaining = snapshot.AvailableFood,
            HorseFeedRemaining = snapshot.AvailableHorseFeed,
            HorseState = snapshot.HorseState
        };

        return journey;
    }

    public void RecalculatePacing(TravelMode travelMode)
    {
        TravelMode = travelMode;
        RemainingDays = RemainingDistance == 0
            ? 0
            : Preview.RouteProfile.CalculateRemainingDays(RemainingDistance, TravelMode) + DelayDays;
    }

    public JourneyProgress AdvanceOneDay()
    {
        if (Status != JourneyStatus.Active)
        {
            throw new InvalidOperationException("Journey is not active.");
        }

        var dailyProgress = TravelMode == TravelMode.Mounted
            ? Preview.RouteProfile.MountedDailyProgress
            : Preview.RouteProfile.FootDailyProgress;

        RemainingDistance = Math.Max(0, RemainingDistance - dailyProgress);
        DaysTravelled++;
        RemainingDays = RemainingDistance == 0
            ? 0
            : Preview.RouteProfile.CalculateRemainingDays(RemainingDistance, TravelMode) + DelayDays;

        return new JourneyProgress(dailyProgress, RemainingDistance == 0);
    }

    public void MarkCompleted()
    {
        Status = JourneyStatus.Completed;
        RemainingDistance = 0;
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
        if (RemainingDistance > 0)
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

    public void SetHorseState(HorseTravelState? horseState)
    {
        HorseState = horseState;
    }

    public TravelJourneySnapshot ToSnapshot()
        => new(
            Preview.OriginTownId,
            Preview.DestinationTownId,
            Preview.OriginTownName,
            Preview.DestinationTownName,
            Preview.RouteProfile,
            TravelMode,
            Status,
            Preview.MountedTravelAvailable && (HorseState?.CanProvideMountedTravel ?? false),
            Preview.WaterSecure,
            Preview.TotalDistance,
            RemainingDistance,
            Preview.ExpectedDays,
            RemainingDays,
            Preview.RequiredFood,
            FoodRemaining,
            Preview.RequiredHorseFeed,
            HorseFeedRemaining,
            HorseState,
            DaysTravelled,
            DelayDays,
            PendingEncounter,
            Preview.Warnings);

    public JourneyEncounterState? TryCreateEncounter()
    {
        if (Status != JourneyStatus.Active || PendingEncounter is not null)
        {
            return null;
        }

        if (Preview.RouteProfile.Risk == TrailRisk.High && DaysTravelled == 1)
        {
            return JourneyEncounterState.CreateFoe("A hard-eyed trail rider steps out from the brush and blocks your way.");
        }

        return null;
    }

    public JourneyTrailEventState? TryCreateTrailEvent()
    {
        if (Status != JourneyStatus.Active || PendingEncounter is not null || RemainingDistance == 0)
        {
            return null;
        }

        if (DaysTravelled != 1)
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

public sealed record JourneyProgress(int DistanceTravelled, bool Completed);

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
        DomainInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(inventory);

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

        var capabilities = CapabilityResolver.Resolve(inventory);
        var mountedTravelAvailable = capabilities.MountedTravelAvailable;
        var travelMode = mountedTravelAvailable ? TravelMode.Mounted : TravelMode.Foot;
        var horseState = inventory.GetHorseState();
        var canteenState = inventory.GetCanteenState();
        var routeProfile = BuildRouteProfile(trail);
        var totalDistance = routeProfile.TotalDistance;
        var expectedDays = routeProfile.ExpectedDays(travelMode);
        var availableFood = inventory.GetQuantity(ItemKind.Food);
        var availableHorseFeed = inventory.GetQuantity(ItemKind.HorseFeed);
        var grazingAvailable = JourneyUpkeepRules.HasGrazing(routeProfile.Terrain);
        var routeWaterSecure = JourneyUpkeepRules.HasRouteWater(routeProfile.WaterFeature);
        var livingHorse = horseState is not null && !horseState.IsDead;
        var requiredFood = expectedDays;
        var requiredHorseFeed = livingHorse && !grazingAvailable ? expectedDays : 0;
        var requiredCanteenCharges = routeWaterSecure ? 0 : expectedDays * JourneyUpkeepRules.WaterChargesRequiredPerDay(horseState);
        var availableCanteenCharges = canteenState?.Charges ?? 0;
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
            warnings.Add("You do not have enough canteen water to cover the whole trail.");
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
            totalDistance,
            totalDistance,
            expectedDays,
            expectedDays,
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

    private static TravelRouteProfile BuildRouteProfile(Trail trail)
    {
        var totalDistance = trail.Risk switch
        {
            TrailRisk.Low => 12,
            TrailRisk.Moderate => 18,
            TrailRisk.High => 24,
            _ => 18
        };

        var mountedDailyProgress = trail.Risk switch
        {
            TrailRisk.Low => 12,
            TrailRisk.Moderate => 13,
            TrailRisk.High => 8,
            _ => 10
        };

        var footDailyProgress = trail.Risk switch
        {
            TrailRisk.Low => 6,
            TrailRisk.Moderate => 5,
            TrailRisk.High => 4,
            _ => 5
        };

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
            totalDistance,
            mountedDailyProgress,
            footDailyProgress,
            warnings);
    }
}
