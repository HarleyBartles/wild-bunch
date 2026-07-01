using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Mapping;

/// <summary>
/// Application-layer renderer that delegates to the Domain-level <see cref="BeatNarration"/> helper.
/// Domain owns the narration logic because GameSession needs it; Application delegates — never the reverse.
/// </summary>
public static class BeatNarrationRenderer
{
    public static string Render(TimeOfDay timeOfDay, TownActionContext context, string townName)
        => BeatNarration.Render(timeOfDay, context, townName);
}
