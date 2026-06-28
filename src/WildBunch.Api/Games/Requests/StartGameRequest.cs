using WildBunch.Domain.Travel;

namespace WildBunch.Api.Games;

public sealed record StartGameRequest(
    string PlayerName,
    TravelDifficulty TravelDifficulty = TravelDifficulty.Normal,
    string? SeedCode = null,
    AdventureRandomnessPolicy Entropy = AdventureRandomnessPolicy.Standard,
    string? StartingTownId = null);
