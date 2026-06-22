using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Exceptions;

/// <summary>
/// Thrown when optimistic concurrency check fails during event append.
/// The handler should reload the aggregate and retry the command.
/// See ADR-0028.
/// </summary>
public sealed class ConcurrencyException : InvalidOperationException
{
    public ConcurrencyException(GameSessionId gameSessionId, int expectedVersion, int actualVersion)
        : base($"Concurrency conflict for game session {gameSessionId.Value}: expected version {expectedVersion}, but found {actualVersion}.")
    {
        GameSessionId = gameSessionId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    /// <summary>
    /// Creates a ConcurrencyException from a database-level unique constraint violation
    /// during event append. The exact expected/actual versions may not be known at the
    /// DB layer; the handler will reload and retry regardless.
    /// </summary>
    public ConcurrencyException(string message)
        : base(message)
    {
        GameSessionId = default;
        ExpectedVersion = -1;
        ActualVersion = -1;
    }

    public GameSessionId GameSessionId { get; }
    public int ExpectedVersion { get; }
    public int ActualVersion { get; }
}
