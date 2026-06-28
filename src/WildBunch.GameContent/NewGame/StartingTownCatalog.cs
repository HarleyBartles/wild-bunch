using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public static class StartingTownCatalog
{
    /// <summary>
    /// Returns the playable starting-town candidates for the canonical world variant.
    /// Matches SeedWorldBuilder.PickStartingTown's candidate filter: towns with Supplies or NoticeBoard services.
    /// </summary>
    public static IReadOnlyList<Town> GetStartingTownCandidates()
    {
        var world = SeedWorldCatalog.CreateWorld(SeedWorldVariant.Canonical, GameSetupDeterministicLabels.WorldTownSetDefault);
        return world.Towns
            .Where(town => (town.Services & TownServices.Supplies) != 0 || (town.Services & TownServices.NoticeBoard) != 0)
            .OrderBy(town => town.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
