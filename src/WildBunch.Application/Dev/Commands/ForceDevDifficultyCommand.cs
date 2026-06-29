using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Commands;

public sealed record ForceDevDifficultyCommand(
    Guid GameSessionId,
    GameDifficulty Difficulty);
