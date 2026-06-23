using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

public sealed class AdvanceTravelDayHandler : GameSessionCommandHandler
{
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public AdvanceTravelDayHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        HudProjector hudProjector,
        DiaryProjector diaryProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _hudProjector = hudProjector;
        _diaryProjector = diaryProjector;
    }

    public async Task<GameTurnResultDto> HandleAsync(AdvanceTravelDayCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        var result = await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            var advanceResult = session.AdvanceJourneyDay();

            return Task.FromResult(GameTurnResultFactory.Create(
                advanceResult.Success,
                advanceResult.Message,
                session,
                advanceResult.Status,
                advanceResult.Journey,
                advanceResult.TrailEvent));
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
