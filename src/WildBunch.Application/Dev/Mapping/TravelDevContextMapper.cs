using WildBunch.Application.Dev.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Mapping;

public static class TravelDevContextMapper
{
    public static TravelDevContextDto ToDto(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var journey = session.Journey;
        var pendingEncounter = journey?.PendingEncounter;
        var foeProfile = pendingEncounter?.FoeProfile;
        var devOverride = session.PendingDevTravelOverride;

        return new TravelDevContextDto(
            session.Id.Value,
            HasActiveJourney: journey is not null,
            JourneyStatus: journey?.Status.ToString(),
            DaysTravelled: journey?.DaysTravelled,
            RemainingDays: journey?.RemainingDays,
            PendingEncounterKind: pendingEncounter?.Kind,
            PendingEncounterMessage: pendingEncounter?.Message,
            PendingFoeProfile: foeProfile is null ? null : new FoeProfileDevDto(
                foeProfile.Speed,
                foeProfile.FightStrength,
                foeProfile.MinimumBribe,
                foeProfile.DescribeSpeedBand(),
                foeProfile.DescribeFightBand(),
                foeProfile.DescribeBribeBand()),
            PendingDevOverride: devOverride is null ? null : new DevOverrideDto(
                devOverride.ForcedCategory.ToString(),
                devOverride.FoeProfile is null ? null : new FoeProfileDevDto(
                    devOverride.FoeProfile.Speed,
                    devOverride.FoeProfile.FightStrength,
                    devOverride.FoeProfile.MinimumBribe,
                    devOverride.FoeProfile.DescribeSpeedBand(),
                    devOverride.FoeProfile.DescribeFightBand(),
                    devOverride.FoeProfile.DescribeBribeBand()),
                devOverride.EncounterMessage));
    }
}
