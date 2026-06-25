using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Queries;

public sealed class GetSessionAuditHandler
{
    private readonly IGameSessionRepository _repository;
    private readonly FullAuditProjector _auditProjector;

    public GetSessionAuditHandler(IGameSessionRepository repository, FullAuditProjector auditProjector)
    {
        _repository = repository;
        _auditProjector = auditProjector;
    }

    public async Task<SessionAuditDto> HandleAsync(GetSessionAuditQuery query, CancellationToken cancellationToken = default)
    {
        var sessionId = new GameSessionId(query.SessionId);
        var session = await _repository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        var events = await _repository.GetEventStreamAsync(sessionId, 0, cancellationToken).ConfigureAwait(false);
        var projection = _auditProjector.Project(events);

        return new SessionAuditDto(
            query.SessionId,
            projection.Entries
                .Select(e => new SessionAuditEntryDto(e.Sequence, e.EventType, e.Summary, e.OccurredAtUtc))
                .ToList());
    }
}
