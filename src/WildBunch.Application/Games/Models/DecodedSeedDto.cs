using WildBunch.Domain.Travel;

namespace WildBunch.Application.Games.Models;

/// <summary>
/// DTO representing the decoded difficulty and entropy from a seed.
/// Used by the frontend's seed editor to reflect the seed's encoded values.
/// </summary>
public sealed record DecodedSeedDto(GameDifficulty GameDifficulty, GameEntropy GameEntropy);
