using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.Abstractions;

public interface INewGameFactory
{
    GameSession Create(
        string playerName,
        GameDifficulty gameDifficulty = GameDifficulty.Normal,
        string? setupSeedCode = null,
        GameEntropy entropy = GameEntropy.Standard,
        string? startingTownId = null);
}
