# Command Load Consistency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure each command-side aggregate load reads its envelope, cache components, event stream and diary projection from one PostgreSQL snapshot during a concurrent append.

**Architecture:** Keep the GameSession aggregate and immutable event stream authoritative. Start one PostgreSQL `RepeatableRead` transaction before the command repository's first session query and retain it through fast-path loading or full event replay; commit the read transaction before returning the aggregate. Prove the boundary with a test-only command interceptor that pauses the real command loader after its second envelope read while a separate context commits a purchase.

**Tech Stack:** C#/.NET, EF Core, Npgsql/PostgreSQL, xUnit integration tests.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery), [row 07 persistence roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#07-persistence-restoration-and-retries), [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [persistence test follow-up PS-07](../investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md#mapping-every-persistence-finding-to-the-right-proof).

**Execution Strategy:** `executing-plans` inline. A deterministic PostgreSQL interleaving test and the transaction boundary change are tightly coupled; one executor can preserve the red-green evidence. Separate implementers would add handoffs around the same interceptor and repository behavior without independent work.

## Global Constraints

- Persisted events record facts that occurred; replay reconstructs the exact state from that event history and never re-rolls random outcomes.
- Snapshot components and diary rows remain rebuildable caches; reads do not write repaired state back.
- Command and query responsibilities remain strict; this slice changes only the command repository load consistency boundary.
- Each ordinary PR to `develop` advances `Directory.Build.props` once to the next unique `0.1.0-dev.N` identity.
- Use a fresh worktree from refreshed `origin/develop`, publish a PR to `develop`, and merge only after the exact PR head passes local and hosted canonical gates.

## Review Focus

- A writer commits between command-load queries: the command load must return a coherent pre-write state from its established snapshot, and a fresh load must return the committed post-write state.
- A test must fail when the command load uses per-query `ReadCommitted` snapshots; a sleep, mocked repository or identical before/after projection is not sufficient.

---

### Task 1: Retire the completed retry-classification slice and establish this successor

**Files:** `.agents/plans/2026-10-09-persistence-retry-classification.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`.

**Consumes:** PR #204's merged source, merge, matching tree, development identity and hosted canonical gate evidence on `develop`.

**Produces:** The current row 07 plan reference, recorded PR #204 completion evidence, retired predecessor plan and the next development identity in the first substantive commit.

- [x] Verify PR #204 merged to `develop` from source `6fdea1f7cdaa92719f31668ce3826e6143928899`, source and merge tree are `b8014c58d64b7aed2aae92afc74492d7d2847c1a`, and hosted canonical run `37868234493` passed.
- [x] In the first substantive commit, set this plan as row 07's current plan, record PR #204 source, merge `3acf6257c9c5f750432796e679575781041a0c9a`, tree, version `0.1.0-dev.19`, and hosted gate; summarize exact event-sequence retry classification and retire the completed predecessor plan.
- [x] Advance `Directory.Build.props` once from `0.1.0-dev.19` to `0.1.0-dev.20` in that same first substantive commit; do not hand-edit generated web version output.
- [x] Keep the plan-only commit separate and first; inspect the staged roadmap, predecessor retirement and version diff before committing.

### Task 2: Prove command loads span one database snapshot

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`.

**Consumes:** The production PostgreSQL repository, a persisted active session with a real event stream, and a test-only `DbCommandInterceptor` that can pause the second `GameSessions` reader query without using sleeps.

**Produces:** A regression test demonstrating one command load sees a coherent pre-append state while a writer commits a purchase, followed by a fresh load observing the complete post-append state.

- [x] Add a test-only interceptor that counts only `GameSessions` reader queries for the command loader, signals after the second envelope query has returned its reader, waits on an explicit release gate, and always releases the reader in `finally`.
- [x] Create and persist a valid session with the production repository; create the reader context with the interceptor and start `GetByIdAsync` until it pauses after the second envelope read.
- [x] In an independent context, load the persisted aggregate, resolve the current town's food offer, purchase food, and commit with `EfGameSessionUnitOfWork`; record the writer's resulting cash, food count, event sequence and stream version.
- [x] Release the reader and assert its aggregate reflects the complete pre-purchase state and version, not a mixture of the old envelope and new cache/events. Use current source behavior as the expected RED: the reader's later per-query commands can observe the committed purchase while retaining the previously read envelope.
- [x] In a fresh context, assert the repository returns the complete post-purchase state and stream version, and verify PostgreSQL contains exactly one new purchase event at the next contiguous sequence.
- [x] Run only this test against the current code and observe the intended mixed-cut failure before changing production code.

Run RED with `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~CommandLoad_ConcurrentAppendReturnsOneConsistentSnapshot"`; the expected failure must identify inconsistent reader state after the explicitly coordinated writer commit.

### Task 3: Keep the command repository load inside RepeatableRead

**Files:** `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`.

**Consumes:** The failing coordinated PostgreSQL test and the current repository's separate envelope, component-presence, snapshot/cache, diary and event queries.

**Produces:** A command-side load whose first query establishes a repeatable database snapshot retained through either snapshot materialization or event replay.

- [x] Add a single `IsolationLevel.RepeatableRead` transaction around the whole private command load operation, beginning before its first envelope query and committing after the selected fast-path or replay result is fully materialized.
- [x] Keep the existing behavior for missing sessions, stale snapshots, incomplete component sets, invalid Player cache fallback, cancellation and propagated exceptions; all queries participating in each selected load path must use the same context transaction.
- [x] Ensure the load transaction is disposed and rolled back on any exception or cancellation, and does not remain active when the aggregate is returned to the application command handler.
- [x] Rerun the focused interleaving test and confirm the reader returns the pre-write state while the fresh load returns the post-write state.
- [x] Prove the test detects the required behavior by temporarily removing or lowering the command-load isolation, rerun to observe the mixed-cut assertion fail, then restore the implementation and rerun green.

### Task 4: Record the bounded PS-07 disposition and deliver

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; this plan; changed source and tests.

**Consumes:** The focused red-green evidence and the exact staged source/document diff.

**Produces:** A dated PS-07 disposition, current row 07 status, and reviewed PR with matching branch version and hosted gate.

- [x] Add a dated PS-07 disposition identifying the exact interleaving, pre-write and post-write state assertions, test name and resulting command-load transaction behavior; preserve the original audit finding.
- [x] State in the roadmap which command-load consistency case closed and which row 07 persistence gaps remain open; keep the row executing and this plan live through its completing PR.
- [x] Compare the change with ADR-0028, event-sourcing integrity doctrine, architecture guardrails, feature matrix, backend-architecture and code-review unslop profiles; leave ADRs and feature matrix unchanged because this work enforces their existing event-history and session-consistency decisions.
- [x] Run focused PostgreSQL proof and the canonical fail-fast `py -3 tools/run.py ci --check` gate; confirm generated web identity is `0.1.0-dev.20` and no migration or event payload changed.
- [x] Complete whole-branch review against this plan, the baseline spec, PS-07, persistence doctrine and code-review runbook; record the required self-review fallback if independent reviewer dispatch remains unavailable.
- [ ] Publish and attach a Draft PR to `develop`, verify its exact source head, then mark it ready so hosted validation runs; verify the canonical gate passes on that exact SHA before merging under active epic authorization, fast-forward `Z:\wild-bunch`, and clean only this verified merged worktree and branch; retain this plan until its next row 07 successor classifies it.

The local commit hook runs `py -3 tools/run.py ci --check` against the staged candidate. Confirm generated `src/WildBunch.Web/dist/version.json` reports `0.1.0-dev.20`; verify no migration or event payload file changed with `git diff origin/develop -- src/WildBunch.Persistence/Migrations src/WildBunch.Domain/Events`. For publication, the PR head must equal local `HEAD`; hosted validation must pass on that exact SHA before the authorized merge.
