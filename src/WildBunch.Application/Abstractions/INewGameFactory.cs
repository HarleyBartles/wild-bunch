using WildBunch.Domain.Game;

namespace WildBunch.Application.Abstractions;

public interface INewGameFactory
{
    GameSession Create(string playerName);
}
