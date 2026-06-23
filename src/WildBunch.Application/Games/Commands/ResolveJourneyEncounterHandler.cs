using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

public sealed class ResolveJourneyEncounterHandler : GameSessionCommandHandler
{
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public ResolveJourneyEncounterHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        HudProjector hudProjector,
        DiaryProjector diaryProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _hudProjector = hudProjector;
        _diaryProjector = diaryProjector;
    }

    public async Task<GameTurnResultDto> HandleAsync(ResolveJourneyEncounterCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        var result = await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            var encounterResult = session.ResolveJourneyEncounter(
                command.ChoiceId, command.BulletSpend, command.BribeAmount, command.ForcedRoll);

            return Task.FromResult(GameTurnResultFactory.Create(
                encounterResult.Success,
                encounterResult.Message,
                session,
                encounterResult.Status,
                encounterResult.Journey));
        }, cancellationToken).ConfigureAwait(false);

        var events = await GameSessionRepository.GetEventStreamAsync(sessionId, 0, cancellationToken)
            .ConfigureAwait(false);
        var hud = _hudProjector.Project(events) with { SessionId = command.GameSessionId };
        var diary = _diaryProjector.Project(events) with { SessionId = command.GameSessionId };

        return result with
        {
            CurrentSession = result.CurrentSession with
            {
                HudProjection = hud,
                DiaryProjection = diary
            }
        };
    }
}
