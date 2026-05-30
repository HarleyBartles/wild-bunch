namespace WildBunch.Domain.Travel;

internal static class TravelWarningFilter
{
    public static IReadOnlyList<string> Filter(IReadOnlyList<string> warnings, bool mountedTravelAvailable)
        => mountedTravelAvailable
            ? warnings
            : warnings.Where(warning => !warning.Contains("horse", StringComparison.OrdinalIgnoreCase)).ToArray();
}
