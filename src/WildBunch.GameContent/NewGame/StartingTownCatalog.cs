using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public static class StartingTownCatalog
{
    /// <summary>
    /// Returns the playable starting-town candidates for the canonical world variant.
    /// Every town has a shop (with prosperity-driven stock), so all towns are
    /// meaningful starting points.
    /// </summary>
    public static IReadOnlyList<Town> GetStartingTownCandidates()
    {
        var world = SeedWorldFactory.CreateCanonicalWorld();
        return world.Towns
            .OrderBy(town => town.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
