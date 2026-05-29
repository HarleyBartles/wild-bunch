using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Games.Queries;

public sealed class GetJournalHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly JournalResolver _journalResolver;

    public GetJournalHandler(IGameSessionRepository gameSessionRepository, JournalResolver journalResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _journalResolver = journalResolver;
    }

    public async Task<JournalDto> HandleAsync(GetJournalQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(query.GameSessionId);
        var session = await _gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        var snapshot = _journalResolver.Resolve(session);
        return JournalMapper.ToDto(snapshot);
    }
}
