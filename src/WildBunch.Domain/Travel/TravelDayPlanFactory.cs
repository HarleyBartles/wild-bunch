using WildBunch.Domain.Game;

namespace WildBunch.Domain.Travel;

/// <summary>
/// Creates a TravelDayPlanState from a dev override, bypassing the generator.
/// The forced plan contains a single encounter matching the override category.
/// See BUNCH-89.
/// </summary>
internal static class TravelDayPlanFactory
{
    public static TravelDayPlanState CreateForcedDayPlan(
        DevTravelOverride overrideValue,
        int dayNumber,
        TravelRulesProfile travelRules)
    {
        ArgumentNullException.ThrowIfNull(overrideValue);

        var encounter = CreateForcedEncounter(overrideValue, dayNumber, travelRules);
        return new TravelDayPlanState(
            dayNumber,
            new[] { encounter },
            CurrentEncounterIndex: 0,
            IsComplete: false);
    }

    private static TravelDayEncounterState CreateForcedEncounter(
        DevTravelOverride overrideValue,
        int slotIndex,
        TravelRulesProfile travelRules)
    {
        var message = overrideValue.EncounterMessage ?? BuildDefaultMessage(overrideValue.ForcedCategory);

        return overrideValue.ForcedCategory switch
        {
            TravelDayEncounterCategory.Foe when overrideValue.FoeProfile is { } foeProfile =>
                new TravelDayEncounterState(
                    slotIndex,
                    TravelDayEncounterCategory.Foe,
                    "Hard-eyed rider",
                    message,
                    TrailEvent: null,
                    PendingEncounter: JourneyEncounterState.CreateFoe(message, foeProfile),
                    Resolution: null),
            TravelDayEncounterCategory.Foe =>
                new TravelDayEncounterState(
                    slotIndex,
                    TravelDayEncounterCategory.Foe,
                    "Hard-eyed rider",
                    message,
                    TrailEvent: null,
                    PendingEncounter: JourneyEncounterState.CreateFoe(
                        message,
                        new JourneyFoeProfile(Speed: 3, FightStrength: 3, MinimumBribe: travelRules.EncounterBribeCash)),
                    Resolution: null),
            TravelDayEncounterCategory.Quiet =>
                new TravelDayEncounterState(
                    slotIndex,
                    TravelDayEncounterCategory.Quiet,
                    "Quiet trail",
                    message,
                    TrailEvent: null,
                    PendingEncounter: null,
                    Resolution: null),
            _ =>
                new TravelDayEncounterState(
                    slotIndex,
                    overrideValue.ForcedCategory,
                    BuildDefaultTitle(overrideValue.ForcedCategory),
                    message,
                    TrailEvent: null,
                    PendingEncounter: JourneyEncounterState.CreateChoiceEncounter(
                        overrideValue.ForcedCategory.ToString().ToLowerInvariant(),
                        message),
                    Resolution: null)
        };
    }

    private static string BuildDefaultMessage(TravelDayEncounterCategory category) => category switch
    {
        TravelDayEncounterCategory.Foe => "A hard-eyed rider cuts across my path.",
        TravelDayEncounterCategory.Npc => "A weathered stranger hails me from the trail.",
        TravelDayEncounterCategory.Lucky => "I spot something glinting by the trail.",
        TravelDayEncounterCategory.Unlucky => "The trail takes a bad turn.",
        TravelDayEncounterCategory.Environmental => "The weather turns rough on the trail.",
        TravelDayEncounterCategory.Resource => "I come across a cache of supplies.",
        TravelDayEncounterCategory.HorseTrouble => "My horse is acting up on the trail.",
        _ => "The trail is quiet."
    };

    private static string BuildDefaultTitle(TravelDayEncounterCategory category) => category switch
    {
        TravelDayEncounterCategory.Foe => "Hard-eyed rider",
        TravelDayEncounterCategory.Npc => "Weathered stranger",
        TravelDayEncounterCategory.Lucky => "Lucky find",
        TravelDayEncounterCategory.Unlucky => "Bad turn",
        TravelDayEncounterCategory.Environmental => "Rough weather",
        TravelDayEncounterCategory.Resource => "Supply cache",
        TravelDayEncounterCategory.HorseTrouble => "Horse trouble",
        _ => "Quiet trail"
    };
}
