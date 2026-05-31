using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Games.Mapping;

public static class JournalMapper
{
    public static JournalDto ToDto(JournalSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new JournalDto(
            snapshot.SessionId,
            snapshot.Status,
            new GameClockDto(snapshot.Day, snapshot.Turn),
            new JournalTownDto(snapshot.CurrentTownId.Value, snapshot.CurrentTownName),
            new JournalCaseFileDto(
                snapshot.OpeningLead,
                ToDto(snapshot.KillerReleaseState),
                snapshot.CaseSummary,
                snapshot.DiscoveredSuspects.Select(ToDto).ToArray(),
                snapshot.KnownClues.Select(ToDto).ToArray(),
                snapshot.KnownWarrants.Select(ToDto).ToArray()),
            snapshot.LogEntries.Select(ToDto).ToArray());
    }

    private static KillerReleaseStateDto ToDto(KillerReleaseState state)
        => new(
            state.IsReleased,
            state.Progress,
            state.RequiredPublicClues,
            state.StatusText);

    private static ClueDto ToDto(Clue clue)
        => new(
            clue.Id.Value,
            clue.Kind,
            clue.Description);

    private static DiscoveredSuspectDto ToDto(Suspect suspect)
        => new(
            suspect.Id.Value,
            suspect.Name,
            suspect.Status);

    private static WarrantDto ToDto(Warrant warrant)
        => new(
            warrant.TargetName,
            warrant.Summary,
            warrant.Terms.IssuingSource,
            warrant.Terms.Disposition,
            warrant.Terms.BountyAmount);

    private static GameLogEntryDto ToDto(GameLogEntry logEntry)
        => new(
            logEntry.Kind,
            logEntry.Message,
            logEntry.Day,
            logEntry.Turn);
}
