using WildBunch.Domain.Cases;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Prologue;

/// <summary>
/// Resolves the player-visible true-culprit descriptor for the prologue from a seed code.
/// Bridges the internal <see cref="StartingWorldDescriptorResolver"/> and
/// <see cref="GameSetupPackageBuilder"/> (both internal to WildBunch.GameContent.NewGame)
/// to the Application layer, which cannot reference them directly.
/// Uses the same <see cref="SaloonPersonOfInterestDescriptor.Describe"/> path used
/// elsewhere for clues/suspects so there is one canonical formatter.
/// Does NOT expose TrueCulpritId, isTrueCulprit, or internal suspect ids.
/// </summary>
public static class PrologueDescriptorResolver
{
    /// <summary>
    /// Resolves the player-visible true-culprit descriptor for the prologue from a seed code.
    /// </summary>
    public static string ResolveTrueCulpritDescriptor(
        TravelDifficulty travelDifficulty = TravelDifficulty.Normal,
        string? setupSeedCode = null,
        AdventureRandomnessPolicy entropy = AdventureRandomnessPolicy.Standard)
    {
        var descriptor = StartingWorldDescriptorResolver.Resolve(setupSeedCode, travelDifficulty, entropy);
        var setupPackage = new GameSetupPackageBuilder().Build(descriptor);
        var trueCulprit = setupPackage.CaseFile.Suspects.First(s => s.Id == setupPackage.CaseFile.TrueCulpritId);
        return SaloonPersonOfInterestDescriptor.Describe(trueCulprit, setupPackage.CaseFile);
    }
}
