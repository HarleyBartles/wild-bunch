using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Abstractions;

public interface IGameSessionReadRepository
{
    Task<GameSessionReadModel?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default);
}
