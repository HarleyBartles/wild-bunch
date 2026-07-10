using WildBunch.Application.Dev.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Commands;

/// <summary>
/// Handler for GenerateRandomTownLayoutSaltsCommand. Generates random
/// salt values for dev exploration.
/// </summary>
public sealed class GenerateRandomTownLayoutSaltsHandler
{
    public Task<TownLayoutSaltsDto> HandleAsync(GenerateRandomTownLayoutSaltsCommand command, CancellationToken cancellationToken = default)
    {
        var randomSalt = SaltSource.CreateRuntime().Salt;
        return Task.FromResult(new TownLayoutSaltsDto(
            "1.0.0",
            randomSalt,
            randomSalt,
            randomSalt,
            randomSalt));
    }
}
