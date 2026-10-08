# Make Player Creation and Recorded Randomness Replay-Complete

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** A normally created playthrough records enough generated facts to reconstruct its setup phase and subsequent legal action exactly from persisted event history, without inventing a salt during replay or cache recovery.

**Architecture:** `CompletePlayerSetupHandler` owns the normal creation command. `PlayerSetupCompleted`, `WorldGenerated` and `CaseFileGenerated` record the accepted setup and settled generated facts; production persistence codecs serialize and restore those facts. Snapshots remain disposable caches. Replay applies event facts and never calls entropy sources. The event stream, not the snapshot, remains authoritative.

**Tech Stack:** C#/.NET Domain, Application, GameContent and Persistence; ASP.NET integration host; xUnit; PostgreSQL-backed integration tests; repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), [PG-001 start contract](../../docs/features.md#pg-001-start-a-hunt-and-choose-the-first-town), [event-sourcing integrity doctrine](../doctrine/event-sourcing-integrity.md), and [hunt creation contract](../investigations/stable-0.1.0/2026-10-07-hunt-creation-contract.md).

**Execution Strategy:** `executing-plans` inline. Setup creation, generated event facts, event codecs, replay and salt restoration are a single producer-to-persistence path; one integration context is needed to prove that the state created by the normal command is recoverable from its real event history.

## Global Constraints

- Start from merged `origin/develop` commit `9d97d667cfb5e3e8b8ec0ec604cf41cfc3e64577`; deliver this plan and implementation by PR to `develop` from this dedicated worktree.
- Advance `Directory.Build.props` exactly once from `0.1.0-dev.14` to `0.1.0-dev.15` for this PR. The web build derives its version identity from this authority; do not add another authored version.
- Preserve the semantic flow `CompletePlayerSetup -> prologue -> starting-town selection -> free first arrival`. This slice makes backend genesis replay-complete; it does not implement the browser setup/resume work owned by row 09 or change phase ownership.
- Events record facts that happened. Initial generation may use the injected `ISaltSourceFactory`; event application, rehydration and repository recovery must not generate or replace random facts.
- Normal setup history includes `PlayerSetupCompleted`, `WorldGenerated` and `CaseFileGenerated`; preserve the current `WorldGenerated.CaseFile` payload and `WorldGenerated` v2 upcaster behavior. Do not change event schema versions, upcasters, event ordering, applied migrations, or the generation algorithm.
- A setup stream without its required `WorldGenerated` fact is invalid and must fail closed. Preserve the supported direct/legacy `GameStarted` replay path when its event contains the recorded salt.
- If a persisted salt cache component is absent, recover the exact salt from `WorldGenerated` through the event loader. Never use `SaltSource.CreateRuntime()` as a cache or replay fallback. Keep broader partial/stale cache restoration for row 07.
- Use behavior tests with independent expected facts and a witnessed RED. Do not add source-text, type-map inventory, event-count, or generated-code detector tests.
- Consult ADR-0028 through the decision-record playbook. Existing doctrine and ADR already settle event authority and replay semantics; do not change the ADR unless implementation evidence proves its decision false.

## Review Focus

- A normal API player-setup command resolves a generated world once, records its salt and generated world/case facts, and persists events that the production loader deserializes and replays into the same setup phase.
- Repeated event replay and a legal next prologue acknowledgement preserve the recorded seed, difficulty, entropy, salt, layout and case facts without another salt-factory call.
- Setup history lacking `WorldGenerated` is rejected; a legacy/direct `GameStarted` stream continues to restore its recorded salt.
- Removing the salt cache component causes the repository's event-only path to recover the original recorded value. Missing historical authority fails closed.
- PG-001 remains assessed as partial while browser setup and full start-flow proof remain outstanding.

---

### Task 1: Bootstrap row 06 and retire completed row 05

**Files:** This plan; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`; delete `.agents/plans/2026-10-08-retire-town-service-variation.md`.

- [x] Record PR #199 as merged to `develop` at `9d97d667cfb5e3e8b8ec0ec604cf41cfc3e64577`, source head `68f761e2df3ca9e4e2385366c754b691adaeb84d`, and hosted canonical gate run 37846121806.
- [x] Classify all row 05 retirements as shipped; verify durable feature truth and relevant investigation outcomes are already present, then retire the completed row 05 plan and stale roadmap link.
- [x] Mark roadmap row 05 done and row 06 executing with this plan, version `0.1.0-dev.15`, and the row 05 delivery evidence above.
- [x] Advance `Directory.Build.props` exactly once from `0.1.0-dev.14` to `0.1.0-dev.15`.
- [x] Stage only the plan, roadmap, version and eligible predecessor retirement; inspect the staged diff and commit through the check-only hook before implementation.
- [x] Record the exact plan-commit SHA in roadmap row 06 in a follow-up docs-only commit before implementation; do not bump the version again.

### Task 2: Reject incomplete genesis and remove salt invention from restoration

**Files:** `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`; `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs`; `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; `tests/WildBunch.Integration.Tests/GameSessionDifficultyPersistenceTests.cs`; `tests/WildBunch.Integration.Tests/FullReplayEqualityTests.cs`.

- [x] Add a Domain negative test with a valid `PlayerSetupCompleted` event but no `WorldGenerated`; witness current replay incorrectly succeeds with a runtime salt, then require an explicit failure. Preserve and run the existing direct `GameStarted` replay test with its recorded salt.
- [x] Add a PostgreSQL repository behavior test that stores a normally event-backed session, removes only its salt-source cache component, loads through a fresh repository context, and asserts the exact salt from its `WorldGenerated` event is restored. Witness failure against the current fast path/fallback before changing production code.
- [x] Make setup replay require `WorldGenerated` and remove runtime salt creation from setup replay. Keep legacy/direct `GameStarted` streams valid only from their recorded salt.
- [x] Route a missing salt cache component through full event replay and fail closed when neither supported genesis event contains the recorded salt. Remove runtime salt fallbacks from production snapshot/component deserialization; do not synthesize a replacement.
- [x] Retire the runtime-salt success expectation from `MissingSaltSourceInLegacySessionJsonFallsBackToRuntimeSalted`; fail closed for the malformed snapshot and cover the production event-backed recovery through the PostgreSQL test above.
- [x] Run the focused Domain replay and PostgreSQL repository tests, including the supported `WorldGenerated` legacy upcast tests.

### Task 3: Prove normal player genesis through production codecs and replay

**Files:** `tests/WildBunch.Integration.Tests/TestInfrastructure/PostgreSqlApiFactory.cs`; `tests/WildBunch.Integration.Tests/Acceptance/PlayerSetupReplayAcceptanceTests.cs` (new); `src/WildBunch.Api/Games/GameSessionEndpoints.cs`; `src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs`; `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`; `src/WildBunch.Persistence/GameSessions/SessionRebuilder.cs`.

- [x] Add an acceptance test through `POST /api/games/setup` using a known seed and a counting fixed `ISaltSourceFactory`. Use the real Application handler, EF store, registered event codec and production event loader.
- [x] Read the persisted event stream through a fresh service scope and assert independently expected player name, seed, difficulty, entropy, recorded salt, generated town/layout facts, and case/culprit facts from `WorldGenerated` and `CaseFileGenerated`.
- [x] Rehydrate from the deserialized stored events, assert the setup phase and those same facts, then apply the legal prologue acknowledgement to both command and replayed sessions and compare the resulting phase/history. Assert replay did not increment the generation salt-factory count.
- [x] Force the repository full event-replay path for this generated setup and verify it yields the same recorded salt and next legal phase. Do not broaden this into row 07's general cache-damage matrix.
- [x] Witness the acceptance test fail if event codec registration or one produced setup event is missing, rather than testing the serializer type map by enumeration.
- [x] Run focused Application, GameContent and Integration tests. Start the non-destructive local PostgreSQL helper with `.\tools\postgres-dev.ps1 ensure` before database tests; never drop/reset the developer database.

### Task 4: Reconcile evidence and deliver

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; all implementation and test paths from Tasks 2-3.

- [x] Update PG-001's evidence to record backend event-backed genesis and production persisted replay coverage, while keeping the complete capability partial because browser setup, resume and start-flow behavior remain unproven.
- [x] Add a dated persistence-test follow-up describing the retired runtime-salt fallback assertion, its production replacement, and remaining row 07 cache-restoration findings.
- [x] Review the change against ADR-0028 and current event-sourcing doctrine; no ADR change is needed because immutable event history remains authoritative, event replay uses recorded facts, and caches remain rebuildable. The reviewed diff implements the current decision.
- [x] Run focused tests and the commit hook's `py -3 tools/run.py ci --check`; verify no migration/event schema version changed and the generated web version identity resolves to `0.1.0-dev.15`.
- [x] Review the whole branch against this plan, baseline spec, feature matrix, backend architecture unslop profile, and code-review runbook. Independent review dispatch is unavailable under the active no-subagents runtime policy; the implementing agent used the documented self-review fallback, inspected the committed diff and relevant decision/replay boundaries, and found no open findings.
- [ ] Open and attach a PR targeting `develop`; verify exact source head, successful hosted canonical gate and merged state. Keep this plan until the next successor slice classifies and retires it.
