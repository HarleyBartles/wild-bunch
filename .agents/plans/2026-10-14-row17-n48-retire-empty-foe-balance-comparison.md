# Row 17 N-48: Retire the Empty-Outcome Foe Balance Comparison Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the four-seed Foe-versus-Unlucky count comparison because it passes when generation produces neither category and no current player contract requires that ratio.

**Architecture:** This is a test and inventory correction only. The all-Quiet mutation will show that the targeted comparison passes with zero Foe and zero Unlucky outcomes, while the separate hostile-encounter reachability test fails. Retain that behavior owner and the independent difficulty, entropy, legality, cooldown, salt and fallback tests.

**Tech Stack:** C# 10 / .NET 10, xUnit, PowerShell host scripts, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially the current feature contracts and Test design and acceptance.

**Execution Strategy:** `executing-plans` (native inline) - the predecessor retirement, focused mutation evidence and test-inventory update form one narrow decision boundary. A separate implementer would add handoff overhead; one fresh whole-branch reviewer supplies independent review.

## Global Constraints

- Audit findings are evidence and design inputs, not execution instructions or proof that a fix works; verify the current test and generator behavior before changing it.
- Preserve gameplay behavior, event history, architecture decisions, feature promises, and the single canonical development-version source.
- Do not add a replacement test unless inspection discovers a distinct current player or domain invariant that lacks an owner.
- Keep the difficulty and entropy corpus comparisons, Foe reachability, legality, cooldown, salt and fallback behavior outside this slice.
- Each implementation PR targets `develop`, passes exact-head hosted CI before merge, and is followed by a passing push gate on its exact merge commit.

## Review Focus

- Confirm the all-Quiet generator mutation leaves the Foe-versus-Unlucky count test green while the existing hostile-encounter test fails because it sees no Foe.
- Confirm the current ADRs and PG-007 make no promise about a Foe-to-Unlucky count ratio, and that retained tests still own actual hostile encounter reachability and the independent difficulty/entropy behavior.
- Confirm N-47 was complete, its delivery evidence was promoted, and its plan is retired only in this successor PR.

---

### Task 1: Classify and retire the completed N-47 plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Create: `.agents/plans/2026-10-14-row17-n48-retire-empty-foe-balance-comparison.md`
- Delete: `.agents/plans/2026-10-14-row17-n47-retire-horse-state-change-comparisons.md`

**Interfaces:**
- Consumes: N-47 implementation and delivery evidence from PR #350, current source and the pinned completed-artifact-custody practice.
- Produces: the N-47 delivery record in the roadmap, a precise list of generator comparisons that remain under investigation, and this committed JIT plan as the active row-17 successor.

- [x] **Step 1: Verify N-47 is complete across its whole scope.** Confirm PR #350 is merged into `develop` at `1e1e7770b06b5add00929392e6d6376f15afe471`, its recorded source is `101a1b7f80de98f4b843ab6fbe55e8f72c95778e`, exact-head gate 38556516483 passed, develop-push gate 38557255902 passed, and the N-47 worktree and exact local/upstream branch refs are absent. Confirm its test removals and dispositions exist on this base before deleting the plan.
- [x] **Step 2: Promote the delivery evidence and make the successor active.** Update roadmap row 17 to link this N-48 plan and planned version `0.1.0-dev.166`; add PR #350, source/merge SHAs, review outcome and both hosted gate links.
- [x] **Step 3: Retire only the completed predecessor artifact.** Delete the N-47 plan from this current `develop` base. Keep the active stable-baseline spec, roadmap, investigation and test-disposition inventory.
- [x] **Step 4: Commit the successor plan and predecessor retirement as the first substantive commit.** Run `git diff --check`, inspect the complete staged change and commit with `docs: plan N-48 foe balance comparison cleanup`.

### Task 2: Falsify and retire the empty-outcome comparison

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

**Interfaces:**
- Consumes: Task 1 delivery record and the live `GenerateKeepsFoeSelectionAheadOfUnluckySelectionOnComparableHighRiskRoutes` assertion.
- Produces: removal of the unsupported aggregate ratio assertion, retained hostile-encounter reachability proof, and a dated evidence-backed disposition.

- [ ] **Step 1: Run the focused baseline and record the production-file hash.** Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~GenerateKeepsFoeSelectionAheadOfUnluckySelectionOnComparableHighRiskRoutes --verbosity minimal`; expected: 1 passed. Record the SHA-256 of `src/WildBunch.Domain/Travel/TravelDayPlanGenerator.Context.cs` outside the repository.
- [ ] **Step 2: Apply the controlled all-Quiet mutation.** Temporarily change only `SelectCategory` to return `TravelDayEncounterCategory.Quiet` for every context and roll. Run the targeted Foe-versus-Unlucky comparison; expected: it still passes because both counts are zero. Run `GenerateCreatesOnlyHostileChoiceEncountersWithAResolvedFoeProfile`; expected: it fails at the nonempty hostile-encounter assertion because no Foe is generated.
- [ ] **Step 3: Restore production byte-for-byte.** Restore the generator file in `finally`, compare its SHA-256 to the recorded value, and verify no production diff remains before proceeding.
- [ ] **Step 4: Remove the unsupported comparison and record the disposition.** Delete only `GenerateKeepsFoeSelectionAheadOfUnluckySelectionOnComparableHighRiskRoutes`. State that its four-seed aggregate accepts zero outcomes and that no ADR or PG-007 contract requires the Foe-to-Unlucky ratio. Preserve `GenerateCreatesOnlyHostileChoiceEncountersWithAResolvedFoeProfile` and the difficulty, entropy, cooldown, legality, salt and fallback owners. Do not add a replacement test.
- [ ] **Step 5: Verify and commit the inventory correction.** Run the full `TravelDayPlanGeneratorTests` filter; expected: 13 passed. Run `git diff --check`, review the changed dispositions and test diff, then commit with `test: remove empty foe balance comparison`.

### Task 3: Verify, review, publish and close the slice

**Files:**
- Modify: `.agents/plans/2026-10-14-row17-n48-retire-empty-foe-balance-comparison.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Review: the complete branch diff and all changed paths.

**Interfaces:**
- Consumes: Task 2's falsification evidence and test-inventory correction.
- Produces: reviewed N-48 implementation merged into `develop` at `0.1.0-dev.166`, with N-47 retired and this plan's worktree and paired branch refs removed.

- [ ] **Step 1: Run focused and complete validation.** Ensure PostgreSQL with `.\tools\postgres-dev.ps1 ensure`; run the Domain suite and `py -3 tools/run.py ci --check`. Expected: the focused generator suite has 13 passes, the Domain suite passes with only its existing skip, and every canonical fail-fast repository lane passes.
- [ ] **Step 2: Set the development version and reconcile durable truth.** Change only `Directory.Build.props` to `0.1.0-dev.166`; verify it is the sole authored version value. Compare the final diff with ADR-0023, ADR-0024, ADR-0040, PG-007 and the full feature matrix; record that no player promise, architecture decision or feature dependency changed.
- [ ] **Step 3: Record final evidence and build the exact review package.** Update this plan and the roadmap with the focused mutation result, 13-test result, canonical gate result and `0.1.0-dev.166`. Build the review package from the exact merge base to current HEAD.
- [ ] **Step 4: Obtain independent review and publish.** Dispatch a fresh-context reviewer on the complete branch diff, plan and spec. Resolve any actionable findings and re-review corrections. Open a Draft PR to `develop`, verify its exact head and body, mark it ready after review, and require its exact-head hosted gate to pass before squash merge.
- [ ] **Step 5: Verify integration and clean up.** Require the develop push gate to pass on the exact merge commit; fast-forward the primary checkout; remove this exact worktree with the cleanup script; then delete the paired local and upstream branches and verify all three are absent while preserving unrelated worktrees.

## Acceptance Criteria

- The controlled all-Quiet mutation passes the retired comparison but fails the retained hostile-encounter reachability test; production is restored byte-for-byte.
- Only the four-seed Foe-versus-Unlucky aggregate comparison is removed; no arbitrary Foe-count ratio is retained as a current player promise.
- Separate difficulty and entropy distribution behavior, actual hostile Foe reachability, cooldowns, legality, salt behavior and fallbacks remain covered.
- N-47 delivery evidence is promoted before its plan is retired in the first substantive successor commit; this N-48 plan remains through its completing PR.
- The canonical gate, fresh independent review, exact-head hosted gate, exact-merge push gate, primary fast-forward, worktree cleanup and paired branch cleanup all complete successfully.
