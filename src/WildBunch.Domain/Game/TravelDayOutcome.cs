namespace WildBunch.Domain.Game;

/// <summary>
/// Outcome of a single travel day advance, carried in TravelDayAdvanced events.
/// </summary>
public enum TravelDayOutcome
{
    Ongoing,
    Interrupted,
    Completed,
}
