using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Games.Commands;

public sealed class CheckSheriffRecordsHandler : GameSessionCommandHandler
{
    private readonly JournalResolver _journalResolver;

    public CheckSheriffRecordsHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        JournalResolver journalResolver)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _journalResolver = journalResolver;
    }

    public async Task<InvestigationActionResultDto> HandleAsync(
        CheckSheriffRecordsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var actionResult = session.CheckSheriffRecords();
            return new InvestigationActionResultDto(
                actionResult.Success,
                actionResult.Message,
                JournalMapper.ToDto(_journalResolver.Resolve(session)));
        }, cancellationToken).ConfigureAwait(false);
    }
}
