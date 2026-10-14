# N-55 Renderer Boundary Test Correction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Remove renderer tests that treat absent property names or a fake Phaser key as proof of architecture, while retaining actual selection, callback, eligibility, fetch-observation, state-ownership and lifecycle behavior.

**Architecture:** React remains authoritative for application state and server coordination; Phaser renders supplied data and returns intent under ADR-0035. Keep the existing tests that observe real selector behavior, including the scoped `globalThis.fetch` observation, and remove only the assertions that cannot observe the claimed boundary. No production code or runtime contract changes.

**Tech Stack:** React, Phaser, Vitest, React Testing Library, Superpowers+ executing-plans.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [whole-test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [ADR-0035](../../docs/decisions/ADR-0035-react-shell-with-phaser-as-renderer-input-adapter-for-playfield-surfaces.md).

**Execution Strategy:** `executing-plans` (Native inline). The tasks are one coupled test-cleanup across three renderer test files and their two finding records; a single implementation context avoids repeating review and context setup for removals that share one ADR boundary. The roadmap explicitly authorizes inline JIT execution and requires one fresh whole-branch review.

## Global Constraints

- Start from `origin/develop`, use a fresh worktree created with the bundled `using-git-worktrees/scripts/new_worktree.py`, and target a PR into `develop`.
- Promote N-54 delivery and retire its completed plan in N-55's first substantive commit; advance only `Directory.Build.props` from `0.1.0-dev.172` to `0.1.0-dev.173`.
- Do not change production code, UI behavior, map selection, town-building selection, API usage, or Phaser/React ownership.
- Keep assertions that observe a real selection callback, valid/invalid selection, latest callback forwarding, scoped `fetch` behavior, state non-mutation, or host lifecycle.
- Do not describe a scoped `fetch` observation as proving the absence of every API or network path; do not replace removed property-name checks with another source-shape detector.
- ADR-0035 remains the architecture authority. Make no ADR or feature-matrix change unless implementation evidence shows a current durable claim became inaccurate.
- Run focused web tests, then rely on the normal check-only commit gate and hosted PR/develop gates for canonical validation; do not bypass hooks.

## Review Focus

- The remaining fetch assertion is limited to the exercised selector call and does not claim full network isolation; retain the exact callback assertion beside it in the same test.
- Host lifecycle and current callback forwarding remain independently observable in `PhaserTownHubHost.test.tsx` after deleting property-name inventories.
- `TownHubScene`'s fake Phaser base ignores its key argument, so the removed key test must not be replaced with a fake that merely echoes the requested key; keep the host's actual scene construction and selection behavior tests.
- Valid and ineligible town/building selection remain covered at their existing behavioral owners; no renderer legality or server authority changes.

---

### Task 1: Promote N-54 and establish N-55

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n55-renderer-boundaries.md`
- Delete: `.agents/plans/2026-10-14-row17-n54-start-map-recovery.md`

**Interfaces:**
- Consumes: merged PR #357 into `develop`, reviewed source `1c4903e9c7e62a1b99ed10d9e10b903ce8dc93b3`, squash merge `2c8121055a732624779a65b5a0b13ac01216c0cb`, exact-head CI run `38599473670`, develop-push CI run `38600905223`, independent clean review, primary checkout fast-forward, and verified removal of the N-54 worktree and both refs.
- Produces: promoted N-54 delivery evidence, active N-55 at `0.1.0-dev.173`, and retirement of the completed N-54 plan.

- [x] **Step 1: Revalidate N-54 integration and cleanup.** Confirm PR #357 is merged into `develop`, its recorded head and merge SHAs match, both hosted gates succeeded on those exact commits, the merge SHA is contained in `origin/develop`, the primary checkout is at the merge, and the N-54 worktree plus local/upstream refs are absent.
- [x] **Step 2: Promote N-54, activate N-55, and advance the version.** Add PR #357, source and merge SHAs, clean independent review, exact-head and develop-push CI runs, and paired worktree/ref cleanup evidence to row 17. Set the active plan/version to N-55 and `0.1.0-dev.173`. Change only the authored version in `Directory.Build.props`.
- [x] **Step 3: Retire the completed predecessor.** Remove the N-54 plan only after its delivery evidence has been promoted. Keep the baseline specification and investigation records.
- [x] **Step 4: Commit successor planning first.** Run `git diff --check`, review the complete staged diff, and commit the roadmap, version, N-55 plan, and N-54 plan retirement with `docs: plan N-55 renderer boundary corrections` before editing tests.

### Task 2: Remove ineffective renderer boundary assertions

**Files:**
- Modify: `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/PhaserTownHubHost.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/TownHubScene.test.ts`

**Interfaces:**
- Consumes: the existing map and town-hub selector callbacks, eligibility rules, Phaser host lifecycle tests, and ADR-0035's React-owned state/Phaser-intent boundary.
- Produces: renderer tests that assert observable selection behavior without presenting property absence or an ignored mock constructor argument as proof of architecture.

- [x] **Step 1: Establish the existing behavior baseline.** Run `npm --prefix src/WildBunch.Web test -- src/tests/PhaserMapHost.test.tsx src/tests/PhaserTownHubHost.test.tsx src/tests/TownHubScene.test.ts` before editing. Expected: all three suites pass. Do not add a new RED test: this slice removes claims that assert implementation shape and leaves behavior unchanged; the existing callback, eligibility, state, fetch-observation and lifecycle tests already own the retained outcomes.
- [x] **Step 2: Preserve the scoped map selector behavior.** In `PhaserMapHost.test.tsx`, rename the `truth boundary` group and selector test to describe the exercised selection callback and `fetch` observation precisely. Keep the valid town ID and one-call callback assertions and the `fetchSpy` assertion for that direct operation. Do not claim that the test excludes every API or network path.
- [x] **Step 3: Retain the town-hub host's real observable behavior.** In `PhaserTownHubHost.test.tsx`, remove only the test that inspects `api`, `requestJson`, `fetch`, and `getGame` properties. Keep and precisely rename the direct Store selection test, including callback and scoped `fetch` assertions; preserve tests for callback updates, unavailable layout, mount, data forwarding, and destroy behavior.
- [x] **Step 4: Retain town-hub scene behavior and remove fake proof.** In `TownHubScene.test.ts`, remove the test inspecting absent `api`, `requestJson`, and `fetch` properties. Rename the selector test to state only that an eligible Store selection invokes its callback without calling `globalThis.fetch` during that operation. Remove `uses the town-hub scene key`: the mocked `Phaser.Scene` discards its constructor argument, and the test only proves `instanceof` on the object just constructed. Preserve actual eligibility and selection tests.
- [x] **Step 5: Re-run the focused renderer suites.** Run `npm --prefix src/WildBunch.Web test -- src/tests/PhaserMapHost.test.tsx src/tests/PhaserTownHubHost.test.tsx src/tests/TownHubScene.test.ts`. Expected: all three suites pass; removed assertions have no replacement, and retained selectors/lifecycles continue to exercise their actual outcomes.

### Task 3: Record the W-04 disposition and compare durable truth

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Inspect only: `docs/decisions/ADR-0035-react-shell-with-phaser-as-renderer-input-adapter-for-playfield-surfaces.md`, `docs/features.md` and `.agents/playbooks/decision-records.md`.

**Interfaces:**
- Consumes: final test behavior and the current ADR-0035 React/Phaser ownership decision.
- Produces: dated W-04 disposition that records removed overclaims, retained scoped behavior proof, and whether any durable decision changed.

- [x] **Step 1: Record the test disposition.** Explain that absent property names did not prove API isolation; the fake Phaser base discarded the key, making its key assertion ineffective; the three scoped selector tests retain callback and direct `fetch` observations. State that no replacement architecture-shape test was added.
- [x] **Step 2: Verify current durable truth.** Confirm production still uses Phaser to render supplied data and return intent while React owns state and server coordination. Leave ADR-0035 and the feature matrix unchanged if that remains true; the disposition must say why their current claims remain accurate.
- [x] **Step 3: Validate the complete intended diff.** Run `git diff --check`, inspect the full source/test and planning-artifact diff, confirm no production files changed, then commit the test correction and dated dispositions as `test: correct renderer boundary assertions`.

### Task 4: Review, publish, integrate, and retire N-55

**Files:**
- Review: complete merge-base diff, N-55 plan, N-54 promotion, W-04 dispositions, ADR-0035 comparison, feature-matrix comparison, and exact focused/canonical evidence.
- Modify: the roadmap with N-55 final delivery evidence only in its next successor's first substantive commit.

**Interfaces:**
- Consumes: committed test corrections and normal check-only hook validation.
- Produces: N-55 integrated into `develop` at `0.1.0-dev.173` with independent whole-branch review and passing hosted gates.

- [ ] **Step 1: Check the exact worktree and diff.** Verify version `0.1.0-dev.173`, clean committed state, focused renderer suites and hooked canonical validation on the final implementation head.
- [ ] **Step 2: Obtain independent review.** Dispatch a fresh-context reviewer for the full merge-base diff, including the independent ADR and feature-matrix truth comparison. Resolve actionable findings with appropriate behavior evidence and repeat review on the changed head.
- [ ] **Step 3: Publish and verify.** Push the branch, create a Draft PR to `develop`, verify body/base/head and exact source SHA, attach it when supported, mark it ready after review, and require the exact-head hosted gate to pass.
- [ ] **Step 4: Integrate and clean up.** Squash-merge to `develop`, require the develop-push gate on the merge SHA to pass, fast-forward the primary checkout while preserving its pre-existing investigation-note edit, then remove this clean worktree with the `finishing-a-development-branch` helper. Delete the exact local and upstream refs after worktree removal and verify all three absences.

## Acceptance

- Property-name absence checks and the fake-key smoke are removed without replacement shape detectors.
- Existing valid/invalid selection, callback forwarding, scoped `fetch`, state non-mutation, and host lifecycle behavior remains covered and passes.
- The scoped fetch assertions no longer claim full API/network isolation.
- No production behavior, feature promise, API contract, or ADR decision changes; W-04 dispositions state why ADR-0035 remains accurate.
- N-54 is promoted and retired only after its exact PR, review, hosted gates, fast-forward, and paired cleanup facts are verified.
- The implementation is independently reviewed, hosted CI passes on PR head and develop merge SHA, and the worktree plus local/upstream refs are removed together.
