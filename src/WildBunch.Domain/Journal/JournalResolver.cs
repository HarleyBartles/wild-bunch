using WildBunch.Domain.Game;

namespace WildBunch.Domain.Journal;

public sealed class JournalResolver
{
    /// <summary>
    /// Resolves a journal snapshot from the session state and projection-backed
    /// log entries. The caller must project log entries from the event stream
    /// via JournalLogProjector (Application.Projections). See BUNCH-86.
    /// </summary>
    public JournalSnapshot Resolve(GameSession session, IReadOnlyList<GameLogEntry> logEntries)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(logEntries);

        var currentTown = session.Player.CurrentTownId is not null
            ? session.World.GetTown(session.Player.CurrentTownId.Value)
            : null;

        return new JournalSnapshot(
            session.Id.Value,
            session.Status,
            session.Clock.Day,
            session.Clock.Turn,
            currentTown?.Id,
            currentTown?.Name,
            session.CaseFile.Accusation.HasValue ? session.CaseFile.Accusation.Value.Value : null,
            session.CaseFile.OpeningLead.Description,
            session.CaseFile.KillerReleaseState,
            "Find the culprit before the law closes in.",
            session.CaseFile.GetDiscoveredSuspects(),
            session.CaseFile.KnownClues.ToArray(),
            session.CaseFile.KnownWarrants.ToArray(),
            session.CaseFile.SheriffTurnInSettlements.ToArray(),
            logEntries.ToArray());
    }
}
