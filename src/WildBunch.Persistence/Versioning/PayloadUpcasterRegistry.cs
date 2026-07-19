using System.Collections.ObjectModel;

namespace WildBunch.Persistence.Versioning;

/// <summary>
/// Registry of payload upcasters, keyed by (PayloadKind, payloadType).
/// Event versions are derived from the count of registered upcasters —
/// no hand-edited version registry. To bump a version, write and register
/// an upcaster. The act of bumping IS the act of writing the upcaster.
/// See the event sourcing integrity policy and ADR-0028.
/// </summary>
internal sealed class PayloadUpcasterRegistry
{
    private readonly Dictionary<(PayloadKind, string), SortedDictionary<int, IPayloadUpcaster>> _upcasters = new();

    public PayloadUpcasterRegistry(IEnumerable<IPayloadUpcaster> upcasters)
    {
        ArgumentNullException.ThrowIfNull(upcasters);

        foreach (var upcaster in upcasters)
        {
            var key = (GetKind(upcaster), upcaster.PayloadType);
            if (!_upcasters.TryGetValue(key, out var chain))
            {
                chain = new SortedDictionary<int, IPayloadUpcaster>();
                _upcasters[key] = chain;
            }

            if (chain.ContainsKey(upcaster.FromVersion))
            {
                throw new InvalidOperationException(
                    $"Duplicate upcaster for {key.Item1} '{key.Item2}' at FromVersion={upcaster.FromVersion}.");
            }

            chain[upcaster.FromVersion] = upcaster;
        }

        // Validate contiguous chains for event upcasters.
        foreach (var ((kind, payloadType), chain) in _upcasters)
        {
            if (kind != PayloadKind.Event)
                continue;

            ValidateContiguousChain(payloadType, chain);
        }
    }

    /// <summary>
    /// Returns the current version for the given payload type.
    /// Derived from the count of registered upcasters: no upcasters -> v1;
    /// N upcasters -> v(N+1). There is no other API to declare a version.
    /// </summary>
    public int CurrentVersion(string payloadType)
    {
        var key = (PayloadKind.Event, payloadType);
        return _upcasters.TryGetValue(key, out var chain)
            ? chain.Keys.Max() + 1   // highest FromVersion + 1
            : 1;                      // no upcasters -> still at v1
    }

    /// <summary>
    /// Upcasts a persisted payload from storedVersion to currentVersion.
    /// Fails closed if storedVersion > current (code is older than data)
    /// or if the chain is non-contiguous (missing upcaster for a transition).
    /// </summary>
    public string Upcast(string payloadType, int storedVersion, string payloadJson)
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
        if (!_upcasters.TryGetValue((PayloadKind.Event, payloadType), out var chain))
        {
            throw new InvalidOperationException(
                $"{payloadType} stored at v{storedVersion} but no upcasters registered.");
        }

        // Run chain from storedVersion to current.
        var version = storedVersion;
        var json = payloadJson;
        while (version < current)
        {
            if (!chain.TryGetValue(version, out var upcaster))
            {
                throw new InvalidOperationException(
                    $"No {payloadType} upcaster for v{version} -> v{version + 1}.");
            }
            json = upcaster.Upcast(json);
            version++;
        }

        return json;
    }

    private static PayloadKind GetKind(IPayloadUpcaster upcaster)
        => upcaster is IEventUpcaster ? PayloadKind.Event : PayloadKind.Projection;

    private static void ValidateContiguousChain(string payloadType, SortedDictionary<int, IPayloadUpcaster> chain)
    {
        // A contiguous chain starts at v1 and goes to v(chain.Count).
        // If the first upcaster is not at FromVersion=1, the chain is non-contiguous.
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
