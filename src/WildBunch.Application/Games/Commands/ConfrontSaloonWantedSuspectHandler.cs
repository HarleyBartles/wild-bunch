using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;

namespace WildBunch.Application.Games.Commands;

public sealed class ConfrontSaloonWantedSuspectHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameSessionUnitOfWork _gameSessionUnitOfWork;

    public ConfrontSaloonWantedSuspectHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
    {
        _gameSessionRepository = gameSessionRepository;
        _gameSessionUnitOfWork = gameSessionUnitOfWork;
    }

    public async Task<WantedSuspectConfrontationResultDto> HandleAsync(
        ConfrontSaloonWantedSuspectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await _gameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var result = session.ConfrontSaloonWantedSuspect();

        if (result.SessionChanged)
        {
            await _gameSessionRepository.StoreAsync(session, cancellationToken).ConfigureAwait(false);
            await _gameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return new WantedSuspectConfrontationResultDto(
            result.Success,
            result.Message,
            result.Outcome,
            GameSessionMapper.ToDto(session),
            result.DeclaredWantedIdentityHandle,
            result.TargetName,
            result.Disposition,
            result.IsAlive,
            result.IsSecured,
            result.SessionChanged);
    }
}
