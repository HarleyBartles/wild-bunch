using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

public sealed class AcknowledgeJourneyArrivalHandler : GameSessionCommandHandler
{
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public AcknowledgeJourneyArrivalHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        HudProjector hudProjector,
        DiaryProjector diaryProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _hudProjector = hudProjector;
        _diaryProjector = diaryProjector;
    }

    public async Task<GameTurnResultDto> HandleAsync(AcknowledgeJourneyArrivalCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        var result = await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            var arrivalResult = session.AcknowledgeJourneyArrival();

            return Task.FromResult(GameTurnResultFactory.Create(
                arrivalResult.Success,
                arrivalResult.Message,
                session,
                journeyStatus: null));
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
