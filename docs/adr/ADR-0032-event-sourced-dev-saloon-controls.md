# ADR-0032 Event-Sourced Dev Saloon Controls

## Status

`live`

## Dated Status History

- 2026-06-26 - live: Event-sourced dev saloon override implemented. Three dev events (DevSaloonOverrideForced, DevSaloonOverrideCleared, DevSaloonOverrideConsumed) flow through GameSession command methods, Apply, and event replay. DevSaloonOverrideConsumed provides replay-safe consumption semantics. SaloonDevPanel registered in DevOverlay. Dev endpoints under /api/dev/sessions/{id}/saloon-context, /saloon/force-override, /saloon/clear-override. This is the first dev endpoint to deliberately expose hidden culprit truth (TrueCulpritId, suspect eligibility) through the ADR-0030 §7 player-vs-dev boundary. Hidden-truth guard test extended to cover the dev saloon-context endpoint.
- 2026-06-27 - live: BUNCH-90 doctrine and enrichment pass. True culprit eligibility is now gate-aware via KillerReleaseState (no longer permanently barred). The force path uses the same gate-aware eligibility as the candidate list — no special permanent true-culprit rejection. SaloonDevContextDto enriched with resolved suspect names, warrant facts (bounty, disposition, known features, summary), aliases, identifying facts, trait tags, citizen info, and gate-aware hidden truth (KillerReleaseStatus, KillerIsReleased, SaloonLoopExplanation). FalseLead removed from DevSaloonPoiKind enum, handler, and tests — it was semantically identical to Citizen and the false-lead outcome belongs to the normal confrontation flow. Force control renamed to "Force next saloon look-around POI" with scope description and candidate dropdowns replacing free-text suspect ID. Dev-overlay doctrine installed at `.agents/docs/dev-overlay-doctrine.md` as binding agent-facing law. Context mismatch detection added. Default panel selection prefers surface owner over Session Audit.

## Decision Type

architecture, dev, event-sourcing

## Related ADRs

- `depends on`: ADR-0028 (event-sourcing posture — dev events are domain events, replayed through Apply)
- `depends on`: ADR-0030 (dev overlay and dev endpoint namespace — /api/dev/ route and DevRoleGuard)
- `related to`: ADR-0007 (hidden culprit boundaries — player APIs must not leak hidden truth; dev APIs MAY)
- `follows`: ADR-0031 (event-sourced dev travel controls — same pattern, different domain area)

## Context

Playtesting saloon Point of Interest (POI) encounters requires:
1. Inspecting the hidden/internal saloon state — which suspects are eligible as saloon POI candidates, which is the true culprit, what warrants and presence states exist.
2. Forcing the next `LookAroundSaloon` to produce a specific POI shape: a particular wanted suspect, any eligible suspect, or a citizen. (The false-lead outcome is not a separate force kind — it comes from the normal confrontation flow when the player declares a wrong wanted identity on a citizen POI.)

The previous debug cockpit had no saloon forcing capability and no way to inspect suspect eligibility without playing through the game. ADR-0030 established the dev overlay and /api/dev/ namespace, and ADR-0031 established the event-sourced dev override pattern for travel. This ADR applies the same pattern to saloon POI encounters.

A key difference from ADR-0031: the saloon dev context is the first dev surface to deliberately expose hidden culprit truth. ADR-0030 §7 established that dev endpoints MAY expose hidden truth when deliberately scoped, guarded, and separated from player DTOs. This ADR exercises that boundary for the first time.

## Decision

1. **DevSaloonOverride record.** A plain domain record `DevSaloonOverride(DevSaloonPoiKind ForcedKind, SuspectId? ForcedSuspectId)` carries the override payload. `DevSaloonPoiKind` is an enum with values `Suspect`, `Citizen`. Factory methods `ForSuspect`, `ForAnySuspect`, `ForCitizen` construct common shapes. The false-lead confrontation outcome is not a separate override kind — it comes from the normal confrontation flow when the player declares a wrong wanted identity on a citizen POI. To test the false-lead path, force a Citizen override and then make a wrong declaration during confrontation.

2. **Three dev events.** The override lifecycle is event-sourced through three sealed record events implementing IDomainEvent:
   - `DevSaloonOverrideForced` — sets the pending override. Produced by `ForceDevSaloonOverride`.
   - `DevSaloonOverrideCleared` — clears the pending override. Produced by `ClearDevSaloonOverride` (no-op if nothing pending).
   - `DevSaloonOverrideConsumed` — marks the override as consumed by the next `LookAroundSaloon`. Produced inside `LookAroundSaloon` when a pending override exists.

   All three events have Apply methods on GameSession and cases in ApplyProducedEvent and GameSessionEventReplay.ApplyEvent.

3. **Consume-once with capture-before-emit.** In `LookAroundSaloon`, the pending override is captured into a local variable before `ProduceEvent(new DevSaloonOverrideConsumed())` is called. `ProduceEvent` calls `Apply`, which clears `_pendingDevSaloonOverride`. The forced POI is then produced through the normal `SaloonPersonOfInterestSpotted` route using the captured override. The consumed event appears before the spotted event in the event stream, making the lifecycle fully auditable.

4. **Suspect eligibility validation.** `ForceDevSaloonOverride` rejects:
   - Unknown suspect IDs (not in CaseFile.Suspects)
   - The true culprit when the killer-release gate is locked (gate-aware via `KillerReleaseState.IsReleased`)
   - Suspects with a known warrant whose presence state is not AvailableInTown or GoneToGround (they are already secured or unavailable)

   The eligibility check reuses the existing `IsEligibleSaloonPersonOfInterestCandidate` logic (made internal for dev mapper access). When the killer-release gate is open, the true culprit becomes a valid saloon POI candidate. `ForceDevSaloonOverride` also rejects when a journey is active (journey-modal state).

5. **Dev endpoints.** Three endpoints under /api/dev/:
   - `GET /api/dev/sessions/{id}/saloon-context` — returns saloon context, hidden truth, suspect eligibility, and pending dev override via `SaloonDevContextDto`.
   - `POST /api/dev/sessions/{id}/saloon/force-override` — forces the next saloon override.
   - `POST /api/dev/sessions/{id}/saloon/clear-override` — clears a pending override.

   All gated by `DevRoleGuard.EnsureDevAccess()`. Dev DTOs are separate types from player DTOs.

6. **Hidden-truth exposure.** `SaloonDevContextDto` includes `HiddenTruthDevDto` (TrueCulpritId, TrueCulpritName, KillerReleaseStatus, KillerIsReleased, SaloonLoopExplanation) and `SaloonSuspectDevDto` (IsTrueCulprit, IsEligibleSaloonPoi, IneligibilityReason, HasKnownWarrant, PresenceState, Aliases, IdentifyingFacts, TraitTags, BountyAmount, WarrantDisposition, WarrantKnownFeatures, WarrantSummary). `ActiveSaloonPoiDto` includes `SuspectName` for resolved display. `CitizenInfoDto` honestly describes the citizen POI shape (no named archetypes exist). `DevSaloonOverrideDto` includes `ForcedSuspectName`. This is the first dev DTO to deliberately expose hidden culprit truth. The exposure is bounded:
   - The DTO is a separate type from player DTOs (GameSessionDto, JournalDto).
   - The endpoint is under /api/dev/ with DevRoleGuard.
   - Player-facing APIs continue to be guarded by `GameApiHiddenTruthTests`.
   - The dev-overlay doctrine (`.agents/docs/dev-overlay-doctrine.md`) governs what hidden truth is useful vs sensational.

7. **Persistence.** The three dev events are registered in `ResolveEventType` for event-stream serialization. The `PendingDevSaloonOverride` is stored as a snapshot component (`pendingDevSaloonOverride`) in the EF component-based snapshot path and as a field in the full `GameSessionSnapshot` record. On load, `_pendingDevSaloonOverride` is set via `GameSessionRehydrator.SetBackingField`. Post-snapshot event replay overwrites the snapshot value via Apply.

8. **Frontend.** `SaloonDevPanel` registered in `DevPanelRegistry` renders saloon context, hidden truth, suspect eligibility, pending override, and force/clear controls. Uses `@tanstack/react-query` for the dev context query and invalidates on force/clear.

9. **Source invariant fix.** BUNCH-90 also corrected a domain invariant: every town always has a saloon and a sheriff's office. `LookAroundSaloon` and `ReadWantedPosters` are always available (Baseline availability), not conditional on `TownServices.NoticeBoard` or `TownServices.None`. The dead `SupportsWantedPosters` property and `IsAvailable` checks were removed. This is a source-of-truth fix, not a dev-only change.

## Options Considered and Rejected

- **Set-and-clear without events.** Rejected: breaks replay. Same reasoning as ADR-0031.
- **Single DevSaloonOverrideSet event.** Rejected: conflates force and clear intents. Same reasoning as ADR-0031.
- **Expose hidden truth through player APIs.** Rejected: violates ADR-0007. Hidden truth is dev-only.
- **Separate hidden-truth endpoint.** Rejected: the saloon dev context already needs suspect eligibility for the force-override UI. Splitting hidden truth into a separate endpoint adds a round-trip without adding a boundary. The single `SaloonDevContextDto` is the dev inspection surface.
- **Make IsEligibleSaloonPersonOfInterestCandidate public.** Rejected: making it internal keeps the domain boundary tight while allowing the dev mapper in the Application layer to call it. Public would expose domain internals to all consumers.

## Consequences

- Dev saloon overrides are fully event-sourced and replay-safe.
- The event stream is auditable: Forced, Cleared, and Consumed events record the full override lifecycle.
- The SaloonDevPanel is the third panel in the DevOverlay, further exercising the registry pattern from ADR-0030.
- Hidden culprit truth is now accessible through the dev overlay for playtesting and debugging, but remains blocked from player APIs.
- The `GameApiHiddenTruthTests` guard test is extended to cover the dev saloon-context endpoint, proving the player boundary holds while the dev surface works.
- Snapshot persistence includes the pending dev saloon override, so a session loaded from snapshot retains the override without replaying the full stream.
- The source invariant fix (saloon always available) means all towns have saloon and sheriff's office actions regardless of TownServices flags.
