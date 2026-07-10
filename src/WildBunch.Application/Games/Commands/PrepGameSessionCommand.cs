using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Command to create a game session in the prepped phase (before world generation).
/// The session has seed, difficulty, and entropy but no world yet.
/// Used for the multi-phase setup flow where dev injections happen before world generation.
/// </summary>
public sealed record PrepGameSessionCommand(
    string SeedCode,
    GameDifficulty GameDifficulty,
    GameEntropy GameEntropy);
