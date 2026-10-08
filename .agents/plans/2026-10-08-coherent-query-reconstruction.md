# Coherent Player and Journal Reads Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task.

**Goal:** Make player-session and journal query results come from one coherent event-stream position and reconstruct current state from events whenever the cached snapshot lags.

**Architecture:** `GameSessionReadStoreLoader` owns the PostgreSQL read boundary for player and journal views. It will read the envelope, components, events and diary projection inside one repeatable-read transaction; when the envelope identifies a stale snapshot it will rehydrate the aggregate from the loaded, upcasted event history and build both read contracts from that aggregate without writing repairs or emitting events.

**Tech Stack:** C#/.NET, EF Core, Npgsql/PostgreSQL, typed `GameSession` events, xUnit, and the repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery), [row 07 persistence roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#07-persistence-restoration-and-retries), [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [persistence investigation](../investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md#findings), and [persistence test follow-up](../investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md#mapping-every-persistence-finding-to-the-right-proof).

**Execution Strategy:** `executing-plans` inline because the stale-snapshot reconstruction and coherent read cut share one PostgreSQL loader, test fixture and event-derived result contract; separate implementers would duplicate setup and obscure whether one read stays consistent end to end.

## Global Constraints

- Start from refreshed `origin/develop` at `c6c5b52efdbdde3a860be5438634531a741a4072` in the canonical `Z:\_agent-worktrees\wild-bunch\codex\stable-0.1-persistence` worktree; target PRs to `develop`.
- Advance `Directory.Build.props` exactly once from `0.1.0-dev.15` to `0.1.0-dev.16` for this PR; `Directory.Build.props` remains the only authored application version.
- Events remain the only authority for established facts. Rehydration applies persisted, upcasted events and never samples a new salt or reruns generation.
- Player and journal reads must use one database snapshot for the envelope, components, ordered events and diary rows; use PostgreSQL `RepeatableRead` for this query boundary.
- A stale snapshot is rebuilt in memory from the loaded event stream. Query handlers do not emit events, write repaired cache rows, mutate game state or invent fallback values.
- Preserve event schema versions, event upcasters, migrations and the existing command/repository mutation boundary. This slice does not fix malformed-current cache validation, partial diary recovery, command-load interleaving or retry classification; those remain row 07 work.
- Preserve the existing one-map, setup, archive and player-surface contracts; do not add APIs, account isolation or gameplay features.

## Review Focus

- A current-schema Player component can still describe an older stream position; a read must report the event-established purchase result and must not write a query-side repair.
- A writer can commit after the reader fetches its envelope but before its component/event queries; the returned status and journal occurrence must describe one side of that commit.
- A persisted `StartingTownSelected` event without `GameStarted` must report `StartingTownSelected` through the read repository, matching the aggregate repository.

---

### Task 1: Record row 06 delivery and bootstrap row 07

**Files:** This plan; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`; delete `.agents/plans/2026-10-08-replay-complete-player-genesis.md`.

- [x] Verify PR #200 is merged into `develop` at `c6c5b52efdbdde3a860be5438634531a741a4072`, its exact reviewed source is `bd2523205dd653166885ac3cf1dad57106c99711`, and hosted canonical gate run 37852123315 passed.
- [x] Classify row 06 as shipped from its complete implementation and acceptance evidence; confirm ADR-0028 and durable replay guidance remain truthful, then retire the row 06 plan and stale roadmap links in this successor's first substantive commit.
- [x] Mark roadmap row 06 done with PR #200, merge/source SHAs and hosted run; mark row 07 executing with this plan and identify this as the first read-side consistency slice while retaining the other row 07 obligations.
- [x] Commit this plan by itself first; then record its commit SHA in the roadmap during the first implementation commit, without another version change.
- [x] After the plan-only commit, stage only the roadmap, version and eligible predecessor retirement; inspect the staged diff and commit through the check-only hook.
- [x] Verify generated `version.json` reports `0.1.0-dev.16` and the worktree remains clean after the task commit.

### Task 2: Rebuild stale player and journal views from events

**Files:** `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`; `src/WildBunch.Persistence/GameSessions/SessionRebuilder.cs`; `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`.

- [x] Add a PostgreSQL behavior test that saves a valid playthrough, captures its Player cache, purchases food through `GameSession.Purchase`, commits the resulting event, then restores only the old Player JSON and marks `SnapshotVersion` behind `StreamVersion`.
- [x] Through fresh `EfGameSessionReadRepository` and `EfGameJournalReadRepository` instances, assert the player cash/inventory reflect the committed purchase, the journal reflects the purchase event, and the stale component/version remain unchanged in PostgreSQL after both reads.
- [x] Witness the new test fail against the existing loader because it trusts the old current-schema Player cache; record the observed stale value as the intended RED.
- [x] Add the intermediate-phase assertion to the read contract: a persisted history ending in `StartingTownSelected` and lacking `GameStarted` returns `StartFlowPhase.StartingTownSelected`, matching `EfGameSessionRepository`.
- [x] On `SnapshotVersion != StreamVersion`, rebuild one aggregate from the already-loaded production-decoded events and construct both read contracts from that aggregate plus event-derived diary projection; keep the current cache path for a current snapshot and include the `StartingTownSelected` event in its phase derivation.
- [x] Run `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~ReadModel_StaleSnapshotRebuildsPlayerAndJournalFromEvents|FullyQualifiedName~ReadModel_StartingTownSelectedRestoresPhase"` and verify the stale test fails if the event-rebuild branch is removed.

### Task 3: Keep query reads on one PostgreSQL snapshot

**Files:** `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`; `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`.

- [x] Add a test-only EF command interceptor with `TaskCompletionSource` barriers that pauses the reader after its envelope query; do not use sleeps or scheduler timing as the synchronization contract.
- [x] Start real PostgreSQL player and journal reads and pause both after their envelope queries; from a fresh writer context purchase food and then call `ArchivePlaythrough("start-over")` before one commit, then release both readers. Assert each read returns active status with no purchase journal entry, and subsequent fresh reads return archived status with the purchase entry.
- [x] Witness the interleaving test fail without a transaction because the old active envelope can be combined with the newly committed Player cache and purchase event.
- [x] Keep the complete `GameSessionReadStoreLoader.LoadStoreAsync` query sequence inside an EF transaction at `IsolationLevel.RepeatableRead`, disposed after all four query groups have materialized.
- [x] Run `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~ReadModels_ConcurrentArchiveReturnsOneCoherentState|FullyQualifiedName~ReadModel_StaleSnapshotRebuildsPlayerAndJournalFromEvents|FullyQualifiedName~ReadModel_StartingTownSelectedRestoresPhase"`.

### Task 4: Reconcile evidence and deliver

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; this plan; all Task 2-3 implementation and test paths.

- [ ] Update PLAT-001 evidence to distinguish the new stale-read/coherent-query proof from still-unproven command-load interleaving, malformed cache recovery and partial diary restoration; keep its overall assessment partial.
- [ ] Add a dated persistence follow-up for the completed PS-03/PS-07 query-read slice and the still-live row 07 scenarios; do not rewrite the static investigation findings as though they never existed.
- [ ] Compare the committed diff against ADR-0028 and the event-sourcing integrity doctrine; leave ADR-0028 unchanged if the work only implements its existing event-authority/cache-rebuild decision, and state why in the plan.
- [ ] Verify no migration, event payload or upcaster version changed; confirm generated web identity is `0.1.0-dev.16` and run focused tests for the affected PostgreSQL behavior.
- [ ] Review the whole branch against this plan, the baseline spec, PLAT-001, backend architecture and code-review unslop profiles, and the code-review runbook; use and disclose the self-review fallback because the active runtime forbids subagents.
- [ ] Open and attach a PR targeting `develop`; verify the exact source head and successful hosted canonical gate, merge under the active epic authorization, fast-forward `Z:\wild-bunch`, and clean this verified merged worktree and branch while retaining this plan until the next row 07 slice classifies it.
