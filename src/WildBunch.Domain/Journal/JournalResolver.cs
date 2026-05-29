using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;

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
            session.CaseFile.TrueCulpritId.Value,
            session.CaseFile.Suspects.ToArray(),
            session.CaseFile.KnownClues.ToArray(),
            session.LogEntries.ToArray());
    }
}
