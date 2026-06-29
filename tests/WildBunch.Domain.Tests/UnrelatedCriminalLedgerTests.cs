using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests;

public sealed class UnrelatedCriminalLedgerTests
{
    private static WarrantId Id(string value) => new(value);

    [Fact]
    public void Constructor_StartsActivePoolAtGangParity()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);

        Assert.Equal(7, ledger.ActiveCriminalCount);
        Assert.Equal(7, ledger.GangMembersAvailable);
        Assert.Equal(21, ledger.PoolSize);
    }

    [Fact]
    public void Constructor_ThrowsWhenRosterSmallerThanThreeTimesGangSize()
    {
        // 3x redundancy rule: 7 gang members require at least 21 unrelated criminals.
        Assert.Throws<ArgumentException>(() => new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 20));
    }

    [Fact]
    public void Constructor_AcceptsExactlyThreeTimesGangSize()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);

        Assert.Equal(7, ledger.ActiveCriminalCount);
        Assert.Equal(21, ledger.PoolSize);
    }

    [Fact]
    public void Constructor_ThrowsOnNegativeGangMemberCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnrelatedCriminalLedger(gangMemberCount: -1, poolSize: 0));
    }

    [Fact]
    public void RealRosterConstructor_UsesProvidedWarrantIdsAndActivatesFirstGangCount()
    {
        var roster = Enumerable.Range(0, 21).Select(i => Id($"unrelated-{i}")).ToArray();

        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, roster: roster);

        Assert.Equal(7, ledger.ActiveCriminalCount);
        Assert.All(ledger.ActiveCriminalIds, id => Assert.Contains(id, roster.Take(7)));
    }

    [Fact]
    public void RealRosterConstructor_ThrowsOnDuplicateRosterIds()
    {
        var roster = Enumerable.Range(0, 21).Select(_ => Id("dup")).ToArray();

        Assert.Throws<ArgumentException>(() => new UnrelatedCriminalLedger(gangMemberCount: 7, roster: roster));
    }

    [Fact]
    public void TakingInCriminal_SpawnsReplacement_WhenBelowGangParity()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
        Assert.Equal(7, ledger.ActiveCriminalCount);

        var spawned = ledger.RecordTakenIn(Id("criminal-0"));

        // Replacement spawned from the unused roster -> back at parity.
        Assert.Equal(7, ledger.ActiveCriminalCount);
        Assert.NotNull(spawned);
        Assert.DoesNotContain(Id("criminal-0"), ledger.ActiveCriminalIds);
        Assert.Contains(spawned!.Value, ledger.ActiveCriminalIds);
    }

    [Fact]
    public void TakingInCriminal_RepeatedlySpawnsUntilUnusedPoolExhausted()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);

        // 7 initial active + 14 spawnable from the unused roster = 21 total.
        // Each take-in removes one active and spawns one replacement, keeping
        // the active pool at 7 until the unused roster is drained.
        for (var i = 0; i < 14; i++)
        {
            var activeId = ledger.ActiveCriminalIds.First();
            ledger.RecordTakenIn(activeId);
            Assert.Equal(7, ledger.ActiveCriminalCount);
        }

        // Unused roster is now exhausted. The next take-in must NOT spawn.
        var lastActive = ledger.ActiveCriminalIds.First();
        var spawned = ledger.RecordTakenIn(lastActive);

        Assert.Equal(6, ledger.ActiveCriminalCount);
        Assert.Null(spawned);
    }

    [Fact]
    public void TakingInCriminal_OfNonActiveId_IsTolerantNoOpAndDoesNotSpawn()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);

        // "criminal-99" is not in the active pool.
        var spawned = ledger.RecordTakenIn(Id("criminal-99"));

        Assert.Equal(7, ledger.ActiveCriminalCount);
        Assert.Null(spawned);
    }

    [Fact]
    public void TakingInCriminal_RecordsTakenInId()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);

        ledger.RecordTakenIn(Id("criminal-0"));

        Assert.Contains(Id("criminal-0"), ledger.TakenInCriminalIds);
    }

    [Fact]
    public void MarkWarrantCollected_TracksCollection()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);

        ledger.MarkWarrantCollected(Id("criminal-0"));

        Assert.Contains(Id("criminal-0"), ledger.WarrantCollectedIds);
    }

    [Fact]
    public void Despawn_PrefersCriminalsPlayerHasNotCollectedWarrantFor()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
        // Mark two active criminals as warrant-collected so they are retained.
        ledger.MarkWarrantCollected(Id("criminal-0"));
        ledger.MarkWarrantCollected(Id("criminal-1"));

        var despawned = ledger.Despawn(count: 2);

        Assert.Equal(2, despawned.Count);
        Assert.DoesNotContain(Id("criminal-0"), despawned);
        Assert.DoesNotContain(Id("criminal-1"), despawned);
        // Collected criminals remain active.
        Assert.Contains(Id("criminal-0"), ledger.ActiveCriminalIds);
        Assert.Contains(Id("criminal-1"), ledger.ActiveCriminalIds);
    }

    [Fact]
    public void Despawn_CanStillDespawnCollectedCriminalsWhenNoUncollectedRemain()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
        // Collect warrants for all active criminals.
        foreach (var id in ledger.ActiveCriminalIds)
        {
            ledger.MarkWarrantCollected(id);
        }

        var despawned = ledger.Despawn(count: 2);

        Assert.Equal(2, despawned.Count);
        Assert.Equal(5, ledger.ActiveCriminalCount);
    }

    [Fact]
    public void Despawn_RetiresWarrantFromSurfacingPool()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
        var target = ledger.ActiveCriminalIds.First();
        Assert.True(ledger.IsSurfacingEligible(target));

        var despawned = ledger.Despawn(count: 1);

        var despawnedId = Assert.Single(despawned);
        Assert.Equal(target, despawnedId);
        Assert.False(ledger.IsSurfacingEligible(target));
        Assert.Contains(target, ledger.RetiredWarrantIds);
    }

    [Fact]
    public void Despawn_ClampsCountToActivePoolSize()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);

        var despawned = ledger.Despawn(count: 99);

        Assert.Equal(7, despawned.Count);
        Assert.Equal(0, ledger.ActiveCriminalCount);
    }

    [Fact]
    public void Despawn_WithNegativeCount_Throws()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);

        Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Despawn(count: -1));
    }

    [Fact]
    public void RecordGangMemberTakenIn_DecreasesParityAndDespawnsExcess()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
        Assert.Equal(7, ledger.ActiveCriminalCount);

        // One gang member taken in -> parity target drops to 6, one excess despawned.
        var despawned = ledger.RecordGangMemberTakenIn();

        Assert.Equal(6, ledger.GangMembersAvailable);
        Assert.Equal(6, ledger.ActiveCriminalCount);
        Assert.NotNull(despawned);
        Assert.Single(despawned!);
    }

    [Fact]
    public void RecordGangMemberTakenIn_DoesNotDespawnWhenAlreadyBelowParity()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
        // Drop active below parity by despawning manually.
        ledger.Despawn(count: 3);
        Assert.Equal(4, ledger.ActiveCriminalCount);

        var despawned = ledger.RecordGangMemberTakenIn();

        Assert.Equal(6, ledger.GangMembersAvailable);
        // Active (4) was already below the new parity (6); no despawn needed.
        Assert.Null(despawned);
        Assert.Equal(4, ledger.ActiveCriminalCount);
    }

    [Fact]
    public void RecordGangMemberTakenIn_StopsAtZeroGangAvailable()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 2, poolSize: 6);

        ledger.RecordGangMemberTakenIn();
        ledger.RecordGangMemberTakenIn();
        Assert.Equal(0, ledger.GangMembersAvailable);
        Assert.Equal(0, ledger.ActiveCriminalCount);

        // Beyond zero is a no-op (clamped).
        var despawned = ledger.RecordGangMemberTakenIn();
        Assert.Equal(0, ledger.GangMembersAvailable);
        Assert.Null(despawned);
    }

    [Fact]
    public void IsSurfacingEligible_TrueForActiveAndFalseForTakenInOrRetired()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
        var activeId = ledger.ActiveCriminalIds.First();
        var unusedId = Id("criminal-20"); // in roster but not active

        Assert.True(ledger.IsSurfacingEligible(activeId));
        Assert.False(ledger.IsSurfacingEligible(unusedId));

        ledger.RecordTakenIn(activeId);
        // The taken-in id is removed from active; a replacement is spawned instead.
        Assert.False(ledger.IsSurfacingEligible(activeId));
    }

    [Fact]
    public void SnapshotRoundTrip_PreservesAllState()
    {
        var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
        ledger.MarkWarrantCollected(Id("criminal-0"));
        ledger.RecordTakenIn(Id("criminal-1"));
        ledger.RecordGangMemberTakenIn();
        ledger.Despawn(count: 1);

        var snapshot = ledger.ToSnapshot();
        var restored = UnrelatedCriminalLedger.FromSnapshot(snapshot);

        Assert.Equal(ledger.ActiveCriminalCount, restored.ActiveCriminalCount);
        Assert.Equal(ledger.GangMembersAvailable, restored.GangMembersAvailable);
        Assert.Equal(ledger.PoolSize, restored.PoolSize);
        Assert.Equal(ledger.ActiveCriminalIds, restored.ActiveCriminalIds);
        Assert.Equal(ledger.TakenInCriminalIds, restored.TakenInCriminalIds);
        Assert.Equal(ledger.WarrantCollectedIds, restored.WarrantCollectedIds);
        Assert.Equal(ledger.RetiredWarrantIds, restored.RetiredWarrantIds);
    }

    [Fact]
    public void FromSnapshot_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => UnrelatedCriminalLedger.FromSnapshot(null!));
    }

}
