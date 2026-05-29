using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;

namespace WildBunch.Application.Games.Queries;

public sealed class GetGameSessionHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public GetGameSessionHandler(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<GameSessionDto> HandleAsync(GetGameSessionQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(query.GameSessionId);
        var session = await _gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        return session is null
            ? throw new GameSessionNotFoundException(sessionId)
            : GameSessionMapper.ToDto(session);
    }
}
