namespace WildBunch.Domain.Cases;

public sealed record CaseInvestigationResult(bool Success, string Message, bool SessionChanged)
{
    public static CaseInvestigationResult Failed(string message) => new(false, message, false);

    public static CaseInvestigationResult Succeeded(string message, bool sessionChanged) => new(true, message, sessionChanged);
}
