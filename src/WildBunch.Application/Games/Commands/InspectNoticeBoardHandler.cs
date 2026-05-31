using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Games.Commands;

public sealed class InspectNoticeBoardHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly JournalResolver _journalResolver;

    public InspectNoticeBoardHandler(
        IGameSessionRepository gameSessionRepository,
        JournalResolver journalResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _journalResolver = journalResolver;
    }

    public async Task<InvestigationActionResultDto> HandleAsync(
        InspectNoticeBoardCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await _gameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);

        var actionResult = session.InspectNoticeBoard();

        if (actionResult.SessionChanged)
        {
            await _gameSessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        }

        return new InvestigationActionResultDto(
            actionResult.Success,
            actionResult.Message,
            JournalMapper.ToDto(_journalResolver.Resolve(session)));
    }
}
