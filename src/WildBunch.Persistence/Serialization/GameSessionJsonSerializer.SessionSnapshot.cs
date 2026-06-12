using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    private sealed record GameSessionSnapshot(
        Guid Id,
        GameStatus Status,
        TravelDifficulty TravelDifficulty,
        AdventureRandomnessPolicy? Entropy,
        TravelRandomnessSnapshot? TravelRandomness,
        TownVisitStateSnapshot? CurrentTownVisit,
        PlayerSnapshot Player,
        WorldSnapshot World,
        CaseFileSnapshot CaseFile,
        PursuitStateSnapshot PursuitState,
        GameClockSnapshot Clock,
        JourneySnapshot? Journey,
        IReadOnlyList<JourneySnapshot>? CompletedJourneyHistory,
        IReadOnlyList<WantedSuspectPresenceSnapshot> WantedSuspectPresenceLedger,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
        IReadOnlyList<GameLogEntrySnapshot> LogEntries)
    {
        public static GameSessionSnapshot FromDomain(GameSession session)
            => new(
                session.Id.Value,
                session.Status,
                session.TravelDifficulty,
                session.Entropy,
                TravelRandomnessSnapshot.FromDomain(session.TravelRandomness),
                TownVisitStateSnapshot.FromDomain(session.CurrentTownVisit),
                PlayerSnapshot.FromDomain(session.Player),
                WorldSnapshot.FromDomain(session.World),
                CaseFileSnapshot.FromDomain(session.CaseFile),
                PursuitStateSnapshot.FromDomain(session.PursuitState),
                GameClockSnapshot.FromDomain(session.Clock),
                session.Journey is null ? null : JourneySnapshot.FromDomain(session.Journey.ToSnapshot(session.TravelRules)),
                session.CompletedJourneyHistory.Select(JourneySnapshot.FromDomain).ToArray(),
                session.WantedSuspectPresenceEntries.Select(WantedSuspectPresenceSnapshot.FromDomain).ToArray(),
                session.TravelDiaryDays.ToArray(),
                session.LogEntries.Select(GameLogEntrySnapshot.FromDomain).ToArray());

        public GameSession ToDomain()
        {
            var world = WorldSnapshot.ToDomain(World);
            var caseFile = CaseFileSnapshot.ToDomain(CaseFile);
            var player = PlayerSnapshot.ToDomain(Player);
            var pursuitState = PursuitStateSnapshot.ToDomain(PursuitState);
            var clock = GameClockSnapshot.ToDomain(Clock);
            var journey = Journey is null ? null : TravelJourney.FromSnapshot(Journey.ToDomain());
            var townVisit = CurrentTownVisit?.ToDomain() ?? new TownVisitState(player.CurrentTownId);
            var session = GameSessionRehydrator.Create(
                new GameSessionId(Id),
                player,
                world,
                caseFile,
                pursuitState,
                clock,
                Status,
                journey,
                TravelDifficulty,
                TravelRandomness?.ToDomain() ?? TravelRandomnessState.CreateRuntimeSalted(),
                Entropy ?? AdventureRandomnessPolicy.Standard,
                townVisit,
                (CompletedJourneyHistory ?? Array.Empty<JourneySnapshot>()).Select(snapshot => snapshot.ToDomain()).ToArray(),
                WantedSuspectPresenceLedger.Select(snapshot => snapshot.ToDomain()).ToArray());

            GameSessionRehydrator.ReplaceTravelDiaryDays(session, TravelDiaryDays);
            GameSessionRehydrator.ReplaceLogEntries(session, LogEntries.Select(GameLogEntrySnapshot.ToDomain).ToArray());
            return session;
        }
    }
}
