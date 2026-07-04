using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Commands;

public sealed class TravelToTownHandler : GameSessionCommandHandler
{
    private readonly TravelResolver _travelResolver;
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public TravelToTownHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        TravelResolver travelResolver,
        HudProjector hudProjector,
        DiaryProjector diaryProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _travelResolver = travelResolver;
        _hudProjector = hudProjector;
        _diaryProjector = diaryProjector;
    }

    public async Task<GameTurnResultDto> HandleAsync(TravelToTownCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        // Preview generation is INSIDE the retry lambda because it depends on
        // mutable session state (inventory, current town). On retry, the session
        // is reloaded and the preview must be regenerated with fresh state.
        // See ADR-0028 and BUNCH-83 Phase 3 Task 4.
        var result = await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var destinationTownId = new TownId(command.DestinationTownId);
            var previewResult = _travelResolver.PreviewJourney(
                session.World,
                session.Player.CurrentTownId!.Value,
                destinationTownId,
                session.Player.Inventory,
                session.TravelRules);

            if (!previewResult.Success || previewResult.Preview is null)
            {
                return GameTurnResultFactory.Create(
                    previewResult.Success,
                    previewResult.Message,
                    session);
            }

            var startResult = session.StartJourney(previewResult.Preview);

            return GameTurnResultFactory.Create(
                startResult.Success,
                startResult.Message,
                session,
                startResult.Status,
                startResult.Journey);
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
