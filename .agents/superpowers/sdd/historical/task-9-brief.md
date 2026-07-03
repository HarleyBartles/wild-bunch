## Task 9: Unrelated criminal parity system (runtime)

**Files:**
- Create: `src/WildBunch.Domain/Cases/UnrelatedCriminalLedger.cs` (or similar)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (wire in when criminal taken in)
- Test: `tests/WildBunch.Domain.Tests/UnrelatedCriminalLedgerTests.cs`

**Interfaces:**
- Consumes: `CaseFile.PublicWarrants` (unrelated criminal warrants), `CaseFile.KnownWarrants`
- Produces: parity tracking â€” when a criminal is taken in, spawn replacement if gang count allows; despawn to maintain parity

This is the most complex runtime piece. The parity system tracks:
- Active unrelated criminals (pool for surfacing)
- Taken-in criminals (removed from pool)
- Spawn/despawn rules

- [ ] **Step 1: Write failing tests for parity rules**

```csharp
[Fact]
public void TakingInCriminal_SpawnsReplacement_WhenBelowGangParity()
{
    var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
    var active = ledger.GetActiveCriminalCount();
    Assert.Equal(7, active); // starts at parity

    ledger.RecordTakenIn(criminalId);
    active = ledger.GetActiveCriminalCount();
    Assert.Equal(7, active); // replacement spawned
}

[Fact]
public void TakingInCriminal_DoesNotSpawn_WhenAtGangParity()
{
    var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
    // Take in all 7, each time a replacement spawns
    for (int i = 0; i < 7; i++) ledger.RecordTakenIn($"criminal-{i}");
    Assert.Equal(7, ledger.GetActiveCriminalCount());

    // Take in one more â€” no replacement since we'd exceed parity
    ledger.RecordTakenIn("criminal-extra");
    Assert.Equal(6, ledger.GetActiveCriminalCount());
}

[Fact]
public void DespawnPrefersCriminalsPlayerHasNotCollectedWarrantFor()
{
    var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
    // Mark some as warrant-collected
    ledger.MarkWarrantCollected("criminal-0");
    ledger.MarkWarrantCollected("criminal-1");

    // Despawn to reduce by 2
    var despawned = ledger.Despawn(count: 2);
    // Should despawn uncollected ones first
    Assert.DoesNotContain("criminal-0", despawned);
    Assert.DoesNotContain("criminal-1", despawned);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~UnrelatedCriminalLedgerTests"`
Expected: FAIL (type not found)

- [ ] **Step 3: Implement UnrelatedCriminalLedger**

Implement the parity tracking, spawn, and despawn logic per the issue spec.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~UnrelatedCriminalLedgerTests"`
Expected: PASS

- [ ] **Step 5: Wire into GameSession**

Hook the ledger into the criminal turn-in flow in GameSession. When a wanted suspect is turned in, record it in the ledger. The ledger adjusts the active pool, which affects what `WantedPosterResolver` can surface.

- [ ] **Step 6: Run full test suite**

Run: `dotnet test`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add unrelated criminal parity system with spawn/despawn rules"
```

---

