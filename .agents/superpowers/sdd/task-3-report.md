# Task 3 Report: Strip town-specific warrants/clues from SeedCaseBuilder

## What I implemented

Stripped setup-time town-specific warrants and clues from `SeedCaseBuilder` so the
case file surfaces only base pools at setup time. Town-specific surfacing is now a
runtime/salt concern (Task 4), not a seed/setup concern.

### `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`
- Removed `CreateTownSpecificPublicClues` method and its call in `CreatePublicClues`.
  `CreatePublicClues` now returns only the 6 base surface-tagged clues (dropped the
  `startingTownId` parameter since it was only used for town-specific gating).
- Removed `CreateTownSpecificPublicWarrants` method and its call.
- Restructured `CreatePublicWarrants` to build all 28 warrants internally:
  - 7 gang member warrants — one per suspect from the roster. The true culprit's
    warrant uses `CaseCharacterRoster.CreateTrueCulpritWarrant`
    (`InvestigationTargetKind.TrueCulprit`); the other six use
    `CreateGangMemberWarrant` (`InvestigationTargetKind.GangMember`). The culprit
    warrant stays in the pool and is gated behind the killer release gate at runtime.
  - 21 unrelated criminal warrants from `CaseCharacterRoster.UnrelatedWantedCriminalPool`.
  - All 28 tagged with `InvestigationSourceKind.SheriffWarrants`.
- Removed `publicWarrant1` / `publicWarrant2` parameters from `BuildCase` and both
  public entry points (`CreateCanonicalCaseFile`, `CreateCaseFile`). Also removed the
  now-unused `startingTownId` parameter from `BuildCase` and `CreateCaseFile` (it was
  only used to gate town-specific warrants/clues, which no longer exist at setup time).

### `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs`
- Removed the `startingTownId` argument from the `SeedCaseBuilder.CreateCaseFile`
  call. `startingTownId` is still computed and used for `setup.StartingTownId`
  (player position), just no longer passed into case-file construction.

### Tests
- `SeededNewGameFactoryTests.cs`: added the 3 spec'd tests
  (`CaseFile_StartsWithOneKnownClueAndZeroKnownWarrants`,
  `CaseFile_PublicWarrants_HasSevenGangPlusTwentyOneUnrelated`,
  `CaseFile_PublicClues_HasSixBaseCluesNoTownSpecificOnes`). Updated
  `CreatesRicherSeedWorldAndCase` (PublicClues 20→6, PublicWarrants 9→28, removed
  the old "Reno Pike" unrelated warrant[1] assertions, flipped the culprit-absent
  assertion to assert the culprit warrant is now present with `TrueCulprit` kind).
  Updated `FrontierDescriptorAddsTownSpecificCivicCluesForTheNextVisitedTown` to
  assert the new fixed setup-time counts (6 clues, 28 warrants) with a note that
  runtime town-specific surfacing is a Task 4 concern.
- `GameSetupResolverTests.cs`: updated `CanonicalTemplateUsesTheExplicitCanonicalPlan`
  (added PublicClues=6 / PublicWarrants=28 count assertions with gang/unrelated
  breakdown, flipped the culprit-absent assertion to assert the culprit warrant is
  present with `TrueCulprit` kind).

## What I tested and test results

- `dotnet build src/WildBunch.GameContent/WildBunch.GameContent.csproj` → Build succeeded (1 pre-existing warning, 0 errors).
- `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj` → **82/82 passing**, output pristine (only pre-existing xUnit2000 warning in an untouched test).
- `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj` → **395/395 passing**.

### TDD Evidence
- **RED**: `dotnet test --filter "FullyQualifiedName~CaseFile_StartsWithOneKnownClue|FullyQualifiedName~CaseFile_PublicWarrants_HasSevenGang|FullyQualifiedName~CaseFile_PublicClues_HasSixBase"` → Failed: 2, Passed: 1 (the KnownClues/KnownWarrants test already passed since those were already correct; PublicWarrants and PublicClues failed as expected because town-specific additions were still present and the 2-base-warrant structure was unchanged).
- **GREEN**: same filter after implementation → Failed: 0, Passed: 3.

## Files changed
- `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`
- `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs`
- `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`
- `tests/WildBunch.GameContent.Tests/GameSetupResolverTests.cs`

## Self-review findings
- Completeness: all spec steps (1-9) done. KnownClues=1, PublicClues=6, KnownWarrants=0, PublicWarrants=28 (7 gang + 21 unrelated), all warrants tagged `SheriffWarrants`. Culprit warrant in pool with `TrueCulprit` kind.
- Quality: removed ~163 lines of town-specific setup code; no dead params left; `world` still used in `BuildCase` for turf assignments and base clue #4.
- Discipline: only touched the 4 in-scope files; did not restructure unrelated code.

## Issues / concerns
- **Pre-existing failures (NOT caused by Task 3):** `WildBunch.Application.Tests` has 4 failing tests (`PurchaseUnknownOfferFailsWithoutSaveOrMutation`, `GetTownStoreOffersReturnsEmptyCatalogWhenTownHasNoStoreServices`, `ReturnsStartingTownCandidatesWithSuppliesOrNoticeBoard`, `ReturnsKnownCanonicalTowns`). These test town services/stores via `StartingTownCatalog` / `TownServices` and are unrelated to case-file warrants/clues. I confirmed via `git stash` that at the committed Task 2 state (f5fad5d) these 4 tests pass; they fail due to other uncommitted worktree changes in the BUNCH-107 batch (e.g. `StartingTownCatalog.cs`, `TownStoreCatalogModels.cs`, `WorldModels.cs`, and the test files themselves — one test even has a visible bug using `TownServices.None` instead of `Supplies`/`NoticeBoard`). My changes only touch case-file construction and do not affect town services/stores. These failures are owned by whichever task introduced those town-service changes, not Task 3.
- The worktree has a large batch of uncommitted changes from other BUNCH-107 tasks. I committed only my 4 in-scope files to keep the Task 3 commit focused.
