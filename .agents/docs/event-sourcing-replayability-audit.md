# Event Sourcing Replayability Audit

Date: 2026-07-18

Audited by: Plan A, Task 6 (event sourcing integrity plan).

## Scope

This audit verifies that every piece of persisted state in the game session
aggregate is reconstructable from the event stream alone, per the
[event sourcing integrity policy](event-sourcing-integrity-policy.md) §Policy
Rule 1. The audit covers:

- 16 component names in `GameSessionComponentNames`
- 9 scalar fields on `GameSessionEntity`
- `TravelDiaryDays` (the known violation Plan B targets)

For each item, the audit asks: is it set by an `Apply` method from event
fields? Is it derivable by a projector from the event stream? If neither, it
is a violation.

## Source Files Examined

- `src/WildBunch.Persistence/GameSessions/GameSessionComponentNames.cs` — 16 component name constants
- `src/WildBunch.Persistence/GameSessions/GameSessionEntity.cs` — entity scalar fields + navigation collections
- `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` — `ToAggregate` (snapshot load + post-snapshot replay), `StoreAsync` (write path), `SyncDiaryDaysAsync`
- `src/WildBunch.Domain/Game/GameSession.cs` — all `Apply` methods, constructor, `RestoreBountyLoopState`, `BuildUnrelatedCriminalLedger`
- `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` — `RehydrateFromEvents` (full event replay factory), `ApplyEvent` dispatcher
- `src/WildBunch.Domain/Game/JourneyLoop.cs` — journey-owned `Apply` methods, `AppendTravelDiaryDay`, `PersistLatestTravelDiaryDay`, `RestoreTravelDiaryDays`
- `src/WildBunch.Domain/Game/BountyLoop.cs` — bounty-owned `Apply` methods, `RestoreUnrelatedCriminalLedger`
- `src/WildBunch.Domain/Game/ActionContextTracker.cs` — `Apply(TownActionContextEntered)`, `RestoreState`
- `src/WildBunch.Domain/Cases/UnrelatedCriminalLedger.cs` — ledger state and mutation methods
- `src/WildBunch.Domain/Travel/TravelDiaryModels.cs` — `TravelDiaryDayState` record shape
- `src/WildBunch.Domain/Travel/TravelDiaryDayFactory.cs` — `TravelDiaryDayFactory.Create` (command-path synthesis)

## Component Names (16)

All 16 component names in `GameSessionComponentNames` are reconstructable from
the event stream via `Apply` methods. No violations found.

| # | Component | Replay Path | Status |
|---|-----------|------------|--------|
| 1 | `Player` | `Apply(GameStarted)` sets Player from event fields; `Apply(StoreItemPurchased)` mutates wallet/inventory; `Apply(TravelDayAdvanced)` / `Apply(TrailEventApplied)` / `Apply(JourneyEncounterResolved)` sync player from journey snapshot; `Apply(PlayerSetupCompleted)` sets name | ✓ |
| 2 | `World` | `Apply(WorldGenerated)` sets `World = e.World.ToDomain()` | ✓ |
| 3 | `CaseFile` | `Apply(WorldGenerated)` sets CaseFile; `Apply(CaseFileGenerated)` sets CaseFile; `Apply(InvestigationPerformed)` reveals clues/warrants; `Apply(WantedSuspectConfronted)` / `Apply(SheriffTurnInSettled)` record confrontation/settlement state | ✓ |
| 4 | `Clock` | `Apply(GameStarted)` does not set clock directly, but `Apply(TownActionContextEntered)` calls `Clock.Set(e.Day, e.Turn)`; `Apply(TravelDayAdvanced)` calls `Clock.Set(e.Day, turn: 0)` | ✓ |
| 5 | `PursuitState` | `Apply(TownActionContextEntered)` calls `PursuitState.SetHeat(e.PursuitHeat)`; `Apply(JourneyStarted)` / `Apply(TravelDayAdvanced)` / `Apply(TrailEventApplied)` / `Apply(JourneyEncounterResolved)` all call `PursuitState.SetHeat(...)` from event fields | ✓ |
| 6 | `Setup` | `Apply(GameStarted)` sets `GameEntropy = e.GameEntropy`; `Apply(PlayerSetupCompleted)` sets `GameEntropy`; `Apply(WorldGenerated)` sets `GameEntropy`; `Apply(DevEntropyChanged)` sets `GameEntropy` | ✓ |
| 7 | `SaltSource` | `Apply(GameStarted)` sets `SaltSource = e.SaltSource`; `Apply(WorldGenerated)` sets `SaltSource`; `Apply(DevSaltSourceForced)` / `Apply(DevSaltSourceCleared)` set/clear | ✓ |
| 8 | `TownVisitState` | `Apply(TownActionContextEntered)` enters town context; `Apply(JourneyCompleted)` calls `RefreshTownVisit(e.DestinationTownId)`; `Apply(SaloonPersonOfInterestSpotted)` / `Apply(SaloonPersonOfInterestConfronted)` mutate town visit state; `Apply(InvestigationPerformed)` checks sources | ✓ |
| 9 | `Journey` | `Apply(JourneyStarted)` / `Apply(TravelDayAdvanced)` / `Apply(TrailEventApplied)` / `Apply(JourneyEncounterResolved)` / `Apply(JourneyCompleted)` all set journey from `e.JourneySnapshot` (ABSOLUTE); `Apply(JourneyArrivalAcknowledged)` clears journey | ✓ |
| 10 | `CompletedJourneyHistory` | `Apply(JourneyArrivalAcknowledged)` adds `e.JourneySnapshot` to completed history and clears active journey | ✓ |
| 11 | `WantedSuspectPresenceLedger` | Constructor starts with empty ledger (in `RehydrateFromEvents`); `Apply(WantedSuspectConfronted)` delegates to `BountyLoop.Apply(e)` which calls `UpdateWantedSuspectPresence` → `_presenceLedger.SetState(...)` | ✓ |
| 12 | `CurrentActionContext` | `Apply(TownActionContextEntered)` delegates to `ActionContextTracker.Apply(e)` which sets context + town ID | ✓ |
| 13 | `PendingDevTravelOverride` | `Apply(DevTravelOverrideForced)` / `Apply(DevTravelOverrideCleared)` / `Apply(DevTravelOverrideConsumed)` delegate to `JourneyLoop.Apply(e)` which sets/clears `_pendingDevTravelOverride` | ✓ |
| 14 | `PendingDevSaloonOverride` | `Apply(DevSaloonOverrideForced)` / `Apply(DevSaloonOverrideCleared)` / `Apply(DevSaloonOverrideConsumed)` delegate to `BountyLoop.Apply(e)` which sets/clears `_pendingDevSaloonOverride` | ✓ |
| 15 | `DevLayoutSalts` | `Apply(DevLayoutSaltsForced)` sets `DevLayoutSalts = e.DevLayoutSalts` | ✓ |
| 16 | `UnrelatedCriminalLedger` | See dedicated section below — **concern found** | ⚠ |

### Component 16: UnrelatedCriminalLedger — Concern

The brief marks this component ✓, describing it as "rebuilt from
`CaseFileGenerated` + gang roster; `RestoreBountyLoopState` handles snapshot
restore, `Apply(SheriffTurnInSettled)` / `Apply(UnrelatedCriminalTurnInSettled)`
handle event replay."

The mutation half of this claim is correct:
`BountyLoop.Apply(SheriffTurnInSettled)` calls
`_unrelatedCriminalLedger.RecordGangMemberTakenIn()`, and
`BountyLoop.Apply(UnrelatedCriminalTurnInSettled)` calls
`MarkWarrantCollected` + `RecordTakenIn`. These are replay-safe.

However, the **initial state** of the ledger is NOT set by any `Apply` method.
It is built once in the `GameSession` constructor via
`BuildUnrelatedCriminalLedger(caseFile)` (`GameSession.cs:92`), which reads
`caseFile.PublicWarrants` for the unrelated-criminal roster and
`caseFile.Suspects.Count` for the gang member count.

In `RehydrateFromEvents` (`GameSessionEventReplay.cs:82–103`), the constructor
is called with a **placeholder** `CaseFile` (empty suspects, empty warrants),
so `BuildUnrelatedCriminalLedger` returns a degenerate no-op ledger
(`gangMemberCount: 0, poolSize: 0`). When `Apply(WorldGenerated)` and
`Apply(CaseFileGenerated)` subsequently set the real `CaseFile`, **no code
rebuilds the ledger**. The mutation `Apply` methods are then silent no-ops on
the degenerate ledger (warrant IDs are not in the roster, gang count is zero).

The ledger is only correct in the snapshot-based load path (`ToAggregate`),
where the constructor receives the real `CaseFile` from the `CaseFile` component
and `RestoreBountyLoopState` overwrites the ledger with the persisted snapshot.

**Assessment:** The ledger's data IS present in the event stream
(`WorldGenerated`/`CaseFileGenerated` carry the full warrant roster and gang
size; `SheriffTurnInSettled`/`UnrelatedCriminalTurnInSettled` carry mutations).
The ledger is reconstructable from events in principle. The gap is that
`RehydrateFromEvents` does not rebuild the ledger when `Apply(WorldGenerated)`
or `Apply(CaseFileGenerated)` sets the real `CaseFile`. A fix would rebuild
the ledger inside `Apply(CaseFileGenerated)` or `Apply(WorldGenerated)` (before
any `InvestigationPerformed` events shrink `PublicWarrants`).

**Severity:** Concern (not a hard violation of design, but an implementation
gap in the full-replay proof). The snapshot-based load path works correctly.
If Plan B's full replay equality test exercises `RehydrateFromEvents` for a
session with unrelated criminals, this gap will surface as a test failure.

**Recommended action:** Add to Plan B's scope — rebuild the
`UnrelatedCriminalLedger` inside `Apply(CaseFileGenerated)` or
`Apply(WorldGenerated)` so that `RehydrateFromEvents` produces the correct
ledger without relying on the snapshot.

## Entity Scalar Fields (9)

All scalar fields on `GameSessionEntity` are either domain state set by `Apply`
methods or repository metadata. No violations found.

| Field | Replay Path | Status |
|-------|------------|--------|
| `Id` | Set by repository on create; not domain state | ✓ (metadata) |
| `CreatedAtUtc` | Set by repository on create | ✓ (metadata) |
| `UpdatedAtUtc` | Set by repository on save | ✓ (metadata) |
| `Status` | `Apply(GameStarted)` sets `Status = Active`; `Apply(PlaythroughArchived)` sets `Status = Archived` | ✓ |
| `GameDifficulty` | `Apply(GameStarted)` sets `GameDifficulty`; `Apply(PlayerSetupCompleted)` sets it; `Apply(DevDifficultyForced)` sets it | ✓ |
| `SeedCode` | `Apply(GameStarted)` sets `SeedCode = e.SeedCode`; `Apply(PlayerSetupCompleted)` sets `SeedCode = e.SeedCode` | ✓ |
| `SchemaVersion` | Set by repository constant | ✓ (metadata) |
| `StreamVersion` | Set by repository for optimistic concurrency | ✓ (metadata) |
| `SnapshotVersion` | Set by repository for snapshot tracking | ✓ (metadata) |

## Known Violation: TravelDiaryDays

`TravelDiaryDayState` rows are **NOT reconstructable** from the event stream.
This is the known violation that Plan B targets.

### Evidence

1. **`Apply(JourneyStarted)` clears the diary** — `JourneyLoop.Apply(JourneyStarted)`
   (`JourneyLoop.cs:48`) calls `_travelDiaryDays.Clear()`. No `Apply` method
   populates the list.

2. **No `Apply` method creates `TravelDiaryDayState` entries.** The `ApplyEvent`
   dispatcher (`GameSessionEventReplay.cs:120–222`) handles all event types via
   `Apply` methods. None of the journey `Apply` methods
   (`TravelDayAdvanced`, `TrailEventApplied`, `JourneyEncounterResolved`,
   `JourneyCompleted`) call `TravelDiaryDayFactory.Create` or add to
   `_travelDiaryDays`.

3. **Diary days are created by command-path side effects.**
   `JourneyLoop.AppendTravelDiaryDay` (`JourneyLoop.cs:1239–1257`) and
   `JourneyLoop.PersistLatestTravelDiaryDay` (`JourneyLoop.cs:1259–1296`)
   call `TravelDiaryDayFactory.Create(...)` and add/replace entries in
   `_travelDiaryDays`. These are called from the command path
   (`AdvanceJourneyDay`, `ResolveJourneyEncounter`, etc.), not from `Apply`.

4. **`RehydrateFromEvents` produces empty `TravelDiaryDays`.** The constructor
   (`GameSession.cs:97`) creates a fresh `JourneyLoop` with an empty
   `_travelDiaryDays` list. No `Apply` method adds to it. After full event
   replay, `TravelDiaryDays` is empty (or cleared by `Apply(JourneyStarted)`).

5. **The repository persists diary days as a separate table.**
   `SyncDiaryDaysAsync` (`EfGameSessionRepository.cs:448–483`) writes
   `TravelDiaryDayState` rows to `GameSessionDiaryDays`. On load,
   `LoadStoreAsync` reads them back and `ToAggregate` passes them to the
   constructor via `store.TravelDiaryDays`. This is a snapshot/projection path,
   not event replay.

### Why a projector is needed

The events (`TravelDayAdvanced`, `TrailEventApplied`, `JourneyEncounterResolved`,
`JourneyCompleted`) carry `JourneySnapshot` which contains much of the data
needed for a diary day (days travelled, origin/destination town names, travel
mode, status, remaining distance, horse state, warnings, etc.). However, the
`TravelDiaryDayState` also includes:

- **`StartingState`** (baseline travel mode, ride-day distance, days remaining,
  delay days, starting resources) — captured at the **start** of each day on
  the command path, not stored directly in any event.
- **`currentResources`** (health, wallet, food, horse feed, canteen charges,
  ammo, heat) — captured at the **end** of each day on the command path.
- **`Entries`** (diary messages) — generated during the command path; some are
  projected from the event stream via `JournalLogProjector`.

A projector can derive these by replaying the event stream in order, tracking
the "starting state" as the ending state of the previous day (or the
`JourneyStarted` snapshot for the first day), and computing resource snapshots
from the journey snapshots and player state carried in each event.

**Fix:** Plan B builds `TravelDiaryDayProjector` that rebuilds diary days from
the event stream.

## Additional Violations

### 1. UnrelatedCriminalLedger initial state (concern)

See the dedicated section under Component 16 above. The ledger's initial state
is not set by any `Apply` method — it is built in the constructor from the
`CaseFile`. In `RehydrateFromEvents`, the constructor receives a placeholder
`CaseFile`, producing a degenerate ledger. The mutation `Apply` methods are
then no-ops. The ledger is only correct via the snapshot restore path
(`RestoreBountyLoopState`).

**Recommended action:** Add to Plan B's scope — rebuild the ledger inside
`Apply(CaseFileGenerated)` or `Apply(WorldGenerated)`.

No other additional violations found.

## Summary

| Category | Count | Violations |
|----------|-------|------------|
| Component names (16) | 16 | 1 concern (UnrelatedCriminalLedger initial state) |
| Entity scalar fields (9) | 9 | 0 |
| TravelDiaryDays | 1 | 1 known violation (Plan B fixes via TravelDiaryDayProjector) |

**Total violations:** 1 known (TravelDiaryDays) + 1 concern
(UnrelatedCriminalLedger initial state in `RehydrateFromEvents`).
