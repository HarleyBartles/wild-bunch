# Row 17 N-19 Trail Graph Behavior Test Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Consolidate duplicate sparse-Boring graph tests so one generated graph proves connectivity and a minimum of two distinct travel neighbors for every expected town slot.

**Architecture:** Test-only change in `TrailGraphGeneratorTests`; production generation, map output, events, persistence and player behavior stay unchanged. Reuse the test's existing adjacency helper, which initializes every expected town slot before adding generated edges, so isolated slots cannot disappear from the minimum-degree assertion.

**Tech Stack:** C#/.NET 10, xUnit, WildBunch.GameContent.Tests.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), graph minimum-degree finding; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), `TrailGraphGeneratorTests.cs`.

**Execution Strategy:** `executing-plans`, inline and sequential. The change consolidates two tests that generate the same graph, proves the minimum-degree assertion detects a missing town with a temporary generator mutation, and updates the same finding's disposition. These steps share one behavioral owner and do not benefit from independent implementation handoffs; a fresh whole-branch review provides the independent check.

## Review Focus

- The minimum-degree check must cover every expected slot, including towns with no incident edges, and use distinct adjacent towns as degree.
- Connectivity and minimum degree must be asserted against the same generated graph so the combined invariant cannot depend on two repeated setup paths.
- No production generator or player-facing behavior change is warranted by this test-only finding.

## Global Constraints

- Start from `develop` at `b62e606ee654b5c6db25992b6f302d24537c0b73` and deliver one PR to `develop`.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.137`; it remains the sole authored application version.
- Retire N-18 because its complete seeded-startup test scope shipped in PR #321 and its delivery and cleanup are verified in row 17.
- Keep the change in the existing GameContent test owner. Do not change generator production code, product claims, ADRs or feature-matrix entries.
- Run the focused graph behavior and complete GameContent suite; let the normal staged-snapshot commit hook run the canonical `ci --check` gate. Obtain fresh whole-branch review and passing exact-head and develop-push hosted gates before merging.
- After integration, fast-forward the primary checkout and remove this clean worktree together with its exact local and upstream branch refs. Preserve the separate AWS learning worktree and branch.

---

### Task 1: Retire N-18 and install the graph successor

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n18-seeded-startup-behavior.md`
- Add: `.agents/plans/2026-10-13-row17-n19-graph-minimum-degree.md`

**Interfaces:**
- Consumes: PR #321 reviewed source `9194591001bc8c552f7e909a8f32f52e903588ef`, squash merge `b62e606ee654b5c6db25992b6f302d24537c0b73`, identical source/merge tree `09cc08f602c24de6ec7387ac0cf7871100eaa7f2`, exact-head hosted gate `38368106024`, develop-push gate `38368805343`, approved whole-branch review, and verified N-18 worktree/ref cleanup.
- Produces: row 17's active N-19 graph-test plan, development version `0.1.0-dev.137`, N-18 retirement, and dated N-18 delivery evidence.

- [x] **Step 1: Record N-18 and select this live slice.** Add PR #321's source, merge, review, hosted gates and verified paired cleanup to row 17. Point the row to N-19, advance the version once to `.137`, and state that startup is closed while graph minimum-degree and broader map-generator findings remain open.
- [x] **Step 2: Retire N-18 and commit the successor artifacts.** Delete the completed N-18 plan, add this plan, validate the row and version, then commit the roadmap/version/plan installation before changing tests.

### Task 2: Make the graph invariant test reach every town

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/TrailGraphGeneratorTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Review unchanged: `src/WildBunch.GameContent/NewGame/TrailGraphGenerator.cs`

**Interfaces:**
- Consumes: `TrailGraphGenerator.Generate`, `BuildAdjacency`, and `IsConnected` from `TrailGraphGeneratorTests.cs`.
- Produces: one test that validates connectivity and at least two distinct neighbors for every expected town slot on the same sparse-Boring generated graph, plus a dated disposition of the test finding.

- [x] **Step 1: Confirm the duplicate owner.** Compare the seed, town count, cluster count, placement, deterministic source, entropy and salt in `Generate_ProducesConnectedGraph` and `Generate_SparseBoring_ConnectedWithMinimumDegree2`. They produce the same graph, so keep one generator call and one owner for both invariants; the existing connectivity case already catches an isolated slot at suite level.
- [x] **Step 2: Consolidate the duplicated graph scenario.** Replace `Generate_ProducesConnectedGraph` and `Generate_SparseBoring_ConnectedWithMinimumDegree2` with one test named `Generate_SparseBoring_IsConnectedAndEveryTownHasTwoConnections`. Generate the graph once, build adjacency with `BuildAdjacency(edges, seed.TownCount)`, assert every expected slot has at least two distinct neighbors with the failing town slot in the message, and assert `IsConnected` for the same adjacency. Remove the edge-count assertion because minimum degree two over the complete simple adjacency already implies at least `TownCount` edges.
- [x] **Step 3: Prove the assertion is behavior-sensitive.** Temporarily make the generator return a complete graph over every expected town except the final slot. Run only the consolidated test; it must fail because the final expected slot has degree zero. Restore production source and rerun the focused test; it must pass. This mutation is validation-only and no production change remains.
- [x] **Step 4: Update the finding disposition.** Record that the two old tests generated the same sparse-Boring graph, that the separate connectivity test already guarded isolation at suite level, and that one consolidated behavior test now owns connectedness and minimum degree together. Leave the independent map-generator findings open.
- [x] **Step 5: Commit the test and disposition change.** Run the focused graph scenario and complete GameContent suite, inspect the intended diff, then commit without bypassing the check-only hook. The hook runs the canonical `py -3 tools/run.py ci --check` gate on the staged candidate.

### Task 3: Review, publish and close out

**Files:**
- Review: complete branch diff and staged-tree result.
- Modify: the active plan checkboxes as each step is completed.

- [ ] **Step 1: Obtain fresh independent whole-branch review.** Review the committed branch against this plan, the test-quality investigation and repository review runbook. Correct actionable findings and rerun affected checks through the normal hook; write no development receipt files into the repository.
- [ ] **Step 2: Publish and merge.** Push this branch, open one PR to `develop`, verify the diff and development version, obtain passing exact-head hosted CI, and merge only after review and gates are clean. The successor plan will record this PR's merge and cleanup evidence in row 17.
- [ ] **Step 3: Complete paired cleanup.** Fast-forward the primary checkout to the merge and verify the develop-push gate passes. Confirm this worktree is clean and merged, remove it, delete its exact local and upstream branch refs, and verify the worktree and both refs are absent. Preserve unrelated worktrees and branches.
