using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class MapGeneratorDevSaltsTests
{
    [Fact]
    public void Generate_WithDevLayoutSalts_PassesToTownLayoutGenerator()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var source = new GameSetupDeterministicSource(SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld).ToString());
        var devSalts = new LayoutSalts("buildings", "roads", "dirt", "props");
        
        var world = MapGenerator.Generate(
            seedWorld,
            source,
            GameEntropy.Classic,
            null,
            devSalts);
        
        Assert.NotNull(world);
        Assert.NotNull(world.Towns);
    }
}
