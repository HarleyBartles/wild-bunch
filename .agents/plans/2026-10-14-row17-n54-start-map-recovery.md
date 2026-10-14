# Row 17 N-54: Starting-Town Map Recovery States

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Make the starting-town map distinguish a real request in progress from a settled failed or unusable result, and let the player recover from a transient map read failure.

**Architecture:** Keep the existing `StartingTownStep` query as the sole owner of its world-map read. Show the loading state only while the request is actually pending. A failed read or a settled response with no selectable towns is a visible unavailable state, with retry when a session identity exists. Keep the loaded map and its single selection callback unchanged. Do not introduce another phase machine, map query, global error state, or renderer behavior.

**Tech Stack:** React, TanStack Query, styled-components, Vitest, React Testing Library.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [PG-003 map and town navigation](../../docs/features.md); [web-layer investigation](../investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md); [web-test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md).

**Execution Strategy:** `executing-plans` (native inline). Preserve the genuinely pending case, prove settled empty and failed results do not remain in a loading state, and prove retry by resolving a second real query attempt. Remove the duplicate map-render smoke; keep the independent successful map display contract.

## Global Constraints

- Do not change the map endpoint, world-map query key, server authority, settled world facts, setup/prologue/town-selection phase order, or first-arrival command.
- Keep the current query's no-automatic-retry policy; recovery is a deliberate player action.
- Treat a successful response with no towns as unavailable because town selection cannot proceed. Do not fabricate a town or silently advance the game.
- Offer retry only when a session ID is present; a disabled query cannot be retried into a valid session.
- Do not add map-level keyboard or screen-reader town selection; ADR-0046 excludes that work from 0.1.0.
- Own the next development identity only in `Directory.Build.props` at `0.1.0-dev.172`; target the fresh `develop` worktree and PR back to `develop`.
- Require independent whole-branch review, green exact-head hosted CI, green develop-push CI, primary-checkout fast-forward, and paired worktree/local/upstream branch cleanup before retiring this worktree.

## Review Focus

- The genuine pending request still presents the existing loading message and no retry action.
- A rejected request and an empty settled map each present a non-loading unavailable state; an error cannot masquerade as indefinite loading.
- Clicking retry issues a deliberate second request, remains pending until it settles, and displays the map when the retry returns selectable towns.
- A missing session ID does not issue a request or offer an ineffective retry action.
- The populated map display and town-selection intent remain unchanged; remove only duplicated render smoke coverage.

---

### Task 1: Promote N-53 and establish N-54

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n54-start-map-recovery.md`
- Delete: `.agents/plans/2026-10-14-row17-n53-world-map-contract.md`

**Interfaces:**
- Consumes: merged N-53 PR #356, reviewed source `101ac8fdf9d183531327b54e64451123403e790e`, squash merge `aa1e636a02b3a2b03526c2b9167b4d5f6a07b251`, identical tree `d97d775edb04810b732f6a057e76bbcf7c37c560`, successful exact-head run `38594530408`, successful develop-push run `38595186102`, and verified removal of its worktree and paired branch refs.
- Produces: promoted N-53 delivery evidence, active N-54 at `0.1.0-dev.172`, and retirement of the completed N-53 plan.

- [x] **Step 1: Revalidate N-53 integration and cleanup.** Confirm PR #356 is merged into `develop`, its recorded source and merge SHAs match, both hosted gates passed on those exact commits, both commits have the same tree, the primary checkout is at the merge, and the N-53 worktree plus local/upstream refs are absent.
- [x] **Step 2: Promote N-53, activate N-54, and advance the version.** Add PR #356, the clean independent review, tree, exact-head CI, develop-push CI, and paired cleanup evidence to row 17. Set the active plan/version to N-54 and `0.1.0-dev.172`. Change only the authored version in `Directory.Build.props`.
- [x] **Step 3: Retire the completed predecessor.** Remove the N-53 plan only after its delivery evidence has been promoted. Keep the baseline specification and investigation records.
- [x] **Step 4: Commit successor planning first.** Run `git diff --check`, review the complete staged diff, and commit the roadmap, version, N-54 plan, and N-53 plan retirement with `docs: plan N-54 starting map recovery` before editing implementation or tests.

### Task 2: Specify map request states in the owning component

**Files:**
- Modify: `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx`
- Inspect only: `src/WildBunch.Web/src/components/start-flow/StorySoFarStep.tsx`, `src/WildBunch.Web/src/components/ui/sharedStyled.ts`, and `src/WildBunch.Web/src/api/wildBunchApi.ts`.

**Interfaces:**
- Consumes: `getWorldMap(sessionId)`, the current TanStack Query lifecycle, and the existing starting-town step.
- Produces: explicit pending, unavailable, and populated presentation states with deliberate retry for a session-bound unavailable map.

- [x] **Step 1: Replace the combined pending predicate.** Use the actual fetching/pending status for the loading presentation. Render a player-facing unavailable message for a rejected request, an unusable settled result, or absent session identity. Keep the existing loaded-map and selection behavior.
- [x] **Step 2: Add deliberate recovery.** For failed or empty results with a session ID, offer an accessible `Try again` button that calls this query's `refetch`; prevent duplicate retry while it is fetching. Do not retry automatically or expose retry for a disabled sessionless query.
- [x] **Step 3: Preserve component ownership.** Keep the error/retry display local to `StartingTownStep`, using its own styled contracts. Do not add shared state, another route step, or parent reach-through styling.

### Task 3: Prove the player-visible state transitions

**Files:**
- Modify: `src/WildBunch.Web/src/tests/StartingTownStep.test.tsx`.

**Interfaces:**
- Consumes: `StartingTownStep`, real TanStack Query behavior through a test `QueryClient`, and the mocked API boundary.
- Produces: independent evidence for true pending, failed and empty results, retry recovery, missing identity, and populated map display.

- [x] **Step 1: Keep one successful map display scenario.** Retain a single test that waits for the map host after the API resolves with towns; remove the duplicate test that asserts the same role and outcome.
- [x] **Step 2: Retain true pending behavior.** Hold the first API promise unresolved and assert the existing loading state while the request is in flight; settle the promise during cleanup so no work leaks across tests.
- [x] **Step 3: Prove rejected and empty results settle visibly.** Reject a deferred request and resolve another with an empty town list. Assert each shows the unavailable state rather than the loading message; assert retry is offered only when the session ID exists.
- [x] **Step 4: Prove deliberate retry recovery.** Click retry after failure and after empty data in the relevant focused cases. Assert a second API call occurs, the pending presentation is shown while it is unresolved, and a populated response produces the map host. A mutation removing the retry callback must fail because the second attempt and map recovery never occur.
- [x] **Step 5: Prove missing identity fails closed.** Render with an empty session ID, assert the API is not called, no loading state persists, and no retry button is offered.
- [x] **Step 6: Run focused behavior and full web checks.** Run `npm --prefix src/WildBunch.Web test -- src/tests/StartingTownStep.test.tsx`, then `py -3 tools/run.py web --check` for web formatting, lint, typecheck, tests and build. Run `py -3 tools/run.py ci --check` on the final tree.

### Task 4: Reconcile the finding and verify durable truth

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Inspect only: `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, `docs/features.md` PG-003, ADR-0039, ADR-0046, and `.agents/playbooks/decision-records.md`.

**Interfaces:**
- Consumes: final pending/unavailable/retry/map behavior and the existing PG-003 and one-map decisions.
- Produces: a dated N-54 disposition for the empty/error loading assertion and duplicate map-render smoke without changing the product promise or durable decisions.

- [x] **Step 1: Record the behavioral disposition.** Explain that only an active request is loading; failures, empty maps, and missing session identity are not pending, and session-bound failures have deliberate retry. Record removal of the duplicate renderer smoke while retaining the populated-map proof.
- [x] **Step 2: Compare durable truth.** Confirm the component still displays the single settled world map, the first town selection remains the existing first-arrival action, PG-003 remains accurate, and ADR-0039/ADR-0046 do not change. Update no ADR or feature row unless the implementation exposes a real durable decision mismatch.
- [x] **Step 3: Validate final changed files.** Run the supported web gate, `git diff --check`, and the full canonical repository gate. Confirm generated/ignored outputs did not alter maintained files.

### Task 5: Review, publish, integrate, and retire N-54

**Files:**
- Review: complete merge-base diff, plan, roadmap, N-53 delivery disposition, N-54 test disposition, player-facing map query states, PG-003, ADR-0039/0046, and focused/canonical evidence.
- Modify: the roadmap with N-54 final delivery evidence only in its next successor's first substantive commit.

**Interfaces:**
- Consumes: independent status-transition tests, deliberate retry proof, missing-identity negative, and final web/canonical checks.
- Produces: N-54 integrated into `develop` at `0.1.0-dev.172` with independent review and passing hosted gates.

- [ ] **Step 1: Check the exact worktree and diff.** Verify version `0.1.0-dev.172`, clean committed state, focused web and full canonical gate evidence on the final implementation head.
- [ ] **Step 2: Obtain independent review.** Dispatch a fresh-context reviewer for the full merge-base diff, including the ADR and feature-matrix truth comparison. Resolve actionable findings with behavior proof and repeat review on the changed head.
- [ ] **Step 3: Publish and verify.** Create a Draft PR to `develop`, attach it in Codex, verify its body and exact head, mark it ready after review, and require exact-head hosted CI to pass.
- [ ] **Step 4: Integrate and clean up.** Squash-merge to `develop`, require the develop push gate on the merge SHA to pass, fast-forward the primary checkout without discarding its pre-existing uncommitted investigation edit, and remove this clean worktree with the `finishing-a-development-branch` helper. Delete the exact local and upstream refs after worktree removal and verify all three absences.

## Acceptance

- A truly pending world-map request remains visibly loading until it settles.
- A rejected request, an empty settled world map, or absent session identity never masquerades as ongoing loading; session-bound failure offers a deliberate retry that can load and display the populated map.
- Missing session identity issues no API request and has no ineffective retry action.
- One meaningful successful map-render test remains; the duplicate render smoke is removed.
- No API, domain, event, persistence, phase, map-selection, or accessibility contract changes.
- Review, exact-head hosted gate, develop-push gate, primary fast-forward, and paired worktree/branch cleanup are verified.
