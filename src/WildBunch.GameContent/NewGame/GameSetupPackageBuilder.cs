using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

internal sealed class GameSetupPackageBuilder
{
    public GameSetupPackage Build(StartingWorldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var plan = StartingWorldGenerationPlan.Create(descriptor);
        var worldSetup = SeedWorldBuilder.CreateWorld(plan);
        var caseFile = SeedCaseBuilder.CreateCaseFile(plan, worldSetup.World, worldSetup.StartingTownId);
        var startingInventory = SeedInventoryBuilder.CreateStartingLoadout(plan.TravelRulesProfile, plan);
        var startingWallet = SeedInventoryBuilder.CreateStartingWallet(plan);

        return new GameSetupPackage(
            descriptor,
            descriptor.Difficulty,
            plan.TravelRulesProfile,
            worldSetup.World,
            worldSetup.StartingTownId,
            startingWallet,
            startingInventory,
            caseFile);
    }
}
