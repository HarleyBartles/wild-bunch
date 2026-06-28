using WildBunch.Domain.Travel;

namespace WildBunch.Application.Games.Commands;

public sealed record StartNewGameCommand(
    string PlayerName,
    GameDifficulty GameDifficulty = GameDifficulty.Normal,
    string? SetupSeedCode = null,
    GameEntropy Entropy = GameEntropy.Standard,
    string? StartingTownId = null);
