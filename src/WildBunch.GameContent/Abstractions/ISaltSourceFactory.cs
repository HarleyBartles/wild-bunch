using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.Abstractions;

public interface ISaltSourceFactory
{
    SaltSource Create(string? setupSeedCode, GameDifficulty gameDifficulty);
}
