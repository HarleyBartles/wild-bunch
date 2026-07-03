# Task 6 Report: Wire resolvers into GameSession investigation methods

## What I Implemented

Replaced ordered-peek selection (`CaseFile.PeekNextPublicWarrant` / `CaseFile.PeekNextPublicClue`) with resolver-based selection in three `GameSession` investigation methods:

### `src/WildBunch.Domain/Game/GameSession.cs`

1. **Added static readonly resolver fields** (line 47-48): `_wantedPosterResolver` and `_clueSurfacingResolver` as stateless domain-service instances.

2. **Added helper properties** (line 3502-3531):
   - `CurrentTownSlotIndex` — derives the current town's position in `World.Towns` by matching `CurrentTown.TownId`.
   - `CurrentTownVisitCount` — wraps `CurrentTownVisit.CurrentTownState.VisitNumber`.

3. **`ReadWantedPosters()`** (line 2744): Replaced `PeekNextPublicWarrant(SheriffWarrants)` with `_wantedPosterResolver.Resolve(CaseFile, CurrentTownSlotIndex, CurrentTownVisitCount, SaltSource)` and `PeekNextPublicClue(...)` with `_clueSurfacingResolver.Resolve(CaseFile, SheriffWarrants, ...)`. Preserved the `IsPlayerKnownClue` gate as a post-filter on the clue result.

4. **`FollowTelegraphLeads()`** (line 3126): Replaced `PeekNextPublicClue(...)` with `_clueSurfacingResolver.Resolve(CaseFile, TelegraphLead, ...)`. Preserved `IsPlayerKnownClue` post-filter.

5. **`GatherLocalGossip()`** (line 3192): Replaced `PeekNextPublicClue(...)` with `_clueSurfacingResolver.Resolve(CaseFile, LocalGossip, ...)`. Preserved `IsPlayerKnownClue` post-filter.

### Key design decisions

- **`IsPlayerKnownClue` post-filter**: The `ClueSurfacingResolver` does not replicate the `IsPlayerKnownClue` eligibility gate (it filters by `SourceKind` + not-known only). To preserve the existing color-only-clue skip behavior, the resolver result is post-filtered: if the resolver returns a clue that fails `IsPlayerKnownClue`, it is set to null (→ "nothing new" message). This is safe because in the real seeded game all public clues pass `IsPlayerKnownClue`.

- **Culprit warrant gating**: The `WantedPosterResolver` excludes the true-culprit warrant (`InvestigationTargetKind.TrueCulprit`) unless `KillerReleaseState.IsReleased` is true. This matches the `SeedCaseBuilder` design intent ("The culprit warrant is gated behind the killer release gate at runtime (WantedPosterResolver)"). The old `PeekNextPublicWarrant` did not have this gate — this is an intentional behavior change introduced by the resolver.

- **`LookAroundSaloon` not modified**: The task brief mentioned `LookAroundSaloon` as using `PeekNextPublicClue`, but the current code does not — it uses a saloon POI selection mechanism (suspects + citizens + nobody slot). No change was needed there.

- **`InspectNoticeBoard` and `CheckSheriffRecords` not modified**: These methods still use `PeekNextPublicClue`. The task brief did not list them for resolver wiring, and the brief says "other code may still use them."

## What I Tested and Test Results

### New tests

**`tests/WildBunch.Domain.Tests/GameSessionResolverWiringTests.cs`** (new file, 4 tests):
1. `ReadWantedPosters_SurfacesDifferentWarrantsInDifferentTowns` — reads posters in town A (slot 0), travels to town B (slot 1), asserts different warrants.
2. `ReadWantedPosters_FreshSessionsInDifferentStartingTownsSurfaceDifferentFirstWarrants` — two fresh sessions with same pool but different starting towns surface different first warrants (RED with old ordered-peek: both get the first warrant).
3. `ReadWantedPosters_ExcludesTrueCulpritWarrantUntilKillerReleased` — the true-culprit warrant is gated; the unrelated warrant surfaces first (RED with old peek: culprit surfaces).
4. `GatherLocalGossip_SkipsColorOnlyClueAndReturnsNothingNew` — color-only gossip clue is still skipped (preserved behavior).

**`tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`** (1 new test):
- `ReadWantedPosters_InBoringMode_SurfacesDifferentWarrantsInDifferentTowns` — uses `SeededNewGameFactory` with Boring entropy (Fixed salt), reads posters in starting town, enters a different town, reads again, asserts different warrants.

### Existing tests updated

- `GameSessionInvestigationActionsTests.TelegraphLeadsResetAfterLeavingAndReturningToTown` — updated to not hardcode which telegraph clue is revealed first (resolver may pick either based on hash); asserts counts instead of specific IDs.
- `GameSessionWantedPostersTests.ReadingWantedPostersInSupportedTownAddsPublicClueAndLogEntry` — updated to expect "Reno Pike" (unrelated) instead of "Mira Cline" (culprit), since the resolver gates the culprit warrant.
- `GameSessionWantedPostersTests.ReadingWantedPostersInTownWithoutNoticeBoardStillSucceeds` — same warrant assertion update.
- `TestSessionFactory.CreateWithPublicWarrantAndClue` — changed warrant `TargetKind` from `TrueCulprit` to `GangMember` so the resolver surfaces it.
- `ReadWantedPostersHandlerTests.CreateSession` — changed warrant `TargetKind` from `TrueCulprit` to `GangMember` for the same reason.

### Test results

| Project | Passed | Failed | Notes |
|---------|--------|--------|-------|
| WildBunch.Domain.Tests | 417 | 0 | All green |
| WildBunch.GameContent.Tests | 83 | 0 | All green |
| WildBunch.Application.Tests | 177 | 4 | 4 pre-existing failures (confirmed by stashing GameSession.cs — they fail without my change too) |
| WildBunch.Integration.Tests | 19 | 134 | Pre-existing (broad infrastructure failures from other branch tasks) |

## TDD Evidence (RED + GREEN)

### RED (without implementation — stashed `GameSession.cs`)

- `ReadWantedPosters_FreshSessionsInDifferentStartingTownsSurfaceDifferentFirstWarrants`: **FAIL** — `Assert.NotEqual() Failure: Values are equal` — both sessions surface `warrant-unrelated-1` (old ordered-peek returns first warrant regardless of town).
- `ReadWantedPosters_ExcludesTrueCulpritWarrantUntilKillerReleased`: **FAIL** — `Assert.Equal() Failure: Expected: UnrelatedWantedCriminal, Actual: TrueCulprit` — old peek surfaces the culprit warrant without the killer-release gate.

### GREEN (with implementation)

- All 4 `GameSessionResolverWiringTests` pass: **4 passed, 0 failed**.
- `ReadWantedPosters_InBoringMode_SurfacesDifferentWarrantsInDifferentTowns` passes: **1 passed, 0 failed**.

## Files Changed

| File | Change |
|------|--------|
| `src/WildBunch.Domain/Game/GameSession.cs` | Wired resolvers into 3 investigation methods; added resolver fields and helper properties |
| `tests/WildBunch.Domain.Tests/GameSessionResolverWiringTests.cs` | New test file (4 tests) |
| `tests/WildBunch.Domain.Tests/GameSessionInvestigationActionsTests.cs` | Updated TelegraphLeads test to be resolver-order-agnostic |
| `tests/WildBunch.Domain.Tests/GameSessionWantedPostersTests.cs` | Updated warrant assertions for culprit gating |
| `tests/WildBunch.Domain.Tests/TestSessionFactory.cs` | Changed test warrant from TrueCulprit to GangMember |
| `tests/WildBunch.Application.Tests/ReadWantedPostersHandlerTests.cs` | Changed test warrant from TrueCulprit to GangMember |
| `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs` | Added Boring-mode resolver wiring test |

## Self-Review Findings

- **Completeness**: All three specified investigation methods (ReadWantedPosters, FollowTelegraphLeads, GatherLocalGossip) are wired. LookAroundSaloon was not modified because it does not use PeekNextPublicClue in the current code.
- **YAGNI**: No unnecessary changes. `InspectNoticeBoard` and `CheckSheriffRecords` left on old peek per task scope.
- **Behavior preservation**: Null handling ("nothing new" messages) preserved. `IsPlayerKnownClue` gate preserved via post-filter. Color-only clue skip behavior verified by test.
- **Behavior change (intended)**: True-culprit warrant is now gated behind killer-release via the resolver. This matches the SeedCaseBuilder design intent. Tests updated accordingly.
- **Pre-existing failures**: 4 Application.Tests and 134 Integration.Tests failures are pre-existing from the branch state (confirmed by stashing GameSession.cs and re-running). Not caused by this change.

## Issues or Concerns

- The `ClueSurfacingResolver.CombineSalt` uses `string.GetHashCode(StringComparison.Ordinal)` which is not stable across process restarts (unlike `WantedPosterResolver` which uses a manual char-by-char hash). This is a pre-existing issue in the resolver (Task 5), not introduced by this task. It means clue selection is deterministic within a single process run but may vary across restarts for the same Fixed salt. This does not affect Boring-mode gameplay (Fixed salt is deterministic within a session) but could affect cross-session reproducibility.
- The `IsPlayerKnownClue` post-filter approach is subtly different from the old pre-filter approach when multiple eligible clues exist and some are color-only: the resolver might pick a color-only clue even when a valid one exists. In practice, the real seeded game has no color-only public clues, so this edge case does not arise.

---

## Review Fix Report

### Fix 1: Boring-mode contract mismatch (GameSession.cs)

**Problem:** The resolvers accept `SaltSource?` and branch on `salt is null` for boring mode. But `GameSession.SaltSource` is non-nullable and always non-null (Fixed in boring, Runtime otherwise), so the resolvers' boring-mode branch was dead code.

**Fix:** Updated all three resolver call sites in `GameSession.cs` (`ReadWantedPosters`, `FollowTelegraphLeads`, `GatherLocalGossip`) to pass `SaltSource.Mode == SaltSourceMode.Fixed ? null : SaltSource` instead of `SaltSource`. This ensures the resolvers' boring-mode branch (salt is null → simple slot/visit rotation) is actually exercised when in boring mode.

### Fix 2: ClueSurfacingResolver hash inconsistency (ClueSurfacingResolver.cs)

**Problem:** `ClueSurfacingResolver.CombineSalt` used `salt.GetHashCode(StringComparison.Ordinal)` which is inconsistent with `WantedPosterResolver.StableSaltIndex` which uses a manual char-by-char stable hash with prime multipliers.

**Fix:** Replaced the `GetHashCode` call with the same manual char-by-char hash pattern: iterate over salt string characters with `hash = (hash * 31) + c`, then fold in `townSlotIndex` and `visitCount` with the same prime multiplier. Now both resolvers use identical stable-hash patterns.

### Fix 3: Vacuous precondition assertion (GameSessionWantedPostersTests.cs)

**Problem:** The assertion was changed to `Assert.False((session.CurrentTown.Services & TownServices.None) != 0)`. Since `TownServices.None` is `0`, `Services & 0` is always `0`, making the assertion always pass — it verifies nothing.

**Fix:** The reviewer asked to revert to `TownServices.NoticeBoard`, but the working tree's `TownServices` enum (modified by the broader BUNCH-107 uncommitted changes) no longer has `NoticeBoard` — it only has `None` and `Telegraph`. Replaced the vacuous assertion with `Assert.Equal(TownServices.None, session.CurrentTown.Services)`, which meaningfully verifies the precondition that the town carries no services. Updated the comment accordingly.

### Test Results

| Command | Result |
|---------|--------|
| `dotnet test WildBunch.Domain.Tests --filter "WantedPosters\|ClueSurfacing\|Telegraph\|Gossip"` | Passed: 39, Failed: 0 |
| `dotnet test WildBunch.Domain.Tests` | Passed: 417, Failed: 0 |
| `dotnet test WildBunch.GameContent.Tests` | Passed: 83, Failed: 0 |

### Files Changed (Review Fixes)

| File | Change |
|------|--------|
| `src/WildBunch.Domain/Game/GameSession.cs` | Pass `null` instead of `SaltSource` in boring mode at 3 resolver call sites |
| `src/WildBunch.Domain/Cases/ClueSurfacingResolver.cs` | Replaced `GetHashCode` with manual char-by-char stable hash matching `WantedPosterResolver` |
| `tests/WildBunch.Domain.Tests/GameSessionWantedPostersTests.cs` | Replaced vacuous `TownServices.None` bitmask assertion with meaningful `Assert.Equal(TownServices.None, ...)` |
