using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Execution;

/// <summary>
/// Base class for game session command handlers that formalizes the
/// load → command → store → commit → project → safe return orchestration.
/// See ADR-0028.
///
/// Subclasses implement <see cref="ExecuteAsync"/> which loads the session,
/// applies the command, and returns the result. The base class wraps the
/// execution with:
/// - Correlation ID generation for event tracing
/// - Setup-phase invariant enforcement for gameplay commands
/// - Optimistic concurrency retry on ConcurrencyException
/// - Event commit marking after successful store + commit
/// </summary>
public abstract class GameSessionCommandHandler
{
    private const int MaxConcurrencyRetries = 3;

    protected readonly IGameSessionRepository GameSessionRepository;
    protected readonly IGameSessionUnitOfWork GameSessionUnitOfWork;

    protected GameSessionCommandHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
    {
        GameSessionRepository = gameSessionRepository;
        GameSessionUnitOfWork = gameSessionUnitOfWork;
    }

    /// <summary>
    /// When true (the default), the base pipeline throws <see cref="SetupPhaseException"/>
    /// if a loaded session has not yet reached <see cref="StartFlowPhase.GameStarted"/>.
    /// Gameplay command handlers inherit this guard for free. Setup-flow and lifecycle
    /// handlers (CompletePlayerSetup, ViewPrologue, CompleteGameStart, ArchivePlaythrough)
    /// override this to false. See the architecture guardrails for the inversion pattern.
    /// </summary>
    protected virtual bool RequiresGameStarted => true;

    /// <summary>
    /// Executes the command with optimistic concurrency retry.
    /// The subclass implementation loads the session, applies the command,
    /// and returns the result. If the command produced uncommitted events,
    /// the base class stores and commits them.
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(
        GameSessionId sessionId,
        Func<GameSession, CancellationToken, Task<T>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executeAsync);

        var correlationId = Guid.NewGuid();
        ConcurrencyException? lastConcurrencyException = null;

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            var session = await GameSessionRepository.LoadRequiredAsync(sessionId, cancellationToken).ConfigureAwait(false);

            if (RequiresGameStarted && session.IsSetupPhase)
            {
                throw new SetupPhaseException(session.Id);
            }

            var result = await executeAsync(session, cancellationToken).ConfigureAwait(false);

            if (session.UncommittedEvents.Count > 0)
            {
                try
                {
                    await GameSessionRepository.StoreAsync(session, correlationId, cancellationToken).ConfigureAwait(false);
                    await GameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
                    session.MarkEventsCommitted();
                }
                catch (ConcurrencyException ex)
                {
                    lastConcurrencyException = ex;
                    continue;
                }
            }

            return result;
        }

        throw lastConcurrencyException ?? new InvalidOperationException("Command failed after maximum retries.");
    }
}

