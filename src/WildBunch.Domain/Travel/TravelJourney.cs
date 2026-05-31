using WildBunch.Domain.Inventory;

namespace WildBunch.Domain.Travel;

public sealed record JourneyProgress(decimal RideDayDistanceTravelled, bool Completed);

public sealed class TravelJourney
{
    internal TravelJourney(TravelPreview preview, int journeySequence, string? openingNarration = null)
    {
        Preview = preview;
        JourneySequence = journeySequence;
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

    public int JourneySequence { get; }

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
        return new TravelJourney(preview, journeySequence: 1);
    }

    public static TravelJourney Start(TravelPreview preview, string? openingNarration)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return new TravelJourney(preview, 1, openingNarration);
    }

    public static TravelJourney Start(TravelPreview preview, int journeySequence)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return new TravelJourney(preview, journeySequence);
    }

    public static TravelJourney Start(TravelPreview preview, int journeySequence, string? openingNarration)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return new TravelJourney(preview, journeySequence, openingNarration);
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

        var journey = new TravelJourney(preview, Math.Max(1, snapshot.JourneySequence))
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

    public void UpdatePendingEncounter(JourneyEncounterState encounter)
    {
        ArgumentNullException.ThrowIfNull(encounter);

        PendingEncounter = encounter;

        if (CurrentDayPlan is null)
        {
            Status = JourneyStatus.Interrupted;
            return;
        }

        var updatedEncounters = CurrentDayPlan.Encounters
            .Select((dayEncounter, index) => index == CurrentDayPlan.CurrentEncounterIndex
                ? dayEncounter with { PendingEncounter = encounter }
                : dayEncounter)
            .ToArray();

        CurrentDayPlan = CurrentDayPlan with
        {
            Encounters = updatedEncounters
        };
        Status = JourneyStatus.Interrupted;
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
            JourneySequence,
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
