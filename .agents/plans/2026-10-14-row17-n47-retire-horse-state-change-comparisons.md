# Row 17 N-47: Retire Broad Horse-State Change Comparisons Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove two travel-generator change-detector tests that only require some output difference after several inputs change, while preserving the focused horse-specific behavior owners.

**Architecture:** This is a test and inventory correction only. N-30 owns the mounted HorseTrouble effect and N-32 owns horse-required candidate legality; the broad comparisons add no distinct player-facing promise and do not prove either invariant.

**Tech Stack:** C# 10 / .NET 10, xUnit, PowerShell host scripts, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially `Test design and acceptance`.

**Execution Strategy:** `executing-plans` (native inline) - the mutation probe must precede deletion, and the test source, disposition records, and successor-plan retirement share one narrow decision boundary. A separate implementer would add handoff overhead; one fresh whole-branch reviewer supplies independent review.

## Global Constraints

- Audit findings are evidence and design inputs, not execution instructions or proof that a fix works; check static findings against live source when the plan is written.
- Each meaningful scenario states the legal precondition, real operation, independent expected outcome, and broken behavior it would detect.
- Do not add a replacement test unless inspection discovers a distinct invariant not covered by the retained behavior owners.
- Preserve the current gameplay, event history, architecture decisions, feature promises, and the single canonical development-version source.
- Each implementation PR targets `develop`, passes exact-head hosted CI before merge, and is followed by a passing push gate on its exact merge commit.

## Review Focus

- Confirm the mutation disables only horse-required candidate filtering and that both broad comparisons remain green while N-32 fails on the illegal SpookedHorse result.
- Confirm N-30 and N-32 remain unchanged behavior owners, and no supported behavior is lost with the broad comparisons.
- Confirm the disposition records distinguish the test removals from gameplay changes and preserve the N-46-to-N-47 planning-artifact handoff.

---

### Task 1: Establish the test finding with controlled evidence

**Files:**
- Read: `tests/WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs`
- Read: `src/WildBunch.Domain/Travel/TravelDayPlanGenerator.Context.cs`
- Read: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

**Interfaces:**
- Consumes: the two broad comparisons, N-30's mounted horse-effect test, and N-32's fixed no-horse candidate-filter test.
- Produces: evidence to decide whether both broad comparisons can be removed without losing a distinct behavior owner.

- [x] **Step 1: Run the focused baseline.** `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~TravelDayPlanGeneratorTests" --verbosity minimal`. Expected: 16 passed, 0 failed.
- [x] **Step 2: Run the controlled eligibility mutation.** Temporarily replace `(!option.RequiresHorse || context.HasHorse)` with `(!option.RequiresHorse || true)`. Run the two broad tests with filter `FullyQualifiedName~GenerateChangesWhenMountedStateChanges|FullyQualifiedName~GenerateChangesWhenLiveStateChangesBeforeTheJourneyAdvances`, then run N-32 alone with filter `FullyQualifiedName~GenerateFallsBackWhenOnlyTheHorseSpecificUnluckyEventRemainsWithoutAHorse`. Expected: broad tests pass; N-32 fails because `BadLuckSpookedHorse` is emitted. Restore the production file byte-for-byte in `finally` and verify its SHA-256 matches the pre-mutation hash.

### Task 2: Retire the change detectors and record their disposition

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Delete: `.agents/plans/2026-10-14-row17-n46-correct-travel-generator-foe-tests.md`

**Interfaces:**
- Consumes: Task 1's passing mutation evidence and the existing N-30 and N-32 assertions.
- Produces: a test inventory that no longer treats arbitrary output differences as a player-facing invariant.

- [x] **Step 1: Remove only the two unsupported comparisons.** Delete `GenerateChangesWhenMountedStateChanges` and `GenerateChangesWhenLiveStateChangesBeforeTheJourneyAdvances`; remove `CreateSeedSensitiveSession` only if repository search confirms it has no other callers. Leave the focused N-30 and N-32 tests unchanged.
- [x] **Step 2: Record the durable disposition.** Explain that one test changes horse and saddle together (also changing derived context) and accepts any difference across six seeds; the other removes both after journey start and accepts any difference across 24 seeds. Record that the controlled horse-filter mutation left both green while N-32 failed, and that no replacement is warranted because these tests promised only arbitrary random-output difference.
- [x] **Step 3: Retire the predecessor plan.** Remove the N-46 plan artifact as the N-47 successor. Keep this N-47 plan as the active plan until a later successor retires it.
- [x] **Step 4: Commit the test and inventory changes.** Use commit message `test: remove horse state output comparison tests` after `git diff --check` and the focused generator suite pass. Expected after removal: 14 `TravelDayPlanGeneratorTests` pass.

### Task 3: Verify, review, publish, and close the slice

**Files:**
- Modify: `.agents/plans/2026-10-14-row17-n47-retire-horse-state-change-comparisons.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Review: the complete branch diff and all changed paths.

**Interfaces:**
- Consumes: Task 2's test and disposition changes.
- Produces: reviewed N-47 implementation merged into `develop` at `0.1.0-dev.165`, with its predecessor plan retired and its worktree and paired branch refs removed.

- [x] **Step 1: Run the complete Domain suite and canonical gate.** Ensure PostgreSQL with `.\tools\postgres-dev.ps1 ensure`; run `py -3 tools/run.py ci --check`. Expected: all fail-fast repository lanes pass; the gate includes Domain tests and all configured application, persistence, API, content, integration, web, and script checks.
- [x] **Step 2: Update the active plan and version evidence.** Mark completed steps, record validation and review outcomes needed to understand the disposition, and confirm only `Directory.Build.props` carries the authored version change to `0.1.0-dev.165`.
- [x] **Step 3: Reconcile durable truth.** Compare the final diff with ADR-0023, ADR-0024, ADR-0028, ADR-0029, ADR-0040, PG-007 in `docs/features.md`, and the full feature matrix. State in the PR description why no ADR or feature-matrix change is required.
- [ ] **Step 4: Review and publish.** Build the exact whole-branch review package; obtain an independent fresh-context review; fix any actionable findings with evidence. Publish the PR to `develop`, require its exact-head hosted gate to pass, then merge.
- [ ] **Step 5: Verify and clean up.** Require the develop push gate to pass on the exact merge commit, fast-forward the primary checkout, remove this exact worktree with the cleanup script, delete the paired local and upstream branches, and verify all three are absent while preserving unrelated worktrees.

## Acceptance Criteria

- Both broad comparisons are removed only after the planned mutation result is confirmed, and the production source is restored byte-for-byte.
- N-30 and N-32 remain unchanged and pass; no test promises arbitrary random output differences for changed inputs.
- The test-quality investigation and test disposition inventory explain why no replacement test is needed.
- The N-46 plan is retired in the N-47 implementation PR; the N-47 plan remains active and the version source is `0.1.0-dev.165`.
- The canonical gate, independent review, exact-head hosted gate, exact-merge push gate, fast-forward, worktree cleanup, and paired branch cleanup all complete successfully.
