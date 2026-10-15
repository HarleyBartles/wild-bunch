# N-65 Consolidate the truthful start-flow success scenario Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep one strong rendered success journey for setup, prologue, and starting-town selection, while preserving distinct failure, retry, refresh, and no-selection behavior tests.

**Architecture:** The backend session phase remains authoritative after each command. The start-flow test must prove the exact submitted setup choices and selected town, and must hold first arrival pending long enough to establish that the creating state is shown before a response. Do not add a second client-side source of truth or alter production flow behavior.

**Tech Stack:** TypeScript, React Testing Library, Vitest, TanStack Query, Prettier, ESLint, and the Wild Bunch validation command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [W-08 whole-test finding](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web-test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [PG-002 playthrough lifecycle contract](../investigations/stable-0.1.0/2026-10-07-playthrough-lifecycle-contract.md); [frontend standards](../doctrine/frontend-standards.md); [validation policy](../doctrine/validation-policy.md).

**Execution Strategy:** `executing-plans` (Native inline). This is a bounded test-owner consolidation with one meaningful pending-state mutation proof and a dated audit disposition. The authorized roadmap goal requires inline execution.

## Global Constraints

- Start from refreshed `origin/develop` in this fresh canonical worktree and target the PR to `develop`.
- Promote N-64's merged delivery, retire its completed plan, and advance the sole authored application version once to `0.1.0-dev.183` in `Directory.Build.props`.
- Keep the server as the authority for setup, prologue acknowledgment, first arrival, and resumed phase; do not change production code or invent a second client-side phase machine.
- Preserve distinct saved-session refresh, rejected/uncertain command recovery, invalid-seed rejection, no-town-selected, and no-Back behavior coverage.
- Remove only redundant successful walks and assertions already proven by the canonical success scenario. Do not change product copy, APIs, events, ADRs, or feature promises.
- Keep generated artifacts and test-result receipts out of the commit. Preserve the user's uncommitted edit in the primary checkout by not touching its file.
- Run focused tests and formatting/lint checks, then the canonical fail-fast `py -3 tools/run.py ci --check` on the committed candidate.

## Review Focus

- The canonical rendered flow submits the entered name, seed, difficulty, and entropy; acknowledges the prologue; and starts only after selecting a town through the map host.
- A held first-arrival response keeps the creating state visible while the request is pending. The test must fail if production moves to creating only after the response.
- Existing separate tests remain the owners for error recovery, refresh/reconciliation, invalid input, absent town selection, and route reachability. The canonical scenario does not claim to prove event persistence or replay.

---

### Task 1: Promote N-64 and establish N-65

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-15-row17-n65-startflow-consolidation.md`
- Delete: `.agents/plans/2026-10-14-row17-n64-startover-retry.md`

**Interfaces:**
- Consumes: PR #367 merged to `develop` from reviewed head `b930ccf205ba7f39a81c7b8e9515d47a5d93bb42`, merge `4068e60e4af4f8169d2afa792a218d341ede9de0`, exact-head gate `38654166461`, develop-push gate `38654786796`, clean independent review, and verified N-64 worktree plus paired ref retirement.
- Produces: promoted N-64 delivery evidence, active N-65 at `0.1.0-dev.183`, and retirement of the completed N-64 plan.

- [x] Verify PR #367 is merged to `develop`; its head and merge identities match the values above; both hosted gates passed; the merge is contained in refreshed `origin/develop`; the primary checkout remains current with the user edit preserved; and the N-64 worktree plus exact local/upstream refs are absent.
- [x] Add PR #367 to row 17's delivery references, make N-65 the active plan, and record N-64's disposition and paired cleanup in a dated roadmap delivery note.
- [x] Advance only `Directory.Build.props` from `0.1.0-dev.182` to `0.1.0-dev.183`; retire N-64 and commit this plan, roadmap, and version update together before changing tests.
- [x] Review the staged planning diff, confirm no generated version copies are tracked or edited, run `git diff --check`, and commit through the normal staged-candidate hook.

### Task 2: Consolidate successful start-flow coverage

**Files:**
- Modify: `src/WildBunch.Web/src/tests/StartFlow.test.tsx`

**Interfaces:**
- Consumes: `PreSessionSurface`, setup/prologue/start commands, and the Phaser map-host selection callback.
- Produces: one rendered success scenario asserting exact setup values, authoritative phase transitions, selected town, and an explicitly pending first-arrival state.

- [x] Strengthen the existing exact-payload success scenario to hold `startGameWithTown` pending after selecting a town through `scene.selectTown`, then assert the creating heading and pending copy before resolving the server response.
- [x] Assert the exact setup name, normalized seed, difficulty, entropy, operation identity, and selected-town request at their command boundaries; preserve assertions only where they prove those behaviors.
- [x] Remove duplicate successful-only tests for setup-to-story, story-to-town, name-draft labeling, immediate creating display, and separate map-host town forwarding once the canonical scenario proves those same behaviors.
- [x] Keep materially distinct refresh, definitive failure and correction, uncertain retry, invalid seed, no-selection, and no-Back scenarios. Do not add a test solely for generated/source shape.
- [x] Temporarily move the creating transition after the awaited start request; confirm the strengthened pending-state assertion fails, restore `PreSessionSurface.tsx` byte-for-byte, and rerun the focused test.
- [x] Run `npm exec -- vitest run src/tests/StartFlow.test.tsx` from `src/WildBunch.Web`; the focused suite must pass.

### Task 3: Reconcile the W-08 and web-test audit

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`

**Interfaces:**
- Consumes: live StartFlow test behavior, existing N-60 startup recovery disposition, and N-64 archive retry disposition.
- Produces: an accurate dated W-08 reconciliation and a current test-owner recommendation that does not continue to describe already-covered failure paths as missing.

- [x] Update the StartFlow inventory row to state that the successful path is consolidated and that failure/retry/refresh behavior remains with its distinct owners.
- [x] Add a dated N-65 disposition describing only the removed redundant success walks and the mutation-proven pending first-arrival assertion; preserve the prior N-60 and N-64 evidence unchanged.
- [x] Reconcile W-08's current wording against the actual setup, prologue, first-arrival, and archive tests; retain any genuinely unproven behavior as an open finding rather than closing it by assertion.
- [x] Confirm PG-002, the feature matrix, and applicable ADRs remain accurate; do not update them absent a durable decision or player-promise change.

### Task 4: Validate and commit the test correction

**Files:**
- Review: the complete candidate from Tasks 1-3.

- [x] Run repository command-bus format and lint checks on the changed TSX file; use `git diff --check` for Markdown and whitespace.
- [x] Run the canonical fail-fast `py -3 tools/run.py ci --check` from the repository root; stop at the first failed lane and repair it before proceeding.
- [x] Inspect the full candidate diff and confirm it contains only N-64 promotion, one version increment, StartFlow test consolidation, and accurate dated audit updates.
- [x] Commit the behavior and disposition through the normal check-only staged-candidate hook.

### Task 5: Review and deliver N-65

- [ ] Prepare a whole-branch review package against this plan, W-08/PG-002, frontend standards, the play-surface and code-review unslop profiles, relevant ADRs, and the feature matrix. Obtain an independent fresh-context review with GPT-6 Luna; resolve actionable findings and request fresh review of the corrected head.
- [ ] Push the branch and create a Draft PR to `develop`; verify its body, base, head SHA, and draft state. State that no durable decision or feature promise changed. Mark ready after clean review and require exact-head hosted CI to pass.
- [ ] Squash-merge under the active goal authorization and require the develop-push gate to pass on the merge SHA.
- [ ] Fast-forward the primary checkout without disturbing the user's existing investigation edit. Verify the merged tree, then remove this clean worktree with the finishing helper, delete only its exact local/upstream refs, and verify all three absences before the next plan.

## Acceptance

One scenario owns the successful start journey and proves the request and pending behavior it names. The remaining tests in `StartFlow.test.tsx` protect distinct user-visible failure, retry, refresh, validation, or selection behavior. N-64 is retired with its delivery evidence retained in the roadmap, the authored version is `0.1.0-dev.183`, the candidate passes the canonical local and hosted gates, and the merged worktree plus paired refs are verified absent.
