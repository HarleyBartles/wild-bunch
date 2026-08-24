using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class ResolvedGameSetupLayoutSaltsTests
{
    [Fact]
    public void ResolvedGameSetup_WithDevLayoutSalts_CreatesSuccessfully()
    {
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");
        var townId = new TownId("test-town");
        var wallet = Wallet.Starting(0m);
        var inventory = Inventory.Empty();
        var travelRules = TravelRulesProfile.For(GameDifficulty.Standard);
        var saltSource = SaltSource.CreateRuntime();
        
        var setup = new ResolvedGameSetup(
            null!,
            GameDifficulty.Standard,
            GameEntropy.Classic,
            null!,
            townId,
            null!,
            wallet,
            inventory,
            0,
            travelRules,
            saltSource,
            "seed",
            salts);

        Assert.Equal(salts, setup.DevLayoutSalts);
    }
}
