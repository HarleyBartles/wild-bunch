using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Application.Games.Queries;

/// <summary>
/// Generates a representative seed that encodes the selected difficulty and entropy.
/// The seed codec encodes the full starting state, so a representative seed
/// is one that decodes to the requested difficulty and entropy.
/// </summary>
public sealed class GenerateRepresentativeSeedHandler
{
    public Task<string> HandleAsync(GenerateRepresentativeSeedQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var descriptor = StartingWorldDescriptorResolver.CreateCanonicalDescriptor(
            query.GameDifficulty,
            query.GameEntropy);

        return Task.FromResult(descriptor.SeedCodeText);
    }
}
