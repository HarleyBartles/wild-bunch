using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Command to record that the player has completed initial setup (name, difficulty, entropy, seed selection).
/// This emits a PlayerSetupCompleted event and advances the start flow phase.
/// </summary>
public sealed record CompletePlayerSetupCommand
{
    public required string PlayerName { get; init; }
    public required GameDifficulty GameDifficulty { get; init; }
    public required GameEntropy GameEntropy { get; init; }
    public required string SeedCode { get; init; }
}