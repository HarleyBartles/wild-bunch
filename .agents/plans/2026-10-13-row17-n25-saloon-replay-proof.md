# Row 17 N-25 Saloon Override Replay Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Prove that replay reconstructs both a forced-but-unconsumed developer saloon override and the actual person of interest produced after the override is consumed.

**Architecture:** Use the original canonical setup events and append the aggregate's actual typed command events. The event stream is authoritative: `DevSaloonOverrideForced` restores pending control state, `DevSaloonOverrideConsumed` clears it, and `SaloonPersonOfInterestSpotted` restores the encounter. Do not reconstruct setup from post-action mutable session state, manually restore child state, or change production code unless a witnessed behavior failure demonstrates a real deviation.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.Domain.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), N-17; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), `DevSaloonOverrideTests`.

**Execution Strategy:** `executing-plans`, inline and sequential. This is one event-replay invariant at the existing Domain test owner; parallelizing the setup and consumed paths would split a single event-history proof.

**Governing Decisions:** ADR-0028 defines typed event replay as the state-reconstruction path. ADR-0032 says saloon overrides are forced, cleared, and consumed as event-backed facts and are consumed once. Neither durable decision changes in this slice. The decision-record author/reviewer checks remain required; the PR author and reviewer must independently verify this against the final diff.

**Scope:** Correct the current test's weak oracle and add the missing pending-state replay proof. Keep production behavior unchanged if those tests pass against the existing typed event application path. Do not broaden into other N-17 or N-18 findings, persistence reconstruction, replay fixtures used by other classes, or unrelated saloon tests.

## Review Focus

- The setup history must be captured before forcing or consuming the override. Do not use `TravelTestFactory.RecaptureSetupEventsForReplay` after state has changed; it derives synthetic setup from current mutable state.
- The pending replay case must assert the exact forced suspect override, so skipping the forced event application makes it fail.
- The consumed replay case must assert both no pending override and that the actual forced suspect is the active saloon person of interest after replay.
- Do not assert implementation method names, event counts, or incidental event list order where a state outcome provides stronger proof.
- No player-facing output, feature promise, production behavior, or ADR changes are expected.

---

### Task 1: Retire N-24 and install N-25

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n24-domain-arrival-context.md`
- Add: `.agents/plans/2026-10-13-row17-n25-saloon-replay-proof.md`

**Interfaces:**
- Consumes: N-24 PR #327, reviewed source `479594b7fa102882c82db176702912f689bff793`, squash merge `ac158859ba4e1801273540cc471cc6c02fb1a68a`, identical source/merge tree `f8e0f659e31fdfc29c45abcf7db4ec32d26bb675`, exact-head hosted gate `38402158150`, approved whole-branch review, develop-push gate `38402890935`, and paired checkout/ref cleanup.
- Produces: active N-25 plan at `0.1.0-dev.143`, N-24 plan retirement, and a dated N-24 delivery record that closes only the Domain arrival/context-refresh test finding.

- [x] **Step 1: Confirm the successor boundary.** Verified N-24 source/merge identities and tree equality, exact-head hosted gate, fresh review, develop-push gate, primary checkout, and paired worktree/local/upstream ref cleanup. Confirmed `develop` is current and the proposed N-25 branch was absent from worktrees and both live refs before creation.
- [x] **Step 2: Record N-24 and install N-25.** Added PR #327 and its review/gate/cleanup evidence to row 17, added the dated delivery record, pointed the row to this plan, added PR #327 to the delivered PR list, and advanced `Directory.Build.props` once to `0.1.0-dev.143`.
- [x] **Step 3: Retire N-24 and commit planning/version changes.** Deleted the completed N-24 plan, preserved this active plan, validated links and version ownership, and committed only planning/version changes before editing tests.

### Task 2: Prove both saloon override replay states

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

**Interfaces:**
- Consumes: `TestSessionFactory.CreateWithConfrontableSaloonSuspect`, its original committed setup events, `GameSession.ForceDevSaloonOverride`, `GameSession.LookAroundSaloon`, `GameSession.AllEvents`, and `GameSession.RehydrateFromEvents`.
- Produces: a pending replay scenario asserting exact `DevSaloonOverride` state; a consumed replay scenario asserting the same suspect is the active person of interest and the pending override is absent; a corrected N-17 disposition describing observed source/test truth.

- [ ] **Step 1: Capture authentic setup history before mutation.** At the beginning of each replay scenario, copy the session's canonical committed setup events. After forcing or consuming the override, append only events that occurred after that captured setup boundary. Do not regenerate setup from the changed session.
- [ ] **Step 2: Add the pending override replay behavior.** Force a named eligible suspect without looking around, rehydrate from the original setup plus the actual forced event, and assert the pending override has the exact suspect kind and ID.
- [ ] **Step 3: Strengthen consumed replay behavior.** Update `EventReplay_ForcedThenConsumed_ReconstructsCorrectState` to use the original setup stream and assert replay clears pending override while reconstructing the active saloon suspect with the forced ID. Keep the test focused on observable state rather than event-list shape.
- [ ] **Step 4: Prove the new test rejects a missing behavior.** Temporarily bypass application of the forced event and confirm the pending replay test fails because the expected override is absent; restore source and rerun. If the test does not fail for that reason, correct its setup/oracle before continuing.
- [ ] **Step 5: Correct the investigation record.** Replace N-17's stale assertion that replay postprocessing unconditionally clears the override with what the current source actually does: replay dispatches typed events through `ApplyEvent`; the existing test only observed a consumed endpoint and reconstructed setup after mutation. Record the new pending/consumed outcomes and the mutation proof. Update the companion file disposition only as needed to reflect the stronger behavior owner. Do not add a test-run receipt.
- [ ] **Step 6: Validate and commit the slice.** Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~DevSaloonOverrideTests`; inspect the focused diff and run the repository's complete `py -3 tools/run.py ci --check` gate before review. Preserve any gate output only as working evidence, not a committed receipt.

### Task 3: Review, publish, merge, and close out

**Files:**
- Review: complete branch diff, plan rulings, ADR-0028 and ADR-0032 consistency.

**Interfaces:**
- Consumes: committed N-25 plan/version slice, test/disposition slice, and refreshed `develop`.
- Produces: fresh whole-branch review, one PR to `develop`, passing exact-head hosted gate, merged source/merge/tree identity, passing develop-push gate, fast-forwarded primary checkout, and paired removal of this worktree and exact local/upstream refs.

- [ ] **Step 1: Obtain a fresh whole-branch review.** Review the full diff from refreshed `develop` against this plan, ADR-0028, ADR-0032, backend architecture and event-sourcing guidance, the selected backend-architecture and code-review unslop profiles, and the repository review runbook. Resolve actionable findings with witnessed RED/GREEN and rerun affected checks. Independently verify the author ADR check.
- [ ] **Step 2: Publish and merge.** Push the branch and open one PR to `develop`. Verify PR head equals local `HEAD`, base is `develop`, author ADR review is resolved, and exact-head hosted CI passes before squash merge. Do not delete the upstream ref as part of PR merge.
- [ ] **Step 3: Verify and retire the paired checkout.** Confirm develop-push CI passes; fast-forward the primary checkout; verify the merge identity and source/merge tree equality; confirm the clean worktree; remove it before deleting this exact local and upstream branch ref; verify all three are absent. Preserve unrelated worktrees.
