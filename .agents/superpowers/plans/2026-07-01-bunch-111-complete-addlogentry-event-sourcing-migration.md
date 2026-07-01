# BUNCH-111: Complete AddLogEntry Event-Sourcing Migration — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove all remaining `[Obsolete] AddLogEntry` call sites from `GameSession` so that aggregate `_logEntries` is no longer populated by any Apply method, then remove `AddLogEntry`, `RecordCaseUpdate`, `RecordTravelUpdate`, the `_logEntries` field, the `LogEntries` property, and the snapshot `LogEntries` field — completing the event-sourcing migration begun in BUNCH-78/80/83/84/86.

**Architecture:** All player-facing log/journal reads already derive from the typed domain event stream via `JournalLogProjector` (BUNCH-84/86). The remaining `AddLogEntry`/`RecordCaseUpdate`/`RecordTravelUpdate` calls inside `Apply(...)` methods only populate the internal `_logEntries` list, which is no longer read by any live path. Removing them is safe because: (1) `JournalLogProjector` already reproduces the exact same `GameLogEntry` sequence from the event stream; (2) the snapshot `LogEntries` field is only used for rehydrating `_logEntries` during snapshot load, and post-snapshot events replay through `Apply` which would re-append — but since nothing reads `_logEntries`, both the snapshot field and the Apply-side appends are dead weight; (3) `RehydrateFromEvents` populates `_logEntries` via Apply, but no replay-equality test reads anything other than `.Count` (which will still match because both command and replay paths stop appending).

**Tech Stack:** C#/.NET 8, xUnit, EF Core, JSON snapshot persistence.

## Global Constraints

- `GameSession` is the live-play aggregate root; gameplay mutations flow through `Apply` methods.
- `AddLogEntry` is `[Obsolete]` projection-legacy per ADR-0028; no new call sites.
- `JournalLogProjector` is the projection-backed replacement for the `GameSessionLogEntries` table on read paths (BUNCH-84).
- All live player-facing read paths already project log entries from the event stream via `JournalLogProjector` / `GameSessionLogProjection.Project(session)` (BUNCH-86). No live path scrapes `session.LogEntries`.
- Snapshot persistence is JSON-oriented; `LogEntries` in the snapshot is dead internal surface (test-only serializer with no production callers per ADR-0028 BUNCH-86 note).
- Dev database drop/recreate is allowed when a snapshot shape changes.
- Do not add compatibility shims for obsolete old saves or internal models.
- When a task calls for replacement, fully replace the old internal model instead of layering a compatibility adapter over it.

---

## Current State (verified against origin/main @ 2136584)

### Remaining `AddLogEntry` call sites in `GameSession.cs` (4 call sites + 1 definition = 5 regex matches, guardrail count = 5)

1. **`Apply(GameStarted)`** at line 1155: `AddLogEntry(GameLogEntryKind.Opening, $"The hunt begins in {e.StartingTownName}.")`
2. **`Apply(StoreItemPurchased)`** at line 1213: `AddLogEntry(GameLogEntryKind.Purchase, $"Purchased {quantityLabel} for ${e.TotalPrice:0.00}.")`
3. **`RecordTravelUpdate`** at line 599: `AddLogEntry(GameLogEntryKind.Travel, message)` — called from 8 travel Apply methods (lines 612, 636, 637, 639, 687, 689, 711, 712, 728, 741)
4. **`RecordCaseUpdate`** at line 3749: `AddLogEntry(GameLogEntryKind.CaseUpdate, message)` — called from 3 Apply methods (lines 486, 511, 1235)
5. **`AddLogEntry` definition** at line 3874 (the `[Obsolete]` private method itself)

### Why the issue says "8" but the guardrail says "5"

The issue description says "8 remaining call sites" — this appears to be a pre-BUNCH-112 count or a count that included indirect call sites (RecordCaseUpdate + RecordTravelUpdate have multiple internal callers). The current verified state on `origin/main @ 2136584` is **4 direct `AddLogEntry` call sites + 1 definition = 5 regex matches**, matching the guardrail test's `KnownLegacyAddLogEntryCallSiteCount = 5`. The migration target is the same regardless: eliminate all `AddLogEntry` calls and the definition.

### Consumers of `_logEntries` / `LogEntries` (to be removed/updated)

| Location | Current use | Action |
| --- | --- | --- |
| `GameSession._logEntries` field (line 37) | Populated by `AddLogEntry` | Remove |
| `GameSession.LogEntries` property (line 180) | `[Obsolete]` read accessor | Remove |
| `GameSession.AddLogEntry` method (line 3874) | `[Obsolete]` private mutator | Remove |
| `GameSession.RecordCaseUpdate` method (line 3747) | Public, calls `AddLogEntry` | Remove |
| `GameSession.RecordTravelUpdate` method (line 596) | Private, calls `AddLogEntry` | Remove |
| `GameSessionJsonSerializer.SessionSnapshot.cs` — `LogEntries` field in snapshot record | Serialized/deserialized | Remove from snapshot record |
| `GameSessionJsonSerializer.Rehydration.cs` — `logEntries` parameter | Passed to `ReplaceLogEntries` | Remove parameter |
| `GameSessionRehydrator.ReplaceLogEntries` | Reflection-based field setter | Remove method + `LogEntriesField` |
| `EfGameSessionRepository.LoadStoreAsync` — projects snapshot-prefix events to `logEntries` | Populates `GameSessionStore.LogEntries` | Remove projection + `LogEntries` from `GameSessionStore` |
| `EfGameSessionRepository.ToAggregate` — passes `store.LogEntries` to `RehydrateGameSession` | Rehydrates `_logEntries` | Remove |
| `GameSessionReadStoreLoader` — `LogEntries` in `GameSessionStore` | Used in read model + journal snapshot | Remove (project on demand instead) |
| `GameSessionReadModel.LogEntries` field | Already projection-backed | Keep (it's already projection-derived, just change the source) |
| `GameSessionMapper.ToDto(DomainGameSession)` | Already uses `GameSessionLogProjection.Project(session)` | No change needed |
| `GameSessionMapper.ToDto(DomainGameSessionReadModel)` | Reads `session.LogEntries` (read model) | No change needed (read model field stays, source changes) |
| `JournalLogProjectorEquivalenceTests` | Compares `session.LogEntries` vs projected | Update to compare projected vs projected (or remove obsolete comparison) |
| `TravelReplayEqualityTests` | Asserts `commandSession.LogEntries.Count == replayed.LogEntries.Count` | Remove these assertions (no longer meaningful) |
| `AddLogEntryGuardrailTests` | Guards against new call sites | Remove (mission complete) |
| `#pragma warning disable CS0618` in `GameSession.cs` | Suppresses obsolete warning for AddLogEntry | Remove |
| `#pragma warning disable CS0618` in `EfGameSessionRepository.cs` | Suppresses obsolete warning for LogEntries | Remove |
| `#pragma warning disable CS0618` in `GameSessionReadStoreLoader.cs` | Suppresses obsolete warning for LogEntries | Remove |
| `#pragma warning disable CS0618` in `GameSessionJsonSerializer.SessionSnapshot.cs` | Suppresses obsolete warning for LogEntries | Remove |
| `#pragma warning disable CS0618` in `JournalMapper.cs` | Suppresses obsolete warning for LogEntries | Remove |
| `#pragma warning disable CS0618` in `GameSessionMapper.cs` | Suppresses obsolete warning for LogEntries | Remove |

### Tests that reference `LogEntries` (to be updated)

- `AddLogEntryGuardrailTests.cs` — remove entirely (mission complete)
- `JournalLogProjectorEquivalenceTests.cs` — update: compare projected-from-events vs projected-from-events (both paths now use projection; the test proves the projector handles the full event set, not that Apply matches projection)
- `TravelReplayEqualityTests.cs` — remove `LogEntries.Count` assertions (lines 97-102, 131-133)
- `GameSessionAggregateRootTests.cs` — update assertions that read `session.LogEntries`
- `ClockTurnCorrectionTests.cs` — update assertions that read `session.LogEntries`
- `BountySaloonEventSourcingTests.cs` — update assertions that read `session.LogEntries`
- `TravelDiaryCharacterizationTests.cs` — update assertions that read `session.LogEntries`
- `JournalResolverTests.cs` — update if it reads `session.LogEntries`
- Various integration/application tests that assert on log entry counts — switch to journal projection assertions

---

## File Structure

### Files to modify

| File | Responsibility |
| --- | --- |
| `src/WildBunch.Domain/Game/GameSession.cs` | Remove `_logEntries`, `LogEntries`, `AddLogEntry`, `RecordCaseUpdate`, `RecordTravelUpdate`, and all calls from Apply methods; remove `#pragma warning disable CS0618` |
| `src/WildBunch.Domain/Game/GameLogEntry.cs` | Remove `[Obsolete]`-related doc references to `AddLogEntry`/`LogEntries` (the record itself stays — `JournalLogProjector` and `GameSessionReadModel` still use it) |
| `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` | Remove `LogEntries` from snapshot record; remove `ReplaceLogEntries` call; remove `#pragma` |
| `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Rehydration.cs` | Remove `logEntries` parameter from `RehydrateGameSession` |
| `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs` | Remove `ReplaceLogEntries` method and `LogEntriesField` |
| `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` | Remove snapshot-prefix log projection, `LogEntries` from `GameSessionStore`, `logEntries` from `ToAggregate`; remove `#pragma` |
| `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs` | Remove `LogEntries` from `GameSessionStore`; project log entries on demand for read model and journal snapshot; remove `#pragma` |
| `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs` | Remove `#pragma` (already projects from events for domain session path) |
| `src/WildBunch.Application/Games/Mapping/JournalMapper.cs` | Remove `#pragma` |
| `src/WildBunch.Application/Projections/JournalLogProjector.cs` | Update doc comment referencing `AddLogEntry/RecordCaseUpdate/RecordTravelUpdate` |
| `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs` | Remove entirely |
| `tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs` | Update to remove `session.LogEntries` comparisons |
| `tests/WildBunch.Domain.Tests/TravelReplayEqualityTests.cs` | Remove `LogEntries.Count` assertions |
| `tests/WildBunch.Domain.Tests/GameSessionAggregateRootTests.cs` | Update `LogEntries` assertions |
| `tests/WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs` | Update `LogEntries` assertions |
| `tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs` | Update `LogEntries` assertions |
| `tests/WildBunch.Domain.Tests/TravelDiaryCharacterizationTests.cs` | Update `LogEntries` assertions |
| `tests/WildBunch.Domain.Tests/JournalResolverTests.cs` | Update if needed |
| `tests/WildBunch.Application.Tests/TestDoubles/InMemoryGameSessionRepository.cs` | Already uses `GameSessionLogProjection.Project(session)` — verify no `LogEntries` reads remain |
| Various integration tests | Update `LogEntries` assertions to use journal projection |

### Files NOT modified

- `src/WildBunch.Application/Games/Models/GameSessionReadModel.cs` — keeps `LogEntries` field (already projection-backed, it's a read model DTO)
- `src/WildBunch.Application/Games/Mapping/GameSessionLogProjection.cs` — unchanged (already the projection path)
- `src/WildBunch.Application/Projections/JournalLogProjector.cs` — logic unchanged (doc comment update only)
- `src/WildBunch.Domain/Events/*` — unchanged
- `src/WildBunch.Web/*` — unchanged (reads DTOs, not domain)

---

## Task 1: Remove AddLogEntry calls from Apply methods and delete the legacy log mutators

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`

**Interfaces:**
- Consumes: `JournalLogProjector` (already produces the same entries from events)
- Produces: `GameSession` with no `_logEntries` field, no `AddLogEntry`/`RecordCaseUpdate`/`RecordTravelUpdate` methods

**What to change in `GameSession.cs`:**

1. Remove the `AddLogEntry` call from `Apply(GameStarted e)` (line 1155)
2. Remove the `AddLogEntry` call from `Apply(StoreItemPurchased e)` (line 1213)
3. Remove the `RecordCaseUpdate(e.Message)` call from `Apply(InvestigationPerformed e)` (line 1235)
4. Remove the `RecordCaseUpdate(e.Message)` call from `Apply(SaloonPersonOfInterestSpotted e)` (line 486) — keep the `if (e.RecordLog)` guard removal too since the whole block is just the log call
5. Remove the `RecordCaseUpdate(e.Message)` call from `Apply(WantedSuspectConfronted e)` (line 511)
6. Remove all `RecordTravelUpdate(...)` calls from the 8 travel Apply methods:
   - `Apply(JourneyStarted e)` — line 612
   - `Apply(TravelDayAdvanced e)` — lines 636, 637, 639
   - `Apply(TrailEventApplied e)` — lines 687, 689
   - `Apply(JourneyEncounterResolved e)` — lines 711, 712
   - `Apply(JourneyCompleted e)` — line 728
   - `Apply(JourneyArrivalAcknowledged e)` — line 741
7. Remove the `RecordTravelUpdate` method definition (lines 590-600)
8. Remove the `RecordCaseUpdate` method definition (lines 3747-3750)
9. Remove the `AddLogEntry` method definition (lines 3873-3877)
10. Remove the `_logEntries` field declaration (line 37)
11. Remove the `LogEntries` property (line 179-180)
12. Remove the `#pragma warning disable CS0618` at line 18 and the comment block at lines 14-17
13. Update the comment at lines 22-24 to reflect that all flows are now event-sourced (no more "Direct-mutation flows" note — or update it to reflect the actual current state of direct-mutation vs event-sourced flows if any direct-mutation flows remain outside the log context)

- [ ] **Step 1: Remove all AddLogEntry/RecordCaseUpdate/RecordTravelUpdate calls from Apply methods**

Remove the 4 direct `AddLogEntry` calls and all `RecordCaseUpdate`/`RecordTravelUpdate` calls (which internally call `AddLogEntry`) from every `Apply(...)` method. Keep all other mutation logic in each Apply method intact.

- [ ] **Step 2: Remove the legacy log methods and field**

Remove `AddLogEntry`, `RecordCaseUpdate`, `RecordTravelUpdate` method definitions, the `_logEntries` field, the `LogEntries` property, and the `#pragma warning disable CS0618` / legacy comment block.

- [ ] **Step 3: Build to verify compilation errors are isolated to consumers**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: Build succeeds for Domain project (no internal references to removed members). Other projects will fail — those are fixed in subsequent tasks.

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-111: remove AddLogEntry calls and legacy log mutators from GameSession"
```

---

## Task 2: Remove LogEntries from persistence snapshot and rehydration

**Files:**
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Rehydration.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs`

**Interfaces:**
- Consumes: `GameSession` (no longer has `_logEntries` field)
- Produces: Snapshot serialization without `LogEntries` field; rehydration without log entry parameter

- [ ] **Step 1: Remove LogEntries from the snapshot record**

In `GameSessionJsonSerializer.SessionSnapshot.cs`:
- Remove the `IReadOnlyList<GameLogEntrySnapshot> LogEntries` parameter from the `GameSessionSnapshot` record
- Remove `session.LogEntries.Select(GameLogEntrySnapshot.FromDomain).ToArray()` from `FromDomain`
- Remove `GameSessionRehydrator.ReplaceLogEntries(session, LogEntries.Select(GameLogEntrySnapshot.ToDomain).ToArray())` from `ToDomain`
- Remove the `#pragma warning disable CS0618` and its comment (line 6-7)

- [ ] **Step 2: Remove logEntries parameter from RehydrateGameSession**

In `GameSessionJsonSerializer.Rehydration.cs`:
- Remove the `IReadOnlyList<GameLogEntry> logEntries` parameter from `RehydrateGameSession`
- Remove the `GameSessionRehydrator.ReplaceLogEntries(session, logEntries)` call

- [ ] **Step 3: Remove ReplaceLogEntries from GameSessionRehydrator**

In `GameSessionRehydrator.cs`:
- Remove the `LogEntriesField` static field
- Remove the `ReplaceLogEntries` method

- [ ] **Step 4: Build the Persistence project**

Run: `dotnet build src/WildBunch.Persistence/WildBunch.Persistence.csproj`
Expected: Build fails only on `EfGameSessionRepository` and `GameSessionReadStoreLoader` (fixed in Task 3)

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Rehydration.cs src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs
git commit -m "BUNCH-111: remove LogEntries from snapshot serialization and rehydration"
```

---

## Task 3: Remove LogEntries from EfGameSessionRepository and GameSessionReadStoreLoader

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`

**Interfaces:**
- Consumes: `GameSessionJsonSerializer.RehydrateGameSession` (no longer takes `logEntries`)
- Produces: Repository load path without `_logEntries` rehydration; read store loader that projects log entries on demand

- [ ] **Step 1: Remove LogEntries from EfGameSessionRepository**

In `EfGameSessionRepository.cs`:
- Remove the `#pragma warning disable CS0618` and its comment (lines 11-13)
- Remove the `_journalLogProjector` field (line 23) — no longer used in load path (read store loader will have its own)
- Remove the snapshot-prefix log projection block in `LoadStoreAsync` (lines 224-248): remove the `allStoredEvents` query, `allEvents` array, `snapshotEvents` projection, and `logEntries` variable. **Keep** the `allEvents` for `AllEvents` in `GameSessionStore` if `SetCommittedEvents` still needs them — check if `allEvents` is used elsewhere in the method.
- Remove `LogEntries` from the `GameSessionStore` record (line 498)
- Remove `logEntries` from the `GameSessionStore` construction (line 262)
- Remove `store.LogEntries` from the `RehydrateGameSession` call in `ToAggregate` (line 319)
- Keep the `allEvents` / `SetCommittedEvents` path if it's still needed for `AllEvents` (line 402). If `allEvents` was only used for log projection, remove it too and remove `AllEvents` from `GameSessionStore`.

**Important:** Verify whether `allEvents` / `SetCommittedEvents` is used for anything other than log projection. If `AllEvents` is used by `GameSessionLogProjection.Project(session)` in the command path, it must be retained. Check `GameSessionMapper.ToDto(DomainGameSession)` — it calls `GameSessionLogProjection.Project(session)` which reads `session.AllEvents`. So `AllEvents` and `SetCommittedEvents` must stay.

- [ ] **Step 2: Remove LogEntries from GameSessionReadStoreLoader**

In `GameSessionReadStoreLoader.cs`:
- Remove the `#pragma warning disable CS0618` and its comment (lines 11-12)
- Remove `LogEntries` from the `GameSessionStore` record (line 175)
- Remove `logEntries` from `GameSessionStore` construction (line 167)
- In `LoadGameSessionReadModelAsync`: project log entries on demand from `store.AllEvents` via `new JournalLogProjector().Project(store.AllEvents)` and pass to `GameSessionReadModel` constructor
- In `LoadJournalSnapshotAsync`: project log entries on demand from `store.AllEvents` via `new JournalLogProjector().Project(store.AllEvents)` and pass to `ApplySlice`
- Keep the `storedEvents` query and `domainEvents` array (needed for `AllEvents` and `DeriveStartFlowPhase`)

- [ ] **Step 3: Build the Persistence project**

Run: `dotnet build src/WildBunch.Persistence/WildBunch.Persistence.csproj`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs
git commit -m "BUNCH-111: remove LogEntries from repository load path, project on demand in read store loader"
```

---

## Task 4: Remove obsolete pragmas and update doc comments in Application layer

**Files:**
- Modify: `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`
- Modify: `src/WildBunch.Application/Games/Mapping/JournalMapper.cs`
- Modify: `src/WildBunch.Application/Projections/JournalLogProjector.cs`
- Modify: `src/WildBunch.Domain/Game/GameLogEntry.cs`

- [ ] **Step 1: Remove obsolete pragmas from mappers**

In `GameSessionMapper.cs`: Remove the `#pragma warning disable CS0618` and its comment block (lines 12-17). The mapper already projects from events for the domain-session path and reads the read-model's `LogEntries` (which is a plain field on `GameSessionReadModel`, not `[Obsolete]`).

In `JournalMapper.cs`: Remove the `#pragma warning disable CS0618` and its comment (lines 6-8). The mapper reads `snapshot.LogEntries` which comes from `JournalSnapshot` — check if `JournalSnapshot.LogEntries` is `[Obsolete]`. If not, the pragma is unnecessary. If it is, the field type is `IReadOnlyList<GameLogEntry>` and `GameLogEntry` itself is not `[Obsolete]` (only the `GameSession.LogEntries` property and `AddLogEntry` method are). So the pragma can be removed.

- [ ] **Step 2: Update doc comments**

In `JournalLogProjector.cs`: Update the doc comment (lines 7-9) to remove the reference to `AddLogEntry/RecordCaseUpdate/RecordTravelUpdate` since those methods no longer exist. Replace with: "Pure projector that derives the legacy `GameLogEntry` sequence from the typed domain event stream. This is the projection-backed replacement for the legacy aggregate log entries. See ADR-0028 and BUNCH-84."

In `GameLogEntry.cs`: Update the doc comment (lines 8-10) to remove the reference to `GameSession.AddLogEntry` and `GameSession.LogEntries` since those no longer exist. Replace with: "This record is retained as the projection output type for `JournalLogProjector` and read-model DTOs; the authoritative source of game history is the typed domain event stream."

- [ ] **Step 3: Build the full solution**

Run: `dotnet build`
Expected: Build succeeds for all projects except tests that reference `session.LogEntries` (fixed in Task 5)

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs src/WildBunch.Application/Games/Mapping/JournalMapper.cs src/WildBunch.Application/Projections/JournalLogProjector.cs src/WildBunch.Domain/Game/GameLogEntry.cs
git commit -m "BUNCH-111: remove obsolete pragmas and update doc comments for completed migration"
```

---

## Task 5: Update and remove tests referencing LogEntries

**Files:**
- Remove: `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/TravelReplayEqualityTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/GameSessionAggregateRootTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/TravelDiaryCharacterizationTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/JournalResolverTests.cs` (if needed)
- Modify: `tests/WildBunch.Application.Tests/TestDoubles/InMemoryGameSessionRepository.cs` (if needed)
- Modify: Various integration/application tests that assert on `session.LogEntries`

**Strategy:**

For each test that reads `session.LogEntries`:
- If the test is proving projection equivalence (command path vs `JournalLogProjector`), change it to compare `JournalLogProjector.Project(events)` vs `JournalLogProjector.Project(events)` — but that's tautological. Instead, change these tests to assert the projected log entry count and content against expected fixed values (characterization tests), since the projector is already tested independently.
- If the test is a replay-equality test asserting `commandSession.LogEntries.Count == replayed.LogEntries.Count`, remove the assertion — it's no longer meaningful since neither session has `LogEntries`.
- If the test asserts specific log entry content via `session.LogEntries`, switch to asserting via `GameSessionLogProjection.Project(session)` or `new JournalLogProjector().Project(events)`.

- [ ] **Step 1: Remove AddLogEntryGuardrailTests.cs entirely**

The guardrail test's mission is complete — there are no more `AddLogEntry` call sites to guard against. Remove the file.

- [ ] **Step 2: Update JournalLogProjectorEquivalenceTests.cs**

Remove the `#pragma warning disable CS0618` blocks and `session.LogEntries` reads. Change each test to assert the projected log entries against expected fixed values (count, kind, message, day, turn) based on the known scenario. This converts equivalence tests into characterization tests for the projector.

- [ ] **Step 3: Update TravelReplayEqualityTests.cs**

Remove the `#pragma warning disable CS0618` blocks and `LogEntries.Count` assertions (lines 97-102, 131-133). The replay-equality tests still prove state equality for all other fields.

- [ ] **Step 4: Update remaining domain test files**

For each test in `GameSessionAggregateRootTests.cs`, `ClockTurnCorrectionTests.cs`, `BountySaloonEventSourcingTests.cs`, `TravelDiaryCharacterizationTests.cs`, `JournalResolverTests.cs` that reads `session.LogEntries`:
- Replace `session.LogEntries` with `GameSessionLogProjection.Project(session)` (requires adding `using WildBunch.Application.Games.Mapping;` — check if Domain.Tests can reference Application; the existing `JournalLogProjectorEquivalenceTests` already does `using WildBunch.Application.Projections;` so the project reference exists)
- Remove `#pragma warning disable CS0618` blocks

- [ ] **Step 5: Update application and integration tests**

For each test in `tests/WildBunch.Application.Tests/` and `tests/WildBunch.Integration.Tests/` that reads `session.LogEntries`:
- Replace with `GameSessionLogProjection.Project(session)` or assert on the DTO `LogEntries` from the handler response
- Remove `#pragma warning disable CS0618` blocks
- Update `InMemoryGameSessionRepository.cs` if it has any remaining `LogEntries` reads (it already uses `GameSessionLogProjection.Project(session)` — verify)

- [ ] **Step 6: Build the full solution**

Run: `dotnet build`
Expected: Build succeeds with no warnings about `LogEntries` or `AddLogEntry`

- [ ] **Step 7: Commit**

```bash
git add tests/
git commit -m "BUNCH-111: update tests to use projection instead of session.LogEntries, remove guardrail test"
```

---

## Task 6: Update ADR-0028 and regenerate index mesh

**Files:**
- Modify: `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`
- Modify: `tests/WildBunch.Application.Tests/INDEX.md` (regenerated)
- Modify: various `INDEX.md` files (regenerated)

- [ ] **Step 1: Add ADR-0028 dated status entry**

Add a new dated status entry to ADR-0028:
```
- 2026-07-01 - live (BUNCH-111): AddLogEntry/RecordCaseUpdate/RecordTravelUpdate migration complete. All remaining Apply-method log calls removed. `_logEntries` field, `LogEntries` property, `AddLogEntry`, `RecordCaseUpdate`, `RecordTravelUpdate` methods removed from GameSession. Snapshot `LogEntries` field removed from JSON serialization. `GameSessionRehydrator.ReplaceLogEntries` removed. Repository load path no longer rehydrates `_logEntries`. Read store loader projects log entries on demand from `AllEvents` via `JournalLogProjector`. `AddLogEntryGuardrailTests` removed (mission complete). All log/journal reads now flow exclusively through `JournalLogProjector` / `GameSessionLogProjection`. `GameLogEntry` record retained as projection output type.
```

- [ ] **Step 2: Regenerate index mesh**

Run: `python scripts/generate_index_mesh.py`
Expected: INDEX.md files updated to reflect removed `AddLogEntryGuardrailTests.cs`

- [ ] **Step 3: Commit**

```bash
git add docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md
git add **/INDEX.md
git commit -m "BUNCH-111: update ADR-0028 for completed migration, regenerate index mesh"
```

---

## Task 7: Full validation

- [ ] **Step 1: Run dotnet build**

Run: `dotnet build`
Expected: Build succeeds with zero warnings related to `AddLogEntry` or `LogEntries`

- [ ] **Step 2: Run dotnet test**

Run: `.\scripts\postgres-dev.ps1 ensure; dotnet test`
Expected: All tests pass

- [ ] **Step 3: Run EF migrations list**

Run: `dotnet tool restore; dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
Expected: No new migrations needed (no schema changes — only snapshot JSON shape changed)

- [ ] **Step 4: Run index mesh check**

Run: `python scripts/generate_index_mesh.py --check`
Expected: Passes (INDEX.md files match generator output)

- [ ] **Step 5: Verify no AddLogEntry references remain**

Search the entire `src/` tree for `AddLogEntry`, `RecordCaseUpdate`, `RecordTravelUpdate`, `_logEntries`, and `session.LogEntries` — confirm zero matches in production code (doc comments referencing historical context are acceptable if they describe the removal, not the usage).

- [ ] **Step 6: Final commit if any fixups needed**

If validation surfaces issues, fix and commit:
```bash
git add -A
git commit -m "BUNCH-111: validation fixups"
```

---

## Self-Review

### Spec coverage

- ✅ "Replace 8 remaining `[Obsolete] AddLogEntry` call sites" — Task 1 removes all 4 direct call sites + the 2 wrapper methods (RecordCaseUpdate, RecordTravelUpdate) that account for the remaining indirect call sites
- ✅ "Remove `[Obsolete]` attribute from AddLogEntry method" — Task 1 removes the entire method (stronger than just removing the attribute; the method has no callers after migration)
- ✅ "Run `dotnet test` to verify no regressions" — Task 7
- ✅ "Verify all AddLogEntry calls are migrated" — Task 7 Step 5

### Placeholder scan

No placeholders found. All steps have concrete file paths and specific changes.

### Type consistency

- `GameLogEntry` record retained throughout (used by `JournalLogProjector`, `GameSessionReadModel`, DTOs)
- `GameSessionLogProjection.Project(session)` is the consistent replacement for `session.LogEntries` in test assertions
- `JournalLogProjector.Project(events)` is the consistent projection method
- `GameSessionStore` record is updated consistently in both `EfGameSessionRepository` and `GameSessionReadStoreLoader`

### Risk notes

- **Snapshot compatibility:** Removing `LogEntries` from the JSON snapshot means existing saved sessions will deserialize without that field. Since no live path reads `_logEntries` from the aggregate, this is safe. The `GameLogEntrySnapshot` deserialization will simply not be called. Old snapshots with the field present will ignore it during deserialization (JSON deserialization ignores extra fields by default in System.Text.Json unless configured otherwise). Verify the serializer configuration tolerates extra fields.
- **`AllEvents` retention:** The `allEvents` / `SetCommittedEvents` path in `EfGameSessionRepository` must be retained because `GameSessionMapper.ToDto(DomainGameSession)` reads `session.AllEvents` via `GameSessionLogProjection.Project(session)`. Task 3 explicitly calls this out.
- **Test project references:** `WildBunch.Domain.Tests` already references `WildBunch.Application` (proven by `JournalLogProjectorEquivalenceTests` using `JournalLogProjector`), so using `GameSessionLogProjection.Project(session)` in domain tests is safe.
