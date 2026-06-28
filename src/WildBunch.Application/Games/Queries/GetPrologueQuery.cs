using WildBunch.Domain.Travel;

namespace WildBunch.Application.Games.Queries;

/// <summary>
/// Query for the prologue read endpoint. <see cref="VariantId"/> is optional —
/// if null, the handler uses the first variant (deterministic default).
/// </summary>
public sealed record GetPrologueQuery(
    TravelDifficulty TravelDifficulty = TravelDifficulty.Normal,
    string? SeedCode = null,
    AdventureRandomnessPolicy Entropy = AdventureRandomnessPolicy.Standard,
    string? VariantId = null);
