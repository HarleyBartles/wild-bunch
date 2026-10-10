using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Creates a new game session in the setup-complete phase.
/// Archives any pre-existing non-archived sessions (one-active-playthrough invariant).
/// The world and case file are resolved from the seed code at this point.
/// </summary>
public sealed class CompletePlayerSetupHandler : GameSessionCommandHandler
{
    private const string SupersededByNewPlaythrough = "superseded-by-new-playthrough";

    private readonly INewGameFactory _newGameFactory;

    public CompletePlayerSetupHandler(
        INewGameFactory newGameFactory,
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _newGameFactory = newGameFactory;
    }

    // Setup-flow handler: creates the session, does not require GameStarted.
    protected override bool RequiresGameStarted => false;

    public async Task<GameSessionDto> HandleAsync(CompletePlayerSetupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var correlationId = Guid.NewGuid();

        // 1. Archive all pre-existing Active sessions (one-active-playthrough invariant).
        var activeSessions = await GameSessionRepository.GetByStatusAsync(
            GameStatus.Active, cancellationToken).ConfigureAwait(false);
        foreach (var activeSession in activeSessions)
        {
            activeSession.ArchivePlaythrough(SupersededByNewPlaythrough);
            await GameSessionRepository.StoreAsync(
                activeSession, correlationId, cancellationToken).ConfigureAwait(false);
        }

        // 2. Resolve the world and case file from the seed code (without starting the game).
        var (world, caseFile, seedCodeText, saltSource) = _newGameFactory.ResolveWorld(
            command.PlayerName, command.GameDifficulty, command.SeedCode, command.GameEntropy);

        // 3. Create the session in setup-complete phase.
        var newSession = GameSession.StartSetup(
            command.PlayerName,
            world,
            caseFile,
            command.GameDifficulty,
            command.GameEntropy,
            seedCodeText,
            saltSource);

        await GameSessionRepository.StoreAsync(
            newSession, correlationId, cancellationToken).ConfigureAwait(false);

        // 4. Commit everything in one transaction.
        await GameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        // 5. Mark events committed on all touched sessions.
        foreach (var archived in activeSessions)
        {
            archived.MarkEventsCommitted();
        }
        newSession.MarkEventsCommitted();

        // 6. Return the setup DTO without gameplay-only projections.
        return GameSessionMapper.ToDto(newSession);
    }
}
