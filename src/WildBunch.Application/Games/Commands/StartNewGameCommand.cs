using WildBunch.Domain.Travel;

namespace WildBunch.Application.Games.Commands;

public sealed record StartNewGameCommand(
    string PlayerName,
    GameDifficulty GameDifficulty = GameDifficulty.Standard,
    string? SetupSeedCode = null,
    GameEntropy GameEntropy = GameEntropy.Classic,
    string? StartingTownId = null);
