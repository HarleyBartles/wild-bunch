using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.Abstractions;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Completes the game start by selecting a starting town and emitting GameStarted.
/// Loads the existing setup-phase session, resolves the difficulty envelope for
/// starting wallet/inventory, and appends GameStarted to the event stream.
/// </summary>
public sealed class CompleteGameStartHandler : GameSessionCommandHandler
{
    private readonly INewGameFactory _newGameFactory;
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public CompleteGameStartHandler(
        INewGameFactory newGameFactory,
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        HudProjector hudProjector,
        DiaryProjector diaryProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _newGameFactory = newGameFactory;
        _hudProjector = hudProjector;
        _diaryProjector = diaryProjector;
    }

    // Setup-flow handler: transitions session from StartingTownSelected to GameStarted.
    protected override bool RequiresGameStarted => false;

    public async Task<GameSessionDto> HandleAsync(CompleteGameStartCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dto = await ExecuteWithRetryAsync(command.SessionId, async (session, ct) =>
        {
            if (session.StartFlowPhase == StartFlowPhase.GameStarted)
            {
                // Already started — return current state.
                return GameSessionMapper.ToDto(session);
            }

            if (session.StartFlowPhase == StartFlowPhase.NotStarted)
            {
                throw new InvalidOperationException("Cannot complete game start before setup is complete.");
            }

            // Resolve the difficulty envelope for starting wallet/inventory.
            var (wallet, inventory) = _newGameFactory.ResolveStartingResources(session.GameDifficulty);
            var startingTownId = new TownId(command.StartingTownId);

            session.SelectStartingTown(startingTownId);

            session.CompleteGameStart(
                wallet,
                inventory);

            return GameSessionMapper.ToDto(session);
        }, cancellationToken).ConfigureAwait(false);

        // Project from the event stream AFTER the base pipeline has stored and
        // committed the new events (StartingTownSelected + GameStarted).
        // Fetching inside the lambda would project from a stream that does not
        // yet include GameStarted, so the HUD would be missing player name,
        // wallet, inventory, current town, and health.
        // See TravelToTownHandler for the same pattern.
        var events = await GameSessionRepository.GetEventStreamAsync(
            command.SessionId, 0, cancellationToken).ConfigureAwait(false);
        var hud = _hudProjector.Project(events) with { SessionId = dto.Id };
        var diary = _diaryProjector.Project(events) with { SessionId = dto.Id };

        return dto with { HudProjection = hud, DiaryProjection = diary };
    }
}