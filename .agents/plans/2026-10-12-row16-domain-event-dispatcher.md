# Domain Event Dispatcher Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every `GameSession` command-produced event and replayed event pass through one authoritative event dispatcher, closing the live dispatcher-drift portion of DN-14 without changing game behavior.

**Architecture:** `GameSession` owns event production and `Apply` mutation under ADR-0028 and ADR-0038. Convert the replay dispatcher into the single instance dispatcher used by both `ProduceEvent` and `RehydrateFromEvents`; route the existing setup-flow event methods through `ProduceEvent` so the same fact-to-state path serves player start and replay. Preserve event order, payloads, and resulting state.

**Tech Stack:** C#/.NET 10, xUnit, Git, repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 16 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; DN-14 in `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline. The dispatcher and setup command changes share one event-authority invariant and must be changed and verified together; separate implementer handoffs would duplicate source context without yielding independently reviewable behavior.

## Global Constraints

- Keep `GameSession` as the sole command boundary; command methods decide intent, typed facts are applied through `Apply`, and replay never rerolls or re-decides.
- Preserve all emitted event types, order, payloads, game state, error behavior, persistence schema, and historical upcaster behavior.
- Do not add a test that snapshots or compares dispatcher switch inventories; use existing start-flow and replay behavior tests and add a test only if source inspection establishes a genuine uncovered observable invariant.
- Keep unrelated DN-14 dead helpers, duplicated predicates, citizen convenience helpers, refresh-policy questions, and legacy convenience methods out of this slice.
- Advance `Directory.Build.props` exactly once from `0.1.0-dev.108` to `0.1.0-dev.109` and retire the `.108` plan only in this successor PR.
- Record PR #293 reviewed source `235baae6d63af8277c96f82abb88b68ebac5a759`, merge `850e86d9e0c9d7367d1c9a9578b0989168134a77`, passing exact-head gate `38223212827`, passing develop push gate `38223752149`, clean fresh whole-branch review, primary fast-forward, and verified worktree/local/upstream ref cleanup in the roadmap.
- Deliver by reviewed PR to `develop`; verify its exact head and hosted gate before merge. After merge, fast-forward the primary checkout, remove the clean worktree first, delete its paired local/upstream branch refs, and verify all three absences before the next plan.

## Review Focus

Verify that setup, prologue, starting-town selection, and game-start commands still emit their established facts and state, while event reconstruction reaches the same start-flow state from that event sequence.

---

### Task 1: Commit the JIT plan before implementation

**Files:**
- Add: `.agents/plans/2026-10-12-row16-domain-event-dispatcher.md`

- [x] Commit this plan by itself before changing Domain code, the investigation, roadmap, or version.

### Task 2: Use one event dispatcher for command production and replay

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`
- Verify: `tests/WildBunch.Domain.Tests/Events/StartFlowEventSourcingTests.cs`
- Verify: `tests/WildBunch.Domain.Tests/Game/GameSessionEventReplayTests.cs`

- [x] First run the focused existing start-flow and event-replay tests to confirm the current behavior baseline.
- [x] Move the typed event switch from `ApplyProducedEvent` into one instance `ApplyEvent` dispatcher available to `GameSession` partials; keep every currently supported replay event case and the explicit unknown-event failure.
- [x] Route `ProduceEvent` through the shared dispatcher before appending to `UncommittedEvents`.
- [x] Route `StartSetup`, `ViewPrologue`, `SelectStartingTown`, and `CompleteGameStart` through `ProduceEvent`, preserving their current event order and idempotent/rejection behavior.
- [x] Remove the duplicate production dispatcher and the unused `firstEvent` local; do not add a second event inventory or reflection-based dispatch.
- [x] Run `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~StartFlowEventSourcingTests` and `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~GameSessionEventReplayTests`; both must pass against the command and replay paths.
- [x] Inspect the focused diff and `git diff --check`; confirm no event payload, migration, upcaster, or externally visible gameplay change was introduced.

### Task 3: Correct the DN-14 disposition

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`

- [x] Preserve DN-14's original observation and add a dated disposition describing the shared command/replay dispatcher, setup-flow routing, and removal of the unused replay local.
- [x] Mark only the dispatcher-drift and unused-local portions closed; leave each unrelated DN-14 observation open for its own JIT disposition.
- [x] Correct the per-file entries for `GameSession.cs` and `GameSessionEventReplay.cs` without rewriting the original audit snapshot.
- [x] Keep ADR-0028 and ADR-0038 unchanged because their current decisions already describe the intended authority; make no feature-matrix change because no product promise or dependency changed.

### Task 4: Retire `.108` and route the `.109` successor

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-domain-occurrence-truth.md`

- [x] Record PR #293's reviewed source and merge identities, exact-head and develop-push gate results, fresh review, and verified paired worktree/branch cleanup; route this `.109` plan as the active row-16 slice.
- [x] Advance the product version exactly once to `0.1.0-dev.109` and retire the `.108` plan in this successor.
- [x] Run the normal check-only commit hook on the staged candidate; it must complete the canonical repository gate before allowing the commit.

**Validation:** Existing `StartFlowEventSourcingTests` and `GameSessionEventReplayTests` cover the observable start-flow facts and reconstructed phase. Do not add structural dispatcher-parity tests; if either existing suite fails to detect an actual behavior loss introduced by the refactor, identify that observable gap and add the smallest independent behavior test before continuing.
