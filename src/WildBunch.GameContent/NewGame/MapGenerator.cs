using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class MapGenerator
{
    public static World Generate(SeedWorld seedWorld, GameSetupDeterministicSource source,
        GameEntropy entropy, SaltSource? saltSource)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(source);

        var townNames = SeedWorldCatalog.DeriveTownNames(
            seedWorld.WorldVariant,
            seedWorld.TownCount,
            seedWorld.AccusationIndex,
            seedWorld.DefaultCulpritIndex,
            seedWorld.CashBonus,
            seedWorld.ProsperityPalette,
            seedWorld.ServicesPalette);

        var placement = ClusterPlacementGenerator.Place(seedWorld, source, entropy, saltSource);

        // If an outlier was added, we need to add an extra town name for it.
        // Derive the full shuffled pool from the outlier seed and pick the first
        // name not already in the main town names. This guarantees uniqueness —
        // the previous approach called DeriveTownNames(townCount: 1, ...) which
        // uses a different seed/shuffle and could pick a name already in the main list.
        if (placement.OutlierSlot.HasValue)
        {
            var existingIds = new HashSet<string>(townNames.Select(t => t.Id));
            var outlierPool = SeedWorldCatalog.DeriveTownNames(
                seedWorld.WorldVariant,
                townCount: SeedWorldCatalog.NamePool.Count,
                accusationIndex: 0,
                defaultCulpritIndex: 0,
                cashBonus: 0,
                prosperityPalette: seedWorld.ProsperityPalette,
                servicesPalette: seedWorld.ServicesPalette);
            var outlierName = outlierPool.First(entry => !existingIds.Contains(entry.Id));
            var townNamesList = townNames.ToList();
            townNamesList.Add(outlierName);
            townNames = townNamesList;
        }

        var edges = TrailGraphGenerator.Generate(seedWorld, placement.Towns, placement.ClusterAssignments,
            source, entropy, saltSource);
        var townIds = townNames.Select(t => t.Id).ToArray();
        var trails = TerrainAssigner.Assign(edges, placement.Towns, placement.ClusterAssignments,
            seedWorld.WorldVariant, townIds, placement.OutlierSlot);
        var (enforcedTrails, adjustedTowns) = OutlierGuarantee.Enforce(
            trails, placement.Towns, placement.OutlierSlot, townIds);

        return SeedWorldCatalog.CreateWorld(
            seedWorld.WorldVariant,
            townNames,
            seedWorld.ServicesPalette,
            seedWorld.ProsperityPalette,
            enforcedTrails,
            townCoordinates: adjustedTowns,
            outlierSlot: placement.OutlierSlot,
            entropy,
            saltSource,
            seedWorld.SeedCode);
    }
}
