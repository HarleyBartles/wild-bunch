using WildBunch.Domain.Events;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Child domain component inside the session boundary that owns travel/journey
/// state and behavior. Receives narrow context records, returns results plus
/// events-to-produce. Does NOT reference the parent aggregate, produce events
/// directly, enter action context, adjust cash, or mutate CaseFile/TownVisitState/Player.
/// See BUNCH-119 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class JourneyLoop
{
    private readonly List<TravelDiaryDayState> _travelDiaryDays = [];
    private readonly List<TravelJourneySnapshot> _completedJourneyHistory = [];
    private int _nextJourneySequence = 1;
    private DevTravelOverride? _pendingDevTravelOverride;
    private TravelJourney? _journey;

    internal JourneyLoop(
        TravelJourney? journey,
        IReadOnlyList<TravelJourneySnapshot>? completedJourneyHistory)
    {
        _journey = journey;
        if (completedJourneyHistory is not null)
        {
            _completedJourneyHistory.AddRange(completedJourneyHistory);
        }
        _nextJourneySequence = CalculateNextJourneySequence(journey, _completedJourneyHistory);
    }

    internal TravelJourney? Journey => _journey;
    internal IReadOnlyList<TravelDiaryDayState> TravelDiaryDays => _travelDiaryDays;
    internal IReadOnlyList<TravelJourneySnapshot> CompletedJourneyHistory => _completedJourneyHistory;
    internal int NextJourneySequence => _nextJourneySequence;
    internal DevTravelOverride? PendingDevTravelOverride => _pendingDevTravelOverride;

    // Command methods — filled in by Tasks 3–7
    // Apply methods — filled in by Task 8

    internal void Apply(JourneyStarted e)
    {
        _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
        _nextJourneySequence = e.JourneySnapshot.JourneySequence + 1;
        _travelDiaryDays.Clear();
    }

    internal void Apply(TravelDayAdvanced e)
    {
        _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
    }

    internal void Apply(TrailEventApplied e)
    {
        _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
    }

    internal void Apply(JourneyEncounterResolved e)
    {
        _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
    }

    internal void Apply(JourneyCompleted e)
    {
        _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
    }

    internal void Apply(JourneyArrivalAcknowledged e)
    {
        _completedJourneyHistory.Add(e.JourneySnapshot);
        _journey = null;
    }

    internal void Apply(DevTravelOverrideForced e)
    {
        _pendingDevTravelOverride = new DevTravelOverride(
            e.ForcedCategory,
            e.FoeProfile,
            e.EncounterMessage);
    }

    internal void Apply(DevTravelOverrideCleared e)
    {
        _pendingDevTravelOverride = null;
    }

    internal void Apply(DevTravelOverrideConsumed e)
    {
        _pendingDevTravelOverride = null;
    }

    internal JourneyLoopResult<bool> ForceDevTravelOverride(ForceDevTravelOverrideContext context)
    {
        if (_journey is null || _journey.Status != JourneyStatus.Active)
        {
            throw new InvalidOperationException("Cannot force a travel override without an active journey.");
        }
        if (_journey.PendingEncounter is not null)
        {
            throw new InvalidOperationException("Cannot force a travel override while an encounter is pending.");
        }

        var e = new DevTravelOverrideForced
        {
            ForcedCategory = context.Override.ForcedCategory,
            FoeProfile = context.Override.FoeProfile,
            EncounterMessage = context.Override.EncounterMessage
        };
        return new JourneyLoopResult<bool>(true, [e]);
    }

    internal JourneyLoopResult<bool> ClearDevTravelOverride()
    {
        if (_pendingDevTravelOverride is null)
        {
            return new JourneyLoopResult<bool>(true, []); // No-op, idempotent
        }

        return new JourneyLoopResult<bool>(true, [new DevTravelOverrideCleared()]);
    }

    internal JourneyLoopResult<TravelJourneyStepResult> StartJourney(StartJourneyContext context)
    {
        if (_journey is not null)
        {
            return new JourneyLoopResult<TravelJourneyStepResult>(
                TravelJourneyStepResult.Failed("You are already on the trail."),
                []);
        }

        var newJourney = TravelJourney.Start(
            context.Preview,
            _nextJourneySequence,
            BuildJourneyOpeningNarration(context.Preview));
        var startMessage = $"You set out from {context.Preview.OriginTownName} toward {context.Preview.DestinationTownName} {DescribeTravelMode(context.Preview.TravelMode)}. The route is {context.Preview.RideDayDistance:0.##} ride-day unit(s) and should take {context.Preview.ExpectedDays} day(s). {DescribeCanteenCoverage(context.Preview)}.";

        var e = new JourneyStarted
        {
            JourneySnapshot = newJourney.ToSnapshot(context.TravelRules),
            DiaryMessage = startMessage,
            PursuitHeat = 0
        };

        var result = new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            startMessage,
            startMessage,
            0,
            newJourney.ToSnapshot(context.TravelRules));

        return new JourneyLoopResult<TravelJourneyStepResult>(result, [e]);
    }

    internal JourneyLoopResult<JourneyArrivalAcknowledgementResult> AcknowledgeJourneyArrival(
        AcknowledgeJourneyArrivalContext context)
    {
        if (_journey is null)
        {
            return new JourneyLoopResult<JourneyArrivalAcknowledgementResult>(
                JourneyArrivalAcknowledgementResult.Failed("No completed journey is waiting to be acknowledged."),
                []);
        }

        if (_journey.Status != JourneyStatus.Completed)
        {
            return new JourneyLoopResult<JourneyArrivalAcknowledgementResult>(
                JourneyArrivalAcknowledgementResult.Failed(
                    "The journey is not ready to be acknowledged.",
                    _journey.ToSnapshot(context.TravelRules)),
                []);
        }

        var completedSnapshot = _journey.ToSnapshot(context.TravelRules);
        var arrivalMessage = $"You step into {completedSnapshot.DestinationTownName} and put the trail behind you.";

        var e = new JourneyArrivalAcknowledged
        {
            JourneySequence = completedSnapshot.JourneySequence,
            JourneySnapshot = completedSnapshot,
            DiaryMessage = string.Empty
        };

        var result = new JourneyArrivalAcknowledgementResult(true, arrivalMessage, completedSnapshot);
        return new JourneyLoopResult<JourneyArrivalAcknowledgementResult>(result, [e]);
    }

    internal void RestoreTravelDiaryDays(IReadOnlyList<TravelDiaryDayState> days)
    {
        _travelDiaryDays.Clear();
        _travelDiaryDays.AddRange(days);
    }

    internal void RestorePendingDevTravelOverride(DevTravelOverride? overrideValue)
    {
        _pendingDevTravelOverride = overrideValue;
    }

    private static int CalculateNextJourneySequence(
        TravelJourney? journey,
        IReadOnlyList<TravelJourneySnapshot> completedJourneyHistory)
    {
        var maxSequence = journey?.JourneySequence ?? 0;

        if (completedJourneyHistory.Count > 0)
        {
            maxSequence = Math.Max(maxSequence, completedJourneyHistory.Max(history => history.JourneySequence));
        }

        return Math.Max(1, maxSequence + 1);
    }

    private static string BuildJourneyOpeningNarration(TravelPreview preview)
    {
        var baselineRidePhrase = $"{preview.BaselineRideDays}-day {DescribeTerrain(preview.RouteProfile.Terrain)} ride";
        var travelMode = DescribeTravelMode(preview.TravelMode);
        var risk = DescribeRisk(preview.RouteProfile.Risk);
        var waterPressure = preview.WaterSecure
            ? $"I had enough water for the base trail, though the canteen still needed watching on a {preview.ExpectedDays}-day run."
            : $"This dry trail asked for {preview.CanteenChargesPerDay} canteen charge(s) a day, and I did not have much slack.";
        var foodPressure = preview.AvailableFood <= preview.ExpectedDays
            ? "My food was tight enough that I noticed every meal."
            : "My food should have held if the trail behaved itself.";
        var horsePressure = preview.HorseState is null
            ? "I was traveling without a horse, so the road had to be enough."
            : preview.MountedTravelAvailable
                ? "My horse was fit enough to carry me for now."
                : "My horse was not fit for mounted travel, so I needed to mind the pace.";

        var openingSentence = preview.TravelMode == TravelMode.Foot
            ? preview.ExpectedDays != preview.BaselineRideDays
                ? $"I set out for {preview.DestinationTownName} on a {baselineRidePhrase}, but without a horse it would take {preview.ExpectedDays} days on foot."
                : $"I set out for {preview.DestinationTownName} on a {baselineRidePhrase} on foot."
            : $"I set out for {preview.DestinationTownName} on a {baselineRidePhrase} {travelMode}.";

        return $"{openingSentence} {risk} {waterPressure} {foodPressure} {horsePressure}";
    }

    private static string DescribeTravelMode(TravelMode travelMode)
        => travelMode == TravelMode.Mounted ? "by mounted travel" : "on foot";

    private static string DescribeCanteenCoverage(TravelPreview preview)
        => DescribeCanteenCoverage(preview.RouteProfile.WaterFeature, preview.CanteenChargesPerDay, preview.CanteenReserveCharges, preview.DelayMarginDays);

    private static string DescribeCanteenCoverage(TravelJourneySnapshot snapshot)
        => DescribeCanteenCoverage(snapshot.RouteProfile.WaterFeature, snapshot.CanteenChargesPerDay, snapshot.CanteenReserveCharges, snapshot.DelayMarginDays);

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

    private static string DescribeTerrain(TrailTerrain terrain)
        => terrain switch
        {
            TrailTerrain.OpenRange => "open-range",
            TrailTerrain.Hills => "hill country",
            TrailTerrain.Badlands => "badlands",
            TrailTerrain.Mountains => "mountain",
            _ => "trail"
        };

    private static string DescribeRisk(TrailRisk risk)
        => risk switch
        {
            TrailRisk.Low => "The route looks steady enough for now.",
            TrailRisk.Moderate => "The route has some teeth, so I will keep my eyes open.",
            TrailRisk.High => "The route looks rough enough to demand respect.",
            _ => "The route is hard to read."
        };
}
