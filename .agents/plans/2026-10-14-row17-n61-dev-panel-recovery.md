# N-61 Prove developer panel mutation recovery

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Close the remaining W-09 web-test gap by proving that every live developer panel mutation reports rejection, preserves server-owned state, permits retry, and displays refreshed state after success.

**Architecture:** The Session, Travel, and Saloon panels render developer DTOs returned by their TanStack Query reads. Their commands do not change the panel's authority: failed commands leave the displayed read model unchanged, and successful commands refresh it from the backend. Keep this slice in the web tests unless a behavior test exposes a concrete production defect; do not add optimistic game state or alter domain/event contracts.

**Tech Stack:** React, TanStack React Query, Vitest, and Testing Library.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [whole-test quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [developer control feature record](../../docs/features.md); [developer overlay doctrine](../playbooks/dev-overlay.md); [frontend standards](../doctrine/frontend-standards.md); [validation policy](../doctrine/validation-policy.md).

**Execution Strategy:** `executing-plans` (Native inline). The three panel test files cover one W-09 behavior boundary and share the same event-backed readback rule. Sequential inline execution keeps mutation proof and mock-state ownership coherent; splitting these tests across fresh implementers would add review and context overhead without independent production deliverables.

## Global Constraints

- Start from refreshed `origin/develop` in a fresh worktree created with the bundled `using-git-worktrees/scripts/new_worktree.py`; target the PR to `develop`.
- Promote N-60 delivery evidence in this first commit, retire its completed plan, and advance only the authored version in `Directory.Build.props` from `0.1.0-dev.178` to `0.1.0-dev.179`.
- Treat Session, Travel, and Saloon developer DTOs as authoritative reads. Rejection must not fabricate or clear server-owned state; successful commands refresh context before the panel shows the new state.
- Preserve each existing dev command, endpoint, eligibility rule, one-shot event behavior, developer-only boundary, and normal player flow. No domain, Application, API, Persistence, ADR, or feature-matrix change is expected.
- Do not add a pending-state UI to SessionDevPanel; its current contract does not define one. For Travel and Saloon, preserve the existing shared pending guard and prove it releases after rejection.
- Run `py -3 tools/run.py ci --check` with PostgreSQL ensured. The canonical gate and hosted checks remain check-only and fail-fast.

## Review Focus

- A rejected mutation must show its actual error and keep the last backend-read state visible; test both the setting and clearing paths for Travel and Saloon, and difficulty and entropy for Session.
- A retry must call the same dev command again, clear the prior error, and show the new state only after the context query refreshes.
- Travel and Saloon controls must be disabled while their existing mutation is pending and usable again after rejection.

---

### Task 1: Promote N-60 and establish N-61

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n61-dev-panel-recovery.md`
- Delete: `.agents/plans/2026-10-14-row17-n60-startup-recovery.md`

**Interfaces:**
- Consumes: merged PR #363, reviewed source `330a4c06356dea8427a0b4dbfc5f3beb1770a95a`, squash merge `b0af13c2ae6b957162164009a5ee0762df8f190d`, exact-head gate `38632606203`, develop-push gate `38633439165`, clean independent review, primary fast-forward with the user's web-test-followup edit preserved, and verified removal of N-60's worktree and both branch refs.
- Produces: promoted N-60 delivery evidence, active N-61 at `0.1.0-dev.179`, and retirement of the completed N-60 plan.

- [ ] **Step 1: Revalidate N-60 integration and cleanup.** Verify PR #363 is merged to `develop`, its recorded head and merge SHAs match, both hosted runs passed on those commits, the merge is contained in `origin/develop`, the primary checkout is at the merge with the pre-existing user edit intact, and N-60's worktree plus exact local/upstream refs are absent.
- [ ] **Step 2: Promote N-60, activate N-61, and advance the version.** Add N-60's PR, source and merge SHAs, independent review, exact-head and develop-push gate runs, and paired worktree/ref cleanup evidence to row 17. Set the active plan/version to N-61 and `0.1.0-dev.179`. Change only the authored version in `Directory.Build.props`.
- [ ] **Step 3: Retire the completed predecessor.** Remove the N-60 plan only after its delivery evidence is promoted; keep the specification and investigation records.
- [ ] **Step 4: Commit successor planning first.** Check whitespace and inspect the staged planning diff, then commit the roadmap, version, N-61 plan, and N-60 plan retirement as `docs: plan N-61 dev panel recovery` before editing tests or source.

### Task 2: Prove Session developer mutation recovery

**Files:**
- Modify: `src/WildBunch.Web/src/tests/SessionDevPanel.test.tsx`
- Review and change only if a behavior test finds a defect: `src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx`

**Interfaces:**
- Consumes: `forceDevDifficulty`, `setDevEntropy`, `getSessionDevContext`, and `gameResourceQueryKeys.devSession` refresh behavior.
- Produces: behavior tests for rejected-then-successful difficulty and entropy commands, including server-owned readback.

- [ ] **Step 1: Add the difficulty rejection and retry test.** Give `getSessionDevContext` a Standard response followed by a Brutal response; reject the first `forceDevDifficulty` call with a concrete message and resolve the retry. Assert the rejection message appears, Standard remains pressed and Brutal remains unpressed before retry, and the command can be retried. After retry, assert the refreshed DTO selects Brutal, the old error is gone, and the command was called twice with the same game and requested difficulty.
- [ ] **Step 2: Add the entropy rejection and retry test.** Give the context query a Classic response followed by a Wild response; reject then resolve `setDevEntropy`. Assert Classic stays selected after rejection, retry succeeds, refreshed readback selects Wild, the error is gone, and both attempts carry the same game and requested entropy.
- [ ] **Step 3: Run and mutation-prove the Session cases.** Run `npm exec -- vitest run src/tests/SessionDevPanel.test.tsx`. Temporarily remove the difficulty handler's error capture and separately suppress the entropy handler's context refresh; each corresponding test must fail at its error or authoritative-readback assertion. Restore production source byte-for-byte and rerun the file.

### Task 3: Prove Travel and Saloon override recovery

**Files:**
- Modify: `src/WildBunch.Web/src/tests/TravelDevPanel.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/SaloonDevPanel.test.tsx`
- Review and change only if a behavior test finds a defect: `src/WildBunch.Web/src/dev/panels/TravelDevPanel.tsx`
- Review and change only if a behavior test finds a defect: `src/WildBunch.Web/src/dev/panels/SaloonDevPanel.tsx`

**Interfaces:**
- Consumes: each panel's force/clear command, query-backed dev context, visible error, and existing shared `actionPending` guard.
- Produces: force and clear failure/retry behavior tests proving unchanged readback after rejection and refreshed readback after success.

- [ ] **Step 1: Cover Travel force recovery.** In `TravelDevPanel.test.tsx`, provide a context with no pending override and a second context with the forced override. Hold the first command pending long enough to assert both action buttons are disabled, then reject it. Assert the error is shown, no override appears, and both buttons are enabled again. Retry successfully and assert the refreshed override appears and the error disappears.
- [ ] **Step 2: Cover Travel clear recovery.** Start with a pending travel override and return the same context after a rejected clear, then a context with no override after retry. Assert the rejected call leaves the displayed override intact and restores both controls; successful retry refreshes to `None pending.` and clears the error.
- [ ] **Step 3: Cover Saloon force recovery.** In `SaloonDevPanel.test.tsx`, provide one eligible suspect and no pending override, select it from the existing force-suspect control, and hold the first force call pending long enough to assert both action buttons are disabled. Reject it, assert its error and unchanged `None pending.` state, and verify both controls are usable. Then return a context containing the selected suspect in the pending override after successful retry; assert the suspect name appears only after query refresh and the old error disappears.
- [ ] **Step 4: Cover Saloon clear recovery.** Start with a pending override and provide the same context on the rejected clear, followed by no override after the retry. Assert rejection preserves the existing displayed override and restores controls; successful retry refreshes to `None pending.` and clears the error.
- [ ] **Step 5: Run and mutation-prove all override cases.** Run `npm exec -- vitest run src/tests/TravelDevPanel.test.tsx src/tests/SaloonDevPanel.test.tsx`. Temporarily remove Travel's force `finally` reset and confirm the control-restoration assertion fails; separately remove Saloon's clear error capture and confirm its rejection test fails. Restore production source byte-for-byte and rerun both files.

### Task 4: Record the W-09 disposition and complete validation

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Review: Session, Travel, and Saloon panel tests and their current command handlers.

**Interfaces:**
- Consumes: passing focused behavior tests and mutation proof from Tasks 2 and 3.
- Produces: dated W-09 evidence that identifies the live panels covered, the observed rejection/retry/readback contracts, and whether production behavior changed.

- [ ] **Step 1: Add the dated W-09 disposition.** Record the exact tests and mutation proofs, state that the current Session, Travel, and Saloon handlers already satisfied the tested behavior if no production fix was needed, and confirm Town Layout and Session RNG lock/clear remain retired. Do not turn the evidence into a test or feature inventory.
- [ ] **Step 2: Resolve author decision and feature checks.** Compare the diff against ADR-0030, ADR-0031, ADR-0032 and ADR-0041 and against the relevant developer-tool and deployment-boundary entries in `docs/features.md`. State in the PR that the tests do not change any durable decision, 0.1.0 capability, or deployment boundary; do not edit those authorities absent evidence of a current mismatch.
- [ ] **Step 3: Validate the final tree.** From `src/WildBunch.Web`, run `npm exec -- vitest run src/tests/SessionDevPanel.test.tsx src/tests/TravelDevPanel.test.tsx src/tests/SaloonDevPanel.test.tsx`, `npm exec -- prettier --check src/tests/SessionDevPanel.test.tsx src/tests/TravelDevPanel.test.tsx src/tests/SaloonDevPanel.test.tsx`, and `npm exec -- eslint --config eslint.config.js src/tests/SessionDevPanel.test.tsx src/tests/TravelDevPanel.test.tsx src/tests/SaloonDevPanel.test.tsx`; from the repository root run `git diff --check`, `.\\tools\\postgres-dev.ps1 ensure`, and then `py -3 tools/run.py ci --check`. Preserve mutation logs only in the branch-scoped `_agent-scratch` task ledger.
- [ ] **Step 4: Obtain an independent whole-branch review.** Review against the dev-overlay profile, code-review profile, relevant ADRs and feature matrix. Fix actionable issues with behavior proof and obtain a fresh clean review.
- [ ] **Step 5: Publish and verify.** Push the branch and create a Draft PR to `develop`; verify PR body, base, head and SHA. Mark ready after clean review and require exact-head hosted CI to pass.
- [ ] **Step 6: Integrate and close out.** Squash-merge to `develop`, require the develop-push gate on its merge SHA, fast-forward the primary checkout while preserving the user's edit, remove the clean worktree with the `finishing-a-development-branch` helper, delete the exact local/upstream refs, and verify all are absent.

## Acceptance

- Session difficulty and entropy rejection leaves the backend-projected selection visible; retry uses the intended command and displays the new selection only after context refresh.
- Travel and Saloon force and clear rejection preserve the last backend-read override state and restore the existing shared controls; successful retry displays the refreshed override state and removes the prior error.
- The tests fail under their specified behavior-breaking mutations and pass after source restoration.
- No player behavior, developer endpoint/event contract, durable decision, feature promise, or developer deployment boundary changes.
- N-60 delivery is promoted and its plan retired only after PR #363, both hosted gates, independent review, primary fast-forward, and paired cleanup are verified.
- N-61 is independently reviewed, both hosted gates pass on their exact commits, and its worktree plus exact branch refs are removed together.
