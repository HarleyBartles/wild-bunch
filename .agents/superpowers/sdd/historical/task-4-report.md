# Task 4 Report: Build WantedPosterResolver (boring + salt mode)

## What I implemented

Created `WantedPosterResolver` — a stateless domain service in `WildBunch.Domain.Cases` that selects which warrant from `CaseFile.PublicWarrants` surfaces on a wanted poster in a given town on a given visit.

**File:** `src/WildBunch.Domain/Cases/WantedPosterResolver.cs`

Selection rules implemented:
- **Boring mode** (salt is null): `warrants[(townSlotIndex + visitCount) % eligibleCount]` with a safe negative-modulo guard.
- **Salt mode** (salt is non-null): a stable manual hash of `salt + townSlotIndex + visitCount` reduced modulo the eligible count. The hash does NOT use `string.GetHashCode()` (not stable across process restarts); it uses the prime-multiplier char-code pattern established by `CitizenCast.StableHash` and `GameSession.StableSaloonRollHash` in this domain.

Eligible warrants = all `PublicWarrants` not already in `KnownWarrants`, with the culprit warrant (`TargetKind == TrueCulprit`) excluded unless `CaseFile.KillerReleaseState.IsReleased` is true. Returns null when the eligible pool is exhausted. Throws `ArgumentNullException` on null caseFile.

**Deviation from the brief (justified):** The brief's salt-mode used `salt.GetHashCode()`, which is not stable across process restarts. The codebase explicitly warns against `string.GetHashCode()` (see `CitizenCast.cs:70`, `GameSession.cs:2990`). I used the established `StableHash` char-code pattern instead. This is a correctness improvement required by codebase conventions; behavior is still deterministic per the tests.

## What I tested and test results

**File:** `tests/WildBunch.Domain.Tests/WantedPosterResolverTests.cs` (10 tests)

Tests cover:
1. `BoringMode_SameTownSameVisit_ReturnsSameWarrant` — determinism
2. `BoringMode_DifferentTowns_ReturnDifferentWarrants` — town differentiation
3. `BoringMode_CulpritWarrantNotSurfacesUntilReleased` — culprit exclusion across all town/visit combos
4. `BoringMode_AfterKillerReleased_CulpritWarrantCanSurface` — culprit becomes eligible after release
5. `BoringMode_AlreadyKnownWarrantsAreSkipped` — known-warrant skipping via `RevealWarrant`
6. `BoringMode_ReturnsNullWhenPoolExhausted` — null on exhausted pool
7. `SaltMode_SameInputsSameSalt_ReturnsSameWarrant` — salt determinism
8. `SaltMode_DifferentSalt_ReturnsDifferentWarrant` — salt differentiation
9. `SaltMode_CulpritWarrantNotSurfacesUntilReleased` — culprit exclusion in salt mode
10. `Resolve_NullCaseFile_Throws` — null-arg guard

The test fixture `BuildTestCaseFile` builds a `CaseFile` with 8 public warrants: one `TrueCulprit` warrant + seven `UnrelatedWantedCriminal` warrants, with `killerReleaseThreshold: 2` and `killerReleaseProgress` set to 0 or 2 to control the released state. Uses `SaltSource.CreateFixed(string)` for salt-mode tests (no custom test double needed).

**Results:** `Passed: 10, Failed: 0, Skipped: 0` — focused filter.

## TDD Evidence (RED + GREEN)

- **RED:** Wrote the test file first. Ran `dotnet test --filter "FullyQualifiedName~WantedPosterResolverTests"` and got `error CS0246: The type or namespace name 'WantedPosterResolver' could not be found` — tests failed to compile because the type did not exist.
- **GREEN:** Implemented `WantedPosterResolver.cs`, rebuilt, and re-ran the same filter: `Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10`.

## Files changed

- `src/WildBunch.Domain/Cases/WantedPosterResolver.cs` (new, 70 lines)
- `tests/WildBunch.Domain.Tests/WantedPosterResolverTests.cs` (new, 10 tests)
- `src/WildBunch.Domain/Cases/INDEX.md` (regenerated — adds `WantedPosterResolver.cs` and sibling task's `ClueSurfacingResolver.cs`)
- `tests/WildBunch.Domain.Tests/INDEX.md` (regenerated — adds `WantedPosterResolverTests.cs` and sibling task's `ClueSurfacingResolverTests.cs`)

## Self-review findings

- **Completeness:** Both modes, culprit exclusion, known-warrant skipping, pool-exhaustion null, null-arg guard all implemented and tested.
- **Quality:** Stable hash (not `GetHashCode`), safe negative-modulo handling for both modes, XML docs, follows existing `StableHash` pattern. Cleaned up a redundant modulo in `StableSaltIndex` before final commit.
- **Discipline (YAGNI):** No extra features, no optional parameters, no speculative extension points. Stateless as specified.
- **Testing:** Real behavior against a real `CaseFile` fixture — no mocks. Tests assert observable warrant identity and target-kind behavior, including exhaustive town/visit sweeps for culprit exclusion.
- **Index mesh:** Regenerated `INDEX.md` for the two folders I touched (self-healing — also picked up a sibling task's unindexed files).

## Issues or concerns

- **Pre-existing failure (NOT mine):** The full `WildBunch.Domain.Tests` suite has 2 failing tests in `ClueSurfacingResolverTests` (`SaltMode_IsDeterministicForSameInputs`, `SaltMode_DifferentSaltCanSelectDifferentClue`) with `System.IndexOutOfRangeException` in `ClueSurfacingResolver.Resolve`. That resolver is in `src/WildBunch.Domain/Cases/ClueSurfacingResolver.cs`, a sibling task's in-progress modified file (`M` in git status, not touched by me). My `WantedPosterResolver` is a new untracked file and does not affect that code. The failure is a bug in the sibling task's salt-mode modulo handling (it does not guard against negative/out-of-range indices the way mine does).
- **Worktree note:** While working, the worktree filesystem produced `.other` merge-artifact copies of my new files (a background sync moved my files to `*.other`). I recovered them and cleaned up the `.other` artifacts before committing. Final committed tree contains exactly the 4 intended files.
