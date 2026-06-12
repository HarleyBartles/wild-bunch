using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    public GameSession RehydrateGameSession(
        Guid id,
        GameStatus status,
        TravelDifficulty travelDifficulty,
        AdventureRandomnessPolicy entropy,
        Player player,
        World world,
        CaseFile caseFile,
        GameClock clock,
        PursuitState pursuitState,
        TravelRandomnessState travelRandomness,
        TownVisitState? townVisitState,
        TravelJourneySnapshot? journey,
        IReadOnlyList<TravelJourneySnapshot> completedJourneyHistory,
        IReadOnlyList<WantedSuspectPresenceEntry> wantedSuspectPresenceEntries,
        IReadOnlyList<TravelDiaryDayState> travelDiaryDays,
        IReadOnlyList<GameLogEntry> logEntries)
    {
        var session = GameSessionRehydrator.Create(
            new GameSessionId(id),
            player,
            world,
            caseFile,
            pursuitState,
            clock,
            status,
            journey is null ? null : TravelJourney.FromSnapshot(journey),
            travelDifficulty,
            travelRandomness,
            entropy,
            townVisitState,
            completedJourneyHistory,
            wantedSuspectPresenceEntries);

        GameSessionRehydrator.ReplaceTravelDiaryDays(session, travelDiaryDays);
        GameSessionRehydrator.ReplaceLogEntries(session, logEntries);
        return session;
    }
}
