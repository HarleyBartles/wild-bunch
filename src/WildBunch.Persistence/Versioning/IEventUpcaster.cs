namespace WildBunch.Persistence.Versioning;

/// <summary>
/// Transforms a persisted event payload from one version to the next.
/// The registry validates the complete event chain when it is constructed.
/// See the event sourcing integrity policy.
/// </summary>
internal interface IEventUpcaster
{
    string PayloadType { get; }
    int FromVersion { get; }
    string Upcast(string payloadJson);
}
