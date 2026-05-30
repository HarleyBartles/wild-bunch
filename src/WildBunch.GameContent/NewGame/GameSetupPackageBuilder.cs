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

        if (seed.IsCanonical)
        {
            return BuildCanonical(seed);
        }

        var seedCode = GameSetupSeedCodec.GetStableKey(seed);
        var travelRulesProfile = TravelRulesProfile.For(seed.Difficulty);
        var worldSetup = SeedWorldBuilder.CreateWorld(seedCode, travelRulesProfile, seed.Options);
        var caseFile = SeedCaseBuilder.CreateCaseFile(seedCode);
        var startingInventory = SeedInventoryBuilder.CreateStartingLoadout(seedCode, travelRulesProfile, seed.Options);
        var startingWallet = SeedInventoryBuilder.CreateStartingWallet(seedCode, seed.Difficulty, seed.Options);

        return new GameSetupPackage(
            seed,
            seed.Difficulty,
            travelRulesProfile,
            worldSetup.World,
            worldSetup.StartingTownId,
            startingWallet,
            startingInventory,
            caseFile);
    }

    private static GameSetupPackage BuildCanonical(GameSetupSeed seed)
    {
        var travelRulesProfile = TravelRulesProfile.For(seed.Difficulty);
        var worldSetup = SeedWorldBuilder.CreateCanonicalWorld();
        var caseFile = SeedCaseBuilder.CreateCanonicalCaseFile();
        var startingInventory = SeedInventoryBuilder.CreateCanonicalLoadout(travelRulesProfile);

        return new GameSetupPackage(
            seed,
            seed.Difficulty,
            travelRulesProfile,
            worldSetup.World,
            worldSetup.StartingTownId,
            Wallet.Starting(25m),
            startingInventory,
            caseFile);
    }
}
