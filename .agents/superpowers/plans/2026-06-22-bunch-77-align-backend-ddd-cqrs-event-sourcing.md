# BUNCH-77 Align Backend Architecture with DDD, CQRS, Event Sourcing Foundations — Revised Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Each step lives in its own document under `./2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing/` and uses checkbox (`- [ ]`) syntax for tracking.

**Linear issue:** [BUNCH-77](https://linear.app/harleys-workspace/issue/BUNCH-77/align-backend-architecture-with-ddd-cqrs-event-sourcing-foundations)
**Parent:** [BUNCH-3](https://linear.app/harleys-workspace/issue/BUNCH-3/define-replayable-session-persistence-with-events-snapshots-rng) — replayable session persistence with events, snapshots, RNG receipts, and command transactions.
**Related:** [BUNCH-67](https://linear.app/harleys-workspace/issue/BUNCH-67/refactor-gamesession-responsibilities-into-domain-aggregates) — refactor `GameSession` responsibilities into domain aggregates.

## What this plan corrects versus the rejected draft

The rejected draft drifted into an event-recording seam beside snapshot mutation and called it Event Sourcing. It put a generic `GameEvent` envelope with `string EventType` and `object Payload` in Domain (storage vocabulary leaking inward), emitted events "in addition to" existing state changes (event recording after mutation), deferred replay-from-events (so Event Sourcing was never materially true), and exposed raw events/audit to the player-facing API (violating hidden culprit boundaries).

This revised plan makes Event Sourcing **materially true for the migrated slice**:

1. **Typed domain events in Domain, no envelope.** `GameStarted` and `StoreItemPurchased` are plain sealed records with structured fields. No `EventId`, `Sequence`, `OccurredAtUtc`, `SchemaVersion`, `CorrelationId`, or `CausationId` on the domain event. The envelope is a persistence/infrastructure concern introduced at the store boundary, not in Domain.

2. **Command methods validate intent and produce domain facts. Apply methods mutate state.** `GameSession.StartNew` validates parameters, constructs a `GameStarted` fact, calls `Apply(GameStarted)`, and records it. `GameSession.Purchase` validates affordability/inventory, constructs a `StoreItemPurchased` fact, calls `Apply(StoreItemPurchased)`, and records it. State changes come from `Apply`, not from direct mutation in the command method.

3. **Replay reconstructs migrated state.** `GameSession.RehydrateFromEvents(id, world, caseFile, events)` constructs a session and replays typed events through `Apply` in order. Tests prove that replay produces the same state as the command path. This is not deferred.

4. **Append uses expected stream version (optimistic concurrency).** The event store checks expected version on append. If another command committed in between, the version mismatch causes a `ConcurrencyException` and the command retries. This prevents lost updates and is a core event-sourcing invariant.

5. **Snapshots are cache, not source of history.** The event stream is the source of history for the migrated slice. The load path is: load snapshot at version N → replay events N+1..latest through `Apply`. Full replay-from-events (no snapshot) is proven in tests. The snapshot is re-stored on every command as a fast-load cache, but it is not the conceptual source of truth.

6. **Projections derive from typed events via pattern matching.** Projectors switch on the typed event (`switch (e) { case GameStarted gs: ...; case StoreItemPurchased p: ...; }`), not on string event-type constants. No generic payload casting.

7. **Player-facing API output remains safe.** The API bridge exposes diary and HUD projections only. No raw domain events, no raw payloads, no full audit entries, no case-file internal truth. Audit is a developer/replay surface, not a player-facing response. Hidden culprit boundaries are preserved.

8. **Narrower migrated slice, true event sourcing.** The migrated slice is **start new game** and **purchase store item** — two flows done as real event sourcing. Other flows (wanted poster read, clue discovery, wrong saloon declaration, travel) remain on the existing direct-mutation path, clearly marked as not-yet-migrated, with follow-up issues to migrate them using the same pattern. A narrower true event-sourced implementation is preferable to a broad event-looking bridge.

## Goal

Make Wild Bunch's backend architecture visibly align with Onion Architecture, DDD aggregate roots, CQRS, and Event Sourcing — not as a skeleton beside the current architecture, but as a materially true event-sourced slice that future work extends. The migrated slice (start game + purchase) proves the full pattern: typed domain facts, command-produces-event-then-applies, replay reconstructs state, optimistic concurrency on append, snapshot as cache, projections from events, safe player-facing API.

## Architecture posture (target)

- **Onion Architecture** — Domain and Application free of Persistence/EF details. Dependency direction inward only.
- **DDD aggregate roots** — `GameSession` owns rules and invariants. Command methods validate intent and produce typed domain facts. `Apply` methods consume facts and mutate state. The aggregate is the consistency boundary.
- **CQRS** — command handlers orchestrate load → command → append → project. Query handlers read projections, not aggregate internals.
- **Event Sourcing** — typed domain events are immutable facts and the source of history. The event stream is the durable record. Snapshots are cache. Replay reconstructs state. Append uses optimistic concurrency.
- **Persistence port location** — the event append and event-stream read are methods on `IGameSessionRepository` in `WildBunch.Application.Abstractions` (the existing repository port). There is no separate `IGameSessionEventStore` interface. `EfGameSessionRepository` in `WildBunch.Persistence` implements both. This keeps the dependency direction inward (Application does not depend on Persistence) and gives the handler a single persistence path: `repository.StoreAsync(session, correlationId, ct)` stages snapshot + events + concurrency check on the `DbContext`; `uow.CommitAsync(ct)` is the single `SaveChangesAsync` + transaction commit. No independent save in event-store code. No double append.
- **Projections / read models** — player diary, HUD feed, case file view, and full audit are separate projections derived from typed events via pattern matching. Each has its own audience, tense, and content rules.
- **Safe API bridge** — player-facing responses expose diary and HUD projections only. Raw events, payloads, and audit are not player-facing. Hidden culprit boundaries are preserved.

## Tech Stack

C# / .NET 10, EF Core on PostgreSQL, xUnit, existing Wild Bunch domain/application tests, repo-local `postgres-dev.ps1` validation lane.

## Migrated representative slice

Two flows, fully event-sourced:

1. **Start new game** — `GameSession.StartNew` validates parameters, produces `GameStarted`, applies it. Replay reconstructs the initial session state.
2. **Purchase store item** — `GameSession.Purchase` validates affordability/inventory/capacity, produces `StoreItemPurchased`, applies it. Replay reconstructs wallet and inventory changes.

Non-migrated flows (existing direct-mutation path, clearly marked as not-yet-migrated):
- Wanted poster read, clue discovery/investigation, wrong saloon wanted declaration, travel start/day/arrival, saloon person-of-interest, sheriff turn-in, bounty loop.

Follow-up issues migrate these using the same pattern established here.

## Workflow route selected by `/using-superpowers`

`writing-plans` → `executing-plans` (outer workflow), with `architecture-decision-records` for Step 0, `cqrs-event-sourcing` + `ddd` + `clean-architecture` + `event-driven-architecture` for seams, and `wild-bunch-dotnet-architecture` + `wild-bunch-domain-modeling` guardrails applied throughout. `unslop-superpowers` controls evidence and closeout language.

## Step sequence

Each step is a standalone document. Steps are ordered so the domain event model and Apply/replay mechanism land before persistence, projections, handlers, and API.

| Step | Document | Summary | Primary AC |
|------|----------|---------|------------|
| 0 | `step-0-adr-0028-backend-architecture-posture.md` | ADR-0028 recording true event-sourcing posture: typed domain facts, command-produces-event-then-applies, replay, optimistic concurrency, snapshot as cache, safe projections. | AC-001 |
| 1 | `step-1-typed-domain-events.md` | `GameStarted` and `StoreItemPurchased` as plain sealed records in `WildBunch.Domain/Events/`. No envelope fields. No string event-type constants. | AC-002 |
| 2 | `step-2-event-sourced-gamesession.md` | `Apply(GameStarted)` and `Apply(StoreItemPurchased)` methods. `StartNew` and `Purchase` refactored to validate → produce event → apply → record. Uncommitted events list + version. `RehydrateFromEvents` replay constructor. Non-migrated flows unchanged. | AC-002, AC-003, AC-006 |
| 3 | `step-3-persistence-event-store.md` | `StoredEvent` envelope entity in Persistence (not Domain). Append with expected version (optimistic concurrency). Load: snapshot at version N → replay events N+1..latest. Full replay-from-events proven in tests. Snapshot is cache. | AC-002, BUNCH-3 |
| 4 | `step-4-projection-contracts.md` | Four projection interfaces + reference projectors deriving from typed events via pattern matching. Diary (first-person past), HUD (second-person present), case file view (neutral, safe), audit (exhaustive, developer-only). | AC-004 |
| 5 | `step-5-handler-orchestration.md` | Command handlers: load (snapshot + replay) → command (validate + produce + apply) → append with expected version → project → return safe projections. UoW commits atomically. | AC-003, AC-006, AC-007 |
| 6 | `step-6-api-bridge-safe-projections.md` | API responses expose diary + HUD projections only. No raw events, no payloads, no audit. Existing `Message` preserved. Hidden culprit boundaries preserved. | AC-007 |
| 7 | `step-7-demark-gamelogentry-as-projection-legacy.md` | `[Obsolete]` on `GameLogEntry`/`AddLogEntry`. `#pragma` blocks existing call sites. `LegacyLogProjector` provides projection-derived replacement. | AC-005 |
| 8 | `step-8-tests-and-validation.md` | End-to-end proofs: replay reconstructs state, optimistic concurrency works, projections derive from typed events, API is safe. Full validation lanes. ADR-0028 promoted to `live`. | AC-006, all |

## Explicit non-goals (preserved from brief)

- Do not build the next HUD event bar UI slice here.
- Do not implement a full SignalR system unless the event/projection seam is already complete and it remains small.
- Do not introduce RabbitMQ, Kafka, EventStoreDB, or another broker/store by default.
- Do not normalize live game session runtime state into many tables. Snapshot persistence stays composed JSONB as cache.
- Do not use comments as live planning truth; issue body and attached documents are the live planning surfaces.
- Do not move gameplay mutation out of `GameSession` to satisfy SOLID; `Apply` methods live inside `GameSession`.
- Do not close Linear issues or add `!`-prefixed labels. BUNCH-77 is a campaign candidate, not delegated.
- Do not expose raw domain events, raw payloads, or full audit entries to player-facing API responses.
- Do not migrate non-migrated flows in this campaign. Establish the pattern with two flows; follow-up issues extend it.

## Scope risks and decisions

1. **Narrower slice is intentional.** Harley's feedback: "A narrower true event-sourced implementation is preferable to a broad event-looking bridge." Two flows done as real Event Sourcing is the campaign deliverable. The ADR records the pattern so follow-up issues extend it without re-deriving the architecture.

2. **Transitional coexistence.** Migrated flows (start game, purchase) use command-produces-event-then-applies. Non-migrated flows use existing direct mutation. The snapshot captures all state. The event stream captures migrated events only. Load: snapshot + replay post-snapshot migrated events. This is honest: the snapshot is cache for all state; events are source of history for migrated state. As more flows migrate, events cover more state and the snapshot becomes pure cache.

3. **External references at load time.** `World` and `CaseFile` (as a fresh template) are provided to `RehydrateFromEvents` as external references, not stored in events. Events capture decisions (player name, starting town, purchase details), not content references. As flows that mutate `CaseFile` migrate (follow-up), `CaseFile` mutations become events.

4. **`GameLogEntry` demotion is corrective but non-breaking.** The legacy log pathway is preserved with `[Obsolete]` markers. New domain code does not add `AddLogEntry` call sites. The `LegacyLogProjector` provides a projection-derived replacement for future DTO switching.

5. **BUNCH-67 interaction.** This campaign keeps `GameSession` as the aggregate root and establishes event-sourced command methods inside it. BUNCH-67 may later split sub-aggregates that consume/emit typed domain events via the public API pattern in ADR-0020. The typed event vocabulary and `Apply`/replay mechanism are designed to support that future split.

6. **BUNCH-3 alignment.** The event store with optimistic concurrency and snapshot-as-cache is the BUNCH-3 foundation. Replay-from-events is proven for the migrated slice in tests. Full production replay-from-events (all flows) is a BUNCH-3 follow-up after all flows migrate.

## PR boundaries (expected)

- **PR 1:** Step 0 (ADR only) — doctrine, no code behavior change.
- **PR 2:** Steps 1 + 2 — typed domain events + event-sourced `GameSession` for migrated slice, no persistence/API change.
- **PR 3:** Step 3 — persistence event store with optimistic concurrency + snapshot cache.
- **PR 4:** Steps 4 + 7 — projection contracts + `GameLogEntry` demotion.
- **PR 5:** Steps 5 + 6 — handler orchestration + safe API bridge.
- **PR 6:** Step 8 — test consolidation and final validation evidence.

Final grouping is subject to Harley's approval.

## Likely follow-up issue split

- **Follow-up 1:** Migrate wanted poster read to event-sourced pattern (`WantedPosterRead` event, `Apply` method, replay coverage).
- **Follow-up 2:** Migrate clue discovery/investigation to event-sourced pattern (`ClueDiscovered` event).
- **Follow-up 3:** Migrate wrong saloon wanted declaration (`WantedDeclarationRejected` / `WantedDeclarationAccepted` events).
- **Follow-up 4:** Migrate travel start/day/arrival (`JourneyStarted`, `TravelDayAdvanced`, `JourneyArrived` events).
- **Follow-up 5:** Full production replay-from-events load path (BUNCH-3 child, after all flows migrate).
- **Follow-up 6:** Remove legacy `GameLogEntry` pathway and switch `LogEntries` DTO to projection-derived output.
- **Follow-up 7:** Projection persistence store (replace in-memory projectors with queryable projection table/view).
- **Follow-up 8:** SignalR transport for projected events (only after UI slice consumes projections).
- **BUNCH-67 interaction:** Sub-aggregate splits consume the typed event vocabulary and `Apply`/replay mechanism established here.

## Validation commands (per AGENTS.md)

- `dotnet build`
- `dotnet test`
- `dotnet tool restore` before EF validation commands.
- `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` when persistence is touched (Step 3).
- `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent tests.
- `.\scripts\postgres-dev.ps1 validate` for the repo-local PostgreSQL-backed validation lane when persistence or integration tests are touched.

## Return evidence required after approved implementation

Per the brief:

- Branch, head commit, PR URL.
- Changed file list grouped by Domain, Application, Infrastructure, API, Web, tests, and docs.
- Summary of which acceptance criteria are fully implemented, partially implemented, or intentionally deferred with follow-up issue recommendations.
- Validation commands and results.
- Explicit note confirming no Linear delegation or `!`-prefixed labels were used by this issue.

## Self-Review

**Spec coverage:** The plan covers AC-001 through AC-007. AC-006 is partially satisfied (two flows fully event-sourced; other flows deferred with follow-up issues). The brief's "at least a bounded representative set" is met by the two flows; the breadth adjustment is explicitly approved by Harley's feedback.

**Event Sourcing materiality:** For the migrated slice, state changes are driven through `Apply`, replay reconstructs state, append uses optimistic concurrency, snapshot is cache, projections derive from typed events. This is materially true Event Sourcing, not event recording.

**Domain purity:** Typed domain events are plain records with no envelope fields. The envelope lives in Persistence. No storage vocabulary leaks into Domain.

**API safety:** Player-facing responses expose diary + HUD only. No raw events, no payloads, no audit. Hidden culprit boundaries preserved.

**Non-goals:** All explicit non-goals from the brief plus the additional safety and scope non-goals are preserved.

**Approval gate:** This revised plan is presented for Harley approval before any code changes. No implementation work has started.
