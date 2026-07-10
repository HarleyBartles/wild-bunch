using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class GameSetupResolverDevSaltsTests
{
    [Fact]
    public void Resolve_WithDevLayoutSalts_PassesToResolvedGameSetup()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var difficulty = DifficultyEnvelope.For(GameDifficulty.Standard);
        var entropy = EntropyPolicy.For(GameEntropy.Classic);
        var devSalts = new LayoutSalts("buildings", "roads", "dirt", "props");
        
        var resolved = new GameSetupResolver().Resolve(
            seedWorld,
            difficulty,
            entropy,
            devLayoutSalts: devSalts);
        
        Assert.Equal(devSalts, resolved.DevLayoutSalts);
    }
}
