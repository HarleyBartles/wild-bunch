using WildBunch.Application.Games.Queries;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Tests.Games.Queries;

public class GenerateRepresentativeSeedHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsSeedThatEncodesRequestedDifficultyAndEntropy()
    {
        var handler = new GenerateRepresentativeSeedHandler();

        var query = new GenerateRepresentativeSeedQuery(GameDifficulty.Challenging, GameEntropy.Boring);
        var seed = await handler.HandleAsync(query, CancellationToken.None);

        Assert.NotNull(seed);
        Assert.True(Guid.TryParse(seed, out _));

        // Verify the seed decodes to the requested difficulty and entropy
        var descriptor = WildBunch.GameContent.NewGame.StartingWorldDescriptorResolver.Resolve(seed);
        Assert.Equal(GameDifficulty.Challenging, descriptor.GameDifficulty);
        Assert.Equal(GameEntropy.Boring, descriptor.GameEntropy);
    }

    [Fact]
    public async Task HandleAsync_WithDefaults_ReturnsStandardClassicSeed()
    {
        var handler = new GenerateRepresentativeSeedHandler();

        var query = new GenerateRepresentativeSeedQuery();
        var seed = await handler.HandleAsync(query, CancellationToken.None);

        var descriptor = WildBunch.GameContent.NewGame.StartingWorldDescriptorResolver.Resolve(seed);
        Assert.Equal(GameDifficulty.Standard, descriptor.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, descriptor.GameEntropy);
    }
}
