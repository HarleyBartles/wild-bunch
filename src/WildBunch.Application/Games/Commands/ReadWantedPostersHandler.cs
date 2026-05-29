using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Journal;
using WildBunch.Domain.WantedPosters;

namespace WildBunch.Application.Games.Commands;

public sealed class ReadWantedPostersHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ReadWantedPostersResolver _readWantedPostersResolver;
    private readonly JournalResolver _journalResolver;

    public ReadWantedPostersHandler(
        IGameSessionRepository gameSessionRepository,
        ReadWantedPostersResolver readWantedPostersResolver,
        JournalResolver journalResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _readWantedPostersResolver = readWantedPostersResolver;
        _journalResolver = journalResolver;
    }

    public async Task<WantedPostersResultDto> HandleAsync(
        ReadWantedPostersCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await LoadSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);

        var actionResult = _readWantedPostersResolver.ReadWantedPosters(session);

        if (actionResult.SessionChanged)
        {
            await _gameSessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        }

        return new WantedPostersResultDto(
            actionResult.Success,
            actionResult.Message,
            JournalMapper.ToDto(_journalResolver.Resolve(session)));
    }

    private async Task<WildBunch.Domain.Game.GameSession> LoadSessionAsync(
        WildBunch.Domain.Game.GameSessionId sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _gameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        return session ?? throw new GameSessionNotFoundException(sessionId);
    }
}
