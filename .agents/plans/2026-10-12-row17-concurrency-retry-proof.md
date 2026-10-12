# Row 17 N-05 Concurrency Retry Proof Implementation Plan

**For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the optimistic-concurrency retry tests that reuse a mutated aggregate with behavioral proofs that retries reload authoritative state and cannot commit command effects twice.

**Architecture:** `GameSessionCommandHandler` owns load, command, store, commit and retry orchestration. Each retry must load a fresh aggregate from persisted event history. The PostgreSQL repository owns stream-version concurrency and event append; the Application tests should prove orchestration with a fake that models durable events and rollback, while existing PostgreSQL integration tests continue to prove persistence conflicts. Do not introduce a generic command receipt or alter the settled startup operation identity rules.

**Tech Stack:** C#/.NET 10, xUnit, `GameSessionCommandHandler`, typed domain events and the existing PostgreSQL integration fixtures.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially command retry authority, persistence/concurrency and paired worktree/ref cleanup. N-05 is recorded in `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`.

**Execution Strategy:** `executing-plans` inline. First inspect the live handler and repository boundary, then make the Application retry tests behaviorally adversarial. Keep PostgreSQL storage conflict proof at the existing integration-test owner. Production code changes are in scope only if the stronger proof exposes a real failure.

## Global Constraints

- Start from refreshed `origin/develop` in the dedicated `codex/row17-n05-concurrency-retry-proof` worktree.
- Keep `Directory.Build.props` as the only authored application version and advance once from `0.1.0-dev.119` to `0.1.0-dev.120` in the first substantive implementation commit, not this plan-only commit.
- In that first substantive commit, retire the completed N-04 `.119` plan and update the roadmap with PR #304's reviewed source/head, squash merge, identical tree, exact-head and develop-push gate results, and paired worktree/ref cleanup evidence. Preserve the current N-05 plan through its completing PR.
- The spec already states that a plan worktree and its matching local/upstream refs are one cleanup unit; retain that truth and avoid duplicating it as a second rule. The roadmap is the delivery-history owner for each plan's actual cleanup evidence.
- Preserve ADR-0028's event-stream authority and existing retry semantics. Update an ADR only if the implementation reveals a durable decision has changed or the current record is false.
- The feature matrix is unchanged unless a current player promise or dependency changes; retry proof alone does not change product scope.
- Do not add generic command receipts, startup operation identity, event-replay roll logic, or unrelated persistence redesign.
- Keep tests behavior-focused. Do not assert fake call shape or implementation details without proving a user-visible/durable retry invariant.
- Never use `--no-verify`; commit only through the check-only staged-snapshot hook and fail-fast repository gate.

## Task 1: Write and commit this JIT plan

**Files:**
- `.agents/plans/2026-10-12-row17-concurrency-retry-proof.md`

- [x] Inspect N-05's recorded failure mode and the current retry handler, repository extension, Application tests and PostgreSQL conflict tests.
- [x] Confirm that the existing spec already requires retries to reload authoritative state and that the existing integration tests own database concurrency behavior.
- [ ] Commit the plan alone without advancing the application version.

## Task 2: Prove retry effects against authoritative durable state

**Files:**
- `tests/WildBunch.Application.Tests/Execution/GameSessionCommandHandlerTests.cs`
- `src/WildBunch.Application/Games/Execution/GameSessionCommandHandler.cs` only if the new behavior proof demonstrates a production defect.
- `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- `.agents/plans/2026-10-12-row17-journal-projector-proof.md`
- `Directory.Build.props`

- [ ] Replace the retry fake's shared mutable object with durable event state that reconstructs a fresh `GameSession` for each load. The failed attempt's uncommitted mutation must be discarded before the retry.
- [ ] For a store conflict, assert the final durable wallet, inventory and purchase event reflect exactly one successful purchase after retry.
- [ ] For a commit conflict, model rollback of staged writes, reload the durable state, retry, and assert exactly one committed purchase effect.
- [ ] For retry exhaustion, assert the original concurrency failure surfaces and the durable session has no effects from failed attempts; retain meaningful bounded-retry proof without depending on mutable aggregate aliasing.
- [ ] Keep the PostgreSQL expected-stream-version and cross-context conflict tests as the production persistence boundary; change or add integration coverage only if inspection finds a real uncovered persistence contract needed by this behavior.
- [ ] Prove the new behavioral assertions fail when the fake is deliberately changed to return the same already-mutated aggregate after the failed attempt; restore it and rerun the focused tests.
- [ ] Update this spec's delivery paragraph only to make the already-settled paired cleanup wording explicit and easy to discover; do not change its semantics. Add PR #304's completed delivery and paired cleanup evidence to the row-17 roadmap, leaving row 17 and later N-findings open.
- [ ] Advance the authored version once to `0.1.0-dev.120` and retire the completed `.119` plan in this same first substantive commit.
- [ ] Run the focused Application and relevant Integration tests, then commit through the normal staged-snapshot hook.

## Task 3: Review, publish, merge and retire the execution unit

**Files:**
- Review the actual branch diff against ADR-0028, the feature matrix, N-05 and applicable unslop guards.

- [ ] Independently inspect the actual diff for durable retry behavior, event ownership, ADR truth and feature-matrix impact; resolve any source-truth discrepancy in the same PR.
- [ ] Obtain a fresh whole-branch review and resolve findings with focused re-review.
- [ ] Publish a Draft PR to `develop`, complete author/reviewer ADR and feature-matrix checks, mark ready, and require the exact-head canonical hosted gate to pass.
- [ ] Squash merge under the already-authorized epic flow; verify the develop-push gate passes and the merged tree matches the reviewed tree.
- [ ] Fast-forward the primary checkout. Verify the merged PR head, remove the clean worktree first, explicitly delete its exact local and upstream refs, and verify all three are absent before the next plan worktree.
- [ ] Preserve later row-17 findings for JIT successor plans; do not claim row 17 complete.
