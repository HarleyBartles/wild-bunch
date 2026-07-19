namespace WildBunch.Persistence.Versioning;

/// <summary>
/// Hand-edited current-version constants for projection types.
/// Projections don't get upcasters — when the stored version doesn't match
/// current, the projection is dropped and rebuilt from the event stream.
/// Bumping a projection version is a code change: update the constant, and
/// the rebuild logic triggers on next load. See the event sourcing integrity
/// policy and ADR-0028.
///
/// Why projections use a hand-edited version while events don't: events have
/// upcasters, so event versions are derived from upcaster count (no
/// hand-edited registry). Projections don't have upcasters (they're rebuilt,
/// not upcasted), so there's no equivalent failure mode to prevent by
/// derivation. A hand-edited constant that doesn't match reality causes a
/// rebuild on every load (wasteful but correct) or no rebuild when one was
/// needed (caught by the projection rebuild parity test). The failure modes
/// are different, so the enforcement mechanisms differ.
/// </summary>
internal static class ProjectionVersions
{
    /// <summary>
    /// Current version for all component projections. All components start
    /// at v1. When a component's JSON shape changes, bump this to 2 and the
    /// PersistedPayloadLoader will rebuild that component from the event
    /// stream on next load.
    /// </summary>
    private const int ComponentVersion = 1;

    /// <summary>
    /// Current version for diary day projections. Starts at v1. When the
    /// TravelDiaryDayState shape changes, bump this to 2 and the
    /// PersistedPayloadLoader will rebuild all diary days via
    /// TravelDiaryDayProjector on next load.
    /// </summary>
    public const int DiaryDay = 1;

    /// <summary>
    /// Returns the current version for the named component projection.
    /// All components share the same version today — if individual components
    /// need independent versioning in the future, switch to a per-component
    /// dictionary.
    /// </summary>
    public static int ForComponent(string componentName) => ComponentVersion;
}
