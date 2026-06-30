# BUNCH-112 Extract BountyLoop Domain Service from GameSession Aggregate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce `GameSession.cs` (3,588 lines) by moving all remaining bounty-loop command and event-application logic into the existing `BountyLoopCoordinator` partial-class file, without changing public behavior, DTO shapes, persistence shape, or aggregate-root authority.

**Architecture:** The `BountyLoopCoordinator` is already an `internal sealed` nested class inside the `GameSession` partial — it is an aggregate-owned internal coordinator, NOT a standalone domain service. This plan completes the extraction the BUNCH-72 stepping stone started: it moves the remaining bounty-loop command methods (`LookAroundSaloon`, `CheckSheriffRecords`, `SettleUnrelatedCriminalTurnIn`, `UpdateWantedSuspectPresence`, `CanConfrontWantedSuspectInCurrentContext`, the `Apply(...)` handlers for the six bounty-loop domain events, the dev-saloon-override command methods, and the warrant/presence helpers) from `GameSession.cs` into `GameSession.BountyLoopCoordinator.cs`. `GameSession` retains the public command entry points (which delegate to the coordinator after the `IsArchived`/`IsJourneyModal` guard), the `Apply` dispatch switch, and aggregate-wide helpers (`ProduceEvent`, `EnterActionContext`, `CurrentTownVisit`, `CaseFile`, `Player`, `Clock`). The coordinator stays a nested class with `internal` access to `GameSession` internals, so mutation still flows through the aggregate root — this is aggregate-internal cohesion, not aggregate bypass (per ADR-0002, ADR-0020, `.agents/architecture-hygiene.md` line 38, and `.agents/unslop/backend-architecture.md` "Aggregate bypass" rule).

**Tech Stack:** C# / .NET 10, xUnit, existing Wild Bunch domain tests.

## Global Constraints

- `GameSession` remains the live-play aggregate root and the only externally loaded/persisted root (ADR-0002, ADR-0020).
- Do not introduce a standalone `BountyLoopService` outside `GameSession`. The coordinator is a nested `internal sealed class BountyLoopCoordinator` inside the `GameSession` partial — aggregate-owned, not aggregate-bypassing.
- Do not change any public method signature, DTO shape, result-object shape, message string, or event payload on `GameSession` or its result types.
- Do not change persistence shape, snapshot codec shape, or EF entity shape. No new migrations.
- Do not change clue, journal, wanted-poster, wallet, inventory, horse, or travel state handling (out of scope per architecture guardrails).
- Preserve all existing domain event types and their `Apply(...)` mutation semantics exactly.
- Keep `CaseFile`, `TownVisitState`, `WantedSuspectPresenceLedger`, `Player`, `BountyDeclarationMatchPolicy`, `BountySettlementPolicy`, `UnrelatedCriminalLedger`, and `CitizenCast` as the explicit owners of their current state and policy concerns — the coordinator composes them, it does not absorb their invariants.
- Dev-only surfaces (`ForceDevSaloonOverride`, `ClearDevSaloonOverride`, `_pendingDevSaloonOverride`) are in scope because they are bounty-loop-adjacent (saloon POI) and currently live in `GameSession.cs`; moving them keeps the bounty-loop file cohesive. Do not polish or expand them.
- Run `dotnet build` and `dotnet test` after every task. Run `.\scripts\postgres-dev.ps1 validate` only if a task touches persistence (none do).
- This is a pure mechanical move + visibility adjustment. No behavior changes. If any task reveals a behavior change, STOP and report — do not "fix" it silently.

---

### Task 1: Characterize the current bounty-loop surface with a regression baseline

**Files:**
- Read-only: `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs`
- Read-only: `tests/WildBunch.Domain.Tests/GameSessionSaloonWantedSuspectLoopTests.cs`
- Read-only: `tests/WildBunch.Domain.Tests/GameSessionSheriffTurnInTests.cs`
- Read-only: `tests/WildBunch.Domain.Tests/GameSessionWantedSuspectConfrontationTests.cs`
- Read-only: `tests/WildBunch.Domain.Tests/GameSessionWantedSuspectPresenceTests.cs`
- Read-only: `tests/WildBunch.Domain.Tests/GameSessionBountyLoopCoordinatorTests.cs`
- Read-only: `tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs`
- Read-only: `tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs`
- Read-only: `tests/WildBunch.Integration.Tests/Acceptance/SaloonConfrontationAcceptanceTests.cs`

**Interfaces:**
- Consumes: nothing (baseline step)
- Produces: a green test baseline that proves the existing bounty-loop behavior is intact before any move. Later tasks re-run this same filter to prove no regression.

- [ ] **Step 1: Run the bounty-loop domain test filter and capture the green baseline**

Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~Saloon|FullyQualifiedName~SheriffTurnIn|FullyQualifiedName~WantedSuspectConfrontation|FullyQualifiedName~WantedSuspectPresence|FullyQualifiedName~BountyLoopCoordinator|FullyQualifiedName~BountySaloonEventSourcing|FullyQualifiedName~DevSaloonOverride"
```
Expected: PASS. Record the exact passed/failed/skipped counts. If any test fails on a clean worktree branched from `origin/main`, STOP — the baseline is not green and the move cannot proceed safely.

- [ ] **Step 2: Run the saloon confrontation acceptance + dev endpoint integration filter**

Run:
```
dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~SaloonConfrontationAcceptance|FullyQualifiedName~DevSaloonEndpoint"
```
Expected: PASS. This requires the repo-local PostgreSQL lane. Run `.\scripts\postgres-dev.ps1 ensure` first if needed. Record counts.

- [ ] **Step 3: Run the full solution build to confirm a clean compile baseline**

Run: `dotnet build WildBunch.sln`
Expected: PASS with zero errors. Record warning count separately.

### Task 2: Move the saloon look-around command into the coordinator

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs` (add `LookAroundSaloon`)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (replace body of `LookAroundSaloon` with a delegate call; keep the public method and its `IsArchived`/`IsJourneyModal` guards)

**Interfaces:**
- Consumes from `GameSession`: `IsArchived`, `IsJourneyModal()`, `EnterActionContext(TownActionContext.Saloon)`, `_pendingDevSaloonOverride`, `ProduceEvent<T>`, `CurrentTown`, `CurrentTownVisit`, `Clock`, `CaseFile`, `CitizenCast`, `SaltSource`, `CollectSuspectFeatureDescriptions()`, `TryGetEligibleSaloonSuspectCandidate(out Suspect)`, `IsEligibleSaloonPersonOfInterestCandidate(Suspect)`, `ArchivedBlockMessage`, `JourneyModalBlockMessage`.
- Produces: `BountyLoopCoordinator.LookAroundSaloon()` returning `CaseInvestigationResult`. `GameSession.LookAroundSaloon()` delegates to it after the archived/journey guards.

- [ ] **Step 1: Add `LookAroundSaloon` to `BountyLoopCoordinator`**

Copy the entire body of `GameSession.LookAroundSaloon` (currently `GameSession.cs` lines ~3153–3336, from the `EnterActionContext(TownActionContext.Saloon);` line through the final `return CaseInvestigationResult.Succeeded(citizenMessage, sessionChanged: true);` and closing brace) into a new `public CaseInvestigationResult LookAroundSaloon()` method on `BountyLoopCoordinator`. Replace every `_session.`-prefixed access with the existing coordinator pattern: the coordinator already references `_session.IsJourneyModal()`, `_session.CurrentTownVisit`, etc. Keep `StableSaloonRollHash` as a `private static` helper inside the coordinator (move it too — it is only used by `LookAroundSaloon`). Keep `CollectSuspectFeatureDescriptions`, `TryGetEligibleSaloonSuspectCandidate`, and `IsEligibleSaloonPersonOfInterestCandidate` on `GameSession` for now (they are also used elsewhere); the coordinator calls them via `_session.`. Do not change any message string, event field, or control flow.

- [ ] **Step 2: Replace `GameSession.LookAroundSaloon` body with a delegate**

Change `GameSession.LookAroundSaloon` to:
```csharp
public CaseInvestigationResult LookAroundSaloon()
{
    if (IsArchived)
    {
        return CaseInvestigationResult.Failed(ArchivedBlockMessage);
    }

    if (IsJourneyModal())
    {
        return CaseInvestigationResult.Failed(JourneyModalBlockMessage);
    }

    return _bountyLoopCoordinator.LookAroundSaloon();
}
```
Keep the `IsArchived` and `IsJourneyModal` guards on `GameSession` (the public entry point) so the aggregate root still owns the modal/archived invariant. The coordinator's own `IsJourneyModal()` check inside the moved body is now redundant but harmless — leave it for now; it will be removed only if a later task proves it is unreachable. Do NOT remove it in this task (minimize diff).

- [ ] **Step 3: Build and run the saloon test filter**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~Saloon"
```
Expected: PASS with the same counts as Task 1 Step 1.

- [ ] **Step 4: Commit**

```
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs
git commit -m "BUNCH-112: move LookAroundSaloon into BountyLoopCoordinator"
```

### Task 3: Move the sheriff-records and unrelated-criminal turn-in commands into the coordinator

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs` (add `CheckSheriffRecords`, `SettleUnrelatedCriminalTurnIn`)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (replace bodies with delegate calls)

**Interfaces:**
- Consumes from `GameSession`: `IsArchived`, `IsJourneyModal()`, `EnterActionContext(TownActionContext.SheriffOffice)`, `CurrentTownVisit`, `CurrentTown`, `CaseFile`, `Clock`, `_unrelatedCriminalLedger`, `ProduceEvent`, `Apply`, `_uncommittedEvents`, `IsPlayerKnownClue`, `DescribeClueLead`, `DescribeWarrantDisposition`, `ArchivedBlockMessage`, `JourneyModalBlockMessage`.
- Produces: `BountyLoopCoordinator.CheckSheriffRecords()` and `BountyLoopCoordinator.SettleUnrelatedCriminalTurnIn(WarrantId, bool)`, both returning `CaseInvestigationResult` / `SheriffTurnInResult`.

- [ ] **Step 1: Add `CheckSheriffRecords` to the coordinator**

Copy the body of `GameSession.CheckSheriffRecords` (currently `GameSession.cs` lines ~3701–3756) into `BountyLoopCoordinator.CheckSheriffRecords()`. The body uses `EnterActionContext`, `CurrentTownVisit.IsSpent`, `CaseFile.PeekNextPublicClue`, `IsPlayerKnownClue`, `DescribeClueLead`, `Apply`, `_uncommittedEvents`, and `ProduceEvent`-equivalent (`Apply` + `_uncommittedEvents.Add`). These are all `GameSession` members; access them via `_session.`. Note: `_uncommittedEvents` and `Apply` are currently `private`/`internal` on `GameSession`. The coordinator is a nested class, so it can access `private` members of `GameSession` via `_session.` — verify this compiles. If `_uncommittedEvents` is not accessible, it is because it is a `private` field on the partial and the nested class is in a different file but still the same partial-class nesting — nested classes can access private members of the enclosing class. Confirm by compiling.

- [ ] **Step 2: Replace `GameSession.CheckSheriffRecords` body with a delegate**

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

    return _bountyLoopCoordinator.CheckSheriffRecords();
}
```

- [ ] **Step 3: Add `SettleUnrelatedCriminalTurnIn` to the coordinator**

Copy the body of `GameSession.SettleUnrelatedCriminalTurnIn` (currently `GameSession.cs` lines ~3440–3505) into `BountyLoopCoordinator.SettleUnrelatedCriminalTurnIn(WarrantId warrantId, bool isAlive)`. It uses `EnterActionContext`, `CaseFile.KnownWarrants`, `_unrelatedCriminalLedger`, `DescribeWarrantDisposition`, `ProduceEvent`, `Clock`. Access via `_session.`. Keep the `contextChanged` local and the `WithSessionChanged()` / `result with { SessionChanged = true }` shaping exactly.

- [ ] **Step 4: Replace `GameSession.SettleUnrelatedCriminalTurnIn` body with a delegate**

```csharp
public SheriffTurnInResult SettleUnrelatedCriminalTurnIn(WarrantId warrantId, bool isAlive)
{
    if (IsArchived)
    {
        return SheriffTurnInResult.Rejected(ArchivedBlockMessage);
    }

    if (IsJourneyModal())
    {
        return SheriffTurnInResult.Rejected(JourneyModalBlockMessage);
    }

    return _bountyLoopCoordinator.SettleUnrelatedCriminalTurnIn(warrantId, isAlive);
}
```

- [ ] **Step 5: Build and run the sheriff + unrelated-criminal test filter**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~SheriffTurnIn|FullyQualifiedName~UnrelatedCriminal"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 6: Commit**

```
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs
git commit -m "BUNCH-112: move CheckSheriffRecords and SettleUnrelatedCriminalTurnIn into BountyLoopCoordinator"
```

### Task 4: Move the bounty-loop event `Apply(...)` handlers into the coordinator

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs` (add `Apply` handlers)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (remove the `Apply` handler methods; keep the dispatch `switch` in `ApplyAll`/`Apply` that routes to them)

**Interfaces:**
- Consumes from `GameSession`: `CurrentTownVisit`, `CaseFile`, `Player`, `_wantedSuspectPresenceLedger`, `_unrelatedCriminalLedger`, `UpdateWantedSuspectPresence` (moved in Task 5 — for this task, leave `UpdateWantedSuspectPresence` on `GameSession` and call it via `_session.`; Task 5 moves it), `RecordCaseUpdate` / `AddLogEntry`, `Clock`.
- Produces: `BountyLoopCoordinator.Apply(SaloonPersonOfInterestSpotted)`, `.Apply(WantedSuspectConfronted)`, `.Apply(SheriffTurnInSettled)`, `.Apply(UnrelatedCriminalTurnInSettled)`, `.Apply(SaloonPersonOfInterestConfronted)`, `.Apply(DevSaloonOverrideForced)`, `.Apply(DevSaloonOverrideCleared)`, `.Apply(DevSaloonOverrideConsumed)` — all `internal void`.

- [ ] **Step 1: Move the six bounty-loop `Apply` handlers + three dev-saloon `Apply` handlers into the coordinator**

Move these methods from `GameSession.cs` into `BountyLoopCoordinator` (as `internal void Apply(...)`):
- `Apply(SaloonPersonOfInterestSpotted e)` (~line 463)
- `Apply(WantedSuspectConfronted e)` (~line 492)
- `Apply(SheriffTurnInSettled e)` (~line 521)
- `Apply(UnrelatedCriminalTurnInSettled e)` (~line 544)
- `Apply(SaloonPersonOfInterestConfronted e)` (~line 562)
- `Apply(DevSaloonOverrideForced e)` (~line 768, `internal`)
- `Apply(DevSaloonOverrideCleared e)` (~line 781, `internal`)
- `Apply(DevSaloonOverrideConsumed e)` (~line 793, `internal`)

Inside each, replace direct member access with `_session.`-prefixed access. `_pendingDevSaloonOverride` is a `private` field on `GameSession`; the nested coordinator can read/write it via `_session._pendingDevSaloonOverride`. If the compiler rejects this (it should not for a nested class), make `_pendingDevSaloonOverride` `internal` and report that as a minimal visibility widening in the commit message. Do not change any mutation logic.

- [ ] **Step 2: Update the `ApplyAll`/`Apply` dispatch in `GameSession` to call the coordinator**

The dispatch `switch` in `GameSession` (around line ~373–421) currently calls `Apply(e)` directly. Change each bounty-loop/dev-saloon case to call `_bountyLoopCoordinator.Apply(e)` instead. Example:
```csharp
case SaloonPersonOfInterestSpotted sp:
    _bountyLoopCoordinator.Apply(sp);
    break;
case WantedSuspectConfronted wc:
    _bountyLoopCoordinator.Apply(wc);
    break;
case SheriffTurnInSettled ts:
    _bountyLoopCoordinator.Apply(ts);
    break;
case UnrelatedCriminalTurnInSettled ucts:
    _bountyLoopCoordinator.Apply(ucts);
    break;
case SaloonPersonOfInterestConfronted sc:
    _bountyLoopCoordinator.Apply(sc);
    break;
case DevSaloonOverrideForced dsf:
    _bountyLoopCoordinator.Apply(dsf);
    break;
case DevSaloonOverrideCleared dsc:
    _bountyLoopCoordinator.Apply(dsc);
    break;
case DevSaloonOverrideConsumed dsc2:
    _bountyLoopCoordinator.Apply(dsc2);
    break;
```
Leave all non-bounty-loop cases (travel, investigation, clock, etc.) calling `GameSession`'s own `Apply` methods unchanged.

- [ ] **Step 3: Build and run the event-sourcing test filter**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~BountySaloonEventSourcing|FullyQualifiedName~InvestigationEventSourcing|FullyQualifiedName~DevSaloonOverride|FullyQualifiedName~TravelReplayEquality"
```
Expected: PASS with the same counts as Task 1. The replay/equality tests are the critical proof that event application semantics did not change.

- [ ] **Step 4: Commit**

```
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs
git commit -m "BUNCH-112: move bounty-loop and dev-saloon Apply handlers into BountyLoopCoordinator"
```

### Task 5: Move the dev-saloon-override commands and bounty-loop helpers into the coordinator

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs` (add `ForceDevSaloonOverride`, `ClearDevSaloonOverride`, `UpdateWantedSuspectPresence`, `CanConfrontWantedSuspectInCurrentContext`, `TryGetKnownWarrantForSuspect`, `MatchesKnownWarrant`, `DescribeWarrantDisposition`)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (replace bodies with delegates; remove pure helpers that are now only used by the coordinator)

**Interfaces:**
- Consumes from `GameSession`: `IsJourneyModal()`, `CaseFile`, `ProduceEvent`, `CurrentActionContext`, `CurrentTownVisit`, `IsEligibleSaloonPersonOfInterestCandidate`, `GetSaloonPoiIneligibilityReason`, `_pendingDevSaloonOverride`, `SetWantedSuspectPresenceState`, `ArchivedBlockMessage`, `JourneyModalBlockMessage`.
- Produces: coordinator-owned `ForceDevSaloonOverride`, `ClearDevSaloonOverride`, `UpdateWantedSuspectPresence`, `CanConfrontWantedSuspectInCurrentContext`, `TryGetKnownWarrantForSuspect`, `MatchesKnownWarrant` (static), `DescribeWarrantDisposition` (static).

- [ ] **Step 1: Move `ForceDevSaloonOverride` and `ClearDevSaloonOverride` into the coordinator**

Copy the bodies (currently `GameSession.cs` ~1335–1394) into `BountyLoopCoordinator`. `ForceDevSaloonOverride` validates suspect eligibility via `CaseFile.Suspects`, `IsEligibleSaloonPersonOfInterestCandidate`, `GetSaloonPoiIneligibilityReason`, and produces `DevSaloonOverrideForced`. Access via `_session.`. Replace `GameSession.ForceDevSaloonOverride` / `ClearDevSaloonOverride` bodies with delegates:
```csharp
public void ForceDevSaloonOverride(DevSaloonOverride overrideValue)
{
    if (IsJourneyModal())
    {
        throw new InvalidOperationException("Cannot force a saloon override while a journey is active.");
    }
    _bountyLoopCoordinator.ForceDevSaloonOverride(overrideValue);
}

public void ClearDevSaloonOverride()
{
    _bountyLoopCoordinator.ClearDevSaloonOverride();
}
```
Keep the `IsJourneyModal` guard on `ForceDevSaloonOverride` at the `GameSession` entry point (it throws, so the aggregate root owns the throw). `ClearDevSaloonOverride` has only a null-check + `ProduceEvent`; delegate it wholly.

- [ ] **Step 2: Move `UpdateWantedSuspectPresence` into the coordinator**

Move the private method (currently `GameSession.cs` ~3396–3410) into `BountyLoopCoordinator` as `private void UpdateWantedSuspectPresence(...)`. It calls `SetWantedSuspectPresenceState` — access via `_session.SetWantedSuspectPresenceState`. The `Apply(WantedSuspectConfronted)` handler (now in the coordinator from Task 4) calls `UpdateWantedSuspectPresence`; update that call site to the local method instead of `_session.UpdateWantedSuspectPresence`.

- [ ] **Step 3: Move `CanConfrontWantedSuspectInCurrentContext` into the coordinator**

Move the public method (currently `GameSession.cs` ~322–336) into `BountyLoopCoordinator` as `public bool CanConfrontWantedSuspectInCurrentContext(SuspectId)`. It reads `CurrentActionContext`, `CurrentTownVisit`. Access via `_session.`. Replace `GameSession.CanConfrontWantedSuspectInCurrentContext` with:
```csharp
public bool CanConfrontWantedSuspectInCurrentContext(SuspectId targetSuspectId)
    => _bountyLoopCoordinator.CanConfrontWantedSuspectInCurrentContext(targetSuspectId);
```

- [ ] **Step 4: Move `TryGetKnownWarrantForSuspect`, `MatchesKnownWarrant`, `DescribeWarrantDisposition` into the coordinator**

Move `TryGetKnownWarrantForSuspect` (currently `internal`, ~3796), `MatchesKnownWarrant` (currently `private static`, ~3783), and `DescribeWarrantDisposition` (currently `private static`, ~3809) into the coordinator. `TryGetKnownWarrantForSuspect` becomes `internal` on the coordinator; the two statics become `private static` on the coordinator. `SettleUnrelatedCriminalTurnIn` (coordinator, from Task 3) and the confrontation methods (already in coordinator) call `DescribeWarrantDisposition` — update those call sites to the local static. `GameSession` retains `internal bool TryGetKnownWarrantForSuspect(...)` as a delegate to `_bountyLoopCoordinator.TryGetKnownWarrantForSuspect(...)` ONLY if external callers (other `GameSession` partials, application layer, tests) reference it — check with a usage search before deciding. If only the coordinator uses it, remove the `GameSession` wrapper entirely.

- [ ] **Step 5: Build and run the full bounty-loop + dev-saloon test filter**

Run: `dotnet build WildBunch.sln`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~Saloon|FullyQualifiedName~SheriffTurnIn|FullyQualifiedName~WantedSuspect|FullyQualifiedName~BountyLoopCoordinator|FullyQualifiedName~BountySaloonEventSourcing|FullyQualifiedName~DevSaloonOverride|FullyQualifiedName~UnrelatedCriminal"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 6: Commit**

```
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs
git commit -m "BUNCH-112: move dev-saloon commands and bounty-loop helpers into BountyLoopCoordinator"
```

### Task 6: Move the saloon-POI eligibility helpers and `CollectSuspectFeatureDescriptions` into the coordinator

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs` (add `IsEligibleSaloonPersonOfInterestCandidate`, `GetSaloonPoiIneligibilityReason`, `TryGetEligibleSaloonSuspectCandidate`, `CollectSuspectFeatureDescriptions`)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (remove or delegate these helpers)

**Interfaces:**
- Consumes from `GameSession`: `CaseFile`, `CurrentTown`, `CitizenCast`. These helpers are pure functions over `CaseFile`/suspect state.
- Produces: coordinator-owned saloon-POI eligibility helpers.

- [ ] **Step 1: Search for external usages of these helpers before moving**

Run a usage search across the whole solution for `IsEligibleSaloonPersonOfInterestCandidate`, `GetSaloonPoiIneligibilityReason`, `TryGetEligibleSaloonSuspectCandidate`, and `CollectSuspectFeatureDescriptions`. If any caller outside `GameSession.BountyLoopCoordinator.cs` and `GameSession.cs` references them (e.g. application handlers, dev panels, tests), keep a delegate on `GameSession` pointing at the coordinator. If only the coordinator uses them, remove them from `GameSession.cs` entirely.

- [ ] **Step 2: Move the helpers into the coordinator**

Move `IsEligibleSaloonPersonOfInterestCandidate` (currently `internal`, ~3968), `GetSaloonPoiIneligibilityReason` (find it via the usage search), `TryGetEligibleSaloonSuspectCandidate` (currently `private`, ~3945), and `CollectSuspectFeatureDescriptions` (currently `private`, ~3893) into `BountyLoopCoordinator`. Adjust visibility: `internal` for ones with external callers, `private` otherwise. Update call sites in the coordinator (from Tasks 2 and 5) to use the local methods.

- [ ] **Step 3: Build and run the saloon-POI test filter**

Run: `dotnet build WildBunch.sln`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~SaloonPoi|FullyQualifiedName~SaloonPersonOfInterest|FullyQualifiedName~SaloonConfrontation|FullyQualifiedName~DevSaloonOverride"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 4: Commit**

```
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs
git commit -m "BUNCH-112: move saloon-POI eligibility helpers into BountyLoopCoordinator"
```

### Task 7: Final validation, line-count proof, and return evidence

**Files:**
- No source changes expected (only verification)

- [ ] **Step 1: Run the full solution build and test suite**

Run: `dotnet build WildBunch.sln`
Expected: PASS, zero errors.
Run: `dotnet test WildBunch.sln`
Expected: PASS. This requires the PostgreSQL lane — run `.\scripts\postgres-dev.ps1 validate` which provisions the cluster, sets the connection string, restores tools, and runs EF + test checks together. Record exact passed/failed/skipped counts. Report warnings separately from failures.

- [ ] **Step 2: Capture the line-count reduction proof**

Run:
```powershell
$main = (Get-Content src/WildBunch.Domain/Game/GameSession.cs | Measure-Object -Line).Lines
$coord = (Get-Content src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs | Measure-Object -Line).Lines
Write-Output "GameSession.cs: $main lines (was 3588)"
Write-Output "GameSession.BountyLoopCoordinator.cs: $coord lines"
```
Record both numbers. The goal is a meaningful reduction in `GameSession.cs` with the bounty-loop logic consolidated in the coordinator file. There is no hard target — report the actual numbers.

- [ ] **Step 3: Run the index-mesh generator and commit if needed**

Run: `python scripts/generate_index_mesh.py`
If any `INDEX.md` files changed, stage and commit them:
```
git add INDEX.md "*/INDEX.md"
git commit -m "BUNCH-112: refresh index mesh after bounty-loop file moves"
```
If nothing changed, skip the commit. (No files were added/removed — only content moved between two existing files — so the mesh should not change. Verify anyway.)

- [ ] **Step 4: Push the branch and open the PR**

Write the PR body to a body file under `.agents/superpowers/sdd/` (do not use an inline shell heredoc — PowerShell mangles them), then create the PR from the body file:

```
git push -u origin harleydbartles/bunch-112-extract-bountyloop-domain-service
gh pr create --title "BUNCH-112: Extract BountyLoop domain service from GameSession aggregate" --body-file .agents/superpowers/sdd/bunch-112-impl-pr-body.md --base main
```

The PR body file is a worker return artifact. Use the implementation return template below (Step 6) as the body content, with every `<return>` field filled in from the actual run. Do not commit the body file to the branch unless repo policy requires it; the sdd folder is for session artifacts. Record the PR URL.

- [ ] **Step 5: Update Linear route state**

Post a Linear comment on BUNCH-112 with: branch name, head commit, PR URL, line-count proof, and validation counts. Do NOT close the issue (workers do not close issues).

- [ ] **Step 6: Fill in the implementation return template**

The implementation return (PR body + Linear closeout) must fill in every `<return>` field below from the actual run. These are return-time evidence fields, not current plan claims. The plan does not assert any of these values; the implementer proves them at GREEN.

```markdown
## Summary
- Completes the BUNCH-72 `BountyLoopCoordinator` stepping stone: moves all remaining bounty-loop command methods, event `Apply` handlers, dev-saloon-override commands, and saloon-POI eligibility helpers from `GameSession.cs` into `GameSession.BountyLoopCoordinator.cs`.
- `GameSession` retains public command entry points (with `IsArchived`/`IsJourneyModal` guards), the `Apply` dispatch switch, and aggregate-wide helpers. The coordinator is a nested `internal sealed` class — aggregate-owned internal cohesion, not aggregate bypass (ADR-0002, ADR-0020).
- No public API, DTO, event payload, message string, persistence shape, or behavior changes. Pure mechanical move + visibility adjustment.

## Validation
- `dotnet build WildBunch.sln` — PASS (warnings: <return warning count>)
- `dotnet test WildBunch.sln` (via `.\scripts\postgres-dev.ps1 validate`) — PASS, passed: <return>, failed: <return>, skipped: <return>
- Bounty-loop domain test filter — PASS, same counts as Task 1 baseline (baseline: <return>, final: <return>)
- `GameSession.cs` line count: <return> (was 3588)
- `GameSession.BountyLoopCoordinator.cs` line count: <return>
- Index mesh regenerated (changed: <return yes/no>)

#### Test plan
- [ ] Full domain test suite green
- [ ] Full integration test suite green (PostgreSQL lane)
- [ ] Saloon confrontation acceptance tests green
- [ ] Event-sourcing/replay equality tests green (proves `Apply` semantics unchanged)
- [ ] Dev saloon override tests green

Generated with [Devin](https://devin.ai)
```

Replace every `<return ...>` token with the observed value before publishing the PR or posting the Linear closeout. A return that leaves any `<return>` token unfilled is AMBER, not GREEN.

## Self-Review

**1. Spec coverage:**
- "Extract bounty loop logic from GameSession aggregate into a domain service or bounded context" — covered by Tasks 2–6. The "domain service" framing is satisfied by completing the existing `BountyLoopCoordinator` nested-class extraction (the issue explicitly names it as the stepping stone). A standalone service outside the aggregate would violate ADR-0002/0020 and the unslop "Aggregate bypass" rule; the plan documents this constraint in the Architecture section.
- "Use existing `BountyLoopCoordinator` partial class as a stepping stone" — covered; the plan extends it rather than replacing it.
- "Run `dotnet test` to verify no regressions" — Tasks 1–7 all run test filters; Task 7 runs the full suite.
- "Verify aggregate invariants are still enforced" — `GameSession` retains the `IsArchived`/`IsJourneyModal` guards and the `Apply` dispatch switch; the coordinator is nested and has no independent mutation path. Event-sourcing replay tests (Task 4 Step 3) prove `Apply` semantics are unchanged.
- "Verify bounty loop logic is properly encapsulated in the service" — Tasks 2–6 move all bounty-loop logic into the coordinator file; Task 7 reports the line-count proof.
- Files `GameSession.cs` and `GameSession.BountyLoopCoordinator.cs` — both modified across tasks.

**2. Placeholder scan:** No TBDs, no "implement later", no "add appropriate error handling". Every implementation step names exact line ranges, exact method names, and exact delegate bodies. The only template tokens are `<return ...>` fields in the Step 6 implementation return template, which are explicit return-time evidence fields the implementer must fill in from the actual run — they are not plan placeholders and are framed as such (a return that leaves them unfilled is AMBER).

**3. Type consistency:** All result types (`CaseInvestigationResult`, `SheriffTurnInResult`, `SaloonPersonOfInterestConfrontationResult`, `WantedSuspectConfrontationResult`) are referenced by their existing names. Method signatures match the existing public surface. The coordinator's `Apply` handlers use the same `internal void Apply(EventType e)` signature the `GameSession` dispatch expects.

## Architecture falsification checks (run before declaring GREEN)

Before claiming complete, verify the plan did NOT:
- move business rules out of the domain into a handler/service/persistence/UI layer — the coordinator is a nested domain class, not an application service.
- make `BountyLoopCoordinator` a separate aggregate root or repository — it is `internal sealed` nested inside `GameSession`, constructed by `GameSession`'s constructor, with no persistence identity.
- create a broad service that becomes an alternate mutation authority — the coordinator has no public entry point outside `GameSession`; all mutation still enters through `GameSession` public methods.
- change clue, wanted-poster, wallet, inventory, horse, or travel state handling — out of scope, untouched.
- change persistence shape, snapshot codec, or EF entity shape — no migrations, no codec changes.
- leak hidden culprit truth — no DTO or projection changes.
