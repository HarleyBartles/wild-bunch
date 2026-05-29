using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Abstractions;

public interface INewGameFactory
{
    GameSession Create(string playerName, TravelDifficulty travelDifficulty = TravelDifficulty.Normal);
}
