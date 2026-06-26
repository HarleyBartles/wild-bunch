# BUNCH-84: Projection-Backed Journal Output Cleanup — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the player-facing journal/log read output off the legacy `GameSessionLogEntries` table onto an event-stream projection, preserving player-facing behavior exactly.

**Architecture:** Introduce a `JournalLogProjector` (pure function over the typed domain event stream → `GameLogEntry[]`) that reproduces the exact legacy log entries the `Apply` methods currently produce via `AddLogEntry`/`RecordCaseUpdate`/`RecordTravelUpdate`. Switch the two table-backed journal-facing read paths (journal endpoint + session read model) to derive `LogEntries` from the event stream via this projector instead of the `GameSessionLogEntries` table. The legacy table write path (`SyncLogEntriesAsync`) and the snapshot+replay command-load read path remain as an explicitly bounded compatibility surface until a follow-up removes `AddLogEntry` from `Apply`.

**Tech Stack:** C#/.NET, xUnit, EF Core, PostgreSQL (dev service on `localhost:5434`), Vite/React/TypeScript frontend.

## Global Constraints

- Player-facing journal/log behavior (text, `kind`, `day`, `turn`, ordering, paging) must remain byte-for-byte identical to current `main` for all production-reachable states.
- `GameSession` remains the command aggregate root; no gameplay mutation moves out of it.
- Do not remove `AddLogEntry`/`RecordCaseUpdate`/`RecordTravelUpdate` from `Apply` in this slice (write-path removal is a deferred follow-up).
- Do not remove the `GameSessionLogEntries` table, its migration, or `SyncLogEntriesAsync` in this slice (compatibility surface for the snapshot+replay command-load path).
- Do not change the `JournalDto` or `GameLogEntryDto` shape (no DTO/frontend contract change).
- Do not change hidden culprit truth, clue/warrant/wanted-poster flows, wallet/inventory/horse/saddle, travel rules, or lawman/heat rules.
- Onion dependency direction: `JournalLogProjector` lives in `WildBunch.Application.Projections`; `WildBunch.Persistence` may reference it (outer → inner).
- Repo uses PowerShell; no `&&` chaining. PostgreSQL tests use `.\scripts\postgres-dev.ps1 test -- ...`.

---

## Preflight Answers (required by the issue)

**Current `main` SHA inspected:** `eb3288a` (BUNCH-83 merge). Worktree/branch will be created from current `main` at execution time per the execution gate.

### Q1. Which current source seams still expose player-facing journal/log output through aggregate compatibility state?

- `src/WildBunch.Domain/Game/GameSession.cs` — `LogEntries` (Obsolete), `AddLogEntry` (Obsolete, private), `RecordCaseUpdate`, `RecordTravelUpdate`, `CompleteCase`. The `Apply` methods populate `_logEntries` as a side effect of replay/command.
- `src/WildBunch.Domain/Journal/JournalResolver.cs` — reads `session.LogEntries` into `JournalSnapshot`.
- `src/WildBunch.Application/Games/Mapping/JournalMapper.cs` — maps `JournalSnapshot.LogEntries` → `JournalDto.LogEntries`.
- `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs` — maps `session.LogEntries` / `GameSessionReadModel.LogEntries` → `GameSessionDto.LogEntries`.
- `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs` — `LoadJournalSnapshotAsync` and `LoadGameSessionReadModelAsync` read the `GameSessionLogEntries` **table**.
- `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` — `LoadStoreAsync` (command path) reads the table; `SyncLogEntriesAsync` writes it.
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` + `GameSessionRehydrator.cs` — snapshot `LogEntries` field + `ReplaceLogEntries` for rehydrate.
- Frontend: `src/WildBunch.Web/src/components/LogPanel.tsx` renders `journal?.logEntries ?? session?.logEntries`; `FieldReportPanel.tsx` shows the count.

### Q2. Which seams are already projection-backed after BUNCH-80/BUNCH-83?

- `src/WildBunch.Application/Projections/DiaryProjector.cs` — derives `DiaryProjection` (travel diary) from the full event stream. Covers GameStarted, StoreItemPurchased, InvestigationPerformed, SaloonPersonOfInterestSpotted, WantedSuspectConfronted, SheriffTurnInSettled, JourneyStarted, TravelDayAdvanced, TrailEventApplied, JourneyEncounterResolved, JourneyCompleted, JourneyArrivalAcknowledged.
- `src/WildBunch.Application/Projections/HudProjector.cs` — HUD projection from events.
- `src/WildBunch.Application/Projections/FullAuditProjector.cs` — developer/replay audit projection.
- `src/WildBunch.Api/Games/ProjectionEndpoints.cs` — exposes `/projections/hud` and `/projections/diary` by loading the full event stream via `IGameSessionRepository.GetEventStreamAsync`.
- `GameSessionDto` carries optional `HudProjection`/`DiaryProjection` fields populated by the migrated command handlers (StartNew, Purchase, 4 travel handlers).

### Q3. Which compatibility surfaces are still required?

- `GameSessionLogEntries` **table** + `SyncLogEntriesAsync` write: still required by the snapshot+replay **command-load** path (`EfGameSessionRepository.LoadStoreAsync` → `ToAggregate` → `ReplaceLogEntries` from table, then `ApplyCommittedEvents` appends post-snapshot log entries). Removing it is the write-path removal slice (deferred).
- `AddLogEntry`/`RecordCaseUpdate`/`RecordTravelUpdate` in `Apply`: required so the in-memory `session.LogEntries` (used by command-response DTOs via `GameSessionMapper.ToDto(session)`) and the replay-equality tests stay consistent. Removing them is the write-path removal slice (deferred).
- `GameSessionJsonSerializer.SessionSnapshot.LogEntries` + `GameSessionRehydrator.ReplaceLogEntries`: required for snapshot rehydrate. Deferred.
- `JournalDto`/`GameLogEntryDto` shape (`kind`, `message`, `day`, `turn`): required by the API contract (`GameApiJournalTests`) and frontend (`LogPanel`). Preserved in this slice.
- `JournalResolver` (reads `session.LogEntries`): used only by the in-memory journal path; will be superseded by the projection-backed read path but retained as compatibility surface (no production caller after the switch except tests).

### Q4. Which player-facing behavior must be preserved exactly, and suspected corrections needing approval?

**Must be preserved exactly:** the `GameLogEntry` sequence — `Kind`, `Message`, `Day`, `Turn` — for every production-reachable state (start, purchase, investigation, bounty/saloon, full travel/journey cycle, encounter resolution, arrival acknowledgment).

**Critical divergence found (DO NOT swap to DiaryProjector without approval):** the existing `DiaryProjector` is NOT behavior-equivalent to the legacy log:
- `GameStarted`: diary says `"Arrived in {town}. The hunt begins."`; legacy log says `"The hunt begins in {town}."`.
- `StoreItemPurchased`: diary adds an entry; legacy `Apply(StoreItemPurchased)` adds **no** log entry.
- `SheriffTurnInSettled`: diary adds an entry; legacy `Apply(SheriffTurnInSettled)` adds **no** log entry.
- `DiaryEntry` has no `Kind` field; `GameLogEntryDto` requires `Kind`.

Therefore a direct journal→DiaryProjector swap would change player-facing text, add entries, and drop `Kind`. **This plan recommends the behavior-preserving route (new `JournalLogProjector`) and does NOT adopt the DiaryProjector text.** Adopting the DiaryProjector text would be a separate, explicitly-approved behavior correction and is out of scope for this slice.

**No suspected corrections are proposed.** Behavior is preserved as-is.

### Q5. Smallest behavior-preserving slice.

Build `JournalLogProjector` (events → exact legacy `GameLogEntry[]`) and switch the two **table-backed read paths** (journal endpoint + session read model) to project from the event stream. Leave the write path (`AddLogEntry` in `Apply`), the table write (`SyncLogEntriesAsync`), and the command-load table read (`EfGameSessionRepository.LoadStoreAsync`) as bounded compatibility surface. Command-response DTOs keep using in-memory `session.LogEntries` (already event-derived).

### Q6. Tests proving command / replay / snapshot-load equivalence.

- Projector unit tests: for each migrated event type, `JournalLogProjector.Project(events)` produces the exact `GameLogEntry` sequence that the command path's `session.LogEntries` produces (same kind/message/day/turn/count).
- Replay-equivalence: `GameSession.RehydrateFromEvents(...)` `LogEntries` == `JournalLogProjector.Project(events)` for the full journey cycle and encounter resolution (reuse `TravelTestFactory` scenarios).
- Snapshot/load equivalence: the projection-backed journal endpoint returns the same `LogEntries` the legacy table returned (existing `GameApiJournalTests` must pass unchanged, including paging `skip`/`take`).
- Session read model: `GameSessionReadModel.LogEntries` (now projected) == command-path `session.LogEntries`.

### Q7. Likely changed files, grouped.

- **Domain:** none (no `Apply`/`AddLogEntry` changes).
- **Application (new):** `src/WildBunch.Application/Projections/JournalLogProjector.cs`.
- **Application (modify):** `src/WildBunch.Application/Projections/JournalLogProjection.cs` (new record) — or reuse `GameLogEntry` directly; see Task 1.
- **Persistence (modify):** `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs` (project log entries from `StoredEvents`; drop the `GameSessionLogEntries` table read from the read-store loader).
- **Tests (new):** `tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs`.
- **Tests (modify):** `tests/WildBunch.Application.Tests/Projections/GameLogEntryLegacyProjectionTests.cs` (add projection-supersedes-table proof), `tests/WildBunch.Integration.Tests/GameApiJournalTests.cs` (assert projection-backed equivalence, keep existing assertions).
- **Docs (modify):** `docs/adr/ADR-0028-...md` (implementation status note: journal read path is projection-backed; legacy table is write-only compatibility surface).
- **Frontend:** none (DTO shape unchanged).
- **API:** none (endpoint shape unchanged).

### Q8. Validation commands.

```text
dotnet build
dotnet test tests/WildBunch.Domain.Tests --no-build
dotnet test tests/WildBunch.Application.Tests --no-build
dotnet test tests/WildBunch.GameContent.Tests --no-build
.\scripts\postgres-dev.ps1 ensure
.\scripts\postgres-dev.ps1 test -- --no-build WildBunch.sln
dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api
```

No API/DTO/UI journal surface changes, so frontend build/typecheck/tests are not strictly required by the issue's validation expectations, but a frontend typecheck will be run as a guard since `GameLogEntryDto` is referenced by the frontend (no shape change expected).

### Q9. One branch / one PR?

Yes. The slice is cohesive (one projector + two read-path switches + tests + ADR note), touches no other subsystem, and produces a single testable deliverable. No repo evidence requires a split.

### Q10. Retained/deferred compatibility surface.

Retained (explicitly bounded in return notes and ADR):
- `GameSessionLogEntries` table + EF migration + `GameSessionLogEntryEntity` + `GameSessionEntityConfiguration`.
- `SyncLogEntriesAsync` (table write on every command).
- `EfGameSessionRepository.LoadStoreAsync` table read (command snapshot+replay load).
- `AddLogEntry`/`RecordCaseUpdate`/`RecordTravelUpdate`/`CompleteCase` in `GameSession`.
- `GameSessionJsonSerializer.SessionSnapshot.LogEntries` + `GameSessionRehydrator.ReplaceLogEntries`.
- `JournalResolver` (in-memory path; no production caller after switch).

Deferred to a follow-up (write-path removal): drop `AddLogEntry` from `Apply`, drop the table, drop the snapshot `LogEntries` field, drop `JournalResolver`, align or merge `JournalLogProjector` with `DiaryProjector` if Harley approves a text correction.

**Known caveat:** `CompleteCase` (direct `AddLogEntry`, non-event-sourced) has **no production callers** (verified by repo search — only its definition and test references to the public `RecordCaseUpdate`). It is a dead non-migrated stub. If it becomes reachable later it would need its own typed event before the projector can cover it. Not blocking this slice.

---

## File Structure

| File | Responsibility | Action |
|------|----------------|--------|
| `src/WildBunch.Application/Projections/JournalLogProjector.cs` | Pure function: `IReadOnlyList<IDomainEvent>` → `IReadOnlyList<GameLogEntry>`, reproducing exact legacy log. | Create |
| `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs` | Load event stream from `StoredEvents`, deserialize, project log entries via `JournalLogProjector`, apply `skip`/`take`; stop reading `GameSessionLogEntries` table for the two read paths. | Modify |
| `tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs` | Pin exact projected log entries per event type (hand-authored events). | Create |
| `tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs` | Full-cycle + encounter-resolution equivalence vs command path (uses `TravelTestFactory`). | Create |
| `tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj` | Add test-only project reference to `Application` (for `JournalLogProjector`). | Modify |
| `tests/WildBunch.Application.Tests/Projections/GameLogEntryLegacyProjectionTests.cs` | Narrowed projector behavior test (NOT a table-removal proof). | Modify |
| `tests/WildBunch.Application.Tests/ReadStoreLoaderJournalProjectionGuardrailTests.cs` | Source-inspection guardrail: read-store loader dropped `GameSessionLogEntries`; command-load repository kept it. | Create |
| `tests/WildBunch.Integration.Tests/GameApiJournalTests.cs` | Existing assertions are the regression gate (no new assertions; existing text/kind/day/turn/paging checks must remain green after the read-path switch). | No change (regression gate) |
| `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md` | Note journal read path is projection-backed; table is write-only compatibility. | Modify |

---

## Task 1: JournalLogProjector — pure event→log projection

**Files:**
- Create: `src/WildBunch.Application/Projections/JournalLogProjector.cs`
- Test: `tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs`

**Interfaces:**
- Consumes: `WildBunch.Domain.Events.*` (typed events), `WildBunch.Domain.Game.GameLogEntry`, `WildBunch.Domain.Game.GameLogEntryKind`.
- Produces: `IReadOnlyList<GameLogEntry>` — exact legacy log sequence. Later tasks consume this via `JournalLogProjector.Project(events)`.

**Legacy log production rules to reproduce exactly (verified from `GameSession.Apply` on current main):**

| Event | Log entries produced (in order) | Day/Turn source |
|-------|---------------------------------|-----------------|
| `GameStarted` | `GameLogEntry(Opening, $"The hunt begins in {e.StartingTownName}.", 1, 0)` | init day=1, turn=0 |
| `TownActionContextEntered` | (none; updates tracked day=e.Day, turn=e.Turn) | tc.Day, tc.Turn |
| `StoreItemPurchased` | (none — `Apply` adds no log entry) | — |
| `InvestigationPerformed` | `GameLogEntry(CaseUpdate, e.Message, day, turn)` | tracked |
| `SaloonPersonOfInterestSpotted` | if `e.RecordLog`: `GameLogEntry(CaseUpdate, e.Message, day, turn)` | tracked |
| `WantedSuspectConfronted` | `GameLogEntry(CaseUpdate, e.Message, day, turn)` | tracked |
| `SheriffTurnInSettled` | (none — `Apply` adds no log entry) | — |
| `SaloonPersonOfInterestConfronted` | (none) | — |
| `JourneyStarted` | if `e.DiaryMessage` non-empty: `GameLogEntry(Travel, e.DiaryMessage, day, turn)` | tracked |
| `TravelDayAdvanced` | for each `e.AdditionalDiaryMessages`: `GameLogEntry(Travel, msg, e.Day, 0)`; then `e.DiaryMessage` (if non-empty); then `e.HorseLostMessage` (if non-empty) | day=e.Day, turn=0 |
| `TrailEventApplied` | `e.DiaryMessage` (if non-empty); then `e.HorseLostMessage` (if non-empty) | tracked (day unchanged, turn=0) |
| `JourneyEncounterResolved` | for each `e.AdditionalDiaryMessages`: `GameLogEntry(Travel, msg, day, turn)`; then `e.DiaryMessage` (if non-empty) | tracked |
| `JourneyCompleted` | `e.DiaryMessage` (if non-empty) | tracked |
| `JourneyArrivalAcknowledged` | `e.DiaryMessage` (if non-empty) | tracked |

**Clock tracking in the projector** (mirrors `Apply` mutation order):
- Start: `day = 1`, `turn = 0`.
- `TownActionContextEntered`: `day = e.Day`, `turn = e.Turn`.
- `TravelDayAdvanced`: `day = e.Day`, `turn = 0` (set before emitting that event's entries).
- All other events: use the currently tracked `day`/`turn`.

- [ ] **Step 1: Write failing projector tests for each event type**

Create `tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs` with these tests:

```csharp
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Projections;

public sealed class JournalLogProjectorTests
{
    private static GameStarted GameStartedEvent() => new()
    {
        PlayerName = "Ranger Vale",
        StartingTownId = new TownId("pinecross"),
        StartingTownName = "Pinecross",
        StartingHealth = 100,
        StartingWallet = 25m,
        StartingInventoryItems = Array.Empty<InventoryItem>(),
        Difficulty = TravelDifficulty.Normal,
        TravelRandomness = TravelRandomnessState.CreateDeterministic("test"),
        Entropy = AdventureRandomnessPolicy.Standard
    };

    [Fact]
    public void GameStarted_ProducesSingleOpeningEntryWithLegacyText()
    {
        var projector = new JournalLogProjector();
        var log = projector.Project(new IDomainEvent[] { GameStartedEvent() });

        Assert.Single(log);
        Assert.Equal(GameLogEntryKind.Opening, log[0].Kind);
        Assert.Equal("The hunt begins in Pinecross.", log[0].Message);
        Assert.Equal(1, log[0].Day);
        Assert.Equal(0, log[0].Turn);
    }

    [Fact]
    public void StoreItemPurchased_ProducesNoLogEntry_MatchingLegacyApply()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new StoreItemPurchased
            {
                TownId = new TownId("pinecross"),
                ItemKind = ItemKind.Food,
                DisplayName = "Trail Biscuits",
                Quantity = 2,
                UnitPrice = 2m,
                TotalPrice = 4m,
                WalletAfter = 21m
            }
        };
        var log = projector.Project(events);

        // Only the opening entry — purchase adds no legacy log entry.
        Assert.Single(log);
        Assert.Equal(GameLogEntryKind.Opening, log[0].Kind);
    }

    [Fact]
    public void SheriffTurnInSettled_ProducesNoLogEntry_MatchingLegacyApply()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new SheriffTurnInSettled
            {
                TargetSuspectId = new SuspectId("suspect-1"),
                TargetName = "Jesse Roe",
                Disposition = WarrantDisposition.DeadOrAlive,
                IsAlive = true,
                BountyAmount = 50m,
                Message = "You turn Jesse Roe in for the bounty.",
                Day = 1,
                Turn = 0
            }
        };
        var log = projector.Project(events);

        Assert.Single(log); // opening only; sheriff turn-in adds no legacy log entry
    }

    [Fact]
    public void InvestigationPerformed_ProducesCaseUpdateEntryWithTrackedDayTurn()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new TownActionContextEntered { Day = 1, Turn = 1, Context = TownActionContext.SheriffOffice, TownId = new TownId("pinecross"), TimeOfDay = TimeOfDay.Afternoon },
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.SheriffWarrants,
                TownId = new TownId("pinecross"),
                Message = "You check the wanted posters."
            }
        };
        var log = projector.Project(events);

        Assert.Equal(2, log.Count);
        Assert.Equal(GameLogEntryKind.CaseUpdate, log[1].Kind);
        Assert.Equal("You check the wanted posters.", log[1].Message);
        Assert.Equal(1, log[1].Day);
        Assert.Equal(1, log[1].Turn);
    }

    [Fact]
    public void TravelDayAdvanced_ProducesTravelEntriesWithAbsoluteDayAndTurnZero()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new JourneyStarted { JourneySnapshot = null!, DiaryMessage = "You set out." },
            new TravelDayAdvanced
            {
                Day = 2,
                JourneySnapshot = null!,
                HealthDelta = 0,
                PursuitHeat = 0,
                DayOutcome = TravelDayOutcome.Ongoing,
                AdditionalDiaryMessages = new[] { "A quiet morning." },
                DiaryMessage = "You reach the next leg.",
                HorseLostMessage = null
            }
        };
        var log = projector.Project(events);

        // GameStarted opening (day 1, turn 0) + JourneyStarted travel entry (day 1, turn 0)
        // + TravelDayAdvanced additional narration (day 2, turn 0) + diary message (day 2, turn 0).
        // The event list includes GameStarted, so the opening entry is log[0]; the travel
        // entries follow. Count is 4, not 3.
        Assert.Equal(4, log.Count);
        Assert.Equal(GameLogEntryKind.Opening, log[0].Kind);
        Assert.Equal("The hunt begins in Pinecross.", log[0].Message);
        Assert.Equal(1, log[0].Day);
        Assert.Equal(0, log[0].Turn);
        Assert.Equal(GameLogEntryKind.Travel, log[1].Kind);
        Assert.Equal("You set out.", log[1].Message);
        Assert.Equal(1, log[1].Day);
        Assert.Equal(0, log[1].Turn);
        Assert.Equal(GameLogEntryKind.Travel, log[2].Kind);
        Assert.Equal("A quiet morning.", log[2].Message);
        Assert.Equal(2, log[2].Day);
        Assert.Equal(0, log[2].Turn);
        Assert.Equal(GameLogEntryKind.Travel, log[3].Kind);
        Assert.Equal("You reach the next leg.", log[3].Message);
        Assert.Equal(2, log[3].Day);
        Assert.Equal(0, log[3].Turn);
    }

    [Fact]
    public void EmptyMessagesAndHorseLostMessage_AreSkippedOrEmittedExactlyAsLegacy()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new TravelDayAdvanced
            {
                Day = 2,
                JourneySnapshot = null!,
                HealthDelta = 0,
                PursuitHeat = 0,
                DayOutcome = TravelDayOutcome.Ongoing,
                AdditionalDiaryMessages = Array.Empty<string>(),
                DiaryMessage = "",
                HorseLostMessage = "Your horse went lame."
            }
        };
        var log = projector.Project(events);

        // opening + horse-lost only; empty DiaryMessage is skipped
        Assert.Equal(2, log.Count);
        Assert.Equal(GameLogEntryKind.Travel, log[1].Kind);
        Assert.Equal("Your horse went lame.", log[1].Message);
        Assert.Equal(2, log[1].Day);
    }
}
```

Note: `JourneySnapshot = null!` is test-only; the projector does not read the snapshot for log text. If the event records require non-null fields to compile, use a minimal valid snapshot via `TravelTestFactory` instead — but the projector only reads `DiaryMessage`/`AdditionalDiaryMessages`/`HorseLostMessage`/`Day`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~JournalLogProjectorTests"`
Expected: FAIL — `JournalLogProjector` does not exist (compile error).

- [ ] **Step 3: Implement JournalLogProjector**

Create `src/WildBunch.Application/Projections/JournalLogProjector.cs`:

```csharp
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Projections;

/// <summary>
/// Pure projector that derives the legacy <see cref="GameLogEntry"/> sequence from the
/// typed domain event stream, reproducing exactly what <see cref="GameSession"/>'s Apply
/// methods produce via AddLogEntry/RecordCaseUpdate/RecordTravelUpdate.
/// This is the projection-backed replacement for the GameSessionLogEntries table on the
/// journal read path. See ADR-0028 and BUNCH-84.
/// </summary>
public sealed class JournalLogProjector
{
    public IReadOnlyList<GameLogEntry> Project(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var day = 1;
        var turn = 0;
        var entries = new List<GameLogEntry>();

        foreach (var e in events)
        {
            switch (e)
            {
                case GameStarted gs:
                    day = 1;
                    turn = 0;
                    entries.Add(new GameLogEntry(GameLogEntryKind.Opening, $"The hunt begins in {gs.StartingTownName}.", day, turn));
                    break;

                case TownActionContextEntered tc:
                    day = tc.Day;
                    turn = tc.Turn;
                    break;

                case StoreItemPurchased:
                    // Legacy Apply adds no log entry for purchases.
                    break;

                case InvestigationPerformed ip:
                    entries.Add(new GameLogEntry(GameLogEntryKind.CaseUpdate, ip.Message, day, turn));
                    break;

                case SaloonPersonOfInterestSpotted sp:
                    if (sp.RecordLog)
                        entries.Add(new GameLogEntry(GameLogEntryKind.CaseUpdate, sp.Message, day, turn));
                    break;

                case WantedSuspectConfronted wc:
                    entries.Add(new GameLogEntry(GameLogEntryKind.CaseUpdate, wc.Message, day, turn));
                    break;

                case SheriffTurnInSettled:
                    // Legacy Apply adds no log entry for sheriff turn-in.
                    break;

                case SaloonPersonOfInterestConfronted:
                    // No log entry.
                    break;

                case JourneyStarted js:
                    if (!string.IsNullOrEmpty(js.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, js.DiaryMessage, day, turn));
                    break;

                case TravelDayAdvanced tda:
                    day = tda.Day;
                    turn = 0;
                    foreach (var narration in tda.AdditionalDiaryMessages)
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, narration, day, turn));
                    if (!string.IsNullOrEmpty(tda.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, tda.DiaryMessage, day, turn));
                    if (!string.IsNullOrEmpty(tda.HorseLostMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, tda.HorseLostMessage, day, turn));
                    break;

                case TrailEventApplied tea:
                    if (!string.IsNullOrEmpty(tea.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, tea.DiaryMessage, day, turn));
                    if (!string.IsNullOrEmpty(tea.HorseLostMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, tea.HorseLostMessage, day, turn));
                    break;

                case JourneyEncounterResolved jer:
                    foreach (var narration in jer.AdditionalDiaryMessages)
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, narration, day, turn));
                    if (!string.IsNullOrEmpty(jer.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, jer.DiaryMessage, day, turn));
                    break;

                case JourneyCompleted jc:
                    if (!string.IsNullOrEmpty(jc.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, jc.DiaryMessage, day, turn));
                    break;

                case JourneyArrivalAcknowledged jaa:
                    if (!string.IsNullOrEmpty(jaa.DiaryMessage))
                        entries.Add(new GameLogEntry(GameLogEntryKind.Travel, jaa.DiaryMessage, day, turn));
                    break;
            }
        }

        return entries;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~JournalLogProjectorTests"`
Expected: PASS.

- [ ] **Step 5: Add full-cycle equivalence test vs command path**

The full-cycle test needs both `JournalLogProjector` (Application) and `TravelTestFactory` (Domain.Tests). Neither test project currently sees both. Put this test in `Domain.Tests` and add a test-only project reference from `Domain.Tests` → `Application`.

First, add the project reference to `tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`:

```xml
<ProjectReference Include="..\..\src\WildBunch.Application\WildBunch.Application.csproj" />
```

This is test-only; the onion dependency rule governs production code, not test projects.

Create `tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs`:

```csharp
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Proves the JournalLogProjector (Application.Projections) reproduces the exact
/// GameLogEntry sequence that the command path's session.LogEntries produces for a
/// full journey cycle. Uses TravelTestFactory for deterministic scenario setup.
/// See ADR-0028 and BUNCH-84.
/// </summary>
public sealed class JournalLogProjectorEquivalenceTests
{
    [Fact]
    public void FullJourneyCycle_ProjectedLogMatchesCommandPathLogEntriesExactly()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);
        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        session.AcknowledgeJourneyArrival();

        var gameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);
        var events = new[] { gameStarted }.Concat(session.UncommittedEvents).ToList();

        var projected = new JournalLogProjector().Project(events);

        Assert.Equal(session.LogEntries.Count, projected.Count);
        for (var i = 0; i < session.LogEntries.Count; i++)
        {
            Assert.Equal(session.LogEntries[i].Kind, projected[i].Kind);
            Assert.Equal(session.LogEntries[i].Message, projected[i].Message);
            Assert.Equal(session.LogEntries[i].Day, projected[i].Day);
            Assert.Equal(session.LogEntries[i].Turn, projected[i].Turn);
        }
    }

    [Fact]
    public void ResolveJourneyEncounter_ProjectedLogMatchesCommandPathLogEntriesExactly()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        var gameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);
        session.StartJourney(preview);

        TravelJourneyStepResult step;
        do
        {
            step = session.AdvanceJourneyDay();
        } while (step.Status == JourneyStatus.Active && step.Success);
        Assert.Equal(JourneyStatus.Interrupted, step.Status);

        var resolved = session.ResolveJourneyEncounter("run", bulletSpend: null, bribeAmount: null, forcedRoll: 0);
        Assert.True(resolved.Success);
        var events = new[] { gameStarted }.Concat(session.UncommittedEvents).ToList();

        var projected = new JournalLogProjector().Project(events);

        Assert.Equal(session.LogEntries.Count, projected.Count);
        for (var i = 0; i < session.LogEntries.Count; i++)
        {
            Assert.Equal(session.LogEntries[i].Kind, projected[i].Kind);
            Assert.Equal(session.LogEntries[i].Message, projected[i].Message);
            Assert.Equal(session.LogEntries[i].Day, projected[i].Day);
            Assert.Equal(session.LogEntries[i].Turn, projected[i].Turn);
        }
    }
}
```

Run:
```powershell
dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~JournalLogProjectorEquivalenceTests"
```
Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/WildBunch.Application/Projections/JournalLogProjector.cs tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj
git commit -m "BUNCH-84: add JournalLogProjector (event-stream -> legacy GameLogEntry projection) + equivalence tests"
```

---

## Task 2: Switch journal endpoint read path to the projection

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs` (`LoadJournalSnapshotAsync` + the read-store loader's private `LoadStoreAsync`)
- Test: `tests/WildBunch.Integration.Tests/GameApiJournalTests.cs` (existing assertions must still pass)

**Interfaces:**
- Consumes: `JournalLogProjector.Project(events)` from Task 1.
- Produces: `JournalSnapshot` with `LogEntries` derived from the event stream (not the table).

**Scope boundary — two `LoadStoreAsync` methods, only one changes:**
There are two distinct `LoadStoreAsync` methods in the persistence layer. This task touches ONLY the read-store loader's:
- `GameSessionReadStoreLoader.LoadStoreAsync` (read path, `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs:102`) — used by `LoadJournalSnapshotAsync` and `LoadGameSessionReadModelAsync`. **CHANGED in this task.**
- `EfGameSessionRepository.LoadStoreAsync` (command-load path, `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs:155`) — used by `GetByIdAsync` (command load). **NOT CHANGED.** Its `GameSessionLogEntries` table read remains intentionally deferred to the write-path-removal follow-up. Do not touch it.

**Approach:** The read-store loader's `LoadStoreAsync` currently queries `GameSessionLogEntries` for `store.LogEntries`. Replace that table query with a `StoredEvents` query (load + deserialize via `GameSessionJsonSerializer.DeserializeEvent`) and project via `JournalLogProjector`. `LoadJournalSnapshotAsync` uses the projected entries (with `skip`/`take` applied after projection) instead of `store.LogEntries`. Keep the `GameSessionDiaryDays` query.

**Persistence/back-compat stop condition (run BEFORE the read-path switch):**
Before changing the read-store loader, verify that every journal-facing state loadable from current repo/test/dev persistence has a complete enough `StoredEvents` stream to reproduce the legacy log. Specifically:

1. Inspect whether any reachable code path can persist `GameSessionLogEntries` rows without a matching complete `StoredEvents` stream. Check:
   - `EfGameSessionRepository.StoreAsync` — every command path that writes log entries also appends uncommitted events to `StoredEvents` (verified: `SyncLogEntriesAsync(session.LogEntries)` is called on the same `StoreAsync` path that appends `session.UncommittedEvents`). So any session with log rows also has events.
   - Existing dev/test databases and migrations — confirm no migration or seed path inserts `GameSessionLogEntries` without `StoredEvents`.
   - The snapshot+replay command-load path in `EfGameSessionRepository.LoadStoreAsync` — it reads the table AND replays post-snapshot events through `Apply`, which re-derives log entries. The table is redundant with the event stream for any session that went through `StoreAsync`.

2. If you find any reachable case where `GameSessionLogEntries` can exist without a complete matching event stream, **STOP**: do not switch the read path. Report the case and whether the read path needs a bounded fallback (e.g. prefer table when event stream is incomplete). Request Harley's decision before proceeding.

3. If current data does not require a fallback (expected: every persisted session has a complete `StoredEvents` stream because `StoreAsync` always appends events alongside snapshot/log writes), state this explicitly in the return notes with the evidence (the `StoreAsync` code path that appends events + writes logs together, and the migration history showing no log-only seed).

- [ ] **Step 1: Run the persistence/back-compat verification**

Inspect `EfGameSessionRepository.StoreAsync` (lines 35-132) and confirm: every `StoreAsync` call that stages log entries via `SyncLogEntriesAsync` (line 128) also appends `session.UncommittedEvents` to `StoredEvents` (lines 69-88) on the same UoW. Confirm no migration or seed inserts `GameSessionLogEntries` without `StoredEvents` (check `src/WildBunch.Persistence/Migrations/` and `tests/WildBunch.Integration.Tests/TestInfrastructure/`). Record the evidence. If a gap is found, STOP and report per the stop condition above.

- [ ] **Step 2: Regression gate — existing integration tests are the contract**

The existing `GameApiJournalTests` pin the journal contract (text, kind, day, turn, paging `skip`/`take`, travel-kind entry presence, hidden-truth boundary). No new integration test is needed here — these are the regression gate that proves the projection reproduces the legacy log through the full HTTP path.

- [ ] **Step 3: Implement event-stream load + projection in the read-store loader**

Modify `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`:

1. Add `using WildBunch.Application.Projections;` and `using WildBunch.Domain.Events;`.
2. In the read-store loader's `LoadStoreAsync` (line 102), replace the `GameSessionLogEntries` table query (lines ~119-124) with a `StoredEvents` query that loads + deserializes the full event stream, then project via `new JournalLogProjector().Project(domainEvents)` into `store.LogEntries`. Keep the `GameSessionDiaryDays` query.

Replace the `logEntries` query block with:

```csharp
var storedEvents = await dbContext.StoredEvents.AsNoTracking()
    .Where(e => e.StreamId == id.Value)
    .OrderBy(e => e.Sequence)
    .ToArrayAsync(cancellationToken)
    .ConfigureAwait(false);

var domainEvents = new IDomainEvent[storedEvents.Length];
for (var i = 0; i < storedEvents.Length; i++)
{
    domainEvents[i] = serializer.DeserializeEvent(storedEvents[i].EventType, storedEvents[i].PayloadJson);
}
var logEntries = new JournalLogProjector().Project(domainEvents);
```

Update the `GameSessionStore` record's `LogEntries` comment to note it is now projection-backed (read-store loader only).

3. `LoadJournalSnapshotAsync` already uses `ApplySlice(store.LogEntries, skip, take)` — this now slices the projected list. No change needed there beyond the source of `store.LogEntries`.

4. **Do NOT touch `EfGameSessionRepository.LoadStoreAsync`** (command-load path). Its table read stays as deferred compatibility surface.

- [ ] **Step 4: Build and run the journal integration tests**

Run:
```powershell
dotnet build
.\scripts\postgres-dev.ps1 ensure
.\scripts\postgres-dev.ps1 test -- --no-build --filter "FullyQualifiedName~GameApiJournalTests"
```
Expected: PASS (all existing journal assertions, including paging `skip`/`take` and the travel-kind entry check, remain green because the projection reproduces the legacy log exactly).

- [ ] **Step 5: Commit**

```powershell
git add src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs
git commit -m "BUNCH-84: derive journal LogEntries from event-stream projection instead of legacy table (read-store loader only)"
```

---

## Task 3: Switch session read model LogEntries to the projection

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs` (`LoadGameSessionReadModelAsync`)
- Test: existing `GameApiTests` / `EfGameSessionRepositoryTests` must remain green.

**Interfaces:**
- Consumes: the projection-backed `store.LogEntries` from Task 2 (already produced in `LoadStoreAsync`).
- Produces: `GameSessionReadModel.LogEntries` projection-backed.

**Approach:** `LoadGameSessionReadModelAsync` already reads `store.LogEntries` (line 52). Since Task 2 made `store.LogEntries` projection-backed, the session read model is now projection-backed automatically. Verify no double-read of the table remains and that `LoadStoreAsync` no longer references `GameSessionLogEntries`.

- [ ] **Step 1: Verify `LoadStoreAsync` no longer queries the table**

Confirm the only remaining `GameSessionLogEntries` reference in `GameSessionReadStoreLoader.cs` is gone (the table query was replaced in Task 2). If any residual table read remains for the read store loader, remove it.

- [ ] **Step 2: Run session read-model integration tests**

Run:
```powershell
.\scripts\postgres-dev.ps1 test -- --no-build --filter "FullyQualifiedName~EfGameSessionRepositoryTests|FullyQualifiedName~GameApiTests"
```
Expected: PASS.

- [ ] **Step 3: Commit (if any change beyond Task 2)**

If Task 2 already covered this (likely, since both loaders share `LoadStoreAsync`), fold this into Task 2's commit and mark this task complete with a note. Otherwise:

```powershell
git add src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs
git commit -m "BUNCH-84: session read model LogEntries now projection-backed via shared loader"
```

---

## Task 4: Projector behavior coverage + read-path switch guardrail

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Projections/GameLogEntryLegacyProjectionTests.cs` (narrow the existing test name/claim; it is a projector behavior test, not a table-removal proof)
- Create: `tests/WildBunch.Application.Tests/ReadStoreLoaderJournalProjectionGuardrailTests.cs` (source-inspection guardrail proving the read-store loader dropped the table query while the command-load repository kept it)
- Verify: `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs` (count must NOT increase).

**Interfaces:**
- Consumes: `JournalLogProjector` from Task 1; the two persistence files' source text.

**Why two separate tests:** The projector behavior test proves the projector reproduces the legacy log over hand-authored events. The source-inspection guardrail proves the actual read-path switch happened in source (`GameSessionReadStoreLoader` no longer queries `GameSessionLogEntries`) AND that the command-load compatibility read in `EfGameSessionRepository` was left untouched. The latter is the real "table read removed from the read path" evidence; the former is not.

- [ ] **Step 1: Narrow the projector behavior test name and claim**

Append to `GameLogEntryLegacyProjectionTests.cs` a projector behavior test (NOT named "supersedes-table"). It proves `JournalLogProjector` reproduces the legacy log for a multi-event stream and that `StoreItemPurchased` adds no entry (the key behavior-preserving divergence vs `DiaryProjector`). The name and XML doc comment must describe projector behavior, not table removal.

```csharp
/// <summary>
/// Projector behavior: JournalLogProjector reproduces the legacy GameLogEntry
/// sequence for a mixed event stream, and (unlike DiaryProjector) adds no entry
/// for StoreItemPurchased — matching the legacy Apply(StoreItemPurchased) which
/// records no log entry. This is a projector behavior test, not a table-removal
/// proof; the read-path switch is proven by
/// ReadStoreLoaderJournalProjectionGuardrailTests.
/// </summary>
[Fact]
public void JournalLogProjector_ReproducesLegacyLogSequence_AndSkipsPurchaseEntries()
{
    var projector = new JournalLogProjector();
    var events = new IDomainEvent[]
    {
        new GameStarted
        {
            PlayerName = "Ranger Vale",
            StartingTownId = new TownId("pinecross"),
            StartingTownName = "Pinecross",
            StartingHealth = 100,
            StartingWallet = 25m,
            StartingInventoryItems = Array.Empty<InventoryItem>(),
            Difficulty = TravelDifficulty.Normal,
            TravelRandomness = TravelRandomnessState.CreateDeterministic("test"),
            Entropy = AdventureRandomnessPolicy.Standard
        },
        new TownActionContextEntered { Day = 1, Turn = 1, Context = TownActionContext.SheriffOffice, TownId = new TownId("pinecross"), TimeOfDay = TimeOfDay.Afternoon },
        new InvestigationPerformed { SourceKind = InvestigationSourceKind.LocalRecords, TownId = new TownId("pinecross"), Message = "A public lead is noted." },
        new StoreItemPurchased { TownId = new TownId("pinecross"), ItemKind = ItemKind.Food, DisplayName = "Trail Biscuits", Quantity = 1, UnitPrice = 2m, TotalPrice = 2m, WalletAfter = 23m }
    };

    var log = projector.Project(events);

    // Opening + case update only; StoreItemPurchased adds no legacy log entry.
    Assert.Equal(2, log.Count);
    Assert.Equal(GameLogEntryKind.Opening, log[0].Kind);
    Assert.Equal("The hunt begins in Pinecross.", log[0].Message);
    Assert.Equal(GameLogEntryKind.CaseUpdate, log[1].Kind);
    Assert.Equal("A public lead is noted.", log[1].Message);
    Assert.Equal(1, log[1].Day);
    Assert.Equal(1, log[1].Turn);
}
```

- [ ] **Step 2: Add the read-path switch source-inspection guardrail**

Create `tests/WildBunch.Application.Tests/ReadStoreLoaderJournalProjectionGuardrailTests.cs`. This mirrors the `AddLogEntryGuardrailTests` file-scoped regex pattern. It proves two things in source:
1. `GameSessionReadStoreLoader.cs` no longer references `GameSessionLogEntries` (the read path switched to the event-stream projection).
2. `EfGameSessionRepository.cs` STILL references `GameSessionLogEntries` (the command-load compatibility read was intentionally left untouched per BUNCH-84 scope).

```csharp
using System.Text.RegularExpressions;

namespace WildBunch.Application.Tests;

/// <summary>
/// Source-inspection guardrail proving the BUNCH-84 read-path switch landed and the
/// command-load compatibility read was left untouched.
/// - GameSessionReadStoreLoader (journal/session read-model read path) must NOT query
///   GameSessionLogEntries after BUNCH-84; it derives LogEntries from StoredEvents via
///   JournalLogProjector.
/// - EfGameSessionRepository (command-load path) MUST still query GameSessionLogEntries
///   as bounded compatibility surface; its table read is deferred to the write-path-removal
///   follow-up and must not be removed in this slice.
/// See ADR-0028 and BUNCH-84.
/// </summary>
public sealed class ReadStoreLoaderJournalProjectionGuardrailTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"Could not find repo root (AGENTS.md sentinel) starting from {AppContext.BaseDirectory}");
    }

    [Fact]
    public void ReadStoreLoader_NoLongerQueriesGameSessionLogEntriesTable()
    {
        var repoRoot = FindRepoRoot();
        var loaderPath = Path.Combine(repoRoot, "src", "WildBunch.Persistence", "GameSessions", "GameSessionReadStoreLoader.cs");
        Assert.True(File.Exists(loaderPath), $"Could not find GameSessionReadStoreLoader.cs at {loaderPath}.");

        var source = File.ReadAllText(loaderPath);

        // After BUNCH-84 the read-store loader derives LogEntries from StoredEvents via
        // JournalLogProjector and must not query the GameSessionLogEntries table.
        Assert.DoesNotMatch(@"GameSessionLogEntries", source);
        // It must reference the projector and StoredEvents to prove the switch landed.
        Assert.Matches(@"JournalLogProjector", source);
        Assert.Matches(@"StoredEvents", source);
    }

    [Fact]
    public void EfGameSessionRepository_StillQueriesGameSessionLogEntriesTable_AsBoundedCompatibilitySurface()
    {
        var repoRoot = FindRepoRoot();
        var repoPath = Path.Combine(repoRoot, "src", "WildBunch.Persistence", "GameSessions", "EfGameSessionRepository.cs");
        Assert.True(File.Exists(repoPath), $"Could not find EfGameSessionRepository.cs at {repoPath}.");

        var source = File.ReadAllText(repoPath);

        // The command-load path intentionally retains the GameSessionLogEntries table read
        // as bounded compatibility surface (deferred to the write-path-removal follow-up).
        // If this assertion fails, the command-load compatibility read was removed outside
        // BUNCH-84 scope — investigate before updating this test.
        Assert.Matches(@"GameSessionLogEntries", source);
    }
}
```

- [ ] **Step 3: Run the guardrail + projection tests**

Run:
```powershell
dotnet test tests/WildBunch.Application.Tests --no-build --filter "FullyQualifiedName~AddLogEntryGuardrailTests|FullyQualifiedName~GameLogEntryLegacyProjectionTests|FullyQualifiedName~JournalLogProjectorTests|FullyQualifiedName~ReadStoreLoaderJournalProjectionGuardrailTests"
```
Expected: PASS. The `AddLogEntryGuardrailTests` count must NOT increase (this slice adds no new `AddLogEntry` call sites). The new guardrail proves the read-path switch landed and the command-load read was untouched.

- [ ] **Step 4: Commit**

```powershell
git add tests/WildBunch.Application.Tests/Projections/GameLogEntryLegacyProjectionTests.cs tests/WildBunch.Application.Tests/ReadStoreLoaderJournalProjectionGuardrailTests.cs
git commit -m "BUNCH-84: narrow projector behavior test + add read-path switch source guardrail"
```

---

## Task 5: ADR-0028 implementation status note

**Files:**
- Modify: `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`

- [ ] **Step 1: Append a BUNCH-84 dated status line + update the deferred-items note**

Add to the Dated Status History (after the BUNCH-83 line):

```markdown
- 2026-06-24 - live (BUNCH-84): Journal-facing read output moved off the GameSessionLogEntries table onto an event-stream projection. New JournalLogProjector (Application.Projections) reproduces the exact legacy GameLogEntry sequence from typed domain events. LoadJournalSnapshotAsync and LoadGameSessionReadModelAsync now derive LogEntries from StoredEvents via the projector; the GameSessionLogEntries table read was removed from the read-store loader. The table write (SyncLogEntriesAsync) and the snapshot+replay command-load table read (EfGameSessionRepository.LoadStoreAsync) remain as bounded compatibility surface until AddLogEntry is removed from Apply in a follow-up. JournalDto/GameLogEntryDto shape unchanged; player-facing behavior preserved. DiaryProjector text remains intentionally distinct (narrative diary) and is not used for the legacy-shaped journal output.
```

Update the "Remaining non-migrated flows" bullet (line ~177) to note `LegacyLogProjector` is now implemented as `JournalLogProjector` and the journal read path is projection-backed; the remaining deferred work is the write-path removal (drop `AddLogEntry` from `Apply`, drop the table, drop the snapshot `LogEntries` field, drop `JournalResolver`).

- [ ] **Step 2: Commit**

```powershell
git add docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md
git commit -m "BUNCH-84: note journal read path is projection-backed in ADR-0028"
```

---

## Task 6: Full validation

- [ ] **Step 1: Build**

Run: `dotnet build`
Expected: PASS, no warnings introduced (the `#pragma warning disable CS0618` blocks remain on the still-relevant legacy surfaces).

- [ ] **Step 2: Domain + Application + GameContent unit tests**

Run:
```powershell
dotnet test tests/WildBunch.Domain.Tests --no-build
dotnet test tests/WildBunch.Application.Tests --no-build
dotnet test tests/WildBunch.GameContent.Tests --no-build
```
Expected: PASS.

- [ ] **Step 3: PostgreSQL-backed integration tests**

Run:
```powershell
.\scripts\postgres-dev.ps1 ensure
.\scripts\postgres-dev.ps1 test -- --no-build WildBunch.sln
```
Expected: PASS (including `GameApiJournalTests`, `EfGameSessionRepositoryTests`, `PostgreSqlPersistenceTests`, `MigrationTests`).

- [ ] **Step 4: EF migrations list (no migration change expected)**

Run:
```powershell
dotnet tool restore
dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api
```
Expected: existing migrations list unchanged (no new migration; table is retained as compatibility surface).

- [ ] **Step 5: Frontend typecheck guard (no shape change expected)**

Run from `src/WildBunch.Web`:
```powershell
npm run typecheck
```
Expected: PASS (the `GameLogEntryDto`/`JournalDto` shape is unchanged).

- [ ] **Step 6: Clean worktree proof**

Run: `git status --short`
Expected: clean (or only the plan/ADR files if not yet committed).

---

## Self-Review

**1. Spec coverage:**
- "Journal-facing output no longer relies on avoidable legacy aggregate log compatibility where a projection-backed route is available" → Tasks 1-3 (journal endpoint + session read model switch to projection); Task 4 guardrail proves the read-store loader dropped the table query.
- "Player-facing journal/log behavior remains stable" → Task 1 reproduces exact legacy log (corrected `TravelDayAdvanced` test expects 4 entries including the opening); divergences from DiaryProjector are explicitly NOT adopted; existing integration tests are the gate.
- "Command path, replay path, and snapshot/load path expose equivalent journal output" → Task 1 full-cycle equivalence test (command == projection); existing replay-equality tests (LogEntries count) remain green; Task 2/3 integration tests prove snapshot/load path equivalence; Task 2 persistence stop condition verifies every journal-facing state has a complete event stream before the switch.
- "Any remaining compatibility surface is explicitly bounded" → Preflight Q10 + ADR note (Task 5) + Task 4 guardrail proving `EfGameSessionRepository` command-load table read is intentionally retained.
- "Backend validation passes" → Task 6.
- "Frontend build/tests pass if API/DTO/UI journal surfaces change" → no surface change; Task 6 Step 5 typecheck guard.
- "Documentation or ADR notes updated" → Task 5.

**2. Placeholder scan:** No TBD/TODO. All code blocks are complete. Event field names match current source (`AdditionalDiaryMessages`, `HorseLostMessage`, `DiaryMessage`, `RecordLog`, `Day`/`Turn` on `TownActionContextEntered`). The `TravelDayAdvanced` test now correctly expects 4 entries (opening + journey started + additional narration + diary message) with shifted indices.

**3. Type consistency:** `JournalLogProjector.Project` returns `IReadOnlyList<GameLogEntry>` everywhere; `GameLogEntryKind.Opening/Travel/CaseUpdate` match the enum; `GameSessionReadStoreLoader` (read-store loader only) consumes the same projector signature in Task 2/3; `EfGameSessionRepository.LoadStoreAsync` (command-load) is explicitly NOT touched. Per-event projector tests live in `Application.Tests` (hand-authored events); full-cycle equivalence tests live in `Domain.Tests` (uses `TravelTestFactory`) with a test-only project reference to `Application`. All event test instances set every `required` field (`TimeOfDay`, `DayOutcome`, `Message`, `SuspectId(string)`).

**4. Read-path vs command-load boundary:** Task 2 names both `LoadStoreAsync` methods and scopes the change to `GameSessionReadStoreLoader.LoadStoreAsync` only. Task 4 guardrail proves `GameSessionReadStoreLoader.cs` no longer references `GameSessionLogEntries` while `EfGameSessionRepository.cs` still does.

---

## Risks / Unknowns

- **Event field nullability in tests:** `JourneyStarted`/`TravelDayAdvanced` carry `JourneySnapshot` which is non-nullable; tests use `null!` since the projector does not read it. If the projector ever needs snapshot-derived text, revisit. Verified the projector reads only `DiaryMessage`/`AdditionalDiaryMessages`/`HorseLostMessage`/`Day`.
- **Full event-stream scan per journal read:** the journal endpoint now loads the full `StoredEvents` stream for the session (same cost as the existing `/projections/diary` endpoint). Acceptable for the current single-session scale; a future read-model cache could materialize the projection.
- **CompleteCase dead stub:** non-event-sourced, no production callers. If revived, it needs a typed event before the projector can cover it. Not blocking.
- **DiaryProjector vs JournalLogProjector permanent divergence:** intentional. They serve different outputs (narrative diary vs legacy-shaped journal log). A future merge requires Harley's approval of a text correction.

## Split / Stop Conditions

- **Persistence/back-compat (Task 2 Step 1):** If source inspection finds any reachable case where `GameSessionLogEntries` can exist without a complete matching `StoredEvents` stream (e.g. a migration or seed that inserts log rows without events, or a code path that writes logs without appending events), STOP before switching the read path. Report the case and whether the read path needs a bounded fallback (e.g. prefer table when event stream is incomplete). Request Harley's decision before proceeding. If no gap is found, state the evidence in the return notes.
- If source inspection during execution finds a production caller of `CompleteCase` (contradicting the search), STOP: the projector would miss that entry. Report and request a typed event for it before proceeding.
- If any existing `GameApiJournalTests` assertion fails after the switch, STOP: that proves a behavior divergence. Do not adjust the tests to mask it; fix the projector to match the legacy log exactly.
- If the `AddLogEntryGuardrailTests` count increases, STOP: this slice must not add `AddLogEntry` call sites.
- If the Task 4 guardrail `EfGameSessionRepository_StillQueriesGameSessionLogEntriesTable_AsBoundedCompatibilitySurface` fails, STOP: the command-load compatibility read was removed outside BUNCH-84 scope. Investigate before updating the test.

## GREEN/AMBER/RED/BLOCKED Judgment for the Plan

**GREEN (plan-ready).** The slice is behavior-preserving, bounded to the read path, touches no other subsystem, has a clear test gate (existing integration tests + new projector equivalence tests), and leaves the write-path removal as an explicitly deferred follow-up. Awaiting Harley approval before source changes per the execution gate.
