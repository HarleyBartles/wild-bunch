using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Exceptions;

/// <summary>
/// Thrown when a gameplay command is invoked on a session that has not yet
/// completed the setup flow (StartFlowPhase &lt; GameStarted). The aggregate
/// root owns this invariant via GameSession.ThrowIfSetupPhase(); the command
/// handler base class centralizes the call so individual gameplay handlers
/// do not need to repeat the guard. See ADR-0028 and the architecture
/// guardrails for the setup-phase inversion pattern.
/// </summary>
public sealed class SetupPhaseException : InvalidOperationException
{
    public SetupPhaseException(GameSessionId gameSessionId)
        : base("The game hasn't started yet. Complete setup first.")
    {
        GameSessionId = gameSessionId;
    }

    public GameSessionId GameSessionId { get; }
}
