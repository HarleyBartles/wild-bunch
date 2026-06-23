using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Application.Projections;

/// <summary>
/// Reference projector for the diary projection.
/// Derives the player's travel diary from typed domain events.
/// This is a pure function over the event stream — no aggregate mutation.
/// See ADR-0028.
/// </summary>
public sealed class DiaryProjector : IDomainEventProjector<DiaryProjection>
{
    public DiaryProjection Project(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        Guid sessionId = Guid.Empty;
        int day = 1;
        int turn = 0;
        TownId currentTownId = default;
        string currentTownName = string.Empty;
        var entries = new List<DiaryEntry>();

        foreach (var e in events)
        {
            switch (e)
            {
                case GameStarted gs:
                    sessionId = default;
                    day = 1;
                    turn = 0;
                    currentTownId = gs.StartingTownId;
                    currentTownName = gs.StartingTownName;
                    entries.Add(new DiaryEntry(day, turn, $"Arrived in {gs.StartingTownName}. The hunt begins."));
                    break;

                case TownActionContextEntered tc:
                    // Track time from the context event — this is the event-sourced clock
                    // state, not a local counter. See ADR-0028 and BUNCH-80.
                    day = tc.Day;
                    turn = tc.Turn;
                    break;

                case StoreItemPurchased sp:
                    entries.Add(new DiaryEntry(day, turn, $"Bought supplies at the general store."));
                    break;

                case InvestigationPerformed ip:
                    entries.Add(new DiaryEntry(day, turn, ip.Message));
                    break;

                case SaloonPersonOfInterestSpotted sp:
                    if (sp.RecordLog)
                        entries.Add(new DiaryEntry(day, turn, sp.Message));
                    break;

                case WantedSuspectConfronted wc:
                    entries.Add(new DiaryEntry(day, turn, wc.Message));
                    break;

                case SheriffTurnInSettled st:
                    entries.Add(new DiaryEntry(day, turn, st.Message));
                    break;

                case SaloonPersonOfInterestConfronted:
                    // No diary entry from this event — log entries come from delegated
                    // WantedSuspectConfronted/SheriffTurnInSettled events.
                    break;
            }
        }

        return new DiaryProjection(
            sessionId,
            day,
            turn,
            currentTownId,
            currentTownName,
            entries);
    }
}
