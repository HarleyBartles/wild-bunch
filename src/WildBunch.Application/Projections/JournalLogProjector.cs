using WildBunch.Domain.Events;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Projections;

/// <summary>
/// Pure projector that derives the legacy <see cref="GameLogEntry"/> sequence from the
/// typed domain event stream, reproducing exactly what <see cref="GameSession"/>'s Apply
/// methods produce via AddLogEntry/RecordCaseUpdate/RecordTravelUpdate.
/// This is the projection-backed replacement for the GameSessionLogEntries table on the
/// journal read path. See ADR-0028 and BUNCH-84.
/// </summary>
public sealed class JournalLogProjector
{
    public IReadOnlyList<GameLogEntry> Project(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var day = 1;
        var turn = 0;
        var entries = new List<GameLogEntry>();

        for (var i = 0; i < events.Count; i++)
        {
            var e = events[i];
            switch (e)
            {
                case GameStarted gs:
                    day = 1;
                    turn = 0;
                    entries.Add(new GameLogEntry(GameLogEntryKind.Opening, $"The hunt begins in {gs.StartingTownName}.", day, turn));
                    break;

                case TownActionContextEntered tc:
                    day = tc.Day;
                    turn = tc.Turn;
                    break;

                case StoreItemPurchased p:
                    var purchaseQuantityLabel = p.Quantity == 1 ? p.DisplayName : $"{p.Quantity} {p.DisplayName}";
                    entries.Add(new GameLogEntry(GameLogEntryKind.Purchase, $"Purchased {purchaseQuantityLabel} for ${p.TotalPrice:0.00}.", day, turn));
                    break;

                case InvestigationPerformed ip:
                    entries.Add(new GameLogEntry(GameLogEntryKind.CaseUpdate, ip.Message, day, turn));
                    break;

                case SaloonPersonOfInterestSpotted sp:
                    if (sp.RecordLog)
                        entries.Add(new GameLogEntry(GameLogEntryKind.CaseUpdate, sp.Message, day, turn));
                    break;

                case WantedSuspectConfronted wc:
                    entries.Add(new GameLogEntry(GameLogEntryKind.CaseUpdate, wc.Message, day, turn));
                    break;

                case SheriffTurnInSettled:
                    // Legacy Apply adds no log entry for sheriff turn-in.
                    break;

                case SaloonPersonOfInterestConfronted:
                    // No log entry.
                    break;

                case JourneyStarted js:
                    if (!string.IsNullOrEmpty(js.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, js.DiaryMessage, day, turn));
                    break;

                case TravelDayAdvanced tda:
                    day = tda.Day;
                    turn = 0;
                    foreach (var narration in tda.AdditionalDiaryMessages)
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, narration, day, turn));
                    if (!string.IsNullOrEmpty(tda.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, tda.DiaryMessage, day, turn));
                    if (!string.IsNullOrEmpty(tda.HorseLostMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, tda.HorseLostMessage, day, turn));
                    break;

                case TrailEventApplied tea:
                    // TrailEventApplied may appear before TravelDayAdvanced in the event
                    // stream. In the command path, the clock is advanced directly before
                    // the trail event narration is logged, so the narration uses the new
                    // day. Look ahead: if the next event is TravelDayAdvanced, use its Day.
                    var trailDay = day;
                    if (i + 1 < events.Count && events[i + 1] is TravelDayAdvanced next)
                        trailDay = next.Day;
                    if (!string.IsNullOrEmpty(tea.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, tea.DiaryMessage, trailDay, turn));
                    if (!string.IsNullOrEmpty(tea.HorseLostMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, tea.HorseLostMessage, trailDay, turn));
                    break;

                case JourneyEncounterResolved jer:
                    foreach (var narration in jer.AdditionalDiaryMessages)
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, narration, day, turn));
                    if (!string.IsNullOrEmpty(jer.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, jer.DiaryMessage, day, turn));
                    break;

                case JourneyCompleted jc:
                    if (!string.IsNullOrEmpty(jc.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, jc.DiaryMessage, day, turn));
                    break;

                case JourneyArrivalAcknowledged jaa:
                    if (!string.IsNullOrEmpty(jaa.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, jaa.DiaryMessage, day, turn));
                    break;
            }
        }

        return entries;
    }
}
