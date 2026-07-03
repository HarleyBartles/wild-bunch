## Task 2: Expand unrelated criminal pool from 6 to 21

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/CaseCharacterRoster.cs:290-370`
- Test: `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs` (new or existing)

**Interfaces:**
- Consumes: nothing new
- Produces: `CaseCharacterRoster.UnrelatedWantedCriminalPool` returns 21 entries (up from 6)

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void UnrelatedWantedCriminalPool_HasAtLeast21Entries()
{
    Assert.True(CaseCharacterRoster.UnrelatedWantedCriminalPool.Count >= 21);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~UnrelatedWantedCriminalPool_HasAtLeast21Entries"`
Expected: FAIL (count is 6)

- [ ] **Step 3: Add 15 new unrelated criminal entries**

Add 15 new `Wanted(...)` entries to the `UnrelatedWantedCriminals` array in `CaseCharacterRoster.cs`, following the same pattern as the existing 6 (fictional economy warrants, varied dispositions, bounties, aliases, features, issuing sources). Names should be distinct Western-flavored fictional names.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~UnrelatedWantedCriminalPool_HasAtLeast21Entries"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: expand unrelated criminal pool from 6 to 21 for parity/respawn coverage"
```

---

