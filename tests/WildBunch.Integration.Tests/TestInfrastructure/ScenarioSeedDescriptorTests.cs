using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Integration.Tests.TestInfrastructure;

public sealed class ScenarioSeedDescriptorTests
{
    [Fact]
    public void TypedDescriptorFormatsAReadableSemanticShape()
    {
        const string codecVersion = "resolver-test";

        var descriptor = ScenarioSeedDescriptor.Create("CanonicalMountedStandard")
            .WithCodecVersion(new ScenarioSeedCodecVersion(codecVersion))
            .WithEntropy(GameEntropy.Boring)
            .WithStartingTownRole(ScenarioStartingTownRole.DefaultPlayableStart)
            .WithHorse(HorseCondition.Healthy)
            .WithSaddle(SaddleState.Present)
            .WithWallet(25m)
            .WithItemCount(8)
            .WithTownCount(8)
            .WithPreview(ScenarioPreviewExpectation.Mounted(2, 2));

        Assert.Equal(
            "resolver-test|CanonicalMountedStandard|entropy=Boring|start=default-playable-start|horse=healthy|saddle=present|wallet=25|items=8|towns=8|preview=mounted:2/2",
            descriptor.FormatRequiredShapeSignature());
    }
}
