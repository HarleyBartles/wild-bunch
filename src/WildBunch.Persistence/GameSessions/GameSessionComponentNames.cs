namespace WildBunch.Persistence.GameSessions;

internal static class GameSessionComponentNames
{
    internal const string Player = "player";
    internal const string World = "world";
    internal const string CaseFile = "caseFile";
    internal const string Clock = "clock";
    internal const string PursuitState = "pursuitState";
    internal const string Setup = "setup";
    internal const string TravelRandomness = "travelRandomness";
    internal const string TownVisitState = "townVisitState";
    internal const string Journey = "journey";
    internal const string CompletedJourneyHistory = "completedJourneyHistory";
}

internal static class GameSessionComponentPayloads
{
    internal static string GetRequiredPayload(IReadOnlyDictionary<string, GameSessionComponentEntity> components, string componentName)
        => components.TryGetValue(componentName, out var component)
            ? component.PayloadJson
            : throw new InvalidOperationException($"Missing required game session component '{componentName}'.");

    internal static string? GetOptionalPayload(IReadOnlyDictionary<string, GameSessionComponentEntity> components, string componentName)
        => components.TryGetValue(componentName, out var component)
            ? component.PayloadJson
            : null;
}
