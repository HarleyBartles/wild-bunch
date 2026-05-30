using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Deterministic setup plan derived from the player-facing seed.
/// Setup facts are selected here once, while travel generation keeps reading live session state later.
/// </summary>
internal sealed record GameSetupGenerationPlan(
    GameSetupSeed Seed,
    string SeedCode,
    GameSetupDeterministicSource Source,
    TravelRulesProfile TravelRulesProfile,
    SeedWorldVariant WorldVariant)
{
    public bool IsCanonical => Seed.IsCanonical;

    public static GameSetupGenerationPlan Create(GameSetupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        var seedCode = GameSetupSeedCodec.GetStableKey(seed);
        var source = new GameSetupDeterministicSource(seedCode);
        var travelRulesProfile = TravelRulesProfile.For(seed.Difficulty);
        var worldVariant = seed.IsCanonical
            ? SeedWorldVariant.Canonical
            : source.Roll(GameSetupDeterministicLabels.WorldVariant) % 2 == 0
                ? SeedWorldVariant.Frontier
                : SeedWorldVariant.Rail;

        return new GameSetupGenerationPlan(seed, seedCode, source, travelRulesProfile, worldVariant);
    }
}
