using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;
using Xunit.Sdk;

namespace WildBunch.Integration.Tests.TestInfrastructure;

public sealed class ScenarioSeedCatalogTests
{
    [Fact]
    public void CachedScenarioSeedContractsStayCurrent()
    {
        ScenarioSeedCatalog.AssertCatalogContractsCurrent();
    }

    [Fact]
    public void CachedScenarioSeedDriftFailuresNameTheFixtureAndShape()
    {
        var fixture = new ScenarioSeedFixture(
            Name: "DriftedFixture",
            SeedCode: ScenarioSeedCatalog.CanonicalMountedStandard.SeedCode,
            GameDifficulty: GameDifficulty.Standard,
            GameEntropy: GameEntropy.Classic,
            ResolverContractVersion: SeedWorldResolver.ResolverContractVersion,
            RequiredShapeSignature: "resolver-v3|DriftedFixture|unexpected-shape",
            DescribeShapeSignature: static (_, _) => "resolver-v3|DriftedFixture|actual-shape",
            AssertCreatedSessionContract: _ => { });

        var exception = Assert.Throws<XunitException>(() => fixture.AssertCachedFixtureContract());

        Assert.Contains("Cached scenario seed 'DriftedFixture' no longer satisfies required shape 'resolver-v3|DriftedFixture|unexpected-shape'. Observed required-shape signature 'resolver-v3|DriftedFixture|actual-shape'.", exception.Message);
        Assert.Contains("Regenerate this fixture through the boring scenario path", exception.Message);
    }
}
