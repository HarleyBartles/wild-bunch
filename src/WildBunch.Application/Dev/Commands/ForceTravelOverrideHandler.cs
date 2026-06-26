using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Commands;

public sealed class ForceTravelOverrideHandler : GameSessionCommandHandler
{
    public ForceTravelOverrideHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ForceTravelOverrideCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            var category = Enum.Parse<TravelDayEncounterCategory>(command.ForcedCategory, ignoreCase: true);
            JourneyFoeProfile? foeProfile = null;
            if (command.FoeSpeed is not null || command.FoeFightStrength is not null || command.FoeMinimumBribe is not null)
            {
                foeProfile = new JourneyFoeProfile(
                    Speed: command.FoeSpeed ?? 3,
                    FightStrength: command.FoeFightStrength ?? 3,
                    MinimumBribe: command.FoeMinimumBribe ?? 5m);
            }

            var overrideValue = foeProfile is not null
                ? DevTravelOverride.ForFoe(foeProfile, command.EncounterMessage)
                : DevTravelOverride.ForCategory(category, command.EncounterMessage);

            session.ForceDevTravelOverride(overrideValue);
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
