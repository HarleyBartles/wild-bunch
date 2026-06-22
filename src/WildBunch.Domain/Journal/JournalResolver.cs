using WildBunch.Domain.Game;

// LogEntries is [Obsolete] (projection-legacy per ADR-0028). The journal resolver
// still reads it for backward-compatible journal output. Do not add new consumers.
#pragma warning disable CS0618

namespace WildBunch.Domain.Journal;

public sealed class JournalResolver
{
    public JournalSnapshot Resolve(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var currentTown = session.World.GetTown(session.Player.CurrentTownId);

        return new JournalSnapshot(
            session.Id.Value,
            session.Status,
            session.Clock.Day,
            session.Clock.Turn,
            currentTown.Id,
            currentTown.Name,
            session.CaseFile.Accusation.HasValue ? session.CaseFile.Accusation.Value.Value : null,
            session.CaseFile.OpeningLead.Description,
            session.CaseFile.KillerReleaseState,
            "Find the culprit before the law closes in.",
            session.CaseFile.GetDiscoveredSuspects(),
            session.CaseFile.KnownClues.ToArray(),
            session.CaseFile.KnownWarrants.ToArray(),
            session.CaseFile.SheriffTurnInSettlements.ToArray(),
            session.LogEntries.ToArray());
    }
}
