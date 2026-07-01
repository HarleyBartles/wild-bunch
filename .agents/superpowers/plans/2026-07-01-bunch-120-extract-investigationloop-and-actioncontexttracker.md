# BUNCH-120 Decompose Remaining Town Action Seams: InvestigationLoop + ActionContextTracker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Decompose the `GameSession` aggregate by extracting two child domain components from the town-action seams: a stateless `InvestigationLoop` that owns investigation source resolution and clue/warrant surfacing decision logic, and a stateful `ActionContextTracker` that owns town action-context state and turn-advancement tracking. Both receive narrow inputs via context records, return explicit results + events to produce, and do NOT reference `GameSession`. `GameSession` retains guards, `ProduceEvent`, Apply dispatch, and the persistence boundary.

**Architecture:** Extract ~460 lines of investigation decision logic (5 investigation methods + helpers) into a stateless `InvestigationLoop` class, and ~60 lines of action-context state/behavior into a stateful `ActionContextTracker` class. `ActionContextTracker` owns `CurrentActionContext` and `CurrentActionContextTownId`, receives narrow context records (Clock, PursuitState, CurrentTownId), returns `TownActionContextEntered` events, and owns its Apply handler for context state. `InvestigationLoop` is stateless — it receives context records (CaseFile, town slot index, visit count, SaltSource, retired warrant IDs, beat narration), returns `InvestigationPerformed` events, and does NOT own Apply handlers (the Apply handler stays on GameSession because it mutates cross-owner state: CurrentTown.CheckSource, CaseFile.RevealClueById, CaseFile.RevealWarrantById). `GameSession` retains: public command entry points (guards + orchestration + EnterActionContext delegation), `ProduceEvent`, Apply dispatch (calling `_actionContextTracker.Apply(e)` for owned-state mutations + applying cross-owner mutations itself), and the persistence/snapshot boundary. JSON snapshot shape is preserved — the same data fields are serialized; only the rehydration construction path changes to restore `ActionContextTracker` state.

**Tech Stack:** C# / .NET 10, xUnit, existing Wild Bunch domain tests.

## Global Constraints

- `GameSession` remains the live-play aggregate root and the only externally loaded/persisted root (ADR-0002, ADR-0020).
- `ActionContextTracker` is a child domain component inside the session boundary, NOT a separate aggregate root, NOT a standalone application service, and NOT a nested class with `_session` access. It is an `internal sealed` class in `WildBunch.Domain/Game/ActionContextTracker.cs` — internal because it is a session-internal component, not a public domain-service surface.
- `InvestigationLoop` is a stateless child domain component inside the session boundary. It is an `internal sealed` class in `WildBunch.Domain/Game/InvestigationLoop.cs`. It holds no mutable state — only the static stateless resolver instances (`WantedPosterResolver`, `ClueSurfacingResolver`) and static helper methods.
- Both components must NOT reference `GameSession` in any way — no field, no parameter, no method call. This is the key falsification check.
- Do NOT introduce separate persistence tables, repositories, or EF entities for either component. Keep JSON snapshot/runtime-session persistence. The snapshot record shape stays the same; only the rehydration construction path changes for `ActionContextTracker`.
- Do NOT change any public method signature on `GameSession`, DTO shape, result-object shape, message string, or event payload.
- Do NOT change clue, journal, wanted-poster, wallet, inventory, horse, bounty-loop, saloon, or travel state handling (out of scope).
- Preserve all existing domain event types and their `Apply(...)` mutation semantics. Event payloads and replay behavior must be identical.
- Neither component calls `ProduceEvent`, `EnterActionContext` (except `ActionContextTracker` which IS the action context), `Player.AdjustCash`, `CaseFile.Record*`/`Reveal*`, `CurrentTown.CheckSource`/`CheckWantedPosters`, or `CurrentTownVisit.CurrentTownState.Set*`. They return events as data; `GameSession` produces them and applies cross-owner mutations.
- `InvestigationLoop` does NOT own an Apply handler — `Apply(InvestigationPerformed)` stays on `GameSession` because it mutates cross-owner state (`CurrentTown`, `CaseFile`). `InvestigationLoop` is purely decision logic that produces the event; `GameSession` produces it and applies the cross-owner mutations.
- `ActionContextTracker` owns its Apply handler for `TownActionContextEntered` — it sets `CurrentActionContext` and `CurrentActionContextTownId`. `GameSession`'s `Apply(TownActionContextEntered)` calls `_actionContextTracker.Apply(e)` for the owned portion, then applies cross-owner mutations (`Clock.Set`, `PursuitState.SetHeat`, `_version++`).
- Run `dotnet build` and `dotnet test` after every task. Run `.\scripts\postgres-dev.ps1 validate` only if a task touches persistence (Task 6 does).
- If any task reveals a behavior change, STOP and report — do not "fix" it silently.
- **BUNCH-119 dependency:** This plan branches from `origin/main` where BUNCH-119 (JourneyLoop) implementation has NOT landed — only its plan is merged. This plan does NOT touch travel/journey state, so it composes cleanly with BUNCH-119 post-merge shape. If BUNCH-119 has landed when implementation starts, recheck the plan against its final shape — the `GameSession` line numbers will have shifted but the investigation/action-context methods are unaffected. **BUNCH-120 should land after BUNCH-119.** If BUNCH-119 and BUNCH-120 are both in flight, BUNCH-120 branches from the state that includes BUNCH-119.
- **BUNCH-5 coordination:** BUNCH-5 (time-of-day action beats, PR #133) has landed. It added `BeatNarration.Render(...)` calls to the investigation methods and a `BeatNarration` field to `CaseInvestigationResult`. This plan preserves the beat narration flow: `GameSession` captures `beatSpent` before `EnterActionContext`, renders the beat narration, and passes it via the context record to `InvestigationLoop`, which includes it in the result. The `ActionContextTracker` extraction does NOT change the beat model — `EnterActionContext` still computes the same clock/heat advancement and returns the same `TownActionContextEntered` event.
- **BUNCH-111 note:** The issue description mentions `RecordCaseUpdate` as a helper that would move. `RecordCaseUpdate` no longer exists — BUNCH-111 (PR #137) removed it as part of the event-sourcing migration. The investigation methods now produce `InvestigationPerformed` events directly. This plan accounts for that: the helpers that move to `InvestigationLoop` are `DescribeClueLead`, `IsPlayerKnownClue`, `CurrentTownSlotIndex`, and `CurrentTownVisitCount` (all still present).

---

## Boundary Definition

### What `ActionContextTracker` owns (state + invariants + Apply)

| State | Current location | Moves to `ActionContextTracker` |
| --- | --- | --- |
| `TownActionContext CurrentActionContext` | `GameSession.CurrentActionContext` (line 254) | Yes — `ActionContextTracker.CurrentActionContext` |
| `TownId? CurrentActionContextTownId` | `GameSession.CurrentActionContextTownId` (line 262) | Yes — `ActionContextTracker.CurrentActionContextTownId` |

| Behavior | Current location | Moves to `ActionContextTracker` |
| --- | --- | --- |
| Enter action context decision logic | `GameSession.EnterActionContext` body (lines 275–311) | Yes — `ActionContextTracker.EnterActionContext(context)` returns event or null |
| Can confront wanted suspect in current context | `GameSession.CanConfrontWantedSuspectInCurrentContext` (lines 328–342) | Yes — `ActionContextTracker.CanConfrontWantedSuspectInCurrentContext(context)` |
| Reset action context for town change | `GameSession.ResetActionContextForTownChange` (lines 1629–1633) | Yes — `ActionContextTracker.Reset()` |
| Apply owned state for `TownActionContextEntered` | `GameSession.Apply(TownActionContextEntered)` owned portion (lines 455–456) | Yes — `ActionContextTracker.Apply(TownActionContextEntered)` sets `CurrentActionContext` + `CurrentActionContextTownId` |

### What `InvestigationLoop` owns (stateless decision logic)

| Behavior | Current location | Moves to `InvestigationLoop` |
| --- | --- | --- |
| Read wanted posters decision logic | `GameSession.ReadWantedPosters` body (lines 2987–3095) | Yes — `InvestigationLoop.ReadWantedPosters(context)` |
| Follow telegraph leads decision logic | `GameSession.FollowTelegraphLeads` body (lines 3451–3522) | Yes — `InvestigationLoop.FollowTelegraphLeads(context)` |
| Gather local gossip decision logic | `GameSession.GatherLocalGossip` body (lines 3524–3590) | Yes — `InvestigationLoop.GatherLocalGossip(context)` |
| Inspect notice board decision logic | `GameSession.InspectNoticeBoard` body (lines 3592–3649) | Yes — `InvestigationLoop.InspectNoticeBoard(context)` |
| Check sheriff records decision logic | `GameSession.CheckSheriffRecords` body (lines 3651–3708) | Yes — `InvestigationLoop.CheckSheriffRecords(context)` |
| Describe clue lead helper | `GameSession.DescribeClueLead` (lines 3831–3832) | Yes — `InvestigationLoop.DescribeClueLead` (static) |
| Is player known clue helper | `GameSession.IsPlayerKnownClue` (lines 3843–3855) | Yes — `InvestigationLoop.IsPlayerKnownClue` (static) |
| Current town slot index helper | `GameSession.CurrentTownSlotIndex` (lines 3861–3878) | No — stays on `GameSession` as a context input. It reads `World.Towns` which is session-level state. `InvestigationLoop` receives the value via context record. |
| Current town visit count helper | `GameSession.CurrentTownVisitCount` (line 3884) | No — stays on `GameSession` as a context input. It reads `CurrentTownVisit` which is session-level state. `InvestigationLoop` receives the value via context record. |
| Wanted poster resolver | `GameSession._wantedPosterResolver` (line 39, static) | Yes — `InvestigationLoop._wantedPosterResolver` (static) |
| Clue surfacing resolver | `GameSession._clueSurfacingResolver` (line 40, static) | Yes — `InvestigationLoop._clueSurfacingResolver` (static) |

### What stays owned by existing domain objects (both components read via context, do NOT mutate)

| State | Owner | Component access |
| --- | --- | --- |
| `CaseFile` (suspects, warrants, clues) | `CaseFile` | Read-only via context record fields |
| `CurrentTownVisit.CurrentTownState` (spent sources, visit number) | `TownVisitState` | Read via context; GameSession mutates from Apply |
| `CurrentTown` (town ID, town name, source availability) | `TownAggregate` | Read via context; GameSession mutates from Apply |
| `Clock` (Day, Turn, TimeOfDay) | `GameClock` | Read via context; GameSession uses in Apply |
| `PursuitState` (Heat) | `PursuitState` | Read via context; GameSession uses in Apply |
| `SaltSource` | `GameSession` | Read salt via context |
| `Player.Wallet` | `Player` | Not accessed by either component |
| Retired warrant IDs | `BountyLoop.UnrelatedCriminalLedger` | Read via context record |

### What only `GameSession` may orchestrate

- `IsArchived` / `IsJourneyModal` guards
- `EnterActionContext` public API (delegates to `_actionContextTracker`, produces returned event)
- `ProduceEvent` (calls Apply + adds to uncommitted events)
- `Apply` dispatch switch
- Cross-owner mutations in Apply handlers: `CurrentTown.CheckSource`/`CheckWantedPosters`, `CaseFile.RevealClueById`/`RevealWarrantById`, `Clock.Set`, `PursuitState.SetHeat`, `_version++`
- `BeatNarration.Render` calls (GameSession captures `beatSpent` before `EnterActionContext`, renders narration, passes via context)
- Snapshot serialization/deserialization coordination

---

## Context Records and Result Types

### ActionContextTracker context and result types

All types live in `WildBunch.Domain/Game/ActionContextTracker.cs`.

```csharp
/// <summary>Read-only inputs for an enter-action-context decision.</summary>
internal sealed record ActionContextEnterInputs(
    GameClock Clock,
    PursuitState PursuitState,
    TownId CurrentTownId);

/// <summary>Read-only inputs for a can-confront-wanted-suspect check.</summary>
internal sealed record CanConfrontInContextInputs(
    SuspectId TargetSuspectId,
    TownId CurrentTownId,
    SuspectId? ActiveSaloonPersonOfInterestId);
```

`ActionContextTracker.EnterActionContext` returns `TownActionContextEntered?` (null = no-op). `ActionContextTracker.CanConfrontWantedSuspectInCurrentContext` returns `bool`.

### InvestigationLoop context and result types

All types live in `WildBunch.Domain/Game/InvestigationLoop.cs`.

```csharp
/// <summary>
/// Read-only inputs for an investigation decision. All five investigation methods
/// share this context record; each method uses the fields it needs.
/// </summary>
internal sealed record InvestigationContext(
    CaseFile CaseFile,
    int CurrentTownSlotIndex,
    int CurrentTownVisitCount,
    SaltSource? SaltSource,  // null = boring mode (SaltSourceMode.Fixed)
    IReadOnlySet<Guid> RetiredWarrantIds,  // from BountyLoop.UnrelatedCriminalLedger
    TownId CurrentTownId,
    string CurrentTownName,
    string? BeatNarration,  // null for ReadWantedPosters (no beat narration in result)
    bool IsSourceSpent,  // CurrentTownVisit.WantedPostersSpent or IsSpent(sourceKind)
    bool IsSourceAvailable);  // CurrentTown.IsAvailable(sourceKind); true for sources always available

/// <summary>
/// Result of an investigation decision. Contains the event to produce and the
/// display message for the player-facing result. GameSession produces the event
/// and wraps the display message in the appropriate result type.
/// </summary>
internal sealed record InvestigationOutcome(
    InvestigationPerformed Event,
    string DisplayMessage);
```

---

## File Structure

| File | Action | Responsibility |
| --- | --- | --- |
| `src/WildBunch.Domain/Game/ActionContextTracker.cs` | Create | ActionContextTracker class, context/result types |
| `src/WildBunch.Domain/Game/InvestigationLoop.cs` | Create | InvestigationLoop class, context/result types |
| `src/WildBunch.Domain/Game/GameSession.cs` | Modify | Rewire command methods to delegate to tracker/loop; rewire Apply dispatch; remove moved state/helpers |
| `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs` | Modify | Replace `SetCurrentActionContext` with `RestoreActionContextState` call |
| `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` | Modify | Update rehydration to call `RestoreActionContextState` |

---

## Task 1: Characterize the boundary and capture a regression baseline

**Files:**
- Read: `src/WildBunch.Domain/Game/GameSession.cs`
- Read: `src/WildBunch.Domain/Game/BountyLoop.cs`
- Read: `tests/WildBunch.Domain.Tests/GameSessionInvestigationActionsTests.cs`
- Read: `tests/WildBunch.Domain.Tests/InvestigationEventSourcingTests.cs`
- Read: `tests/WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs`
- Read: `tests/WildBunch.Domain.Tests/BeatModelEconomyTests.cs`

**Interfaces:**
- Consumes: nothing (baseline capture)
- Produces: baseline test counts and line counts for falsification checks

- [ ] **Step 1: Capture GameSession.cs line count**

Run: `(Get-Content src/WildBunch.Domain/Game/GameSession.cs).Count`
Expected: `4006` (record exact value — this is the baseline)

- [ ] **Step 2: Capture baseline test counts**

Run: `dotnet test WildBunch.sln --no-build 2>&1 | Select-String "Passed:|Failed:|Skipped:"`
Expected: Record exact passed/failed/skipped counts. If build is needed, run `dotnet build` first.

- [ ] **Step 3: Capture specific test file counts**

Run: `dotnet test WildBunch.sln --filter "FullyQualifiedName~GameSessionInvestigationActionsTests|FullyQualifiedName~InvestigationEventSourcingTests|FullyQualifiedName~ClockTurnCorrectionTests|FullyQualifiedName~BeatModelEconomyTests" --no-build 2>&1 | Select-String "Passed:|Failed:|Skipped:"`
Expected: Record exact counts for these test files — they are the regression baseline for the extraction.

- [ ] **Step 4: Confirm the methods to extract are present and line-numbered**

Verify these methods exist at the expected locations in `GameSession.cs`:
- `EnterActionContext` (line ~275)
- `CanConfrontWantedSuspectInCurrentContext` (line ~328)
- `ResetActionContextForTownChange` (line ~1629)
- `Apply(TownActionContextEntered)` (line ~453)
- `ReadWantedPosters` (line ~2987)
- `FollowTelegraphLeads` (line ~3451)
- `GatherLocalGossip` (line ~3524)
- `InspectNoticeBoard` (line ~3592)
- `CheckSheriffRecords` (line ~3651)
- `DescribeClueLead` (line ~3831)
- `IsPlayerKnownClue` (line ~3843)
- `CurrentTownSlotIndex` (line ~3861)
- `CurrentTownVisitCount` (line ~3884)
- `Purchase` (line ~2927)
- `CanPurchaseInventoryItem` (line ~3928)
- `IsStackableItemKind` (line ~3970)

- [ ] **Step 5: Commit baseline evidence**

```bash
git add -A
git commit -m "BUNCH-120: capture regression baseline for InvestigationLoop + ActionContextTracker extraction"
```

---

## Task 2: Create the ActionContextTracker class skeleton with owned state and context/result types

**Files:**
- Create: `src/WildBunch.Domain/Game/ActionContextTracker.cs`

**Interfaces:**
- Consumes: `TownActionContext`, `TownId`, `GameClock`, `PursuitState`, `SuspectId`, `TownActionContextEntered` event
- Produces: `ActionContextTracker` class with `CurrentActionContext`, `CurrentActionContextTownId` properties, `EnterActionContext`, `CanConfrontWantedSuspectInCurrentContext`, `Reset`, `Apply(TownActionContextEntered)` methods, and context record types

- [ ] **Step 1: Create the ActionContextTracker.cs file**

```csharp
using WildBunch.Domain.Events;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Child domain component inside the GameSession boundary that owns town action-context
/// state and turn-advancement tracking. Receives narrow context records, returns events
/// to produce. Does NOT reference GameSession, produce events directly, enter action
/// context (it IS the action context), or mutate Clock/PursuitState/CaseFile/Player.
/// See BUNCH-120 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class ActionContextTracker
{
    internal TownActionContext CurrentActionContext { get; private set; } = TownActionContext.None;
    internal TownId? CurrentActionContextTownId { get; private set; }

    /// <summary>
    /// Decides whether entering the given context produces a TownActionContextEntered event.
    /// Returns the event to produce, or null if no-op (None context, or same context in same town).
    /// Does NOT mutate Clock or PursuitState — the event carries the computed values and
    /// GameSession's Apply handler sets them.
    /// </summary>
    internal TownActionContextEntered? EnterActionContext(
        TownActionContext context,
        ActionContextEnterInputs inputs)
    {
        if (context == TownActionContext.None)
        {
            return null;
        }

        // Same context only suppresses time advancement if it was entered in the same town.
        if (context == CurrentActionContext && inputs.CurrentTownId.Equals(CurrentActionContextTownId))
        {
            return null;
        }

        // Compute resulting clock state (do NOT mutate Clock directly — Apply does that).
        var newTurn = inputs.Clock.Turn + 1;
        var newDay = inputs.Clock.Day;
        var newHeat = inputs.PursuitState.Heat;
        if (newTurn >= 4)
        {
            newDay++;
            newTurn = 0;
            // A full day passed in town — heat increases by 1 (lawman pressure).
            newHeat = inputs.PursuitState.Heat + 1;
        }

        return new TownActionContextEntered
        {
            Context = context,
            TownId = inputs.CurrentTownId,
            Day = newDay,
            Turn = newTurn,
            TimeOfDay = (TimeOfDay)newTurn,
            PursuitHeat = newHeat
        };
    }

    /// <summary>
    /// Named predicate expressing the invariant for direct wanted-suspect confrontation:
    /// confrontation itself does not advance time and is only valid when the player is
    /// already in an appropriate active POI/location context. For this first version the
    /// only supported confrontation context is the saloon POI loop.
    /// </summary>
    internal bool CanConfrontWantedSuspectInCurrentContext(CanConfrontInContextInputs inputs)
    {
        if (CurrentActionContext != TownActionContext.Saloon)
        {
            return false;
        }

        if (CurrentActionContextTownId is null || !CurrentActionContextTownId.Equals(inputs.CurrentTownId))
        {
            return false;
        }

        return inputs.ActiveSaloonPersonOfInterestId is not null
            && inputs.ActiveSaloonPersonOfInterestId.Equals(inputs.TargetSuspectId);
    }

    /// <summary>
    /// Resets CurrentActionContext and CurrentActionContextTownId to None/null.
    /// Called by GameSession.RefreshTownVisit when the current town changes.
    /// </summary>
    internal void Reset()
    {
        CurrentActionContext = TownActionContext.None;
        CurrentActionContextTownId = null;
    }

    /// <summary>
    /// Applies a TownActionContextEntered event to mutate owned state.
    /// GameSession's Apply handler calls this for the owned portion, then applies
    /// cross-owner mutations (Clock.Set, PursuitState.SetHeat, _version++).
    /// </summary>
    internal void Apply(TownActionContextEntered e)
    {
        CurrentActionContext = e.Context;
        CurrentActionContextTownId = e.TownId;
    }

    /// <summary>
    /// Restores owned state from a persisted snapshot. Called by GameSession during
    /// rehydration after the constructor builds a fresh ActionContextTracker.
    /// </summary>
    internal void RestoreState(TownActionContext context, TownId? townId)
    {
        CurrentActionContext = context;
        CurrentActionContextTownId = townId;
    }
}

/// <summary>Read-only inputs for an enter-action-context decision.</summary>
internal sealed record ActionContextEnterInputs(
    GameClock Clock,
    PursuitState PursuitState,
    TownId CurrentTownId);

/// <summary>Read-only inputs for a can-confront-wanted-suspect check.</summary>
internal sealed record CanConfrontInContextInputs(
    SuspectId TargetSuspectId,
    TownId CurrentTownId,
    SuspectId? ActiveSaloonPersonOfInterestId);
```

- [ ] **Step 2: Build to verify the new file compiles**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: PASS (the class is not yet referenced by GameSession, but it must compile standalone)

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Domain/Game/ActionContextTracker.cs
git commit -m "BUNCH-120: add ActionContextTracker class skeleton with owned state and context types"
```

---

## Task 3: Move EnterActionContext decision logic into ActionContextTracker

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (lines 34, 254–311)

**Interfaces:**
- Consumes: `ActionContextTracker.EnterActionContext` from Task 2
- Produces: `GameSession.EnterActionContext` delegates to `_actionContextTracker`, produces returned event

- [ ] **Step 1: Add the ActionContextTracker field to GameSession**

In `GameSession.cs`, after the `_bountyLoop` field (line 34), add:

```csharp
    private readonly BountyLoop _bountyLoop;
    private readonly ActionContextTracker _actionContextTracker = new();
    private DevTravelOverride? _pendingDevTravelOverride;
```

- [ ] **Step 2: Replace the CurrentActionContext and CurrentActionContextTownId properties with delegate properties**

Replace lines 254–262:

```csharp
    public TownActionContext CurrentActionContext => _actionContextTracker.CurrentActionContext;

    public TownId? CurrentActionContextTownId => _actionContextTracker.CurrentActionContextTownId;
```

Remove the old `private set` backing — these are now read-only delegates.

- [ ] **Step 3: Rewrite EnterActionContext to delegate to the tracker**

Replace the entire `EnterActionContext` method body (lines 275–311) with:

```csharp
    public bool EnterActionContext(TownActionContext context)
    {
        var inputs = new ActionContextEnterInputs(Clock, PursuitState, CurrentTown.TownId);
        var e = _actionContextTracker.EnterActionContext(context, inputs);
        if (e is null)
        {
            return false;
        }

        ProduceEvent(e);
        return true;
    }
```

- [ ] **Step 4: Build and run tests**

Run: `dotnet build WildBunch.sln`
Expected: PASS

Run: `dotnet test WildBunch.sln --filter "FullyQualifiedName~ClockTurnCorrectionTests|FullyQualifiedName~BeatModelEconomyTests" --no-build`
Expected: PASS with same counts as baseline (Task 1 Step 3)

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-120: move EnterActionContext decision logic into ActionContextTracker"
```

---

## Task 4: Move CanConfrontWantedSuspectInCurrentContext and ResetActionContextForTownChange into ActionContextTracker

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (lines 328–342, 1629–1633)

**Interfaces:**
- Consumes: `ActionContextTracker.CanConfrontWantedSuspectInCurrentContext`, `ActionContextTracker.Reset` from Task 2
- Produces: `GameSession.CanConfrontWantedSuspectInCurrentContext` and `ResetActionContextForTownChange` delegate to tracker

- [ ] **Step 1: Rewrite CanConfrontWantedSuspectInCurrentContext to delegate**

Replace the method body (lines 328–342) with:

```csharp
    public bool CanConfrontWantedSuspectInCurrentContext(SuspectId targetSuspectId)
    {
        var activeSaloonPoiId = CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId;
        var inputs = new CanConfrontInContextInputs(targetSuspectId, CurrentTown.TownId, activeSaloonPoiId);
        return _actionContextTracker.CanConfrontWantedSuspectInCurrentContext(inputs);
    }
```

- [ ] **Step 2: Rewrite ResetActionContextForTownChange to delegate**

Replace the method body (lines 1629–1633) with:

```csharp
    internal void ResetActionContextForTownChange() => _actionContextTracker.Reset();
```

- [ ] **Step 3: Build and run tests**

Run: `dotnet build WildBunch.sln`
Expected: PASS

Run: `dotnet test WildBunch.sln --filter "FullyQualifiedName~GameSessionWantedSuspectConfrontationTests|FullyQualifiedName~ClockTurnCorrectionTests" --no-build`
Expected: PASS with same counts as baseline

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-120: move CanConfrontWantedSuspectInCurrentContext and ResetActionContextForTownChange into ActionContextTracker"
```

---

## Task 5: Move owned-state Apply handler into ActionContextTracker and rewire GameSession dispatch

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (lines 453–460)

**Interfaces:**
- Consumes: `ActionContextTracker.Apply(TownActionContextEntered)` from Task 2
- Produces: `GameSession.Apply(TownActionContextEntered)` calls `_actionContextTracker.Apply(e)` then applies cross-owner mutations

- [ ] **Step 1: Rewrite Apply(TownActionContextEntered) to delegate owned state and keep cross-owner mutations**

Replace the method body (lines 453–460) with:

```csharp
    private void Apply(TownActionContextEntered e)
    {
        _actionContextTracker.Apply(e);
        Clock.Set(e.Day, e.Turn);
        PursuitState.SetHeat(e.PursuitHeat);
        _version++;
    }
```

- [ ] **Step 2: Build and run tests**

Run: `dotnet build WildBunch.sln`
Expected: PASS

Run: `dotnet test WildBunch.sln --filter "FullyQualifiedName~ClockTurnCorrectionTests|FullyQualifiedName~InvestigationEventSourcingTests|FullyQualifiedName~BeatModelEconomyTests|FullyQualifiedName~GameSessionEventSourcingTests" --no-build`
Expected: PASS with same counts as baseline

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-120: move owned-state Apply(TownActionContextEntered) into ActionContextTracker and rewire dispatch"
```

---

## Task 6: Update snapshot rehydration for ActionContextTracker

**Files:**
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs` (lines 90–100)
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` (lines 82–83)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add RestoreActionContextState method)

**Interfaces:**
- Consumes: `ActionContextTracker.RestoreState` from Task 2
- Produces: `GameSession.RestoreActionContextState` internal method; rehydration calls it instead of `SetCurrentActionContext`

- [ ] **Step 1: Add RestoreActionContextState method to GameSession**

In `GameSession.cs`, near the `RestoreBountyLoopState` method (after line ~120), add:

```csharp
    /// <summary>
    /// Restores ActionContextTracker-owned state from a persisted snapshot. Called by the
    /// rehydration path after the constructor builds a fresh ActionContextTracker.
    /// See BUNCH-120.
    /// </summary>
    internal void RestoreActionContextState(TownActionContext context, TownId? townId)
    {
        _actionContextTracker.RestoreState(context, townId);
    }
```

- [ ] **Step 2: Update GameSessionRehydrator to use RestoreActionContextState**

In `GameSessionRehydrator.cs`, replace the `SetCurrentActionContext` method (lines 90–100) with:

```csharp
    /// <summary>
    /// Restores the session's ActionContextTracker-owned state (CurrentActionContext and
    /// the town it was entered in) when loading from snapshot. These are also reconstructed
    /// from event replay via Apply(TownActionContextEntered). Both paths (snapshot load +
    /// event replay) must produce the same values. See ADR-0028, BUNCH-80, and BUNCH-120.
    /// </summary>
    public static void RestoreActionContextState(GameSession session, TownActionContext context, TownId? townId)
    {
        session.RestoreActionContextState(context, townId);
    }
```

- [ ] **Step 3: Update GameSessionJsonSerializer.SessionSnapshot to call RestoreActionContextState**

In `GameSessionJsonSerializer.SessionSnapshot.cs`, replace lines 82–83:

```csharp
            TownId? contextTownId = CurrentActionContextTownId is null ? null : new TownId(CurrentActionContextTownId);
            GameSessionRehydrator.RestoreActionContextState(session, CurrentActionContext, contextTownId);
```

- [ ] **Step 4: Build and run tests**

Run: `dotnet build WildBunch.sln`
Expected: PASS

Run: `dotnet test WildBunch.sln --filter "FullyQualifiedName~EventStorePersistenceTests|FullyQualifiedName~EventSourcingEndToEndTests|FullyQualifiedName~GameSessionDifficultyPersistenceTests" --no-build`
Expected: PASS with same counts as baseline

- [ ] **Step 5: Run PostgreSQL-backed validation**

Run: `.\scripts\postgres-dev.ps1 validate`
Expected: PASS (EF migrations list + full test suite)

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs
git commit -m "BUNCH-120: update snapshot rehydration to restore ActionContextTracker state"
```

---

## Task 7: Create the InvestigationLoop class skeleton with context/result types

**Files:**
- Create: `src/WildBunch.Domain/Game/InvestigationLoop.cs`

**Interfaces:**
- Consumes: `CaseFile`, `SaltSource`, `WantedPosterResolver`, `ClueSurfacingResolver`, `InvestigationPerformed` event, `InvestigationSourceKind`, `Clue`, `Warrant`, `TownId`
- Produces: `InvestigationLoop` class (stateless), `InvestigationContext` record, `InvestigationOutcome` record

- [ ] **Step 1: Create the InvestigationLoop.cs file with the skeleton, context/result types, and static helpers**

```csharp
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.WantedPosters;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Stateless child domain component inside the GameSession boundary that owns investigation
/// source resolution and clue/warrant surfacing decision logic. Receives narrow context records,
/// returns InvestigationPerformed events for GameSession to produce. Does NOT reference
/// GameSession, produce events directly, enter action context, adjust cash, or mutate
/// CaseFile/CurrentTown/TownVisitState/Player. See BUNCH-120 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class InvestigationLoop
{
    // Stateless domain-service resolvers for investigation surfacing.
    // BUNCH-107: replace ordered-peek selection with town/visit-aware resolver selection.
    private static readonly WantedPosterResolver _wantedPosterResolver = new();
    private static readonly ClueSurfacingResolver _clueSurfacingResolver = new();

    // Command methods — filled in by Tasks 8–9

    /// <summary>
    /// Trims and strips trailing punctuation from a clue description for display in lead messages.
    /// </summary>
    internal static string DescribeClueLead(string description)
        => description.Trim().TrimEnd('.', '!', '?');

    /// <summary>
    /// A clue is "player known" if it is a warrant, alias, identity fact, or culprit trail clue,
    /// or if any of its anchor subjects have a non-blank alias or feature. Used to filter which
    /// clues surface from investigation sources.
    /// </summary>
    internal static bool IsPlayerKnownClue(Clue clue)
    {
        ArgumentNullException.ThrowIfNull(clue);

        if (clue.Kind is ClueKind.Warrant or ClueKind.Alias or ClueKind.IdentityFact or ClueKind.CulpritTrail)
        {
            return true;
        }

        return clue.Anchors.Subjects.Any(subject =>
            !string.IsNullOrWhiteSpace(subject.Alias)
            || !string.IsNullOrWhiteSpace(subject.Feature));
    }
}

/// <summary>
/// Read-only inputs for an investigation decision. All five investigation methods share this
/// context record; each method uses the fields it needs.
/// </summary>
internal sealed record InvestigationContext(
    CaseFile CaseFile,
    int CurrentTownSlotIndex,
    int CurrentTownVisitCount,
    SaltSource? SaltSource,  // null = boring mode (SaltSourceMode.Fixed)
    IReadOnlySet<Guid> RetiredWarrantIds,  // from BountyLoop.UnrelatedCriminalLedger
    TownId CurrentTownId,
    string CurrentTownName,
    string? BeatNarration,  // null for ReadWantedPosters (no beat narration in result)
    bool IsSourceSpent,  // CurrentTownVisit.WantedPostersSpent or IsSpent(sourceKind)
    bool IsSourceAvailable);  // CurrentTown.IsAvailable(sourceKind); true for sources always available

/// <summary>
/// Result of an investigation decision. Contains the event to produce and the display message
/// for the player-facing result. GameSession produces the event and wraps the display message
/// in the appropriate result type (ReadWantedPostersResult or CaseInvestigationResult).
/// </summary>
internal sealed record InvestigationOutcome(
    InvestigationPerformed Event,
    string DisplayMessage);
```

- [ ] **Step 2: Build to verify the new file compiles**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Domain/Game/InvestigationLoop.cs
git commit -m "BUNCH-120: add InvestigationLoop class skeleton with context/result types and static helpers"
```

---

## Task 8: Move ReadWantedPosters decision logic into InvestigationLoop

**Files:**
- Modify: `src/WildBunch.Domain/Game/InvestigationLoop.cs` (add `ReadWantedPosters` method)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (lines 2987–3095, 39–40, 3831–3832, 3843–3855)

**Interfaces:**
- Consumes: `InvestigationContext`, `InvestigationOutcome` from Task 7
- Produces: `InvestigationLoop.ReadWantedPosters(context)` returns `InvestigationOutcome`; `GameSession.ReadWantedPosters` delegates and produces event

- [ ] **Step 1: Add ReadWantedPosters method to InvestigationLoop**

In `InvestigationLoop.cs`, replace the `// Command methods — filled in by Tasks 8–9` comment with:

```csharp
    /// <summary>
    /// Read wanted posters decision logic. Resolves a warrant and/or clue from the wanted
    /// poster resolver and clue surfacing resolver. Returns the InvestigationPerformed event
    /// and display message. GameSession produces the event and wraps the display message in
    /// a ReadWantedPostersResult.
    /// </summary>
    internal InvestigationOutcome ReadWantedPosters(InvestigationContext context)
    {
        if (context.IsSourceSpent)
        {
            var msg = "You study the wanted posters again, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.SheriffWarrants,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var warrant = _wantedPosterResolver.Resolve(
            context.CaseFile,
            context.CurrentTownSlotIndex,
            context.CurrentTownVisitCount,
            context.SaltSource,
            context.RetiredWarrantIds.Count > 0 ? context.RetiredWarrantIds : null);
        var clue = _clueSurfacingResolver.Resolve(
            context.CaseFile,
            InvestigationSourceKind.SheriffWarrants,
            context.CurrentTownSlotIndex,
            context.CurrentTownVisitCount,
            context.SaltSource);
        if (clue is not null && !IsPlayerKnownClue(clue))
        {
            clue = null;
        }

        if (warrant is null && clue is null)
        {
            var msg = "You study the wanted posters, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.SheriffWarrants,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        if (warrant is not null && clue is not null)
        {
            var msg = $"You study the wanted posters and copy down a wanted notice for {warrant.TargetName}, noting a public lead: {DescribeClueLead(clue.Description)}.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.SheriffWarrants,
                    TownId = context.CurrentTownId,
                    Message = msg,
                    ClueId = clue?.Id,
                    WarrantId = warrant?.Id
                },
                "You study the wanted posters and uncover a wanted notice and a public lead.");
        }

        if (warrant is not null)
        {
            var msg = $"You study the wanted posters and copy down a wanted notice for {warrant.TargetName}.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.SheriffWarrants,
                    TownId = context.CurrentTownId,
                    Message = msg,
                    WarrantId = warrant?.Id
                },
                msg);
        }

        var clueOnlyMsg = $"You study the wanted posters and note a public lead: {DescribeClueLead(clue!.Description)}.";
        return new InvestigationOutcome(
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.SheriffWarrants,
                TownId = context.CurrentTownId,
                Message = clueOnlyMsg,
                ClueId = clue?.Id
            },
            "You study the wanted posters and uncover a public lead.");
    }
```

- [ ] **Step 2: Rewrite GameSession.ReadWantedPosters to delegate to InvestigationLoop**

Replace the entire method body (lines 2987–3095) with:

```csharp
    public ReadWantedPostersResult ReadWantedPosters()
    {
        if (IsArchived)
        {
            return ReadWantedPostersResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return ReadWantedPostersResult.Failed(JourneyModalBlockMessage);
        }

        EnterActionContext(TownActionContext.SheriffOffice);

        var boringSalt = SaltSource.Mode == SaltSourceMode.Fixed ? null : SaltSource;
        var context = new InvestigationContext(
            CaseFile,
            CurrentTownSlotIndex,
            CurrentTownVisitCount,
            boringSalt,
            RetiredWarrantIds,
            CurrentTown.TownId,
            CurrentTown.TownName,
            BeatNarration: null,
            IsSourceSpent: CurrentTownVisit.WantedPostersSpent,
            IsSourceAvailable: true);
        var outcome = _investigationLoop.ReadWantedPosters(context);
        ProduceEvent(outcome.Event);
        return ReadWantedPostersResult.Succeeded(outcome.DisplayMessage, sessionChanged: true);
    }
```

- [ ] **Step 3: Add the InvestigationLoop field to GameSession**

In `GameSession.cs`, after the `_actionContextTracker` field, add:

```csharp
    private readonly ActionContextTracker _actionContextTracker = new();
    private readonly InvestigationLoop _investigationLoop = new();
```

- [ ] **Step 4: Remove the static resolver fields from GameSession**

Remove lines 39–40 (the `_wantedPosterResolver` and `_clueSurfacingResolver` static fields and their comment). They are now on `InvestigationLoop`.

- [ ] **Step 5: Remove the DescribeClueLead and IsPlayerKnownClue helpers from GameSession**

Remove the `DescribeClueLead` method (lines 3831–3832) and the `IsPlayerKnownClue` method (lines 3843–3855). They are now on `InvestigationLoop`.

- [ ] **Step 6: Build and run tests**

Run: `dotnet build WildBunch.sln`
Expected: PASS

Run: `dotnet test WildBunch.sln --filter "FullyQualifiedName~GameSessionInvestigationActionsTests|FullyQualifiedName~InvestigationEventSourcingTests|FullyQualifiedName~GameSessionWantedPostersTests|FullyQualifiedName~GameSessionResolverWiringTests" --no-build`
Expected: PASS with same counts as baseline

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Domain/Game/InvestigationLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-120: move ReadWantedPosters decision logic into InvestigationLoop"
```

---

## Task 9: Move remaining investigation methods into InvestigationLoop

**Files:**
- Modify: `src/WildBunch.Domain/Game/InvestigationLoop.cs` (add `FollowTelegraphLeads`, `GatherLocalGossip`, `InspectNoticeBoard`, `CheckSheriffRecords` methods)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (lines 3451–3708)

**Interfaces:**
- Consumes: `InvestigationContext`, `InvestigationOutcome` from Task 7
- Produces: Four new `InvestigationLoop` methods; `GameSession` methods delegate and produce events

- [ ] **Step 1: Add FollowTelegraphLeads method to InvestigationLoop**

In `InvestigationLoop.cs`, after the `ReadWantedPosters` method, add:

```csharp
    /// <summary>
    /// Follow telegraph leads decision logic. Resolves a clue from the clue surfacing
    /// resolver for the TelegraphLead source kind. Returns the InvestigationPerformed event
    /// and display message.
    /// </summary>
    internal InvestigationOutcome FollowTelegraphLeads(InvestigationContext context)
    {
        if (context.IsSourceSpent)
        {
            var msg = "You ask after telegraph leads again, but no new wire has come in.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.TelegraphLead,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var clue = _clueSurfacingResolver.Resolve(
            context.CaseFile,
            InvestigationSourceKind.TelegraphLead,
            context.CurrentTownSlotIndex,
            context.CurrentTownVisitCount,
            context.SaltSource);
        if (clue is not null && !IsPlayerKnownClue(clue))
        {
            clue = null;
        }

        if (clue is null)
        {
            var msg = "You follow the telegraph leads, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.TelegraphLead,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var foundMsg = $"You follow the telegraph leads and uncover a public lead: {DescribeClueLead(clue.Description)}.";
        return new InvestigationOutcome(
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.TelegraphLead,
                TownId = context.CurrentTownId,
                Message = foundMsg,
                ClueId = clue?.Id
            },
            "You follow the telegraph leads and uncover a public lead.");
    }
```

- [ ] **Step 2: Add GatherLocalGossip method to InvestigationLoop**

```csharp
    /// <summary>
    /// Gather local gossip decision logic. Resolves a clue from the clue surfacing
    /// resolver for the LocalGossip source kind. Returns the InvestigationPerformed event
    /// and display message.
    /// </summary>
    internal InvestigationOutcome GatherLocalGossip(InvestigationContext context)
    {
        if (context.IsSourceSpent)
        {
            var msg = "You ask around again, but hear nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.LocalGossip,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var clue = _clueSurfacingResolver.Resolve(
            context.CaseFile,
            InvestigationSourceKind.LocalGossip,
            context.CurrentTownSlotIndex,
            context.CurrentTownVisitCount,
            context.SaltSource);
        if (clue is not null && !IsPlayerKnownClue(clue))
        {
            clue = null;
        }

        if (clue is null)
        {
            var msg = "You ask around for local gossip, but hear nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.LocalGossip,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var foundMsg = $"You ask around for local gossip and uncover a public lead: {DescribeClueLead(clue.Description)}.";
        return new InvestigationOutcome(
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.LocalGossip,
                TownId = context.CurrentTownId,
                Message = foundMsg,
                ClueId = clue?.Id
            },
            "You ask around for local gossip and uncover a public lead.");
    }
```

- [ ] **Step 3: Add InspectNoticeBoard method to InvestigationLoop**

```csharp
    /// <summary>
    /// Inspect notice board decision logic. Peeks the next public clue for the NoticeBoard
    /// source kind. Returns the InvestigationPerformed event and display message.
    /// </summary>
    internal InvestigationOutcome InspectNoticeBoard(InvestigationContext context)
    {
        if (context.IsSourceSpent)
        {
            var msg = "You inspect the notice board again, but nothing new has been posted.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.NoticeBoard,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var clue = context.CaseFile.PeekNextPublicClue(c => c.SourceKind == InvestigationSourceKind.NoticeBoard);

        if (clue is null)
        {
            var msg = "You inspect the notice board, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.NoticeBoard,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var foundMsg = $"You inspect the notice board and uncover a civic notice: {DescribeClueLead(clue.Description)}.";
        return new InvestigationOutcome(
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.NoticeBoard,
                TownId = context.CurrentTownId,
                Message = foundMsg,
                ClueId = clue?.Id
            },
            "You inspect the notice board and uncover a civic notice.");
    }
```

- [ ] **Step 4: Add CheckSheriffRecords method to InvestigationLoop**

```csharp
    /// <summary>
    /// Check sheriff records decision logic. Peeks the next public clue for the LocalRecords
    /// source kind, filtered to player-known clues. Returns the InvestigationPerformed event
    /// and display message.
    /// </summary>
    internal InvestigationOutcome CheckSheriffRecords(InvestigationContext context)
    {
        if (context.IsSourceSpent)
        {
            var msg = "You check the local records again, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.LocalRecords,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var clue = context.CaseFile.PeekNextPublicClue(c => IsPlayerKnownClue(c) && c.SourceKind == InvestigationSourceKind.LocalRecords);

        if (clue is null)
        {
            var msg = "You check the local records, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.LocalRecords,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var foundMsg = $"You check the local records and uncover a public lead: {DescribeClueLead(clue.Description)}.";
        return new InvestigationOutcome(
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.LocalRecords,
                TownId = context.CurrentTownId,
                Message = foundMsg,
                ClueId = clue?.Id
            },
            "You check the local records and uncover a public lead.");
    }
```

- [ ] **Step 5: Rewrite GameSession.FollowTelegraphLeads to delegate**

Replace the entire method body (lines 3451–3522) with:

```csharp
    public CaseInvestigationResult FollowTelegraphLeads()
    {
        if (IsArchived)
        {
            return CaseInvestigationResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        if (!CurrentTown.IsAvailable(InvestigationSourceKind.TelegraphLead))
        {
            return CaseInvestigationResult.Failed("There is no telegraph office here.");
        }

        var beatSpent = Clock.TimeOfDay;
        EnterActionContext(TownActionContext.TelegraphOffice);
        var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.TelegraphOffice, CurrentTown.TownName);

        var boringSalt = SaltSource.Mode == SaltSourceMode.Fixed ? null : SaltSource;
        var context = new InvestigationContext(
            CaseFile,
            CurrentTownSlotIndex,
            CurrentTownVisitCount,
            boringSalt,
            RetiredWarrantIds,
            CurrentTown.TownId,
            CurrentTown.TownName,
            beatNarration,
            IsSourceSpent: CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead),
            IsSourceAvailable: true);
        var outcome = _investigationLoop.FollowTelegraphLeads(context);
        ProduceEvent(outcome.Event);
        return CaseInvestigationResult.Succeeded(outcome.DisplayMessage, sessionChanged: true, beatNarration: beatNarration);
    }
```

- [ ] **Step 6: Rewrite GameSession.GatherLocalGossip to delegate**

Replace the entire method body (lines 3524–3590) with:

```csharp
    public CaseInvestigationResult GatherLocalGossip()
    {
        if (IsArchived)
        {
            return CaseInvestigationResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        var beatSpent = Clock.TimeOfDay;
        EnterActionContext(TownActionContext.Saloon);
        var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.Saloon, CurrentTown.TownName);

        var boringSalt = SaltSource.Mode == SaltSourceMode.Fixed ? null : SaltSource;
        var context = new InvestigationContext(
            CaseFile,
            CurrentTownSlotIndex,
            CurrentTownVisitCount,
            boringSalt,
            RetiredWarrantIds,
            CurrentTown.TownId,
            CurrentTown.TownName,
            beatNarration,
            IsSourceSpent: CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalGossip),
            IsSourceAvailable: true);
        var outcome = _investigationLoop.GatherLocalGossip(context);
        ProduceEvent(outcome.Event);
        return CaseInvestigationResult.Succeeded(outcome.DisplayMessage, sessionChanged: true, beatNarration: beatNarration);
    }
```

- [ ] **Step 7: Rewrite GameSession.InspectNoticeBoard to delegate**

Replace the entire method body (lines 3592–3649) with:

```csharp
    public CaseInvestigationResult InspectNoticeBoard()
    {
        if (IsArchived)
        {
            return CaseInvestigationResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        var beatSpent = Clock.TimeOfDay;
        EnterActionContext(TownActionContext.TownSquare);
        var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.TownSquare, CurrentTown.TownName);

        var context = new InvestigationContext(
            CaseFile,
            CurrentTownSlotIndex,
            CurrentTownVisitCount,
            SaltSource: null,
            RetiredWarrantIds,
            CurrentTown.TownId,
            CurrentTown.TownName,
            beatNarration,
            IsSourceSpent: CurrentTownVisit.IsSpent(InvestigationSourceKind.NoticeBoard),
            IsSourceAvailable: true);
        var outcome = _investigationLoop.InspectNoticeBoard(context);
        ProduceEvent(outcome.Event);
        return CaseInvestigationResult.Succeeded(outcome.DisplayMessage, sessionChanged: true, beatNarration: beatNarration);
    }
```

- [ ] **Step 8: Rewrite GameSession.CheckSheriffRecords to delegate**

Replace the entire method body (lines 3651–3708) with:

```csharp
    public CaseInvestigationResult CheckSheriffRecords()
    {
        if (IsArchived)
        {
            return CaseInvestigationResult.Failed(ArchivedBlockMessage);
        }

        if (IsJourneyModal())
        {
            return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
        }

        var beatSpent = Clock.TimeOfDay;
        EnterActionContext(TownActionContext.SheriffOffice);
        var beatNarration = BeatNarration.Render(beatSpent, TownActionContext.SheriffOffice, CurrentTown.TownName);

        var context = new InvestigationContext(
            CaseFile,
            CurrentTownSlotIndex,
            CurrentTownVisitCount,
            SaltSource: null,
            RetiredWarrantIds,
            CurrentTown.TownId,
            CurrentTown.TownName,
            beatNarration,
            IsSourceSpent: CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalRecords),
            IsSourceAvailable: true);
        var outcome = _investigationLoop.CheckSheriffRecords(context);
        ProduceEvent(outcome.Event);
        return CaseInvestigationResult.Succeeded(outcome.DisplayMessage, sessionChanged: true, beatNarration: beatNarration);
    }
```

- [ ] **Step 9: Add a private helper for retired warrant IDs on GameSession**

All five investigation methods pass `RetiredWarrantIds` in their context records. Add a private helper near the other helpers so the logic is defined once:

```csharp
    private IReadOnlySet<Guid> RetiredWarrantIds
        => _bountyLoop.UnrelatedCriminalLedger.RetiredWarrantIds
            .Concat(_bountyLoop.UnrelatedCriminalLedger.TakenInCriminalIds)
            .ToHashSet();
```

The `ReadWantedPosters` method (Task 8 Step 2) already uses `RetiredWarrantIds` in its context construction. The other four methods (Steps 5–8) also use `RetiredWarrantIds` — they don't read warrants, but passing the same property keeps the context construction uniform and avoids a separate empty-set allocation.

- [ ] **Step 10: Build and run tests**

Run: `dotnet build WildBunch.sln`
Expected: PASS

Run: `dotnet test WildBunch.sln --filter "FullyQualifiedName~GameSessionInvestigationActionsTests|FullyQualifiedName~InvestigationEventSourcingTests|FullyQualifiedName~BeatModelEconomyTests|FullyQualifiedName~BeatNarrationDomainTests|FullyQualifiedName~ActionAvailabilityResolverTests" --no-build`
Expected: PASS with same counts as baseline

- [ ] **Step 11: Commit**

```bash
git add src/WildBunch.Domain/Game/InvestigationLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-120: move FollowTelegraphLeads, GatherLocalGossip, InspectNoticeBoard, CheckSheriffRecords into InvestigationLoop"
```

---

## Task 10: Move Store/Purchase into a small StoreLoop (optional)

> **Scope note:** This task is OPTIONAL. The issue says "Include only if it naturally falls out of the action-context work." The `Purchase` method calls `EnterActionContext(TownActionContext.Store)`, so it interacts with `ActionContextTracker`, but the store logic itself (inventory validation, cash check) is independent. Include this task only if the extraction falls out naturally. Otherwise, leave it as a small follow-up issue and skip to Task 11.

**Files:**
- Create: `src/WildBunch.Domain/Game/StoreLoop.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (lines 2927–2985, 3928–3971)

**Interfaces:**
- Consumes: `StoreOffer`, `ItemKind`, `Player` (read-only via context)
- Produces: `StoreLoop` class (stateless), `StorePurchaseContext` record, `StorePurchaseOutcome` record

- [ ] **Step 1: Create StoreLoop.cs with the decision logic**

```csharp
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Stateless child domain component inside the GameSession boundary that owns store
/// purchase decision logic. Receives narrow context records, returns StoreItemPurchased
/// events for GameSession to produce. Does NOT reference GameSession, produce events
/// directly, enter action context, or mutate Player. See BUNCH-120.
/// </summary>
internal sealed class StoreLoop
{
    /// <summary>
    /// Purchase decision logic. Validates quantity, stackability, cash, and inventory
    /// constraints. Returns the StoreItemPurchased event and display message, or null
    /// with a failure message if validation fails.
    /// </summary>
    internal StorePurchaseOutcome Purchase(StorePurchaseContext context)
    {
        ArgumentNullException.ThrowIfNull(context.Offer);

        if (context.Quantity < 1)
        {
            return StorePurchaseOutcome.Failed("Quantity must be at least 1.");
        }

        if (context.Offer.ItemKind == ItemKind.Horse && context.Quantity != 1)
        {
            return StorePurchaseOutcome.Failed("Horse items must have a quantity of 1.");
        }

        if (context.Quantity != 1 && !IsStackableItemKind(context.Offer.ItemKind))
        {
            return StorePurchaseOutcome.Failed($"{context.Offer.ItemKind} does not stack.");
        }

        var totalPrice = context.Offer.Price * context.Quantity;
        if (!context.PlayerCanAfford(totalPrice))
        {
            return StorePurchaseOutcome.Failed("Not enough cash.");
        }

        if (!CanPurchaseInventoryItem(context.Offer, context.Quantity, context.PlayerHasItem, out var inventoryFailureMessage))
        {
            return StorePurchaseOutcome.Failed(inventoryFailureMessage);
        }

        var e = new StoreItemPurchased
        {
            TownId = context.CurrentTownId,
            ItemKind = context.Offer.ItemKind,
            DisplayName = context.Offer.DisplayName,
            Quantity = context.Quantity,
            UnitPrice = context.Offer.Price,
            TotalPrice = totalPrice,
            WalletAfter = context.PlayerCash - totalPrice
        };

        var quantityLabel = context.Quantity == 1 ? context.Offer.DisplayName : $"{context.Quantity} {context.Offer.DisplayName}";
        return StorePurchaseOutcome.Succeeded(e, $"Purchased {quantityLabel} for ${totalPrice:0.00}.");
    }

    private static bool CanPurchaseInventoryItem(StoreOffer offer, int quantity, Func<ItemKind, bool> playerHasItem, out string failureMessage)
    {
        if (quantity < 1)
        {
            failureMessage = "Quantity must be at least 1.";
            return false;
        }

        if (offer.ItemKind == ItemKind.Horse)
        {
            if (quantity != 1)
            {
                failureMessage = "Horse items must have a quantity of 1.";
                return false;
            }

            if (playerHasItem(ItemKind.Horse))
            {
                failureMessage = "Horse already exists in inventory.";
                return false;
            }

            failureMessage = string.Empty;
            return true;
        }

        if (quantity != 1 && !IsStackableItemKind(offer.ItemKind))
        {
            failureMessage = $"{offer.ItemKind} does not stack.";
            return false;
        }

        if (!IsStackableItemKind(offer.ItemKind) && playerHasItem(offer.ItemKind))
        {
            failureMessage = $"{offer.ItemKind} already exists in inventory.";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    internal static bool IsStackableItemKind(ItemKind kind)
        => kind is ItemKind.Food or ItemKind.HorseFeed or ItemKind.RevolverAmmo or ItemKind.RifleAmmo;
}

internal sealed record StorePurchaseContext(
    StoreOffer Offer,
    int Quantity,
    TownId CurrentTownId,
    decimal PlayerCash,
    Func<decimal, bool> PlayerCanAfford,
    Func<ItemKind, bool> PlayerHasItem);

internal sealed record StorePurchaseOutcome(bool Success, StoreItemPurchased? Event, string Message)
{
    internal static StorePurchaseOutcome Failed(string message) => new(false, null, message);
    internal static StorePurchaseOutcome Succeeded(StoreItemPurchased e, string message) => new(true, e, message);
}
```

- [ ] **Step 2: Rewrite GameSession.Purchase to delegate to StoreLoop**

Replace the entire method body (lines 2927–2985) with:

```csharp
    public StorePurchaseResult Purchase(StoreOffer offer, int quantity)
    {
        if (IsArchived)
        {
            return StorePurchaseResult.Failed(ArchivedBlockMessage);
        }

        ArgumentNullException.ThrowIfNull(offer);

        if (IsJourneyModal())
        {
            return StorePurchaseResult.Failed(JourneyModalBlockMessage);
        }

        EnterActionContext(TownActionContext.Store);

        var context = new StorePurchaseContext(
            offer,
            quantity,
            CurrentTown.TownId,
            Player.Wallet.Cash,
            Player.CanAfford,
            Player.HasItem);
        var outcome = _storeLoop.Purchase(context);
        if (!outcome.Success)
        {
            return StorePurchaseResult.Failed(outcome.Message);
        }

        ProduceEvent(outcome.Event!);
        return StorePurchaseResult.Succeeded(outcome.Message);
    }
```

- [ ] **Step 3: Add the StoreLoop field to GameSession**

```csharp
    private readonly InvestigationLoop _investigationLoop = new();
    private readonly StoreLoop _storeLoop = new();
```

- [ ] **Step 4: Remove CanPurchaseInventoryItem and IsStackableItemKind from GameSession**

Remove the `CanPurchaseInventoryItem` method (lines 3928–3968) and `IsStackableItemKind` method (lines 3970–3971). They are now on `StoreLoop`.

- [ ] **Step 5: Build and run tests**

Run: `dotnet build WildBunch.sln`
Expected: PASS

Run: `dotnet test WildBunch.sln --filter "FullyQualifiedName~StorePurchase|FullyQualifiedName~Purchase|FullyQualifiedName~BeatModelEconomyTests" --no-build`
Expected: PASS with same counts as baseline

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/Game/StoreLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-120: move Store/Purchase decision logic into StoreLoop"
```

---

## Task 11: Final validation, falsification checks, and cleanup

**Files:**
- Read: `src/WildBunch.Domain/Game/ActionContextTracker.cs`
- Read: `src/WildBunch.Domain/Game/InvestigationLoop.cs`
- Read: `src/WildBunch.Domain/Game/StoreLoop.cs` (if Task 10 was done)
- Read: `src/WildBunch.Domain/Game/GameSession.cs`

**Interfaces:**
- Consumes: all previous tasks
- Produces: falsification check results, line-count proof, validation counts

- [ ] **Step 1: Run falsification checks**

Run each of these searches and confirm zero matches:

```
# ActionContextTracker does NOT reference GameSession
rg "GameSession" src/WildBunch.Domain/Game/ActionContextTracker.cs
# Expected: 0 matches (comments mentioning GameSession are OK if they say "does NOT reference")

# InvestigationLoop does NOT reference GameSession
rg "GameSession" src/WildBunch.Domain/Game/InvestigationLoop.cs
# Expected: 0 matches (comments mentioning GameSession are OK if they say "does NOT reference")

# StoreLoop does NOT reference GameSession (if Task 10 was done)
rg "GameSession" src/WildBunch.Domain/Game/StoreLoop.cs
# Expected: 0 matches

# None of the three components call ProduceEvent, EnterActionContext, Player.AdjustCash,
# CaseFile.Record*/Reveal*, CurrentTown.CheckSource/CheckWantedPosters,
# CurrentTownVisit.CurrentTownState.Set*, Clock.Set, PursuitState.SetHeat
rg "ProduceEvent|EnterActionContext|AdjustCash|RecordWantedSuspect|RecordSheriffTurnIn|RevealClueById|RevealWarrantById|CheckSource|CheckWantedPosters|SetHeat|Clock\.Set" src/WildBunch.Domain/Game/ActionContextTracker.cs src/WildBunch.Domain/Game/InvestigationLoop.cs src/WildBunch.Domain/Game/StoreLoop.cs
# Expected: 0 matches (ActionContextTracker.EnterActionContext is its own method, not GameSession's)
```

- [ ] **Step 2: Confirm GameSession still controls guards, ProduceEvent, Apply dispatch, _version++**

Verify in `GameSession.cs`:
- All public command methods start with `IsArchived` / `IsJourneyModal` guards
- `ProduceEvent` is still the canonical event production path
- `Apply` dispatch switch still calls `_actionContextTracker.Apply(e)` for `TownActionContextEntered` and applies cross-owner mutations
- `Apply(InvestigationPerformed)` still calls `CurrentTown.CheckSource`/`CheckWantedPosters` and `CaseFile.RevealClueById`/`RevealWarrantById` and `_version++`
- `_version++` is present in every Apply handler

- [ ] **Step 3: Build the full solution**

Run: `dotnet build WildBunch.sln`
Expected: PASS (record warnings separately from failures)

- [ ] **Step 4: Run the full test suite via PostgreSQL validation lane**

Run: `.\scripts\postgres-dev.ps1 validate`
Expected: PASS (record exact passed/failed/skipped counts)

- [ ] **Step 5: Capture line-count delta**

Run: `(Get-Content src/WildBunch.Domain/Game/GameSession.cs).Count`
Expected: Record exact value (was 4006 at baseline)

Run: `(Get-Content src/WildBunch.Domain/Game/ActionContextTracker.cs).Count`
Run: `(Get-Content src/WildBunch.Domain/Game/InvestigationLoop.cs).Count`
Run: `(Get-Content src/WildBunch.Domain/Game/StoreLoop.cs).Count` (if Task 10 was done)

- [ ] **Step 6: Regenerate index mesh**

Run: `python scripts/generate_index_mesh.py`
Expected: INDEX.md files updated for new files in `src/WildBunch.Domain/Game/`

- [ ] **Step 7: Commit index mesh and any cleanup**

```bash
git add -A
git commit -m "BUNCH-120: regenerate index mesh for new child domain component files"
```

- [ ] **Step 8: Run final falsification check — confirm no behavior change**

Run: `dotnet test WildBunch.sln --filter "FullyQualifiedName~GameSessionInvestigationActionsTests|FullyQualifiedName~InvestigationEventSourcingTests|FullyQualifiedName~ClockTurnCorrectionTests|FullyQualifiedName~BeatModelEconomyTests|FullyQualifiedName~GameSessionEventSourcingTests|FullyQualifiedName~EventStorePersistenceTests" --no-build`
Expected: PASS with same counts as baseline (Task 1 Step 3)

- [ ] **Step 9: Report validation evidence**

Post a comment on the PR with:
```
## Validation Evidence

### Falsification checks
- ActionContextTracker does NOT reference GameSession: <return 0 matches>
- InvestigationLoop does NOT reference GameSession: <return 0 matches>
- StoreLoop does NOT reference GameSession: <return 0 matches or N/A>
- None of the three components call forbidden methods: <return 0 matches>
- GameSession still controls guards, ProduceEvent, Apply dispatch, _version++: <return confirm>

### Build
- dotnet build WildBunch.sln — PASS (warnings: <return>)

### Tests
- dotnet test (via postgres-dev.ps1 validate) — PASS, passed: <return>, failed: <return>, skipped: <return>
- Baseline test counts match: <return confirm>

### Line-count delta
- GameSession.cs: <return> (was 4006)
- ActionContextTracker.cs: <return>
- InvestigationLoop.cs: <return>
- StoreLoop.cs: <return or N/A>

### Index mesh
- Regenerated: <return yes/no>

#### Test plan
- [ ] Full domain test suite green
- [ ] Full integration test suite green (PostgreSQL lane)
- [ ] Investigation action tests green
- [ ] Event-sourcing/replay equality tests green (proves Apply semantics unchanged)
- [ ] Clock turn correction tests green
- [ ] Beat model economy tests green
- [ ] Snapshot persistence tests green
```

- [ ] **Step 10: Update Linear route state**

Post a Linear comment on BUNCH-120 with: branch name, head commit, PR URL, falsification check results, line-count proof, and validation counts. Do NOT close the issue.

---

## Falsification checks

- `ActionContextTracker` does NOT reference `GameSession`: search and confirm zero matches
- `InvestigationLoop` does NOT reference `GameSession`: search and confirm zero matches
- `StoreLoop` does NOT reference `GameSession` (if Task 10 done): search and confirm zero matches
- None of the three components call `ProduceEvent`, `EnterActionContext` (except `ActionContextTracker` which IS the action context), `Player.AdjustCash`, `CaseFile.Record*`/`Reveal*`, `CurrentTown.CheckSource`/`CheckWantedPosters`, `CurrentTownVisit.CurrentTownState.Set*`, `Clock.Set`, `PursuitState.SetHeat`: search and confirm zero matches
- `GameSession` no longer directly owns investigation decision rules or action-context state: confirm method bodies are guard + context + call + produce
- `GameSession` still controls guards, `ProduceEvent`, Apply dispatch, `_version++`: confirm
- All investigation/clock/beat tests pass with same counts as baseline: confirm

## Validation

- `dotnet build WildBunch.sln` — PASS (warnings: <return>)
- `dotnet test WildBunch.sln` (via `postgres-dev.ps1 validate`) — PASS, passed: <return>, failed: <return>, skipped: <return>
- `GameSession.cs` line count: <return> (was 4006)
- `ActionContextTracker.cs` line count: <return>
- `InvestigationLoop.cs` line count: <return>
- `StoreLoop.cs` line count: <return or N/A>
- Index mesh regenerated (changed: <return yes/no>)

#### Test plan
- [ ] Full domain test suite green
- [ ] Full integration test suite green (PostgreSQL lane)
- [ ] Investigation action tests green
- [ ] Event-sourcing/replay equality tests green (proves Apply semantics unchanged)
- [ ] Clock turn correction tests green
- [ ] Beat model economy tests green
- [ ] Snapshot persistence tests green

Generated with [Devin](https://devin.ai)

## Self-Review

**1. Spec coverage:**
- "Extract InvestigationLoop (~300 lines)" — Tasks 7–9 extract the 5 investigation methods + helpers into a stateless `InvestigationLoop`. The issue says it could be stateless; this plan makes it stateless because the Apply handler mutates cross-owner state (CurrentTown, CaseFile), not investigation-owned state.
- "Extract ActionContextTracker (~60 lines)" — Tasks 2–6 extract `CurrentActionContext`, `CurrentActionContextTownId`, `EnterActionContext`, `CanConfrontWantedSuspectInCurrentContext`, `ResetActionContextForTownChange`, and the owned portion of `Apply(TownActionContextEntered)` into a stateful `ActionContextTracker`.
- "Store/Purchase (optional)" — Task 10 is marked optional per the issue's "Include only if it naturally falls out" guidance.
- "Both components are internal sealed" — Global Constraints + Task 2/7/10 class declarations.
- "Both receive narrow inputs via context records" — Tasks 2/7/10 define context records; Tasks 3–5/8–9/10 use them.
- "Both return explicit results + events to produce" — `InvestigationOutcome` (Task 7), `TownActionContextEntered?` (Task 2), `StorePurchaseOutcome` (Task 10).
- "Both own Apply handlers for their own state" — `ActionContextTracker.Apply(TownActionContextEntered)` (Task 5). `InvestigationLoop` is stateless and does NOT own an Apply handler — the issue says "stateless or owns investigation-source tracking if needed"; it's stateless because the Apply handler mutates cross-owner state.
- "Neither references GameSession" — Falsification check in Task 11 Step 1.
- "GameSession retains guards, ProduceEvent, Apply dispatch, persistence boundary" — documented in Architecture section, verified by Task 11 Step 2.
- "No public API, DTO, event payload, message string, or snapshot shape changes" — Global Constraints.
- "Coordinate with BUNCH-5" — Global Constraints note: BUNCH-5 has landed, beat narration preserved.
- "Should land after BUNCH-112 and BUNCH-119" — Global Constraints note: BUNCH-112 is on main, BUNCH-119 plan is merged but implementation not yet on main; this plan branches from current main and composes cleanly.

**2. Placeholder scan:** No TBDs, no "implement later", no "add appropriate error handling". Every implementation step names exact methods, exact context fields, and exact rewired bodies. The only template tokens are `<return>` fields in the Task 11 Step 9 return template, which are explicit return-time evidence fields.

**3. Type consistency:** `InvestigationContext` is used consistently across all 5 investigation methods. `InvestigationOutcome` is returned by all 5. `ActionContextEnterInputs` and `CanConfrontInContextInputs` match the `ActionContextTracker` method signatures. `StorePurchaseContext` and `StorePurchaseOutcome` match the `StoreLoop.Purchase` signature. The `RestoreActionContextState` internal method (Task 6) matches the rehydration call in `SessionSnapshot.ToDomain`.

## Architecture falsification checks (run before declaring GREEN)

Before claiming complete, verify the plan did NOT:
- leave `ActionContextTracker`, `InvestigationLoop`, or `StoreLoop` with any reference to `GameSession` — search and confirm zero matches.
- leave any of the three components calling `ProduceEvent`, `EnterActionContext` (except `ActionContextTracker` which IS the action context), `Player.AdjustCash`, `CaseFile.Record*`/`Reveal*`, `CurrentTown.CheckSource`/`CheckWantedPosters`, `CurrentTownVisit.CurrentTownState.Set*`, `Clock.Set`, or `PursuitState.SetHeat` — search and confirm zero matches.
- move business rules into a handler/service/persistence/UI layer — all three are domain classes in `WildBunch.Domain`.
- make any of the three a separate aggregate root or repository — they have no persistence identity, no `IAggregateRoot`, no repository.
- create a broad service that becomes an alternate mutation authority — all three return events as data; `GameSession` produces them.
- change clue, wanted-poster, wallet, inventory, horse, bounty-loop, saloon, or travel state handling — out of scope, untouched.
- change persistence shape, snapshot codec record, or EF entity shape — snapshot record unchanged; only rehydration construction path changes for `ActionContextTracker`.
- change any public method signature, DTO shape, result-object shape, message string, or event payload — Global Constraints, verified by test suite.
