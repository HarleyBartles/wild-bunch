using WildBunch.Application.Dev.Models;

namespace WildBunch.Application.Dev.Queries;

/// <summary>
/// Handler for GetTownLayoutSaltsQuery. Returns the current layout salts
/// from the game session, or defaults if none are set.
/// </summary>
public sealed class GetTownLayoutSaltsHandler
{
    public TownLayoutSaltsDto Handle(GetTownLayoutSaltsQuery query)
    {
        // TODO: Load game session and return DevLayoutSalts
        // For now, return placeholder
        return new TownLayoutSaltsDto(
            "1.0.0",
            "placeholder-buildings",
            "placeholder-roads",
            "placeholder-dirt",
            "placeholder-props");
    }
}
