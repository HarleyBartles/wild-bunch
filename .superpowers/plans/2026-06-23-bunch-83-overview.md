# BUNCH-83: Migrate Travel and Journey Flows to Event Sourcing — Campaign Overview

> **For agentic workers:** This is the master overview for BUNCH-83. Phase plans are in separate documents:
> - Phase 1: `2026-06-23-bunch-83-phase1-characterization-tests.md` — Characterization tests pinning exact current behavior with deterministic ForcedRoll scenarios
> - Phase 2: `2026-06-23-bunch-83-phase2-events-apply-migration.md` — 6 typed domain events + 6 Apply methods + 8 domain method migrations + clock decoupling + replay-equality proof
> - Phase 3: `2026-06-23-bunch-83-phase3-projections-persistence-handlers.md` — Projections + persistence + handler migration (preview inside retry boundary) + ADR update + validation

**Goal:** Migrate the complete travel/journey state machine to typed domain events + `Apply` mutation + replay + projections, following the ADR-0028 event-sourcing architecture established by BUNCH-77/78/80. This is the dedicated follow-up to BUNCH-80 (which migrated bounty/saloon + clock/turn correction and explicitly deferred travel).

**Base:** `main` at `7dc15d4` (BUNCH-80 merged, PR #98). Clean worktree. **All line numbers, file paths, guardrail counts, and ADR paths in this document are preflight notes from this commit and MUST be re-verified at execution time against current `main`.**

**Approval gate:** Awaiting Harley approval before execution. Do not start implementation until approved.

---

## Preflight Summary (from `main` at `7dc15d4`, verified against source)

### A. Already event-sourced (8 event types registered in persistence deserializer)

`GameStarted`, `StoreItemPurchased`, `InvestigationPerformed`, `TownActionContextEntered`, `SaloonPersonOfInterestSpotted`, `WantedSuspectConfronted`, `SheriffTurnInSettled`, `SaloonPersonOfInterestConfronted`

### B. Travel/journey seam — NOT event-sourced (the target)

**B1. Domain methods on `GameSession` (`GameSession.cs`):**

| Method | Purpose | State Mutated |
|--------|---------|---------------|
| `StartJourney` | Creates journey, clears diary, increments sequence | Journey, _travelDiaryDays, _nextJourneySequence |
| `AdvanceJourneyDay` → `AdvanceJourneyDayDeterministic` | Advances one trail day: upkeep, encounters, trail events, completion | Journey, Player, PursuitState, Clock, _travelDiaryDays |
| `PrepareTravelDayAdvance` | Helper: upkeep, clock advance, day plan generation | Journey, Player, Clock |
| `HandleInterruptedTravelDay` | Day result when interrupted by encounter | Journey (MarkInterrupted), _travelDiaryDays |
| `HandleCompletedTravelDay` | Day result when journey completes | Journey (MarkCompleted), Player (TravelTo), _travelDiaryDays |
| `HandleOngoingTravelDay` | Day result when journey continues | _travelDiaryDays |
| `ApplyTrailEvent` | Applies trail event deltas | Player (wallet, food, canteen, horse), Journey, PursuitState |
| `ResolveJourneyEncounter` → `ResolveJourneyEncounterDeterministic` | Resolves encounter (run/fight/bribe), continues day | Player, Journey, PursuitState, _travelDiaryDays |
| `ContinueCurrentDayAfterEncounterResolution` | Continues day after encounter, may apply trail events, present new encounter, or complete | Journey, Player, _travelDiaryDays |
| `AcknowledgeJourneyArrival` | Archives completed journey, clears active journey | _completedJourneyHistory, Journey |
| `CompleteJourneyAtDestination` | Marks completed, travels to destination, refills canteen | Journey, Player, _currentTown |

**B2. AddLogEntry call sites in travel methods (13 total, all `GameLogEntryKind.Travel`):**

These are the call sites to be replaced by `RecordTravelUpdate`. Exact line numbers must be re-verified at execution time.

**B3. Handlers (4, all using manual load/store/commit — NOT `GameSessionCommandHandler` base):**

| Handler | GameSession method called | Retry-safe? |
|---------|--------------------------|-------------|
| `TravelToTownHandler` | `StartJourney` | **NO** — preview generated outside retry boundary, `StartJourney` trusts preview blindly |
| `AdvanceTravelDayHandler` | `AdvanceJourneyDay` | NO — no retry at all |
| `ResolveJourneyEncounterHandler` | `ResolveJourneyEncounter` | NO — no retry at all |
| `AcknowledgeJourneyArrivalHandler` | `AcknowledgeJourneyArrival` | NO — no retry at all |

**Critical handler finding:** `TravelToTownHandler` generates the journey preview via `TravelResolver.PreviewJourney` using `session.Player.CurrentTownId` and `session.Player.Inventory` — both mutable session state. `StartJourney` blindly trusts the preview (no re-validation). If a concurrency conflict occurs during store and the session is reloaded, the preview would be stale. The Phase 3 handler migration MUST move preview generation inside the `ExecuteWithRetryAsync` lambda so it is regenerated on each retry attempt with fresh session state.

**B4. Journey state type:**

`TravelJourney` (`src/WildBunch.Domain/Travel/TravelJourney.cs`) — sealed class with `private set` properties. Key mutable fields: TravelMode, Status, RemainingRideDayDistance, RemainingDays, DaysTravelled, DelayDays, PendingEncounter, CurrentDayPlan, FoodRemaining, HorseFeedRemaining, AvailableCanteenCharges, HorseState, OpeningNarration. Immutable fields: Preview (read-only), JourneySequence (read-only).

**B5. `TravelJourneySnapshot` ALREADY EXISTS:**

`TravelJourneySnapshot` is a sealed record at `src/WildBunch.Domain/Travel/TravelRouteModels.cs:82` with 30+ fields. `TravelJourney.ToSnapshot()` (line 347) and `TravelJourney.FromSnapshot()` (line 78) already exist. **No new snapshot type needs to be created.** The plan uses the existing snapshot type inside events.

**B6. Hidden encounter state:**

`JourneyEncounterHiddenState` — sealed record with init-only properties: BribeOffersMade, CumulativeBribePaid, BribeLockedOut, ChaseFatigue, Annoyance, Shaken. Accumulates within a single encounter across resolution attempts. Part of `JourneyEncounterState` (sealed record) which is part of `TravelJourneySnapshot`. NOT exposed in DTOs (TravelMapper excludes it).

**B7. Determinism:**

`TravelDayPlanGenerator` uses SHA256(seed) with complex seed composition. `JourneyEncounterResolutionEngine` uses deterministic rolls from seed + label. Tests use `TravelRandomnessState.CreateDeterministic(string.Empty)` and `ForcedRoll` parameters (`0UL` = success, `99UL` = failure, `null` = actual deterministic roll).

**B8. Clock coupling:**

`Clock.AdvanceTravelDay()` called from `PrepareTravelDayAdvance` — increments Day, resets Turn to 0. This is the ONLY clock advancement in travel. `Clock.Set(int day, int turn)` already exists (used by `Apply(TownActionContextEntered)`). The migration will replace the direct `AdvanceTravelDay()` call with `Clock.Set(e.Day, 0)` inside `Apply(TravelDayAdvanced)`.

### C. Existing tests (50+ across multiple files)

- `TravelToTownHandlerTests` — 3 tests (success/failure/empty inventory)
- `AdvanceTravelDayHandlerTests` — 6 tests (trail events, encounters, horse lameness, journey completion, multi-day)
- `ResolveJourneyEncounterHandlerTests` — 7 tests (run/fight/bribe resolution, diary accumulation, resource tracking)
- `GameSessionJourneyHistoryTests` — 1 test (journey sequencing and archival)
- `JourneyUpkeepRulesTests` — 5 tests (horse state machine, upkeep rules, terrain/water)
- `TravelResolverTests`, `TravelDayPlanGeneratorTests`, `TravelDiaryDayFactoryTests`, `TravelRulesProfileTests`
- `GameSessionCommandHandlerTests` — 5 tests (orchestration, retry, no-op detection)
- `EventSourcingEndToEndTests` — 3 tests (full cycle, replay determinism, concurrency)
- `BountySaloonEventSourcingTests` — BUNCH-80 pattern: assert exact event types, counts, and command-path == replay-path state equality
- `AddLogEntryGuardrailTests` — 1 test (current cap: `Assert.True(count <= 19)`, upper bound, includes method definition)

**Gaps to fill (Phase 1):** Full state machine transitions with exact value assertions, replay determinism from events, edge cases (blocked encounters, chase fatigue, bribe lockout, horse lameness, ammo depletion, retaliation). All tests must use `ForcedRoll` for deterministic outcomes and assert exact field values, not directional changes.

### D. Current projectors (no travel coverage)

- `DiaryProjector` — handles 8 event types, NO travel events
- `HudProjector` — handles 4 event types, NO travel events
- `CaseFileViewProjector` — handles 3 event types, NO travel events
- `FullAuditProjector` — generic audit for all events

### E. Event-sourcing infrastructure (from BUNCH-77/78/80, proven)

- `ProduceEvent<T>(T e)` — canonical produce step: `Apply(e)` + `_uncommittedEvents.Add(e)`
- `ApplyProducedEvent(IDomainEvent e)` — dispatch switch mirroring `GameSessionEventReplay.ApplyEvent`
- `GameSessionEventReplay.ApplyEvent` — dispatch for replay
- `GameSessionCommandHandler.ExecuteWithRetryAsync<T>` — load → lambda → store → commit, retries up to 3 on `ConcurrencyException`, detects no-op by `UncommittedEvents.Count == 0`
- `GameSessionJsonSerializer.Events.cs` — `ResolveEventType` switch for deserialization, System.Text.Json with `JsonSerializerDefaults.Web`
- `RehydrateFromEvents` — creates placeholder session, replays all events through `ApplyEvent`, returns fully reconstructed session

---

## Snapshot Safety Proof

### Why carrying `TravelJourneySnapshot` inside events is replay-safe

The BUNCH-80 pattern carries primitives + IDs inside events, not snapshots. However, `TravelJourneySnapshot` is not a mutable domain object — it is a sealed record containing only immutable sealed records and primitives. Carrying it inside events is safe and precedented:

- `GameStarted` already carries `IReadOnlyList<InventoryItem>` (immutable records) and `TravelRandomnessState` (immutable record) inside the event
- `TravelJourneySnapshot` is the same kind of type: an immutable sealed record

### Immutability verification (all nested types are sealed records with init-only properties)

| Type | File | Kind | Sealed | Immutable | Safe for ref copy |
|------|------|------|--------|-----------|-------------------|
| `TravelJourneySnapshot` | `TravelRouteModels.cs:82` | record | Yes | Yes (init-only) | Yes |
| `TravelPreview` | `TravelRouteModels.cs:51` | record | Yes | Yes (init-only) | Yes |
| `TravelRouteProfile` | `TravelRouteModels.cs:23` | record | Yes | Yes (init-only) | Yes |
| `JourneyEncounterState` | `JourneyEncounterModels.cs:71` | record | Yes | Yes (init-only) | Yes |
| `JourneyEncounterHiddenState` | `JourneyEncounterModels.cs:5` | record | Yes | Yes (init-only) | Yes |
| `JourneyEncounterChoiceState` | `JourneyEncounterModels.cs:3` | record | Yes | Yes (init-only) | Yes |
| `JourneyFoeProfile` | `JourneyEncounterModels.cs:35` | record | Yes | Yes (init-only) | Yes |
| `TravelDayPlanState` | `TravelDiaryModels.cs:89` | record | Yes | Yes (init-only) | Yes |
| `TravelDayEncounterState` | `TravelDiaryModels.cs:75` | record | Yes | Yes (init-only) | Yes |
| `JourneyTrailEventState` | `JourneyTrailEventModels.cs:20` | record | Yes | Yes (init-only) | Yes |
| `TravelDiaryEncounterResolutionState` | `TravelDiaryModels.cs:53` | record | Yes | Yes (init-only) | Yes |
| `HorseTravelState` | `HorseTravelState.cs:5` | record | Yes | Yes (read-only) | Yes |
| `TravelMode` | `TravelRouteModels.cs:9` | enum | N/A | Yes | Yes |
| `JourneyStatus` | `TravelRouteModels.cs:15` | enum | N/A | Yes | Yes |
| `TownId` | `WorldModels.cs:3` | readonly record struct | Yes | Yes | Yes |

**Conclusion:** Every type nested inside `TravelJourneySnapshot` is a sealed record (or enum/struct) with init-only or read-only properties. No nested type has public setters or mutation methods. Reference copy into events is safe — the snapshot cannot be mutated after creation, and replay will reconstruct identical state.

**The one mutable type — `TravelJourney` itself — is never carried inside events.** Only its immutable snapshot is carried. `TravelJourney.FromSnapshot()` reconstructs a new mutable `TravelJourney` from the immutable snapshot during replay.

### Serialization safety

`GameSessionJsonSerializer` already serializes/deserializes `TravelJourneySnapshot` for session persistence (via `TravelJourneySnapshot.FromDomain` / `ToDomain` conversion methods in the serializer). System.Text.Json with `JsonSerializerDefaults.Web` handles sealed records automatically. The same serialization path will work for events carrying the snapshot.

---

## Event Design (6 typed domain events)

All events are sealed records implementing `IDomainEvent`, owned by `WildBunch.Domain.Events`. Events carry structured fields only — no envelope metadata.

### Event semantics: absolute snapshots vs additive deltas

**Critical rule to prevent double-application:**

- **Journey state** is carried as `TravelJourneySnapshot` (ABSOLUTE). `Apply` sets `_journey = TravelJourney.FromSnapshot(e.JourneySnapshot)`. The snapshot captures the journey state AFTER the event's changes. No journey deltas are applied — the snapshot IS the journey state.
- **Player state** (health, wallet, food, ammo, inventory) is carried as deltas (ADDITIVE). `Apply` adds `e.HealthDelta` to player health, `e.WalletDelta` to player wallet, etc.
- **Pursuit state** (heat) is carried as deltas (ADDITIVE). `Apply` adds `e.PursuitHeatDelta` to pursuit heat.
- **Clock state** is carried as absolute values. `Apply` calls `Clock.Set(e.Day, 0)`.

This separation prevents double-application because:
1. Journey state comes ONLY from the snapshot (never from deltas)
2. Player/pursuit state comes ONLY from deltas (never from the snapshot)
3. The two do not overlap (journey snapshot does not carry player health/wallet)

### Event 1: `JourneyStarted`
**Produced by:** `StartJourney`
**Fields:**
- `JourneySnapshot: TravelJourneySnapshot` — initial journey state (ABSOLUTE — Apply sets `_journey`)
- `DiaryMessage: string` — journey start narration

### Event 2: `TravelDayAdvanced`
**Produced by:** `AdvanceJourneyDay` (always, when a day advances)
**Fields:**
- `Day: int` — new day after advancement (ABSOLUTE — Apply calls `Clock.Set(e.Day, 0)`)
- `JourneySnapshot: TravelJourneySnapshot` — post-advancement journey state (ABSOLUTE — Apply sets `_journey`)
- `HealthDelta: int` — from upkeep (ADDITIVE — Apply adds to player health)
- `PursuitHeatDelta: decimal` — from risk (ADDITIVE — Apply adds to pursuit heat)
- `DayOutcome: TravelDayOutcome` — enum: Ongoing, Interrupted, Completed
- `DiaryMessage: string` — day narration
- `HorseLostMessage: string` — horse loss narration (empty if none)

### Event 3: `TrailEventApplied`
**Produced by:** `AdvanceJourneyDay` and `ResolveJourneyEncounter` (during continuation)
**Fields:**
- `JourneySnapshot: TravelJourneySnapshot` — post-trail-event journey state (ABSOLUTE — captures delay days, horse state changes, travel mode changes)
- `TrailEventKind: JourneyTrailEventKind` — Lucky or BadLuck
- `TrailEventId: JourneyTrailEventId` — specific event type
- `WalletDelta: decimal` (ADDITIVE — Apply adds to player wallet)
- `FoodDelta: int` (ADDITIVE — Apply adds to player food)
- `CanteenChargeDelta: int` (ADDITIVE — Apply adds to player canteen charges)
- `HorseHungerDelta: int` — informational (journey snapshot captures horse state)
- `HorseThirstDelta: int` — informational (journey snapshot captures horse state)
- `HorseExhaustionDelta: int` — informational (journey snapshot captures horse state)
- `DelayDays: int` — informational (journey snapshot captures delay days)
- `HeatIncrease: decimal` (ADDITIVE — Apply adds to pursuit heat)
- `TravelModeChangedTo: TravelMode?` — informational (journey snapshot captures travel mode)
- `DiaryMessage: string` — trail event narration
- `HorseLostMessage: string` — horse loss narration (empty if none)

Note: Horse/delay/mode fields are informational for projections and audit. The actual state changes are captured in the journey snapshot (ABSOLUTE). Player resource deltas (wallet, food, canteen) and pursuit heat are ADDITIVE.

### Event 4: `JourneyEncounterResolved`
**Produced by:** `ResolveJourneyEncounter`
**Fields:**
- `ChoiceId: string` — "run", "fight", or "bribe"
- `ChoiceLabel: string` — display label
- `Resolved: bool` — true if encounter was resolved (not just another failed attempt)
- `HealthDelta: int` (ADDITIVE — Apply adds to player health)
- `WalletDelta: decimal` (ADDITIVE — Apply adds to player wallet)
- `AmmoSpent: int` (ADDITIVE — Apply subtracts from player ammo)
- `StolenItemKind: ItemKind?` — if items were stolen (bribe retaliation)
- `StolenItemQuantity: int` (ADDITIVE — Apply removes from player inventory)
- `PursuitHeatDelta: decimal` (ADDITIVE — Apply adds to pursuit heat)
- `HorseExhaustionDelta: int` — informational (journey snapshot captures horse state)
- `ContinuedOnFoot: bool` — informational (journey snapshot captures travel mode)
- `JourneySnapshot: TravelJourneySnapshot` — post-resolution journey state (ABSOLUTE — captures status, pending encounter if new one presented, day plan, hidden state)
- `DiaryMessage: string` — resolution narration
- `DayCompleted: bool` — if day plan is complete after continuation
- `JourneyCompleted: bool` — if journey reached destination during continuation

### Event 5: `JourneyCompleted`
**Produced by:** `AdvanceJourneyDay` and `ResolveJourneyEncounter` (when destination reached)
**Fields:**
- `DestinationTownId: TownId` (ABSOLUTE — Apply sets player town)
- `DestinationTownName: string`
- `JourneySnapshot: TravelJourneySnapshot` — completed journey state (ABSOLUTE — Apply sets `_journey`)
- `DiaryMessage: string` — arrival narration

### Event 6: `JourneyArrivalAcknowledged`
**Produced by:** `AcknowledgeJourneyArrival`
**Fields:**
- `JourneySequence: int` — for archival identification
- `JourneySnapshot: TravelJourneySnapshot` — the completed snapshot being archived
- `DiaryMessage: string` — arrival acknowledgement narration

### Composite event production

| Command | Events produced |
|---------|----------------|
| `StartJourney` (success) | `JourneyStarted` |
| `StartJourney` (failure: already on trail) | 0 events |
| `AdvanceJourneyDay` (ongoing, no trail event) | `TravelDayAdvanced` |
| `AdvanceJourneyDay` (ongoing, with trail event) | `TravelDayAdvanced` + `TrailEventApplied` |
| `AdvanceJourneyDay` (interrupted by encounter) | `TravelDayAdvanced` (outcome=Interrupted, snapshot includes pending encounter) |
| `AdvanceJourneyDay` (completed) | `TravelDayAdvanced` (outcome=Completed) + `JourneyCompleted` |
| `AdvanceJourneyDay` (failure: no journey/pending encounter/not active) | 0 events |
| `ResolveJourneyEncounter` (resolved, day continues) | `JourneyEncounterResolved` |
| `ResolveJourneyEncounter` (resolved, trail event during continuation) | `JourneyEncounterResolved` + `TrailEventApplied` |
| `ResolveJourneyEncounter` (resolved, journey completes) | `JourneyEncounterResolved` + `JourneyCompleted` |
| `ResolveJourneyEncounter` (failed attempt, encounter persists) | `JourneyEncounterResolved` (Resolved=false) |
| `ResolveJourneyEncounter` (failure: no journey/no encounter/not interrupted) | 0 events |
| `AcknowledgeJourneyArrival` (success) | `JourneyArrivalAcknowledged` |
| `AcknowledgeJourneyArrival` (failure: no journey/not completed) | 0 events |

### Snapshot timing rule (prevents double-application)

Each event's `JourneySnapshot` captures the journey state **AFTER that event's changes have been applied**. When multiple events are produced in sequence (e.g. `TravelDayAdvanced` + `TrailEventApplied`), each snapshot reflects the cumulative state up to that point:

1. `TravelDayAdvanced` snapshot: journey state after upkeep + day plan generation + encounter/trail event determination
2. `TrailEventApplied` snapshot: journey state after trail event deltas (delay days, horse state, travel mode) applied on top of the `TravelDayAdvanced` state

During replay, `Apply` sets `_journey` absolutely from each snapshot in order. The final journey state matches the command-path state because each snapshot captures the exact intermediate state.

### Hidden state handling

**Standard:** Hidden encounter state (`JourneyEncounterHiddenState`: BribeOffersMade, CumulativeBribePaid, BribeLockedOut, ChaseFatigue, Annoyance, Shaken) MAY exist inside internal persisted events for replay correctness, but MUST NOT leak through player-facing projections, DTOs, or API responses.

- `JourneyEncounterHiddenState` is part of `JourneyEncounterState` which is part of `TravelJourneySnapshot`
- Events carry the journey snapshot (including hidden state) in their internal persisted form — this is required for replay fidelity
- Projections (`DiaryProjector`, `HudProjector`) derive only diary/HUD output from events and never read or expose hidden state fields
- `TravelMapper` already excludes hidden state from DTOs
- Hidden-truth boundary tests (Phase 3) verify no leakage through projections or DTOs — they do NOT verify absence from internal event JSON, because hidden state is intentionally present there for replay

### Clock decoupling

- `TravelDayAdvanced` carries the new `Day` value
- `Apply(TravelDayAdvanced)` calls `Clock.Set(e.Day, 0)` — `Clock.Set` already exists (used by `Apply(TownActionContextEntered)`)
- `Clock.AdvanceTravelDay()` is no longer called directly from domain methods
- This mirrors the `TownActionContextEntered` pattern where the event carries clock state

---

## Plan Structure (3 phases)

### Phase 1: Characterization Tests
Pin exact current behavior before refactor. These tests verify CURRENT behavior and must continue passing after migration. They serve as the safety net.

**Standard for characterization tests (addressing the gap in the first draft):**
- Use `ForcedRoll` to force specific encounter outcomes (`0UL` = success, `99UL` = failure) — never loop-until-desired
- Use `TravelRandomnessState.CreateDeterministic(string.Empty)` for reproducible day plans
- Assert EXACT field values (health=8, wallet=15.50, food=3, RemainingDays=2) — not directional changes
- Assert exact event types and counts (following BUNCH-80 `BountySaloonEventSourcingTests` pattern)
- Assert exact diary message content (not just "entries exist")
- Assert exact journey state fields (Status, DaysTravelled, RemainingDays, PendingEncounter)
- No conditional assertions — force the scenario deterministically, don't hope for it

Key test areas:
- Full state machine transitions (Active→Interrupted→Active→Completed→Archived) with exact state at each step
- Encounter resolution (run/fight/bribe) with ForcedRoll and exact resource deltas
- Edge cases (blocked encounters, chase fatigue, bribe lockout, horse lameness, ammo depletion)
- Travel diary accumulation with exact entry counts and content
- Resource tracking with exact values (food consumption, health deltas, wallet changes, pursuit heat)

### Phase 2: Events + Apply + Domain Method Migration
- Define 6 typed domain events in `src/WildBunch.Domain/Events/` (carrying existing `TravelJourneySnapshot` + deltas)
- Define `TravelDayOutcome` enum
- Implement 6 `Apply` methods on `GameSession` (journey from snapshot=absolute, player/pursuit from deltas=additive)
- Register 6 new event types in `ApplyProducedEvent` dispatch and `GameSessionEventReplay.ApplyEvent` dispatch
- Migrate 8 domain methods to command-produces-event-then-applies pattern
- Introduce `RecordTravelUpdate(string message)` helper (travel equivalent of `RecordCaseUpdate`) — 1 new AddLogEntry call site
- Remove 13 direct `AddLogEntry(GameLogEntryKind.Travel, ...)` call sites from travel methods
- Decouple `Clock.AdvanceTravelDay()` → event-sourced via `TravelDayAdvanced`
- **Replay-equality tests:** prove command-path == replay-path for exact fields (wallet, health, food, journey state, clock, version) — following `BountySaloonEventSourcingTests` pattern
- **Snapshot immutability proof test:** verify that `TravelJourneySnapshot` nested objects are immutable (defensive test against future regression)
- Update `AddLogEntryGuardrailTests` constant (19 → 7: 1 definition + 5 remaining non-travel call sites + 1 RecordTravelUpdate call)
- TDD: failing event production/Apply/replay tests first, then implement, then verify

### Phase 3: Projections + Persistence + Handlers + ADR + Validation
- Add travel event cases to `DiaryProjector` (6 new cases) and `HudProjector` (health/wallet/town changes from travel events)
- Register 6 new event types in `GameSessionJsonSerializer.Events.cs` deserializer
- Migrate 4 handlers to `GameSessionCommandHandler` base class
  - **CRITICAL:** `TravelToTownHandler` must move `TravelResolver.PreviewJourney` INSIDE the `ExecuteWithRetryAsync` lambda — preview depends on mutable session state (inventory, current town) and must be regenerated on retry
  - Add concurrency retry test for `TravelToTownHandler` proving preview is regenerated on retry
- Add travel-specific event persistence + replay tests (following `EventSourcingEndToEndTests` pattern) with exact field equality
- Add hidden-truth boundary tests for travel events — verify hidden state field names do not appear in projection output or serialized DTOs (hidden state IS intentionally present in internal event JSON for replay correctness)
- Update ADR-0028 (search for exact path at execution time — likely `docs/adr/ADR-0028-*.md`)
- Full validation: `dotnet build`, `dotnet test`, `.\scripts\postgres-dev.ps1 validate`

---

## File-Touch Forecast by Layer

### Domain (src/WildBunch.Domain)
- **Create:** `Events/JourneyStarted.cs`, `Events/TravelDayAdvanced.cs`, `Events/TrailEventApplied.cs`, `Events/JourneyEncounterResolved.cs`, `Events/JourneyCompleted.cs`, `Events/JourneyArrivalAcknowledged.cs`
- **Create:** `Game/TravelDayOutcome.cs` (enum: Ongoing, Interrupted, Completed)
- **No new snapshot type needed** — `TravelJourneySnapshot` already exists at `Travel/TravelRouteModels.cs:82`
- **No new `FromSnapshot`/`ToSnapshot` needed** — already exist on `TravelJourney`
- **No new `Clock.Set` needed** — already exists
- **Modify:** `Game/GameSession.cs` (8 method migrations + 6 Apply methods + `RecordTravelUpdate` helper + remove 13 AddLogEntry sites + decouple Clock.AdvanceTravelDay + register events in `ApplyProducedEvent`)
- **Modify:** `Game/GameSessionEventReplay.cs` (6 new dispatch cases in `ApplyEvent`)

### Application (src/WildBunch.Application)
- **Modify:** `Projections/DiaryProjector.cs` (6 new event cases + travel day tracking)
- **Modify:** `Projections/HudProjector.cs` (health/wallet/town/inventory changes from travel events)
- **Modify:** `Games/Commands/TravelToTownHandler.cs` (migrate to `GameSessionCommandHandler` base, move preview inside retry boundary)
- **Modify:** `Games/Commands/AdvanceTravelDayHandler.cs` (migrate to `GameSessionCommandHandler` base)
- **Modify:** `Games/Commands/ResolveJourneyEncounterHandler.cs` (migrate to `GameSessionCommandHandler` base)
- **Modify:** `Games/Commands/AcknowledgeJourneyArrivalHandler.cs` (migrate to `GameSessionCommandHandler` base)

### Persistence (src/WildBunch.Persistence)
- **Modify:** `Serialization/GameSessionJsonSerializer.Events.cs` (6 new cases in `ResolveEventType` switch)

### Tests
- **Create:** Domain characterization tests (Phase 1) — exact value assertions with ForcedRoll
- **Create:** Domain event Apply + replay-equality tests (Phase 2) — command-path == replay-path
- **Create:** Application projection tests (Phase 3) — DiaryProjector + HudProjector travel cases
- **Create:** Integration travel event persistence + replay tests (Phase 3) — PostgreSQL-backed
- **Create:** Hidden-truth boundary tests (Phase 3) — projection output and DTO verification (not event JSON — hidden state is intentionally present in internal events)
- **Create:** Handler concurrency retry test (Phase 3) — TravelToTownHandler preview regeneration
- **Modify:** `AddLogEntryGuardrailTests.cs` (constant 19 → 7)

### Docs
- **Modify:** ADR-0028 (search for exact path at execution time)
