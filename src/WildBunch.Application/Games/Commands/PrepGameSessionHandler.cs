using WildBunch.Application.Abstractions;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Creates a new game session in the prepped phase (before world generation).
/// The session has seed, difficulty, and entropy but no world yet.
/// Used for the multi-phase setup flow where dev injections happen before world generation.
/// </summary>
public sealed class PrepGameSessionHandler
{
    private readonly IGameSessionRepository _repository;
    private readonly IGameSessionUnitOfWork _unitOfWork;

    public PrepGameSessionHandler(
        IGameSessionRepository repository,
        IGameSessionUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PrepGameSessionResult> HandleAsync(
        PrepGameSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var session = GameSession.StartPrepped(
            command.SeedCode,
            command.GameDifficulty,
            command.GameEntropy);

        await _repository.StoreAsync(session, Guid.NewGuid(), cancellationToken).ConfigureAwait(false);
        await _unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        session.MarkEventsCommitted();

        return new PrepGameSessionResult(session.Id.Value.ToString());
    }
}

/// <summary>
/// Result of the prep game session command.
/// </summary>
public sealed record PrepGameSessionResult(string GameSessionId);
