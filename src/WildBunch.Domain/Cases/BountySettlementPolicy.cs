namespace WildBunch.Domain.Cases;

public static class BountySettlementPolicy
{
    public static decimal CalculateCappedFine(decimal walletCash, decimal fineAmount)
        => Math.Min(fineAmount, walletCash);

    public static bool TryCreateSheriffTurnInSettlementState(
        CaseFile caseFile,
        SheriffTurnInResult assessment,
        SuspectId targetSuspectId,
        bool isAlive,
        int day,
        int turn,
        out SheriffTurnInSettlementState settlementState,
        out SheriffTurnInResult rejectionResult)
    {
        ArgumentNullException.ThrowIfNull(caseFile);

        return TryCreateSheriffTurnInSettlementState(
            caseFile.SheriffTurnInSettlements,
            assessment,
            targetSuspectId,
            isAlive,
            day,
            turn,
            out settlementState,
            out rejectionResult);
    }

    /// <summary>
    /// Overload that takes the existing settlement states directly, for use by
    /// BountyLoop which receives them via context records instead of a CaseFile.
    /// </summary>
    public static bool TryCreateSheriffTurnInSettlementState(
        IReadOnlyList<SheriffTurnInSettlementState> existingSettlements,
        SheriffTurnInResult assessment,
        SuspectId targetSuspectId,
        bool isAlive,
        int day,
        int turn,
        out SheriffTurnInSettlementState settlementState,
        out SheriffTurnInResult rejectionResult)
    {
        ArgumentNullException.ThrowIfNull(existingSettlements);

        if (!assessment.Success)
        {
            throw new ArgumentException("A successful sheriff turn-in assessment is required.", nameof(assessment));
        }

        var existingSettlement = existingSettlements.FirstOrDefault(s => s.SuspectId.Equals(targetSuspectId));
        if (existingSettlement is not null)
        {
            settlementState = null!;
            rejectionResult = SheriffTurnInResult.Rejected(
                $"You have already been paid for {existingSettlement.TargetName}.",
                existingSettlement.TargetName,
                existingSettlement.Disposition,
                existingSettlement.BountyAmount);
            return false;
        }

        var bountyAmount = assessment.BountyAmount
            ?? throw new InvalidOperationException("Sheriff turn-in assessment did not include a bounty amount.");
        var targetName = assessment.TargetName
            ?? throw new InvalidOperationException("Sheriff turn-in assessment did not include a target name.");
        var disposition = assessment.Disposition
            ?? throw new InvalidOperationException("Sheriff turn-in assessment did not include a disposition.");

        settlementState = new SheriffTurnInSettlementState(
            targetSuspectId,
            targetName,
            disposition,
            isAlive,
            bountyAmount,
            day,
            turn);
        rejectionResult = default!;
        return true;
    }
}
