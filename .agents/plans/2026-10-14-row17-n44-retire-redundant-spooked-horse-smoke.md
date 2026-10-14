# N-44 Retire Redundant No-Horse Spooked-Horse Smoke Test Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Remove the command-level no-horse spooked-horse scenario that does not detect the horse-eligibility defect already protected by the focused generator behavior test.

**Architecture:** Keep production behavior unchanged. `TravelDayPlanGeneratorTests.GenerateFallsBackWhenOnlyTheHorseSpecificUnluckyEventRemainsWithoutAHorse` owns the invariant that horse-specific BadLuck is excluded without a horse; its test fails when that filter is disabled. The command-level scenario passed the same mutation because its fixed setup did not require the relevant outcome, so it protects no additional path or state transition.

**Tech Stack:** .NET 10, xUnit, Wild Bunch Domain travel tests and test audit documents.

**Spec:** The [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), the [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), the [test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), validation doctrine and ADR-0028.

**Execution Strategy:** `executing-plans` (native inline execution; one sequential test-audit correction slice).

## Global Constraints

- Preserve route-risk weighting, horse eligibility, difficulty, entropy, event generation and all gameplay behavior.
- Remove only `AdvanceJourneyDayOnFootWithoutAHorseDoesNotProduceSpookedHorse` and `CreateNoHorseBadLuckSession` if repository search confirms the fixture has no other caller.
- Retain N-32's generator-level behavior proof; do not add another command assertion for the same candidate filter.
- Advance the single authored development version to `0.1.0-dev.162` in this slice.
- Use the fresh canonical worktree and PR to `develop`; require independent review, exact-head hosted CI and develop-push CI; retire the worktree before its paired local and remote branch refs.

## Review Focus

- A future command-level regression test should be added only if it protects a command-owned effect or event-stream invariant not already proven at the generator owner. The existing command scenario did not reach or require `BadLuckSpookedHorse`; disabling the generator's horse filter left it passing.

---

## Task 1: Record N-43 delivery and commit the N-44 plan

**Files:**
- Create: `.agents/plans/2026-10-14-row17-n44-retire-redundant-spooked-horse-smoke.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-14-row17-n43-remove-unsupported-badluck-exclusion.md`

**Interfaces:**
- Consumes: merged N-43 source `4770635ad3e7cdb5b063dd9471264339fe438d76`, PR #346, squash merge `379f0c355dbdab5bc2537e390835c0aedb6a15dc`, identical source/merge tree `483195d1ab3c50ba51c5fa85e15a5354419501b7`, exact-head gate run `38538527054`, develop-push gate run `38539265745`, clean N-43 worktree/ref retirement, and current `develop`.
- Produces: an active N-44 plan, successor retirement and dated delivery record for N-43, and application version `0.1.0-dev.162`.

- [x] Verify PR #346 merged the exact N-43 branch head to `develop`, both hosted gates passed, the primary checkout is at merge `379f0c355dbdab5bc2537e390835c0aedb6a15dc`, and the N-43 worktree plus local/upstream refs are absent.
- [x] Record N-43's independent review, local test and hosted-gate outcomes in row 17; select N-44 and advance `Directory.Build.props` to `0.1.0-dev.162`.
- [x] Commit the N-43 retirement, N-44 plan, roadmap update and version transition before changing the test inventory.

## Task 2: Retire the command-level no-horse smoke test

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`

**Interfaces:**
- Consumes: `AdvanceJourneyDayOnFootWithoutAHorseDoesNotProduceSpookedHorse`, its sole-use `CreateNoHorseBadLuckSession` fixture, N-32's `GenerateFallsBackWhenOnlyTheHorseSpecificUnluckyEventRemainsWithoutAHorse`, the horse-required candidate filter and the controlled mutation result showing the command-level scenario passes when that filter is disabled.
- Produces: removal of a redundant sampled command test and its only-use fixture, with a dated disposition documenting the mutation evidence and retained owner-level behavior proof.

- [x] Confirm `CreateNoHorseBadLuckSession` has no caller beyond the command-level test; record that both targeted tests pass before mutation.
- [x] Disable the `RequiresHorse` candidate filter temporarily and rerun both tests. Preserve the evidence that the command-level test passes while the N-32 generator test fails on the spooked-horse candidate; restore production byte-for-byte and verify the worktree diff is clean before editing.
- [x] Remove the command-level test and its sole-use fixture; do not replace it because N-32 proves the exact invariant at its owner and the command test detected no distinct behavior.
- [x] Update the test dispositions and investigation with the dated N-44 rationale; compare ADR-0028 and the feature matrix and record why neither changes.
- [x] Run the retained focused generator behavior test, the complete Domain test project, then commit through the check-only hook and require the fail-fast `py -3 tools/run.py ci --check` staged-candidate gate to pass.

## Task 3: Review and deliver N-44

- [ ] Build the whole-branch review package against the recorded `develop` base and have a fresh independent reviewer inspect the committed diff, row-17 scope, ADR-0029, N-18, dispositions and feature matrix. The user has given standing authorization for an independent reviewer on every plan in this epic.
- [ ] Resolve any Critical or Important finding with a scoped correction and re-review the exact fix; preserve documented disposition for nonblocking observations.
- [ ] Open a Draft PR to `develop`, verify its body and exact head, mark ready after clean review, and require exact-head canonical hosted CI.
- [ ] Squash-merge the reviewed PR, verify the merge commit and passing develop-push canonical CI, then fast-forward the primary checkout to `origin/develop`.
- [ ] Remove the clean N-44 worktree before deleting its exact local/upstream refs, and verify the worktree registration and both refs are absent.
