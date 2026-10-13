# Row 17 N-27 Lucky Food Cache Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Replace the weak Lucky Food Cache travel test with a normal command-path scenario proving the event is reached, grants its recorded food effect, updates the absolute event snapshot and player inventory, and reconstructs the same state from the complete event history.

**Architecture:** Keep travel authority in GameSession; use ordinary setup, StartJourney and AdvanceJourneyDay, with a deterministic runtime salt that reaches the existing Easy/OpenRange/LuckyFoodCache path. TrailEventApplied records the fact and absolute journey snapshot; replay consumes that fact without rerolling. This is test and audit correction only; do not change production code, architecture decisions or product promises unless the new behavior test exposes a real defect.

**Tech Stack:** C#/.NET 10, xUnit, WildBunch.Domain.Tests.

**Spec:** Stable 0.1.0 baseline at ../specs/2026-10-07-stable-0.1.0-baseline.md; cleanup roadmap at ../roadmaps/2026-10-07-stable-0.1.0-cleanup.md, row 17; whole-test quality investigation at ../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md, N-18; .NET test dispositions at ../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md, TravelResolverTests.cs.

**Execution Strategy:** executing-plans, inline and sequential. The behavior test and its two audit corrections share one deterministic scenario and depend on a single source inspection; subagent-driven development would add repeated setup/context transfer without independent implementation tracks, so one inline integration context is the lower-cost fit.

**Governing Decisions:** ADR-0028 governs event-derived state, absolute event snapshots and exact replay; ADR-0029 governs current trail-event pressure boundaries. The test exercises these existing decisions without changing them. The author and reviewer must independently compare the actual diff against relevant ADRs and the feature matrix; no decision or product promise is expected to change.

## Global Constraints

- Keep the deterministic salt in SaltSourceMode.Runtime; do not turn this behavior test into a fixed-salt-mode product assertion.
- Reach the cache through canonical session creation, ordinary travel preview, StartJourney and AdvanceJourneyDay; do not directly construct the event, patch inventory, or set TrailEvent.
- Assert facts observable at the command result, emitted TrailEventApplied, player inventory and full replay; avoid incidental messages and implementation-shape assertions.
- Limit the source diff to the existing weak food-cache test/helper and the two N-18 audit/disposition documents. Make no production, ADR or feature-matrix change unless the observed behavior contradicts its owner.
- Advance the application version once for this PR to 0.1.0-dev.145; the single authored version source remains Directory.Build.props.

## Review Focus

- A normal Easy/OpenRange travel command must actually reach LuckyFoodCache; the test must fail if it reaches no event or a different event.
- The cache must record a +2 food outcome and the absolute snapshot/final inventory must reflect daily upkeep plus reward; the test must fail if the reward is suppressed while the event ID remains.
- Rehydrating the complete event stream must reconstruct the same journey food and inventory; do not compare incidental event ordering or narration.
- The audit must distinguish the N-18 snapshot-fallback claim already fixed by PR #262 from the still-open food-cache weakness; its earlier history remains unchanged.

---

### Task 1: Retire N-26 and install N-27

**Files:**
- Modify: .agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md
- Modify: Directory.Build.props
- Delete: .agents/plans/2026-10-13-row17-n26-travel-horse-lameness.md
- Add: .agents/plans/2026-10-13-row17-n27-lucky-food-cache-proof.md

**Interfaces:**
- Consumes: PR #329 merged head 641f6f1fb95da004cf6819021b953054952f78f2, merge bb7e69563fec21971831660f956aeee07731eaf4, identical tree 8a8b03b8e1a812c88e44fceb09714f0dfc4661b1, clean independent correction review, exact-head hosted gate 38425972433, develop-push gate 38426812327, and verified paired worktree/ref cleanup.
- Produces: current N-27 plan at 0.1.0-dev.145, retired N-26 plan, and truthful N-26 delivery plus N-27 scope in row 17.

- [x] **Step 1: Verify the successor boundary.** Confirm PR #329 is merged to develop at the stated head and merge identities; verify source/merge tree equality, correction review, exact-head and develop-push gates, and absence of the N-26 worktree and local/upstream refs. Confirm this worktree and proposed branch are dedicated to N-27, primary checkout is clean on develop at the merge, and the separate AWS learning worktree still exists.
- [x] **Step 2: Install N-27 and record N-26.** Add PR #329 and its source, merge, tree, review, gate and cleanup evidence to row 17; set its active link to this plan and its version to 0.1.0-dev.145; state that N-26 closed only run-induced horse lameness and N-27 addresses only Lucky Food Cache behavior plus the stale-claim correction.
- [x] **Step 3: Retire and commit planning artifacts.** Advance Directory.Build.props exactly once to 0.1.0-dev.145, remove the completed N-26 plan, validate plan links/version ownership and commit only the roadmap, version and plan changes before editing tests or investigations.

### Task 2: Prove Lucky Food Cache through command and replay

**Files:**
- Modify: tests/WildBunch.Domain.Tests/TravelResolverTests.cs
- Modify: .agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md
- Modify: .agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md

**Interfaces:**
- Consumes: TestSessionFactory.StartGameCanonical, SaltSource(SaltSourceMode.Runtime, ...), TravelResolver.PreviewJourney, GameSession.StartJourney, AdvanceJourneyDay, TrailEventApplied, GameSession.AllEvents, and GameSession.RehydrateFromEvents.
- Produces: one deterministic reached command/event/effect/replay proof; a dated N-18 disposition that closes only the food-cache weakness and corrects the PR #262 snapshot-fallback claim.

- [ ] **Step 1: Write the failing behavioral proof.** Change CreateEasyLuckyFoodSession to accept an explicit runtime SaltSource; use the established salt n27-lucky-food-1, which reaches LuckyFoodCache on the ordinary Easy/OpenRange pinecross to openpass route. Assert starting inventory contains 3 food, start travel through the resolver and aggregate, advance one day, then assert a real reached result with TrailEvent present, kind Lucky, ID LuckyFoodCache, and FoodDelta == 2. Require the corresponding single TrailEventApplied, assert its recorded ID/delta and JourneySnapshot.AvailableFood == 4, assert final inventory food is 4, and assert the active journey agrees. Rehydrate from the full session.AllEvents history and assert journey status/food and inventory food equal the command-path result (4 food). Remove the old optional-event branch and unchanged-resource assertion.
- [ ] **Step 2: Witness the intended failure.** Run dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~AdvanceJourneyDayCanTriggerAnAdditionalLuckyFoodCacheEventOnEasyOpenRangeRoutes. Before changing production, temporarily set the Easy LuckyTrailFoodReward from 2 to 0 while preserving LuckyFoodCache eligibility/identity; confirm the new test fails at the food-delta or final-inventory expectation. Restore the production value immediately and rerun the focused test. Do not commit the mutation.
- [ ] **Step 3: Correct only the owned audit records.** In N-18, replace the claim that a snapshot-fallback test remains weak with a dated correction: AdvanceJourneyDaySwitchesToFootImmediatelyWhenHorseBecomesLameAndRecalculatesRemainingDays now requires a nonnull command snapshot and direct mode/day assertions since PR #262 (1a69cbf981ffb945f6151f54218d9765daa43afe); retain that commit's original history and do not create a replacement test. Add a dated N-27 disposition stating the food-cache test now proves the reached fact, +2 recorded delta, absolute snapshot, net inventory result and full replay. Keep unrelated bad-luck, spooked-horse, other Lucky, completion and generator findings open. Update the TravelResolverTests.cs disposition consistently and make no other inventory claim.
- [ ] **Step 4: Validate and commit the test slice.** Run the focused test, all TravelResolverTests, inspect the behavioral and audit diff, start shared PostgreSQL with .\tools\postgres-dev.ps1 ensure, confirm no production mutation remains, and commit the staged test/audit changes through the normal check-only hook. Use that staged-snapshot gate as the canonical local result; do not rerun CI immediately before or after a successful hooked commit. Rerun the focused tests and canonical gate only if a later correction changes the committed tree.

### Task 3: Review, publish, merge and close out

**Files:**
- Review: full N-27 diff, N-18 test dispositions, ADR-0028, ADR-0029, feature matrix, and selected backend/code-review unslop profiles.

**Interfaces:**
- Consumes: committed N-27 plan/version and test/audit slices, fresh whole-branch review, canonical local and hosted gates, and refreshed develop.
- Produces: one reviewed PR to develop, passing exact-head hosted gate and develop-push gate, verified merge/tree identity, fast-forwarded primary checkout, and paired worktree/local/upstream branch cleanup.

- [ ] **Step 1: Obtain a fresh independent review.** Build the branch review package from the actual merge base and head; review the complete committed diff against this plan, ADR-0028/0029 and the feature matrix. Independently verify that the author ADR and feature-matrix checks match the diff; record why there is no durable decision or product-promise change. Resolve any material finding with witnessed behavior proof and obtain a fresh review of the corrected head.
- [ ] **Step 2: Publish and merge.** Push the branch and open one PR to develop. Confirm its head equals local HEAD, author and reviewer ADR/feature checks are resolved, and the exact-head hosted canonical gate passes before squash merge.
- [ ] **Step 3: Verify integration and paired cleanup.** Verify the merge and source tree identities, passing develop-push gate, fast-forward the primary checkout, and verify the merge is present. Confirm the N-27 worktree is clean, remove that worktree first, then delete its exact local and upstream branch refs and verify all three are absent. Preserve the separate AWS learning worktree.
