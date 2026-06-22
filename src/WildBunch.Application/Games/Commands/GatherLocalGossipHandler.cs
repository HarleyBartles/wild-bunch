using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Games.Commands;

public sealed class GatherLocalGossipHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameSessionUnitOfWork _gameSessionUnitOfWork;
    private readonly JournalResolver _journalResolver;

    public GatherLocalGossipHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        JournalResolver journalResolver)
    {
        _gameSessionRepository = gameSessionRepository;
        _gameSessionUnitOfWork = gameSessionUnitOfWork;
        _journalResolver = journalResolver;
    }

    public async Task<InvestigationActionResultDto> HandleAsync(
        GatherLocalGossipCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await _gameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);

        var actionResult = session.GatherLocalGossip();

        if (actionResult.SessionChanged)
        {
            await _gameSessionRepository.StoreAsync(session, cancellationToken: cancellationToken).ConfigureAwait(false);
            await _gameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return new InvestigationActionResultDto(
            actionResult.Success,
            actionResult.Message,
            JournalMapper.ToDto(_journalResolver.Resolve(session)));
    }
}
