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
    string? DeclaredWantedIdentityHandle,
    string? TargetName,
    WarrantDisposition? Disposition,
    bool? IsAlive,
    bool? IsSecured,
    bool SessionChanged,
    SaloonPersonOfInterestKind? PersonOfInterestKind = null)
{
    public static WantedSuspectConfrontationResult Surrendered(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition disposition,
        string message)
        => new(true, message, WantedSuspectConfrontationOutcome.Surrendered, declaredWantedIdentityHandle, targetName, disposition, true, true, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static WantedSuspectConfrontationResult Fled(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition disposition,
        string message)
        => new(true, message, WantedSuspectConfrontationOutcome.Fled, declaredWantedIdentityHandle, targetName, disposition, true, false, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static WantedSuspectConfrontationResult Killed(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition disposition,
        string message)
        => new(true, message, WantedSuspectConfrontationOutcome.Killed, declaredWantedIdentityHandle, targetName, disposition, false, true, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static WantedSuspectConfrontationResult Abandoned(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition disposition,
        string message)
        => new(true, message, WantedSuspectConfrontationOutcome.Abandoned, declaredWantedIdentityHandle, targetName, disposition, null, null, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static WantedSuspectConfrontationResult Rejected(
        string message,
        string? declaredWantedIdentityHandle = null,
        string? targetName = null,
        WarrantDisposition? disposition = null,
        bool sessionChanged = false)
        => new(false, message, WantedSuspectConfrontationOutcome.Rejected, declaredWantedIdentityHandle, targetName, disposition, null, null, sessionChanged, SaloonPersonOfInterestKind.WantedSuspect);
}
