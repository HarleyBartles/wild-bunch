using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class LayoutSaltDeriverTests
{
    [Fact]
    public void DeriveLayoutSalts_SameInputs_ProducesSameSalts()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var entropyPolicy = EntropyPolicy.For(GameEntropy.Classic);
        var townId = new TownId("town-1");

        var salts1 = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyPolicy, townId, 0);
        var salts2 = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyPolicy, townId, 0);

        Assert.Equal(salts1, salts2);
    }

    [Fact]
    public void DeriveLayoutSalts_DifferentEntropyMode_ProducesDifferentSalts()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var entropyRuntime = EntropyPolicy.For(GameEntropy.Classic);
        var entropyFixed = EntropyPolicy.For(GameEntropy.Boring);
        var townId = new TownId("town-1");

        var saltsRuntime = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyRuntime, townId, 0);
        var saltsFixed = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyFixed, townId, 0);

        Assert.NotEqual(saltsRuntime, saltsFixed);
    }
}
