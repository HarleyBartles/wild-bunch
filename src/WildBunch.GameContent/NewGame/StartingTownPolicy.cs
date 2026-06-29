using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Validates and resolves the player's chosen starting town against the
/// generated world. This is the setup/policy seam between seed-owned map
/// generation and the final starting town stored in <see cref="ResolvedGameSetup"/>.
/// Today the policy is permissive: the player can start in any town that
/// exists in the generated world. If no starting town is supplied, a safe
/// default is used (the first town in the world — slot 0, which is always
/// present and well-connected).
/// Future seam: difficulty may constrain eligible starting towns (easy allows
/// any except accusation town, standard prefers inner/well-connected towns,
/// harder constrains to outposts). An accusation/black-spot town may become
/// non-stoppable. Difficulty should not redraw the map — it only filters
/// eligibility.
/// </summary>
internal static class StartingTownPolicy
{
    /// <summary>
    /// Resolves the starting town from the player's choice or a safe default.
    /// Throws if the player's chosen town does not exist in the generated world.
    /// </summary>
    public static TownId ResolveStartingTown(World world, TownId? playerChosenTownId)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (playerChosenTownId is not null)
        {
            if (!world.Towns.Any(town => town.Id.Equals(playerChosenTownId)))
            {
                throw new ArgumentException(
                    $"Starting town '{playerChosenTownId.Value}' is not in the generated world.",
                    nameof(playerChosenTownId));
            }

            return playerChosenTownId.Value;
        }

        // Safe default: the first town in the world (slot 0). This is always
        // present and well-connected. The default is NOT seed-authored — it
        // is a fixed property of the world topology, not a hash of the seed code.
        // Future seam: difficulty-aware eligibility may change this default.
        return world.Towns.First().Id;
    }
}
