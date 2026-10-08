# Current Player Cache Shape Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task.

**Goal:** Rebuild event-backed Player state when a current-version Player cache is malformed, and let reads resume without silently losing required player facts.

**Architecture:** Keep the Player component as a shortcut cache and use the registered production event loader plus `SessionRebuilder` when its required persisted shape is invalid. Aggregate, player-read and journal-read consumers must all recover from the same event facts; queries leave the bad cache untouched, and a later normal command save writes current cache state through the existing unit of work.

**Tech Stack:** C#/.NET, EF Core, Npgsql/PostgreSQL, typed `GameSession` events, xUnit, and the repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery), [row 07 persistence roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#07-persistence-restoration-and-retries), [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [persistence investigation](../investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md#ps-04-current-version-corruption-is-either-accepted-or-throws-without-replay-recovery), and [persistence test follow-up](../investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md#mapping-every-persistence-finding-to-the-right-proof).

**Execution Strategy:** `executing-plans` inline because Player decoding, event fallback and the three persistence consumers must share one invalid-cache contract; a separate implementation context per consumer risks divergent recovery behavior.

## Global Constraints

- Start from `origin/develop` at `3fdc0d058eb72ed0ad7c04dd3906550e6cce274d` in the canonical `Z:\_agent-worktrees\wild-bunch\codex\stable-0.1-query-recovery` worktree; target the PR to `develop`.
- Advance `Directory.Build.props` exactly once from `0.1.0-dev.16` to `0.1.0-dev.17` for this PR; it remains the only authored application version.
- Events remain the authority for established player facts; rebuild through the production event upcaster/decoder path and never invent defaults for missing required cache facts.
- A query may reconstruct invalid Player state in memory but must not emit events, mutate the aggregate, or write a repaired cache row.
- A later legal command may persist recovered Player state only through the existing aggregate, repository and unit-of-work path.
- This slice covers malformed current-version Player cache shape only; it does not close corruption of other component shapes, optional-fact recovery, partial diary recovery, command-load interleaving or retry classification.
- Preserve event payload versions, upcasters, migrations and gameplay/API contracts; do not reset or delete the database.

## Review Focus

- Current-version Player JSON with `wallet: null` currently throws, while `inventory: null` currently becomes an empty inventory; both must recover the event-established values through aggregate, player-read and journal-read repositories.
- A read must not repair the stored malformed cache; a later normal command save must persist the reconstructed state and advance through the existing write path.
- Recovery must not hide unsupported or corrupt event history; if authoritative history cannot rebuild the session, the existing fail-closed event-load error must escape.
- PLAT-001 remains partial because this plan does not validate every component, recover partially missing diary rows, or make command-load reads coherent with concurrent commits.

---

### Task 1: Retire the first row 07 slice and advance its successor

**Files:** this plan; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `.agents/plans/2026-10-08-coherent-query-reconstruction.md`; `Directory.Build.props`.

- [x] Verify PR #201 merged to `develop` at `3fdc0d058eb72ed0ad7c04dd3906550e6cce274d`, reviewed source `e78c97c8d4591d209a5955bd310e1a85bcf3e472` has the same tree as the squash merge, and hosted canonical gate run 37857634884 passed on that exact source.
- [x] Classify the prior row 07 plan's full scope from its committed code, PostgreSQL behavior tests, feature evidence and hosted delivery; preserve its distinct stale-snapshot, phase-parity, query-no-writeback and coherent-read results before retiring it.
- [x] In this successor's first substantive implementation commit, replace the row 07 current-plan link, record PR #201 source/merge/version/gate facts, keep row 07 executing with its remaining obligations, and delete the completed prior plan.
- [x] Advance `Directory.Build.props` exactly once from `0.1.0-dev.16` to `0.1.0-dev.17` in that same substantive commit; do not hand-edit generated version outputs.
- [x] Keep the plan-only commit separate and first; inspect the staged roadmap, plan retirement and version diff before the normal check-only commit hook.
- [x] Verify the production web build reports `0.1.0-dev.17` before the completing PR is published.

### Task 2: Prove current-version Player cache shape loss at PostgreSQL boundaries

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`.

- [ ] Add a PostgreSQL behavior scenario that saves a valid session and purchases food so the event-established inventory and wallet differ from their initial values.
- [ ] Keep the envelope `SnapshotVersion == StreamVersion` and Player `ComponentVersion` current, then damage only the Player JSON by setting required `wallet` or `inventory` state to null; retain the original semantic assertions independently of the corrupted cache.
- [ ] Through fresh `EfGameSessionRepository`, `EfGameSessionReadRepository` and `EfGameJournalReadRepository` instances, assert the player name, cash, food quantity and purchase journal entry match the event-established state for both cache-shape cases.
- [ ] Assert all reads leave the malformed JSON and version fields unchanged; no query-side repair may satisfy the behavioral assertions.
- [ ] Add a negative case where the Player cache is invalid and persisted event history has an unsupported version; assert the load fails through the existing fail-closed history path rather than returning a plausible default.
- [ ] Run the focused PostgreSQL tests and witness the new cache-shape cases fail against the current behavior for the intended reasons: empty inventory or load exception.

### Task 3: Rebuild invalid Player cache state from loaded history

**Files:** `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`; focused integration tests from Task 2.

- [ ] Define the current Player cache's required shape explicitly: wallet, inventory and inventory items must be present; absence is invalid cache state, not a legitimate empty value.
- [ ] Make Player deserialization distinguish invalid persisted cache shape from unrelated programming or authoritative-history failures; callers must catch only that cache-specific failure.
- [ ] On aggregate load, route the invalid Player cache through the production full event-replay path; do not save from `GetByIdAsync` or rewrite events.
- [ ] On player and journal reads, rebuild one aggregate from the already-loaded production-decoded events and build both contracts from that state while preserving event-derived journal output.
- [ ] Preserve the malformed database row through query reads, then use one legal command and the existing repository/unit-of-work commit to prove the Player component is restored and a fresh load returns the same facts.
- [ ] Keep the negative unsupported-history test failing closed; do not catch event decoder, upcaster or reconstruction failures as cache damage.
- [ ] Run `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~ReadModel_CurrentPlayerCacheShapeRebuildsFromEvents|FullyQualifiedName~ReadModel_InvalidPlayerCacheDoesNotHideUnreplayableHistory"` and prove the recovery assertions fail if the Player fallback is removed.

### Task 4: Reconcile evidence and deliver

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; this plan; changed persistence and test files.

- [ ] Update PLAT-001 with the exact Player cache recovery evidence while keeping the assessment partial and distinguishing untouched component/diary/command-load/retry gaps.
- [ ] Add a dated row 07 disposition to the persistence test follow-up; preserve earlier static findings as history and do not claim general cache recovery from one Player shape.
- [ ] Compare the branch with ADR-0028, the event-sourcing integrity doctrine, architecture guardrails, feature matrix and backend unslop profile; leave the ADR unchanged if no durable boundary changes and state the reason.
- [ ] Verify no migration, event payload or upcaster changed; run focused PostgreSQL behavior tests and confirm generated version `0.1.0-dev.17`.
- [ ] Review the whole branch against this plan, the baseline spec, PLAT-001, backend architecture and code-review unslop profiles, and the code-review runbook; disclose the self-review fallback if the active runtime still forbids an independent reviewer.
- [ ] Open and attach a Draft PR targeting `develop`, verify its exact source head and hosted canonical gate, mark it ready according to the PR runbook, merge using the repository's established squash route under the active epic authorization, fast-forward `Z:\wild-bunch`, and clean only this verified merged worktree and branch while retaining this plan until the next row 07 successor classifies it.
