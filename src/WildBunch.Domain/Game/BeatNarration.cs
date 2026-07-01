namespace WildBunch.Domain.Game;

/// <summary>
/// Generates diegetic beat narration from the current TimeOfDay, TownActionContext, and town name.
/// Domain-level helper so GameSession can populate CaseInvestigationResult.BeatNarration without
/// referencing the Application layer. The Application layer's BeatNarrationRenderer delegates to this.
/// </summary>
public static class BeatNarration
{
    private static readonly Dictionary<TownActionContext, string> LocationNames = new()
    {
        { TownActionContext.SheriffOffice, "the sheriff's office" },
        { TownActionContext.Saloon, "the saloon" },
        { TownActionContext.Store, "the general store" },
        { TownActionContext.Stable, "the stable" },
        { TownActionContext.Jail, "the jail" },
        { TownActionContext.TelegraphOffice, "the telegraph office" },
        { TownActionContext.TownSquare, "the town square" },
    };

    public static string Render(TimeOfDay timeOfDay, TownActionContext context, string townName)
    {
        var location = LocationNames.TryGetValue(context, out var name) ? name : "town";
        return $"You spent the {timeOfDay.ToString().ToLowerInvariant()} at {location} in {townName}";
    }
}
