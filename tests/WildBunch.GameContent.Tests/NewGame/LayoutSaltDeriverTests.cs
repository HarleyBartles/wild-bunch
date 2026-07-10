using WildBunch.Domain.Game;
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
        var source = new GameSetupDeterministicSource(SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld).ToString());
        
        var salts1 = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyPolicy, townId, 0, source, null);
        var salts2 = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyPolicy, townId, 0, source, null);
        
        Assert.Equal(salts1, salts2);
    }

    [Fact]
    public void DeriveLayoutSalts_WithDevSalts_UsesDevSalts()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var entropyPolicy = EntropyPolicy.For(GameEntropy.Classic);
        var townId = new TownId("town-1");
        var source = new GameSetupDeterministicSource(SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld).ToString());
        var devSalts = new LayoutSalts("dev-buildings", "dev-roads", "dev-dirt", "dev-props");
        
        var salts = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyPolicy, townId, 0, source, devSalts);
        
        Assert.Equal(devSalts, salts);
    }

    [Fact]
    public void DeriveLayoutSalts_DifferentEntropyMode_ProducesDifferentSalts()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var entropyRuntime = EntropyPolicy.For(GameEntropy.Classic);
        var entropyFixed = EntropyPolicy.For(GameEntropy.Boring);
        var townId = new TownId("town-1");
        var source = new GameSetupDeterministicSource(SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld).ToString());
        
        var saltsRuntime = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyRuntime, townId, 0, source, null);
        var saltsFixed = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyFixed, townId, 0, source, null);
        
        Assert.NotEqual(saltsRuntime, saltsFixed);
    }
}
