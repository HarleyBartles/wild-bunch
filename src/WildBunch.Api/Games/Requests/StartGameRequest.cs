using WildBunch.Domain.Travel;

namespace WildBunch.Api.Games;

public sealed record StartGameRequest(
    string PlayerName,
    TravelDifficulty TravelDifficulty = TravelDifficulty.Normal,
    string? SeedCode = null);
