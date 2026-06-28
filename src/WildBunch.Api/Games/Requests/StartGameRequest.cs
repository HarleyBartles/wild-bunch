using WildBunch.Domain.Travel;

namespace WildBunch.Api.Games;

public sealed record StartGameRequest(
    string PlayerName,
    GameDifficulty GameDifficulty = GameDifficulty.Normal,
    string? SeedCode = null,
    GameEntropy Entropy = GameEntropy.Standard,
    string? StartingTownId = null);
