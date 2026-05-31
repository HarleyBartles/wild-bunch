using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;

namespace WildBunch.Application.Games.Queries;

public sealed class GetGameSessionHandler
{
    private readonly IGameSessionReadRepository _gameSessionReadRepository;

    public GetGameSessionHandler(IGameSessionReadRepository gameSessionReadRepository)
    {
        _gameSessionReadRepository = gameSessionReadRepository;
    }

    public async Task<GameSessionDto> HandleAsync(GetGameSessionQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(query.GameSessionId);
        var session = await _gameSessionReadRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new GameSessionNotFoundException(sessionId);

        return GameSessionMapper.ToDto(session);
    }
}
