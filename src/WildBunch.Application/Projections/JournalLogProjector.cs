using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Projections;

/// <summary>
/// Pure projector that derives the legacy <see cref="GameLogEntry"/> sequence from the
/// typed domain event stream. This is the projection-backed replacement for the legacy
/// aggregate log entries. See ADR-0028 and BUNCH-84.
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

                case SaloonPersonOfInterestConfronted confrontation:
                    // Delegated wanted outcomes already have typed narration events; only unique saloon-level results belong here.
                    if (confrontation.Outcome is SaloonPersonOfInterestConfrontationOutcome.Rejected
                        or SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration)
                    {
                        entries.Add(new GameLogEntry(GameLogEntryKind.CaseUpdate, confrontation.Message, day, turn));
                    }
                    break;

                case SheriffTurnInSettled settlement:
                    entries.Add(new GameLogEntry(
                        GameLogEntryKind.CaseUpdate,
                        settlement.Message,
                        settlement.Day,
                        settlement.Turn));
                    break;

                case JourneyStarted js:
                    if (!string.IsNullOrEmpty(js.DiaryMessage))
                        entries.Add(TravelEntry(js.DiaryMessage, day, turn, js.JourneySnapshot.JourneySequence));
                    break;

                case TravelDayAdvanced tda:
                    day = tda.Day;
                    turn = 0;
                    foreach (var narration in tda.AdditionalDiaryMessages)
                        entries.Add(TravelEntry(narration, day, turn, tda.JourneySnapshot.JourneySequence));
                    if (!string.IsNullOrEmpty(tda.DiaryMessage))
                        entries.Add(TravelEntry(tda.DiaryMessage, day, turn, tda.JourneySnapshot.JourneySequence));
                    else if (!string.IsNullOrEmpty(tda.HorseLostMessage))
                        entries.Add(TravelEntry(tda.HorseLostMessage, day, turn, tda.JourneySnapshot.JourneySequence));
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
                        entries.Add(TravelEntry(tea.DiaryMessage, trailDay, turn, tea.JourneySnapshot.JourneySequence));
                    else if (!string.IsNullOrEmpty(tea.HorseLostMessage))
                        entries.Add(TravelEntry(tea.HorseLostMessage, trailDay, turn, tea.JourneySnapshot.JourneySequence));
                    break;

                case JourneyEncounterResolved jer:
                    foreach (var narration in jer.AdditionalDiaryMessages)
                        entries.Add(TravelEntry(narration, day, turn, jer.JourneySnapshot.JourneySequence));
                    if (!string.IsNullOrEmpty(jer.DiaryMessage))
                        entries.Add(TravelEntry(jer.DiaryMessage, day, turn, jer.JourneySnapshot.JourneySequence));
                    break;

                case JourneyCompleted jc:
                    if (!string.IsNullOrEmpty(jc.DiaryMessage))
                        entries.Add(TravelEntry(jc.DiaryMessage, day, turn, jc.JourneySnapshot.JourneySequence));
                    break;

                case JourneyArrivalAcknowledged jaa:
                    if (!string.IsNullOrEmpty(jaa.DiaryMessage))
                        entries.Add(TravelEntry(jaa.DiaryMessage, day, turn, jaa.JourneySequence));
                    break;
            }
        }

        return entries;
    }

    private static GameLogEntry TravelEntry(string message, int day, int turn, int journeySequence)
        => new(GameLogEntryKind.Travel, message, day, turn, journeySequence);
}
