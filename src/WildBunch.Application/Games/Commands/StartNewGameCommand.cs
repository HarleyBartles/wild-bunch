using WildBunch.Domain.Travel;

namespace WildBunch.Application.Games.Commands;

public sealed record StartNewGameCommand(
    string PlayerName,
    TravelDifficulty TravelDifficulty = TravelDifficulty.Normal,
    string? SetupSeedCode = null,
    AdventureRandomnessPolicy Entropy = AdventureRandomnessPolicy.Standard);
