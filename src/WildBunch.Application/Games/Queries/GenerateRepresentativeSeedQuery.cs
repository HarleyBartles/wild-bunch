using WildBunch.Domain.Travel;

namespace WildBunch.Application.Games.Queries;

/// <summary>
/// Query to generate a representative seed that encodes the selected difficulty and entropy.
/// Used by the frontend's seed randomizer to ensure the seed reflects the user's selections.
/// </summary>
public sealed record GenerateRepresentativeSeedQuery(
    GameDifficulty GameDifficulty = GameDifficulty.Standard,
    GameEntropy GameEntropy = GameEntropy.Classic);
