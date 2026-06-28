using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.Abstractions;

public interface INewGameFactory
{
    GameSession Create(
        string playerName,
        GameDifficulty gameDifficulty = GameDifficulty.Standard,
        string? setupSeedCode = null,
        GameEntropy gameEntropy = GameEntropy.Classic,
        string? startingTownId = null);
}
