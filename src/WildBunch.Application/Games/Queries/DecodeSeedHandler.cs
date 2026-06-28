using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;
using WildBunch.Application.Games.Models;

namespace WildBunch.Application.Games.Queries;

public sealed class DecodeSeedHandler
{
    public Task<DecodedSeedDto> HandleAsync(DecodeSeedQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!Guid.TryParse(query.SeedCode, out var seed))
        {
            throw new ArgumentException("Seed code must be a valid UUID.", nameof(query.SeedCode));
        }

        var descriptor = StartingWorldDescriptorResolver.Resolve(seed);
        return Task.FromResult(new DecodedSeedDto(descriptor.GameDifficulty, descriptor.GameEntropy));
    }
}
