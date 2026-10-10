using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Actions;

namespace WildBunch.Application.Games.Queries;

public sealed class GetAvailableActionsHandler
{
    private readonly IGameSessionReadRepository _gameSessionReadRepository;
    private readonly ActionAvailabilityResolver _actionAvailabilityResolver;

    public GetAvailableActionsHandler(
        IGameSessionReadRepository gameSessionReadRepository,
        ActionAvailabilityResolver actionAvailabilityResolver)
    {
        _gameSessionReadRepository = gameSessionReadRepository;
        _actionAvailabilityResolver = actionAvailabilityResolver;
    }

    public async Task<IReadOnlyList<AvailableActionDto>> HandleAsync(
        GetAvailableActionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(query.GameSessionId);
        var session = await _gameSessionReadRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new WildBunch.Application.Games.Exceptions.GameSessionNotFoundException(sessionId);

        var context = new ActionAvailabilityContext(
            session.StartFlowPhase,
            session.World,
            session.Player.CurrentTownId,
            session.Journey);
        var availableActions = _actionAvailabilityResolver.Resolve(context);
        return AvailableActionMapper.ToDto(availableActions);
    }
}
