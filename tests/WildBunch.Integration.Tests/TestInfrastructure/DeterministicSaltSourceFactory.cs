using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Integration.Tests.TestInfrastructure;

internal sealed class DeterministicSaltSourceFactory : ISaltSourceFactory
{
    public SaltSource Create(string? setupSeedCode, GameDifficulty gameDifficulty)
        => SaltSource.CreateFixed(string.Empty);
}
