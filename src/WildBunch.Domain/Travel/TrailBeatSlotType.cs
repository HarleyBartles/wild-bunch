namespace WildBunch.Domain.Travel;

/// <summary>
/// Names the four beat slot types for a trail day. This is a naming layer over the existing
/// <see cref="TravelDayEncounterCategory"/> system — no generator or roll-up change.
/// See BUNCH-5 and the Linear planning note on trail-day beat slots.
/// </summary>
public enum TrailBeatSlotType
{
    Quiet = 0,
    Minor = 1,
    Eventful = 2,
    Interrupting = 3
}
