# Persistence Retry Classification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Translate only the stored-event stream-sequence unique violation into a retryable concurrency result, while preserving unrelated database integrity failures and proving a fresh legal retry after a real append race.

**Architecture:** Keep retry classification at the EF unit-of-work commit boundary, where PostgreSQL reports the failed constraint. Identify the actual stored-event stream/sequence constraint from typed provider error metadata; do not infer concurrency from localized or generic message text. Keep aggregate reload and command retry in the existing application handler flow.

**Tech Stack:** C#/.NET, EF Core, Npgsql/PostgreSQL, xUnit integration tests.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery), [row 07 persistence roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#07-persistence-restoration-and-retries), [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [persistence test follow-up PS-10](../investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md#mapping-every-persistence-finding-to-the-right-proof).

**Execution Strategy:** `executing-plans` inline. The tests, classifier and PostgreSQL evidence are tightly coupled at one persistence boundary; a single executor can perform the required red-green cycle and then review the integrated change.

**Plan readiness:** 9/10. The supported retry signal, owning commit boundary, test database, conflicting constraint and required failure identity are known from source and the settled event-sourcing policy.

## Scope and invariants

- Preserve immutable event history as authoritative; the losing concurrent command must leave no event or cache writes behind.
- Only PostgreSQL unique violation `23505` for the actual stored-event `(StreamId, Sequence)` primary-key constraint becomes `ConcurrencyException`; all other unique violations retain their database-integrity identity.
- A retry reloads aggregate state in a fresh context and can commit exactly one legal action after the competing append.
- Keep current migration history, schemas, event payloads, gameplay contracts and API responses unchanged.
- Keep row 07 limited to append-race classification and retry proof. Command-load interleaving tests and remaining malformed-component recovery stay for later row 07 slices.

## Review Focus

- An `EventId` collision across two distinct stream/sequence keys raises a PostgreSQL unique violation but must remain a `DbUpdateException` with its `PostgresException` SQLSTATE and constraint name intact.
- A losing append collision rolls back its staged snapshot, components, diary and event writes; a fresh legal command then commits once from the winning state.
- Message text containing “unique constraint” without provider SQLSTATE and the exact sequence constraint must not be sufficient for retry classification.

## Tasks

### Task 1: Retire the completed diary-cache slice and set this successor

**Files:** `.agents/plans/2026-10-09-travel-diary-cache-completeness.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`.

- [ ] Verify PR #203 merged to `develop` at `2b6472cafac1869b0d94ddbe13891b1364424052`, source `66165171b92a9f578d7ee43a2c9a473ed98c9ad7` has the same tree as merge `db90d4bc89b428d4f10f38f89fe259fc41da9dcf`, and hosted canonical run `37865661644` passed.
- [ ] In the first substantive implementation commit, make this the row 07 current-plan link, record PR #203 source, merge, tree, version `0.1.0-dev.18` and hosted gate evidence, summarize the diary-cache recovery behavior, and retire the completed diary-cache plan.
- [ ] Advance `Directory.Build.props` exactly once from `0.1.0-dev.18` to `0.1.0-dev.19` in that same first substantive implementation commit; do not hand-edit generated web version output.
- [ ] Keep the plan-only commit separate and first; inspect the staged roadmap, predecessor retirement and version diff before committing.

### Task 2: Prove the unit-of-work distinguishes sequence races from other integrity failures

**Files:** `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionUnitOfWork.cs`.

- [ ] Ensure the shared PostgreSQL service, run the current cross-DbContext append-race test, then add a two-session test that persists valid sessions and stages two `StoredEventEntity` rows with distinct `(StreamId, Sequence)` keys but the same `EventId`.
- [ ] Commit those rows through `EfGameSessionUnitOfWork`; assert the thrown exception is `DbUpdateException`, not `ConcurrencyException`, and that the nested provider error retains SQLSTATE `23505` and identifies the EventId unique index.
- [ ] Run the new test alone and confirm it fails on current code because the generic “unique constraint” message is translated into `ConcurrencyException`; do not change the implementation before observing that failure.
- [ ] Extend the existing append-race scenario to assert the losing context has no persisted losing event or snapshot/cache writes after rollback, then create a fresh scope, reload the winner's aggregate, execute one legal purchase, commit, and assert the event stream contains exactly the winner's purchase followed by the retry purchase with ordered, unique sequences and corresponding independent inventory/cash effects.
- [ ] Confirm the focused tests can fail for the intended reasons by temporarily disabling the exact classifier or rollback behavior locally, observe the targeted assertion fail, and restore production code before continuing.

### Task 3: Classify only the actual event-sequence constraint

**Files:** `src/WildBunch.Persistence/GameSessions/EfGameSessionUnitOfWork.cs`; `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs`.

- [ ] Read the migration and live PostgreSQL exception metadata to identify the exact primary-key constraint name for `GameSessionStoredEvents(StreamId, Sequence)` and the unique index name for `EventId`.
- [ ] Replace message substring matching with a typed Npgsql `PostgresException` check for SQLSTATE `23505` and the exact sequence primary-key constraint; unrelated unique violations and provider errors without that identity propagate unchanged and do not clear the change tracker as though a concurrency retry were safe.
- [ ] Preserve the existing `ConcurrencyException` message contract for the recognized append race and clear tracked entities only after rollback of that recognized concurrency failure.
- [ ] Run the focused PostgreSQL tests and verify the EventId integrity error remains inspectable while the sequence collision still becomes retryable concurrency.

### Task 4: Record evidence and deliver

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; this plan; changed source and tests.

- [ ] Add a dated PS-10 disposition recording only the real PostgreSQL failure identities, rollback/retry behavior and resulting test names; preserve the original audit finding as historical evidence.
- [ ] Compare the diff with ADR-0028, event-sourcing integrity doctrine, architecture guardrails, feature matrix, backend-architecture and code-review unslop profiles; leave ADRs and feature matrix unchanged because no durable architecture or gameplay promise changes, and state that reason in the PR.
- [ ] Run the focused PostgreSQL tests, then the canonical fail-fast `py -3 tools/run.py ci --check` gate; confirm generated web identity is `0.1.0-dev.19` and no migration or event payload changed.
- [ ] Complete whole-branch self-review against this plan, the baseline spec, PS-10, architecture doctrine and code-review runbook; record the required self-review fallback because the active runtime does not permit reviewer delegation.
- [ ] Publish and attach a Draft PR to `develop`, verify its exact source head and hosted canonical gate, mark it ready, merge via the established squash route under active epic authorization, fast-forward `Z:\wild-bunch`, and clean only this verified merged worktree and branch; retain this plan until its next row 07 successor classifies it.
