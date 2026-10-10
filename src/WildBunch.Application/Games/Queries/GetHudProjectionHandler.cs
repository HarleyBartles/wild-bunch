using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Queries;

public sealed class GetHudProjectionHandler
{
    private readonly IGameSessionEventReadRepository _eventReadRepository;
    private readonly HudProjector _projector;

    public GetHudProjectionHandler(
        IGameSessionEventReadRepository eventReadRepository,
        HudProjector projector)
    {
        _eventReadRepository = eventReadRepository;
        _projector = projector;
    }

    public async Task<HudProjection?> HandleAsync(
        GetHudProjectionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new GameSessionId(query.GameSessionId);
        var events = await _eventReadRepository.GetEventStreamAsync(sessionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new GameSessionNotFoundException(sessionId);

        if (!events.OfType<GameStarted>().Any())
        {
            return null;
        }

        return _projector.Project(events) with { SessionId = query.GameSessionId };
    }
}
