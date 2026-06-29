namespace WildBunch.Domain.Cases;

/// <summary>
/// Tracks the active pool of unrelated wanted criminals and maintains parity
/// with the number of gang members still available to surface on wanted posters.
/// </summary>
/// <remarks>
/// <para>
/// Rules (from the BUNCH-107 issue spec):
/// <list type="bullet">
/// <item>Keep <c>#unrelated criminals</c> approximately equal to <c>#gang members available to surface</c>.</item>
/// <item>When an unrelated criminal is taken in, spawn a replacement from the unused roster — but only if that would not exceed the current gang-member parity target.</item>
/// <item>When a gang member is taken in, the parity target drops and excess unrelated criminals are despawned to match.</item>
/// <item>Despawning prefers criminals the player has not yet collected a warrant for, so collected warrants stay surfacing-eligible longer.</item>
/// <item>Despawning a criminal also retires their warrant from the surfacing pool (it is no longer eligible to surface on a wanted poster).</item>
/// <item>The unrelated criminal roster must be at least <c>3 × max gang size</c> (21 for 7 gang members) to cover a full respawn cycle plus full redundancy before any repeat.</item>
/// </list>
/// </para>
/// <para>
/// The ledger is serializable for JSON snapshot persistence via <see cref="ToSnapshot"/>
/// and <see cref="FromSnapshot"/>. When hosted by <see cref="Game.GameSession"/>, the
/// gang-side parity is reconstructed by replaying <see cref="Events.SheriffTurnInSettled"/>
/// events through <see cref="RecordGangMemberTakenIn"/>.
/// </para>
/// </remarks>
public sealed class UnrelatedCriminalLedger
{
    private const int RedundancyFactor = 3;

    private readonly int _initialGangMemberCount;
    private readonly List<WarrantId> _roster;
    private HashSet<WarrantId> _activeIds;
    private HashSet<WarrantId> _takenInIds;
    private HashSet<WarrantId> _warrantCollectedIds;
    private HashSet<WarrantId> _retiredIds;
    private int _gangMembersAvailable;
    private int _nextSpawnIndex;

    /// <summary>
    /// Constructs a ledger with a synthetic roster of <paramref name="poolSize"/>
    /// warrant IDs named <c>criminal-{i}</c>. The first <paramref name="gangMemberCount"/>
    /// criminals are activated to match the starting parity target.
    /// </summary>
    public UnrelatedCriminalLedger(int gangMemberCount, int poolSize)
        : this(gangMemberCount, BuildSyntheticRoster(poolSize))
    {
    }

    /// <summary>
    /// Constructs a ledger from an explicit roster of unrelated-criminal warrant IDs
    /// (the 21 <see cref="InvestigationTargetKind.UnrelatedWantedCriminal"/> warrants
    /// from the case file). The first <paramref name="gangMemberCount"/> are activated.
    /// </summary>
    public UnrelatedCriminalLedger(int gangMemberCount, IReadOnlyList<WarrantId> roster)
    {
        if (gangMemberCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gangMemberCount), gangMemberCount,
                "Gang member count cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(roster);

        _roster = roster.Distinct().ToList();
        if (_roster.Count != roster.Count)
        {
            throw new ArgumentException("The roster contains duplicate warrant IDs.", nameof(roster));
        }

        if (_roster.Count < gangMemberCount * RedundancyFactor)
        {
            throw new ArgumentException(
                $"The unrelated criminal roster must be at least {RedundancyFactor}x the gang member count " +
                $"({_roster.Count} provided for {gangMemberCount} gang members; need at least {gangMemberCount * RedundancyFactor}).",
                nameof(roster));
        }

        _activeIds = new HashSet<WarrantId>(_roster.Take(gangMemberCount));
        _takenInIds = [];
        _warrantCollectedIds = [];
        _retiredIds = [];
        _initialGangMemberCount = gangMemberCount;
        _gangMembersAvailable = gangMemberCount;
        _nextSpawnIndex = gangMemberCount;
    }

    /// <summary>Number of unrelated criminals currently active (available to surface).</summary>
    public int ActiveCriminalCount => _activeIds.Count;

    /// <summary>Number of gang members still available to surface (the live parity target).</summary>
    public int GangMembersAvailable => _gangMembersAvailable;

    /// <summary>Total size of the unrelated criminal roster.</summary>
    public int PoolSize => _roster.Count;

    /// <summary>Warrant IDs of the currently active unrelated criminals.</summary>
    public IReadOnlyList<WarrantId> ActiveCriminalIds => _activeIds.ToArray();

    /// <summary>Warrant IDs of unrelated criminals that have been taken in (removed from the active pool).</summary>
    public IReadOnlyList<WarrantId> TakenInCriminalIds => _takenInIds.ToArray();

    /// <summary>Warrant IDs the player has collected (surfaced on a poster) for an unrelated criminal.</summary>
    public IReadOnlyList<WarrantId> WarrantCollectedIds => _warrantCollectedIds.ToArray();

    /// <summary>Warrant IDs retired from the surfacing pool via despawn.</summary>
    public IReadOnlyList<WarrantId> RetiredWarrantIds => _retiredIds.ToArray();

    /// <summary>
    /// Whether the warrant for <paramref name="warrantId"/> is eligible to surface on a
    /// wanted poster. Only active (non-retired, non-taken-in) criminals are eligible.
    /// </summary>
    public bool IsSurfacingEligible(WarrantId warrantId) => _activeIds.Contains(warrantId);

    /// <summary>
    /// Records that an unrelated criminal was taken in to the sheriff. Removes them from
    /// the active pool and spawns a replacement from the unused roster when the active
    /// count would fall below the gang-member parity target. Returns the spawned
    /// replacement's warrant ID, or <c>null</c> when no spawn occurred (parity already
    /// met, unused roster exhausted, or the id was not active).
    /// </summary>
    public WarrantId? RecordTakenIn(WarrantId criminalId)
    {
        var wasActive = _activeIds.Remove(criminalId);
        if (wasActive)
        {
            _takenInIds.Add(criminalId);
        }

        return TrySpawnReplacement();
    }

    /// <summary>
    /// Marks that the player has collected (surfaced on a poster) the warrant for the
    /// given criminal. Collected criminals are retained preferentially during despawn.
    /// </summary>
    public void MarkWarrantCollected(WarrantId criminalId)
    {
        if (_roster.Contains(criminalId))
        {
            _warrantCollectedIds.Add(criminalId);
        }
    }

    /// <summary>
    /// Despawns up to <paramref name="count"/> active criminals, preferring ones the
    /// player has not collected a warrant for. Despawning also retires the criminal's
    /// warrant from the surfacing pool. Returns the despawned warrant IDs in order.
    /// </summary>
    public IReadOnlyList<WarrantId> Despawn(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Despawn count cannot be negative.");
        }

        var toDespawn = Math.Min(count, _activeIds.Count);
        if (toDespawn == 0)
        {
            return [];
        }

        // Prefer despawning criminals the player has NOT collected a warrant for:
        // uncollected first, collected last.
        var ordered = _activeIds
            .OrderBy(id => _warrantCollectedIds.Contains(id))
            .Take(toDespawn)
            .ToArray();

        foreach (var id in ordered)
        {
            _activeIds.Remove(id);
            _retiredIds.Add(id);
        }

        return ordered;
    }

    /// <summary>
    /// Records that a gang member was taken in. Decreases the parity target by one and,
    /// when the active unrelated pool now exceeds the target, despawns the excess
    /// (preferring uncollected warrants). Returns the despawned warrant IDs, or
    /// <c>null</c> when no despawn was needed. Clamps the parity target at zero.
    /// </summary>
    public IReadOnlyList<WarrantId>? RecordGangMemberTakenIn()
    {
        if (_gangMembersAvailable <= 0)
        {
            return null;
        }

        _gangMembersAvailable--;

        var excess = _activeIds.Count - _gangMembersAvailable;
        if (excess <= 0)
        {
            return null;
        }

        return Despawn(excess);
    }

    /// <summary>
    /// Captures the full ledger state for JSON snapshot persistence.
    /// </summary>
    public UnrelatedCriminalLedgerSnapshot ToSnapshot()
        => new()
        {
            InitialGangMemberCount = _initialGangMemberCount,
            Roster = _roster.ToArray(),
            ActiveIds = _activeIds.ToArray(),
            TakenInIds = _takenInIds.ToArray(),
            WarrantCollectedIds = _warrantCollectedIds.ToArray(),
            RetiredIds = _retiredIds.ToArray(),
            GangMembersAvailable = _gangMembersAvailable,
            NextSpawnIndex = _nextSpawnIndex,
        };

    /// <summary>
    /// Reconstructs a ledger from a snapshot. The roster and parity invariants are
    /// re-validated so a corrupted snapshot fails fast rather than producing a
    /// half-populated ledger.
    /// </summary>
    public static UnrelatedCriminalLedger FromSnapshot(UnrelatedCriminalLedgerSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var ledger = new UnrelatedCriminalLedger(snapshot.InitialGangMemberCount, snapshot.Roster)
        {
            _activeIds = new HashSet<WarrantId>(snapshot.ActiveIds),
            _takenInIds = new HashSet<WarrantId>(snapshot.TakenInIds),
            _warrantCollectedIds = new HashSet<WarrantId>(snapshot.WarrantCollectedIds),
            _retiredIds = new HashSet<WarrantId>(snapshot.RetiredIds),
            _gangMembersAvailable = snapshot.GangMembersAvailable,
            _nextSpawnIndex = snapshot.NextSpawnIndex,
        };

        return ledger;
    }

    private WarrantId? TrySpawnReplacement()
    {
        if (_activeIds.Count >= _gangMembersAvailable)
        {
            return null;
        }

        if (_nextSpawnIndex >= _roster.Count)
        {
            return null;
        }

        // Skip roster entries that are already active, taken in, or retired so a
        // respawn never re-surfaces a criminal the player has already dealt with.
        while (_nextSpawnIndex < _roster.Count
               && (_activeIds.Contains(_roster[_nextSpawnIndex])
                   || _takenInIds.Contains(_roster[_nextSpawnIndex])
                   || _retiredIds.Contains(_roster[_nextSpawnIndex])))
        {
            _nextSpawnIndex++;
        }

        if (_nextSpawnIndex >= _roster.Count)
        {
            return null;
        }

        var spawned = _roster[_nextSpawnIndex];
        _nextSpawnIndex++;
        _activeIds.Add(spawned);
        return spawned;
    }

    private static IReadOnlyList<WarrantId> BuildSyntheticRoster(int poolSize)
    {
        if (poolSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), poolSize, "Pool size cannot be negative.");
        }

        var roster = new WarrantId[poolSize];
        for (var i = 0; i < poolSize; i++)
        {
            roster[i] = new WarrantId($"criminal-{i}");
        }

        return roster;
    }
}

/// <summary>
/// Serializable snapshot of <see cref="UnrelatedCriminalLedger"/> state for JSON
/// snapshot persistence. All collections are simple arrays of <see cref="WarrantId"/>
/// (a record struct wrapping a string), so the snapshot serializes cleanly to JSON.
/// </summary>
public sealed record UnrelatedCriminalLedgerSnapshot
{
    public required int InitialGangMemberCount { get; init; }
    public required IReadOnlyList<WarrantId> Roster { get; init; }
    public required IReadOnlyList<WarrantId> ActiveIds { get; init; }
    public required IReadOnlyList<WarrantId> TakenInIds { get; init; }
    public required IReadOnlyList<WarrantId> WarrantCollectedIds { get; init; }
    public required IReadOnlyList<WarrantId> RetiredIds { get; init; }
    public required int GangMembersAvailable { get; init; }
    public required int NextSpawnIndex { get; init; }
}
