namespace WildBunch.Domain.Cases;

public sealed record CaseInvestigationResult(bool Success, string Message, bool SessionChanged, string? BeatNarration = null)
{
    public static CaseInvestigationResult Failed(string message) => new(false, message, false, null);

    public static CaseInvestigationResult Succeeded(string message, bool sessionChanged, string? beatNarration = null)
        => new(true, message, sessionChanged, beatNarration);
}
