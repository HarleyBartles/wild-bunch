using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Domain.Game;

/// <summary>
/// Child domain component inside the GameSession boundary that owns bounty-loop
/// state and behavior. Receives narrow context records, returns results plus
/// events-to-produce. Does NOT reference GameSession, produce events directly,
/// enter action context, adjust cash, or mutate CaseFile/TownVisitState/Player.
/// See BUNCH-112 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class BountyLoop
{
    private readonly WantedSuspectPresenceLedger _presenceLedger;
    private UnrelatedCriminalLedger _unrelatedCriminalLedger;
    private DevSaloonOverride? _pendingDevSaloonOverride;

    internal BountyLoop(
        IReadOnlyList<WantedSuspectPresenceEntry>? presenceEntries,
        UnrelatedCriminalLedger unrelatedCriminalLedger)
    {
        _presenceLedger = new WantedSuspectPresenceLedger(presenceEntries);
        _unrelatedCriminalLedger = unrelatedCriminalLedger
            ?? throw new ArgumentNullException(nameof(unrelatedCriminalLedger));
    }

    internal IReadOnlyList<WantedSuspectPresenceEntry> PresenceEntries => _presenceLedger.Entries;
    internal UnrelatedCriminalLedger UnrelatedCriminalLedger => _unrelatedCriminalLedger;
    internal DevSaloonOverride? PendingDevSaloonOverride => _pendingDevSaloonOverride;

    internal WantedSuspectPresenceState GetWantedSuspectPresenceState(SuspectId suspectId)
        => _presenceLedger.GetState(suspectId);

    internal bool TryGetWantedSuspectPresenceState(SuspectId suspectId, out WantedSuspectPresenceState state)
        => _presenceLedger.TryGetState(suspectId, out state);

    // Command methods — filled in by Tasks 3–7
    // Apply methods — filled in by Task 8

    internal void RestoreUnrelatedCriminalLedger(UnrelatedCriminalLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        _unrelatedCriminalLedger = ledger;
    }

    internal void RestorePendingDevSaloonOverride(DevSaloonOverride? overrideValue)
    {
        _pendingDevSaloonOverride = overrideValue;
    }
}
