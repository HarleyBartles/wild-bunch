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
        GameDifficulty gameDifficulty,
        GameEntropy entropy,
        Player player,
        World world,
        CaseFile caseFile,
        GameClock clock,
        PursuitState pursuitState,
        SaltSource saltSource,
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
            gameDifficulty,
            saltSource,
            entropy,
            townVisitState,
            completedJourneyHistory,
            wantedSuspectPresenceEntries);

        GameSessionRehydrator.ReplaceTravelDiaryDays(session, travelDiaryDays);
        GameSessionRehydrator.ReplaceLogEntries(session, logEntries);
        return session;
    }
}
