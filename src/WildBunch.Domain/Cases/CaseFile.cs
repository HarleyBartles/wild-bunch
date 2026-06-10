using System.Collections.ObjectModel;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Domain.Cases;

public sealed class CaseFile
{
    private readonly List<Suspect> _suspects;
    private readonly ReadOnlyCollection<Suspect> _suspectsView;
    private readonly List<SuspectId> _discoveredSuspectIds = [];
    private readonly ReadOnlyCollection<SuspectId> _discoveredSuspectIdsView;
    private readonly List<Clue> _knownClues = [];
    private readonly ReadOnlyCollection<Clue> _knownCluesView;
    private readonly List<Clue> _publicClues = [];
    private readonly ReadOnlyCollection<Clue> _publicCluesView;
    private readonly List<Warrant> _knownWarrants = [];
    private readonly ReadOnlyCollection<Warrant> _knownWarrantsView;
    private readonly List<Warrant> _publicWarrants = [];
    private readonly ReadOnlyCollection<Warrant> _publicWarrantsView;
    private readonly List<WantedSuspectConfrontationState> _wantedSuspectConfrontations = [];
    private readonly ReadOnlyCollection<WantedSuspectConfrontationState> _wantedSuspectConfrontationsView;
    private readonly List<SuspectTurfAssignment> _suspectTurfAssignments = [];
    private readonly ReadOnlyCollection<SuspectTurfAssignment> _suspectTurfAssignmentsView;
    private int _killerReleaseProgress;

    public CaseFile(
        SuspectId? accusation,
        IEnumerable<Suspect> suspects,
        SuspectId trueCulpritId,
        IEnumerable<Clue> knownClues,
        IEnumerable<SuspectId>? discoveredSuspectIds = null,
        IEnumerable<Clue>? publicClues = null,
        IEnumerable<SuspectTurfAssignment>? suspectTurfAssignments = null,
        IEnumerable<WantedSuspectConfrontationState>? wantedSuspectConfrontations = null)
        : this(
            accusation,
            suspects,
            trueCulpritId,
            CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues,
            discoveredSuspectIds,
            publicClues,
            suspectTurfAssignments: suspectTurfAssignments,
            wantedSuspectConfrontations: wantedSuspectConfrontations)
    {
    }

    public CaseFile(
        SuspectId? accusation,
        IEnumerable<Suspect> suspects,
        SuspectId trueCulpritId,
        CaseOpeningLead openingLead,
        IEnumerable<Clue> knownClues,
        IEnumerable<SuspectId>? discoveredSuspectIds = null,
        IEnumerable<Clue>? publicClues = null,
        int killerReleaseThreshold = 2,
        int killerReleaseProgress = 0,
        IEnumerable<Warrant>? knownWarrants = null,
        IEnumerable<Warrant>? publicWarrants = null,
        IEnumerable<SuspectTurfAssignment>? suspectTurfAssignments = null,
        IEnumerable<WantedSuspectConfrontationState>? wantedSuspectConfrontations = null)
    {
        ArgumentNullException.ThrowIfNull(suspects);
        ArgumentNullException.ThrowIfNull(knownClues);
        ArgumentNullException.ThrowIfNull(openingLead);

        Accusation = accusation;
        _suspects = suspects.ToList();
        _suspectsView = _suspects.AsReadOnly();
        TrueCulpritId = trueCulpritId;
        OpeningLead = openingLead;
        KillerReleaseThreshold = Math.Max(1, killerReleaseThreshold);
        _killerReleaseProgress = Math.Max(0, killerReleaseProgress);

        _discoveredSuspectIds.AddRange((discoveredSuspectIds ?? Array.Empty<SuspectId>()).DistinctBy(suspectId => suspectId.Value));
        _discoveredSuspectIdsView = _discoveredSuspectIds.AsReadOnly();

        _knownClues.AddRange(knownClues.DistinctBy(clue => clue.Id));
        _knownCluesView = _knownClues.AsReadOnly();

        _publicClues.AddRange((publicClues ?? Array.Empty<Clue>()).DistinctBy(clue => clue.Id));
        _publicCluesView = _publicClues.AsReadOnly();

        _knownWarrants.AddRange((knownWarrants ?? Array.Empty<Warrant>()).DistinctBy(warrant => warrant.Id));
        _knownWarrantsView = _knownWarrants.AsReadOnly();

        _publicWarrants.AddRange((publicWarrants ?? Array.Empty<Warrant>()).DistinctBy(warrant => warrant.Id));
        _publicWarrantsView = _publicWarrants.AsReadOnly();

        _wantedSuspectConfrontations.AddRange((wantedSuspectConfrontations ?? Array.Empty<WantedSuspectConfrontationState>()).DistinctBy(state => state.SuspectId));
        foreach (var confrontation in _wantedSuspectConfrontations)
        {
            if (!_suspects.Any(suspect => suspect.Id.Equals(confrontation.SuspectId)))
            {
                throw new ArgumentException("The confrontation state does not belong to this case.", nameof(wantedSuspectConfrontations));
            }
        }

        _wantedSuspectConfrontationsView = _wantedSuspectConfrontations.AsReadOnly();

        _suspectTurfAssignments.AddRange((suspectTurfAssignments ?? Array.Empty<SuspectTurfAssignment>()).DistinctBy(assignment => assignment.SuspectId));
        foreach (var assignment in _suspectTurfAssignments)
        {
            if (!_suspects.Any(suspect => suspect.Id.Equals(assignment.SuspectId)))
            {
                throw new ArgumentException("The turf assignment does not belong to this case.", nameof(suspectTurfAssignments));
            }
        }

        _suspectTurfAssignmentsView = _suspectTurfAssignments.AsReadOnly();
    }

    public SuspectId? Accusation { get; private set; }

    public IReadOnlyList<Suspect> Suspects => _suspectsView;

    public IReadOnlyList<Suspect> GangRoster => _suspectsView;

    public IReadOnlyList<SuspectId> DiscoveredSuspectIds => _discoveredSuspectIdsView;

    public SuspectId TrueCulpritId { get; }

    public CaseOpeningLead OpeningLead { get; }

    public KillerReleaseState KillerReleaseState => new(_killerReleaseProgress, KillerReleaseThreshold);

    public int KillerReleaseThreshold { get; }

    public int KillerReleaseProgress => _killerReleaseProgress;

    public IReadOnlyList<Clue> KnownClues => _knownCluesView;

    public IReadOnlyList<Clue> PublicClues => _publicCluesView;

    public IReadOnlyList<Warrant> KnownWarrants => _knownWarrantsView;

    public IReadOnlyList<Warrant> PublicWarrants => _publicWarrantsView;

    public IReadOnlyList<WantedSuspectConfrontationState> WantedSuspectConfrontations => _wantedSuspectConfrontationsView;

    public IReadOnlyList<SuspectTurfAssignment> SuspectTurfAssignments => _suspectTurfAssignmentsView;

    public IReadOnlyList<Suspect> GetDiscoveredSuspects()
        => _suspects.Where(suspect => _discoveredSuspectIds.Any(discovered => discovered.Equals(suspect.Id))).ToArray();

    public bool IsSuspectDiscovered(SuspectId suspectId)
        => _discoveredSuspectIds.Any(discovered => discovered.Equals(suspectId));

    public bool DiscoverSuspect(SuspectId suspectId)
    {
        if (!_suspects.Any(suspect => suspect.Id.Equals(suspectId)))
        {
            throw new ArgumentException("The suspect does not belong to this case.", nameof(suspectId));
        }

        if (IsSuspectDiscovered(suspectId))
        {
            return false;
        }

        _discoveredSuspectIds.Add(suspectId);
        return true;
    }

    public void SetAccusation(SuspectId suspectId)
    {
        Accusation = suspectId;
    }

    public void AddClue(Clue clue)
        => DiscoverClue(clue);

    public bool DiscoverClue(Clue clue, bool advanceKillerReleaseProgress = false)
    {
        ArgumentNullException.ThrowIfNull(clue);

        if (_knownClues.Any(existing => existing.Id.Equals(clue.Id)))
        {
            return false;
        }

        _knownClues.Add(clue);
        DiscoverSuspectsFromClue(clue);

        if (advanceKillerReleaseProgress)
        {
            AdvanceKillerReleaseProgress();
        }

        return true;
    }

    public void AddWarrant(Warrant warrant)
        => DiscoverWarrant(warrant);

    public bool DiscoverWarrant(Warrant warrant)
    {
        ArgumentNullException.ThrowIfNull(warrant);

        if (_knownWarrants.Any(existing => existing.Id.Equals(warrant.Id)))
        {
            return false;
        }

        _knownWarrants.Add(warrant);
        return true;
    }

    public bool TryGetWantedSuspectConfrontationState(SuspectId suspectId, out WantedSuspectConfrontationState confrontationState)
    {
        foreach (var state in _wantedSuspectConfrontations)
        {
            if (state.SuspectId.Equals(suspectId))
            {
                confrontationState = state;
                return true;
            }
        }

        confrontationState = default!;
        return false;
    }

    public void RecordWantedSuspectConfrontationState(WantedSuspectConfrontationState confrontationState)
    {
        ArgumentNullException.ThrowIfNull(confrontationState);

        for (var i = 0; i < _wantedSuspectConfrontations.Count; i++)
        {
            if (_wantedSuspectConfrontations[i].SuspectId.Equals(confrontationState.SuspectId))
            {
                _wantedSuspectConfrontations[i] = confrontationState;
                return;
            }
        }

        _wantedSuspectConfrontations.Add(confrontationState);
    }

    public Clue? RevealNextPublicClue(InvestigationSourceKind? sourceKind = null, bool advanceKillerReleaseProgress = false)
    {
        for (var i = 0; i < _publicClues.Count; i++)
        {
            var clue = _publicClues[i];

            if (sourceKind.HasValue && clue.SourceKind != sourceKind)
            {
                continue;
            }

            if (_knownClues.Any(existing => existing.Id.Equals(clue.Id)))
            {
                _publicClues.RemoveAt(i);
                i--;
                continue;
            }

            _publicClues.RemoveAt(i);
            if (DiscoverClue(clue, advanceKillerReleaseProgress))
            {
                return clue;
            }
        }

        return null;
    }

    public Clue? RevealNextPublicClue(Func<Clue, bool> canReveal, bool advanceKillerReleaseProgress = false)
    {
        ArgumentNullException.ThrowIfNull(canReveal);

        for (var i = 0; i < _publicClues.Count; i++)
        {
            var clue = _publicClues[i];

            if (!canReveal(clue))
            {
                continue;
            }

            if (_knownClues.Any(existing => existing.Id.Equals(clue.Id)))
            {
                _publicClues.RemoveAt(i);
                i--;
                continue;
            }

            _publicClues.RemoveAt(i);
            if (DiscoverClue(clue, advanceKillerReleaseProgress))
            {
                return clue;
            }
        }

        return null;
    }

    public Warrant? RevealNextPublicWarrant(InvestigationSourceKind? sourceKind = null)
    {
        for (var i = 0; i < _publicWarrants.Count; i++)
        {
            var warrant = _publicWarrants[i];

            if (sourceKind.HasValue && warrant.Terms.SourceKind != sourceKind)
            {
                continue;
            }

            if (_knownWarrants.Any(existing => existing.Id.Equals(warrant.Id)))
            {
                _publicWarrants.RemoveAt(i);
                i--;
                continue;
            }

            _publicWarrants.RemoveAt(i);
            if (DiscoverWarrant(warrant))
            {
                return warrant;
            }
        }

        return null;
    }

    public bool TryGetSuspectTurf(SuspectId suspectId, out TownId turfTownId)
    {
        foreach (var assignment in _suspectTurfAssignments)
        {
            if (assignment.SuspectId.Equals(suspectId))
            {
                turfTownId = assignment.TurfTownId;
                return true;
            }
        }

        turfTownId = default;
        return false;
    }

    private void AdvanceKillerReleaseProgress()
    {
        _killerReleaseProgress = Math.Min(KillerReleaseThreshold, _killerReleaseProgress + 1);
    }

    private void DiscoverSuspectsFromClue(Clue clue)
    {
        foreach (var suspectId in clue.LinkedSuspectIds)
        {
            if (_suspects.Any(suspect => suspect.Id.Equals(suspectId)))
            {
                DiscoverSuspect(suspectId);
            }
        }
    }
}
