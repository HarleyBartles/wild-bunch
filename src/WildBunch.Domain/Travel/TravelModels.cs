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
    HorseCondition? HorseCondition,
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
    HorseCondition? HorseCondition,
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
        HorseCondition = preview.HorseCondition;
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

    public HorseCondition? HorseCondition { get; private set; }

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
            snapshot.HorseCondition,
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
            HorseCondition = snapshot.HorseCondition
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

    public void SetHorseCondition(HorseCondition? horseCondition)
    {
        HorseCondition = horseCondition;
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
            TravelMode == TravelMode.Mounted && HorseCondition == WildBunch.Domain.Inventory.HorseCondition.Healthy,
            Preview.WaterSecure,
            Preview.TotalDistance,
            RemainingDistance,
            Preview.ExpectedDays,
            RemainingDays,
            Preview.RequiredFood,
            FoodRemaining,
            Preview.RequiredHorseFeed,
            HorseFeedRemaining,
            HorseCondition,
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
        var horseCondition = inventory.GetHorseCondition();
        var routeProfile = BuildRouteProfile(trail);
        var totalDistance = routeProfile.TotalDistance;
        var expectedDays = routeProfile.ExpectedDays(travelMode);
        var availableFood = inventory.GetQuantity(ItemKind.Food);
        var availableHorseFeed = inventory.GetQuantity(ItemKind.HorseFeed);
        var requiredFood = expectedDays;
        var requiredHorseFeed = travelMode == TravelMode.Mounted ? expectedDays : 0;
        var warnings = new List<string>(routeProfile.Warnings);

        if (!mountedTravelAvailable)
        {
            warnings.Add("Mounted travel is unavailable, so the route will continue on foot.");
        }

        if (availableFood < requiredFood)
        {
            warnings.Add("You do not have enough food to cover the full trail.");
        }

        if (travelMode == TravelMode.Mounted && availableHorseFeed < requiredHorseFeed)
        {
            warnings.Add("You do not have enough horse feed to keep the horse fresh for the whole trail.");
        }

        if (!capabilities.NormalRouteWaterSecure)
        {
            warnings.Add("A canteen would keep water secure on this route.");
        }

        var preview = new TravelPreview(
            currentTownId,
            destinationTownId,
            originTown!.Name,
            destinationTown!.Name,
            routeProfile,
            travelMode,
            mountedTravelAvailable,
            capabilities.NormalRouteWaterSecure,
            totalDistance,
            totalDistance,
            expectedDays,
            expectedDays,
            requiredFood,
            availableFood,
            requiredHorseFeed,
            availableHorseFeed,
            horseCondition,
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
