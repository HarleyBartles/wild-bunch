using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.Abstractions;

public interface INewGameFactory
{
    GameSession Create(
        string playerName,
        TravelDifficulty travelDifficulty = TravelDifficulty.Normal,
        string? setupSeedCode = null,
        AdventureRandomnessPolicy entropy = AdventureRandomnessPolicy.Standard,
        string? startingTownId = null);
}
