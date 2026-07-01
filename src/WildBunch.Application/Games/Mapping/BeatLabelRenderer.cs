using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Mapping;

/// <summary>
/// Renders diegetic Western beat labels from the existing <see cref="TimeOfDay"/> enum and day number.
/// This is a presentation-layer naming over the existing <see cref="GameClock.Turn"/> int — no domain state change.
/// </summary>
public static class BeatLabelRenderer
{
    public static string Render(TimeOfDay timeOfDay, int day)
        => $"{timeOfDay} of Day {day}";
}
