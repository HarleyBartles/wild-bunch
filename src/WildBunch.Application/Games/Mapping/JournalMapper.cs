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
                CaseReadMapper.ToDto(snapshot.KillerReleaseState),
                snapshot.CaseSummary,
                snapshot.DiscoveredSuspects.Select(suspect => ToDto(suspect, snapshot.KnownClues)).ToArray(),
                snapshot.KnownClues.Select(CaseReadMapper.ToDto).ToArray(),
                snapshot.KnownWarrants.Select(ToDto).ToArray()),
            snapshot.LogEntries.Select(ToDto).ToArray());
    }

    private static DiscoveredSuspectDto ToDto(Suspect suspect, IReadOnlyList<Clue> knownClues)
        => new(
            suspect.Id.Value,
            suspect.Name,
            suspect.Status,
            knownClues
                .Where(clue => clue.LinkedSuspectIds.Any(linkedSuspectId => linkedSuspectId.Equals(suspect.Id)))
                .Select(CaseReadMapper.ToLeadSummary)
                .ToArray());

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
