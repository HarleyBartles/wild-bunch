using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;

namespace WildBunch.Application.Games.Commands;

public sealed class ConfrontSaloonPersonOfInterestHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameSessionUnitOfWork _gameSessionUnitOfWork;

    public ConfrontSaloonPersonOfInterestHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
    {
        _gameSessionRepository = gameSessionRepository;
        _gameSessionUnitOfWork = gameSessionUnitOfWork;
    }

    public async Task<SaloonPersonOfInterestConfrontationResultDto> HandleAsync(
        ConfrontSaloonPersonOfInterestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(command.GameSessionId);
        var session = await _gameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);
        // Trust boundary: the UI supplies only public wanted-poster handles here, and this slice preserves the declared handle as-is.
        var result = session.ConfrontSaloonPersonOfInterest(command.DeclaredWantedIdentityHandle);

        if (result.SessionChanged)
        {
            await _gameSessionRepository.StoreAsync(session, cancellationToken).ConfigureAwait(false);
            await _gameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return new SaloonPersonOfInterestConfrontationResultDto(
            result.Success,
            result.Message,
            result.Outcome,
            GameSessionMapper.ToDto(session),
            result.DeclaredWantedIdentityHandle,
            result.TargetName,
            result.Disposition,
            result.IsAlive,
            result.IsSecured,
            result.IsCitizen,
            result.FineAmount,
            result.WalletBefore,
            result.WalletAfter,
            result.SessionChanged,
            result.PersonOfInterestKind);
    }
}
