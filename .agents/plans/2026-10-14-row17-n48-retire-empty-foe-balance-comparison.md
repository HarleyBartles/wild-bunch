# Row 17 N-48: Validate and Retain the Foe Distribution Guards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve the dispositions of the remaining Foe distribution comparisons through behavior-based evidence, correcting the earlier investigation's assumption that the four-seed Foe-versus-Unlucky comparison accepts empty outcomes.

**Architecture:** This is a test-inventory correction only. Retain the existing fixed-corpus difficulty and entropy comparisons and the four-seed Foe-versus-Unlucky guard: they measure distinct generator balance behavior, and controlled mutations show each assertion detects a change it is intended to constrain. Do not claim their aggregate ratios as player-facing contracts.

**Tech Stack:** C# 10 / .NET 10, xUnit, PowerShell host scripts, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially the current feature contracts and Test design and acceptance.

**Execution Strategy:** `executing-plans` (native inline) - the predecessor retirement, focused mutation evidence and test-inventory update form one narrow decision boundary. A separate implementer would add handoff overhead; one fresh whole-branch reviewer supplies independent review.

## Global Constraints

- Audit findings are evidence and design inputs, not execution instructions or proof that a fix works; verify the current test and generator behavior before changing it.
- Preserve gameplay behavior, event history, architecture decisions, feature promises, and the single canonical development-version source.
- Do not add a replacement test unless inspection discovers a distinct current player or domain invariant that lacks an owner.
- Preserve the difficulty and entropy corpus comparisons, Foe reachability, legality, cooldown, salt and fallback behavior; this slice changes only their audit disposition.
- Each implementation PR targets `develop`, passes exact-head hosted CI before merge, and is followed by a passing push gate on its exact merge commit.

## Review Focus

- Confirm the all-Quiet generator mutation fails the Foe-versus-Unlucky test at its positive-Foe reachability assertion.
- Confirm a controlled increase to Unlucky weight fails the Foe-versus-Unlucky balance assertion while retaining Foe outcomes.
- Confirm the difficulty test's initial apparent failure disappears after a clean rebuild, and record that the earlier result came from stale incremental build outputs.
- Confirm the current ADRs and PG-007 make no player-facing promise about an exact Foe count or ratio; retained comparisons remain internal guards for the stated generator weighting behavior.
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

### Task 2: Prove the remaining Foe distribution assertions are behavior guards

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

**Interfaces:**
- Consumes: Task 1 delivery record and the live `GenerateKeepsFoeSelectionAheadOfUnluckySelectionOnComparableHighRiskRoutes` assertion.
- Produces: a dated evidence-backed disposition that retains the valid distribution behavior owners and corrects the stale test inventory.

- [x] **Step 1: Rebuild before interpreting the baseline.** Ran `dotnet clean tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`, then reran the Foe, Brutal and Wild focused checks and the complete `TravelDayPlanGeneratorTests` filter. All 14 generator tests passed after the clean rebuild. The first incremental Brutal run had reported Standard 60 and Brutal 65; it was stale build output, not a current-source or platform-specific failure. Production generator SHA-256 was `DE340053E9C3713E2DF51184348BA5143CF55335343D32D90FE41275BB36CBF8`.
- [x] **Step 2: Apply the controlled all-Quiet mutation.** Temporarily changed only `SelectCategory` to return `TravelDayEncounterCategory.Quiet` for every context and roll. `GenerateKeepsFoeSelectionAheadOfUnluckySelectionOnComparableHighRiskRoutes` failed at `foeCount > 0`; the production file was restored byte-for-byte with its recorded SHA-256.
- [x] **Step 3: Apply a balance-sensitive mutation.** Temporarily increased only the high-risk Unlucky weight by 50 while preserving Foe reachability. The same focused test failed at the Foe-versus-Unlucky balance assertion; production was restored byte-for-byte and the hash matched.
- [x] **Step 4: Record the evidence-backed disposition.** Retain all three distribution tests. The difficulty comparison, entropy comparison and Foe-versus-Unlucky comparison each detect distinct balance changes under fixed seeds; they constrain generator behavior without promising an exact player-facing encounter ratio.
- [x] **Step 5: Verify and commit the corrected inventory.** The complete `TravelDayPlanGeneratorTests` filter passed (14 tests); `git diff --check` passed and the changed dispositions were reviewed.

### Task 3: Verify, review, publish and close the slice

**Files:**
- Modify: `.agents/plans/2026-10-14-row17-n48-retire-empty-foe-balance-comparison.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Review: the complete branch diff and all changed paths.

**Interfaces:**
- Consumes: Task 2's falsification evidence and test-inventory correction.
- Produces: reviewed N-48 inventory correction merged into `develop` at `0.1.0-dev.166`, with N-47 retired and this plan's worktree and paired branch refs removed.

- [x] **Step 1: Run focused and complete validation.** PostgreSQL was ensured with `.\tools\postgres-dev.ps1 ensure`; the Domain suite passed (534 passed, 1 skipped), and `py -3 tools/run.py ci --check` passed all canonical fail-fast lanes at `0.1.0-dev.166`, including 331 integration tests and 376 web tests.
- [x] **Step 2: Set the development version and reconcile durable truth.** Changed only `Directory.Build.props` to `0.1.0-dev.166`; the canonical CI version-identity check passed. This inventory-only slice changes no player promise, architecture decision or feature dependency, so ADRs and the feature matrix need no edits.
- [ ] **Step 3: Record final evidence and build the exact review package.** Update this plan and the roadmap with the focused mutation result, 14-test result, canonical gate result and `0.1.0-dev.166`. Build the review package from the exact merge base to current HEAD.
- [ ] **Step 4: Obtain independent review and publish.** Dispatch a fresh-context reviewer on the complete branch diff, plan and spec. Resolve any actionable findings and re-review corrections. Open a Draft PR to `develop`, verify its exact head and body, mark it ready after review, and require its exact-head hosted gate to pass before squash merge.
- [ ] **Step 5: Verify integration and clean up.** Require the develop push gate to pass on the exact merge commit; fast-forward the primary checkout; remove this exact worktree with the cleanup script; then delete the paired local and upstream branches and verify all three are absent while preserving unrelated worktrees.

## Acceptance Criteria

- The controlled all-Quiet mutation fails the retained Foe-versus-Unlucky test at its explicit reachability assertion; production is restored byte-for-byte.
- A controlled Unlucky overweight mutation fails the Foe-versus-Unlucky balance assertion while Foe remains reachable; production is restored byte-for-byte.
- All three fixed-seed Foe distribution tests remain covered as internal generator behavior guards, without being represented as exact player-facing count promises.
- The complete generator test class passes 14 tests, and the Domain suite passes with its existing skip.
- N-47 delivery evidence is promoted before its plan is retired in the first substantive successor commit; this N-48 plan remains through its completing PR.
- The canonical gate, fresh independent review, exact-head hosted gate, exact-merge push gate, primary fast-forward, worktree cleanup and paired branch cleanup all complete successfully.
