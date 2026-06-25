namespace WildBunch.Domain.Game;

/// <summary>
/// Tracks heat: lawman pursuit pressure that accumulates from time spent in
/// town. Each full day (4 turns) in town increases heat by 1. Leaving town
/// (starting a journey) resets heat to 0. Heat does not change on the trail.
/// A future lawman system will consume heat as a pressure clock. See ADR-0029.
/// </summary>
public sealed class PursuitState
{
    public int Heat { get; private set; }

    public void IncreaseHeat(int amount)
    {
        Heat = Math.Max(0, Heat + amount);
    }

    /// <summary>
    /// Sets heat to an absolute value. Used by event-sourced Apply methods
    /// that carry the absolute heat (e.g. TravelDayAdvanced) so command-path
    /// direct mutations and replay-path event applications converge on the
    /// same state. See ADR-0028 and BUNCH-83.
    /// </summary>
    internal void SetHeat(int value)
    {
        Heat = Math.Max(0, value);
    }
}
