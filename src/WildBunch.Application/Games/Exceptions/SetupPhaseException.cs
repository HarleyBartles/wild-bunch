using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Exceptions;

/// <summary>
/// Thrown by the Application command-handler pipeline when a handler requires
/// a started game but the loaded session reports <see cref="GameSession.IsSetupPhase"/>.
/// The aggregate exposes the phase fact; the Application pipeline enforces
/// this use-case guard unless the handler opts out. See ADR-0028 and the
/// architecture guardrails for the setup-phase inversion pattern.
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
