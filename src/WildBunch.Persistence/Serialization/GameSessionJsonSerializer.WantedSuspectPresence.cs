using System.Text.Json;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    public string SerializeWantedSuspectPresenceLedger(IReadOnlyList<WantedSuspectPresenceEntry> presenceEntries)
    {
        ArgumentNullException.ThrowIfNull(presenceEntries);
        return JsonSerializer.Serialize(presenceEntries.Select(WantedSuspectPresenceSnapshot.FromDomain).ToArray(), Options);
    }

    internal IReadOnlyList<WantedSuspectPresenceEntry> DeserializeWantedSuspectPresenceLedger(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<WantedSuspectPresenceEntry>();
        }

        try
        {
            var snapshots = Deserialize<WantedSuspectPresenceSnapshot[]>(json);
            return snapshots.Select(snapshot => snapshot.ToDomain()).ToArray();
        }
        catch (InvalidComponentCacheShapeException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or NullReferenceException)
        {
            throw new InvalidComponentCacheShapeException(
                "wantedSuspectPresenceLedger",
                "WantedSuspectPresenceLedger component cache could not be decoded.",
                exception);
        }
    }

    private sealed record WantedSuspectPresenceSnapshot(string SuspectId, WantedSuspectPresenceState State)
    {
        public static WantedSuspectPresenceSnapshot FromDomain(WantedSuspectPresenceEntry entry)
            => new(entry.SuspectId.Value, entry.State);

        public WantedSuspectPresenceEntry ToDomain()
            => new(new SuspectId(SuspectId), State);
    }
}
