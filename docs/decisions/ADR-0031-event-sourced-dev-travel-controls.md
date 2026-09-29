# ADR-0031 Event-Sourced Dev Travel Controls

## Status

`live`

## Dated Status History

- 2026-06-25 - live: Event-sourced dev travel override implemented. Three dev events (DevTravelOverrideForced, DevTravelOverrideCleared, DevTravelOverrideConsumed) flow through GameSession command methods, Apply, and event replay. DevTravelOverrideConsumed provides replay-safe consumption semantics. TravelDevPanel registered in DevOverlay. Dev endpoints under /api/dev/sessions/{id}/travel-context, /travel/force-override, /travel/clear-override. Hidden-truth guard test extended to cover the dev travel-context endpoint.

## Decision Type

architecture, dev, event-sourcing

## Related ADRs

- `depends on`: ADR-0028 (event-sourcing posture — dev events are domain events, replayed through Apply)
- `depends on`: ADR-0030 (dev overlay and dev endpoint namespace — /api/dev/ route and DevRoleGuard)
- `related to`: ADR-0007 (hidden culprit boundaries — dev travel-context must not leak hidden truth)

## Context

Playtesting travel encounters requires forcing specific encounter categories (Foe, Npc, Lucky, etc.) and foe profiles (speed, fight strength, minimum bribe) on the next travel-day advance. The previous debug cockpit had no travel forcing capability. ADR-0030 established the dev overlay and /api/dev/ namespace but did not implement travel-specific dev controls.

The core design challenge is replay safety: a forced override must be consumed exactly once by the next AdvanceJourneyDay, and replaying the event stream must reconstruct the correct final state. A naive "set a flag and clear it in the command" approach breaks replay because the clear happens outside the event stream.

## Decision

1. **DevTravelOverride record.** A plain domain record `DevTravelOverride(TravelDayEncounterCategory ForcedCategory, JourneyFoeProfile? FoeProfile, string? EncounterMessage)` carries the override payload. Factory methods `ForFoe` and `ForCategory` construct common shapes.

2. **Three dev events.** The override lifecycle is event-sourced through three sealed record events implementing IDomainEvent:
   - `DevTravelOverrideForced` — sets the pending override. Produced by `ForceDevTravelOverride`.
   - `DevTravelOverrideCleared` — clears the pending override. Produced by `ClearDevTravelOverride` (no-op if nothing pending).
   - `DevTravelOverrideConsumed` — marks the override as consumed by the next travel-day advance. Produced by `PrepareTravelDayAdvance` when a pending override exists.

   All three events have Apply methods on GameSession and cases in ApplyProducedEvent and GameSessionEventReplay.ApplyEvent.

3. **Consume-once with capture-before-emit.** In `PrepareTravelDayAdvance`, the pending override is captured into a local variable before `ProduceEvent(new DevTravelOverrideConsumed())` is called. This is critical because `ProduceEvent` calls `Apply`, which clears `_pendingDevTravelOverride`. The forced day plan is built from the captured local, not the field. This ordering bug was caught during plan review.

4. **TravelDayPlanFactory.** A pure helper creates a `TravelDayPlanState` from a `DevTravelOverride`, bypassing the normal `TravelDayPlanGenerator`. The forced plan contains a single encounter matching the override category. For Foe overrides, the encounter uses the provided foe profile or a default derived from `TravelRulesProfile.EncounterBribeCash`.

5. **Dev endpoints.** Three endpoints under /api/dev/:
   - `GET /api/dev/sessions/{id}/travel-context` — returns journey state, pending encounter, and pending dev override via `TravelDevContextDto`.
   - `POST /api/dev/sessions/{id}/travel/force-override` — forces the next travel override.
   - `POST /api/dev/sessions/{id}/travel/clear-override` — clears a pending override.

   All gated by `DevRoleGuard.EnsureDevAccess()`. Dev DTOs are separate types from player DTOs.

6. **Persistence.** The three dev events are registered in `ResolveEventType` for event-stream serialization. The `PendingDevTravelOverride` is stored as a snapshot component (`pendingDevTravelOverride`) in the EF component-based snapshot path and as a field in the full `GameSessionSnapshot` record. On load, `_pendingDevTravelOverride` is set via `GameSessionRehydrator.SetBackingField`. Post-snapshot event replay overwrites the snapshot value via Apply.

7. **Frontend.** `TravelDevPanel` registered in `DevPanelRegistry` renders journey state, pending override, and force/clear controls. Uses `@tanstack/react-query` for the dev context query and invalidates on force/clear.

8. **Hidden-truth boundary.** The dev travel-context endpoint exposes journey internals (status, days, pending encounter kind/message, foe profile, dev override) but does NOT expose hidden culprit truth. `GameApiHiddenTruthTests` includes a guard test for the dev travel-context endpoint.

## Options Considered and Rejected

- **Set-and-clear without events.** Rejected: breaks replay. The clear happens outside the event stream, so a replayed session would have a stale pending override.
- **Single DevTravelOverrideSet event (force + clear in one).** Rejected: conflates two distinct dev intents and makes the event stream harder to audit.
- **Consume in AdvanceJourneyDay without a DevTravelOverrideConsumed event.** Rejected: the consumption is invisible in the event stream. Replay cannot distinguish "override was pending and consumed" from "override was never set." The explicit consumed event makes the lifecycle fully auditable.
- **Dev override as a flag on TravelDayAdvanced.** Rejected: couples dev state to a gameplay event. Dev events should be separate so they can be filtered, audited, and stripped independently.

## Consequences

- Dev travel overrides are fully event-sourced and replay-safe.
- The event stream is auditable: Forced, Cleared, and Consumed events record the full override lifecycle.
- Future dev controls (saloon POI forcing, encounter seeding) follow the same pattern: dev events + Apply + replay cases + dev endpoints + DevRoleGuard.
- The TravelDevPanel is the second panel in the DevOverlay, establishing the registry pattern from ADR-0030.
- Snapshot persistence includes the pending dev override, so a session loaded from snapshot retains the override without replaying the full stream.
