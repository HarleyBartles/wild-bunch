namespace WildBunch.Domain.WantedPosters;

public sealed record ReadWantedPostersResult(bool Success, string Message, bool SessionChanged)
{
    public static ReadWantedPostersResult Failed(string message) => new(false, message, false);

    public static ReadWantedPostersResult Succeeded(string message, bool sessionChanged) => new(true, message, sessionChanged);
}
