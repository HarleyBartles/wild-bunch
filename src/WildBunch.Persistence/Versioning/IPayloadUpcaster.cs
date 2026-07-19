namespace WildBunch.Persistence.Versioning;

/// <summary>
/// The kind of persisted payload. Events have upcasters; projections have rebuild.
/// The enum exists for version-check uniformity in the registry, but only Event
/// has upcaster chains. See the event sourcing integrity policy.
/// </summary>
internal enum PayloadKind
{
    Event,
    Projection
}

/// <summary>
/// Transforms a persisted payload from one version to the next.
/// Upcasters are registered in the PayloadUpcasterRegistry. The chain from
/// v1 to currentVersion is validated at startup. See the event sourcing
/// integrity policy.
/// </summary>
public interface IPayloadUpcaster
{
    string PayloadType { get; }
    int FromVersion { get; }      // transforms FromVersion -> FromVersion + 1
    string Upcast(string payloadJson);
}

/// <summary>
/// Marker interface for event upcasters. Used for DI filtering and
/// build-time completeness tests. See the event sourcing integrity policy.
/// </summary>
public interface IEventUpcaster : IPayloadUpcaster { }
