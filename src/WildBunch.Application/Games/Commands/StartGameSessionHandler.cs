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
        var (world, caseFile, seedCodeText, saltSource) = _newGameFactory.ResolveWorld(
            "Player",
            preppedSession.GameDifficulty,
            preppedSession.SeedCode,
            preppedSession.GameEntropy,
            devLayoutSalts);

        // Create the session in setup-complete phase
        var newSession = GameSession.StartSetup(
            "Player",
            world,
            caseFile,
            preppedSession.GameDifficulty,
            preppedSession.GameEntropy,
            seedCodeText,
            saltSource);

        await GameSessionRepository.StoreAsync(
            newSession,
            Guid.NewGuid(),
            cancellationToken).ConfigureAwait(false);
        await GameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        newSession.MarkEventsCommitted();

        var dto = GameSessionMapper.ToDto(newSession);
        var events = await GameSessionRepository.GetEventStreamAsync(
            newSession.Id, 0, cancellationToken).ConfigureAwait(false);
        var hud = _hudProjector.Project(events) with { SessionId = dto.Id };
        var diary = _diaryProjector.Project(events) with { SessionId = dto.Id };

        return dto with { HudProjection = hud, DiaryProjection = diary };
    }
}
