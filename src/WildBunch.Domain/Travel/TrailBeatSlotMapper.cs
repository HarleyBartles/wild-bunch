namespace WildBunch.Domain.Travel;

/// <summary>
/// Maps <see cref="TravelDayEncounterCategory"/> to <see cref="TrailBeatSlotType"/>.
/// Interrupting (requiresChoice) overrides any category-based mapping.
/// </summary>
public static class TrailBeatSlotMapper
{
    public static TrailBeatSlotType ToSlotType(TravelDayEncounterCategory category, bool requiresChoice)
    {
        if (requiresChoice)
        {
            return TrailBeatSlotType.Interrupting;
        }

        return category switch
        {
            TravelDayEncounterCategory.Quiet => TrailBeatSlotType.Quiet,
            TravelDayEncounterCategory.Lucky => TrailBeatSlotType.Minor,
            TravelDayEncounterCategory.Unlucky => TrailBeatSlotType.Minor,
            TravelDayEncounterCategory.Resource => TrailBeatSlotType.Minor,
            TravelDayEncounterCategory.HorseTrouble => TrailBeatSlotType.Minor,
            TravelDayEncounterCategory.Foe => TrailBeatSlotType.Eventful,
            TravelDayEncounterCategory.Npc => TrailBeatSlotType.Eventful,
            TravelDayEncounterCategory.Environmental => TrailBeatSlotType.Eventful,
            _ => TrailBeatSlotType.Quiet
        };
    }
}
