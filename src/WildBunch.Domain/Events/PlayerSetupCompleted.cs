using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player completed initial setup (name, difficulty, entropy, seed selection).
/// This marks the transition from "no game" to "setup complete, ready to view prologue".
/// </summary>
public sealed record PlayerSetupCompleted : IDomainEvent
{
    public required string PlayerName { get; init; }
    public required GameDifficulty GameDifficulty { get; init; }
    public required GameEntropy GameEntropy { get; init; }
    public required string SeedCode { get; init; }
}