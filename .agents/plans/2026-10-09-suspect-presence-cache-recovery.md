# Recover the WantedSuspectPresenceLedger Cache Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Recover event-established wanted-suspect presence when its optional cache row is missing or malformed, so a later command does not treat a confronted suspect as never encountered.

**Architecture:** `WantedSuspectConfronted` records the outcome and choice that update the wanted-suspect presence ledger. The `wantedSuspectPresenceLedger` component is a cache. Command loading must replay the ordered event history when a missing row follows an event that establishes ledger state, or when a present row has an invalid cache shape. A history with no state-establishing confrontation may legitimately produce an empty ledger. In particular, this slice does not claim that `AvailableInTown` is event-backed; the existing saloon setup path seeds that state directly and its broader event-sourcing contract remains outside this cache-recovery change. Reads never write repaired cache rows; a later ordinary unit-of-work save may repair them.

**Tech Stack:** .NET 10, C#, EF Core, PostgreSQL, xUnit, existing event replay and persistence loaders.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially event-history authority, rebuildable caches and fail-closed history.

**Execution Strategy:** `executing-plans` - the persistence fallback, typed cache decoding and PostgreSQL proof share one command-load boundary and are tightly coupled; inline sequential TDD preserves the context needed to distinguish cache damage from unreplayable history.

## Global Constraints

- Preserve the event stream as the only authority for what happened; a cache is rebuildable state, not history.
- Keep CQRS strict. This component is consumed by command aggregate state; do not broaden query repositories to load it or write repaired state during reads.
- Recover only presence state that ordered supported events establish. Do not infer `AvailableInTown`, add a new event, or retrofit initial saloon availability in this slice.
- Replaying a valid stream with no state-establishing confrontation yields an empty ledger; abandoned or rejected encounters do not invent a ledger entry.
- Invalid authoritative event history must still fail through replay; cache recovery must not hide event decode or reconstruction failures.
- Prove read no-writeback, ordinary later-save repair, exact presence state after fresh reload, and unchanged event/envelope/diary facts during recovery.
- Do not alter event payloads/versions, migrations, domain feature behavior, query projections, or ADRs unless JIT implementation inspection proves an existing durable decision would become untrue.
- Advance `Directory.Build.props` once for this PR from `0.1.0-dev.26` to `0.1.0-dev.27`; it remains the only authored application version.
- Use the canonical fail-fast `py -3 tools/run.py ci --check` gate and the repository's `develop` PR delivery contract.

## Review Focus

- Removing the ledger after a persisted non-abandoned confrontation must restore the event-established `GoneToGround`, `SecuredAlive` or `SecuredDead` state through command replay.
- Missing state before any state-establishing confrontation must not conjure an entry from an unrelated event or cached state.
- JSON `null` is syntactically valid but not a valid ledger cache shape; it must use typed cache recovery without rewriting storage during reads.
- An undecodable authoritative `WantedSuspectConfronted` event must fail closed when the cache is missing or malformed.
- A later legal command save must repair the cache through the normal repository/unit-of-work path.
- The test must not treat test-only direct seeding of `AvailableInTown` as proof that this state is replayable.

---

### Task 1: Save the JIT plan before implementation

**Files:** `.agents/plans/2026-10-09-suspect-presence-cache-recovery.md`.

**Consumes:** Refreshed `origin/develop` at PR #211 merge `4918530154f161e39329a2deebfcbd13a5a9f647`.

**Produces:** A committed plan for missing and malformed wanted-suspect-presence cache recovery, before changing predecessor artifacts, version identity, tests or implementation.

- [x] Confirm the presence transitions in `BountyLoop.Apply(WantedSuspectConfronted)`, ordered event replay, command loading and the production PostgreSQL fixture. Keep the direct `AvailableInTown` seed distinction explicit.
- [ ] Commit only this plan as the first branch commit; record its SHA in this plan after commit.

### Task 2: Retire the completed predecessor and record verified delivery

**Files:** `.agents/plans/2026-10-09-current-action-context-cache-recovery.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `docs/features.md`; `Directory.Build.props`.

**Consumes:** PR #211 merged source `0e54b9835009fb90ffd04ce09c7aa26f203a77da`, merge `4918530154f161e39329a2deebfcbd13a5a9f647`, and hosted canonical gate run `37892336842` passing on that exact source.

**Produces:** A truthful PR #211 delivery record, a narrow completion disposition for the retired predecessor, an honest remaining row 07 gap list, and `0.1.0-dev.27`.

- [ ] In the first substantive implementation commit, record PR #211's target, source head, merge commit and exact-head hosted gate result in row 07; record the delivered missing/null-root `currentActionContext` behavior and retire `.agents/plans/2026-10-09-current-action-context-cache-recovery.md` with stale links removed.
- [ ] Update the row 07 investigation and PLAT-001 only with behavior this slice proves; preserve the finding that direct `AvailableInTown` seeding is not event-established and leave unrelated cache gaps open.
- [ ] Confirm ADR-0028 remains truthful because this slice applies its existing event-history authority and rebuildable-cache choice; do not create or alter an ADR unless the implementation changes that boundary.
- [ ] Advance `Directory.Build.props` once from `0.1.0-dev.26` to `0.1.0-dev.27`; do not edit generated web identity output.
- [ ] Inspect and commit the intended successor-artifact and version diff before adding or changing tests and implementation.

### Task 3: Recover event-established presence through command replay

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.WantedSuspectPresence.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`; add a narrowly scoped classifier only if ordered event inspection shows it is needed.

**Consumes:** A normally started persisted session, a real `WantedSuspectConfronted` event with a state-changing outcome, production event decoding, and the PostgreSQL fixture.

**Produces:** A single TDD change that restores event-established ledger state from committed history when its cache is missing or null-root, without read writeback or hidden replay failures.

Ruling: Keep the failing behavior test and production fallback in one plan task because committing the intentional red state would leave the canonical gate failing; witness pre-fix failures first, then implement and commit only after focused PostgreSQL behavior is green.

- [ ] Add a PostgreSQL behavior test that creates a deterministic saloon suspect confrontation, persists its real event, and captures the target, choice, outcome, resulting presence state, ordered event rows, component version/payload, envelope positions and diary rows before damage.
- [ ] Cover missing-row and JSON-null-root cache cases. Assert replay restores the event-established presence state and that storage, event rows and envelope/diary metadata are unchanged by the load.
- [ ] Keep `AvailableInTown` direct seeding clearly outside this recovery claim. Existing replay semantics leave the ledger empty when no state-establishing event exists; do not add a test that repeats the optional-component default without proving replay ran.
- [ ] Add a fail-closed negative that damages the ledger cache and makes its authoritative confrontation event undecodable; command loading must surface the replay error.
- [ ] Run the focused PostgreSQL tests before production changes and record the observed wrong result or exception; prove the new assertions fail for the intended behavior, not a fixture/setup error.
- [ ] Route any missing `wantedSuspectPresenceLedger` row through full replay, matching the missing-`currentActionContext` approach: replay restores event-established transitions and naturally produces an empty ledger when no event establishes one. Do not add a presence-event classifier; it duplicates domain replay semantics and risks drifting from `Apply`.
- [ ] Wrap null-root and other specifically exercised invalid ledger payload shapes in the existing typed invalid-component-cache exception with component identity `wantedSuspectPresenceLedger`; do not catch event loading or replay failures as cache damage.
- [ ] Re-run the focused cases; assert a subsequent legal game command and ordinary unit-of-work save repair the component, then verify a fresh command load returns the same ledger state.
- [ ] Run the completion helper for task 3 only after the exact focused PostgreSQL command passes on the completed task commit.

### Task 4: Record the bounded result and deliver the slice

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `docs/features.md`; this plan; changed source and tests.

**Consumes:** Focused PostgreSQL proof, the baseline specification, ADR-0028, event-sourcing doctrine, decision-record, feature-matrix, unslop and code-review playbooks.

**Produces:** A dated narrow presence-cache recovery disposition, reviewed PR and evidence for the committed development version.

- [ ] Record only covered missing/null-root presence recovery, legitimate absence, query exclusion, read no-writeback, later legal-save repair and unreplayable-history failure; retain direct `AvailableInTown` seeding as a separate unresolved event-authority concern if still present.
- [ ] Inspect migration, event serializer/upcaster and event payload diffs; confirm no schema or event-contract change.
- [ ] Run the focused PostgreSQL command, then `py -3 tools/run.py ci --check`; verify generated `src/WildBunch.Web/dist/version.json` reports `0.1.0-dev.27` and ensure evidence belongs to the same committed candidate.
- [ ] Apply the repository's [implementing runbook](../runbooks/implementing.md), complete the focused behavior test with the local PostgreSQL service available via `./tools/postgres-dev.ps1 ensure`, and use the [code-review runbook](../runbooks/code-review.md) for the fresh whole-branch review.
- [ ] Complete fresh whole-branch review against this plan, baseline specification, PS-04/05, ADR-0028, event-sourcing doctrine, unslop profile and code-review runbook; resolve actionable findings and inspect the final committed head.
- [ ] Publish the reviewed implementation PR to `develop`, verify its exact source head/body and hosted canonical gate, mark it ready after review and validation pass, and merge it under the active goal authorization.
- [ ] Fast-forward the main checkout to merged `develop`; remove only this verified merged worktree and its branch-scoped scratch after checking exact containment and clean Git state.
