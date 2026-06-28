using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Deterministic setup plan derived from the player-facing seed code and resolved descriptor.
/// Setup facts are selected here once, while travel generation keeps reading live session state later.
/// </summary>
internal sealed record StartingWorldGenerationPlan(
    StartingWorldDescriptor Descriptor,
    string SeedCode,
    GameSetupDeterministicSource Source,
    TravelRulesProfile TravelRulesProfile,
    SeedWorldVariant WorldVariant)
{
    public bool IsCanonical => Descriptor == StartingWorldDescriptorResolver.CreateCanonicalDescriptor(Descriptor.GameDifficulty, Descriptor.GameEntropy);

    public GameDifficulty GameDifficulty => Descriptor.GameDifficulty;

    public static StartingWorldGenerationPlan Create(StartingWorldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var validation = StartingWorldDescriptorResolver.Validate(descriptor);
        if (!validation.Success)
        {
            throw new ArgumentException(validation.ErrorMessage ?? "Starting world descriptor is invalid.", nameof(descriptor));
        }

        var seedCode = StartingWorldDescriptorResolver.FormatSeedCode(descriptor.SeedCode);
        var source = new GameSetupDeterministicSource(seedCode);
        var travelRulesProfile = TravelRulesProfile.For(descriptor.GameDifficulty);
        var worldVariant = descriptor.World.Variant;

        return new StartingWorldGenerationPlan(descriptor, seedCode, source, travelRulesProfile, worldVariant);
    }
}
