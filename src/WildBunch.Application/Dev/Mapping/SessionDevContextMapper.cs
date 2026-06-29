using WildBunch.Application.Dev.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Mapping;

public static class SessionDevContextMapper
{
    public static SessionDevContextDto ToDto(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new SessionDevContextDto(
            SessionId: session.Id.Value,
            Status: session.Status.ToString(),
            GameDifficulty: session.GameDifficulty.ToString(),
            GameEntropy: session.GameEntropy.ToString(),
            SaltPosture: new SaltPostureDevDto(
                session.SaltSource.Mode.ToString(),
                session.SaltSource.Salt),
            Clock: new ClockDevDto(
                session.Clock.Day,
                session.Clock.Turn,
                session.Clock.TimeOfDay.ToString()),
            CurrentTownId: session.CurrentTown.TownId.Value,
            CurrentTownName: session.CurrentTown.TownName,
            CurrentActionContext: session.CurrentActionContext.ToString(),
            HasActiveJourney: session.Journey is not null,
            SeedCodeRetained: session.SeedCode is not null,
            SeedCodeText: session.SeedCode,
            TravelRules: new TravelRulesDevDto(
                session.TravelRules.CanteenCapacity,
                session.TravelRules.MountedRideDayProgress,
                session.TravelRules.FootRideDayProgress,
                session.TravelRules.EncounterFightAmmoHealthLoss,
                session.TravelRules.EncounterFightUnarmedHealthLoss,
                session.TravelRules.EncounterRunFootHealthLoss));
    }
}
