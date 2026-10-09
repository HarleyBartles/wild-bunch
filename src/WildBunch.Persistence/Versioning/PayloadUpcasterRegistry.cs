namespace WildBunch.Persistence.Versioning;

/// <summary>
/// Registry of event upcasters, keyed by payload type.
/// Event versions are derived from the count of registered upcasters —
/// no hand-edited version registry. To bump a version, write and register
/// an upcaster. The act of bumping IS the act of writing the upcaster.
/// See the event sourcing integrity policy and ADR-0028.
/// </summary>
public sealed class PayloadUpcasterRegistry
{
    private readonly Dictionary<string, SortedDictionary<int, IEventUpcaster>> _upcasters = new(StringComparer.Ordinal);

    internal PayloadUpcasterRegistry(IEnumerable<IEventUpcaster> upcasters)
    {
        ArgumentNullException.ThrowIfNull(upcasters);

        foreach (var upcaster in upcasters)
        {
            var key = upcaster.PayloadType;
            if (!_upcasters.TryGetValue(key, out var chain))
            {
                chain = new SortedDictionary<int, IEventUpcaster>();
                _upcasters[key] = chain;
            }

            if (chain.ContainsKey(upcaster.FromVersion))
            {
                throw new InvalidOperationException(
                    $"Duplicate event upcaster for '{key}' at FromVersion={upcaster.FromVersion}.");
            }

            chain[upcaster.FromVersion] = upcaster;
        }

        foreach (var (payloadType, chain) in _upcasters)
        {
            ValidateContiguousChain(payloadType, chain);
        }
    }

    /// <summary>
    /// Returns the current version for the given payload type.
    /// Derived from the count of registered upcasters: no upcasters -> v1;
    /// N upcasters -> v(N+1). There is no other API to declare a version.
    /// </summary>
    internal int CurrentVersion(string payloadType)
    {
        return _upcasters.TryGetValue(payloadType, out var chain)
            ? chain.Keys.Max() + 1   // highest FromVersion + 1
            : 1;                      // no upcasters -> still at v1
    }

    /// <summary>
    /// Upcasts a persisted payload from storedVersion to currentVersion.
    /// Fails closed if storedVersion > current (code is older than data)
    /// or if an event with a non-v1 version has no registered chain.
    /// </summary>
    internal string Upcast(string payloadType, int storedVersion, string payloadJson)
    {
        var current = CurrentVersion(payloadType);

        if (storedVersion > current)
        {
            throw new InvalidOperationException(
                $"{payloadType} stored at v{storedVersion} but current code " +
                $"supports up to v{current}. Code is older than the data.");
        }

        if (storedVersion == current)
        {
            return payloadJson;  // no upcast needed
        }

        // Unknown type with storedVersion != 1: fail closed.
        if (!_upcasters.TryGetValue(payloadType, out var chain))
        {
            throw new InvalidOperationException(
                $"{payloadType} stored at v{storedVersion} but no upcasters registered.");
        }

        // Run chain from storedVersion to current.
        var version = storedVersion;
        var json = payloadJson;
        while (version < current)
        {
            json = chain[version].Upcast(json);
            version++;
        }

        return json;
    }

    private static void ValidateContiguousChain(string payloadType, SortedDictionary<int, IEventUpcaster> chain)
    {
        var expectedFromVersion = 1;
        foreach (var (fromVersion, _) in chain)
        {
            if (fromVersion != expectedFromVersion)
            {
                throw new InvalidOperationException(
                    $"Non-contiguous upcaster chain for event '{payloadType}': " +
                    $"expected FromVersion={expectedFromVersion}, found FromVersion={fromVersion}.");
            }
            expectedFromVersion++;
        }
    }
}
