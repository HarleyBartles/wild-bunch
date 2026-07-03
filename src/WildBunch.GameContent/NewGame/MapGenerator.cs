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

        // If an outlier was added, we need to add an extra town name for it
        if (placement.OutlierSlot.HasValue)
        {
            var outlierIndex = placement.OutlierSlot.Value;
            var extraName = SeedWorldCatalog.DeriveTownNames(
                seedWorld.WorldVariant,
                townCount: 1,
                accusationIndex: 0,
                defaultCulpritIndex: 0,
                cashBonus: 0,
                prosperityPalette: seedWorld.ProsperityPalette,
                servicesPalette: seedWorld.ServicesPalette);
            var townNamesList = townNames.ToList();
            townNamesList.Add(extraName[0]);
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
