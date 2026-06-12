using System.Collections.ObjectModel;
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Game;

public enum WantedSuspectPresenceState
{
    Unavailable = 0,
    AvailableInTown = 1,
    GoneToGround = 2,
    SecuredAlive = 3,
    SecuredDead = 4
}

public sealed record WantedSuspectPresenceEntry(SuspectId SuspectId, WantedSuspectPresenceState State);

public sealed class WantedSuspectPresenceLedger
{
    private readonly List<WantedSuspectPresenceEntry> _entries = [];
    private readonly ReadOnlyCollection<WantedSuspectPresenceEntry> _entriesView;

    public WantedSuspectPresenceLedger()
    {
        _entriesView = _entries.AsReadOnly();
    }

    public WantedSuspectPresenceLedger(IEnumerable<WantedSuspectPresenceEntry>? entries)
        : this()
    {
        ReplaceEntries((entries ?? Array.Empty<WantedSuspectPresenceEntry>()).ToArray());
    }

    public IReadOnlyList<WantedSuspectPresenceEntry> Entries => _entriesView;

    public WantedSuspectPresenceState GetState(SuspectId suspectId)
        => TryGetState(suspectId, out var state)
            ? state
            : WantedSuspectPresenceState.Unavailable;

    public bool TryGetState(SuspectId suspectId, out WantedSuspectPresenceState state)
    {
        foreach (var entry in _entries)
        {
            if (entry.SuspectId.Equals(suspectId))
            {
                state = entry.State;
                return true;
            }
        }

        state = WantedSuspectPresenceState.Unavailable;
        return false;
    }

    public void SetState(SuspectId suspectId, WantedSuspectPresenceState state)
    {
        var existingIndex = _entries.FindIndex(entry => entry.SuspectId.Equals(suspectId));

        if (state == WantedSuspectPresenceState.Unavailable)
        {
            if (existingIndex >= 0)
            {
                _entries.RemoveAt(existingIndex);
            }

            return;
        }

        var entry = new WantedSuspectPresenceEntry(suspectId, state);
        if (existingIndex >= 0)
        {
            _entries[existingIndex] = entry;
            return;
        }

        _entries.Add(entry);
    }

    public void ReplaceEntries(IReadOnlyList<WantedSuspectPresenceEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        _entries.Clear();
        foreach (var entry in entries)
        {
            if (entry.State == WantedSuspectPresenceState.Unavailable)
            {
                continue;
            }

            SetState(entry.SuspectId, entry.State);
        }
    }
}
