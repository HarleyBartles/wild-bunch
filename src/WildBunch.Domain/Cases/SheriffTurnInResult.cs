namespace WildBunch.Domain.Cases;

public enum SheriffTurnInOutcome
{
    AcceptedAlive = 0,
    AcceptedDead = 1,
    WrongPersonAlive = 2,
    WrongPersonDead = 3,
    Rejected = 4
}

public sealed record SheriffTurnInResult(
    bool Success,
    string Message,
    SheriffTurnInOutcome Outcome,
    string? TargetName,
    WarrantDisposition? Disposition,
    decimal? BountyAmount,
    bool SessionChanged)
{
    public static SheriffTurnInResult AcceptedAlive(string targetName, WarrantDisposition disposition, decimal bountyAmount, string message)
        => new(true, message, SheriffTurnInOutcome.AcceptedAlive, targetName, disposition, bountyAmount, false);

    public static SheriffTurnInResult AcceptedDead(string targetName, WarrantDisposition disposition, decimal bountyAmount, string message)
        => new(true, message, SheriffTurnInOutcome.AcceptedDead, targetName, disposition, bountyAmount, false);

    public static SheriffTurnInResult WrongPersonAlive(string message, string? targetName = null)
        => new(false, message, SheriffTurnInOutcome.WrongPersonAlive, targetName, null, null, false);

    public static SheriffTurnInResult WrongPersonDead(string message, string? targetName = null)
        => new(false, message, SheriffTurnInOutcome.WrongPersonDead, targetName, null, null, false);

    public static SheriffTurnInResult Rejected(string message, string? targetName = null, WarrantDisposition? disposition = null, decimal? bountyAmount = null)
        => new(false, message, SheriffTurnInOutcome.Rejected, targetName, disposition, bountyAmount, false);
}
