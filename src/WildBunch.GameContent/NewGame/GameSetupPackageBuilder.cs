using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

internal sealed class GameSetupPackageBuilder
{
    public GameSetupPackage Build(GameSetupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        var plan = GameSetupGenerationPlan.Create(seed);
        var worldSetup = SeedWorldBuilder.CreateWorld(plan);
        var caseFile = SeedCaseBuilder.CreateCaseFile(plan, worldSetup.World, worldSetup.StartingTownId);
        var startingInventory = SeedInventoryBuilder.CreateStartingLoadout(plan);
        var startingWallet = SeedInventoryBuilder.CreateStartingWallet(plan);

        return new GameSetupPackage(
            seed,
            seed.Difficulty,
            plan.TravelRulesProfile,
            worldSetup.World,
            worldSetup.StartingTownId,
            startingWallet,
            startingInventory,
            caseFile);
    }
}
