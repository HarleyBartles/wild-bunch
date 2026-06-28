using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.Abstractions;

public interface ITravelRandomnessSource
{
    TravelRandomnessState Create(string? setupSeedCode, GameDifficulty gameDifficulty);
}
