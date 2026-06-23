namespace WildBunch.Domain.Game;

/// <summary>
/// Names the four turn slots of a town day. This is a naming layer over the existing
/// int <see cref="GameClock.Turn"/> (0-3) — no persistence format change. See ADR-0028
/// and BUNCH-80 clock/turn correction.
/// </summary>
public enum TimeOfDay
{
    Morning = 0,
    Afternoon = 1,
    Evening = 2,
    Night = 3
}
