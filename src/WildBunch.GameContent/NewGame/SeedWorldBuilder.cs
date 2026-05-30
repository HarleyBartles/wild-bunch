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

    public static SeedWorldSetup CreateWorld(string seedCode, TravelRulesProfile travelRulesProfile, GameSetupOptionsV1 options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        ArgumentNullException.ThrowIfNull(options);

        var variant = Roll(seedCode, "world-variant") % 2 == 0
            ? SeedWorldVariant.Frontier
            : SeedWorldVariant.Rail;
        var world = SeedWorldCatalog.CreateWorld(variant);

        return new SeedWorldSetup(world, PickStartingTown(seedCode, world, options));
    }

    private static TownId PickStartingTown(string seedCode, World world, GameSetupOptionsV1 options)
    {
        var candidates = world.Towns
            .Where(town => (town.Services & TownServices.Supplies) != 0 || (town.Services & TownServices.NoticeBoard) != 0)
            .OrderBy(town => town.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidates.Length == 0)
        {
            return world.Towns.First().Id;
        }

        var label = options.StartWithHorse ? "starting-town-horse" : "starting-town-foot";
        var index = (int)(Roll(seedCode, label) % (ulong)candidates.Length);
        return candidates[index].Id;
    }

    private static ulong Roll(string seedCode, string label)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seedCode}|{label}"));
        return BitConverter.ToUInt64(bytes, 0);
    }
}
