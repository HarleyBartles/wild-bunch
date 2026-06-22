using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Games.Commands;

public sealed class ReadWantedPostersHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameSessionUnitOfWork _gameSessionUnitOfWork;
    private readonly JournalResolver _journalResolver;

    public ReadWantedPostersHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        JournalResolver journalResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _gameSessionUnitOfWork = gameSessionUnitOfWork;
        _journalResolver = journalResolver;
    }

    public async Task<WantedPostersResultDto> HandleAsync(
        ReadWantedPostersCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await _gameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);

        var actionResult = session.ReadWantedPosters();

        if (actionResult.SessionChanged)
        {
            await _gameSessionRepository.StoreAsync(session, cancellationToken: cancellationToken).ConfigureAwait(false);
            await _gameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return new WantedPostersResultDto(
            actionResult.Success,
            actionResult.Message,
            JournalMapper.ToDto(_journalResolver.Resolve(session)),
            WantedPosterMapper.ToDto(session.CaseFile.KnownWarrants));
    }
}
