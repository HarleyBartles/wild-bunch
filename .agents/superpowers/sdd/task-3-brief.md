## Task 3: Strip town-specific warrants and clues from SeedCaseBuilder

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`
- Test: `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`

**Interfaces:**
- Consumes: `CaseCharacterRoster.UnrelatedWantedCriminalPool` (21 entries after Task 2)
- Produces: `SeedCaseBuilder.CreateCaseFile` / `CreateCanonicalCaseFile` that populate:
  - KnownClues: 1 (prologue)
  - PublicClues: 6 base surface-tagged clues (no town-specific additions)
  - KnownWarrants: 0
  - PublicWarrants: 7 gang warrants + 21 unrelated criminal warrants (28 total)

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void CaseFile_StartsWithOneKnownClueAndZeroKnownWarrants()
{
    var factory = new SeededNewGameFactory();
    var session = factory.Create("Ranger Vale");

    Assert.Single(session.CaseFile.KnownClues);
    Assert.Empty(session.CaseFile.KnownWarrants);
}

[Fact]
public void CaseFile_PublicWarrants_HasSevenGangPlusTwentyOneUnrelated()
{
    var factory = new SeededNewGameFactory();
    var session = factory.Create("Ranger Vale");

    Assert.Equal(28, session.CaseFile.PublicWarrants.Count);
    Assert.Equal(7, session.CaseFile.PublicWarrants.Count(w => w.Terms.TargetKind == InvestigationTargetKind.GangMember || w.Terms.TargetKind == InvestigationTargetKind.TrueCulprit));
    Assert.Equal(21, session.CaseFile.PublicWarrants.Count(w => w.Terms.TargetKind == InvestigationTargetKind.UnrelatedWantedCriminal));
}

[Fact]
public void CaseFile_PublicClues_HasSixBaseCluesNoTownSpecificOnes()
{
    var factory = new SeededNewGameFactory();
    var session = factory.Create("Ranger Vale");

    Assert.Equal(6, session.CaseFile.PublicClues.Count);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~CaseFile_StartsWithOneKnownClue|FullyQualifiedName~CaseFile_PublicWarrants_HasSevenGang|FullyQualifiedName~CaseFile_PublicClues_HasSixBase"`
Expected: FAIL

- [ ] **Step 3: Strip town-specific methods from SeedCaseBuilder**

Remove `CreateTownSpecificPublicClues` and `CreateTownSpecificPublicWarrants` methods entirely. Remove the calls to them in `CreatePublicClues` and `CreatePublicWarrants`.

- [ ] **Step 4: Restructure PublicWarrants to include all 7 gang + 21 unrelated**

Replace `CreatePublicWarrants` to build:
- 7 gang member warrants (one per suspect, including true culprit â€” culprit's warrant has `InvestigationTargetKind.TrueCulprit`)
- 21 unrelated criminal warrants from `CaseCharacterRoster.UnrelatedWantedCriminalPool`
- All tagged with `InvestigationSourceKind.SheriffWarrants`

Remove the `publicWarrant1` / `publicWarrant2` parameters from `BuildCase` and the public entry points.

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~CaseFile_StartsWithOneKnownClue|FullyQualifiedName~CaseFile_PublicWarrants_HasSevenGang|FullyQualifiedName~CaseFile_PublicClues_HasSixBase"`
Expected: PASS

- [ ] **Step 6: Fix SeededNewGameFactoryTests assertions**

Update `CreatesRicherSeedWorldAndCase`:
- `PublicClues.Count` = 6 (not 20)
- `PublicWarrants.Count` = 28 (not 9)
- Remove assertions on specific town-specific warrant names/clue descriptions that no longer exist
- Keep assertions on gang roster, culprit, opening lead, known clues

- [ ] **Step 7: Fix GameSetupResolverTests assertions**

Update `CanonicalTemplateUsesTheExplicitCanonicalPlan`:
- `PublicClues.Count` = 6
- `PublicWarrants.Count` = 28
- Remove assertions on specific public warrant names that were the old 2 base warrants

- [ ] **Step 8: Run all GameContent.Tests**

Run: `dotnet test --filter "FullyQualifiedName~WildBunch.GameContent.Tests"`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "refactor: strip town-specific warrants/clues, restructure case file pools"
```

---

