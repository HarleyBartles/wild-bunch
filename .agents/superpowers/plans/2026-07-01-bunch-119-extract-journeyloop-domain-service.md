# BUNCH-119 Decompose GameSession by Extracting a JourneyLoop Child Domain Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Decompose the god `GameSession` aggregate by extracting a real `JourneyLoop` child domain component that owns travel/journey state and behavior through narrow inputs and explicit results, while preserving `GameSession` as the session aggregate root that orchestrates guards, event production, cross-owner mutations, and persistence.

**Architecture:** Extract ~1,100 lines of travel/journey decision logic from `GameSession` (currently 8,108 lines) into a standalone `JourneyLoop` class that does NOT reference `GameSession`. `JourneyLoop` owns: `_travelDiaryDays`, `_completedJourneyHistory`, `_nextJourneySequence`, `_pendingDevTravelOverride`, and `Journey` (active journey). It receives narrow context records as inputs (NOT the whole GameSession), returns result objects plus events-to-produce (NOT mutation through `_session`), and owns Apply handlers for its own state. `GameSession` retains: public command entry points (guards + orchestration), `EnterActionContext`, `ProduceEvent`, Apply dispatch (calling `_journeyLoop.Apply(e)` for owned-state mutations + applying cross-owner mutations itself), and the persistence/snapshot boundary. JSON snapshot shape is preserved — the same data fields are serialized; only the rehydration construction path changes to construct `JourneyLoop`.

**Tech Stack:** C# / .NET 10, xUnit, existing Wild Bunch domain tests.

## Global Constraints

- `GameSession` remains the live-play aggregate root and the only externally loaded/persisted root (ADR-0002, ADR-0020).
- `JourneyLoop` is a child domain component inside the session boundary, NOT a separate aggregate root, NOT a standalone application service, and NOT a nested class with `_session` access. It is an `internal sealed` class in `WildBunch.Domain/Game/JourneyLoop.cs` — internal because it is a session-internal component, not a public domain-service surface. If a concrete external caller later requires public visibility, widen at that point with justification.
- `JourneyLoop` must NOT reference `GameSession` in any way — no field, no parameter, no method call. This is the key falsification check.
- Do NOT introduce separate persistence tables, repositories, or EF entities for `JourneyLoop`. Keep JSON snapshot/runtime-session persistence. The snapshot record shape stays the same; only the rehydration construction path changes.
- Do NOT change any public method signature on `GameSession`, DTO shape, result-object shape, message string, or event payload.
- Do NOT change clue, journal, wanted-poster, wallet, inventory, horse, bounty-loop, or saloon state handling (out of scope).
- Preserve all existing domain event types and their `Apply(...)` mutation semantics. Event payloads and replay behavior must be identical.
- `JourneyLoop` does not call `ProduceEvent`, `EnterActionContext`, `Player.AdjustCash`, `CaseFile.Record*`, `RecordCaseUpdate`, or `CurrentTownVisit.CurrentTownState.Set*`. It returns events as data; `GameSession` produces them and applies cross-owner mutations.
- The journey snapshot (`TravelJourneySnapshot`) is the source of truth for food/canteen/horse-feed/horse-state during travel. The command-path direct Player mutations in `PrepareTravelDayAdvance` and `ApplyTrailEvent` are redundant with `SyncPlayerFromJourneySnapshot` in the Apply handlers (the code comments confirm this: "On the command path, these are no-ops"). `JourneyLoop` computes from owned journey state; `GameSession`'s Apply handlers sync Player from the journey snapshot in the event. This preserves the command-path == replay-path invariant.
- Run `dotnet build` and `dotnet test` after every task. Run `.\scripts\postgres-dev.ps1 validate` only if a task touches persistence (Tasks 9–10 do).
- If any task reveals a behavior change, STOP and report — do not "fix" it silently.

---

## Boundary Definition

### What `JourneyLoop` owns (state + invariants + Apply)

| State | Current location | Moves to `JourneyLoop` |
| --- | --- | --- |
| `TravelDiaryDayState` list | `GameSession._travelDiaryDays` (line 38) | Yes — `JourneyLoop._travelDiaryDays` |
| `TravelJourneySnapshot` history list | `GameSession._completedJourneyHistory` (line 39) | Yes — `JourneyLoop._completedJourneyHistory` |
| Next sequence counter | `GameSession._nextJourneySequence` (line 40) | Yes — `JourneyLoop._nextJourneySequence` |
| Pending dev travel override | `GameSession._pendingDevTravelOverride` (line 43) | Yes — `JourneyLoop._pendingDevTravelOverride` |
| Active journey | `GameSession.Journey` (line 151) | Yes — `JourneyLoop._journey` (GameSession exposes via delegate property) |

| Behavior | Current location | Moves to `JourneyLoop` |
| --- | --- | --- |
| Start journey decision logic | `GameSession.StartJourney` body (lines 1259–1291) | Yes — `JourneyLoop.StartJourney(context)` |
| Advance travel day decision logic | `GameSession.AdvanceJourneyDayDeterministic` (lines 1961–2041) | Yes — `JourneyLoop.AdvanceJourneyDay(context)` |
| Prepare travel day advance | `GameSession.PrepareTravelDayAdvance` (lines 1717–1815) | Yes — `JourneyLoop.PrepareTravelDayAdvance(context)` |
| Handle interrupted/completed/ongoing travel day | `GameSession.HandleInterruptedTravelDay` / `HandleCompletedTravelDay` / `HandleOngoingTravelDay` (lines 1817–1959) | Yes — `JourneyLoop.Handle*` |
| Resolve journey encounter decision logic | `GameSession.ResolveJourneyEncounterDeterministic` (lines 2291–2632) | Yes — `JourneyLoop.ResolveJourneyEncounter(context)` |
| Continue current day after encounter resolution | `GameSession.ContinueCurrentDayAfterEncounterResolution` (lines 2672–2765) | Yes — `JourneyLoop.ContinueCurrentDayAfterEncounterResolution` |
| Apply trail event | `GameSession.ApplyTrailEvent` (lines 1521–1607) | Yes — `JourneyLoop.ApplyTrailEvent` |
| Apply horse delta | `GameSession.ApplyHorseDelta` (lines 1609–1637) | Yes — `JourneyLoop.ApplyHorseDelta` (static) |
| Apply encounter horse pressure | `GameSession.ApplyEncounterHorsePressure` (lines 1495–1519) | Yes — `JourneyLoop.ApplyEncounterHorsePressure` |
| Sync player from journey snapshot | `GameSession.SyncPlayerFromJourneySnapshot` (lines 649–672) | Stays on `GameSession` — it mutates Player, which is cross-owner. JourneyLoop returns the snapshot; GameSession syncs. |
| Create travel day generation context | `GameSession.CreateTravelDayGenerationContext` (lines 2075–2138) | Yes — `JourneyLoop.CreateTravelDayGenerationContext` (reads from owned journey state + context) |
| Pressure-band factories | `GameSession.CreateFoodPressureBand` etc. (lines 2140–2289) | Yes — `JourneyLoop.Create*PressureBand` (static) |
| Recover foe profile | `GameSession.RecoverFoeProfile` (lines 2798–2822) | Yes — `JourneyLoop.RecoverFoeProfile` |
| Rebuild travel diary baseline | `GameSession.RebuildCurrentTravelDiaryBaselineState` (lines 2767–2796) | Yes — `JourneyLoop.RebuildCurrentTravelDiaryBaselineState` |
| Build journey opening narration | `GameSession.BuildJourneyOpeningNarration` (lines 2824–2848) | Yes — `JourneyLoop.BuildJourneyOpeningNarration` (static) |
| Complete journey at destination | `GameSession.CompleteJourneyAtDestination` (lines 1646–1657) | Yes — `JourneyLoop.CompleteJourneyAtDestination` |
| Append travel diary day | `GameSession.AppendTravelDiaryDay` (lines 1699–1715) | Yes — `JourneyLoop.AppendTravelDiaryDay` |
| Persist latest travel diary day | `GameSession.PersistLatestTravelDiaryDay` (lines 3829–3863) | Yes — `JourneyLoop.PersistLatestTravelDiaryDay` |
| Calculate next journey sequence | `GameSession.CalculateNextJourneySequence` (lines 2922–2935) | Yes — `JourneyLoop.CalculateNextJourneySequence` (static) |
| Description helpers | `DescribeTerrain`, `DescribeRisk`, `DescribeTravelMode`, `DescribeCanteenCoverage`, `DescribeHorseLoss`, `PrependHorseLossMessage`, `CombineHorseLossMessage` | Yes — `JourneyLoop.*` (static) |
| Dev travel override force/clear validation | `GameSession.ForceDevTravelOverride` / `ClearDevTravelOverride` (lines 1308–1340) | Yes — `JourneyLoop.ForceDevTravelOverride` / `ClearDevTravelOverride` |
| Acknowledge journey arrival | `GameSession.AcknowledgeJourneyArrival` body (lines 2043–2073) | Yes — `JourneyLoop.AcknowledgeJourneyArrival` |

### What stays owned by existing domain objects (JourneyLoop reads via context, does NOT mutate)

| State | Owner | JourneyLoop access |
| --- | --- | --- |
| `Player.Wallet` | `Player` | Read cash via context; GameSession mutates from Apply |
| `Player` inventory (food, canteen, horse feed, horse state) | `Player` | Read via context for capabilities; GameSession syncs from Apply via `SyncPlayerFromJourneySnapshot` |
| `Player.Health` | `Player` | Read via context; GameSession mutates from Apply |
| `PursuitState.Heat` | `PursuitState` | Read via context; GameSession mutates from Apply |
| `Clock` (Day, Turn) | `GameClock` | Read day via context; GameSession sets from Apply |
| `World` / `TownVisitState` | `GameSession` | Read current town via context; GameSession mutates from Apply (JourneyCompleted) |
| `TravelRules` | `GameSession` | Read via context (needed for journey snapshot, horse capabilities, pacing) |
| `SaltSource` | `GameSession` | Read salt via context (needed for day plan generation) |

### What only `GameSession` may orchestrate

- `IsArchived` / `IsJourneyModal` guards
- `EnterActionContext` (advances clock, emits context event)
- `ProduceEvent` (calls Apply + adds to uncommitted events)
- `Apply` dispatch switch
- Cross-owner mutations in Apply handlers: `Player.AdjustHealth`, `Player.SetCash`, `Player.SetHealth`, `Player.TravelTo`, `PursuitState.SetHeat`, `Clock.Set`, `RefreshTownVisit`, `RefillCanteenAfterArrival`, `SpendFirearmAmmo`, `SyncPlayerFromJourneySnapshot`, `RecordTravelUpdate` / `AddLogEntry`
- `_version++` in every Apply handler
- Snapshot serialization/deserialization coordination

---

## Context Records and Result Types

`JourneyLoop` receives narrow inputs via context records and returns results plus events-to-produce. All types live in `WildBunch.Domain/Game/JourneyLoopContexts.cs`.

### Context records

```csharp
/// <summary>Read-only inputs for starting a journey.</summary>
internal sealed record StartJourneyContext(
    TravelPreview Preview,
    int NextJourneySequence,
    TravelRules TravelRules);

/// <summary>Read-only inputs for advancing a travel day.</summary>
internal sealed record AdvanceJourneyDayContext(
    TravelRules TravelRules,
    string Salt,
    int ClockDay,
    int CurrentHeat,
    PlayerCapabilities Capabilities,
    int AvailableFood,
    int AvailableHorseFeed,
    CanteenState? CanteenState,
    HorseTravelState? HorseState);

/// <summary>Read-only inputs for resolving a journey encounter.</summary>
internal sealed record ResolveJourneyEncounterContext(
    string ChoiceId,
    int? BulletSpend,
    decimal? BribeAmount,
    ulong? ForcedRoll,
    TravelRules TravelRules,
    decimal PlayerCash,
    int PlayerHealth,
    int CurrentHeat,
    int AvailableAmmo);

/// <summary>Read-only inputs for acknowledging journey arrival.</summary>
internal sealed record AcknowledgeJourneyArrivalContext(
    TravelRules TravelRules);

/// <summary>Read-only inputs for forcing a dev travel override.</summary>
internal sealed record ForceDevTravelOverrideContext(
    DevTravelOverride Override);

/// <summary>
/// Player capabilities snapshot for travel decisions. Computed by
/// GameSession from Player state and passed to JourneyLoop as read-only context.
/// </summary>
internal sealed record PlayerCapabilities(
    bool MountedTravelAvailable,
    bool FirearmThreatAvailable);
```

### Result wrapper

```csharp
/// <summary>
/// Result from a JourneyLoop command method. Carries the public result object
/// plus events that GameSession must produce. JourneyLoop does not produce events.
/// </summary>
internal sealed record JourneyLoopResult<TResult>(TResult Result, IReadOnlyList<IDomainEvent> Events);
```

---

## Task 1: Characterize the boundary and capture a regression baseline

**Files:**
- Read-only: all journey/travel test files (listed in the codebase survey)
- Read-only: `src/WildBunch.Domain/Game/GameSession.cs`

**Interfaces:**
- Consumes: nothing (baseline step)
- Produces: a green test baseline proving existing journey/travel behavior is intact before any extraction.

- [ ] **Step 1: Run the journey/travel domain test filter and capture the green baseline**

Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~Travel|FullyQualifiedName~Journey|FullyQualifiedName~DevTravelOverride|FullyQualifiedName~TravelDiary|FullyQualifiedName~TravelDayPlan|FullyQualifiedName~TravelEvent|FullyQualifiedName~TravelReplay|FullyQualifiedName~TravelResource|FullyQualifiedName~TravelStateMachine|FullyQualifiedName~TravelResolver|FullyQualifiedName~TravelEncounter|FullyQualifiedName~JourneyUpkeep|FullyQualifiedName~JourneyHistory"
```
Expected: PASS. Record exact passed/failed/skipped counts (baseline: ~114 domain tests). If any test fails on a clean worktree from `origin/main`, STOP.

- [ ] **Step 2: Run the application test filter**

Run:
```
dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~Travel|FullyQualifiedName~Journey|FullyQualifiedName~AdvanceTravel|FullyQualifiedName~ResolveJourney|FullyQualifiedName~PreviewTravel|FullyQualifiedName~DevTravel|FullyQualifiedName~TravelDiary"
```
Expected: PASS. Record counts (baseline: ~33 application tests).

- [ ] **Step 3: Run the integration test filter**

Run: `.\scripts\postgres-dev.ps1 ensure`
Then:
```
dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~Travel|FullyQualifiedName~Journey|FullyQualifiedName~DevTravel|FullyQualifiedName~EfGameSessionRepository"
```
Expected: PASS. Record counts (baseline: ~22 integration tests).

- [ ] **Step 4: Run the full solution build**

Run: `dotnet build WildBunch.sln`
Expected: PASS, zero errors. Record warning count separately.

- [ ] **Step 5: Record the GameSession line count baseline**

Run: count lines in `src/WildBunch.Domain/Game/GameSession.cs` (baseline: 8,108 lines). Record this as the pre-extraction baseline for the line-count delta.

### Task 2: Create the JourneyLoop class skeleton with owned state and context/result types

**Files:**
- Create: `src/WildBunch.Domain/Game/JourneyLoop.cs`
- Create: `src/WildBunch.Domain/Game/JourneyLoopContexts.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add `_journeyLoop` field alongside existing journey state — do not remove anything yet)

**Interfaces:**
- Consumes: `TravelJourney`, `TravelJourneySnapshot`, `TravelDiaryDayState`, `DevTravelOverride`, domain event types, `TravelRules`, `TravelPreview`, `PlayerCapabilities`
- Produces: `JourneyLoop` class with state, constructor, context records, result wrapper, and `Apply` methods for owned state (empty bodies for now — filled in later tasks)

- [ ] **Step 1: Create `JourneyLoopContexts.cs` with all context records and the result wrapper**

Create the file with the context records and `JourneyLoopResult<TResult>` shown in the "Context Records and Result Types" section above. Add `using` directives for `WildBunch.Domain.Events`, `WildBunch.Domain.Travel`, `WildBunch.Domain.Economy`, `WildBunch.Domain.Inventory`. The context records are `internal sealed record` types in `namespace WildBunch.Domain.Game`.

- [ ] **Step 2: Create `JourneyLoop.cs` with the class skeleton**

```csharp
using WildBunch.Domain.Events;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Game;

/// <summary>
/// Child domain component inside the GameSession boundary that owns travel/journey
/// state and behavior. Receives narrow context records, returns results plus
/// events-to-produce. Does NOT reference GameSession, produce events directly,
/// enter action context, adjust cash, or mutate CaseFile/TownVisitState/Player.
/// See BUNCH-119 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class JourneyLoop
{
    private readonly List<TravelDiaryDayState> _travelDiaryDays = [];
    private readonly List<TravelJourneySnapshot> _completedJourneyHistory = [];
    private int _nextJourneySequence = 1;
    private DevTravelOverride? _pendingDevTravelOverride;
    private TravelJourney? _journey;

    internal JourneyLoop(
        TravelJourney? journey,
        IReadOnlyList<TravelJourneySnapshot>? completedJourneyHistory)
    {
        _journey = journey;
        if (completedJourneyHistory is not null)
        {
            _completedJourneyHistory.AddRange(completedJourneyHistory);
        }
        _nextJourneySequence = CalculateNextJourneySequence(journey, _completedJourneyHistory);
    }

    internal TravelJourney? Journey => _journey;
    internal IReadOnlyList<TravelDiaryDayState> TravelDiaryDays => _travelDiaryDays;
    internal IReadOnlyList<TravelJourneySnapshot> CompletedJourneyHistory => _completedJourneyHistory;
    internal int NextJourneySequence => _nextJourneySequence;
    internal DevTravelOverride? PendingDevTravelOverride => _pendingDevTravelOverride;

    // Command methods — filled in by Tasks 3–7
    // Apply methods — filled in by Task 8

    internal void RestoreTravelDiaryDays(IReadOnlyList<TravelDiaryDayState> days)
    {
        _travelDiaryDays.Clear();
        _travelDiaryDays.AddRange(days);
    }

    internal void RestorePendingDevTravelOverride(DevTravelOverride? overrideValue)
    {
        _pendingDevTravelOverride = overrideValue;
    }

    private static int CalculateNextJourneySequence(
        TravelJourney? journey,
        IReadOnlyList<TravelJourneySnapshot> completedHistory)
    {
        // Copy the exact logic from GameSession.CalculateNextJourneySequence (lines 2922-2935).
        // The sequence is max(journey?.JourneySequence, completedHistory.Max(x => x.JourneySequence)) + 1,
        // or 1 if both are empty.
    }
}
```

Copy the exact `CalculateNextJourneySequence` body from `GameSession.cs` lines 2922–2935 into the static method above.

- [ ] **Step 3: Add `_journeyLoop` field to `GameSession` and construct it**

In `GameSession`'s constructor (after the existing journey state setup, ~line 106), add:
```csharp
_journeyLoop = new JourneyLoop(journey, _completedJourneyHistory);
```
Add the field declaration near line 42:
```csharp
private readonly JourneyLoop _journeyLoop;
```
Do NOT remove `_travelDiaryDays`, `_completedJourneyHistory`, `_nextJourneySequence`, `_pendingDevTravelOverride`, or `Journey` yet. Both coexist during the migration. The `JourneyLoop` constructor receives the same `journey` and `completedJourneyHistory` values that `GameSession` already receives.

- [ ] **Step 4: Build and run the full domain test suite**

Run: `dotnet build WildBunch.sln` then `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`
Expected: PASS with the same counts as Task 1. The `JourneyLoop` class exists but is not yet used by any code path — this proves the skeleton compiles without breaking anything.

- [ ] **Step 5: Commit**

```
git add src/WildBunch.Domain/Game/JourneyLoop.cs src/WildBunch.Domain/Game/JourneyLoopContexts.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-119: add JourneyLoop class skeleton with owned state and context types"
```

### Task 3: Move DevTravelOverride force/clear commands to JourneyLoop

**Files:**
- Modify: `src/WildBunch.Domain/Game/JourneyLoop.cs` (add `ForceDevTravelOverride` and `ClearDevTravelOverride` methods)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (rewire `ForceDevTravelOverride` and `ClearDevTravelOverride` to delegate to `_journeyLoop`)

**Interfaces:**
- Consumes: `DevTravelOverride`, `DevTravelOverrideForced`, `DevTravelOverrideCleared` events
- Produces: `JourneyLoop.ForceDevTravelOverride(context)` and `JourneyLoop.ClearDevTravelOverride()` returning `JourneyLoopResult<bool>`

- [ ] **Step 1: Add `ForceDevTravelOverride` and `ClearDevTravelOverride` to JourneyLoop**

```csharp
internal JourneyLoopResult<bool> ForceDevTravelOverride(ForceDevTravelOverrideContext context)
{
    if (_journey is null || _journey.Status != JourneyStatus.Active)
    {
        throw new InvalidOperationException("Cannot force a travel override without an active journey.");
    }
    if (_journey.PendingEncounter is not null)
    {
        throw new InvalidOperationException("Cannot force a travel override while an encounter is pending.");
    }

    var e = new DevTravelOverrideForced
    {
        ForcedCategory = context.Override.ForcedCategory,
        FoeProfile = context.Override.FoeProfile,
        EncounterMessage = context.Override.EncounterMessage
    };
    return new JourneyLoopResult<bool>(true, [e]);
}

internal JourneyLoopResult<bool> ClearDevTravelOverride()
{
    if (_pendingDevTravelOverride is null)
    {
        return new JourneyLoopResult<bool>(true, []); // No-op, idempotent
    }

    return new JourneyLoopResult<bool>(true, [new DevTravelOverrideCleared()]);
}
```

- [ ] **Step 2: Rewire GameSession.ForceDevTravelOverride to delegate to _journeyLoop**

Replace the body of `GameSession.ForceDevTravelOverride` (lines 1308–1326) with:
```csharp
public void ForceDevTravelOverride(DevTravelOverride overrideValue)
{
    ArgumentNullException.ThrowIfNull(overrideValue);
    var context = new ForceDevTravelOverrideContext(overrideValue);
    var result = _journeyLoop.ForceDevTravelOverride(context);
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
}
```
Note: the `IsArchived` guard is NOT needed here because the original method does not have one — it throws `InvalidOperationException` if no active journey, which JourneyLoop preserves.

- [ ] **Step 3: Rewire GameSession.ClearDevTravelOverride to delegate to _journeyLoop**

Replace the body of `GameSession.ClearDevTravelOverride` (lines 1332–1340) with:
```csharp
public void ClearDevTravelOverride()
{
    var result = _journeyLoop.ClearDevTravelOverride();
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
}
```

- [ ] **Step 4: Build and run the dev travel override test filter**

Run: `dotnet build WildBunch.sln`
Then:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~DevTravelOverride"
```
Expected: PASS with the same counts as Task 1 (baseline: 10 tests).

- [ ] **Step 5: Commit**

```
git add src/WildBunch.Domain/Game/JourneyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-119: move DevTravelOverride force/clear commands to JourneyLoop"
```

### Task 4: Move StartJourney command to JourneyLoop

**Files:**
- Modify: `src/WildBunch.Domain/Game/JourneyLoop.cs` (add `StartJourney` method + `BuildJourneyOpeningNarration` + `DescribeTravelMode` + `DescribeCanteenCoverage` helpers)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (rewire `StartJourney` to delegate to `_journeyLoop`)

**Interfaces:**
- Consumes: `TravelPreview`, `TravelJourney`, `JourneyStarted` event, `TravelRules`
- Produces: `JourneyLoop.StartJourney(context)` returning `JourneyLoopResult<TravelJourneyStepResult>`

- [ ] **Step 1: Add `StartJourney` and supporting static helpers to JourneyLoop**

Move `BuildJourneyOpeningNarration` (lines 2824–2848), `DescribeTravelMode` (lines 2879–2880), and `DescribeCanteenCoverage` overloads (lines 2882–2898) into `JourneyLoop` as `private static` methods. Copy the exact method bodies.

Add the `StartJourney` method:
```csharp
internal JourneyLoopResult<TravelJourneyStepResult> StartJourney(StartJourneyContext context)
{
    if (_journey is not null)
    {
        return new JourneyLoopResult<TravelJourneyStepResult>(
            TravelJourneyStepResult.Failed("You are already on the trail."),
            []);
    }

    var newJourney = TravelJourney.Start(
        context.Preview,
        _nextJourneySequence,
        BuildJourneyOpeningNarration(context.Preview));
    var startMessage = $"You set out from {context.Preview.OriginTownName} toward {context.Preview.DestinationTownName} {DescribeTravelMode(context.Preview.TravelMode)}. The route is {context.Preview.RideDayDistance:0.##} ride-day unit(s) and should take {context.Preview.ExpectedDays} day(s). {DescribeCanteenCoverage(context.Preview)}.";

    var e = new JourneyStarted
    {
        JourneySnapshot = newJourney.ToSnapshot(context.TravelRules),
        DiaryMessage = startMessage,
        PursuitHeat = 0
    };

    var result = new TravelJourneyStepResult(
        true,
        JourneyStatus.Active,
        startMessage,
        startMessage,
        0,
        newJourney.ToSnapshot(context.TravelRules));

    return new JourneyLoopResult<TravelJourneyStepResult>(result, [e]);
}
```

Note: `TravelJourney.Start` creates the journey object. JourneyLoop will set `_journey` in `Apply(JourneyStarted)`, not here — following the BountyLoop pattern where owned state is mutated in Apply, not in command methods. The event carries the snapshot; Apply sets `_journey = TravelJourney.FromSnapshot(e.JourneySnapshot)`.

- [ ] **Step 2: Rewire GameSession.StartJourney to delegate to _journeyLoop**

Replace the body of `GameSession.StartJourney` (lines 1259–1291) with:
```csharp
public TravelJourneyStepResult StartJourney(TravelPreview preview)
{
    if (IsArchived)
    {
        return TravelJourneyStepResult.Failed(ArchivedBlockMessage);
    }

    ArgumentNullException.ThrowIfNull(preview);

    var context = new StartJourneyContext(preview, _journeyLoop.NextJourneySequence, TravelRules);
    var result = _journeyLoop.StartJourney(context);
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
    return result.Result;
}
```

- [ ] **Step 3: Build and run the journey start test filter**

Run: `dotnet build WildBunch.sln`
Then:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TravelResolver|FullyQualifiedName~TravelStateMachine|FullyQualifiedName~TravelReplay|FullyQualifiedName~JourneyHistory"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 4: Commit**

```
git add src/WildBunch.Domain/Game/JourneyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-119: move StartJourney command to JourneyLoop"
```

### Task 5: Move AcknowledgeJourneyArrival command to JourneyLoop

**Files:**
- Modify: `src/WildBunch.Domain/Game/JourneyLoop.cs` (add `AcknowledgeJourneyArrival` method)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (rewire `AcknowledgeJourneyArrival` to delegate to `_journeyLoop`)

**Interfaces:**
- Consumes: `JourneyArrivalAcknowledged` event, `TravelRules`
- Produces: `JourneyLoop.AcknowledgeJourneyArrival(context)` returning `JourneyLoopResult<JourneyArrivalAcknowledgementResult>`

- [ ] **Step 1: Add `AcknowledgeJourneyArrival` to JourneyLoop**

```csharp
internal JourneyLoopResult<JourneyArrivalAcknowledgementResult> AcknowledgeJourneyArrival(
    AcknowledgeJourneyArrivalContext context)
{
    if (_journey is null)
    {
        return new JourneyLoopResult<JourneyArrivalAcknowledgementResult>(
            JourneyArrivalAcknowledgementResult.Failed("No completed journey is waiting to be acknowledged."),
            []);
    }

    if (_journey.Status != JourneyStatus.Completed)
    {
        return new JourneyLoopResult<JourneyArrivalAcknowledgementResult>(
            JourneyArrivalAcknowledgementResult.Failed(
                "The journey is not ready to be acknowledged.",
                _journey.ToSnapshot(context.TravelRules)),
            []);
    }

    var completedSnapshot = _journey.ToSnapshot(context.TravelRules);
    var arrivalMessage = $"You step into {completedSnapshot.DestinationTownName} and put the trail behind you.";

    var e = new JourneyArrivalAcknowledged
    {
        JourneySequence = completedSnapshot.JourneySequence,
        JourneySnapshot = completedSnapshot,
        DiaryMessage = string.Empty
    };

    var result = new JourneyArrivalAcknowledgementResult(true, arrivalMessage, completedSnapshot);
    return new JourneyLoopResult<JourneyArrivalAcknowledgementResult>(result, [e]);
}
```

- [ ] **Step 2: Rewire GameSession.AcknowledgeJourneyArrival to delegate to _journeyLoop**

Replace the body of `GameSession.AcknowledgeJourneyArrival` (lines 2043–2073) with:
```csharp
public JourneyArrivalAcknowledgementResult AcknowledgeJourneyArrival()
{
    if (IsArchived)
    {
        return JourneyArrivalAcknowledgementResult.Failed(ArchivedBlockMessage);
    }

    var context = new AcknowledgeJourneyArrivalContext(TravelRules);
    var result = _journeyLoop.AcknowledgeJourneyArrival(context);
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
    return result.Result;
}
```

- [ ] **Step 3: Build and run the journey acknowledgement test filter**

Run: `dotnet build WildBunch.sln`
Then:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~JourneyHistory|FullyQualifiedName~TravelStateMachine|FullyQualifiedName~TravelEvent|FullyQualifiedName~TravelReplay"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 4: Commit**

```
git add src/WildBunch.Domain/Game/JourneyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-119: move AcknowledgeJourneyArrival command to JourneyLoop"
```

### Task 6: Move AdvanceJourneyDay decision logic to JourneyLoop

This is the largest and most complex task. The decision logic spans `AdvanceJourneyDayDeterministic`, `PrepareTravelDayAdvance`, `HandleInterruptedTravelDay`, `HandleCompletedTravelDay`, `HandleOngoingTravelDay`, `ApplyTrailEvent`, `ApplyHorseDelta`, `ApplyEncounterHorsePressure`, `CreateTravelDayGenerationContext`, pressure-band factories, description helpers, `CompleteJourneyAtDestination`, `AppendTravelDiaryDay`, `PersistLatestTravelDiaryDay`, `RebuildCurrentTravelDiaryBaselineState`, `CaptureTravelResources` (journey portion), and the `TravelDayAdvanceState` / `TrailEventApplicationResult` record types.

**Key design principle:** The journey snapshot (`TravelJourneySnapshot`) is the source of truth for food/canteen/horse-feed/horse-state. The current command-path direct Player mutations (e.g., `Player.RemoveQuantity(ItemKind.Food, 1)`) are redundant with `SyncPlayerFromJourneySnapshot` in `Apply(TravelDayAdvanced)` — the code comments confirm: "On the command path, direct mutations in PrepareTravelDayAdvance already set these values, so these are no-ops. On replay, they set the correct values from the snapshot." JourneyLoop computes from owned journey state; GameSession's Apply handlers sync Player from the event's journey snapshot.

**Files:**
- Modify: `src/WildBunch.Domain/Game/JourneyLoop.cs` (add all advance-day decision logic + helpers)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (rewire `AdvanceJourneyDay` to delegate to `_journeyLoop`)

**Interfaces:**
- Consumes: `AdvanceJourneyDayContext`, `TravelDayAdvanced`, `TrailEventApplied`, `DevTravelOverrideConsumed`, `JourneyCompleted` events, `TravelRules`, `TravelDayPlanGenerator`, `TravelDayPlanFactory`, `JourneyUpkeepRules`, `TravelDiaryDayFactory`, `TravelResourceSnapshotFactory`
- Produces: `JourneyLoop.AdvanceJourneyDay(context)` returning `JourneyLoopResult<TravelJourneyStepResult>`

- [ ] **Step 1: Move the private record types into JourneyLoop**

Move `TrailEventApplicationResult` (lines 2850–2857) and `TravelDayAdvanceState` (lines 2852–2857) into `JourneyLoop` as `private sealed record` types. Copy the exact field definitions.

- [ ] **Step 2: Move all static helper methods into JourneyLoop**

Move these `private static` methods from `GameSession` into `JourneyLoop`, copying exact bodies:
- `DescribeTerrain` (lines 1473–1481)
- `DescribeRisk` (lines 1483–1490)
- `DescribeHorseLoss` (lines 1453–1469)
- `PrependHorseLossMessage` (lines 1492–1494)
- `CombineHorseLossMessage` (lines 1639–1644)
- `ApplyHorseDelta` (lines 1609–1637)
- `CreateFoodPressureBand` (lines 2140–2163)
- `CreateCanteenPressureBand` (lines 2165–2202)
- `CreateHorseFeedPressureBand` (lines 2204–2236)
- `CreateHorseConditionBand` (lines 2238–2264)
- `CreateWalletBand` (lines 2266–2289)

- [ ] **Step 3: Move `CreateTravelDayGenerationContext` into JourneyLoop**

Move `CreateTravelDayGenerationContext` (lines 2075–2138) into `JourneyLoop`. This method reads from `_journey`, `SaltSource` (passed via context as `Salt`), and `TravelRules` (passed via context). Adjust it to read `_journey` (owned) and receive salt/rules from the context rather than from `GameSession` fields. Copy the exact body, replacing `Journey` references with `_journey` and `SaltSource.Salt` with the salt parameter.

- [ ] **Step 4: Move `ApplyTrailEvent` and `ApplyEncounterHorsePressure` into JourneyLoop**

Move `ApplyTrailEvent` (lines 1521–1607) into `JourneyLoop`. The current method directly mutates `Player` (cash, food, canteen, horse state) and `Journey`. In JourneyLoop:
- Replace `Player.AdjustCash(trailEvent.WalletDelta)` with tracking the wallet delta in the result (the event carries `WalletCash` as ABSOLUTE, and `Apply(TrailEventApplied)` sets `Player.SetCash(e.WalletCash)`)
- Replace `ApplyFoodDelta(trailEvent.FoodDelta)` with `Journey.AdjustFood(trailEvent.FoodDelta)` (journey-owned)
- Replace `Player.SetCanteenState(...)` with `Journey.SetCanteenCharges(...)` (journey-owned)
- Replace `Player.SetHorseState(...)` with `Journey.SetHorseState(...)` (journey-owned)
- The event's `WalletCash` field must be computed as `currentPlayerCash + trailEvent.WalletDelta`. Pass `PlayerCash` via the `AdvanceJourneyDayContext` and track a running wallet total within the advance-day computation.
- `ProduceEvent(new TrailEventApplied { ... })` becomes returning the event in the result list.

Move `ApplyEncounterHorsePressure` (lines 1495–1519) into `JourneyLoop`. This method reads from `Player.GetHorseState()` and `TravelRules` — adjust to read horse state from `_journey` or context.

- [ ] **Step 5: Move `PrepareTravelDayAdvance` into JourneyLoop**

Move `PrepareTravelDayAdvance` (lines 1717–1815) into `JourneyLoop`. Key adjustments:
- Replace `Player.GetCapabilities(TravelRules)` with `context.Capabilities`
- Replace `Player.GetQuantity(ItemKind.Food)` with `_journey.AvailableFood` (journey-owned)
- Replace `Player.RemoveQuantity(ItemKind.Food, 1)` with `_journey.ConsumeFood()` (journey-owned only — Apply syncs Player)
- Replace `Player.GetHorseState()`, `Player.GetCanteenState()`, `Player.GetQuantity(ItemKind.HorseFeed)` with journey-owned values (`_journey.HorseState`, `_journey.AvailableCanteenCharges`, `_journey.AvailableHorseFeed`)
- Replace `Player.SetCanteenState(...)`, `Player.SetHorseState(...)`, `Player.RemoveQuantity(ItemKind.HorseFeed, ...)` with journey-owned equivalents (`_journey.SetCanteenCharges(...)`, `_journey.SetHorseState(...)`, `_journey.ConsumeHorseFeed(...)`)
- Replace `Clock.AdvanceTravelDay()` with computing `newDay = context.ClockDay + 1` (the clock is set in Apply from the event's `Day` field)
- Replace `PursuitState.Heat` with `context.CurrentHeat`
- Replace `ProduceEvent(new DevTravelOverrideConsumed())` with capturing the event in the result list
- Replace `_pendingDevTravelOverride` access with owned `_pendingDevTravelOverride`
- `CaptureTravelResources()` currently calls `TravelResourceSnapshotFactory.Capture(Player, PursuitState)` — replace with building the snapshot from journey-owned values + context heat

- [ ] **Step 6: Move `HandleInterruptedTravelDay`, `HandleCompletedTravelDay`, `HandleOngoingTravelDay` into JourneyLoop**

Move these three methods (lines 1817–1959) into `JourneyLoop`. Key adjustments:
- Replace `ProduceEvent(new TravelDayAdvanced { ... })` with collecting the event in a list to return
- Replace `ProduceEvent(new JourneyCompleted { ... })` with collecting the event
- Replace `PursuitState.Heat` with the heat value from `TravelDayAdvanceState.PursuitHeat`
- Replace `Player.Wallet.Cash` with the running wallet total tracked in the advance-day computation
- `AppendTravelDiaryDay` calls move to the JourneyLoop-owned diary list
- `PersistLatestTravelDiaryDay` moves to JourneyLoop (it updates the latest diary day — owned state)

- [ ] **Step 7: Move `AdvanceJourneyDayDeterministic` into JourneyLoop**

Move `AdvanceJourneyDayDeterministic` (lines 1961–2041) into `JourneyLoop` as the `AdvanceJourneyDay(context)` method. This is the top-level orchestrator that calls `PrepareTravelDayAdvance`, iterates the day plan, calls `ApplyTrailEvent`, and dispatches to `Handle*` methods. Collect all produced events and return them in the `JourneyLoopResult`.

- [ ] **Step 8: Move `CompleteJourneyAtDestination`, `AppendTravelDiaryDay`, `PersistLatestTravelDiaryDay`, `RebuildCurrentTravelDiaryBaselineState`, `RecoverFoeProfile` into JourneyLoop**

Move these helper methods (lines 1646–1657, 1699–1715, 3829–3863, 2767–2796, 2798–2822) into `JourneyLoop`. Adjust `Player` references to journey-owned values. `CaptureTravelResources` becomes a JourneyLoop helper that builds `TravelResourceSnapshot` from journey-owned values + context heat.

- [ ] **Step 9: Rewire GameSession.AdvanceJourneyDay to delegate to _journeyLoop**

Replace the body of `GameSession.AdvanceJourneyDay` (lines 1293–1301) with:
```csharp
public TravelJourneyStepResult AdvanceJourneyDay()
{
    if (IsArchived)
    {
        return TravelJourneyStepResult.Failed(ArchivedBlockMessage);
    }

    var capabilities = Player.GetCapabilities(TravelRules);
    var context = new AdvanceJourneyDayContext(
        TravelRules,
        SaltSource.Salt,
        Clock.Day,
        PursuitState.Heat,
        new PlayerCapabilities(
            capabilities.MountedTravelAvailable,
            capabilities.FirearmThreatAvailable),
        Player.GetQuantity(ItemKind.Food),
        Player.GetQuantity(ItemKind.HorseFeed),
        Player.GetCanteenState(),
        Player.GetHorseState());

    var result = _journeyLoop.AdvanceJourneyDay(context);
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
    return result.Result;
}
```

- [ ] **Step 10: Build and run the advance-day test filter**

Run: `dotnet build WildBunch.sln`
Then:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TravelResolver|FullyQualifiedName~TravelStateMachine|FullyQualifiedName~TravelResource|FullyQualifiedName~TravelReplay|FullyQualifiedName~TravelEvent|FullyQualifiedName~TravelDiary|FullyQualifiedName~TravelDayPlan|FullyQualifiedName~JourneyUpkeep"
```
Expected: PASS with the same counts as Task 1. The replay equality tests (`TravelReplayEqualityTests`) are the critical proof that command-path == replay-path semantics are preserved.

If any test fails, STOP and investigate. The most likely failure mode is a timing mismatch where the command path previously relied on direct Player mutation before event production. The fix is to ensure the journey snapshot in the event captures the correct post-mutation state, and the Apply handler syncs Player from it.

- [ ] **Step 11: Commit**

```
git add src/WildBunch.Domain/Game/JourneyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-119: move AdvanceJourneyDay decision logic to JourneyLoop"
```

### Task 7: Move ResolveJourneyEncounter decision logic to JourneyLoop

**Files:**
- Modify: `src/WildBunch.Domain/Game/JourneyLoop.cs` (add `ResolveJourneyEncounter` + `ContinueCurrentDayAfterEncounterResolution`)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (rewire `ResolveJourneyEncounter` to delegate to `_journeyLoop`)

**Interfaces:**
- Consumes: `ResolveJourneyEncounterContext`, `JourneyEncounterResolved` event, `TravelDayAdvanced` event (if encounter resolution completes the day), `TravelRules`
- Produces: `JourneyLoop.ResolveJourneyEncounter(context)` returning `JourneyLoopResult<JourneyEncounterResolutionResult>`

- [ ] **Step 1: Move `ResolveJourneyEncounterDeterministic` into JourneyLoop**

Move `ResolveJourneyEncounterDeterministic` (lines 2291–2632) into `JourneyLoop` as `ResolveJourneyEncounter(context)`. Key adjustments:
- Replace `Player.Wallet.Cash` with `context.PlayerCash` (and track running wallet total for event production)
- Replace `Player.Health` with `context.PlayerHealth` (and track running health for event production)
- Replace `PursuitState.Heat` with `context.CurrentHeat`
- Replace `SpendFirearmAmmo(...)` calls with tracking ammo spent in the event payload (Apply handles `SpendFirearmAmmo`)
- Replace `Player.RemoveQuantity(kind, quantity)` with tracking stolen items in the event payload (Apply handles removal)
- Replace `ProduceEvent(new JourneyEncounterResolved { ... })` with collecting the event
- If the encounter resolution completes the travel day (calls `AdvanceJourneyDay`-like logic), collect those events too

- [ ] **Step 2: Move `ContinueCurrentDayAfterEncounterResolution` into JourneyLoop**

Move `ContinueCurrentDayAfterEncounterResolution` (lines 2672–2765) into `JourneyLoop`. This method continues the day plan iteration after an encounter is resolved. It may produce `TravelDayAdvanced` or `JourneyCompleted` events — collect them in the result.

- [ ] **Step 3: Rewire GameSession.ResolveJourneyEncounter to delegate to _journeyLoop**

Replace the body of the internal `ResolveJourneyEncounter` (lines 2865–2877) with:
```csharp
internal JourneyEncounterResolutionResult ResolveJourneyEncounter(
    string choiceId,
    int? bulletSpend,
    decimal? bribeAmount,
    ulong? forcedRoll)
{
    if (IsArchived)
    {
        return JourneyEncounterResolutionResult.Failed(ArchivedBlockMessage, JourneyStatus.Failed);
    }

    var context = new ResolveJourneyEncounterContext(
        choiceId,
        bulletSpend,
        bribeAmount,
        forcedRoll,
        TravelRules,
        Player.Wallet.Cash,
        Player.Health,
        PursuitState.Heat,
        Player.GetQuantity(ItemKind.Ammo));

    var result = _journeyLoop.ResolveJourneyEncounter(context);
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
    return result.Result;
}
```

- [ ] **Step 4: Build and run the encounter resolution test filter**

Run: `dotnet build WildBunch.sln`
Then:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TravelEncounter|FullyQualifiedName~TravelResolver|FullyQualifiedName~TravelReplay|FullyQualifiedName~TravelStateMachine"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 5: Commit**

```
git add src/WildBunch.Domain/Game/JourneyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-119: move ResolveJourneyEncounter decision logic to JourneyLoop"
```

### Task 8: Move Apply handlers into JourneyLoop and rewire GameSession dispatch

**Files:**
- Modify: `src/WildBunch.Domain/Game/JourneyLoop.cs` (add Apply methods for owned state)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (rewire Apply handlers to delegate owned-state mutations to `_journeyLoop.Apply(e)`)

**Interfaces:**
- Consumes: all journey/travel event types
- Produces: `JourneyLoop.Apply(...)` methods for owned-state mutations; GameSession retains cross-owner mutations + `_version++`

- [ ] **Step 1: Add Apply methods to JourneyLoop for owned-state mutations**

Add these Apply methods to `JourneyLoop`:

```csharp
internal void Apply(JourneyStarted e)
{
    _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
    _nextJourneySequence = e.JourneySnapshot.JourneySequence + 1;
    _travelDiaryDays.Clear();
}

internal void Apply(TravelDayAdvanced e)
{
    _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
}

internal void Apply(TrailEventApplied e)
{
    _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
}

internal void Apply(JourneyEncounterResolved e)
{
    _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
}

internal void Apply(JourneyCompleted e)
{
    _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
}

internal void Apply(JourneyArrivalAcknowledged e)
{
    _completedJourneyHistory.Add(e.JourneySnapshot);
    _journey = null;
}

internal void Apply(DevTravelOverrideForced e)
{
    _pendingDevTravelOverride = new DevTravelOverride(
        e.ForcedCategory,
        e.FoeProfile,
        e.EncounterMessage);
}

internal void Apply(DevTravelOverrideCleared e)
{
    _pendingDevTravelOverride = null;
}

internal void Apply(DevTravelOverrideConsumed e)
{
    _pendingDevTravelOverride = null;
}
```

Note: `Apply(TravelDayAdvanced)` in GameSession also calls `AppendTravelDiaryDay` / `PersistLatestTravelDiaryDay` for diary management. Check whether diary mutations happen in the Apply handler or in the command method. If diary mutations happen in the command method (Handle* methods), they stay in JourneyLoop command methods. If they happen in Apply, move them to `JourneyLoop.Apply(TravelDayAdvanced)`. Inspect the current Apply handler body to determine the exact split.

- [ ] **Step 2: Update GameSession Apply handlers to delegate owned-state mutations to JourneyLoop**

Update each `Apply` handler in `GameSession` to call `_journeyLoop.Apply(e)` for the owned-state portion while retaining cross-owner mutations and `_version++`. Example for `Apply(JourneyStarted)`:
```csharp
internal void Apply(JourneyStarted e)
{
    _journeyLoop.Apply(e);
    PursuitState.SetHeat(e.PursuitHeat);
    RecordTravelUpdate(e.DiaryMessage);
    _version++;
}
```

For `Apply(TravelDayAdvanced)`:
```csharp
internal void Apply(TravelDayAdvanced e)
{
    Clock.Set(e.Day, turn: 0);
    _journeyLoop.Apply(e);
    if (e.HealthDelta != 0)
        Player.AdjustHealth(e.HealthDelta);
    PursuitState.SetHeat(e.PursuitHeat);
    SyncPlayerFromJourneySnapshot(e.JourneySnapshot);
    foreach (var narration in e.AdditionalDiaryMessages)
        RecordTravelUpdate(narration);
    RecordTravelUpdate(e.DiaryMessage);
    if (!string.IsNullOrEmpty(e.HorseLostMessage))
        RecordTravelUpdate(e.HorseLostMessage);
    _version++;
}
```

For `Apply(TrailEventApplied)`:
```csharp
internal void Apply(TrailEventApplied e)
{
    _journeyLoop.Apply(e);
    Player.SetCash(e.WalletCash);
    PursuitState.SetHeat(e.PursuitHeat);
    SyncPlayerFromJourneySnapshot(e.JourneySnapshot);
    RecordTravelUpdate(e.DiaryMessage);
    if (!string.IsNullOrEmpty(e.HorseLostMessage))
        RecordTravelUpdate(e.HorseLostMessage);
    _version++;
}
```

For `Apply(JourneyEncounterResolved)`:
```csharp
internal void Apply(JourneyEncounterResolved e)
{
    _journeyLoop.Apply(e);
    Player.SetHealth(e.PlayerHealth);
    Player.SetCash(e.WalletCash);
    if (e.AmmoSpent > 0)
        SpendFirearmAmmo(e.AmmoSpent);
    if (e.StolenItemKind is { } kind && e.StolenItemQuantity > 0)
        Player.RemoveQuantity(kind, e.StolenItemQuantity);
    PursuitState.SetHeat(e.PursuitHeat);
    foreach (var narration in e.AdditionalDiaryMessages)
        RecordTravelUpdate(narration);
    RecordTravelUpdate(e.DiaryMessage);
    _version++;
}
```

For `Apply(JourneyCompleted)`:
```csharp
internal void Apply(JourneyCompleted e)
{
    _journeyLoop.Apply(e);
    Player.TravelTo(e.DestinationTownId);
    RefreshTownVisit(e.DestinationTownId);
    RefillCanteenAfterArrival();
    RecordTravelUpdate(e.DiaryMessage);
    _version++;
}
```

For `Apply(JourneyArrivalAcknowledged)`:
```csharp
internal void Apply(JourneyArrivalAcknowledged e)
{
    _journeyLoop.Apply(e);
    RecordTravelUpdate(e.DiaryMessage);
    _version++;
}
```

For the three dev travel override Apply handlers:
```csharp
internal void Apply(DevTravelOverrideForced e)
{
    _journeyLoop.Apply(e);
    _version++;
}

internal void Apply(DevTravelOverrideCleared e)
{
    _journeyLoop.Apply(e);
    _version++;
}

internal void Apply(DevTravelOverrideConsumed e)
{
    _journeyLoop.Apply(e);
    _version++;
}
```

- [ ] **Step 3: Build and run the event-sourcing + replay test filter**

Run: `dotnet build WildBunch.sln`
Then:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TravelReplay|FullyQualifiedName~TravelEvent|FullyQualifiedName~DevTravelOverride"
```
Expected: PASS with the same counts as Task 1. The replay/equality tests are the critical proof that Apply semantics are unchanged.

- [ ] **Step 4: Commit**

```
git add src/WildBunch.Domain/Game/JourneyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-119: move owned-state Apply handlers into JourneyLoop, rewire GameSession dispatch"
```

### Task 9: Update snapshot rehydration to construct JourneyLoop

**Files:**
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add internal rehydration method for JourneyLoop)

**Interfaces:**
- Consumes: existing snapshot record shape (unchanged)
- Produces: rehydration path that constructs `JourneyLoop` and sets it on `GameSession`

- [ ] **Step 1: Add internal rehydration method on GameSession for JourneyLoop**

Add to `GameSession`:
```csharp
internal void RestoreJourneyLoopState(IReadOnlyList<TravelDiaryDayState>? travelDiaryDays,
    DevTravelOverride? pendingDevTravelOverride)
{
    if (travelDiaryDays is not null)
    {
        _journeyLoop.RestoreTravelDiaryDays(travelDiaryDays);
    }
    if (pendingDevTravelOverride is not null)
    {
        _journeyLoop.RestorePendingDevTravelOverride(pendingDevTravelOverride);
    }
}
```

Note: The active `Journey` and `CompletedJourneyHistory` are already passed through the `GameSession` constructor (as `journey` and `completedJourneyHistory`), and `GameSession`'s constructor now constructs `JourneyLoop` from them. So no change needed for those — only travel diary days and pending dev travel override need restore methods.

- [ ] **Step 2: Update the snapshot ToDomain rehydration path**

In `GameSessionSnapshot.ToDomain()` (lines ~96–100 of `SessionSnapshot.cs`), replace the direct `GameSessionRehydrator.ReplaceTravelDiaryDays(session, TravelDiaryDays)` and `SetBackingField(session, "_pendingDevTravelOverride", ...)` calls with:
```csharp
session.RestoreJourneyLoopState(TravelDiaryDays, PendingDevTravelOverride);
```
Keep `ReplaceTravelDiaryDays` available if other callers use it, but search for remaining callers before removing.

- [ ] **Step 3: Update EfGameSessionRepository load path if needed**

Check `EfGameSessionRepository.cs` load path (lines ~376–392). If it uses `SetBackingField` for `_pendingDevTravelOverride` or `ReplaceTravelDiaryDays`, update those to call `session.RestoreJourneyLoopState(...)` instead. The journey and completed journey history are already passed through the constructor, so no change needed there.

- [ ] **Step 4: Build and run the persistence + integration test filter**

Run: `.\scripts\postgres-dev.ps1 ensure`
Then: `dotnet build WildBunch.sln`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~EfGameSessionRepository|FullyQualifiedName~Travel|FullyQualifiedName~Journey|FullyQualifiedName~DevTravel"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 5: Commit**

```
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs src/WildBunch.Persistence/Games/EfGameSessionRepository.cs
git commit -m "BUNCH-119: update snapshot rehydration to construct JourneyLoop from persisted state"
```

### Task 10: Remove dead code from GameSession and final validation

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (remove dead journey state fields, helper methods, and rewire public properties to delegate to `_journeyLoop`)

**Interfaces:**
- Consumes: all previous tasks
- Produces: a clean codebase with no dead journey code in GameSession

- [ ] **Step 1: Remove dead journey state fields from GameSession**

Search for remaining direct references to `_travelDiaryDays`, `_completedJourneyHistory`, `_nextJourneySequence`, and `_pendingDevTravelOverride` in `GameSession.cs`. If all access now goes through `_journeyLoop`, remove the fields. Update `GameSession`'s public properties to delegate:
```csharp
public TravelJourney? Journey => _journeyLoop.Journey;
public IReadOnlyList<TravelDiaryDayState> TravelDiaryDays => _journeyLoop.TravelDiaryDays;
public IReadOnlyList<TravelJourneySnapshot> CompletedJourneyHistory => _journeyLoop.CompletedJourneyHistory;
internal DevTravelOverride? PendingDevTravelOverride => _journeyLoop.PendingDevTravelOverride;
```

- [ ] **Step 2: Remove dead journey helper methods from GameSession**

Search for remaining references to the moved helper methods (`ApplyTrailEvent`, `ApplyHorseDelta`, `ApplyEncounterHorsePressure`, `PrepareTravelDayAdvance`, `HandleInterruptedTravelDay`, `HandleCompletedTravelDay`, `HandleOngoingTravelDay`, `AdvanceJourneyDayDeterministic`, `ResolveJourneyEncounterDeterministic`, `ContinueCurrentDayAfterEncounterResolution`, `CreateTravelDayGenerationContext`, pressure-band factories, `RecoverFoeProfile`, `RebuildCurrentTravelDiaryBaselineState`, `BuildJourneyOpeningNarration`, `CompleteJourneyAtDestination`, `AppendTravelDiaryDay`, `PersistLatestTravelDiaryDay`, `CalculateNextJourneySequence`, description helpers). Remove any that are no longer called from GameSession.

Keep `SyncPlayerFromJourneySnapshot` on GameSession — it mutates Player (cross-owner). Keep `RefreshTownVisit`, `RefillCanteenAfterArrival`, `RecordTravelUpdate`, `SpendFirearmAmmo`, `ApplyFoodDelta`, `ApplyCanteenChargeDelta` on GameSession — they are cross-owner helpers used by Apply handlers.

- [ ] **Step 3: Remove dead rehydrator methods**

Search for remaining callers of `GameSessionRehydrator.ReplaceTravelDiaryDays`. If no callers remain, remove the method and its reflection field cache. Search for `SetBackingField(session, "_pendingDevTravelOverride", ...)` callers — if none remain, the backing field is gone.

- [ ] **Step 4: Run falsification checks**

Run these checks and verify zero matches:
```
rg "GameSession" src/WildBunch.Domain/Game/JourneyLoop.cs
rg "ProduceEvent|EnterActionContext|Player\.AdjustCash|CaseFile\.Record|RecordCaseUpdate|CurrentTownVisit.*Set" src/WildBunch.Domain/Game/JourneyLoop.cs
```
Expected: zero matches for both. `JourneyLoop` must not reference `GameSession` or call any cross-owner mutation method.

- [ ] **Step 5: Run full validation**

Run: `.\scripts\postgres-dev.ps1 validate`
Expected: PASS — build, EF migrations, and all tests pass.

Run the full domain test suite:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj
```
Expected: PASS with the same counts as Task 1.

Run the full integration test suite:
```
dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 6: Record the line-count delta**

Count lines in `src/WildBunch.Domain/Game/GameSession.cs` and `src/WildBunch.Domain/Game/JourneyLoop.cs`. Record:
- GameSession: pre = 8,108 lines, post = ??? lines, delta = ??? lines
- JourneyLoop: ??? lines
- Expected: GameSession should be ~7,000 lines (reduced by ~1,100), JourneyLoop should be ~1,100+ lines.

- [ ] **Step 7: Regenerate the index mesh**

Run: `python scripts/generate_index_mesh.py`
Expected: INDEX.md files updated to reflect the new `JourneyLoop.cs` and `JourneyLoopContexts.cs` files. Commit the updated INDEX.md files.

- [ ] **Step 8: Commit**

```
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/JourneyLoop.cs src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs
git commit -m "BUNCH-119: remove dead journey code from GameSession, final validation"
```

Then regenerate and commit the index mesh:
```
python scripts/generate_index_mesh.py
git add **/INDEX.md
git commit -m "BUNCH-119: regenerate index mesh for JourneyLoop files"
```

---

## Falsification Checks

After all tasks complete, verify:

- `JourneyLoop` does NOT reference `GameSession` (zero code matches): `rg "GameSession" src/WildBunch.Domain/Game/JourneyLoop.cs`
- `JourneyLoop` does NOT call `ProduceEvent`/`EnterActionContext`/`Player.AdjustCash`/`CaseFile.Record*`/`RecordCaseUpdate`/`CurrentTownVisit.Set*` (zero code matches): `rg "ProduceEvent|EnterActionContext|Player\.|CaseFile\.|RecordCaseUpdate|CurrentTownVisit" src/WildBunch.Domain/Game/JourneyLoop.cs`
- `GameSession` no longer directly owns travel/journey decision rules (the methods listed in the Boundary Definition have moved)
- `GameSession` still controls guards, `EnterActionContext`, `ProduceEvent`, Apply dispatch, `_version++`
- All 194 journey/travel tests pass with same counts as baseline
- Snapshot shape is unchanged (same `JourneySnapshot`, `TravelDiaryDayState`, `DevTravelOverride` fields)
- Line-count delta recorded

## Validation

- `dotnet build WildBunch.sln`
- `dotnet test` (domain + application + integration, PostgreSQL lane via `.\scripts\postgres-dev.ps1 validate`)
- Falsification checks above
- Line-count delta recorded

## Dependencies

- Should land after BUNCH-112 (BountyLoop extraction, PR #131, merged at commit 2136584) — already on `main`.
- Should land before the aggregate-decomposition audit capstone.
