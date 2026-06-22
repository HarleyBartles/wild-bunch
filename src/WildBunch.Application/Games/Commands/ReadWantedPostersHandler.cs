using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Games.Commands;

public sealed class ReadWantedPostersHandler : GameSessionCommandHandler
{
    private readonly JournalResolver _journalResolver;

    public ReadWantedPostersHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        JournalResolver journalResolver)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _journalResolver = journalResolver;
    }

    public async Task<WantedPostersResultDto> HandleAsync(
        ReadWantedPostersCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var actionResult = session.ReadWantedPosters();
            return new WantedPostersResultDto(
                actionResult.Success,
                actionResult.Message,
                JournalMapper.ToDto(_journalResolver.Resolve(session)),
                WantedPosterMapper.ToDto(session.CaseFile.KnownWarrants));
        }, cancellationToken).ConfigureAwait(false);
    }
}
