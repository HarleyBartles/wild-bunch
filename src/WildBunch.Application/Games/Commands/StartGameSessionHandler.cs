using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Starts a prepped game session by generating the world with dev layout salts.
/// Loads the prepped session, reads DevLayoutSalts, calls INewGameFactory with dev salts,
/// and creates a new session in the setup-complete phase.
/// </summary>
public sealed class StartGameSessionHandler : GameSessionCommandHandler
{
    private readonly INewGameFactory _newGameFactory;
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public StartGameSessionHandler(
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

    // Setup-flow handler: transitions session from Prepped to SetupComplete.
    protected override bool RequiresGameStarted => false;

    public async Task<GameSessionDto> HandleAsync(
        StartGameSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        // Load the prepped session first to validate status and get dev salts
        var preppedSession = await GameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (preppedSession is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        if (preppedSession.Status != GameStatus.Prepped)
        {
            throw new InvalidOperationException("Session must be in Prepped status to start");
        }

        var devLayoutSalts = preppedSession.DevLayoutSalts;

        // Resolve the world and case file with dev layout salts
        // Note: Player name is intentionally reset to "Player" during start.
        // The prepped session uses "Prepped" as a placeholder. If player name
        // needs to be preserved, it should be added to PrepGameSessionCommand.
        var (world, caseFile, seedCodeText, saltSource) = _newGameFactory.ResolveWorld(
            "Player",
            preppedSession.GameDifficulty,
            preppedSession.SeedCode,
            preppedSession.GameEntropy,
            devLayoutSalts);

        // Transition the prepped session to setup-complete phase (preserves session ID and event history)
        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            session.StartFromPrepped(world, caseFile, seedCodeText, saltSource);
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);

        // Reload the session to get the updated state
        var updatedSession = await GameSessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (updatedSession is null)
        {
            throw new InvalidOperationException("Failed to reload session after start");
        }

        var dto = GameSessionMapper.ToDto(updatedSession);
        var events = await GameSessionRepository.GetEventStreamAsync(
            updatedSession.Id, 0, cancellationToken).ConfigureAwait(false);
        var hud = _hudProjector.Project(events) with { SessionId = dto.Id };
        var diary = _diaryProjector.Project(events) with { SessionId = dto.Id };

        return dto with { HudProjection = hud, DiaryProjection = diary };
    }
}
