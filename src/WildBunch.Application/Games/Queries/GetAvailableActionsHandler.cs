using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Actions;

namespace WildBunch.Application.Games.Queries;

public sealed class GetAvailableActionsHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ActionAvailabilityResolver _actionAvailabilityResolver;

    public GetAvailableActionsHandler(
        IGameSessionRepository gameSessionRepository,
        ActionAvailabilityResolver actionAvailabilityResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _actionAvailabilityResolver = actionAvailabilityResolver;
    }

    public async Task<IReadOnlyList<AvailableActionDto>> HandleAsync(
        GetAvailableActionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(query.GameSessionId);
        var session = await _gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        var availableActions = _actionAvailabilityResolver.Resolve(session);
        return AvailableActionMapper.ToDto(availableActions);
    }
}
