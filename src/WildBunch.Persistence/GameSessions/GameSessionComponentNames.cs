namespace WildBunch.Persistence.GameSessions;

/// <summary>
/// Component names for game session persistence components stored in the
/// GameSessionComponents table. Each component is a JSONB payload keyed by
/// session ID and component name. The table is generic and can accommodate
/// new components without schema migration (just add a new constant here).
/// </summary>
internal static class GameSessionComponentNames
{
    internal const string Player = "player";
    internal const string World = "world";
    internal const string CaseFile = "caseFile";
    internal const string Clock = "clock";
    internal const string PursuitState = "pursuitState";
    internal const string Setup = "setup";
    internal const string SaltSource = "saltSource";
    internal const string TownVisitState = "townVisitState";
    internal const string Journey = "journey";
    internal const string CompletedJourneyHistory = "completedJourneyHistory";
    internal const string WantedSuspectPresenceLedger = "wantedSuspectPresenceLedger";
    internal const string CurrentActionContext = "currentActionContext";
    internal const string PendingDevTravelOverride = "pendingDevTravelOverride";
    internal const string PendingDevSaloonOverride = "pendingDevSaloonOverride";
    internal const string DevLayoutSalts = "devLayoutSalts";
    /// <summary>
    /// UnrelatedCriminalLedger component (BUNCH-107). Uses the existing
    /// GameSessionComponents table without schema migration — the table
    /// is generic and can accommodate new components. The ledger's ToSnapshot()
    /// and FromSnapshot() methods handle serialization via JSONB.
    /// </summary>
    internal const string UnrelatedCriminalLedger = "unrelatedCriminalLedger";
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
