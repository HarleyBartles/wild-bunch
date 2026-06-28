using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.GameContent.NewGame;

public sealed class RuntimeSaltSourceFactory : ISaltSourceFactory
{
    public SaltSource Create(string? setupSeedCode, GameDifficulty gameDifficulty)
        => SaltSource.CreateRuntime();
}
