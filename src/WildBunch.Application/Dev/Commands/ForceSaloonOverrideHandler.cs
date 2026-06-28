using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Commands;

public sealed class ForceSaloonOverrideHandler : GameSessionCommandHandler
{
    public ForceSaloonOverrideHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ForceSaloonOverrideCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            var forcedKind = Enum.Parse<DevSaloonPoiKind>(command.ForcedKind, ignoreCase: true);
            SuspectId? forcedSuspectId = null;
            if (!string.IsNullOrWhiteSpace(command.ForcedSuspectId))
            {
                forcedSuspectId = new SuspectId(command.ForcedSuspectId);
            }

            var overrideValue = forcedKind switch
            {
                DevSaloonPoiKind.Suspect when forcedSuspectId is not null
                    => DevSaloonOverride.ForSuspect(forcedSuspectId.Value),
                DevSaloonPoiKind.Suspect
                    => DevSaloonOverride.ForAnySuspect(),
                DevSaloonPoiKind.Citizen when !string.IsNullOrWhiteSpace(command.ForcedCitizenRoleKey)
                    => DevSaloonOverride.ForCitizen(command.ForcedCitizenRoleKey),
                DevSaloonPoiKind.Citizen
                    => DevSaloonOverride.ForCitizen(),
                DevSaloonPoiKind.None
                    => DevSaloonOverride.ForNone(),
                _ => throw new ArgumentOutOfRangeException(nameof(command.ForcedKind))
            };

            session.ForceDevSaloonOverride(overrideValue);
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
