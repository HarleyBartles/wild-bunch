using WildBunch.Domain.Events;

namespace WildBunch.Application.Projections;

/// <summary>
/// Reference projector for the full audit projection.
/// Derives the complete event log from typed domain events.
/// This is a pure function over the event stream — no aggregate mutation.
/// See ADR-0028.
/// </summary>
public sealed class FullAuditProjector : IDomainEventProjector<FullAuditProjection>
{
    public FullAuditProjection Project(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var entries = new List<AuditEntry>();
        var sequence = 0;
        foreach (var e in events)
        {
            sequence++;
            entries.Add(new AuditEntry(
                sequence,
                e.GetType().Name,
                Summarize(e),
                DateTime.UtcNow));
        }

        return new FullAuditProjection(Guid.Empty, entries);
    }

    private static string Summarize(IDomainEvent e) => e switch
    {
        GameStarted gs => $"Game started: {gs.PlayerName} in {gs.StartingTownName} ({gs.Difficulty}).",
        StoreItemPurchased sp => $"Purchased {sp.Quantity}x {sp.DisplayName} for {sp.TotalPrice:C} (wallet: {sp.WalletAfter:C}).",
        DevSaloonOverrideForced forced => forced.ForcedSuspectId is null
            ? $"Forced saloon override: {forced.ForcedKind}."
            : $"Forced saloon override: {forced.ForcedKind} for suspect {forced.ForcedSuspectId}.",
        DevSaloonOverrideCleared => "Cleared pending saloon override.",
        DevSaloonOverrideConsumed => "Consumed pending saloon override during saloon look-around.",
        _ => e.GetType().Name
    };
}
