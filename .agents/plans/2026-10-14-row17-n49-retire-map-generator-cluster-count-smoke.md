# Row 17 N-49: Retire the Map Generator Cluster-Count Smoke

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the map-generator theory that claims to verify cluster assignment but only checks town count, while retaining the behavior owner that asserts the configured number of distinct cluster assignments.

**Architecture:** Test-inventory correction only. `MapGeneratorTests` verifies generated-world behavior; `ClusterPlacementGeneratorTests` owns assignment count. Do not add a replacement test or alter gameplay generation.

**Tech Stack:** C# 10 / .NET 10, xUnit, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially [test design and acceptance](../specs/2026-10-07-stable-0.1.0-baseline.md#test-design-and-acceptance) and the settled generated-world contracts.

**Execution Strategy:** `executing-plans` (native inline) - the duplicate-test mutation proof, removal, and inventory correction are one narrow behavior-ownership decision; a fresh whole-branch reviewer supplies independent review.

## Global Constraints

- Treat prior audit notes as claims to verify against current source and tests.
- Preserve generated worlds, cluster placement, seed semantics, gameplay behavior, ADR decisions, feature promises, and the single canonical development-version source.
- Do not add a replacement test unless inspection finds a distinct current invariant without a behavior owner.
- Assign exactly one unique next `0.1.0-dev.N` version for the merged PR, and retain `develop` as the PR base.
- The exact-head hosted gate must pass before merge, and the develop push gate must pass on the exact merge SHA before cleanup.

## Review Focus

- The MapGenerator theory asserts only eight towns for every cluster count and does not observe cluster assignments.
- The direct `ClusterPlacementGenerator` behavior test requires the configured number of distinct assignments and fails when assignment generation collapses into one cluster.
- The mutation is restored byte-for-byte; production behavior and decision/feature records remain unchanged.
- N-48 delivery is fully verified and its plan is retired only after its evidence is promoted in this successor's first substantive commit.

---

### Task 1: Promote N-48 and establish N-49 as its successor

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n49-retire-map-generator-cluster-count-smoke.md`
- Delete: `.agents/plans/2026-10-14-row17-n48-retire-empty-foe-balance-comparison.md`

**Interfaces:**
- Consumes: N-48 PR #351, its exact-head review and hosted gate, develop merge and push gate, and the live branch/worktree state.
- Produces: promoted N-48 delivery evidence, the active N-49 plan at `0.1.0-dev.167`, and retirement of the completed N-48 plan.

- [x] **Step 1: Verify N-48 delivery and cleanup.** Confirm PR #351 is merged to `develop`, reviewed head `449511710ec595ed18e8274109a9480fbbf1d9d4` equals the PR head, merge SHA `8729b11a16267d259a8b7ca4733b713455ff715e` is in current `develop`, exact-head gate run `38564059978` passed, develop push gate run `38564841220` passed on that merge SHA, and the primary checkout and paired branch/worktree cleanup are complete. Confirm N-48's mutation evidence is already present in the investigation and test-disposition inventory before retiring its plan.
- [x] **Step 2: Promote delivery, activate N-49, and advance version.** Update roadmap row 17 to link this plan, assign `0.1.0-dev.167`, add merged PR #351 and its exact reviewed/merge identities and hosted gate evidence, and record N-49's bounded cluster-count smoke disposition. Change only the authored version in `Directory.Build.props` to `0.1.0-dev.167`.
- [x] **Step 3: Retire the completed predecessor.** Delete the N-48 plan after confirming its full scope was delivered and the evidence now has current owners. Preserve the baseline specification, roadmap, test-quality investigation, and test-disposition inventory.
- [x] **Step 4: Commit successor planning and predecessor retirement first.** Run `git diff --check`, inspect the complete staged change, and commit the plan, roadmap, version, and N-48 retirement as the first substantive commit using `docs: plan N-49 cluster-count test ownership`.

### Task 2: Prove and retire the misleading map-level cluster-count theory

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Inspect only: `tests/WildBunch.GameContent.Tests/ClusterPlacementGeneratorTests.cs` and `src/WildBunch.GameContent/NewGame/ClusterPlacementGenerator.cs`

**Interfaces:**
- Consumes: `MapGeneratorTests.Generate_ClusterCount_AllTownsAssignedToValidClusters` and `ClusterPlacementGeneratorTests.Place_ClusterCount_ProducesExpectedClusterAssignments`.
- Produces: one current owner for configured cluster-assignment behavior and an evidence-backed disposition of the redundant map-level theory.

- [ ] **Step 1: Reconfirm the behavior boundary.** Run both focused tests on the unchanged baseline. Verify the map-level theory checks only town count and the direct cluster-placement theory asserts the number of distinct assignment values for cluster counts 1 through 4.
- [ ] **Step 2: Falsify the map-level assertion's claimed sensitivity.** Temporarily change the Boring assignment branch in `ClusterPlacementGenerator.AssignTownsToClusters` so every town is assigned to cluster 0. Run the map-level theory and confirm it still passes; run `Place_ClusterCount_ProducesExpectedClusterAssignments` and confirm its cases for cluster counts above 1 fail. Restore the production file byte-for-byte and verify its recorded hash.
- [ ] **Step 3: Remove only the unsupported smoke.** Delete `Generate_ClusterCount_AllTownsAssignedToValidClusters` and its four cluster-count theory rows from `MapGeneratorTests`. Add a dated N-49 disposition to the investigation and update the `MapGeneratorTests.cs` inventory entry to point to `Place_ClusterCount_ProducesExpectedClusterAssignments` as the sole assignment-count behavior owner. Do not add a replacement test.
- [ ] **Step 4: Verify the focused owners.** Run the cluster-assignment owner and the relevant MapGenerator test class; confirm current cluster-count behavior remains covered and all remaining map-generator assertions pass.

### Task 3: Review, publish, merge, and retire N-49

**Files:**
- Review: the complete diff, N-49 plan, roadmap, and applicable decision/feature records.
- Modify: `.agents/plans/2026-10-14-row17-n49-retire-map-generator-cluster-count-smoke.md` and roadmap with final delivery evidence.

**Interfaces:**
- Consumes: Task 2's controlled mutation results and focused tests.
- Produces: N-49 merged to `develop` at `0.1.0-dev.167`, with reviewed evidence and the exact worktree and paired branch refs retired.

- [ ] **Step 1: Validate the final tree.** Run `.\tools\postgres-dev.ps1 ensure`, then `py -3 tools/run.py ci --check` on the committed versioned tree. Confirm the generated build identity consumes `Directory.Build.props` and no second application version was authored.
- [ ] **Step 2: Reconcile durable truth.** Compare the actual diff with ADR-0021 and ADR-0039 through the decision-record playbook and with `docs/features.md` through its maintenance playbook. State why no durable decision or feature promise changes; do not add ADR or feature-matrix edits for this test-inventory correction.
- [ ] **Step 3: Review and publish.** Build the review package from the exact merge base to current `HEAD`; dispatch a fresh-context reviewer on the complete diff, plan, roadmap, and evidence. Resolve findings and re-review corrections. Publish a Draft PR to `develop`, verify the exact head and body, mark it ready, and require exact-head hosted CI to pass before squash merge.
- [ ] **Step 4: Verify integration and clean up.** Require the develop push gate to pass on the exact merge commit; fast-forward the primary checkout; remove this worktree using the `finishing-a-development-branch` skill script; then delete and verify absence of the matching local and upstream branches.

## Acceptance Criteria

- The controlled one-cluster mutation leaves the map-level town-count theory passing but makes the direct assignment-owner test fail for cluster counts above one; production is restored byte-for-byte.
- The unsupported map-level theory is removed without adding a replacement, and current cluster assignment behavior remains owned by `Place_ClusterCount_ProducesExpectedClusterAssignments`.
- No gameplay source, seed behavior, architecture decision, or feature promise changes; both test inventories identify the same current behavior owner.
- N-48 delivery evidence is promoted before its completed plan is retired in the first substantive successor commit; this plan remains through its completing PR.
- The focused tests, canonical local gate, fresh independent review, exact-head hosted gate, exact-merge develop gate, primary fast-forward, and paired worktree/ref cleanup are verified.
