using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Game;

/// <summary>
/// Pending dev override for the next travel-day generation.
/// When present, AdvanceJourneyDay uses this instead of calling TravelDayPlanGenerator.
/// Consumed once by the next advance, then cleared from aggregate state.
/// This is dev-only session state, not player-facing. See BUNCH-89.
/// </summary>
public sealed record DevTravelOverride(
    TravelDayEncounterCategory ForcedCategory,
    JourneyFoeProfile? FoeProfile,
    string? EncounterMessage)
{
    public static DevTravelOverride ForFoe(JourneyFoeProfile foeProfile, string? encounterMessage = null)
        => new(TravelDayEncounterCategory.Foe, foeProfile, encounterMessage);

    public static DevTravelOverride ForCategory(TravelDayEncounterCategory category, string? encounterMessage = null)
        => new(category, null, encounterMessage);
}
