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

        var activeWarrants = ExcludeCapturedWarrants(snapshot.KnownWarrants, snapshot.SheriffTurnInSettlements);

        return new JournalDto(
            snapshot.SessionId,
            snapshot.Status,
            new GameClockDto(snapshot.Day, snapshot.Turn, ((TimeOfDay)snapshot.Turn).ToString(), BeatLabelRenderer.Render((TimeOfDay)snapshot.Turn, snapshot.Day)),
            new JournalTownDto(snapshot.CurrentTownId.Value, snapshot.CurrentTownName),
            new JournalCaseFileDto(
                snapshot.OpeningLead,
                CaseReadMapper.ToDto(snapshot.KillerReleaseState),
                snapshot.CaseSummary,
                snapshot.DiscoveredSuspects.Select(ToDto).ToArray(),
                CaseBoardMapper.ToDto(snapshot.KnownClues, snapshot.KnownWarrants, snapshot.SheriffTurnInSettlements),
                snapshot.KnownClues.Select(CaseReadMapper.ToDto).ToArray(),
                activeWarrants.Select(ToDto).ToArray(),
                WantedPosterMapper.ToDto(activeWarrants)),
            snapshot.LogEntries.Select(ToDto).ToArray());
    }

    private static IReadOnlyList<Warrant> ExcludeCapturedWarrants(
        IReadOnlyList<Warrant> warrants,
        IReadOnlyList<SheriffTurnInSettlementState> sheriffTurnInSettlements)
    {
        var capturedTargetNames = sheriffTurnInSettlements
            .Select(settlement => Normalize(settlement.TargetName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return warrants
            .Where(warrant => !capturedTargetNames.Contains(Normalize(warrant.TargetName)))
            .ToArray();
    }

    private static string Normalize(string value)
        => string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

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
