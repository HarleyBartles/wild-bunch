using WildBunch.Domain.Travel;

namespace WildBunch.Api.Games;

public sealed record SetupGameRequest(
    string PlayerName,
    GameDifficulty GameDifficulty = GameDifficulty.Standard,
    string? SeedCode = null,
    GameEntropy GameEntropy = GameEntropy.Classic);
