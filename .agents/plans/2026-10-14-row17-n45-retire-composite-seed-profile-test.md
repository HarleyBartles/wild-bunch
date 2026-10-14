# N-45 Retire Composite Seed and Scenario Profile Comparison Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Remove the travel-generator test that changes GameSeed and ScenarioProfileId together and therefore proves neither input independently.

**Architecture:** Keep the live journey generator, its salt/entropy inputs, and event behavior unchanged. Preserve same-context determinism and actual generator legality/entropy coverage; remove only the test-only conditional comparison that is absent from production callers.

**Tech Stack:** C# 10 / .NET 10, xUnit, Python command bus.

**Spec:** The [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md) and row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md).

**Execution Strategy:** executing-plans, because the metadata successor and single test disposition are sequential parts of one narrowly scoped reviewable slice.

## Global Constraints

- Ordinary development targets develop and each plan uses a fresh script-created worktree.
- Assign 0.1.0-dev.163 as the next merged development version.
- Do not change product behavior, generator inputs, event contracts, or gameplay promises in this test-inventory slice.
- Tests must prove a behavior and must fail when that behavior is broken; conditional output-difference checks do not establish which input caused the difference.
- Preserve strict CQRS, aggregate authority, event-sourced mutation, deterministic replay, and existing live salt/entropy behavior.
- Run the canonical fail-fast gate with py -3 tools/run.py ci --check before publication.

## Review Focus

- Removing this test must not remove same-context determinism, runtime salt/entropy, or generator legality coverage; retain those existing behavior owners and compare the changed diff against ADR-0028 and PG-007.

---

### Task 1: Record N-44 delivery and commit the N-45 plan

**Files:**
- Modify: .agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md
- Modify: Directory.Build.props
- Modify: .agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md
- Modify: .agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md
- Delete: .agents/plans/2026-10-14-row17-n44-retire-redundant-spooked-horse-smoke.md
- Create: .agents/plans/2026-10-14-row17-n45-retire-composite-seed-profile-test.md

- [x] Record N-44 PR #347, reviewed source e8427428e09a2c3bbcc434bbe1fba8be447f72ec, squash merge 04657adb59d923d0409cadc13eb215eacb65e195, tree 7e643c8fc67d648d10635e318f096f8dd5ac9731, review result, exact-head gate 38542724501, develop-push gate 38543436116, local Domain/staged-gate results, and verified worktree/ref cleanup in row 17.
- [x] Select N-45, link this plan, and advance Directory.Build.props to 0.1.0-dev.163.
- [x] Retire N-44's completed plan and correct its investigation disposition from ADR-0029 to ADR-0028; keep the N-44 test result, behavioral owner, and no-product-change conclusion intact.
- [x] Commit the plan and successor metadata before modifying the test inventory.

### Task 2: Prove and retire the composite comparison

**Files:**
- Modify: tests/WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs
- Modify: .agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md
- Modify: .agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md

- [ ] Run GenerateChangesWhenSeedOrScenarioProfileChanges on the unmodified baseline and record the passing result.
- [ ] Temporarily omit GameSeed from ComposeSeed, run the focused test, and verify it still passes because ScenarioProfileId changes too; restore the source byte-for-byte.
- [ ] Temporarily omit ScenarioProfileId from ComposeSeed, run the focused test, and verify it still passes because GameSeed changes too; restore the source byte-for-byte.
- [ ] Remove only GenerateChangesWhenSeedOrScenarioProfileChanges; retain GenerateIsDeterministicForTheSameContext, live-state comparisons, fixed seeded legality tests, and actual salt/entropy behavior tests.
- [ ] Record the N-45 evidence and the remaining conditional-comparison findings without claiming seed/profile behavior from this test; state that production behavior, ADR-0028, and PG-007 are unchanged.
- [ ] Run the focused generator tests, the Domain suite, and the canonical fail-fast repository gate.

### Task 3: Independent review and delivery

- [ ] Compare the committed diff independently with ADR-0028, PG-007, the test dispositions, and selected backend/test unslop profiles; correct any false feature or architecture claim.
- [ ] Push the exact branch and open a Draft PR targeting develop; include the plan, evidence, no-ADR-change and no-feature-matrix-change rulings.
- [ ] Request fresh-context whole-branch review, resolve actionable findings, run hosted exact-head gates, then mark the PR ready and merge it to develop.
- [ ] Wait for and verify develop-push CI, fast-forward the primary checkout, retire the clean worktree before deleting the exact local/upstream branch refs, and verify all three are absent.
