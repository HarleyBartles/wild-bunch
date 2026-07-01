using WildBunch.Domain.Events;
using WildBunch.Domain.Travel;

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
}
