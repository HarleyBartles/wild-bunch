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
            // The original game-start UUID seed code is not retained on the live
            // GameSession (it is consumed at StartNew to derive world/difficulty/entropy/salt).
            // Session dev says this honestly rather than fabricate a seed code.
            SeedCodeRetained: false,
            SeedCodeText: null);
    }
}
