namespace WildBunch.Domain.Cases;

public enum WantedSuspectConfrontationChoice
{
    Surrendered = 0,
    Fled = 1,
    Killed = 2,
    Abandoned = 3
}

public enum WantedSuspectConfrontationOutcome
{
    Surrendered = 0,
    Fled = 1,
    Killed = 2,
    Abandoned = 3,
    Rejected = 4
}

public sealed record WantedSuspectConfrontationState(
    SuspectId SuspectId,
    string TargetName,
    WarrantDisposition Disposition,
    WantedSuspectConfrontationOutcome Outcome,
    bool IsAlive,
    bool IsSecured,
    int Day,
    int Turn)
{
    public bool IsTurnInEligible => IsSecured;
}

public sealed record WantedSuspectConfrontationResult(
    bool Success,
    string Message,
    WantedSuspectConfrontationOutcome Outcome,
    string? TargetName,
    WarrantDisposition? Disposition,
    bool? IsAlive,
    bool? IsSecured,
    bool SessionChanged)
{
    public static WantedSuspectConfrontationResult Surrendered(
        string targetName,
        WarrantDisposition disposition,
        string message)
        => new(true, message, WantedSuspectConfrontationOutcome.Surrendered, targetName, disposition, true, true, true);

    public static WantedSuspectConfrontationResult Fled(
        string targetName,
        WarrantDisposition disposition,
        string message)
        => new(true, message, WantedSuspectConfrontationOutcome.Fled, targetName, disposition, true, false, true);

    public static WantedSuspectConfrontationResult Killed(
        string targetName,
        WarrantDisposition disposition,
        string message)
        => new(true, message, WantedSuspectConfrontationOutcome.Killed, targetName, disposition, false, true, true);

    public static WantedSuspectConfrontationResult Abandoned(
        string targetName,
        WarrantDisposition disposition,
        string message)
        => new(true, message, WantedSuspectConfrontationOutcome.Abandoned, targetName, disposition, null, null, true);

    public static WantedSuspectConfrontationResult Rejected(
        string message,
        string? targetName = null,
        WarrantDisposition? disposition = null,
        bool sessionChanged = false)
        => new(false, message, WantedSuspectConfrontationOutcome.Rejected, targetName, disposition, null, null, sessionChanged);
}
