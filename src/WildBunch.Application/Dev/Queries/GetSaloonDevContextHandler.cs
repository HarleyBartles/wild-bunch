using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Mapping;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Queries;

public sealed class GetSaloonDevContextHandler
{
    private readonly IGameSessionRepository _repository;

    public GetSaloonDevContextHandler(IGameSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<SaloonDevContextDto> HandleAsync(GetSaloonDevContextQuery query, CancellationToken cancellationToken = default)
    {
        var sessionId = new GameSessionId(query.SessionId);
        var session = await _repository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        return SaloonDevContextMapper.ToDto(session);
    }
}
