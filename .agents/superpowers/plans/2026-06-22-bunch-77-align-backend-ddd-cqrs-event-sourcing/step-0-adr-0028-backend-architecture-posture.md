# Step 0 — ADR-0028 Backend Architecture Posture (True Event Sourcing)

> Parent plan: `../2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing.md`
> Acceptance criteria covered: **AC-001** (architecture decision is durable).

## Goal

Add a new ADR that records Wild Bunch's backend architecture posture as a single durable decision: Onion Architecture as the structural style, DDD aggregate roots as the authority model, CQRS as the command/query split, **true Event Sourcing** as the source-of-history model (not event recording beside mutation), and projections/read models for player diary, HUD feed, case file view, and full audit.

This step is **doctrine only**. No production code behavior changes. It unblocks Steps 1–8 by giving every later step a single citation target and by preventing the half-pattern drift the rejected draft fell into.

## Files

- Add: `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`
- Modify: `docs/adr/README.md` (add ADR-0028 to the index)
- Modify: `.agents/architecture-hygiene.md` (add a one-line pointer to ADR-0028)
- No source code changes.

## ADR-0028 content outline

The ADR follows `docs/adr/TEMPLATE.md` and contains at minimum the sections below. The worker writes the full ADR text during this step; this outline is the contract.

### Status

`planned` on creation (Step 0), promoted to `live` once Steps 1–6 land and the event-sourced slice is proven by Step 8 tests. The plan records both status transitions in the Dated Status History.

### Decision Type

`architecture`, `persistence`, `process`

### Related ADRs

- `depends on`: ADR-0002 (GameSession is the command aggregate root), ADR-0003 (composed JSONB session persistence), ADR-0013 (travel journey is a session-owned aggregate subtree), ADR-0014 (DDD + Onion + CQRS + repositories + UoW), ADR-0020 (aggregate domain authority and root persistence posture)
- `informs`: ADR-0015 (minimal API boundary), ADR-0017 (testing posture), ADR-0019 (typed frontend API client), future ADR-0029+ (event store details, replay, SignalR transport)
- `related to`: BUNCH-3 (replayable session persistence), BUNCH-67 (refactor GameSession into domain aggregates), BUNCH-77 (this campaign)

### Context

- The next UI feature slice needs event-sourced projections, not mutable session log scraping.
- The current repo leads future developers to infer the wrong dominant architecture: mutable `GameSession` snapshot state plus embedded log/diary strings.
- A prior draft of this campaign drifted into event-recording beside snapshot mutation and called it Event Sourcing. This ADR explicitly rejects that half-pattern and records the true posture.
- ADR-0014 records DDD + Onion + CQRS + repositories + UoW. ADR-0020 records aggregate authority vs persistence posture. Neither records Event Sourcing or the projection taxonomy. ADR-0028 closes that gap.

### Decision Drivers

- Event Sourcing must be materially true for the migrated slice, not an event-recording bridge.
- Domain events are typed facts owned by Domain, not generic envelopes with string dispatch and `object` payload casting.
- State changes must flow through event application (`Apply`), not direct mutation beside event emission.
- Replay must reconstruct migrated state from events.
- Append must use optimistic concurrency (expected stream version).
- Snapshots are cache, not the conceptual source of history.
- Player-facing API output must remain safe: no raw events, no raw payloads, no full audit entries. Hidden culprit boundaries are preserved.
- No broker/store introduction (RabbitMQ, Kafka, EventStoreDB) by default.

### Decision Summary

Wild Bunch's backend architecture is Onion-structured with DDD aggregate roots, CQRS command/query handlers, true Event Sourcing for session history, and a projection/read-model taxonomy for player-facing outputs. Typed domain events are plain records owned by Domain; the persistence envelope is infrastructure. Command methods validate intent, produce typed domain facts, and apply them through `Apply` methods that mutate state. Replay reconstructs state from events. Append uses optimistic concurrency. Snapshots are cache. Projections derive from typed events via pattern matching. The player-facing API exposes safe projections only (diary + HUD); raw events, payloads, and audit are not player-facing.

### Detailed Decision Breakdown

The ADR must record:

1. **Structural style — Onion Architecture.** Domain and Application free of Persistence/EF details. Dependency direction inward only. (Reinforces ADR-0014.)

2. **Authority model — DDD aggregate roots.** `GameSession` is the command consistency boundary for live play. Sub-aggregates (`CaseFile`, `TownAggregate`, future pursuit/lawman) own their own legality per ADR-0020. BUNCH-67 may later split sub-aggregates; this ADR does not pre-empt that work.

3. **Command/query split — CQRS.** Command handlers orchestrate load → command → append → project through aggregate-scoped repositories and the first-class UoW (ADR-0014). Query handlers read projections, not aggregate internals.

4. **Source of history — Event Sourcing (true, not recording).**
   - Typed domain events are plain sealed records owned by `WildBunch.Domain`. They carry structured fields only — no envelope fields (EventId, Sequence, OccurredAtUtc, SchemaVersion, CorrelationId, CausationId). Those are infrastructure.
   - Command methods on `GameSession` validate intent, produce a typed domain fact, call `Apply(fact)` to mutate state, and record the fact in the uncommitted-events list.
   - `Apply` methods are the single mutation path for migrated state. Command methods do not directly mutate state for migrated flows.
   - Replay: `GameSession.RehydrateFromEvents(id, world, caseFile, events)` constructs a session and replays typed events through `Apply` in order. Replay reconstructs the same state as the command path.
   - The event stream is the durable source of history for migrated flows. Snapshots are cache.
   - This is materially true Event Sourcing for the migrated slice, not event recording beside mutation.

5. **Persistence envelope — infrastructure, not domain.**
   - The envelope (`StoredEvent` with EventId, StreamId, Sequence, OccurredAtUtc, EventType, PayloadJson, CorrelationId, CausationId, SchemaVersion) lives in `WildBunch.Persistence`, not Domain.
   - The envelope wraps typed domain events at the store boundary for storage, indexing, and concurrency.
   - On load, the store deserializes envelope payloads back to typed domain events and the aggregate replays them.
   - Domain never sees the envelope. Domain events are typed facts, period.

6. **Persistence port location — single repository port, no separate event-store interface.**
   - The event append and event-stream read are methods on `IGameSessionRepository` in `WildBunch.Application.Abstractions` (the existing repository port). There is no separate `IGameSessionEventStore` interface.
   - `EfGameSessionRepository` in `WildBunch.Persistence` implements both snapshot persistence and event append/read.
   - This keeps the dependency direction inward: Application handlers depend on `IGameSessionRepository` (in Application.Abstractions), not on any Persistence-layer type.
   - The handler has a single persistence path: `repository.StoreAsync(session, correlationId, ct)` stages snapshot upsert + event append + concurrency check on the same `DbContext`; `uow.CommitAsync(ct)` is the single `SaveChangesAsync` + transaction commit. No independent save in event-store code. No double append.

7. **Optimistic concurrency.**
   - The concurrency check is inside `StoreAsync` (the stage-time version check). The aggregate tracks its version (number of applied events).
   - If the persisted stream version does not match the expected version, `StoreAsync` throws `ConcurrencyException` and the command retries (reload + re-execute).
   - The unique DB index on `(StreamId, Sequence)` is the backstop if a race occurs between the stage-time check and the UoW commit.
   - This prevents lost updates and is a core event-sourcing invariant.

8. **Snapshots as cache.**
   - The composed JSONB snapshot (ADR-0003) is a fast-load cache, not the conceptual source of history.
   - Load path: load snapshot at version N → replay events N+1..latest through `Apply`.
   - Full replay-from-events (no snapshot) is proven in tests for the migrated slice.
   - The snapshot is re-stored on every command as a cache update.
   - As more flows migrate, events cover more state and the snapshot becomes pure cache. When all flows are migrated, replay-from-events becomes the production load path (BUNCH-3 follow-up).

9. **Projection taxonomy.**
   Four first-class projections with distinct audiences and narration rules, deriving from typed domain events via pattern matching:
   - **Player diary** — curated first-person past-tense narrative. The player's authored record.
   - **HUD feed** — second-person immediate present-tense notices. Transient presentation.
   - **Case file view** — neutral evidence-shaped read model. Safe subset only; no hidden culprit truth.
   - **Full audit** — exhaustive technical/session history. Developer/replay surface, **not player-facing**.

10. **API safety.**
    - Player-facing command responses expose **diary and HUD projections only**.
    - Raw domain events, raw payloads, full audit entries, and case-file internal truth are **not** exposed to player-facing API responses.
    - The existing `Message` field is preserved for backward compatibility.
    - Hidden culprit boundaries (ADR-0007) are preserved. The case file view projection exposes only public clues and warrants, never hidden truth.

11. **Transport posture.** SignalR/server push (when introduced) is a transport for projected events, not source truth. Polling remains the reconciliation path.

12. **`GameLogEntry` demotion.** `GameLogEntry` and `AddLogEntry` are `[Obsolete]` legacy projection-only output. New domain code does not add `AddLogEntry` call sites. A `LegacyLogProjector` derives `GameLogEntry`-shaped rows from typed events for future DTO switching.

13. **Migrated slice scope.** This campaign migrates two flows (start new game, purchase store item) as true event sourcing. Other flows remain on the existing direct-mutation path, clearly marked as not-yet-migrated, with follow-up issues to extend the pattern. A narrower true event-sourced implementation is preferable to a broad event-looking bridge.

### Options Considered and Rejected

- **Event recording beside snapshot mutation (the rejected draft).** Rejected: that is the half-pattern this campaign exists to prevent. Events emitted "in addition to" direct mutation is not Event Sourcing.
- **Generic `GameEvent` envelope with `string EventType` and `object Payload` in Domain.** Rejected: leaks storage vocabulary inward, brings stringly-typed dispatch and generic payload casting. Domain owns typed facts; the envelope is infrastructure.
- **Separate `IGameSessionEventStore` interface in Persistence.** Rejected: Application handlers would depend on a Persistence-layer type, violating Onion dependency direction. The event append and read are absorbed into `IGameSessionRepository` (in Application.Abstractions) instead.
- **Independent `SaveChangesAsync` in event-store code.** Rejected: breaks the existing UoW transaction pattern (`StoreAsync` stages; `CommitAsync` is the single save + transaction). Event append stages on the same `DbContext`; the UoW commits.
- **Handler calls a separate event-store append.** Rejected: creates double-append ambiguity and splits the persistence path. The handler calls `repository.StoreAsync` (stages snapshot + events) then `uow.CommitAsync` (commits). One path, one commit.
- **Big-bang rewrite to full event-sourced persistence.** Rejected: brief says vertical slices of provable value.
- **Keep mutable snapshot + log as the dominant architecture.** Rejected: misleads future developers and blocks the next UI slice.
- **Introduce a broker (RabbitMQ/Kafka/EventStoreDB).** Rejected: out of scope per brief.
- **Replace `GameSession` as the aggregate root.** Rejected: ADR-0002 and the brief keep it as the root. BUNCH-67 handles sub-aggregate splits.
- **Expose raw events/audit to player-facing API.** Rejected: violates hidden culprit boundaries (ADR-0007) and the safety requirement.
- **Defer replay-from-events.** Rejected: if replay is not proven for the migrated slice, Event Sourcing is not materially true.

### When a Rejected Option Would Have Been Better

- The rejected draft's event-recording approach would only be better if the goal were audit logging, not Event Sourcing. The goal is Event Sourcing.
- A big-bang rewrite would only be better if the repo were greenfield, which it is not.
- Exposing raw events would only be better if the API audience were developers only, which it is not.

### Benefits

- One durable citation target for the true backend architecture posture.
- Future workers cannot infer the wrong dominant architecture from the codebase.
- The next UI slice has a safe, documented contract for projection consumption.
- BUNCH-3 (replay) and BUNCH-67 (sub-aggregates) have a clear dependency target.
- The half-pattern drift is explicitly rejected, not just undocumented.

### Accepted Tradeoffs

- Only two flows are migrated in this campaign. Non-migrated flows remain on direct mutation. The ADR records this transitional coexistence honestly.
- The snapshot is re-stored on every command (transitional cache). When all flows migrate, the snapshot becomes pure cache and replay-from-events becomes the production load path.
- `GameLogEntry` is demoted but not yet removed.

### Risks

- The ADR could be cited as if it were already fully implemented before Steps 1–6 land. Mitigation: status starts `planned`; Implementation Status names the implementing steps.
- Future workers could re-centralize narration in `GameSession` despite the projection taxonomy. Mitigation: ADR-0020 and this ADR both record the anti-pattern.
- The transitional coexistence (migrated + non-migrated) could confuse future developers. Mitigation: non-migrated flows are clearly marked; the ADR records the coexistence and the migration path.

### Consequences for Future Work

- New command-side work for migrated flows assumes command-produces-event-then-applies, not direct mutation.
- New read-side work assumes projections, not aggregate internals.
- Follow-up issues migrate non-migrated flows using the same pattern: typed event → `Apply` → replay → projection.
- BUNCH-3 replay work assumes the typed event vocabulary and `Apply`/replay mechanism from this campaign.
- BUNCH-67 sub-aggregate splits assume typed events can travel between aggregates via the public API pattern in ADR-0020.
- Future SignalR work is a transport layer over safe projections, not a new source truth.

### Implementation Status or Plan

`planned` on Step 0 landing. Promoted to `live` after Steps 1–6 land and the event-sourced slice is proven by Step 8 tests. The Implementation Status section names each implementing step and its PR.

### Related Stable Source Surfaces

- `docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md`
- `docs/adr/ADR-0003-composed-jsonb-session-persistence.md`
- `docs/adr/ADR-0007-hidden-culprit-truth-and-hidden-progress-boundaries.md`
- `docs/adr/ADR-0014-use-ddd-onion-cqrs-repositories-and-first-class-unit-of-work.md`
- `docs/adr/ADR-0020-aggregate-domain-authority-and-root-persistence-posture.md`
- `.agents/architecture-hygiene.md`
- `src/WildBunch.Domain/Game/GameSession.cs` (referenced as the aggregate root; modified in Step 2)
- Future: `src/WildBunch.Domain/Events/` (Step 1), `src/WildBunch.Application/Projections/` (Step 4), `src/WildBunch.Persistence/EventStore/` (Step 3)

### Proof of Implementation or Explicit Non-Implementation

This step is doctrine-only. Proof is the ADR file itself plus the README index entry. Code proof arrives in Steps 1–8 and is cited in the ADR's Implementation Status section as those steps land.

### Review Triggers

- When BUNCH-3 lands full replay-from-events and the snapshot becomes pure cache.
- When BUNCH-67 splits sub-aggregates and events cross aggregate boundaries via public APIs.
- When all flows are migrated and the transitional coexistence ends.
- When SignalR transport is introduced.
- When `GameLogEntry` is fully removed.

## Tasks

- [ ] **Task 1: Write ADR-0028** following `docs/adr/TEMPLATE.md` and the content outline above. Verify the next free ADR number by listing `docs/adr/`; the master plan assumes 0028 but the worker confirms.
- [ ] **Task 2: Add ADR-0028 to `docs/adr/README.md` index** with a one-line summary matching the existing index style.
- [ ] **Task 3: Add a one-line pointer to `.agents/architecture-hygiene.md`** matching the existing file's style.
- [ ] **Task 4: Verify ADR numbering and index consistency.** `ls docs/adr/ADR-*.md` count matches README index entries.

## Validation

- [ ] **V1: ADR file exists and follows the template.** Read the new ADR back; confirm every template section is present.
- [ ] **V2: ADR index is consistent.** `ls docs/adr/ADR-*.md` count matches README index entries.
- [ ] **V3: No source code changes.** `git status` shows only `docs/adr/ADR-0028-*.md`, `docs/adr/README.md`, and `.agents/architecture-hygiene.md`.

## Acceptance mapping

- **AC-001 (Architecture decision is durable):** satisfied by the ADR file + README index + architecture-hygiene pointer.

## Non-goals for this step

- No source code changes.
- No event types (Step 1).
- No `Apply` methods (Step 2).
- No persistence (Step 3).
- No projections (Step 4).
- No ADR status promotion to `live` until Steps 1–6 land.

## Self-Review

**Spec coverage:** Step 0 covers AC-001. The ADR outline includes every required posture element and explicitly rejects the half-pattern from the rejected draft.

**Placeholder scan:** The ADR number is assumed 0028 (latest is ADR-0027). Task 1 verifies.

**Type consistency:** The ADR follows the existing template and citation style.

**Non-goals:** All six non-goals preserved.
