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
    string? DeclaredWantedIdentityHandle,
    string? TargetName,
    WarrantDisposition? Disposition,
    bool? IsAlive,
    bool? IsSecured,
    bool SessionChanged)
{
    public static SaloonPersonOfInterestConfrontationResult Surrendered(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Surrendered, declaredWantedIdentityHandle, targetName, disposition, true, true, true);

    public static SaloonPersonOfInterestConfrontationResult Fled(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Fled, declaredWantedIdentityHandle, targetName, disposition, true, false, true);

    public static SaloonPersonOfInterestConfrontationResult Killed(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Killed, declaredWantedIdentityHandle, targetName, disposition, false, true, true);

    public static SaloonPersonOfInterestConfrontationResult Abandoned(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Abandoned, declaredWantedIdentityHandle, targetName, disposition, null, null, true);

    public static SaloonPersonOfInterestConfrontationResult Rejected(
        string message,
        string? declaredWantedIdentityHandle = null,
        string? targetName = null,
        WarrantDisposition? disposition = null,
        bool sessionChanged = false)
        => new(false, message, SaloonPersonOfInterestConfrontationOutcome.Rejected, declaredWantedIdentityHandle, targetName, disposition, null, null, sessionChanged);

    public static SaloonPersonOfInterestConfrontationResult FromWantedSuspectResult(WantedSuspectConfrontationResult result)
        => new(
            result.Success,
            result.Message,
            (SaloonPersonOfInterestConfrontationOutcome)result.Outcome,
            result.DeclaredWantedIdentityHandle,
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
            DeclaredWantedIdentityHandle,
            TargetName,
            Disposition,
            IsAlive,
            IsSecured,
            SessionChanged);
}
