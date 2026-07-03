using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    private sealed record GameSessionSnapshot(
        Guid Id,
        GameStatus Status,
        GameDifficulty GameDifficulty,
        GameEntropy? GameEntropy,
        SaltSourceSnapshot? SaltSource,
        string? SeedCode,
        TownVisitStateSnapshot? CurrentTownVisit,
        PlayerSnapshot Player,
        global::WildBunch.Domain.World.WorldSnapshot World,
        CaseFileSnapshot CaseFile,
        PursuitStateSnapshot PursuitState,
        GameClockSnapshot Clock,
        TownActionContext CurrentActionContext,
        string? CurrentActionContextTownId,
        JourneySnapshot? Journey,
        IReadOnlyList<JourneySnapshot>? CompletedJourneyHistory,
        IReadOnlyList<WantedSuspectPresenceSnapshot> WantedSuspectPresenceLedger,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
        DevTravelOverride? PendingDevTravelOverride,
        DevSaloonOverride? PendingDevSaloonOverride,
        UnrelatedCriminalLedgerSnapshot? UnrelatedCriminalLedger)
    {
        public static GameSessionSnapshot FromDomain(GameSession session)
            => new(
                session.Id.Value,
                session.Status,
                session.GameDifficulty,
                session.GameEntropy,
                SaltSourceSnapshot.FromDomain(session.SaltSource),
                session.SeedCode,
                TownVisitStateSnapshot.FromDomain(session.CurrentTownVisit),
                PlayerSnapshot.FromDomain(session.Player),
                global::WildBunch.Domain.World.WorldSnapshot.FromDomain(session.World),
                CaseFileSnapshot.FromDomain(session.CaseFile),
                PursuitStateSnapshot.FromDomain(session.PursuitState),
                GameClockSnapshot.FromDomain(session.Clock),
                session.CurrentActionContext,
                session.CurrentActionContextTownId?.Value,
                session.Journey is null ? null : JourneySnapshot.FromDomain(session.Journey.ToSnapshot(session.TravelRules)),
                session.CompletedJourneyHistory.Select(JourneySnapshot.FromDomain).ToArray(),
                session.WantedSuspectPresenceEntries.Select(WantedSuspectPresenceSnapshot.FromDomain).ToArray(),
                session.TravelDiaryDays.ToArray(),
                session.PendingDevTravelOverride,
                session.PendingDevSaloonOverride,
                session.UnrelatedCriminalLedger.ToSnapshot());

        public GameSession ToDomain()
        {
            var world = World.ToDomain();
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
                GameDifficulty,
                SaltSource?.ToDomain() ?? WildBunch.Domain.Game.SaltSource.CreateRuntime(),
                GameEntropy ?? WildBunch.Domain.Travel.GameEntropy.Classic,
                townVisit,
                (CompletedJourneyHistory ?? Array.Empty<JourneySnapshot>()).Select(snapshot => snapshot.ToDomain()).ToArray(),
                (WantedSuspectPresenceLedger ?? Array.Empty<WantedSuspectPresenceSnapshot>()).Select(snapshot => snapshot.ToDomain()).ToArray());

            TownId? contextTownId = CurrentActionContextTownId is null ? null : new TownId(CurrentActionContextTownId);
            GameSessionRehydrator.RestoreActionContextState(session, CurrentActionContext, contextTownId);
            
            // Set SeedCode from snapshot as a cache. The true source of truth is the
            // GameStarted event, which will be applied during event replay if there are
            // post-snapshot events. When the snapshot is current, this restores the
            // persisted seed code. See BUNCH-101.
            GameSessionRehydrator.SetBackingField(session, "<SeedCode>k__BackingField", SeedCode);
            
            GameSessionRehydrator.ReplaceTravelDiaryDays(session, TravelDiaryDays);
            if (PendingDevTravelOverride is not null)
            {
                session.RestorePendingDevTravelOverride(PendingDevTravelOverride);
            }
            if (PendingDevSaloonOverride is not null || UnrelatedCriminalLedger is not null)
            {
                session.RestoreBountyLoopState(
                    UnrelatedCriminalLedger is not null
                        ? WildBunch.Domain.Cases.UnrelatedCriminalLedger.FromSnapshot(UnrelatedCriminalLedger)
                        : null,
                    PendingDevSaloonOverride);
            }

            return session;
        }
    }
}
