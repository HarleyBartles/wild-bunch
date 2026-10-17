using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Creates a new game session in the setup-complete phase.
/// The world and case file are resolved from the seed code at this point.
/// </summary>
public sealed class CompletePlayerSetupHandler : GameSessionCommandHandler
{
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

        if (command.OperationId == Guid.Empty)
        {
            throw new StartupOperationConflictException("A non-empty startup operation ID is required.");
        }

        var fingerprint = StartupOperationFingerprint.ForSetup(command);
        var existingOperation = await GameSessionRepository.GetStartupOperationAsync(command.OperationId, cancellationToken)
            .ConfigureAwait(false);
        if (existingOperation is not null)
        {
            return await ResolveExistingOperationAsync(existingOperation, StartupOperationKind.Setup, fingerprint, cancellationToken)
                .ConfigureAwait(false);
        }

        var activeSessions = await GameSessionRepository.GetByStatusAsync(GameStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        if (activeSessions.Count > 0)
        {
            // Another delivery of this same operation may have committed after
            // the initial receipt lookup but before the active-session query.
            // Recheck its atomic receipt before treating the active session as
            // a separate playthrough conflict.
            var racedOperation = await GameSessionRepository.GetStartupOperationAsync(command.OperationId, cancellationToken)
                .ConfigureAwait(false);
            if (racedOperation is not null)
            {
                return await ResolveExistingOperationAsync(racedOperation, StartupOperationKind.Setup, fingerprint, cancellationToken)
                    .ConfigureAwait(false);
            }

            throw new StartupOperationConflictException("An active playthrough must be archived with Start Over before a new game can be started.");
        }

        var (world, caseFile, seedCodeText, saltSource) = _newGameFactory.ResolveWorld(
            command.PlayerName, command.GameDifficulty, command.SeedCode, command.GameEntropy);

        var newSession = GameSession.StartSetup(
            command.PlayerName,
            world,
            caseFile,
            command.GameDifficulty,
            command.GameEntropy,
            seedCodeText,
            saltSource);

        await GameSessionRepository.StoreAsync(newSession, Guid.NewGuid(), cancellationToken).ConfigureAwait(false);
        GameSessionRepository.StoreStartupOperation(new StartupOperation(
            command.OperationId,
            StartupOperationKind.Setup,
            fingerprint,
            newSession.Id.Value));

        try
        {
            await GameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            newSession.MarkEventsCommitted();
            return GameSessionMapper.ToDto(newSession);
        }
        catch (ConcurrencyException)
        {
            var racedOperation = await GameSessionRepository.GetStartupOperationAsync(command.OperationId, cancellationToken)
                .ConfigureAwait(false);
            if (racedOperation is null)
            {
                var activeSession = await GameSessionRepository.GetByStatusAsync(GameStatus.Active, cancellationToken)
                    .ConfigureAwait(false);
                if (activeSession.Count > 0)
                {
                    throw new StartupOperationConflictException(
                        "An active playthrough must be archived with Start Over before a new game can be started.");
                }

                throw;
            }
            return await ResolveExistingOperationAsync(racedOperation, StartupOperationKind.Setup, fingerprint, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<GameSessionDto> ResolveExistingOperationAsync(
        StartupOperation operation,
        StartupOperationKind expectedKind,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (operation.Kind != expectedKind || !StringComparer.Ordinal.Equals(operation.RequestFingerprint, fingerprint))
        {
            throw new StartupOperationConflictException("The startup operation ID was already used for a different request.");
        }

        var session = await GameSessionRepository.GetByIdAsync(new GameSessionId(operation.SessionId), cancellationToken)
            .ConfigureAwait(false);
        return session is null
            ? throw new InvalidOperationException("The startup operation references a missing playthrough.")
            : GameSessionMapper.ToDto(session);
    }
}
