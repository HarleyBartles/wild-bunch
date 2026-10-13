# Row 17 N-18 Seeded Startup Test Behavior Plan

> **For agentic workers:** Use the repository's inline `executing-plans` lane for this bounded slice.

**Goal:** Remove self-confirming and duplicate tests around generated startup setup while retaining meaningful non-first-town and invalid-town behavior proof.

**Architecture:** Test-only GameContent changes. The existing production start contract and session events stay unchanged; tests stop treating `CanonicalStartFlow`'s test-only first-town choice as an independently established product default. Existing behavior evidence is consolidated at its owning consumer.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.GameContent.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), seeded factory and seed-world factory findings; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md).

**Execution Strategy:** `executing-plans`, inline and sequential. The test and evidence edits share one GameContent behavior owner and require one focused validation and branch review.

## Review Focus

- The retained non-first-town test must fail if the selected town is ignored or replaced by the first town.
- Removing first-town assertions must not remove proof of production startup behavior; those assertions only mirrored a test helper and an internal setup projection, while the player-facing start flow chooses a town after the prologue.
- Every remaining seeded-startup assertion must be owned by a test that reaches the relevant output; retain the invalid-town rejection and town selection consumer scenarios.
- Current salt behavior remains owned by `TravelEntropyVarianceTests`; do not lose it when removing duplicate factory-level salt assertions.
- No production code, gameplay behavior, event schema, ADR, or feature promise changes.

## Global Constraints

- Start from `develop` at `5715a9f3911892aad017885554a40f40570dc548` and deliver one PR to `develop`.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.136`; it remains the sole authored application version.
- Retire N-17 only because its catalog scope shipped in PR #320 and its retained consumer behaviors have current owners.
- Keep the real non-first-town selection and invalid-town rejection tests; do not add tests that assert only helper wiring or source shape.
- Run focused and complete GameContent tests plus `py -3 tools/run.py ci --check`; obtain fresh whole-branch review and passing exact-head and develop-push hosted gates before merging.
- After integration, fast-forward the primary checkout, then remove this worktree and its exact local/upstream branch refs together. Preserve the separate AWS learning worktree and branch.

### Task 1: Record PR #320 and install the successor plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n17-travel-seed-fixture-behavior.md`
- Add: `.agents/plans/2026-10-13-row17-n18-seeded-startup-behavior.md`

**Interfaces:**
- Consumes: PR #320 reviewed source `bb4291dde8b1426465eea4aa265d5b988d726e5f`, squash merge `5715a9f3911892aad017885554a40f40570dc548`, exact-head hosted gate `38362763623`, develop-push gate `38363469040`, completed travel catalog behavior, and verified paired worktree/local/upstream cleanup.
- Produces: row 17's current pointer to N-18, version `0.1.0-dev.136`, retirement of N-17, and dated delivery evidence for the completed travel-catalog finding. The current test investigation remains unchanged until N-18 implementation is complete.

- [x] **Step 1: Record N-17 delivery and retire its plan.** Add PR #320's source, merge, hosted gates, and verified paired cleanup to row 17. Point the row to this N-18 plan, advance the version once to `.136`, and delete the N-17 plan because all its acceptance criteria shipped. Keep the AWS learning worktree explicitly outside this cleanup.
- [x] **Step 2: Commit the plan installation.** Keep the test investigation and dispositions unchanged until implementation completes. Commit the roadmap, version, N-18 plan and N-17 retirement as the first substantive commit, before test edits.

### Task 2: Keep startup behavior tests at their real owners

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldFactoryTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/GameSetupResolverTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Review unchanged: `tests/WildBunch.GameContent.Tests/CanonicalStartFlow.cs`
- Review unchanged: `tests/WildBunch.GameContent.Tests/TravelEntropyVarianceTests.cs`

**Interfaces:**
- Consumes: `StartingTownPolicy.ResolveStartingTown`, `SeededNewGameFactory`, the test-only `CanonicalStartFlow`, and `TravelEntropyVarianceTests` as the existing owner of production entropy salt posture.
- Produces: startup tests whose assertions name independently reached behavior, with no duplicated or helper-derived first-town default claim.

- [x] **Step 1: Remove assertions that only echo the test helper.** Delete `StartingTownPolicyDefaultsToFirstTown` (membership is not proof of the default) and `StartingTownPolicyAcceptsAnyValidTownChoice` (it selects the first town); retain `StartingTownPolicyRejectsInvalidTownChoice` as the negative policy boundary. Remove the first-town equality assertion from `CreatesRicherSeedWorldAndCase`, and remove `CreateWithNullStartingTownIdUsesSafeDefault`, because `CanonicalStartFlow` itself chooses `world.Towns.First()` when no choice is supplied. Remove the parallel first-town assertion from `GameSetupResolverTests.CanonicalTemplateUsesTheExplicitCanonicalPlan`; it compares the resolver with a second call to the same `MapGenerator` and does not prove the player's post-prologue choice.
- [x] **Step 2: Preserve independent startup behavior and consolidate duplicates.** Keep and rename the test to `StartGameCanSelectANonFirstTown`; it chooses a valid town different from the test helper's first-town choice and asserts the session ends there. Fold `Assert.Empty(session.CaseFile.KnownWarrants)` into `CreatesRicherSeedWorldAndCase`, which already proves the initial known clue, and delete `CaseFile_StartsWithOneKnownClueAndZeroKnownWarrants`. Remove `FrontierDescriptorKeepsTheRetainedStartingClueSources`: its town-name-pool membership assertion is unrelated to its title and the sheriff-poster fact is already independently asserted by richer setup tests.
- [x] **Step 3: Remove stale and duplicate setup claims.** Rename `AllDifficultiesGetTransitionalHorseAndSaddleDefaults` to state that it covers Easy and Brutal only; remove obsolete BUNCH-107/BUNCH-94 roadmap comments. Remove `DefaultAdventureRandomnessStaysRuntimeSaltedAndBoringModeCanOptIntoDeterminism`, because `TravelEntropyVarianceTests` already proves Fixed/Runtime mode, runtime variation, and same seed-owned world facts through real sessions. Do not change production setup or travel behavior.
- [x] **Step 4: Validate the focused behavior owner.** Run `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~SeededNewGameFactoryTests|FullyQualifiedName~SeedWorldFactoryTests|FullyQualifiedName~GameSetupResolverTests|FullyQualifiedName~TravelEntropyVarianceTests"`. Confirm `StartGameCanSelectANonFirstTown` would fail if `CanonicalStartFlow` ignored the requested town, then restore the helper and rerun the focused tests. No new test is required for this removal/consolidation slice.
- [x] **Step 5: Record current dispositions.** Update the test-quality investigation and .NET test disposition rows to say exactly which assertions were removed, which independent behaviors remain covered, and which graph/map findings remain open. Avoid claiming a product default-town rule from the test helper.

### Task 3: Verify, review, publish and close out

**Files:**
- Review: the complete branch diff and staged-tree result.
- Modify: the active plan checkboxes as each step is completed.

- [x] **Step 1: Run the complete GameContent suite and canonical gate.** Run `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj`, then `py -3 tools/run.py ci --check`. Repair any failure at its owning source, inspect and stage intended changes, and commit without bypassing the hook.
- [x] **Step 2: Obtain fresh independent whole-branch review.** Review the branch against this plan, the test-quality investigation, and repository review runbook. Correct all actionable findings and rerun affected checks; record no development receipt files in the repository.
- [ ] **Step 3: Publish and merge.** Push this branch, open one PR to `develop`, verify its diff and development version, obtain passing exact-head hosted CI, mark ready and merge only after review/gates are clean. Record PR, source and merge SHAs, gate run IDs, and cleanup evidence in row 17.
- [ ] **Step 4: Complete successor cleanup.** Fast-forward the primary checkout to the merge and verify develop-push CI passes. Confirm the current worktree is clean and merged, remove it, delete its exact local and upstream branch refs, then verify worktree and local-ref absence and query the live upstream to verify remote-ref absence. Preserve unrelated worktrees and branches.
