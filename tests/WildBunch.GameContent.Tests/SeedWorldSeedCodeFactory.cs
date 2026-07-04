using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

internal static class SeedWorldSeedCodeFactory
{
    /// <summary>
    /// Creates a UUID seed code that produces a SeedWorld with the given
    /// world variant and case fields. Uses 8 towns with default palettes.
    /// </summary>
    internal static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong salt)
    {
        var variant = (SeedWorldVariant)worldVariant;
        var townCount = 8;
        var prosperityPalette = ProsperityPalette.UniformProsperous;
        var servicesPalette = ServicesPalette.HubTelegraph;
        var clusterCount = 1; var graphDensity = GraphDensity.Sparse;

        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, accusationIndex, defaultCulpritIndex,
            cashBonus, prosperityPalette, servicesPalette);
        var selectedTownIds = townNames.Select(t => t.Id).ToArray();
        var townServices = townNames
            .Select((t, i) => (t.Id, Services: ServicesPalettes.Resolve(servicesPalette, i)))
            .ToDictionary(x => x.Id, x => x.Services);
        var trails = Array.Empty<SeedWorldTrail>();

        var target = new SeedWorld(
            Guid.Empty,
            variant,
            townCount,
            servicesPalette,
            prosperityPalette,
            clusterCount, graphDensity,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            selectedTownIds,
            townServices,
            trails,
            OutlierSlotType: 0);

        return SeedWorldResolver.CreateRepresentativeSeedCode(target);
    }

    /// <summary>
    /// Creates a UUID seed code for a SeedWorld with specific services and
    /// prosperity palettes. Uses 8 towns.
    /// </summary>
    internal static Guid CreateSeedCodeWithServices(
        byte worldVariant,
        byte accusationIndex,
        byte defaultCulpritIndex,
        byte cashBonus,
        ServicesPalette servicesPalette,
        ProsperityPalette prosperityPalette = ProsperityPalette.UniformProsperous)
    {
        var variant = (SeedWorldVariant)worldVariant;
        var townCount = 8;
        var clusterCount = 1; var graphDensity = GraphDensity.Sparse;

        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, accusationIndex, defaultCulpritIndex,
            cashBonus, prosperityPalette, servicesPalette);
        var selectedTownIds = townNames.Select(t => t.Id).ToArray();
        var townServices = townNames
            .Select((t, i) => (t.Id, Services: ServicesPalettes.Resolve(servicesPalette, i)))
            .ToDictionary(x => x.Id, x => x.Services);
        var trails = Array.Empty<SeedWorldTrail>();

        var target = new SeedWorld(
            Guid.Empty,
            variant,
            townCount,
            servicesPalette,
            prosperityPalette,
            clusterCount, graphDensity,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            selectedTownIds,
            townServices,
            trails,
            OutlierSlotType: 0);

        return SeedWorldResolver.CreateRepresentativeSeedCode(target);
    }
}
