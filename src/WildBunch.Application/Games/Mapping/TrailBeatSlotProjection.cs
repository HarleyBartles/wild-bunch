using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Games.Mapping;

/// <summary>
/// Derives beat slot DTOs from existing TravelDiaryDayState fields.
/// This is a mapper-only projection — no fields are added to TravelDiaryDayState.
/// Follows the same pattern as JourneyBeat/ResourceBeat (null in domain, filled in mapper).
///
/// Derivation rules:
/// - PendingEncounter != null && EncounterResolution == null -> Interrupting (RequiresChoice equivalent)
/// - PendingEncounter != null && EncounterResolution != null -> Eventful (resolved encounter)
/// - TrailEvent != null -> Minor (trail event occurred)
/// - Otherwise -> Quiet
/// </summary>
public static class TrailBeatSlotProjection
{
    public static IReadOnlyList<TrailBeatSlotDto> FromDayState(TravelDiaryDayState day)
    {
        var slots = new List<TrailBeatSlotDto>();
        int index = 0;

        // Interrupting: day was paused by a choice-requiring encounter (RequiresChoice equivalent)
        if (day.PendingEncounter is not null && day.EncounterResolution is null)
        {
            slots.Add(new TrailBeatSlotDto(
                index++,
                TrailBeatSlotType.Interrupting,
                "Interrupting",
                day.PendingEncounter.Kind,
                day.PendingEncounter.Message));
        }
        // Eventful: encounter was resolved
        else if (day.PendingEncounter is not null && day.EncounterResolution is not null)
        {
            slots.Add(new TrailBeatSlotDto(
                index++,
                TrailBeatSlotType.Eventful,
                "Eventful",
                day.PendingEncounter.Kind,
                day.PendingEncounter.Message));
        }

        // Minor: a trail event occurred (weather, terrain, resource)
        if (day.TrailEvent is not null)
        {
            slots.Add(new TrailBeatSlotDto(
                index++,
                TrailBeatSlotType.Minor,
                "Minor",
                day.TrailEvent.Title,
                day.TrailEvent.Message));
        }

        // Quiet: if no other slots, add a single quiet slot
        if (slots.Count == 0)
        {
            slots.Add(new TrailBeatSlotDto(
                index,
                TrailBeatSlotType.Quiet,
                "Quiet",
                null,
                null));
        }

        return slots;
    }
}
