using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

// LogEntries is [Obsolete] (projection-legacy per ADR-0028).
#pragma warning disable CS0618

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
        WorldSnapshot World,
        CaseFileSnapshot CaseFile,
        PursuitStateSnapshot PursuitState,
        GameClockSnapshot Clock,
        TownActionContext CurrentActionContext,
        string? CurrentActionContextTownId,
        JourneySnapshot? Journey,
        IReadOnlyList<JourneySnapshot>? CompletedJourneyHistory,
        IReadOnlyList<WantedSuspectPresenceSnapshot> WantedSuspectPresenceLedger,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
        IReadOnlyList<GameLogEntrySnapshot> LogEntries,
        DevTravelOverride? PendingDevTravelOverride,
        DevSaloonOverride? PendingDevSaloonOverride)
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
                WorldSnapshot.FromDomain(session.World),
                CaseFileSnapshot.FromDomain(session.CaseFile),
                PursuitStateSnapshot.FromDomain(session.PursuitState),
                GameClockSnapshot.FromDomain(session.Clock),
                session.CurrentActionContext,
                session.CurrentActionContextTownId?.Value,
                session.Journey is null ? null : JourneySnapshot.FromDomain(session.Journey.ToSnapshot(session.TravelRules)),
                session.CompletedJourneyHistory.Select(JourneySnapshot.FromDomain).ToArray(),
                session.WantedSuspectPresenceEntries.Select(WantedSuspectPresenceSnapshot.FromDomain).ToArray(),
                session.TravelDiaryDays.ToArray(),
                session.LogEntries.Select(GameLogEntrySnapshot.FromDomain).ToArray(),
                session.PendingDevTravelOverride,
                session.PendingDevSaloonOverride);

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
                GameDifficulty,
                SaltSource?.ToDomain() ?? WildBunch.Domain.Game.SaltSource.CreateRuntime(),
                GameEntropy ?? WildBunch.Domain.Travel.GameEntropy.Classic,
                townVisit,
                (CompletedJourneyHistory ?? Array.Empty<JourneySnapshot>()).Select(snapshot => snapshot.ToDomain()).ToArray(),
                (WantedSuspectPresenceLedger ?? Array.Empty<WantedSuspectPresenceSnapshot>()).Select(snapshot => snapshot.ToDomain()).ToArray());

            TownId? contextTownId = CurrentActionContextTownId is null ? null : new TownId(CurrentActionContextTownId);
            GameSessionRehydrator.SetCurrentActionContext(session, CurrentActionContext, contextTownId);
            
            // Set SeedCode from snapshot as a cache. The true source of truth is the
            // GameStarted event, which will be applied during event replay if there are
            // post-snapshot events. When the snapshot is current, this restores the
            // persisted seed code. See BUNCH-101.
            GameSessionRehydrator.SetBackingField(session, "<SeedCode>k__BackingField", SeedCode);
            
            GameSessionRehydrator.ReplaceTravelDiaryDays(session, TravelDiaryDays);
            GameSessionRehydrator.ReplaceLogEntries(session, LogEntries.Select(GameLogEntrySnapshot.ToDomain).ToArray());
            if (PendingDevTravelOverride is not null)
            {
                GameSessionRehydrator.SetBackingField(session, "_pendingDevTravelOverride", PendingDevTravelOverride);
            }
            if (PendingDevSaloonOverride is not null)
            {
                GameSessionRehydrator.SetBackingField(session, "_pendingDevSaloonOverride", PendingDevSaloonOverride);
            }
            return session;
        }
    }
}
