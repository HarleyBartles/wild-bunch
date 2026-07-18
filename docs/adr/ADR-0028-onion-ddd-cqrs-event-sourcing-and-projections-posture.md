# ADR-0028 Onion, DDD, CQRS, Event Sourcing, and Projections Posture

## Status

`live`

## Dated Status History

- 2026-06-22 - planned: ADR-0028 records the true event-sourcing posture for the migrated representative slice (start new game, purchase store item). Promotion to `live` follows after the migrated slice is proven by tests and the implementation steps land.
- 2026-06-22 - live: The migrated slice (start new game, purchase store item) is proven by tests. Typed domain events, event-sourced GameSession, persistence event store with optimistic concurrency, projection contracts and reference projectors, handler orchestration, and API bridge safe projections are all implemented and tested. GameLogEntry is demoted to projection-legacy.
- 2026-06-23 - live (BUNCH-80): Bounty/saloon flows migrated to event sourcing. 5 new event types (TownActionContextEntered, SaloonPersonOfInterestSpotted, WantedSuspectConfronted, SheriffTurnInSettled, SaloonPersonOfInterestConfronted). Clock/turn correction: RecordCaseUpdate decoupled from clock, TimeOfDay enum added, TownActionContext-based turn advancement via EnterActionContext. 5 handlers migrated to GameSessionCommandHandler. DiaryProjector, HudProjector, CaseFileViewProjector updated. 5 event types registered in persistence deserializer. CurrentActionContext persisted in snapshot. TimeOfDay added to GameClockDto and frontend display.
- 2026-06-23 - live (BUNCH-83): Travel/journey flows migrated to event sourcing. 6 new event types (JourneyStarted, TravelDayAdvanced, TrailEventApplied, JourneyEncounterResolved, JourneyCompleted, JourneyArrivalAcknowledged). TravelDayAdvanced carries ABSOLUTE PursuitHeat and uses journey snapshot for player food/canteen/horse feed/horse state sync (SyncPlayerFromJourneySnapshot). TravelDayAdvanced and JourneyEncounterResolved carry AdditionalDiaryMessages for narration-only encounters (no TrailEvent), so all travel/journey diary/log accumulation flows through typed events and RecordTravelUpdate during Apply — command-path and replay-path behavior are kept in sync with no direct AddLogEntry remaining in the migrated travel flow. TrailEventApplied carries ABSOLUTE WalletCash and PursuitHeat. JourneyCompleted carries empty DiaryMessage (arrival message already in TravelDayAdvanced). JourneyArrivalAcknowledged carries empty DiaryMessage (arrival message is in the return result only). 4 handlers migrated to GameSessionCommandHandler with ExecuteWithRetryAsync (TravelToTownHandler preview generation inside retry boundary). DiaryProjector and HudProjector updated with travel event cases. 6 event types registered in persistence deserializer. AddLogEntryGuardrailTests reduced from 19 to 6 remaining direct legacy call sites. Characterization tests guard exact state values for travel state machine, resource tracking, diary accumulation, and encounter resolution. Replay equality tests prove command-path state == replay-path state for LogEntries counts across journey start, day advance, full cycle, and encounter resolution.
- 2026-06-24 - live (BUNCH-84): Journal-facing read paths switched from legacy GameSessionLogEntries table reads to event-stream projection. New JournalLogProjector (Application.Projections) reproduces the exact legacy GameLogEntry sequence (kind/message/day/turn) from the typed domain event stream, including a look-ahead for TrailEventApplied events that precede TravelDayAdvanced (the command path advances the clock directly before logging the trail event narration, so the narration uses the new day). GameSessionReadStoreLoader.LoadStoreAsync now loads StoredEvents, deserializes via GameSessionJsonSerializer.DeserializeEvent, and projects LogEntries via JournalLogProjector; both LoadJournalSnapshotAsync (/journal endpoint) and LoadGameSessionReadModelAsync (session read model) share this loader and are now projection-backed. EfGameSessionRepository.LoadStoreAsync (command-load path) intentionally retains the GameSessionLogEntries table read as bounded compatibility surface; the table write (SyncLogEntriesAsync), AddLogEntry/RecordCaseUpdate/RecordTravelUpdate in Apply, the snapshot LogEntries field, and JournalResolver remain as compatibility surface deferred to the write-path-removal follow-up. Per-event projector tests (Application.Tests), full-cycle and encounter-resolution equivalence tests (Domain.Tests, test-only project reference to Application), and ReadStoreLoaderJournalProjectionGuardrailTests (source-inspection: read-store loader no longer queries dbContext.GameSessionLogEntries; command-load repository still does) guard the switch. AddLogEntryGuardrailTests count unchanged (6).
- 2026-06-24 - live (BUNCH-86): Purchase journal projection regression fixed, legacy log table fully removed, and all live player-facing aggregate-log read paths switched to projection-backed output. Apply(StoreItemPurchased) now records the Purchase log entry (moved from Purchase() command method), and JournalLogProjector projects StoreItemPurchased to a Purchase entry — fixing the regression where the /journal endpoint omitted purchase entries. EfGameSessionRepository.LoadStoreAsync (command-load path) switched from GameSessionLogEntries table reads to event-stream projection via JournalLogProjector, matching the read-store loader. GameSessionLogEntries table, GameSessionLogEntryEntity, GameSessionLogEntryEntityConfiguration, DbSet, SyncLogEntriesAsync, and the navigation property on GameSessionEntity are all removed; EF migration DropGameSessionLogEntries drops the table. Dead CompleteCase stub removed. AddLogEntryGuardrailTests count reduced from 6 to 5. ReadStoreLoaderJournalProjectionGuardrailTests updated: both read-store loader and command-load repository now must NOT query GameSessionLogEntries. GameSession gains AllEvents (committed + uncommitted) and SetCommittedEvents (called by repository on load); MarkEventsCommitted transfers uncommitted to committed (keeping AllEvents correct post-commit). GameSessionMapper.ToDto(DomainGameSession) projects log entries from session.AllEvents via JournalLogProjector instead of scraping session.LogEntries. JournalResolver.Resolve accepts projected log entries as a parameter instead of reading session.LogEntries; 6 investigation handlers project via GameSessionLogProjection.Project(session) and pass the result. No live player-facing read path scrapes aggregate session.LogEntries. LoadStoreAsync projects only the snapshot-prefix events for aggregate LogEntries rehydration; post-snapshot events are replayed via ApplyCommittedEvents and Apply(...) methods append their own log entries — this prevents duplicated log entries when SnapshotVersion < StreamVersion (proven by GetByIdAsync_WithLaggingSnapshot_DoesNotDuplicateAggregateLogEntries). Remaining compatibility surface: AddLogEntry/RecordCaseUpdate/RecordTravelUpdate in Apply (5 call sites — internal bookkeeping that populates _logEntries but is not read by any live path), snapshot LogEntries field (test-only serializer with no production callers). These remain as bounded internal surface for a future cleanup follow-up.
- 2026-06-27 - live (BUNCH-102): Playthrough archive lifecycle added as a new event-sourced flow, not a flow migration. New typed domain event `PlaythroughArchived` (carries ArchiveReason, last-position snapshot, and StatusBeforeArchive) flows through `GameSession.ArchivePlaythrough` → `ProduceEvent` → `Apply(PlaythroughArchived)` (sets `Status = GameStatus.Archived`) and replays via `GameSessionEventReplay.ApplyEvent`. New `GameStatus.Archived` enum value (3) stored as string in the existing Status column — no new tables or migrations. The one-active-playthrough invariant is enforced in `CompletePlayerSetupHandler`: all pre-existing `Active` sessions are archived (reason `superseded-by-new-playthrough`) and the new session is created in one correlation id and one UoW commit. `PlaythroughArchived` registered in the persistence event deserializer. See ADR-0034 for the full archive lifecycle and invariant decision.
- 2026-07-01 - live (BUNCH-111): AddLogEntry/RecordCaseUpdate/RecordTravelUpdate migration complete. All remaining Apply-method log calls removed. `_logEntries` field, `LogEntries` property, `AddLogEntry`, `RecordCaseUpdate`, `RecordTravelUpdate` methods removed from GameSession. Snapshot `LogEntries` field removed from JSON serialization. `GameSessionRehydrator.ReplaceLogEntries` removed. Repository load path no longer rehydrates `_logEntries`. Read store loader projects log entries on demand from `AllEvents` via `JournalLogProjector`. `AddLogEntryGuardrailTests` removed (mission complete). All log/journal reads now flow exclusively through `JournalLogProjector` / `GameSessionLogProjection`. `GameLogEntry` record retained as projection output type.

## Decision Type

architecture, persistence, process

## Related ADRs

- `depends on`: ADR-0002, ADR-0003, ADR-0013, ADR-0014, ADR-0020
- `informs`: ADR-0015, ADR-0017, ADR-0019
- `related to`: ADR-0034 (playthrough archive lifecycle — `PlaythroughArchived` is a typed domain event replayed through Apply per this posture)
- `related to`: BUNCH-3 (replayable session persistence), BUNCH-67 (refactor GameSession into domain aggregates — closed as historical/superseded; see Historical Notes), BUNCH-77 (this campaign)

## Context

The next UI feature slice needs event-sourced projections, not mutable session log scraping. The current repo leads future developers to infer the wrong dominant architecture: mutable `GameSession` snapshot state plus embedded log/diary strings.

A prior draft of BUNCH-77 drifted into event-recording beside snapshot mutation and called it Event Sourcing. That draft put a generic `GameEvent` envelope with `string EventType` and `object Payload` in Domain (storage vocabulary leaking inward), emitted events "in addition to" existing state changes (event recording after mutation), deferred replay-from-events (so Event Sourcing was never materially true), and exposed raw events and full audit to the player-facing API (violating hidden culprit boundaries).

ADR-0014 records DDD + Onion + CQRS + repositories + UoW. ADR-0020 records aggregate authority vs persistence posture. Neither records Event Sourcing or the projection taxonomy. ADR-0028 closes that gap and explicitly rejects the half-pattern drift.

## Decision Drivers

- Event Sourcing must be materially true for the migrated slice, not an event-recording bridge.
- Domain events are typed facts owned by Domain, not generic envelopes with string dispatch and `object` payload casting.
- State changes must flow through event application (`Apply`), not direct mutation beside event emission.
- Replay must reconstruct migrated state from events.
- Append must use optimistic concurrency (expected stream version).
- Snapshots are cache, not the conceptual source of history.
- Player-facing API output must remain safe: no raw events, no raw payloads, no full audit entries. Hidden culprit boundaries are preserved.
- No broker/store introduction (RabbitMQ, Kafka, EventStoreDB) by default.
- Onion dependency direction is enforced: Application handlers depend on ports in Application.Abstractions, not on Persistence-layer types.

## Decision Summary

Wild Bunch's backend architecture is Onion-structured with DDD aggregate roots, CQRS command/query handlers, true Event Sourcing for session history, and a projection/read-model taxonomy for player-facing outputs. Typed domain events are plain records owned by Domain; the persistence envelope is infrastructure. Command methods validate intent, produce typed domain facts, and apply them through `Apply` methods that mutate state. Replay reconstructs state from events. Append uses optimistic concurrency. Snapshots are cache. Projections derive from typed events via pattern matching. The BUNCH-77 API bridge exposes safe projections (diary + HUD) via query endpoints; raw events, payloads, and audit are not player-facing. Command response DTOs are preserved legacy shapes during migration and do not yet include projection output inline (see §10).

## Detailed Decision Breakdown

1. **Structural style — Onion Architecture.** Domain and Application free of Persistence/EF details. Dependency direction inward only. (Reinforces ADR-0014.)

2. **Authority model — DDD aggregate roots.** `GameSession` is the command consistency boundary for live play. Sub-aggregates (`CaseFile`, `TownAggregate`, future pursuit/lawman) own their own legality per ADR-0020. BUNCH-67 may later split sub-aggregates; this ADR does not pre-empt that work.

3. **Command/query split — CQRS.** Command handlers orchestrate load → command → store → commit → project through aggregate-scoped repositories and the first-class UoW (ADR-0014). Query handlers read projections, not aggregate internals.

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
    - The BUNCH-77 API bridge is projection **query endpoints** (`GET /api/games/{id}/projections/hud` and `GET /api/games/{id}/projections/diary`) serving safe projections derived from the event stream, plus **preserved legacy command DTOs** (`GameSessionDto` / `GameTurnResultDto`) during migration. The legacy command DTOs still expose `LogEntries` (projection-legacy, `[Obsolete]`) and do not yet include HUD/diary projection output inline.
    - Raw domain events, raw payloads, full audit entries, and case-file internal truth are **not** exposed to player-facing API responses. The full audit projection is a developer/replay surface and is **not** exposed on the normal game API.
    - The existing `Message` field is preserved for backward compatibility.
    - A follow-up issue should migrate the command response DTOs to include safe diary/HUD projection output and drop `LogEntries` from player-facing command responses. Until that follow-up lands, the durable ADR records this transitional state honestly: command responses are legacy DTOs, not projection-only. BUNCH-78 addressed the first half of this follow-up: migrated command responses (start-new-game, purchase-store-item) now include safe HUD/diary projection output inline via optional `HudProjection` and `DiaryProjection` fields on `GameSessionDto`. Legacy `LogEntries` remains for backward compatibility. Dropping `LogEntries` from command responses entirely is a future slice pending UI migration.
    - Hidden culprit boundaries (ADR-0007) are preserved. The case file view projection exposes only public clues and warrants, never hidden truth.

11. **Transport posture.** SignalR/server push (when introduced) is a transport for projected events, not source truth. Polling remains the reconciliation path.

12. **`GameLogEntry` demotion.** `GameLogEntry` and `AddLogEntry` are `[Obsolete]` legacy projection-only output. New domain code does not add `AddLogEntry` call sites. A `LegacyLogProjector` derives `GameLogEntry`-shaped rows from typed events for future DTO switching. Note: `LegacyLogProjector` is referenced in this ADR but not yet implemented in source as of BUNCH-78. It remains a future implementation item.

13. **Migrated slice scope.** This campaign migrates two flows (start new game, purchase store item) as true event sourcing. BUNCH-80 extends the migrated slice to bounty/saloon flows (LookAroundSaloon, ConfrontSaloonPersonOfInterest, ConfrontSaloonWantedSuspect, ResolveWantedSuspectConfrontation, SettleSheriffTurnIn, plus 5 investigation methods that now produce TownActionContextEntered events). 5 new event types: `TownActionContextEntered`, `SaloonPersonOfInterestSpotted`, `WantedSuspectConfronted`, `SheriffTurnInSettled`, `SaloonPersonOfInterestConfronted`. Clock/turn correction: `RecordCaseUpdate` is decoupled from the clock; `TimeOfDay` enum added as a naming layer over `Turn` (0-3); `EnterActionContext` produces replayable `TownActionContextEntered` events that advance the turn. Other flows (travel/journey, case completion) remain on the existing direct-mutation path, clearly marked as not-yet-migrated, with follow-up issues to extend the pattern. A narrower true event-sourced implementation is preferable to a broad event-looking bridge.

## Options Considered and Rejected

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

## When a Rejected Option Would Have Been Better

- The rejected draft's event-recording approach would only be better if the goal were audit logging, not Event Sourcing. The goal is Event Sourcing.
- A big-bang rewrite would only be better if the repo were greenfield, which it is not.
- Exposing raw events would only be better if the API audience were developers only, which it is not.
- A separate event-store interface would only be better if the event store had a lifecycle independent of the aggregate repository, which it does not.

## Benefits

- One durable citation target for the true backend architecture posture.
- Future workers cannot infer the wrong dominant architecture from the codebase.
- The next UI slice has a safe, documented contract for projection consumption.
- BUNCH-3 (replay) and BUNCH-67 (sub-aggregates) have a clear dependency target.
- The half-pattern drift is explicitly rejected, not just undocumented.

## Accepted Tradeoffs

- Only two flows are migrated in this campaign. Non-migrated flows remain on direct mutation. The ADR records this transitional coexistence honestly.
- The snapshot is re-stored on every command (transitional cache). When all flows migrate, the snapshot becomes pure cache and replay-from-events becomes the production load path.
- `GameLogEntry` is demoted but not yet removed.

## Risks

- The ADR could be cited as if it were already fully implemented before the implementation steps land. Mitigation: status starts `planned`; Implementation Status names the implementing steps.
- Future workers could re-centralize narration in `GameSession` despite the projection taxonomy. Mitigation: ADR-0020 and this ADR both record the anti-pattern.
- The transitional coexistence (migrated + non-migrated) could confuse future developers. Mitigation: non-migrated flows are clearly marked; the ADR records the coexistence and the migration path.

## Consequences for Future Work

- New command-side work for migrated flows assumes command-produces-event-then-applies, not direct mutation.
- New read-side work assumes projections, not aggregate internals.
- Follow-up issues migrate non-migrated flows using the same pattern: typed event → `Apply` → replay → projection.
- BUNCH-3 replay work assumes the typed event vocabulary and `Apply`/replay mechanism from this campaign.
- BUNCH-67 sub-aggregate splits assume typed events can travel between aggregates via the public API pattern in ADR-0020.
- Future SignalR work is a transport layer over safe projections, not a new source truth.

## Implementation Status or Plan

`planned` on ADR-0028 landing. Promotion to `live` after the implementation steps land and the event-sourced slice is proven by tests. The implementing steps are:

- Step 1: typed domain events (`GameStarted`, `StoreItemPurchased`) in `WildBunch.Domain/Events/`.
- Step 2: event-sourced `GameSession` (`Apply` methods, `RehydrateFromEvents`, refactored `StartSetup`/`CompleteGameStart` (canonical start flow) and `Purchase`).
- Step 3: persistence event store (envelope, optimistic concurrency, snapshot cache, repository port extension).
- Step 4: projection contracts and reference projectors.
- Step 5: handler orchestration (load → command → store → commit → project → safe return).
- Step 6: API bridge safe projections.
- Step 7: `GameLogEntry` demotion.
- Step 8: consolidated tests and validation, ADR promotion to `live`.
- BUNCH-80 Step 1: 5 new typed domain events (TownActionContextEntered, SaloonPersonOfInterestSpotted, WantedSuspectConfronted, SheriffTurnInSettled, SaloonPersonOfInterestConfronted) + Apply methods + TimeOfDay enum + TownActionContext + EnterActionContext + RecordCaseUpdate decoupling.
- BUNCH-80 Step 2: DiaryProjector, HudProjector, CaseFileViewProjector updated for bounty/saloon events.
- BUNCH-80 Step 3: 5 event types registered in persistence deserializer; CurrentActionContext persisted in snapshot.
- BUNCH-80 Step 4: 5 bounty/saloon handlers migrated to GameSessionCommandHandler orchestration.
- BUNCH-80 Step 5: TimeOfDay added to GameClockDto + frontend display.
- BUNCH-80 Step 6: Hidden-truth boundary tests for 5 new event types.
- Remaining non-migrated flows: travel/journey (12+ AddLogEntry sites), case completion (1). LegacyLogProjector still not implemented (deferred to follow-up). LogEntries still in DTOs for backward compatibility.

## Related Stable Source Surfaces

- `docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md`
- `docs/adr/ADR-0003-composed-jsonb-session-persistence.md`
- `docs/adr/ADR-0007-hidden-culprit-truth-and-hidden-progress-boundaries.md`
- `docs/adr/ADR-0014-use-ddd-onion-cqrs-repositories-and-first-class-unit-of-work.md`
- `docs/adr/ADR-0020-aggregate-domain-authority-and-root-persistence-posture.md`
- `.agents/docs/architecture-hygiene.md`
- `.agents/docs/event-sourcing-integrity-policy.md` (the primary operational surface for event sourcing integrity — canonical flow, policy rules, negative constraints, and enforcement)
- `src/WildBunch.Domain/Game/GameSession.cs` (the aggregate root; modified in Step 2)
- Future: `src/WildBunch.Domain/Events/` (Step 1), `src/WildBunch.Application/Projections/` (Step 4), `src/WildBunch.Persistence/EventStore/` (Step 3)

## Proof of Implementation or Explicit Non-Implementation

This ADR is doctrine-only on landing. Proof is the ADR file itself plus the README index entry. Code proof arrives in the implementation steps and is cited in the Implementation Status section as those steps land.

## Review Triggers

- When BUNCH-3 lands full replay-from-events and the snapshot becomes pure cache.
- When BUNCH-67 splits sub-aggregates and events cross aggregate boundaries via public APIs.
- When all flows are migrated and the transitional coexistence ends.
- When SignalR transport is introduced.
- When `GameLogEntry` is fully removed.

## Historical Notes

BUNCH-67 (refactor GameSession into domain aggregates), BUNCH-68 (map GameSession responsibility slices and aggregate candidates), and BUNCH-72 (introduce bounty loop aggregate candidate inside GameSession) are closed as historical/superseded. The concrete child-component extraction pattern established by BUNCH-112 (`BountyLoop`), BUNCH-119 (`JourneyLoop`), and BUNCH-120 (`InvestigationLoop` + `ActionContextTracker` + `StoreLoop`) supersedes the earlier "future sub-aggregate splits" language referenced in this ADR. The references to BUNCH-67 above (lines 27, 57, 124, 140, 161, 202) are retained as part of the ADR's reasoning record but should be read as historical context, not as open future work. The current child-component inventory and lawful boundary rules are recorded in `.agents/docs/game-session-decomposition-audit.md`. Do not reopen the BUNCH-67/68/72 tracks.
