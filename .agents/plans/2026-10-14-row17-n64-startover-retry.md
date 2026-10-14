# N-64 Prove Start Over retry after uncertain archive Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove that a player can retry Start Over after both the archive request and its authoritative reconciliation read fail, and that the retry completes the archive through the existing dialog.

**Architecture:** Extend the existing rendered `StartOverConfirmation` scenario at the web interaction owner. Keep the server and event stream authoritative: failed or uncertain archive calls retain the saved session, while a later successful server response clears local selection and returns to setup. The server-side archive event, replay and persistence boundaries remain owned by their existing tests.

**Tech Stack:** TypeScript, React Testing Library, Vitest, TanStack Query, Prettier, ESLint, and the Wild Bunch validation command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [W-08 whole-test finding](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web-test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [PG-002 playthrough lifecycle contract](../investigations/stable-0.1.0/2026-10-07-playthrough-lifecycle-contract.md); [frontend standards](../doctrine/frontend-standards.md); [validation policy](../doctrine/validation-policy.md).

**Execution Strategy:** `executing-plans` (Native inline). The single scenario, its mutation proof, and its dated audit disposition share one behavior owner and one fixture; continuity avoids splitting one retry lifecycle across handoffs. The authorized roadmap goal requires inline execution.

## Global Constraints

- Start from refreshed `origin/develop` in this fresh worktree and target the PR to `develop`.
- Promote N-63's merged delivery, retire its completed plan, and advance the authored version once to `0.1.0-dev.182` in `Directory.Build.props`.
- A confirmed archive is an immutable server event. Do not clear the browser's selected session after an archive or uncertain response unless authoritative server state confirms the archive.
- Retain the existing cancellation, conflict, lost-response reconciliation, server-read failure, reload, and event/replay coverage; add only the missing retry-after-reconciliation-failure behavior.
- Do not change product copy, archive semantics, APIs, events, ADRs, or feature promises unless live behavior proves the accepted contract is violated.
- Keep generated artifacts and test-result receipts out of the commit. Preserve the user's uncommitted edit in the primary checkout by not touching its file.
- Run the focused web test and formatter/linter checks, then the canonical fail-fast `py -3 tools/run.py ci --check` on the committed candidate.

## Review Focus

- A transient archive failure followed by a failed status read must leave the saved game selected and the confirmation available; a second user confirmation must make a new server request and finish only after success.
- While the retry request is pending, both dialog actions remain disabled and repeated confirmation must not dispatch a duplicate archive request.
- The web test proves the UI's server-response behavior only; it must not claim that a mocked client call proves event persistence or replay.

---

### Task 1: Promote N-63 and establish N-64

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n64-startover-retry.md`
- Delete: `.agents/plans/2026-10-14-row17-n63-w11-presentation-proof.md`

**Interfaces:**
- Consumes: PR #366 merged to `develop` from reviewed head `502858c2e3bfcada84440e43e509d28de354418c`, merge `1df26ced3604e0ed23073dc99c11dbe6e7238e3b`, exact-head gate `38648698000`, develop-push gate `38649233711`, clean independent review, and verified N-63 worktree plus paired branch-ref retirement.
- Produces: promoted N-63 delivery evidence, active N-64 at `0.1.0-dev.182`, and retirement of the completed N-63 plan.

- [x] Verify PR #366 is merged to `develop`; its head and merge identities match the values above; its exact-head and develop-push gates passed; the merge is contained in refreshed `origin/develop`; the primary checkout remains current with the user edit preserved; and the N-63 worktree plus exact local/upstream refs are absent.
- [x] Add PR #366 to row 17's delivery references, make N-64 the active plan, and record the N-63 disposition and paired cleanup in a dated roadmap delivery note.
- [x] Advance only `Directory.Build.props` from `0.1.0-dev.181` to `0.1.0-dev.182`; retire N-63 and commit this plan, roadmap, and version update together before changing tests.
- [x] Review the staged planning diff, confirm no generated version copies are tracked or edited, run `git diff --check`, and commit through the normal staged-candidate hook.

### Task 2: Prove retry after archive and reconciliation failures

**Files:**
- Modify: `src/WildBunch.Web/src/tests/StartOverConfirmation.test.tsx`

**Interfaces:**
- Consumes: the rendered Start Over dialog, `archiveGame`, `getGame`, and the current stored session ID.
- Produces: one scenario proving failure preserves the current playthrough and a deliberate retry completes it without duplicate dispatch while pending.

- [ ] Add one rendered scenario where the first `archiveGame` request fails, the follow-up `getGame` reconciliation also fails, and the error remains visible while `wild-bunch.current-game-id`, the Game Settings overlay, and Start Over confirmation remain available.
- [ ] Retry by clicking the same confirmation action. Hold the second archive request pending and assert it was dispatched exactly once and both dialog actions are disabled. Resolve it successfully and assert the local ID is cleared, both dialogs close, setup returns, the success notice appears, and the previous error is gone.
- [ ] Run `npm exec -- vitest run src/tests/StartOverConfirmation.test.tsx` from `src/WildBunch.Web`; the focused suite must pass.
- [ ] Temporarily break the retried success path in `useGameSessionMutations.ts` so a successful retry does not reconcile local session state, run the new test and confirm it fails on the stale selected game or missing success state, then restore production source byte-for-byte and rerun the focused suite.
- [ ] Make no production change if the existing implementation passes this behavior. If the new scenario exposes a real contract violation, fix only that path test-first and preserve the same externally visible contract.

### Task 3: Date the W-08 archive retry disposition

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Review: `docs/decisions/README.md`, relevant archive/event ADRs, and `docs/features.md`

**Interfaces:**
- Consumes: the final behavior test and its mutation proof.
- Produces: a dated W-08 disposition that closes only archive retry/reconciliation coverage, while keeping the original audit history and distinguishing frontend orchestration proof from server event/replay proof.

- [ ] Record the actual failure, retained-session, pending-guard, retry, and success behavior at W-08 and the `StartOverConfirmation.test.tsx` owner; do not record test counts or a development receipt.
- [ ] Confirm the scenario remains aligned with PG-002 and its applicable ADRs. Update neither the decision catalogue nor feature matrix because this slice adds no player promise, dependency, event, or durable rule; pause if live evidence requires different treatment.
- [ ] Remove no neighboring lifecycle coverage and leave the primary checkout's user edit untouched.

### Task 4: Validate and commit the behavior correction

**Files:**
- Review: the complete candidate produced by Tasks 1-3.

- [ ] Run `py -3 tools/run.py format --check src/WildBunch.Web/src/tests/StartOverConfirmation.test.tsx` and `py -3 tools/run.py lint --check src/WildBunch.Web/src/tests/StartOverConfirmation.test.tsx` from the repository root. Markdown is excluded from those targets; use `git diff --check` for whitespace validation.
- [ ] From the repository root, run `git diff --check` and `py -3 tools/run.py ci --check`; stop at the first failed canonical lane and repair it before proceeding.
- [ ] Inspect the full candidate diff and confirm it contains only N-63 promotion, the single authored version increment, the retry behavior test, and dated W-08 dispositions.
- [ ] Commit the behavior and disposition through the normal check-only staged-candidate hook.

### Task 5: Review and deliver N-64

- [ ] Prepare the whole-branch review package against this plan, W-08/PG-002, the frontend standards, the play-surface and code-review unslop profiles, relevant ADRs, and the feature matrix. Obtain an independent fresh-context review with GPT-6 Luna; resolve actionable findings and request a fresh review of the corrected head.
- [ ] Push the branch and create a Draft PR to `develop`; verify its body, base, head SHA, and draft state. State that no durable decision or feature promise changed. Mark ready after clean review and require exact-head hosted CI to pass.
- [ ] Squash-merge under the active goal authorization and require the develop-push gate to pass on the merge SHA.
- [ ] Fast-forward the primary checkout without disturbing the user's existing investigation edit. Verify the merged tree, then remove this clean worktree with the finishing helper, delete only its exact local/upstream refs, and verify all three are absent before the next plan.

## Acceptance

- A failed archive plus failed authoritative reconciliation does not clear the saved session or dismiss Start Over; a later deliberate retry makes a new request and completes only after success.
- A pending retry blocks duplicate confirmation, and the new test fails when the successful retry no longer reconciles local session state.
- Existing cancellation, conflict, lost-response, read-failure, reload, event-history, and replay coverage remains intact.
- N-63 is promoted and retired; N-64 merges to `develop` at `0.1.0-dev.182`, with independent review, passing exact-head/develop-push CI, and paired worktree/branch cleanup verified.
