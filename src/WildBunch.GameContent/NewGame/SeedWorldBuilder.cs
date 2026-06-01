using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal sealed record SeedWorldSetup(World World, TownId StartingTownId);

internal static class SeedWorldBuilder
{
    public static SeedWorldSetup CreateCanonicalWorld()
    {
        var world = SeedWorldCatalog.CreateWorld(SeedWorldVariant.Canonical);
        return new SeedWorldSetup(world, SeedWorldCatalog.PinecrossId);
    }

    public static SeedWorldSetup CreateWorld(StartingWorldGenerationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.IsCanonical)
        {
            return CreateCanonicalWorld();
        }

        var world = SeedWorldCatalog.CreateWorld(plan.WorldVariant);
        return new SeedWorldSetup(world, PickStartingTown(plan, world));
    }

    private static TownId PickStartingTown(StartingWorldGenerationPlan plan, World world)
    {
        var candidates = world.Towns
            .Where(town => (town.Services & TownServices.Supplies) != 0 || (town.Services & TownServices.NoticeBoard) != 0)
            .OrderBy(town => town.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidates.Length == 0)
        {
            return world.Towns.First().Id;
        }

        var label = plan.Descriptor.World.StartingTownSelectionKey;
        var index = plan.Source.PickIndex(label, candidates.Length);
        return candidates[index].Id;
    }
}
