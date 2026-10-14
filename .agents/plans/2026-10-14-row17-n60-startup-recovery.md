# N-60 Keep startup failures recoverable

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Keep the player on the current start-flow step when a prologue acknowledgment or first-town command fails, and clear the displayed error when a retry succeeds.

**Architecture:** The backend `GameSession` phase is authoritative. The UI advances from prologue only after `markPrologueViewed` succeeds, and from town selection only after `startGameWithTown` succeeds. The domain already treats repeated `ViewPrologue` as idempotent. First-town delivery already persists a per-session operation ID and town ID so an uncertain retry can repeat the same command. Preserve those semantics and prove the visible recovery path.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [whole-test quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [frontend standards](../doctrine/frontend-standards.md); [validation policy](../doctrine/validation-policy.md).

**Execution Strategy:** `executing-plans` (Native inline). This is a focused test-and-recovery slice within the existing startup flow. Do not change event contracts, start-flow product semantics, or archive behavior.

## Global Constraints

- Start from `origin/develop` in a fresh worktree created with the bundled `using-git-worktrees/scripts/new_worktree.py`; target the PR to `develop`.
- Promote N-59 delivery evidence in the first substantive commit, retire its completed plan, and advance only the authored version in `Directory.Build.props` from `0.1.0-dev.177` to `0.1.0-dev.178`.
- A failed prologue acknowledgment leaves the prologue visible. A successful retry advances to starting-town selection and removes the prior error.
- An uncertain first-town response leaves town selection available and preserves its pending town and operation ID. A successful retry uses that exact operation and clears the prior error.
- Preserve the existing archive outcomes and setup retry behavior. Existing Start Over tests already distinguish archived-server reconciliation, active-session conflict, and unreadable reconciliation.
- No event, API, ADR, feature-matrix, or persistent-storage contract change is expected.
- Run `py -3 tools/run.py ci --check` with PostgreSQL ensured. Commit hooks and hosted CI remain check-only.

## Review Focus

- A rejected prologue acknowledgment must not advance the visible phase, and the user can retry without an unhandled event-handler rejection.
- An uncertain first-town response must not report a started game or discard the persisted town/operation identity; retry reuses both and only then completes.
- Successful retries clear the previous command error instead of leaving a stale alert over the new phase.
- Existing setup retry and archive-conflict/reconciliation behavior remains covered and unchanged.

---

### Task 1: Promote N-59 and establish N-60

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n60-startup-recovery.md`
- Delete: `.agents/plans/2026-10-14-row17-n59-prosperity-wire-contract.md`

**Interfaces:**
- Consumes: merged PR #362, reviewed source `2983c91dc50914bcac1577ad6ff46e2621ebf098`, squash merge `a4ab0e59bc3b46568e55d2c2aae88879743513ec`, exact-head gate `38626501617`, develop-push gate `38627312986`, clean independent review, primary fast-forward with the user's investigation edit preserved, and verified removal of N-59's worktree and both branch refs.
- Produces: promoted N-59 delivery evidence, active N-60 at `0.1.0-dev.178`, and retirement of the completed N-59 plan.

- [ ] **Step 1: Revalidate N-59 integration and cleanup.** Verify PR #362 is merged to `develop`, the recorded source and merge SHAs match, both hosted gates succeeded on those commits, the merge is contained in `origin/develop`, the primary checkout is at that merge with the pre-existing user edit intact, and N-59's worktree plus exact local/upstream refs are absent.
- [ ] **Step 2: Promote N-59, activate N-60, and advance the version.** Add N-59's PR, source and merge SHAs, independent review, exact-head and develop-push gate runs, and paired worktree/ref cleanup evidence to row 17. Set the active plan/version to N-60 and `0.1.0-dev.178`. Change only the authored version in `Directory.Build.props`.
- [ ] **Step 3: Retire the completed predecessor.** Remove the N-59 plan only after its delivery evidence is promoted; keep the baseline specification and investigation records.
- [ ] **Step 4: Commit successor planning first.** Check whitespace and inspect the staged planning diff, then commit the roadmap, version, N-60 plan, and N-59 plan retirement as `docs: plan N-60 startup recovery` before editing tests or source.

### Task 2: Prove and repair startup command recovery

**Files:**
- Modify: `src/WildBunch.Web/src/tests/StartFlow.test.tsx`
- Modify: `src/WildBunch.Web/src/flow/PreSessionSurface.tsx`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionMutations.ts`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`

**Interfaces:**
- Consumes: current event-backed start phases, `ViewPrologue` idempotency, `PreSessionSurface` command handlers, and `startupOperationStorage`'s pending first-arrival identity.
- Produces: observable failed-then-successful retry behavior for prologue acknowledgment and first-town selection, with no stale error after success.

- [ ] **Step 1: Add the prologue failure and retry scenario.** From a saved session in the prologue phase, reject `markPrologueViewed` once and then resolve it. Assert the alert is shown, the prologue remains visible, town selection is not shown, the action can be retried, and successful retry advances and clears the alert.
- [ ] **Step 2: Run the focused scenario and establish the missing behavior.** Run the new test alone. Expect it to fail because the rejection escapes the surface handler and the successful mutation does not clear the prior error. Preserve the failure output in the off-repo execution ledger only.
- [ ] **Step 3: Add the uncertain first-town retry scenario.** Reject the first town-selection response with a connection failure, then select the same town after returning to the map. Assert the pending storage still names that town and operation ID, both command attempts use the same request and operation ID, no started notice appears before success, and success clears the error and pending operation.
- [ ] **Step 4: Run both focused scenarios and prove RED.** Run only the new cases and confirm their intended assertions fail against current behavior. If a test fails for setup or timing instead, correct the test without weakening its behavior claim.
- [ ] **Step 5: Implement the smallest recovery correction.** Catch and retain the prologue step on acknowledgment rejection; clear the surfaced command error on successful prologue and first-town mutations. Keep existing pending-operation storage and backend phase authority unchanged.
- [ ] **Step 6: Prove the tests reject broken recovery.** Temporarily advance the prologue despite a rejected acknowledgment and separately disable error clearing on successful retry; confirm the relevant assertions fail, then restore both mutations and rerun the focused tests.
- [ ] **Step 7: Record the W-08 disposition.** Add a dated investigation note describing the two repaired recovery gaps and their behavior proof. Note that setup retries and archive reconciliation already had focused behavior coverage and remain unchanged. No production rule, ADR, or feature promise changes.

### Task 3: Validate, review, publish, integrate, and retire N-60

**Files:**
- Review: complete merge-base diff, N-59 promotion, startup recovery behavior, and dated audit disposition.
- Update: row 17 receives N-60 delivery evidence in the next successor's first substantive commit.

**Interfaces:**
- Consumes: committed startup recovery behavior proof and passing canonical gate.
- Produces: N-60 integrated into `develop` at `0.1.0-dev.178`, independently reviewed and verified by hosted CI.

- [ ] **Step 1: Validate the final slice.** Ensure PostgreSQL, run focused startup tests, run `git diff --check`, and run `py -3 tools/run.py ci --check` on the committed tree. Compare the passing result with the exact committed head.
- [ ] **Step 2: Obtain independent whole-branch review.** Review the complete diff against the unslop profile, frontend standards, testing doctrine, relevant ADR catalogue, and feature matrix. Fix actionable findings with behavior proof and obtain a fresh clean review.
- [ ] **Step 3: Publish and verify.** Push the branch, create a Draft PR to `develop`, verify body/base/head and exact SHA, mark ready after clean review, and require the exact-head hosted gate to pass.
- [ ] **Step 4: Integrate and clean up.** Squash-merge to `develop`, require the develop-push gate on the merge SHA, fast-forward the primary checkout while preserving its user edit, remove the clean worktree with the `finishing-a-development-branch` helper, delete the exact local/upstream refs, and verify all are absent.

## Acceptance

- A failed prologue acknowledgment remains on the prologue, reports the failure, and can be retried; successful retry advances and clears the failure.
- A failed first-town response preserves the town selection operation ID and request; retry uses the same identity, and only successful completion reports the game as started.
- No stale error remains after successful retry.
- Existing setup retry and archive reconciliation behavior remains unchanged.
- N-59 is promoted and retired only after its exact PR, independent review, both hosted gates, primary fast-forward and paired cleanup facts are verified.
- N-60 is independently reviewed, hosted CI passes on its exact PR head and develop merge SHA, and its worktree plus exact branch refs are removed together.
