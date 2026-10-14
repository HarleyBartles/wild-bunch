# N-56 Background Placement Test Correction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Correct W-05's background-building planner tests so each retained claim is independently observable and matches the current deterministic slot and prosperity-budget behavior.

**Architecture:** This slice changes tests and dated investigation findings only. `collectEligibleBackgroundSlots` remains the owner of road/spur candidate eligibility and trailhead exclusion; `planBackgroundBuildings` remains the owner of seeded prosperity counts. Generated layout persistence and the player's world-map/town-hub flow remain unchanged.

**Tech Stack:** React web package, TypeScript, Vitest, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [whole-test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [PG-003](../../docs/features.md#pg-003-starting-town-and-world-map).

**Execution Strategy:** `executing-plans` (Native inline). The findings concern three assertions in one test file and one shared slot-eligibility boundary; one inline context keeps the fixture and behavior oracle coherent while mutation checks exercise both corrected invariants. The roadmap authorizes JIT inline execution and requires one fresh whole-branch review.

## Global Constraints

- Start from `origin/develop` in a fresh worktree created with the bundled `using-git-worktrees/scripts/new_worktree.py`; target the PR to `develop`.
- Promote N-55 and retire its completed plan in N-56's first substantive commit; advance only the authored version in `Directory.Build.props` from `0.1.0-dev.173` to `0.1.0-dev.174`.
- Keep this a test-quality correction. Do not change production layout generation, prosperity behavior, player UI, APIs, or event/persistence contracts.
- Preserve the existing prosperity ranges. The current Boomtown range fills all but at most two eligible slots; it can fill every eligible slot.
- Retain deterministic layout behavior. Do not convert these internal salt fixtures into a public seed or compatibility promise.
- Keep ADR-0039 and PG-003 unchanged if the implementation still preserves settled generated layouts and the existing world-map/town-hub promise.
- Use `py -3 tools/run.py web --check` to install locked web dependencies and run the web validation lane; use the focused Vitest command for this test owner. Normal commits must pass the staged check-only hook.

## Review Focus

- The trailhead exclusion test must independently reject every actual forbidden road slot and still prove that unrelated eligible road candidates exist; disabling the exclusion guard must fail it.
- The fixed prosperity fixtures must make both Destitute's maximum and Boomtown's minimum observable; loosening either bound must fail the relevant test.
- The seed sweep must continue checking each seed's budget while no longer asserting that a set populated unconditionally inside the same loop is nonempty.
- Follow the scoped [unslop playbook](../playbooks/unslop.md), [testing playbook](../playbooks/testing.md), [decision-record playbook](../playbooks/decision-records.md), [PR runbook](../runbooks/pr.md), and [code-review runbook](../runbooks/code-review.md); the code-review lane remains a fresh whole-branch review.

---

### Task 1: Promote N-55 and establish N-56

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n56-background-placement-assertions.md`
- Delete: `.agents/plans/2026-10-14-row17-n55-renderer-boundaries.md`

**Interfaces:**
- Consumes: merged PR #358 into `develop`, reviewed source `a6af00fcd01da5f4284e23fee2242660fd3ae569`, squash merge `17bf7815e88880cbf23a064e28d9556511d5236d`, exact-head gate run `38605484845`, develop-push gate run `38606332137`, clean independent review, primary checkout fast-forward, and verified removal of N-55's worktree and both branch refs.
- Produces: promoted N-55 delivery evidence, active N-56 at `0.1.0-dev.174`, and retirement of the completed N-55 plan.

- [x] **Step 1: Revalidate N-55 integration and cleanup.** Confirm PR #358 is merged into `develop`, its source and merge SHAs match, both hosted gates succeeded on those commits, the merge SHA is contained in `origin/develop`, the primary checkout is at the merge, and the N-55 worktree plus local/upstream refs are absent. Confirm the existing AppShell investigation edit is still preserved in the primary checkout.
- [x] **Step 2: Promote N-55, activate N-56, and advance the version.** Add PR #358, source and merge SHAs, clean independent review, exact-head and develop-push CI runs, and paired worktree/ref cleanup evidence to row 17. Set the active plan/version to N-56 and `0.1.0-dev.174`. Change only the authored version in `Directory.Build.props`.
- [x] **Step 3: Retire the completed predecessor.** Remove the N-55 plan only after its delivery evidence has been promoted. Keep the baseline specification and investigation records.
- [x] **Step 4: Commit successor planning first.** Run `git diff --check`, inspect the complete staged planning diff, and commit the roadmap, version, N-56 plan, and N-55 plan retirement as `docs: plan N-56 background placement assertions` before editing tests.

### Task 2: Repair W-05 background planner assertions

**Files:**
- Modify: `src/WildBunch.Web/src/components/town-hub/background-building-planner.test.ts`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Inspect only: `src/WildBunch.Web/src/components/town-hub/background-building-planner.ts`, `src/WildBunch.Web/src/components/town-hub/town-hub-geometry.ts`, `docs/decisions/ADR-0039-seeded-game-setup-and-free-first-arrival.md`, `docs/features.md`, and `.agents/playbooks/decision-records.md`.

**Interfaces:**
- Consumes: the planner's current road/spur candidate rules, trailhead-footprint exclusion, and seeded `TownProsperity` budget ranges.
- Produces: behavioral assertions that independently reject forbidden slots, retain eligible positive controls, state the existing Boomtown range truthfully, and keep only useful per-seed budget checks.

- [ ] **Step 1: Reconfirm the test baseline before editing.** Run `npm --prefix src/WildBunch.Web test -- src/components/town-hub/background-building-planner.test.ts`. The current baseline passed all 10 tests. If dependencies are absent, first run `py -3 tools/run.py web --check`, which installs locked web dependencies, then rerun the focused command. Do not alter production code to make a test pass.
- [ ] **Step 2: Make trailhead exclusions and positive controls explicit.** In `excludes the immediate east and west slots beside both trailheads from background placement`, add road cells at row 4, columns 4 and 5. Replace the negated `arrayContaining` matcher with individual slot-key assertions; the four forbidden keys are `0:3`, `0:6`, `9:3`, and `9:6`, while `4:3` and `4:6` are positive controls. Do not call rows 1 or 8 forbidden: this fixture has no road/spur candidate source there.

```typescript
const slotKeys = new Set(slots.map(({ row, col }) => `${row}:${col}`));
for (const key of ["0:3", "0:6", "9:3", "9:6"]) {
  expect(slotKeys.has(key)).toBe(false);
}
expect(slotKeys.has("4:3")).toBe(true);
expect(slotKeys.has("4:6")).toBe(true);
```

- [ ] **Step 3: Make fixed prosperity boundaries observable.** Remove the Destitute `placements.length >= 0` assertion, retain its maximum of two, and set `buildingsSalt` to `n56-destitute-13`; this fixture has six eligible slots and currently places two, while raising the maximum to three places three. Rename the Boomtown test to say it leaves at most two eligible spaces empty, assert the fixture has more than two eligible spaces, and keep the exact current interval from `eligibleCount - 2` through `eligibleCount`. Set `buildingsSalt` to `n56-boomtown-4`; this fixture has ten eligible slots and currently places eight, while reducing the minimum by one places seven.

```typescript
expect(eligibleCount).toBeGreaterThan(2);
expect(placements.length).toBeGreaterThanOrEqual(eligibleCount - 2);
expect(placements.length).toBeLessThanOrEqual(eligibleCount);
```
- [ ] **Step 4: Remove the guaranteed seed-set assertion.** Delete `observedCounts`, its unconditional `add` call, and `observedCounts.size > 0`. Keep the 64 independent seed cases and both per-case budget bounds for each prosperity tier.
- [ ] **Step 5: Prove the corrected oracles fail for their intended mutations.** With the test edits in place, temporarily bypass `collectEligibleBackgroundSlots`' trailhead-exclusion guard and run the focused suite; the individual forbidden-slot assertion must fail while a positive control remains present. Restore production byte-for-byte. Temporarily reduce Boomtown's minimum by one and increase Destitute's maximum by one; the fixed fixtures must fail their respective budget assertions. Restore production byte-for-byte and rerun the focused suite successfully. Do not commit any production mutation.
- [ ] **Step 6: Record the W-05 disposition and compare durable truth.** In both investigation records, explain the original multi-value negated matcher could pass with most forbidden slots present, the rows 1/8 candidates were unreachable in its fixture, the positive controls and individually absent actual candidates now own trailhead exclusion, and the fixed salts make the current prosperity bounds mutation-sensitive. State that the in-loop set-size assertion was guaranteed by construction. Confirm no production behavior, ADR-0039 decision, generated-layout persistence promise, or PG-003 feature disposition changed.
- [ ] **Step 7: Validate the complete intended diff and commit.** Run `git diff --check`, inspect the full diff, confirm production files are byte-for-byte restored and no product/source contract changed, then commit the test and dated dispositions as `test: correct background placement assertions`.

### Task 3: Review, publish, integrate, and retire N-56

**Files:**
- Review: complete merge-base diff, N-56 plan, N-55 promotion, W-05 dispositions, ADR-0039 comparison, PG-003 comparison, and exact focused/canonical evidence.
- Modify: the roadmap with N-56 final delivery evidence only in its next successor's first substantive commit.

**Interfaces:**
- Consumes: committed test corrections and normal check-only hook validation.
- Produces: N-56 integrated into `develop` at `0.1.0-dev.174` with independent whole-branch review and passing hosted gates.

- [ ] **Step 1: Check the exact worktree and diff.** Verify version `0.1.0-dev.174`, clean committed state, focused background-planner suite and hooked canonical validation on the final implementation head.
- [ ] **Step 2: Obtain independent review.** Dispatch a fresh-context reviewer for the full merge-base diff, including the independent ADR and feature-matrix truth comparison. Resolve actionable findings with appropriate behavior evidence and repeat review on the changed head.
- [ ] **Step 3: Publish and verify.** Push the branch, create a Draft PR to `develop`, verify body/base/head and exact source SHA, attach it when supported, mark it ready after review, and require the exact-head hosted gate to pass.
- [ ] **Step 4: Integrate and clean up.** Squash-merge to `develop`, require the develop-push gate on the merge SHA to pass, fast-forward the primary checkout while preserving its pre-existing investigation-note edit, then remove this clean worktree with the `finishing-a-development-branch` helper. Delete the exact local and upstream refs after worktree removal and verify all three absences.

## Acceptance

- Every actual trailhead-adjacent road candidate is individually excluded while unblocked positive-control candidates remain eligible.
- Destitute's maximum and Boomtown's `eligibleCount - 2` minimum are independently observable under deterministic fixtures; each test fails when its boundary is relaxed.
- The per-seed range sweep still checks all 64 seeds for each prosperity tier without a self-guaranteed nonempty-set assertion.
- No production behavior, player promise, ADR decision, API contract, or persistence rule changes; dated W-05 dispositions explain why ADR-0039 and PG-003 remain accurate.
- N-55 is promoted and retired only after exact PR, review, hosted gates, fast-forward, and paired cleanup facts are verified.
- The implementation is independently reviewed, hosted CI passes on PR head and develop merge SHA, and the worktree plus local/upstream refs are removed together.
