namespace WildBunch.Application.Dev.Models;

/// <summary>
/// Request DTO for the force-difficulty dev endpoint.
/// Difficulty is a string matching a <see cref="WildBunch.Domain.Travel.GameDifficulty"/> enum name
/// (Easy, Standard, Challenging, Brutal). Case-insensitive.
/// See BUNCH-94.
/// </summary>
public sealed record ForceDevDifficultyRequestDto(string? Difficulty);
