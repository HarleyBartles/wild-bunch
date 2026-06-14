namespace WildBunch.Domain.Cases;

public enum SaloonPersonOfInterestConfrontationOutcome
{
    Surrendered = 0,
    Fled = 1,
    Killed = 2,
    Abandoned = 3,
    Rejected = 4
}

public sealed record SaloonPersonOfInterestConfrontationResult(
    bool Success,
    string Message,
    SaloonPersonOfInterestConfrontationOutcome Outcome,
    string? TargetName,
    WarrantDisposition? Disposition,
    bool? IsAlive,
    bool? IsSecured,
    bool SessionChanged)
{
    public static SaloonPersonOfInterestConfrontationResult Surrendered(
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Surrendered, targetName, disposition, true, true, true);

    public static SaloonPersonOfInterestConfrontationResult Fled(
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Fled, targetName, disposition, true, false, true);

    public static SaloonPersonOfInterestConfrontationResult Killed(
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Killed, targetName, disposition, false, true, true);

    public static SaloonPersonOfInterestConfrontationResult Abandoned(
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Abandoned, targetName, disposition, null, null, true);

    public static SaloonPersonOfInterestConfrontationResult Rejected(
        string message,
        string? targetName = null,
        WarrantDisposition? disposition = null,
        bool sessionChanged = false)
        => new(false, message, SaloonPersonOfInterestConfrontationOutcome.Rejected, targetName, disposition, null, null, sessionChanged);

    public static SaloonPersonOfInterestConfrontationResult FromWantedSuspectResult(WantedSuspectConfrontationResult result)
        => new(
            result.Success,
            result.Message,
            (SaloonPersonOfInterestConfrontationOutcome)result.Outcome,
            result.TargetName,
            result.Disposition,
            result.IsAlive,
            result.IsSecured,
            result.SessionChanged);

    public WantedSuspectConfrontationResult ToWantedSuspectResult()
        => new(
            Success,
            Message,
            (WantedSuspectConfrontationOutcome)Outcome,
            TargetName,
            Disposition,
            IsAlive,
            IsSecured,
            SessionChanged);
}
