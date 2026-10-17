using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Mapping;

public static class CaseBoardMapper
{
    public static CaseBoardDto ToDto(
        IReadOnlyList<Clue> clues,
        IReadOnlyList<Warrant> warrants,
        IReadOnlyList<SheriffTurnInSettlementState>? sheriffTurnInSettlements = null)
    {
        ArgumentNullException.ThrowIfNull(clues);
        ArgumentNullException.ThrowIfNull(warrants);

        var settlementsBySuspectId = (sheriffTurnInSettlements ?? Array.Empty<SheriffTurnInSettlementState>())
            .ToDictionary(settlement => settlement.SuspectId);
        var warrantRecords = warrants.Select(warrant => ToWarrantRecord(warrant, settlementsBySuspectId)).ToArray();
        var clueRecords = clues.Select(ToClueRecord).ToArray();

        return new CaseBoardDto(warrantRecords, clueRecords);
    }

    private static CaseWarrantRecordDto ToWarrantRecord(
        Warrant warrant,
        IReadOnlyDictionary<SuspectId, SheriffTurnInSettlementState> settlementsBySuspectId)
    {
        ArgumentNullException.ThrowIfNull(warrant);

        var settlement = warrant.TargetSuspectId is { } suspectId
            && settlementsBySuspectId.TryGetValue(suspectId, out var foundSettlement)
                ? new SheriffTurnInSettlementDto(
                    foundSettlement.IsAlive,
                    foundSettlement.BountyAmount,
                    foundSettlement.Day,
                    foundSettlement.Turn)
                : null;

        return new CaseWarrantRecordDto(
            warrant.Id.Value,
            warrant.TargetName,
            warrant.Terms.Disposition,
            warrant.Terms.BountyAmount,
            warrant.Terms.KnownAliases.ToArray(),
            warrant.Terms.KnownFeatures.ToArray(),
            warrant.Terms.IssuingSource,
            warrant.Summary,
            settlement);
    }

    private static CaseClueDto ToClueRecord(Clue clue)
    {
        ArgumentNullException.ThrowIfNull(clue);

        return new CaseClueDto(
            clue.Id.Value,
            clue.Kind,
            clue.Description,
            clue.SourceKind,
            clue.Source,
            clue.Context,
            CaseReadMapper.ToDto(clue.Anchors));
    }
}
