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
        Player player,
        World world,
        CaseFile caseFile,
        GameClock clock,
        PursuitState pursuitState,
        TravelRandomnessState travelRandomness,
        TravelJourneySnapshot? journey,
        IReadOnlyList<TravelJourneySnapshot> completedJourneyHistory,
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
            completedJourneyHistory);

        GameSessionRehydrator.ReplaceTravelDiaryDays(session, travelDiaryDays);
        GameSessionRehydrator.ReplaceLogEntries(session, logEntries);
        return session;
    }
}
