# Travel Diary Cache Completeness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover a missing or gapped travel-diary cache from event history and let a later legal save restore its ordered rows without changing authoritative history.

**Architecture:** Keep travel-diary rows as a cache of the event-derived `TravelDiaryDayProjector`, with a persisted applied-stream watermark and day count on the session envelope. The fast path verifies the watermark, count, row versions and contiguous sequence keys without projecting the event stream on every healthy load; an invalid cache rebuilds from events. A later command save reconciles rows by their sequence keys through the existing repository and Unit of Work.

**Tech Stack:** C#/.NET, EF Core, Npgsql/PostgreSQL, typed `GameSession` events, xUnit, and the repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery), [row 07 persistence roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#07-persistence-restoration-and-retries), [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [persistence investigation](../investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md#ps-06-diary-cache-completeness-is-not-checked), and [persistence test follow-up](../investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md#mapping-every-persistence-finding-to-the-right-proof).

**Execution Strategy:** `executing-plans` inline because event-derived completeness, PostgreSQL reads and same-UoW row repair share the same ordered projection contract and need one continuous falsification/recovery test context.

## Global Constraints

- Start from `origin/develop` at `11a87a61be2676d1991b6bce79fea2f6baa6b681` in the canonical `Z:\_agent-worktrees\wild-bunch\codex\stable-0.1-projection-restoration` worktree; target the PR to `develop`.
- Advance `Directory.Build.props` exactly once from `0.1.0-dev.17` to `0.1.0-dev.18` in the first substantive implementation commit; it remains the only authored application version.
- Immutable typed events remain authoritative; diary rows are a reconstructible cache, and the production event decoder plus `TravelDiaryDayProjector` define expected diary days.
- Reads may rebuild the diary in memory but must not modify diary rows, events, stream versions or snapshots.
- A later legal command save repairs rows through `EfGameSessionRepository` and the existing unit of work; no query-side or independent commit may repair them.
- Keep this slice to missing/gapped diary rows and their ordered repair, with stream-position/count metadata for the diary projection only. Other malformed diary payloads, missing or optional component recovery, command-load interleavings, per-component watermark redesign, retry classification and unrelated persistence cleanup remain outside this plan.
- Preserve existing migrations, event payload versions and gameplay/API contracts. Add a forward migration for nullable diary-projection metadata only; do not delete playthrough data or rewrite migration history.

## Review Focus

- A trailing diary row is missing while all remaining rows have the current schema version; aggregate and player-read results must include the event-established final day.
- A middle diary row is missing, leaving a sequence gap; reads must restore the full ordered diary, and the later normal save must restore contiguous row keys and the correct payload at every sequence.
- Read recovery must not repair persisted rows or advance the snapshot/event stream; assert the stored sequence and payloads before and after all reads.
- A current-version orphan diary row with no corresponding event-derived day is invalid cache, not authority; do not retain the low-level test that accepts a fabricated row against an empty event stream. Null legacy watermarks are also unverified and must rebuild.

---

### Task 1: Retire the completed cache-shape slice and set this successor

**Files:** this plan; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `.agents/plans/2026-10-09-player-cache-shape-recovery.md`; `Directory.Build.props`.

- [x] Verify PR #202 merged to `develop` at `11a87a61be2676d1991b6bce79fea2f6baa6b681`, source `010ddce1b9f6dc5ab812fa3aa7ec71f150ce7355` has the same tree as the squash merge, and hosted canonical run `37861616672` passed on that source.
- [x] In the first substantive implementation commit, make this the row 07 current-plan link, record PR #202's source, merge, version and hosted gate facts, summarize the Player-cache recovery evidence, and retire the completed Player-cache plan.
- [x] Advance `Directory.Build.props` exactly once from `0.1.0-dev.17` to `0.1.0-dev.18` in that same first substantive implementation commit; do not hand-edit generated web version output.
- [x] Keep the plan-only commit separate and first; inspect the staged roadmap, prior-plan retirement and version diff before the normal check-only commit hook.

### Task 2: Reproduce partial current-version diary loss at PostgreSQL boundaries

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; `tests/WildBunch.Integration.Tests/MigrationTests.cs`; `tests/WildBunch.Integration.Tests/Versioning/VersionMismatchBehaviorTests.cs`.

- [ ] Add an integration scenario that creates an active journey with at least four planned days, advances and persists three days through the production aggregate/repository with deterministic travel inputs, and proves a fourth legal advance remains available.
- [ ] In separate test cases, remove one trailing row and one interior row while retaining current `SchemaVersion`, envelope `SnapshotVersion == StreamVersion`, all authoritative events and the remaining row payloads.
- [ ] Through fresh aggregate and `EfGameSessionReadRepository` instances, assert the complete ordered diary matches independent facts derived from the persisted event stream, including each expected day number and travel result.
- [ ] Assert read recovery leaves the missing row missing and preserves existing payloads, row schema versions, envelope versions and event count.
- [ ] In the interior-gap case, advance the loaded active journey by one legal day and save through the existing repository/unit-of-work path; verify the persisted sequence keys are exactly `0..N-1`, each payload describes its matching event-derived day, and a fresh aggregate/read load retains all days.
- [ ] Add a PostgreSQL negative case with an orphan current-version diary row and no event-derived travel day; assert the read returns no diary days and does not change the orphan row.
- [ ] Add a PostgreSQL case where existing diary rows are complete but both new envelope watermark fields are null; assert event-backed diary recovery and no query-side metadata writeback.
- [ ] Remove `LoadDiaryDays_CurrentVersion_UsesStoredJson` from `VersionMismatchBehaviorTests`: it asserts an invented current cache row is trusted even when the event stream establishes no diary day, contradicting event authority and the completeness requirement. The PostgreSQL boundary cases replace its invalid contract.
- [ ] Update version-mismatch loader tests to pass stream and cache metadata explicitly; retain meaningful stale/mixed-version rebuild behavior.
- [ ] Run the focused PostgreSQL tests before changing the loader and confirm at least the trailing-row case returns a truncated diary on the current code; preserve the interior-gap save failure or wrong-key behavior as an observed independent failure if the baseline reaches it.

### Task 3: Validate diary completeness and repair by sequence identity

**Files:** `src/WildBunch.Persistence/GameSessions/GameSessionEntity.cs`; `src/WildBunch.Persistence/Versioning/PersistedPayloadLoader.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`; `src/WildBunch.Persistence/Migrations/WildBunchDbContextModelSnapshot.cs`; `src/WildBunch.Persistence/Migrations/`; `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; `tests/WildBunch.Integration.Tests/MigrationTests.cs`; `tests/WildBunch.Integration.Tests/Versioning/VersionMismatchBehaviorTests.cs`.

- [ ] Add nullable `TravelDiaryProjectionStreamVersion` and `TravelDiaryProjectionDayCount` metadata to `GameSessionEntity` and a forward EF migration; nullable legacy metadata means the cache is unverified and must rebuild.
- [ ] In `StoreAsync`, stage the diary watermark as `session.Version` and the day count as `session.TravelDiaryDays.Count` in the same transaction as event append, snapshot and diary-row synchronization.
- [ ] Change `LoadDiaryDays` to accept `stored`, `events`, envelope `streamVersion`, nullable `projectionStreamVersion` and nullable `projectionDayCount`; use stored rows only when the watermark equals `streamVersion`, the count equals stored row count, every row has current `SchemaVersion`, and sequences are exactly contiguous zero-based indices. Otherwise project once from the already decoded full event list and return without saving.
- [ ] In `SyncDiaryDaysAsync`, match retained rows by `Sequence`, update only the row for that desired day, insert absent desired sequences, and remove rows whose sequence is outside the desired projection; do not associate a database row with a diary day by its position in a possibly gapped query result.
- [ ] Make the failing PostgreSQL cases pass and confirm both aggregate and read repositories consume the same reconstructed ordered days; ensure aggregate and query loads pass the same envelope watermark/count to the loader.
- [ ] Run `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~ReadModel_PartialDiaryDayCacheRebuildsFromEvents|FullyQualifiedName~ReadModel_OrphanDiaryDayCacheDoesNotOverrideEventHistory"` with the shared PostgreSQL service ensured.

### Task 4: Reconcile evidence and deliver

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; this plan; changed persistence and test files.

- [ ] Update PLAT-001 with the exact partial-diary recovery and sequence-repair evidence while retaining its partial assessment and remaining restoration limits.
- [ ] Add a dated PS-06 disposition to the persistence test follow-up; preserve the original static finding and record only what the source and PostgreSQL behavior tests prove.
- [ ] Compare the actual diff with ADR-0028, the event-sourcing integrity doctrine, architecture guardrails, feature matrix and backend/code-review unslop profiles; leave the ADR unchanged if no durable boundary changed and state why.
- [ ] Verify the only schema change is the additive nullable diary-cache metadata migration; no destructive migration, event payload or upcaster changed. Confirm generated web version `0.1.0-dev.18`, run focused PostgreSQL/migration scenarios and the canonical `py -3 tools/run.py ci --check` gate.
- [ ] Complete whole-branch review against this plan, the baseline spec, PLAT-001, backend architecture and code-review unslop profiles, and the code-review runbook; disclose self-review if the active runtime still forbids independent reviewer dispatch.
- [ ] Publish and attach a Draft PR to `develop`, verify its exact source head and hosted canonical gate, mark it ready, merge via the established squash route under active epic authorization, fast-forward `Z:\wild-bunch`, and clean only this verified merged worktree and branch; retain this plan until its next row 07 successor classifies it.
