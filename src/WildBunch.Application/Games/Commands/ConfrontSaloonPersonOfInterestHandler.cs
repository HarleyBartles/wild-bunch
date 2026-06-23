using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

public sealed class ConfrontSaloonPersonOfInterestHandler : GameSessionCommandHandler
{
    public ConfrontSaloonPersonOfInterestHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task<SaloonPersonOfInterestConfrontationResultDto> HandleAsync(
        ConfrontSaloonPersonOfInterestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            // Trust boundary: the UI supplies only public wanted-poster handles here, and this slice preserves the declared handle as-is.
            var result = session.ConfrontSaloonPersonOfInterest(command.DeclaredWantedIdentityHandle);

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
        }, cancellationToken).ConfigureAwait(false);
    }
}
