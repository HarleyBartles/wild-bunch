# BUNCH-80: Finish Architecture Stack Refactor — Campaign Overview

> **For agentic workers:** This is the master overview for BUNCH-80. Phase plans are in separate documents:
> - Phase 1: `2026-06-23-bunch-80-phase1-events-and-apply.md` — Clock/turn correction + events + Apply + domain method migration
> - Phase 3: `2026-06-23-bunch-80-phase3-projections-persistence-handlers.md` — Projections + persistence + handlers + DTO/frontend + tests

**Goal:** Migrate the bounty/saloon gameplay seam to typed domain events + `Apply` mutation + replay + projections + handler orchestration, AND decouple clock advancement from case-file recordkeeping by introducing a `TimeOfDay`-named turn model and action-context-based turn advancement. This brings the architecture stack closer to complete and leaves exactly two bounded follow-up issues (travel/journey event migration and legacy-log/UI projection deprecation).

**Base:** `main` at `4744853` (BUNCH-78 merged, PR #96). Clean worktree.

**Approval gate:** Awaiting Harley approval before execution. Do not start implementation until approved.

---

## Preflight Summary (from current `main`, verified against source)

### A. Architecture completion map

**A1. Event-sourced flows (typed events + Apply):**
- Start New Game → `GameStarted` → `Apply(GameStarted)` at `GameSession.cs:236`
- Purchase Store Item → `StoreItemPurchased` → `Apply(StoreItemPurchased)` at `GameSession.cs:256`
- 5 Investigation methods → `InvestigationPerformed` → `Apply(InvestigationPerformed)` at `GameSession.cs:270`

**A2. Handlers using GameSessionCommandHandler orchestration (7):**
`StartNewGameHandler`, `PurchaseStoreItemHandler`, `ReadWantedPostersHandler`, `InspectNoticeBoardHandler`, `CheckSheriffRecordsHandler`, `FollowTelegraphLeadsHandler`, `GatherLocalGossipHandler`. Base class at `GameSessionCommandHandler.cs:19-99`.

**A3. Flows still mutating directly (non-migrated):**
- Travel/journey: 12+ `AddLogEntry` sites across `StartJourney`, `AdvanceJourneyDay`, `ResolveJourneyEncounter`, `HandleInterruptedTravelDay`, `HandleCompletedTravelDay`, `HandleOngoingTravelDay` (`GameSession.cs:220-1444`)
- Bounty/saloon: `RecordCaseUpdate` at `GameSession.cs:1763,1771` (LookAroundSaloon); `RecordCaseUpdate` at `GameSession.BountyLoopCoordinator.cs:244,294` (ResolveWantedSuspectConfrontation, SettleSheriffTurnIn)
- Purchase legacy log: `AddLogEntry` at `GameSession.cs:1654`
- Case completion: `AddLogEntry` at `GameSession.cs:2032`
- `AddLogEntry` is `[Obsolete]` at `GameSession.cs:2156`. Guardrail test prevents new sites.

**A4. Event types registered in persistence deserializer (3):**
`GameStarted`, `StoreItemPurchased`, `InvestigationPerformed` at `GameSessionJsonSerializer.Events.cs:36-38`.

**A5. Safe projections (4 projectors):**
- `HudProjector` (player-facing) — `Projections/HudProjector.cs:14`
- `DiaryProjector` (player-facing) — `Projections/DiaryProjector.cs:13`
- `CaseFileViewProjector` (developer, not exposed) — `Projections/CaseFileViewProjector.cs:14`
- `FullAuditProjector` (developer, not exposed) — `Projections/FullAuditProjector.cs:11`
- Player-facing endpoints: `GET /api/games/{id}/projections/hud` and `/diary` at `ProjectionEndpoints.cs:18,23`

**A6. DTOs/frontend surfaces exposing LogEntries:**
- Backend: `GameSessionDto.LogEntries` (`GameDtos.cs:24`), `JournalDto.LogEntries` (`JournalDto.cs:11`), `GameSessionReadModel.LogEntries` (`GameSessionReadModel.cs:22`)
- Frontend: `types.ts:479,528`, `LogPanel.tsx`, `FieldReportPanel.tsx:77`, `DebugCockpitRoute.tsx:124`, `GlobalOverlays.tsx:70`

**A7. ADR-0028 status:**
- File: `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`, status `live`
- Implemented: typed events, Apply mutation, replay, optimistic concurrency, snapshot-as-cache, projection taxonomy, HUD/diary player-facing endpoints, hidden-truth boundaries, GameLogEntry demotion
- Future: `LegacyLogProjector` (§12, not implemented), non-migrated flow migration (§13,§148-152), full replay-from-events production path (§85,§148), drop LogEntries from command responses (§10,§98)

### B. Travel/journey seam

- 8 core methods + 3 helpers, 4 handlers (`TravelToTownHandler`, `AdvanceTravelDayHandler`, `ResolveJourneyEncounterHandler`, `AcknowledgeJourneyArrivalHandler`)
- 8 state aggregates mutated: journey, diary days, player resources, horse, wallet, inventory, ammo, health, heat, pursuit, clock, town entry
- 0 travel-specific events exist
- **Risk: HIGH.** Multi-day state machine, 12+ AddLogEntry sites, encounter resolution with hidden state (bribe lockout, chase fatigue, annoyance, shaken), entropy/seed dependence (`TravelDayPlanGenerator` uses SHA256(seed)), complex `ContinueCurrentDayAfterEncounterResolution` loop
- Safe parts: `StartJourney`, `AcknowledgeJourneyArrival` (low risk, but not substantial alone)
- Moderate: `AdvanceJourneyDay`, `ResolveJourneyEncounter`
- Too risky for this campaign: `ContinueCurrentDayAfterEncounterResolution`, `ApplyTrailEvent`
- Existing tests: 50+ across `TravelResolverTests`, `AdvanceTravelDayHandlerTests`, `TravelToTownHandlerTests`, etc. Gaps in state machine, replay, determinism, edge cases

### C. Bounty/saloon seam

- 5 mutating methods: `LookAroundSaloon`, `ConfrontSaloonPersonOfInterest`, `ConfrontSaloonWantedSuspect`, `ResolveWantedSuspectConfrontation`, `SettleSheriffTurnIn`
- 1 non-mutating method: `AssessSheriffTurnIn` (pure validation, no migration needed)
- 5 handlers: `LookAroundSaloonHandler`, `ConfrontSaloonPersonOfInterestHandler`, `ConfrontSaloonWantedSuspectHandler`, `ConfrontWantedSuspectHandler`, `TurnInToSheriffHandler`
- All owned by `BountyLoopCoordinator` (`GameSession.BountyLoopCoordinator.cs`) except `LookAroundSaloon` (direct in GameSession)
- Hidden truth boundary well-enforced: `TrueCulpritId`, `LinkedSuspectIds`, `TargetKind`, `KillerReleaseState` never in DTOs. `GameApiHiddenTruthTests` validates no leakage.
- `ConfrontSaloonPersonOfInterest` is composite: may call `ResolveWantedSuspectConfrontation` + `SettleSheriffTurnIn` internally (armed+correct path produces 2 events)
- Existing tests: `GameSessionBountyLoopCoordinatorTests`, `GameSessionWantedSuspectConfrontationTests`, `GameSessionSheriffTurnInTests`, `GameSessionSaloonWantedSuspectLoopTests`, `ConfrontWantedSuspectHandlerTests`, `ConfrontSaloonWantedSuspectHandlerTests`, `TurnInToSheriffHandlerTests`, `SaloonConfrontationAcceptanceTests`, `BountySettlementPolicyTests`, `BountyDeclarationMatchPolicyTests`

### D. Legacy log and projection migration

- 18 remaining `AddLogEntry` call sites: 12+ travel, 2 bounty coordinator, 1 purchase, 1 case completion, 1 investigation (transitional bridge in `Apply(InvestigationPerformed)`)
- `LegacyLogProjector`: NOT implemented (ADR-0028 §12 gap). Not needed until all flows are event-sourced and UI migrates to projections.
- `LogEntries` cannot be removed yet: 4 API endpoints expose it, 4 UI components consume it. Travel flow has no projection coverage.
- Hard dependencies: `GameLogEntry`, `AddLogEntry`, `GameSessionDto.LogEntries`, `JournalDto.LogEntries`, persistence `LogEntries` sync

### E. Clock/turn model and RecordCaseUpdate coupling

**E1. GameClock type** (`GameClock.cs:3-25`):
- `Day` (int, starts at 1), `Turn` (int, 0-3)
- `Advance()`: increments Turn, wraps at 4 to next Day (line 9-18)
- `AdvanceTravelDay()`: increments Day, resets Turn to 0 (line 20-24)
- No `TimeOfDay` enum, no turn names, no `Morning`/`Afternoon`/`Evening`/`Night` anywhere in codebase

**E2. RecordCaseUpdate clock coupling** (`GameSession.cs:2019-2027`):
```csharp
public void RecordCaseUpdate(string message, bool advanceClock = true)
{
    if (advanceClock) { Clock.Advance(); }
    AddLogEntry(GameLogEntryKind.CaseUpdate, message);
}
```
- `advanceClock` defaults to `true` — case-file updates advance the clock by default
- `Clock.Advance()` is called from exactly ONE site: inside `RecordCaseUpdate` (line 2023)
- `Clock.AdvanceTravelDay()` is called from exactly ONE site: `AdvanceJourneyDay` (line 621)

**E3. All RecordCaseUpdate call sites (5 total):**

| File:Line | advanceClock | Flow | Path |
|-----------|-------------|------|------|
| `GameSession.cs:281` | `e.AdvanceClock` (default=true) | Investigation | `Apply(InvestigationPerformed)` — all 4 investigation methods set default true |
| `GameSession.cs:1763` | default=true | Saloon | `LookAroundSaloon` — repeat path |
| `GameSession.cs:1771` | default=true | Saloon | `LookAroundSaloon` — found suspect path |
| `BountyLoopCoordinator.cs:244` | default=true | Bounty | `ResolveWantedSuspectConfrontation` — Abandoned path |
| `BountyLoopCoordinator.cs:294` | default=true | Bounty | `ResolveWantedSuspectConfrontation` — Surrendered/Fled/Killed paths |

**E4. Paths that do NOT advance the clock:**
- `LookAroundSaloon` citizen path (no `RecordCaseUpdate` call, line 1775-1777)
- `ConfrontSaloonPersonOfInterest` citizen wrong-declaration (no `RecordCaseUpdate`)
- `ConfrontSaloonPersonOfInterest` wanted wrong-declaration (no `RecordCaseUpdate`)
- `SettleSheriffTurnIn` (no `RecordCaseUpdate` — captures `Clock.Day`/`Clock.Turn` but doesn't advance)
- All rejection paths (no state change, no clock advance)

**E5. Town/context model:**
- `TownVisitState` (`TownVisitState.cs:7-114`) tracks `CurrentTownId` and per-town visit state (spent sources, saloon person)
- `TownAggregate` (`TownAggregate.cs:12-60`) wraps `Town` definition + `TownVisitState`
- Entering a town (`EnterTown`, `RefreshTownVisit`) does NOT advance the clock
- No "action context" or "current location within town" concept exists
- Player can call any investigation/bounty/saloon action directly from town — no "enter location" step

**E6. Clock consumers (DTO/frontend):**
- `GameClockDto(int Day, int Turn)` at `GameDtos.cs:290` — exposes numeric turn
- Frontend: `Hud.tsx:33` displays `Day ${session.clock.day}, Turn ${session.clock.turn}`
- Frontend: `DebugCockpitRoute.tsx:60` displays same
- Frontend: `CaseFileSurface.tsx:201-202,409` displays `Day ${journal.clock.day}, turn ${journal.clock.turn}`
- Tests: 100+ assertions on `Clock.Day`/`Clock.Turn` across domain, application, and integration tests

**E7. BountyLoopCoordinator Clock.Turn + 1 pattern** (`BountyLoopCoordinator.cs:261,271,279,295`):
- Confrontation state records `_session.Clock.Turn + 1` — this is because `RecordCaseUpdate` advances the clock BEFORE the state is recorded, so the state captures the NEXT turn number
- After decoupling, this `+1` pattern must be removed — the turn no longer advances from the record call

---

## Campaign Choice: Bounty/Saloon Event Migration + Clock/Turn Correction

### Selected slice

Two coupled changes in one PR:

1. **Bounty/saloon event migration** — migrate 5 mutating methods to typed domain events + `Apply` + replay + projections + handler orchestration
2. **Clock/turn correction** — decouple clock advancement from `RecordCaseUpdate`, rename the four turn slots to Morning/Afternoon/Evening/Night, introduce action-context-based turn advancement

These two changes are coupled because the bounty/saloon Apply methods are the natural place to stop calling `RecordCaseUpdate(advanceClock: true)` and instead let the action-context entry handle turn advancement. Doing them together avoids a double-touch of the same methods.

### Justification

Travel is the largest remaining seam but source inspection confirms it is NOT safe for a single campaign:
- Multi-day state machine with entangled encounter resolution
- Entropy/seed dependence in `TravelDayPlanGenerator`
- Hidden encounter state (bribe lockout, chase fatigue, annoyance, shaken)
- 12+ `AddLogEntry` sites across 8 methods
- `ContinueCurrentDayAfterEncounterResolution` is deeply coupled to day plan generation
- StartJourney and AcknowledgeJourneyArrival cannot be safely migrated as bookends — they depend on the full journey state machine and would create inconsistent event streams (source: `GameSession.cs:305-829`, `TravelJourney.cs:7-77`)

Bounty/saloon is the largest SAFE remaining seam:
- 5 mutating methods, all coordinator-owned (encapsulated logic)
- 4 clean candidate events following the established `InvestigationPerformed` pattern
- Hidden-truth boundary already well-tested (`GameApiHiddenTruthTests`)
- State mutations are bounded: wallet (fine/bounty), confrontation state, settlement state, presence state, saloon person state
- Composite operations produce multiple events per command — standard event sourcing
- Existing test coverage provides characterization baseline

Clock/turn correction is the natural second change because:
- All 5 `RecordCaseUpdate` call sites are in bounty/saloon and investigation flows being touched
- The `Clock.Turn + 1` pattern in `BountyLoopCoordinator` (lines 261,271,279,295) is a direct artifact of clock-coupled recordkeeping
- The citizen path in `LookAroundSaloon` already breaks the pattern (no clock advance) — the correction makes this consistent
- The `TimeOfDay` rename is a small additive change (derived enum + DTO field + frontend display)

### Rejected alternatives

1. **Full travel migration** — too broad, high risk of replay divergence, would require 7+ event types and complex seed/determinism handling. Better as a dedicated follow-up.
2. **Partial travel bookend (StartJourney + AcknowledgeJourneyArrival)** — source evidence proves unsafe: `AdvanceJourneyDay` depends on `Journey` being set by `StartJourney` (line 330), `AcknowledgeJourneyArrival` depends on `Journey.Status == Completed` (line 816). Migrating bookends alone creates inconsistent event streams that break replay.
3. **Legacy log deprecation alone** — can't remove `LogEntries` until travel flows also have projection coverage. `LogPanel.tsx:10` falls back to `sessionLogEntries`; travel flows have no projection. Premature without travel migration.
4. **LegacyLogProjector implementation** — feasible for migrated flows only, but `DiaryProjection` lacks a `Kind` field needed by `LogPanel.tsx:27` (`formatLogKind(entry.kind)`). Would require extending `DiaryEntry` and updating all projectors. Better as part of the dedicated legacy-log follow-up.

---

## Event Model

5 new typed domain events, all carrying only public data (no hidden culprit truth). **No event carries `AdvanceClock`** — clock advancement is handled by the `TownActionContextEntered` event, not by a boolean flag on gameplay events or by `RecordCaseUpdate`.

| Event | Produced By | Key Payload | Hidden-Truth Safe |
|-------|-------------|-------------|-------------------|
| `TownActionContextEntered` | `EnterActionContext` (called by investigation/bounty/saloon methods) | Context (TownActionContext), Day, Turn, TimeOfDay | Yes — context and time are public |
| `SaloonPersonOfInterestSpotted` | `LookAroundSaloon` | TownId, Message, SuspectId?, Descriptor?, PersonOfInterestKind?, RecordLog (bool — controls log entry only, NOT clock) | Yes — SuspectId is public; TrueCulpritId never in event |
| `WantedSuspectConfronted` | `ResolveWantedSuspectConfrontation` | TargetSuspectId, TargetName, Disposition, Choice, Outcome, IsAlive, IsSecured, Message, DeclaredWantedIdentityHandle? | Yes — carries public target name/disposition, not culprit truth |
| `SheriffTurnInSettled` | `SettleSheriffTurnIn` | TargetSuspectId, TargetName, Disposition, IsAlive, BountyAmount, Message, Day, Turn | Yes — bounty amount is public |
| `SaloonPersonOfInterestConfronted` | `ConfrontSaloonPersonOfInterest`, `ConfrontSaloonWantedSuspect` | Message, SuspectId?, TargetName, PersonOfInterestKind, Outcome, IsAlive?, IsSecured?, FineAmount?, WalletBefore?, WalletAfter?, DeclaredWantedIdentityHandle?, IsCitizen | Yes — citizen path has no SuspectId; wanted path carries only public name |

### Event ordering for a typical action

```
LookAroundSaloon (entering Saloon from None context):
  1. TownActionContextEntered(Saloon, Day=1, Turn=1, Afternoon)
  2. SaloonPersonOfInterestSpotted(suspect, "You spot a shady figure.")

ResolveWantedSuspectConfrontation (already in Saloon):
  1. WantedSuspectConfronted(suspect, Surrendered, ...)

SettleSheriffTurnIn (entering SheriffOffice from Saloon):
  1. TownActionContextEntered(SheriffOffice, Day=1, Turn=2, Evening)
  2. SheriffTurnInSettled(suspect, bounty=$50, Day=1, Turn=2)
```

### Composite event production

`ConfrontSaloonPersonOfInterest` may produce 0-3 events per call (plus context events from delegated calls):
- Rejection without state change and without context entry → 0 events
- Rejection after context entry (e.g., rejected sheriff turn-in) → 1 `TownActionContextEntered` event (context changed, time passed, but no gameplay event)
- Citizen/wrong-declaration path → 1 `SaloonPersonOfInterestConfronted` event
- No-firearm wanted path → 1 `WantedSuspectConfronted` + 1 `SaloonPersonOfInterestConfronted` (clears saloon person)
- Armed+correct path → 1 `WantedSuspectConfronted` + 1 `SheriffTurnInSettled` + 1 `SaloonPersonOfInterestConfronted` (clears saloon person). The `SheriffTurnInSettled` is preceded by a `TownActionContextEntered(SheriffOffice)` event.
- Rejection clearing saloon person → 1 `SaloonPersonOfInterestConfronted` event (Outcome=Rejected)

---

## Clock/Turn Correction Model

### Problem

Today, `RecordCaseUpdate(message, advanceClock: true)` is the ONLY mechanism that advances the town clock (`GameSession.cs:2019-2027`). This couples case-file recordkeeping (a passive consequence of an action) to time progression (a gameplay resource). The citizen path in `LookAroundSaloon` already breaks this pattern by not calling `RecordCaseUpdate`, proving the coupling is fragile.

### First-version turn model

1. **Rename the four turn slots** to `Morning=0`, `Afternoon=1`, `Evening=2`, `Night=3` via a new `TimeOfDay` enum. `GameClock.Turn` stays as int (0-3) for persistence compatibility; `GameClock.TimeOfDay` is a derived property.

2. **Action-context-based turn advancement, event-sourced.** Introduce a `TownActionContext` enum (`None`, `SheriffOffice`, `Saloon`, `Store`, `Stable`, `Jail`, `TelegraphOffice`, `TownSquare`). The player enters a context when they perform an action tied to that context. Entering a NEW context (different from the current one) advances the turn. Staying in the same context does NOT advance the turn.

3. **`TownActionContextEntered` event — the replayable clock/context mutation.** When a context change occurs, the command method emits a `TownActionContextEntered` event carrying the new context and the resulting `Day`/`Turn`/`TimeOfDay`. `Apply(TownActionContextEntered)` sets `CurrentActionContext` and `Clock` from the event. This event appears in the uncommitted stream BEFORE any gameplay event for that action. During replay, `Apply(TownActionContextEntered)` reconstructs both the context and the clock state — no divergence between command execution and replay.

4. **Decouple `RecordCaseUpdate` from the clock.** Remove the `advanceClock` parameter. `RecordCaseUpdate` becomes a pure log-entry append — it never advances the clock. The `AddLogEntry` call inside it still captures `Clock.Day`/`Clock.Turn` for the log entry timestamp, but does not change them.

5. **Action methods call `EnterActionContext` after availability checks, before local action resolution.** Each investigation/bounty/saloon method maps its `InvestigationSourceKind` or action type to a `TownActionContext` and calls `EnterActionContext(mappedContext)` AFTER confirming the action context exists in the current town (e.g., saloon exists, source available) but BEFORE resolving the local action. If the context is the same as the current one, no event is produced and no turn advance occurs. If different, a `TownActionContextEntered` event is produced and the turn advances.

6. **Rejected actions that enter a context still produce the context event.** If a player goes to the sheriff's office and the turn-in is rejected, the `TownActionContextEntered(SheriffOffice)` event is still in the stream — the player went there, time passed. The rejection simply means no `SheriffTurnInSettled` event follows.

7. **`CurrentActionContext` is persisted and replayed.** It is stored in the session snapshot alongside `Clock` and reconstructed from event replay via `Apply(TownActionContextEntered)`. It does NOT reset to `None` after each load.

8. **Trail advancement stays day-level.** `Clock.AdvanceTravelDay()` (line 621) is unchanged — it increments Day and resets Turn to 0 (Morning). No intra-day trail event subsystem is implemented in this slice. Conceptually a trail day maps to the four named turn slots, but that mapping is not implemented now.

9. **No full time subsystem.** No scheduler, action-point economy, or opening-hours model. The `TimeOfDay` enum is a naming layer; the `TownActionContext` enum is a simple context tracker. Both are minimal.

### Event-sourced context entry flow

```
Command method:
  1. Check journey modal → reject (no context entry, no event)
  2. Check action context exists in town (e.g., saloon exists) → reject (no context entry, no event)
  3. EnterActionContext(mappedContext)
     → if same context: no event, no turn advance
     → if different context: compute new Day/Turn, emit TownActionContextEntered event, Apply sets context+clock
  4. Resolve local action → emit gameplay event(s)
  5. Return result

Replay:
  1. Apply(TownActionContextEntered) → sets CurrentActionContext + Clock from event
  2. Apply(gameplay event) → mutates game state, logs (no clock advance)
```

### Where clock coupling is removed or contained

| Site | Current behavior | New behavior |
|------|-----------------|--------------|
| `RecordCaseUpdate` (`GameSession.cs:2019-2027`) | Calls `Clock.Advance()` when `advanceClock=true` (default) | No `advanceClock` parameter. Never calls `Clock.Advance()`. Pure log append. |
| `Apply(InvestigationPerformed)` (`GameSession.cs:281`) | Calls `RecordCaseUpdate(e.Message, advanceClock: e.AdvanceClock)` | Calls `RecordCaseUpdate(e.Message)` — no clock advance. Clock advance happened via `TownActionContextEntered` event earlier in the stream. |
| `LookAroundSaloon` (`GameSession.cs:1763,1771`) | Calls `RecordCaseUpdate(msg)` with default `advanceClock=true` | After saloon-exists check, calls `EnterActionContext(Saloon)` which emits `TownActionContextEntered` event. `RecordCaseUpdate(msg)` in Apply just logs. |
| `ResolveWantedSuspectConfrontation` (`BountyLoopCoordinator.cs:244,294`) | Calls `RecordCaseUpdate(narration)` with default `advanceClock=true` | Already in Saloon context (from `LookAroundSaloon`). No context event. `RecordCaseUpdate(narration)` in Apply just logs. No turn advance. |
| `SettleSheriffTurnIn` (`BountyLoopCoordinator.cs:413-438`) | No `RecordCaseUpdate` call — captures `Clock.Day`/`Clock.Turn` | Calls `EnterActionContext(SheriffOffice)` which emits `TownActionContextEntered` event (turn advances if coming from Saloon). Assessment runs after. Rejected turn-ins still produce the context event. Captures `Clock.Day`/`Clock.Turn` after context entry. |
| `BountyLoopCoordinator.cs:261,271,279,295` | Records `Clock.Turn + 1` (because `RecordCaseUpdate` advanced the clock first) | Records `Clock.Turn` directly (no `+1` — clock no longer advances from recordkeeping; it advances from `TownActionContextEntered` which is already applied) |
| `InvestigationPerformed` event | Carries `AdvanceClock` field (default true) | `AdvanceClock` field REMOVED. Clock advance is not an event concern — it's in `TownActionContextEntered`. |
| All 4 investigation methods | Produce event with default `AdvanceClock=true`; Apply calls `RecordCaseUpdate(advanceClock: e.AdvanceClock)` | After source-availability check, call `EnterActionContext(mappedContext)` which emits `TownActionContextEntered`. Then produce `InvestigationPerformed` without `AdvanceClock`. Apply calls `RecordCaseUpdate(msg)` — no clock advance. |

### New source of turn advancement (all event-sourced)

- **Town actions:** `EnterActionContext(TownActionContext)` → emits `TownActionContextEntered` event → `Apply` sets `Clock` and `CurrentActionContext`
- **Travel:** `Clock.AdvanceTravelDay()` — unchanged, day-level only (travel migration is a follow-up; this stays as direct mutation for now)
- **Nothing else advances the clock**

### Tests that prove passive updates no longer advance the clock

1. `RecordCaseUpdate_DoesNotAdvanceClock` — call `RecordCaseUpdate("test")`, assert `Clock.Turn` unchanged
2. `EnterActionContext_SameContext_DoesNotProduceEvent` — enter Saloon, enter Saloon again, assert no new `TownActionContextEntered` event and turn unchanged
3. `EnterActionContext_DifferentContext_ProducesEventAndAdvancesTurn` — enter Saloon (event + turn advance), enter SheriffOffice (event + turn advance)
4. `Replay_TownActionContextEntered_ReconstructsContextAndClock` — replay events, verify `CurrentActionContext` and `Clock` match command-path state
5. `LookAroundSaloon_CitizenPath_AdvancesTurnViaContextEvent` — citizen path produces `TownActionContextEntered` event, proving clock advance is replayable
6. `ResolveWantedSuspectConfrontation_DoesNotAdvanceTurn_WhenAlreadyInSaloonContext` — no new context event, no turn advance
7. `SettleSheriffTurnIn_Rejected_StillProducesContextEvent` — rejected turn-in still has `TownActionContextEntered(SheriffOffice)` in stream
8. `SettleSheriffTurnIn_AdvancesTurn_WhenEnteringSheriffContextFromSaloon` — context event produced, turn advances
9. `InvestigationPerformed_Apply_DoesNotAdvanceClock` — Apply logs but does not advance; turn advance came from `TownActionContextEntered` earlier in stream
10. `Replay_FullBountySaloonFlow_ReconstructsClockAndContext` — full flow replay matches command-path clock/context state
11. `LookAroundSaloon_NoSaloonInTown_DoesNotProduceContextEvent` — availability check fails before context entry, no event, no turn advance

---

## File-Touch Forecast by Layer

### Domain (src/WildBunch.Domain)
- **Create:** `Events/TownActionContextEntered.cs`, `Events/SaloonPersonOfInterestSpotted.cs`, `Events/WantedSuspectConfronted.cs`, `Events/SheriffTurnInSettled.cs`, `Events/SaloonPersonOfInterestConfronted.cs`
- **Create:** `Game/TimeOfDay.cs` (enum: Morning=0, Afternoon=1, Evening=2, Night=3)
- **Create:** `Game/TownActionContext.cs` (enum: None, SheriffOffice, Saloon, Store, Stable, Jail, TelegraphOffice, TownSquare)
- **Modify:** `Game/GameClock.cs` (add `TimeOfDay` derived property + `Set(int day, int turn)` method for replay)
- **Modify:** `Game/GameSession.cs` (5 method migrations + 5 Apply methods + `EnterActionContext` method that emits event + `CurrentActionContext` persisted field + `RecordCaseUpdate` decoupling + investigation method context entry + remove `AdvanceClock` from `InvestigationPerformed` event construction)
- **Modify:** `Game/GameSession.BountyLoopCoordinator.cs` (coordinator method refactoring to produce events + remove `Clock.Turn + 1` pattern + context entry for `SettleSheriffTurnIn`)
- **Modify:** `Game/GameSessionEventReplay.cs` (5 new event dispatch cases including `TownActionContextEntered`)
- **Modify:** `Events/InvestigationPerformed.cs` (remove `AdvanceClock` field)

### Application (src/WildBunch.Application)
- **Modify:** `Projections/DiaryProjector.cs` (5 new event cases including `TownActionContextEntered` for time tracking + remove `AdvanceClock`-dependent turn tracking)
- **Modify:** `Projections/HudProjector.cs` (wallet/case changes + `TimeOfDay` in projection)
- **Modify:** `Projections/CaseFileViewProjector.cs` (confrontation/settlement state)
- **Modify:** `Games/Commands/LookAroundSaloonHandler.cs`, `ConfrontSaloonPersonOfInterestHandler.cs`, `ConfrontSaloonWantedSuspectHandler.cs`, `ConfrontWantedSuspectHandler.cs`, `TurnInToSheriffHandler.cs` (migrate to `GameSessionCommandHandler` base)
- **Modify:** `Games/Models/GameDtos.cs` (add `TimeOfDay` to `GameClockDto`)
- **Modify:** `Games/Mapping/GameSessionMapper.cs` (map `TimeOfDay` to DTO)
- **Modify:** `Games/Mapping/JournalMapper.cs` (map `TimeOfDay` to DTO)

### Persistence (src/WildBunch.Persistence)
- **Modify:** `Serialization/GameSessionJsonSerializer.Events.cs` (5 new event type registrations in `ResolveEventType` including `TownActionContextEntered`)
- **Modify:** `Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` (add `CurrentActionContext` to `GameSessionSnapshot` for snapshot persistence)
- **Modify:** `Serialization/GameSessionRehydrator.cs` (pass `CurrentActionContext` to `GameSession` constructor or set via backing field)
- **Modify:** `Serialization/GameSessionJsonSerializer.Components.cs` (`GameClockSnapshot` — add `TimeOfDay` if persisted, or derive from `Turn`)

### API (src/WildBunch.Api)
- No changes expected (existing endpoints return same DTO shapes; `TimeOfDay` is additive to `GameClockDto`)

### Web (src/WildBunch.Web)
- **Modify:** `src/api/types.ts` (add `timeOfDay` to `GameClockDto`)
- **Modify:** `src/shell/Hud.tsx` (display `TimeOfDay` name instead of `Turn N`)
- **Modify:** `src/routes/DebugCockpitRoute.tsx` (display `TimeOfDay` name)
- **Modify:** `src/components/CaseFileSurface.tsx` (display `TimeOfDay` name)
- **Modify:** test factories in `src/tests/test-utils/factories.ts` (add `timeOfDay` to clock fixtures)

### Tests
- **Create:** `tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs` (event production + Apply + replay)
- **Create:** `tests/WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs` (clock decoupling + context entry + TimeOfDay)
- **Modify:** existing bounty/saloon handler and domain tests for event assertions + clock behavior changes
- **Modify:** existing investigation tests for `AdvanceClock` removal + context entry behavior
- **Create/Modify:** `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs` (bounty/saloon event persistence + replay)
- **Modify:** `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs` (update guardrail count)
- **Modify:** tests asserting `Clock.Turn` values (100+ sites — update for context-based advancement)

### Docs
- **Modify:** `docs/adr/ADR-0028` (mark bounty/saloon as migrated, document clock/turn correction, update remaining-work notes)

---

## Tests to Write First

1. **Characterization tests** — verify current bounty/saloon behavior before refactor (extend existing `GameSessionBountyLoopCoordinatorTests`, `GameSessionWantedSuspectConfrontationTests`, `GameSessionSheriffTurnInTests`)
2. **Clock decoupling tests** — prove `RecordCaseUpdate` no longer advances clock, `EnterActionContext` does
3. **Event production tests** — each method produces correct event type(s) with correct payload (follow `InvestigationEventSourcingTests` pattern)
4. **Apply behavior tests** — each Apply method mutates state correctly without advancing clock
5. **Replay tests** — `RehydrateFromEvents` reconstructs same state as command path for bounty/saloon flows
6. **Persistence tests** — `GetEventStreamAsync` returns typed events after DB-backed store (follow `EventStorePersistenceTests` pattern)
7. **Projection tests** — DiaryProjector/HudProjector/CaseFileViewProjector produce correct output for bounty/saloon events
8. **Hidden-truth boundary tests** — verify no new hidden-state leakage through events or projections
9. **TimeOfDay display tests** — verify frontend displays named time-of-day instead of numeric turn

---

## Validation Plan

```powershell
# Build
dotnet build

# PostgreSQL-backed validation (required — persistence and integration surfaces change)
.\scripts\postgres-dev.ps1 validate

# Frontend build (required — frontend displays change for TimeOfDay)
cd src\WildBunch.Web
npm run build
```

---

## Non-Goals Confirmation

1. **No new persisted aggregate root** — `GameSession` remains the aggregate root; `BountyLoopCoordinator` stays an internal coordinator. ✓
2. **No live-session runtime-state table fan-out** — events go to existing `StoredEvents` table; snapshots remain JSON. ✓
3. **No raw events/payloads/audit/hidden truth exposed** — events are internal; player-facing APIs use projections only. ✓
4. **No LogEntries removal** — `LogEntries` stays for backward compatibility; projections are additive. ✓
5. **No broad rewrite without characterization tests** — characterization tests run first. ✓
6. **No PR/ADR overclaiming** — ADR updated to reflect only what is implemented. ✓
7. **No full time subsystem** — no scheduler, action-point economy, or opening-hours model. `TimeOfDay` is a naming layer; `TownActionContext` is a simple context tracker. ✓
8. **No intra-day trail event subsystem** — trail advancement stays day-level. Conceptual mapping to four turn slots is noted but not implemented. ✓
9. **No travel/journey migration** — travel stays on direct mutation. Migrated as a dedicated follow-up. ✓
10. **No legacy-log/UI projection deprecation** — `LogEntries` stays; `LegacyLogProjector` not implemented. Migrated as a dedicated follow-up. ✓

---

## Follow-Up Issue Outlines (exactly 2)

### Follow-up 1: Travel/Journey Event Migration

**Scope:** Migrate the complete travel/journey state machine to typed domain events + `Apply` + replay + projections. This is a substantial, bounded issue that must be done as a unit — source evidence proves bookend migration is unsafe (`StartJourney` at `GameSession.cs:305-328` creates `Journey`; `AdvanceJourneyDay` at `GameSession.cs:330-621` depends on `Journey` being set; `AcknowledgeJourneyArrival` at `GameSession.cs:809-829` depends on `Journey.Status == Completed`).

**Methods to migrate (8):**
- `StartJourney`, `AdvanceJourneyDay`, `HandleInterruptedTravelDay`, `HandleCompletedTravelDay`, `HandleOngoingTravelDay`, `ResolveJourneyEncounter`, `ContinueCurrentDayAfterEncounterResolution`, `AcknowledgeJourneyArrival`

**Candidate events (7):**
- `JourneyStarted`, `TravelDayAdvanced`, `JourneyEncounterInterrupted`, `JourneyEncounterResolved`, `TrailEventApplied`, `JourneyCompleted`, `JourneyArrivalAcknowledged`

**Key challenges:**
- Seed/determinism in `TravelDayPlanGenerator` (SHA256-based seed at `GameSession.cs:621`)
- Hidden encounter state (bribe lockout, chase fatigue, annoyance, shaken)
- `ContinueCurrentDayAfterEncounterResolution` loop (`GameSession.cs:1536+`)
- 12+ `AddLogEntry` sites across travel methods
- `Clock.AdvanceTravelDay()` coupling at `GameSession.cs:621`

**Required before refactor:**
- Characterization test suite covering: full state machine transitions, replay determinism, edge cases (blocked encounters, chase fatigue, horse lameness, ammo depletion)
- Travel-specific projection design (travel diary projection from events)

**Non-goals:**
- No new `TravelJourney` aggregate root (per architecture-hygiene.md)
- No instant multi-day travel
- No intra-day trail event subsystem (trail days remain day-level)

**Estimated size:** 7+ event types, 8 method migrations, 4 handler migrations, 2+ projector updates, travel-specific persistence/replay tests.

### Follow-up 2: Legacy Log/UI Projection Deprecation

**Scope:** Implement `LegacyLogProjector`, migrate frontend from `LogEntries` to projection-based output, and remove the legacy log infrastructure. This is a substantial, bounded issue that depends on BOTH bounty/saloon AND travel flows being event-sourced with projection coverage.

**Prerequisites:**
- BUNCH-80 (bounty/saloon + clock/turn) merged
- Follow-up 1 (travel/journey event migration) merged

**Steps:**
1. Extend `DiaryEntry` with a `Kind` field (or create `DiaryLogEntry` record) to carry `GameLogEntryKind` — needed by `LogPanel.tsx:27` (`formatLogKind(entry.kind)`)
2. Update `DiaryProjector` to track `Kind` from all event types
3. Implement `LegacyLogProjector` (ADR-0028 §12) — derives `GameLogEntry`-shaped rows from typed events for all migrated flows
4. Migrate `LogPanel.tsx` to consume `DiaryProjection` (or `LegacyLogProjector` output) instead of `JournalDto.LogEntries`
5. Migrate `FieldReportPanel.tsx:77` and `DebugCockpitRoute.tsx:124` to projection-based output
6. Remove `LogEntries` from `GameSessionDto` (`GameDtos.cs:24`), `JournalDto` (`JournalDto.cs:11`), `GameSessionReadModel` (`GameSessionReadModel.cs:22`)
7. Remove `AddLogEntry` method and `GameLogEntry` type from domain
8. Remove `GameSessionLogEntryEntity` table and related persistence code
9. Remove `RecordCaseUpdate` (now just a log append with no clock coupling — fully replaced by projections)

**Non-goals:**
- No raw event/audit exposure to player-facing APIs
- No change to projection endpoint shapes (`/projections/hud`, `/projections/diary`)

**Estimated size:** 1 new projector, 4+ frontend component migrations, 3 DTO field removals, 1 table drop, domain cleanup.

---

## Issue-Goal Conformance Notes

- BUNCH-78 was treated as merged historical context only. Current `main` at `4744853` was inspected directly.
- Preflight questions answered with source evidence (file:line citations in phase documents).
- Campaign slice is substantial: 5 method migrations, 5 events (including `TownActionContextEntered`), 5 Apply methods, clock/turn decoupling with `TimeOfDay` rename and event-sourced action-context model, 3 projector updates, 5 handler migrations, persistence/replay/projection tests, frontend display updates.
- Selected slice uses typed events and `Apply` as the mutation path.
- Clock advancement is decoupled from recordkeeping — `RecordCaseUpdate` no longer advances the clock; `EnterActionContext` does.
- Replay and persisted event-stream tests will prove migrated flows.
- Safe projection/read-model output covers player-facing needs (DiaryProjector, HudProjector).
- Handlers for migrated flows use `GameSessionCommandHandler` orchestration consistently.
- Remaining work converted into exactly 2 bounded, substantial follow-up issues.
- Validation separates tests/build success from issue-goal conformance.
- PR body and ADR will not overclaim beyond implemented source behavior.
