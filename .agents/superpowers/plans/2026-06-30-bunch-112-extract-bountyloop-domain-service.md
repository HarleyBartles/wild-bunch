# BUNCH-112 Decompose GameSession by Extracting a BountyLoop Child Domain Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Decompose the god `GameSession` aggregate by extracting a real `BountyLoop` child domain component that owns bounty-loop state and behavior through narrow inputs and explicit results, while preserving `GameSession` as the session aggregate root that orchestrates guards, event production, cross-owner mutations, and persistence.

**Architecture:** Replace the existing `BountyLoopCoordinator` (a nested class with unrestricted `_session` access) with a standalone `BountyLoop` class that does NOT reference `GameSession`. `BountyLoop` owns: `WantedSuspectPresenceLedger`, `UnrelatedCriminalLedger`, and `DevSaloonOverride` state. It receives narrow context records as inputs, returns result objects plus events-to-produce (it does not produce events, enter action context, adjust cash, or mutate `CaseFile`/`TownVisitState`/`Player`). `GameSession` retains: public command entry points with `IsArchived`/`IsJourneyModal` guards, `EnterActionContext`, `ProduceEvent`, the `Apply` dispatch switch (calling `_bountyLoop.Apply(e)` for owned-state mutations while applying cross-owner mutations itself), and the persistence/snapshot boundary. JSON snapshot shape is preserved — the same data fields are serialized; only the rehydration path changes to construct `BountyLoop`.

**Tech Stack:** C# / .NET 10, xUnit, existing Wild Bunch domain tests.

## Global Constraints

- `GameSession` remains the live-play aggregate root and the only externally loaded/persisted root (ADR-0002, ADR-0020).
- `BountyLoop` is a child domain component inside the session boundary, NOT a separate aggregate root, NOT a standalone application service, and NOT a nested class with `_session` access. It is an `internal sealed` class in `WildBunch.Domain/Game/BountyLoop.cs` — internal because it is a session-internal component, not a public domain-service surface. If a concrete external caller later requires public visibility, widen at that point with justification.
- `BountyLoop` must NOT reference `GameSession` in any way — no field, no parameter, no method call. This is the key falsification check.
- Do NOT introduce separate persistence tables, repositories, or EF entities for `BountyLoop`. Keep JSON snapshot/runtime-session persistence. The snapshot record shape stays the same; only the rehydration construction path changes.
- Do NOT change any public method signature on `GameSession`, DTO shape, result-object shape, message string, or event payload.
- Do NOT change clue, journal, wanted-poster, wallet, inventory, horse, or travel state handling (out of scope).
- Preserve all existing domain event types and their `Apply(...)` mutation semantics. Event payloads and replay behavior must be identical.
- `BountyLoop` does not call `ProduceEvent`, `EnterActionContext`, `Player.AdjustCash`, `CaseFile.Record*`, `RecordCaseUpdate`, or `CurrentTownVisit.CurrentTownState.Set*`. It returns events as data; `GameSession` produces them and applies cross-owner mutations.
- Run `dotnet build` and `dotnet test` after every task. Run `.\scripts\postgres-dev.ps1 validate` only if a task touches persistence (Tasks 8–9 do).
- If any task reveals a behavior change, STOP and report — do not "fix" it silently.

---

## Boundary Definition

### What `BountyLoop` owns (state + invariants + Apply)

| State | Current location | Moves to `BountyLoop` |
| --- | --- | --- |
| `WantedSuspectPresenceLedger` | `GameSession._wantedSuspectPresenceLedger` | Yes — `BountyLoop._presenceLedger` |
| `UnrelatedCriminalLedger` | `GameSession._unrelatedCriminalLedger` | Yes — `BountyLoop._unrelatedCriminalLedger` |
| `DevSaloonOverride?` (pending) | `GameSession._pendingDevSaloonOverride` | Yes — `BountyLoop._pendingDevSaloonOverride` |

| Behavior | Current location | Moves to `BountyLoop` |
| --- | --- | --- |
| Saloon look-around decision logic (roll, POI selection, dev override consume) | `GameSession.LookAroundSaloon` body | Yes — `BountyLoop.LookAroundSaloon(context)` |
| Saloon POI confrontation decision logic | `BountyLoopCoordinator.ConfrontSaloonPersonOfInterest` | Yes — `BountyLoop.ConfrontSaloonPersonOfInterest(context)` |
| Wanted suspect confrontation decision logic | `BountyLoopCoordinator.ResolveWantedSuspectConfrontation` | Yes — `BountyLoop.ResolveWantedSuspectConfrontation(context)` |
| Sheriff turn-in assess + settle decision logic | `BountyLoopCoordinator.AssessSheriffTurnIn` / `SettleSheriffTurnIn` | Yes — `BountyLoop.AssessSheriffTurnIn(context)` / `SettleSheriffTurnIn(context)` |
| Unrelated criminal turn-in decision logic | `GameSession.SettleUnrelatedCriminalTurnIn` body | Yes — `BountyLoop.SettleUnrelatedCriminalTurnIn(context)` |
| Dev saloon override force/clear/consume validation | `GameSession.ForceDevSaloonOverride` / `ClearDevSaloonOverride` | Yes — `BountyLoop.ForceDevSaloonOverride(override)` / `ClearDevSaloonOverride()` |
| Saloon POI eligibility checks | `GameSession.IsEligibleSaloonPersonOfInterestCandidate` etc. | Yes — `BountyLoop.IsEligibleSaloonPersonOfInterestCandidate(Suspect, CaseFile)` |
| `UpdateWantedSuspectPresence` | `GameSession` private method | Yes — `BountyLoop.Apply(WantedSuspectConfronted)` owned portion |
| `CanConfrontWantedSuspectInCurrentContext` | `GameSession` | Stays on `GameSession` — it reads `CurrentActionContext` + `CurrentTownVisit`, which are session-level state, not bounty-loop state |

### What stays owned by existing domain objects (BountyLoop reads via context, does NOT mutate)

| State | Owner | BountyLoop access |
| --- | --- | --- |
| `CaseFile` (suspects, warrants, confrontation states, settlement states) | `CaseFile` | Read-only via context record fields |
| `CurrentTownVisit.CurrentTownState` (active saloon POI) | `TownVisitState` | Read via context; GameSession mutates from Apply |
| `Player.Wallet` | `Player` | Read cash via context; GameSession mutates from Apply |
| `Clock` (Day, Turn) | `GameClock` | Read via context; GameSession uses in Apply |
| `SaltSource` | `GameSession` | Read salt via context |
| `CitizenCast` | static domain service | Read via context (pre-resolved or role count) |

### What only `GameSession` may orchestrate

- `IsArchived` / `IsJourneyModal` guards
- `EnterActionContext` (advances clock, emits context event)
- `ProduceEvent` (calls Apply + adds to uncommitted events)
- `Apply` dispatch switch
- Cross-owner mutations in Apply handlers: `Player.AdjustCash`, `CaseFile.Record*`, `CurrentTownVisit.CurrentTownState.Set*/Clear*`, `RecordCaseUpdate`/`AddLogEntry`
- `_version++` in every Apply handler
- Snapshot serialization/deserialization coordination

---

## Context Records and Result Types

`BountyLoop` receives narrow inputs via context records and returns results plus events-to-produce. All types live in `WildBunch.Domain/Game/BountyLoop.cs` or a paired `BountyLoopContexts.cs` file.

### Context records

```csharp
/// <summary>Read-only inputs for a saloon look-around decision.</summary>
internal sealed record SaloonLookAroundContext(
    TownId TownId,
    int Day,
    int Turn,
    int VisitNumber,
    string Salt,
    IReadOnlyList<Suspect> EligibleSuspects,
    int CitizenRoleCount,
    bool IsSaloonSourceSpent,
    DevSaloonOverride? PendingDevOverride,
    IReadOnlyList<string> SuspectFeatureDescriptions,
    Func<TownId, int, int, int, IReadOnlyList<string>, CitizenEncounter> CitizenSelect,
    Func<CitizenEncounter, string> CitizenDescriptorResolver);

/// <summary>Read-only inputs for a saloon POI confrontation decision.</summary>
internal sealed record SaloonConfrontationContext(
    SuspectId? ActiveSaloonSuspectId,
    string? ActiveSaloonDescriptor,
    SaloonPersonOfInterestKind? ActiveSaloonPOIKind,
    IReadOnlyList<Suspect> Suspects,
    IReadOnlyList<Warrant> KnownWarrants,
    IReadOnlyDictionary<SuspectId, WantedSuspectConfrontationState> ConfrontationStates,
    bool FirearmThreatAvailable,
    decimal PlayerCash,
    int ClockDay,
    int ClockTurn,
    string? DeclaredWantedIdentityHandle);

/// <summary>Read-only inputs for a wanted-suspect confrontation decision.</summary>
internal sealed record WantedSuspectConfrontationContext(
    SuspectId TargetSuspectId,
    WantedSuspectConfrontationChoice Choice,
    string? DeclaredWantedIdentityHandle,
    bool CanConfrontInCurrentContext,
    IReadOnlyList<Suspect> Suspects,
    IReadOnlyList<Warrant> KnownWarrants,
    IReadOnlyDictionary<SuspectId, WantedSuspectConfrontationState> ConfrontationStates);

/// <summary>Read-only inputs for a sheriff turn-in assess/settle decision.</summary>
internal sealed record SheriffTurnInContext(
    SuspectId TargetSuspectId,
    bool IsAlive,
    bool IsJourneyModal,
    IReadOnlyList<Suspect> Suspects,
    IReadOnlyList<Warrant> KnownWarrants,
    IReadOnlyDictionary<SuspectId, WantedSuspectConfrontationState> ConfrontationStates,
    int ClockDay,
    int ClockTurn);

/// <summary>Read-only inputs for an unrelated-criminal turn-in decision.</summary>
internal sealed record UnrelatedCriminalTurnInContext(
    WarrantId WarrantId,
    bool IsAlive,
    IReadOnlyList<Warrant> KnownWarrants,
    int ClockDay,
    int ClockTurn);
```

### Result wrapper

```csharp
/// <summary>
/// Result from a BountyLoop command method. Carries the public result object
/// plus events that GameSession must produce. BountyLoop does not produce events.
/// </summary>
internal sealed record BountyLoopResult<TResult>(TResult Result, IReadOnlyList<IDomainEvent> Events);
```

---

### Task 1: Characterize the boundary and capture a regression baseline

**Files:**
- Read-only: all bounty-loop test files (listed in the codebase survey)
- Read-only: `src/WildBunch.Domain/Game/GameSession.cs`
- Read-only: `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs`

**Interfaces:**
- Consumes: nothing (baseline step)
- Produces: a green test baseline proving existing bounty-loop behavior is intact before any extraction.

- [ ] **Step 1: Run the bounty-loop domain test filter and capture the green baseline**

Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~Saloon|FullyQualifiedName~SheriffTurnIn|FullyQualifiedName~WantedSuspectConfrontation|FullyQualifiedName~WantedSuspectPresence|FullyQualifiedName~BountyLoopCoordinator|FullyQualifiedName~BountySaloonEventSourcing|FullyQualifiedName~DevSaloonOverride"
```
Expected: PASS. Record exact passed/failed/skipped counts. If any test fails on a clean worktree from `origin/main`, STOP.

- [ ] **Step 2: Run the integration test filter**

Run: `.\scripts\postgres-dev.ps1 ensure`
Then:
```
dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~SaloonConfrontationAcceptance|FullyQualifiedName~DevSaloonEndpoint|FullyQualifiedName~UnrelatedCriminalLedgerPersistence"
```
Expected: PASS. Record counts.

- [ ] **Step 3: Run the full solution build**

Run: `dotnet build WildBunch.sln`
Expected: PASS, zero errors. Record warning count separately.

### Task 2: Create the BountyLoop class skeleton with owned state and context/result types

**Files:**
- Create: `src/WildBunch.Domain/Game/BountyLoop.cs`
- Create: `src/WildBunch.Domain/Game/BountyLoopContexts.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add `_bountyLoop` field alongside the existing `_bountyLoopCoordinator` — do not remove the coordinator yet)

**Interfaces:**
- Consumes: `WantedSuspectPresenceLedger`, `UnrelatedCriminalLedger`, `DevSaloonOverride`, domain event types, `Suspect`, `Warrant`, `CaseFile` (read-only)
- Produces: `BountyLoop` class with state, constructor, context records, result wrapper, and `Apply` methods for owned state (empty bodies for now — filled in later tasks)

- [ ] **Step 1: Create `BountyLoopContexts.cs` with all context records and the result wrapper**

Create the file with the context records and `BountyLoopResult<TResult>` shown in the "Context Records and Result Types" section above. Add `using` directives for `WildBunch.Domain.Cases`, `WildBunch.Domain.Events`, `WildBunch.Domain.World`. The context records are `internal sealed record` types in `namespace WildBunch.Domain.Game` — they are internal because `BountyLoop` is an internal child component inside the `GameSession` session boundary, not a public domain-service surface. If a concrete external caller later requires public visibility, widen at that point with justification.

- [ ] **Step 2: Create `BountyLoop.cs` with the class skeleton**

```csharp
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Domain.Game;

/// <summary>
/// Child domain component inside the GameSession boundary that owns bounty-loop
/// state and behavior. Receives narrow context records, returns results plus
/// events-to-produce. Does NOT reference GameSession, produce events directly,
/// enter action context, adjust cash, or mutate CaseFile/TownVisitState/Player.
/// See BUNCH-112 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class BountyLoop
{
    private readonly WantedSuspectPresenceLedger _presenceLedger;
    private UnrelatedCriminalLedger _unrelatedCriminalLedger;
    private DevSaloonOverride? _pendingDevSaloonOverride;

    internal BountyLoop(
        IReadOnlyList<WantedSuspectPresenceEntry>? presenceEntries,
        UnrelatedCriminalLedger unrelatedCriminalLedger)
    {
        _presenceLedger = new WantedSuspectPresenceLedger(presenceEntries);
        _unrelatedCriminalLedger = unrelatedCriminalLedger
            ?? throw new ArgumentNullException(nameof(unrelatedCriminalLedger));
    }

    internal IReadOnlyList<WantedSuspectPresenceEntry> PresenceEntries => _presenceLedger.Entries;
    internal UnrelatedCriminalLedger UnrelatedCriminalLedger => _unrelatedCriminalLedger;
    internal DevSaloonOverride? PendingDevSaloonOverride => _pendingDevSaloonOverride;

    internal WantedSuspectPresenceState GetWantedSuspectPresenceState(SuspectId suspectId)
        => _presenceLedger.GetState(suspectId);

    internal bool TryGetWantedSuspectPresenceState(SuspectId suspectId, out WantedSuspectPresenceState state)
        => _presenceLedger.TryGetState(suspectId, out state);

    // Command methods — filled in by Tasks 3–7
    // Apply methods — filled in by Task 8

    internal void RestoreUnrelatedCriminalLedger(UnrelatedCriminalLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        _unrelatedCriminalLedger = ledger;
    }

    internal void RestorePendingDevSaloonOverride(DevSaloonOverride? overrideValue)
    {
        _pendingDevSaloonOverride = overrideValue;
    }
}
```

- [ ] **Step 3: Add `_bountyLoop` field to `GameSession` and construct it alongside the coordinator**

In `GameSession`'s constructor, after the existing ledger construction lines (~99–108), add:
```csharp
_bountyLoop = new BountyLoop(wantedSuspectPresenceEntries, _unrelatedCriminalLedger);
```
Add the field declaration near line 44:
```csharp
private readonly BountyLoop _bountyLoop;
```
Do NOT remove `_bountyLoopCoordinator` or any ledger fields yet. Both coexist during the migration.

- [ ] **Step 4: Build and run the full domain test suite**

Run: `dotnet build WildBunch.sln` then `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`
Expected: PASS with the same counts as Task 1. No behavior changed — `BountyLoop` exists but is not yet used.

- [ ] **Step 5: Commit**

```
git add src/WildBunch.Domain/Game/BountyLoop.cs src/WildBunch.Domain/Game/BountyLoopContexts.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-112: add BountyLoop child domain component skeleton with owned state"
```

### Task 3: Move saloon look-around decision logic into BountyLoop

**Files:**
- Modify: `src/WildBunch.Domain/Game/BountyLoop.cs` (add `LookAroundSaloon` method)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (rewire `LookAroundSaloon` to build context, call `_bountyLoop`, produce events)

**Interfaces:**
- Consumes from GameSession: `IsArchived`, `IsJourneyModal()`, `EnterActionContext`, `CurrentTown`, `CurrentTownVisit`, `Clock`, `CaseFile`, `SaltSource`, `CitizenCast`, `ProduceEvent`, `CollectSuspectFeatureDescriptions`, `IsEligibleSaloonPersonOfInterestCandidate`
- Produces: `BountyLoop.LookAroundSaloon(SaloonLookAroundContext)` returning `BountyLoopResult<CaseInvestigationResult>`

- [ ] **Step 1: Implement `BountyLoop.LookAroundSaloon`**

Add the method to `BountyLoop`. Move the decision logic from `GameSession.LookAroundSaloon` (the body after the `EnterActionContext` call, lines ~3169–3336). The method receives a `SaloonLookAroundContext` and returns a `BountyLoopResult<CaseInvestigationResult>` with the result and the `SaloonPersonOfInterestSpotted` event to produce. Key changes from the original:
- Replace `_session.CurrentTown.TownId` with `context.TownId`
- Replace `Clock.Day/Turn` with `context.Day/Turn`
- Replace `CurrentTownVisit.CurrentTownState.VisitNumber` with `context.VisitNumber`
- Replace `SaltSource.Salt` with `context.Salt`
- Replace `CaseFile.Suspects.Where(IsEligibleSaloonPersonOfInterestCandidate)` with `context.EligibleSuspects`
- Replace `CitizenCast.Roles.Count` with `context.CitizenRoleCount`
- Replace `CurrentTownVisit.IsSpent(...)` with `context.IsSaloonSourceSpent`
- Replace `_pendingDevSaloonOverride` with `context.PendingDevOverride`
- Replace `CollectSuspectFeatureDescriptions()` with `context.SuspectFeatureDescriptions`
- Replace `CitizenCast.Select(...)` with `context.CitizenSelect(...)`
- Replace `CitizenCast.ResolveDescriptor(...)` with `context.CitizenDescriptorResolver(...)`
- Instead of `ProduceEvent(spotEvent)`, add the event to the result's `Events` list
- Instead of `ProduceEvent(new DevSaloonOverrideConsumed())`, add it to the `Events` list
- Consume the dev override: set `_pendingDevSaloonOverride = null` (this is owned state — BountyLoop may mutate it)
- Keep `StableSaloonRollHash` as a `private static` method inside `BountyLoop`
- Keep all message strings exactly as they are

- [ ] **Step 2: Rewire `GameSession.LookAroundSaloon` to orchestrate through BountyLoop**

Replace the body of `GameSession.LookAroundSaloon` with:
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

    EnterActionContext(TownActionContext.Saloon);

    var eligibleSuspects = CaseFile.Suspects.Where(IsEligibleSaloonPersonOfInterestCandidate).ToList();
    var context = new SaloonLookAroundContext(
        CurrentTown.TownId,
        Clock.Day,
        Clock.Turn,
        CurrentTownVisit.CurrentTownState.VisitNumber,
        SaltSource.Salt,
        eligibleSuspects,
        CitizenCast.Roles.Count,
        CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround),
        _bountyLoop.PendingDevSaloonOverride,
        CollectSuspectFeatureDescriptions(),
        (townId, day, turn, visit, features) => CitizenCast.Select(townId, day, turn, visit, features),
        encounter => CitizenCast.ResolveDescriptor(encounter));

    var result = _bountyLoop.LookAroundSaloon(context);
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
    return result.Result;
}
```
The `ProduceEvent` calls will trigger `Apply(SaloonPersonOfInterestSpotted)` on `GameSession`, which still handles `CurrentTown.CheckSource`, `RecordCaseUpdate`, and `CurrentTownVisit.CurrentTownState.SetActiveSaloonPersonOfInterest`. The `ProduceEvent(DevSaloonOverrideConsumed)` will trigger `Apply(DevSaloonOverrideConsumed)` which will be rewired in Task 8 to call `_bountyLoop.Apply(...)`.

- [ ] **Step 3: Build and run the saloon test filter**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~Saloon"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 4: Commit**

```
git add src/WildBunch.Domain/Game/BountyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-112: move saloon look-around decision logic into BountyLoop with context-record inputs"
```

### Task 4: Move confrontation decision logic into BountyLoop

**Files:**
- Modify: `src/WildBunch.Domain/Game/BountyLoop.cs` (add `ConfrontSaloonPersonOfInterest`, `ConfrontSaloonWantedSuspect`, `ResolveWantedSuspectConfrontation`)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (rewire the three public confrontation methods)

**Interfaces:**
- Consumes from GameSession: `IsArchived`, `IsJourneyModal()`, `CurrentTownVisit`, `CaseFile`, `Player`, `Clock`, `CanConfrontWantedSuspectInCurrentContext`, `ProduceEvent`
- Produces: three `BountyLoop` methods returning `BountyLoopResult<SaloonPersonOfInterestConfrontationResult>` and `BountyLoopResult<WantedSuspectConfrontationResult>`

- [ ] **Step 1: Implement `BountyLoop.ConfrontSaloonPersonOfInterest`**

Move the decision logic from `BountyLoopCoordinator.ConfrontSaloonPersonOfInterest` (lines ~18–283 of the coordinator file). The method receives a `SaloonConfrontationContext` and returns `BountyLoopResult<SaloonPersonOfInterestConfrontationResult>`. Key changes:
- Replace `_session.CurrentTownVisit.CurrentTownState.*` with context fields
- Replace `_session.CaseFile.Suspects` with `context.Suspects`
- Replace `_session.TryGetKnownWarrantForSuspect(...)` with a local lookup using `context.KnownWarrants` + `MatchesKnownWarrant` (move `MatchesKnownWarrant` as a `private static` method on `BountyLoop`)
- Replace `_session.CaseFile.TryGetWantedSuspectConfrontationState(...)` with `context.ConfrontationStates.TryGetValue(...)`
- Replace `_session.Player.GetCapabilities(...).FirearmThreatAvailable` with `context.FirearmThreatAvailable`
- Replace `_session.Player.Wallet.Cash` with `context.PlayerCash`
- Replace `Clock.Day/Turn` with `context.ClockDay/Turn`
- Instead of `ProduceSaloonConfrontedEvent(...)` / `_session.ProduceEvent(...)`, build the events and add them to the result's `Events` list
- Move `ProduceSaloonConfrontedEvent` as a `private` helper on `BountyLoop` that returns an event instead of producing it
- Move `BuildCitizenRevealNarration` as a `private static` method on `BountyLoop`
- Move `DescribeConfrontationNarration` as a `private static` method on `BountyLoop` (it is currently on `GameSession` — check if it is used elsewhere; if so, keep a delegate on `GameSession`)
- Move `DescribeWarrantDisposition` as a `private static` method on `BountyLoop`
- Keep all message strings exactly as they are

- [ ] **Step 2: Implement `BountyLoop.ConfrontSaloonWantedSuspect` and `BountyLoop.ResolveWantedSuspectConfrontation`**

Move the decision logic from the coordinator methods (lines ~284–454). `ConfrontSaloonWantedSuspect` builds its own context from the saloon context and delegates to `ConfrontSaloonPersonOfInterest`. `ResolveWantedSuspectConfrontation` receives a `WantedSuspectConfrontationContext` and returns `BountyLoopResult<WantedSuspectConfrontationResult>`. The `CanConfrontInCurrentContext` flag comes from `GameSession.CanConfrontWantedSuspectInCurrentContext` (which stays on `GameSession` because it reads `CurrentActionContext` + `CurrentTownVisit`). Instead of `_session.ProduceEvent(confrontationEvent)`, add the event to the result's `Events` list.

- [ ] **Step 3: Rewire `GameSession` confrontation methods to orchestrate through BountyLoop**

Replace the bodies of `GameSession.ConfrontSaloonPersonOfInterest`, `ConfrontSaloonWantedSuspect`, and `ResolveWantedSuspectConfrontation` with guard + context-building + `_bountyLoop` call + event production. Example for `ConfrontSaloonPersonOfInterest`:
```csharp
public SaloonPersonOfInterestConfrontationResult ConfrontSaloonPersonOfInterest(string? declaredWantedIdentityHandle = null)
{
    if (IsArchived)
    {
        return SaloonPersonOfInterestConfrontationResult.Rejected(ArchivedBlockMessage, declaredWantedIdentityHandle);
    }

    var context = new SaloonConfrontationContext(
        CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId,
        CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor,
        CurrentTownVisit.CurrentTownState.ResolveActiveSaloonPersonOfInterestKind(),
        CaseFile.Suspects,
        CaseFile.KnownWarrants,
        CaseFile.WantedSuspectConfrontationStates.ToDictionary(),
        Player.GetCapabilities(TravelRules).FirearmThreatAvailable,
        Player.Wallet.Cash,
        Clock.Day,
        Clock.Turn,
        declaredWantedIdentityHandle);

    var result = _bountyLoop.ConfrontSaloonPersonOfInterest(context);
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
    return result.Result;
}
```
Check whether `CaseFile.WantedSuspectConfrontationStates` exists as a public property or dictionary. If not, build the dictionary from `CaseFile.Suspects` + `CaseFile.TryGetWantedSuspectConfrontationState`. Verify the exact API by inspecting `CaseFile.cs`.

- [ ] **Step 4: Build and run the confrontation test filter**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~Confront|FullyQualifiedName~WantedSuspect"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 5: Commit**

```
git add src/WildBunch.Domain/Game/BountyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-112: move confrontation decision logic into BountyLoop with context-record inputs"
```

### Task 5: Move sheriff turn-in and unrelated-criminal turn-in decision logic into BountyLoop

**Files:**
- Modify: `src/WildBunch.Domain/Game/BountyLoop.cs` (add `AssessSheriffTurnIn`, `SettleSheriffTurnIn`, `SettleUnrelatedCriminalTurnIn`)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (rewire the public methods)

**Interfaces:**
- Consumes from GameSession: `IsArchived`, `IsJourneyModal()`, `EnterActionContext`, `CaseFile`, `Clock`, `_unrelatedCriminalLedger` (now on BountyLoop), `ProduceEvent`
- Produces: three `BountyLoop` methods returning `BountyLoopResult<SheriffTurnInResult>`

- [ ] **Step 1: Implement `BountyLoop.AssessSheriffTurnIn` and `BountyLoop.SettleSheriffTurnIn`**

Move the decision logic from `BountyLoopCoordinator.AssessSheriffTurnIn` and `SettleSheriffTurnIn` (lines ~456–582). `AssessSheriffTurnIn` receives a `SheriffTurnInContext` and returns `SheriffTurnInResult` (no events — assessment is read-only). `SettleSheriffTurnIn` receives the same context, calls `AssessSheriffTurnIn` internally, and returns `BountyLoopResult<SheriffTurnInResult>` with the `SheriffTurnInSettled` event in the `Events` list. Key changes:
- Replace `_session.CaseFile.*` with context fields
- Replace `_session.Clock.*` with context fields
- Replace `_session.EnterActionContext(...)` — this stays on `GameSession` (see Step 3)
- Replace `BountySettlementPolicy.TryCreateSheriffTurnInSettlementState(...)` — keep the call, it is a stateless policy
- Instead of `_session.ProduceEvent(settledEvent)`, add to the result's `Events` list

- [ ] **Step 2: Implement `BountyLoop.SettleUnrelatedCriminalTurnIn`**

Move the decision logic from `GameSession.SettleUnrelatedCriminalTurnIn` (lines ~3440–3505). Receives an `UnrelatedCriminalTurnInContext`, returns `BountyLoopResult<SheriffTurnInResult>` with the `UnrelatedCriminalTurnInSettled` event. Key changes:
- Replace `CaseFile.KnownWarrants` with `context.KnownWarrants`
- Replace `_unrelatedCriminalLedger.IsSurfacingEligible(...)` with `_unrelatedCriminalLedger.IsSurfacingEligible(...)` (this is now owned state — BountyLoop may read it)
- Replace `Clock.Day/Turn` with context fields
- Instead of `ProduceEvent(settledEvent)`, add to the result's `Events` list

- [ ] **Step 3: Rewire `GameSession` sheriff/unrelated methods to orchestrate through BountyLoop**

`GameSession.AssessSheriffTurnIn`:
```csharp
public SheriffTurnInResult AssessSheriffTurnIn(SuspectId targetSuspectId, bool isAlive)
{
    if (IsArchived)
    {
        return SheriffTurnInResult.Rejected(ArchivedBlockMessage);
    }

    var context = new SheriffTurnInContext(
        targetSuspectId, isAlive, IsJourneyModal(),
        CaseFile.Suspects, CaseFile.KnownWarrants,
        CaseFile.WantedSuspectConfrontationStates.ToDictionary(),
        Clock.Day, Clock.Turn);

    return _bountyLoop.AssessSheriffTurnIn(context);
}
```

`GameSession.SettleSheriffTurnIn`:
```csharp
public SheriffTurnInResult SettleSheriffTurnIn(SuspectId targetSuspectId, bool isAlive)
{
    if (IsArchived)
    {
        return SheriffTurnInResult.Rejected(ArchivedBlockMessage);
    }

    var contextChanged = EnterActionContext(TownActionContext.SheriffOffice);
    var context = new SheriffTurnInContext(
        targetSuspectId, isAlive, IsJourneyModal(),
        CaseFile.Suspects, CaseFile.KnownWarrants,
        CaseFile.WantedSuspectConfrontationStates.ToDictionary(),
        Clock.Day, Clock.Turn);

    var result = _bountyLoop.SettleSheriffTurnIn(context);
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
    return result.Result with { SessionChanged = result.Result.SessionChanged || contextChanged };
}
```
Note: `EnterActionContext` stays on `GameSession` (session-level orchestration). The `contextChanged` flag is merged into the result.

`GameSession.SettleUnrelatedCriminalTurnIn`:
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

    var contextChanged = EnterActionContext(TownActionContext.SheriffOffice);
    var context = new UnrelatedCriminalTurnInContext(
        warrantId, isAlive, CaseFile.KnownWarrants, Clock.Day, Clock.Turn);

    var result = _bountyLoop.SettleUnrelatedCriminalTurnIn(context);
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
    return result.Result with { SessionChanged = result.Result.SessionChanged || contextChanged };
}
```

- [ ] **Step 4: Build and run the sheriff + unrelated-criminal test filter**

Run: `dotnet build src/WildBunch.Domain/WildBunch.Domain.csproj`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~SheriffTurnIn|FullyQualifiedName~UnrelatedCriminal"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 5: Commit**

```
git add src/WildBunch.Domain/Game/BountyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-112: move sheriff and unrelated-criminal turn-in decision logic into BountyLoop"
```

### Task 6: Move dev saloon override commands into BountyLoop

**Files:**
- Modify: `src/WildBunch.Domain/Game/BountyLoop.cs` (add `ForceDevSaloonOverride`, `ClearDevSaloonOverride`)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (rewire the public methods)

**Interfaces:**
- Consumes from GameSession: `IsJourneyModal()`, `CaseFile`, `ProduceEvent`
- Produces: `BountyLoop.ForceDevSaloonOverride(...)` and `BountyLoop.ClearDevSaloonOverride()` returning `BountyLoopResult` with dev override events

- [ ] **Step 1: Implement `BountyLoop.ForceDevSaloonOverride` and `BountyLoop.ClearDevSaloonOverride`**

Move the validation + event-construction logic from `GameSession.ForceDevSaloonOverride` (lines ~1335–1383) and `ClearDevSaloonOverride` (lines ~1387–1394). `ForceDevSaloonOverride` receives the `DevSaloonOverride` record plus an eligibility-check function (or the suspect list + eligibility function), validates, sets `_pendingDevSaloonOverride`, and returns `BountyLoopResult<Unit>` with the `DevSaloonOverrideForced` event. `ClearDevSaloonOverride` clears `_pendingDevSaloonOverride` and returns the `DevSaloonOverrideCleared` event. The eligibility check (`IsEligibleSaloonPersonOfInterestCandidate`) is now on `BountyLoop`, so `BountyLoop` can do the validation itself if it receives the `CaseFile` suspects list as a context input. Define:
```csharp
internal BountyLoopResult<bool> ForceDevSaloonOverride(
    DevSaloonOverride overrideValue,
    IReadOnlyList<Suspect> suspects,
    IReadOnlyList<string> citizenRoleKeys,
    bool isJourneyModal)
```
If `isJourneyModal`, throw `InvalidOperationException` (the guard stays on `GameSession` as the entry point — see Step 2). Actually, keep the throw on `GameSession` and have `BountyLoop` assume the journey guard already passed. `BountyLoop` validates suspect/citizen eligibility and returns the event.

- [ ] **Step 2: Rewire `GameSession.ForceDevSaloonOverride` and `ClearDevSaloonOverride`**

```csharp
public void ForceDevSaloonOverride(DevSaloonOverride overrideValue)
{
    if (IsJourneyModal())
    {
        throw new InvalidOperationException("Cannot force a saloon override while a journey is active.");
    }

    var result = _bountyLoop.ForceDevSaloonOverride(
        overrideValue,
        CaseFile.Suspects,
        CitizenCast.Roles.Select(r => r.Key).ToList(),
        isJourneyModal: false);
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
}

public void ClearDevSaloonOverride()
{
    if (_bountyLoop.PendingDevSaloonOverride is null)
    {
        return;
    }

    var result = _bountyLoop.ClearDevSaloonOverride();
    foreach (var e in result.Events)
    {
        ProduceEvent(e);
    }
}
```

- [ ] **Step 3: Move saloon-POI eligibility helpers into BountyLoop**

Move `IsEligibleSaloonPersonOfInterestCandidate`, `GetSaloonPoiIneligibilityReason`, `TryGetEligibleSaloonSuspectCandidate`, and `CollectSuspectFeatureDescriptions` into `BountyLoop` (or keep `CollectSuspectFeatureDescriptions` on `GameSession` if it reads state that `BountyLoop` should not own — inspect it first). `GameSession.LookAroundSaloon` (Task 3) already passes `eligibleSuspects` via context, so `GameSession` still calls `IsEligibleSaloonPersonOfInterestCandidate` to build the context. If that method is now on `BountyLoop`, `GameSession` needs a reference to call it. Options: (a) keep a delegate on `GameSession` that calls `_bountyLoop.IsEligibleSaloonPersonOfInterestCandidate(suspect, CaseFile)`, or (b) keep the method on `GameSession` and also on `BountyLoop`. Prefer (a) — `GameSession` delegates to `_bountyLoop` for the eligibility check, passing `CaseFile` as a read-only input.

- [ ] **Step 4: Build and run the dev saloon override test filter**

Run: `dotnet build WildBunch.sln`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~DevSaloonOverride"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 5: Commit**

```
git add src/WildBunch.Domain/Game/BountyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-112: move dev saloon override commands and eligibility helpers into BountyLoop"
```

### Task 7: Move owned-state Apply handlers into BountyLoop and rewire GameSession dispatch

**Files:**
- Modify: `src/WildBunch.Domain/Game/BountyLoop.cs` (add `Apply` methods for owned state)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (update `Apply` handlers to call `_bountyLoop.Apply` for owned-state mutations)

**Interfaces:**
- Consumes from GameSession: the existing `Apply` dispatch switch
- Produces: `BountyLoop.Apply(WantedSuspectConfronted)`, `.Apply(SheriffTurnInSettled)`, `.Apply(UnrelatedCriminalTurnInSettled)`, `.Apply(DevSaloonOverrideForced)`, `.Apply(DevSaloonOverrideCleared)`, `.Apply(DevSaloonOverrideConsumed)` — all `internal void`

- [ ] **Step 1: Add owned-state Apply methods to BountyLoop**

These methods mutate ONLY BountyLoop-owned state. They do NOT touch `Player`, `CaseFile`, `CurrentTownVisit`, logs, or `_version`.

```csharp
internal void Apply(WantedSuspectConfronted e)
{
    if (e.Outcome is not WantedSuspectConfrontationOutcome.Abandoned)
    {
        UpdateWantedSuspectPresence(e.TargetSuspectId, e.Choice);
    }
}

internal void Apply(SheriffTurnInSettled e)
{
    _unrelatedCriminalLedger.RecordGangMemberTakenIn();
}

internal void Apply(UnrelatedCriminalTurnInSettled e)
{
    _unrelatedCriminalLedger.MarkWarrantCollected(e.WarrantId);
    _unrelatedCriminalLedger.RecordTakenIn(e.WarrantId);
}

internal void Apply(DevSaloonOverrideForced e)
{
    _pendingDevSaloonOverride = new DevSaloonOverride(
        e.ForcedKind, e.ForcedSuspectId, e.ForcedCitizenRoleKey);
}

internal void Apply(DevSaloonOverrideCleared e)
{
    _pendingDevSaloonOverride = null;
}

internal void Apply(DevSaloonOverrideConsumed e)
{
    _pendingDevSaloonOverride = null;
}
```

Move `UpdateWantedSuspectPresence` as a `private` method on `BountyLoop`:
```csharp
private void UpdateWantedSuspectPresence(SuspectId suspectId, WantedSuspectConfrontationChoice choice)
{
    var nextPresenceState = choice switch
    {
        WantedSuspectConfrontationChoice.Surrendered => WantedSuspectPresenceState.SecuredAlive,
        WantedSuspectConfrontationChoice.Fled => WantedSuspectPresenceState.GoneToGround,
        WantedSuspectConfrontationChoice.Killed => WantedSuspectPresenceState.SecuredDead,
        _ => WantedSuspectPresenceState.Unavailable
    };

    if (nextPresenceState != WantedSuspectPresenceState.Unavailable)
    {
        _presenceLedger.SetState(suspectId, nextPresenceState);
    }
}
```

- [ ] **Step 2: Update GameSession Apply handlers to delegate owned-state mutations to BountyLoop**

Update each `Apply` handler in `GameSession` to call `_bountyLoop.Apply(e)` for the owned-state portion while retaining the cross-owner mutations and `_version++`. Example for `Apply(WantedSuspectConfronted)`:
```csharp
private void Apply(WantedSuspectConfronted e)
{
    RecordCaseUpdate(e.Message);

    if (e.Outcome is not WantedSuspectConfrontationOutcome.Abandoned)
    {
        var confrontationState = new WantedSuspectConfrontationState(
            e.TargetSuspectId, e.TargetName, e.Disposition,
            e.Outcome, e.IsAlive, e.IsSecured, Clock.Day, Clock.Turn);
        CaseFile.RecordWantedSuspectConfrontationState(confrontationState);
        _bountyLoop.Apply(e); // owns presence ledger mutation
    }

    _version++;
}
```

For `Apply(SheriffTurnInSettled)`:
```csharp
private void Apply(SheriffTurnInSettled e)
{
    Player.AdjustCash(e.BountyAmount);
    var settlementState = new SheriffTurnInSettlementState(
        e.TargetSuspectId, e.TargetName, e.Disposition,
        e.IsAlive, e.BountyAmount, e.Day, e.Turn);
    CaseFile.RecordSheriffTurnInSettlementState(settlementState);
    _bountyLoop.Apply(e); // owns unrelated-criminal ledger mutation
    _version++;
}
```

For `Apply(UnrelatedCriminalTurnInSettled)`:
```csharp
private void Apply(UnrelatedCriminalTurnInSettled e)
{
    Player.AdjustCash(e.BountyAmount);
    _bountyLoop.Apply(e); // owns unrelated-criminal ledger mutation
    _version++;
}
```

For `Apply(SaloonPersonOfInterestConfronted)` — no owned state, no delegation needed. Keep as-is.

For `Apply(SaloonPersonOfInterestSpotted)` — no owned state, no delegation needed. Keep as-is.

For the three dev saloon override Apply handlers, replace the direct `_pendingDevSaloonOverride` mutations with `_bountyLoop.Apply(e)` calls:
```csharp
internal void Apply(DevSaloonOverrideForced e)
{
    _bountyLoop.Apply(e);
    _version++;
}

internal void Apply(DevSaloonOverrideCleared e)
{
    _bountyLoop.Apply(e);
    _version++;
}

internal void Apply(DevSaloonOverrideConsumed e)
{
    _bountyLoop.Apply(e);
    _version++;
}
```

- [ ] **Step 3: Build and run the event-sourcing + replay test filter**

Run: `dotnet build WildBunch.sln`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~BountySaloonEventSourcing|FullyQualifiedName~InvestigationEventSourcing|FullyQualifiedName~DevSaloonOverride|FullyQualifiedName~TravelReplayEquality|FullyQualifiedName~WantedSuspectPresence"
```
Expected: PASS with the same counts as Task 1. The replay/equality tests are the critical proof that Apply semantics are unchanged.

- [ ] **Step 4: Commit**

```
git add src/WildBunch.Domain/Game/BountyLoop.cs src/WildBunch.Domain/Game/GameSession.cs
git commit -m "BUNCH-112: move owned-state Apply handlers into BountyLoop, rewire GameSession dispatch"
```

### Task 8: Update snapshot rehydration to construct BountyLoop

**Files:**
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add internal rehydration method for BountyLoop)

**Interfaces:**
- Consumes: existing snapshot record shape (unchanged)
- Produces: rehydration path that constructs `BountyLoop` and sets it on `GameSession`

- [ ] **Step 1: Add internal rehydration method on GameSession for BountyLoop**

Add to `GameSession`:
```csharp
internal void RestoreBountyLoopState(
    UnrelatedCriminalLedger? unrelatedCriminalLedger,
    DevSaloonOverride? pendingDevSaloonOverride)
{
    if (unrelatedCriminalLedger is not null)
    {
        _bountyLoop.RestoreUnrelatedCriminalLedger(unrelatedCriminalLedger);
    }
    if (pendingDevSaloonOverride is not null)
    {
        _bountyLoop.RestorePendingDevSaloonOverride(pendingDevSaloonOverride);
    }
}
```

- [ ] **Step 2: Update the snapshot ToDomain rehydration path**

In `GameSessionSnapshot.ToDomain()` (lines ~102–113 of `SessionSnapshot.cs`), replace the direct `SetBackingField(session, "_pendingDevSaloonOverride", ...)` and `SetUnrelatedCriminalLedger(session, ...)` calls with:
```csharp
if (PendingDevSaloonOverride is not null || UnrelatedCriminalLedger is not null)
{
    session.RestoreBountyLoopState(
        UnrelatedCriminalLedger is not null
            ? WildBunch.Domain.Cases.UnrelatedCriminalLedger.FromSnapshot(UnrelatedCriminalLedger)
            : null,
        PendingDevSaloonOverride);
}
```
The `WantedSuspectPresenceLedger` entries are already passed through the `GameSession` constructor (as `wantedSuspectPresenceEntries`), and `GameSession`'s constructor now constructs `BountyLoop` from them. So no change needed for presence entries.

- [ ] **Step 3: Remove the now-unused `SetUnrelatedCriminalLedger` and `_pendingDevSaloonOverride` SetBackingField calls from the rehydrator**

Remove `SetUnrelatedCriminalLedger` from `GameSessionRehydrator.cs` if it is no longer called anywhere. Remove the `_pendingDevSaloonOverride` `SetBackingField` call from `SessionSnapshot.cs`. Search for any remaining callers before removing.

- [ ] **Step 4: Build and run the persistence + integration test filter**

Run: `.\scripts\postgres-dev.ps1 ensure`
Then: `dotnet build WildBunch.sln`
Expected: PASS.
Run:
```
dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~EfGameSessionRepository|FullyQualifiedName~EventStorePersistence|FullyQualifiedName~UnrelatedCriminalLedgerPersistence|FullyQualifiedName~GameSessionDifficultyPersistence"
```
Expected: PASS with the same counts as Task 1.

- [ ] **Step 5: Commit**

```
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs
git commit -m "BUNCH-112: update snapshot rehydration to construct BountyLoop from persisted state"
```

### Task 9: Remove the old BountyLoopCoordinator and dead code, then final validation

**Files:**
- Delete: `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (remove `_bountyLoopCoordinator` field, constructor line, and any dead helper methods that were only used by the coordinator)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (remove old `_wantedSuspectPresenceLedger`, `_unrelatedCriminalLedger`, `_pendingDevSaloonOverride` fields if they are now fully owned by BountyLoop — verify no remaining direct references)

**Interfaces:**
- Consumes: all previous tasks
- Produces: a clean codebase with no dead coordinator code

- [ ] **Step 1: Remove `BountyLoopCoordinator` and its field/construction from GameSession**

Delete `GameSession.BountyLoopCoordinator.cs`. Remove `private readonly BountyLoopCoordinator _bountyLoopCoordinator;` and `_bountyLoopCoordinator = new BountyLoopCoordinator(this);` from `GameSession`. Search for any remaining references to `_bountyLoopCoordinator` or `BountyLoopCoordinator` and remove them.

- [ ] **Step 2: Remove dead ledger/override fields from GameSession if fully migrated**

Search for remaining direct references to `_wantedSuspectPresenceLedger`, `_unrelatedCriminalLedger`, and `_pendingDevSaloonOverride` in `GameSession.cs`. If all access now goes through `_bountyLoop`, remove the fields. Update `GameSession`'s public properties (`WantedSuspectPresenceEntries`, `PendingDevSaloonOverride`, `GetWantedSuspectPresenceState`, `TryGetWantedSuspectPresenceState`, `SetWantedSuspectPresenceState`) to delegate to `_bountyLoop`. The `UnrelatedCriminalLedger` property should delegate to `_bountyLoop.UnrelatedCriminalLedger`.

- [ ] **Step 3: Update the test that directly constructs BountyLoopCoordinator**

`GameSessionBountyLoopCoordinatorTests.cs` constructs `new GameSession.BountyLoopCoordinator(session)`. This test must be updated to either test `BountyLoop` directly (constructing it with its own state) or removed if the behavior is already covered by the `GameSession`-level tests. Inspect the test — if it tests `SettleSheriffTurnIn` rejection before securing, that behavior is now tested through `GameSession.SettleSheriffTurnIn` → `_bountyLoop.SettleSheriffTurnIn`. Update the test to call through `GameSession` or construct a `BountyLoop` directly with the appropriate context.

- [ ] **Step 4: Run the full solution build and test suite**

Run: `.\scripts\postgres-dev.ps1 validate`
Expected: PASS. Record exact passed/failed/skipped counts. Report warnings separately.

- [ ] **Step 5: Run falsification checks**

Verify each of these is true by inspecting the final code:
1. `BountyLoop` does NOT reference `GameSession` — search `BountyLoop.cs` and `BountyLoopContexts.cs` for `GameSession` and confirm zero matches.
2. `BountyLoop` does NOT call `ProduceEvent`, `EnterActionContext`, `Player.AdjustCash`, `CaseFile.Record*`, `RecordCaseUpdate`, or `CurrentTownVisit.CurrentTownState.Set*` — search for these patterns and confirm zero matches.
3. `GameSession` no longer directly owns bounty-loop decision rules — the public method bodies are guard + context-build + `_bountyLoop` call + event production, not decision logic.
4. `GameSession` still controls session-level guards, `EnterActionContext`, `ProduceEvent`, the `Apply` dispatch, and `_version++`.
5. All bounty-loop tests pass with the same counts as the Task 1 baseline.

- [ ] **Step 6: Capture the line-count and structure proof**

Run:
```powershell
$main = (Get-Content src/WildBunch.Domain/Game/GameSession.cs | Measure-Object -Line).Lines
$bounty = (Get-Content src/WildBunch.Domain/Game/BountyLoop.cs | Measure-Object -Line).Lines
Write-Output "GameSession.cs: $main lines (was 3588)"
Write-Output "BountyLoop.cs: $bounty lines"
Write-Output "BountyLoopCoordinator.cs: deleted"
```
Record both numbers.

- [ ] **Step 7: Regenerate the index mesh and commit if needed**

Run: `python scripts/generate_index_mesh.py`
If any `INDEX.md` files changed (a file was deleted and two were added), stage and commit:
```
git add INDEX.md "*/INDEX.md"
git commit -m "BUNCH-112: refresh index mesh after BountyLoop extraction"
```

- [ ] **Step 8: Push the branch and open the PR**

Write the PR body to `.agents/superpowers/sdd/bunch-112-impl-pr-body.md` using the implementation return template (Step 9), then:
```
git push -u origin harleydbartles/bunch-112-extract-bountyloop-domain-service
gh pr create --title "BUNCH-112: Decompose GameSession by extracting BountyLoop child domain boundary" --body-file .agents/superpowers/sdd/bunch-112-impl-pr-body.md --base main
```
Record the PR URL.

- [ ] **Step 9: Fill in the implementation return template**

The implementation return (PR body + Linear closeout) must fill in every `<return>` field below from the actual run. These are return-time evidence fields, not current plan claims. A return that leaves any `<return>` unfilled is AMBER, not GREEN.

```markdown
## Summary
- Decomposes the god GameSession aggregate by extracting a real BountyLoop child domain component that owns bounty-loop state and behavior through narrow context-record inputs and explicit results+events.
- BountyLoop owns: WantedSuspectPresenceLedger, UnrelatedCriminalLedger, DevSaloonOverride state. It does NOT reference GameSession, produce events, enter action context, adjust cash, or mutate CaseFile/TownVisitState/Player.
- GameSession retains: public command entry points (guards + orchestration), EnterActionContext, ProduceEvent, Apply dispatch (calling BountyLoop.Apply for owned state + applying cross-owner mutations itself), and the persistence boundary.
- No public API, DTO, event payload, message string, or snapshot shape changes. JSON snapshot persistence preserved.

## Falsification checks
- BountyLoop does NOT reference GameSession: <return confirm zero matches>
- BountyLoop does NOT call ProduceEvent/EnterActionContext/Player.AdjustCash/CaseFile.Record*/RecordCaseUpdate/CurrentTownVisit.Set*: <return confirm zero matches>
- GameSession no longer directly owns bounty-loop decision rules: <return confirm method bodies are guard+context+call+produce>
- GameSession still controls guards, EnterActionContext, ProduceEvent, Apply dispatch, _version++: <return confirm>
- All bounty-loop tests pass with same counts as baseline: <return baseline vs final counts>

## Validation
- dotnet build WildBunch.sln — PASS (warnings: <return>)
- dotnet test WildBunch.sln (via postgres-dev.ps1 validate) — PASS, passed: <return>, failed: <return>, skipped: <return>
- GameSession.cs line count: <return> (was 3588)
- BountyLoop.cs line count: <return>
- BountyLoopCoordinator.cs: deleted
- Index mesh regenerated (changed: <return yes/no>)

#### Test plan
- [ ] Full domain test suite green
- [ ] Full integration test suite green (PostgreSQL lane)
- [ ] Saloon confrontation acceptance tests green
- [ ] Event-sourcing/replay equality tests green (proves Apply semantics unchanged)
- [ ] Dev saloon override tests green
- [ ] Unrelated criminal ledger persistence tests green

Generated with [Devin](https://devin.ai)
```

- [ ] **Step 10: Update Linear route state**

Post a Linear comment on BUNCH-112 with: branch name, head commit, PR URL, falsification check results, line-count proof, and validation counts. Do NOT close the issue.

## Self-Review

**1. Spec coverage:**
- "Decompose the god GameSession aggregate by extracting a real bounty-loop child domain boundary" — Tasks 2–9 extract `BountyLoop` as a standalone class with owned state, narrow inputs, and explicit results. This is a real domain decomposition, not a file move.
- "Preserving GameSession as the session aggregate root" — GameSession retains guards, EnterActionContext, ProduceEvent, Apply dispatch, persistence boundary. Documented in Architecture section and verified by Task 9 Step 5.
- "Do NOT implement a standalone service that bypasses GameSession" — BountyLoop is a child component, not a service. GameSession orchestrates all command entry.
- "Do NOT introduce separate persistence tables or repositories" — JSON snapshot shape preserved (Task 8 only changes rehydration construction path).
- "Do NOT just move methods into a nested helper with _session access" — BountyLoop is NOT nested and does NOT take GameSession. Falsification check in Task 9 Step 5 proves zero GameSession references.
- "Narrow inputs via context records" — Tasks 2 defines context records; Tasks 3–6 use them.
- "Returns explicit results + events to produce" — `BountyLoopResult<TResult>` wrapper defined in Task 2, used in all command methods.
- "Owns Apply handlers for its own state" — Task 7 moves owned-state Apply methods into BountyLoop.
- "Does not produce events directly, enter action context, adjust cash, or mutate CaseFile/TownVisitState/Player" — Falsification check in Task 9 Step 5.
- "GameSession orchestrates: builds context, calls BountyLoop, produces events, retains Apply dispatch" — all rewired in Tasks 3–7.
- "Preserve hidden culprit truth, clue/journal/wanted-poster behavior, event payloads, message strings, DTOs, snapshot shape, replay semantics" — Global Constraints + per-task test gates.
- "Falsification checks" — Task 9 Step 5 covers all five required checks.

**2. Placeholder scan:** No TBDs, no "implement later", no "add appropriate error handling". Every implementation step names exact methods, exact context fields, and exact rewired bodies. The only template tokens are `<return>` fields in the Step 9 implementation return template, which are explicit return-time evidence fields — a return leaving them unfilled is AMBER.

**3. Type consistency:** `BountyLoopResult<TResult>` is used consistently across all command methods. Context record field names match between definition (Task 2) and usage (Tasks 3–6). `BountyLoop.Apply` method signatures match the event types used in the GameSession dispatch (Task 7). The `RestoreBountyLoopState` internal method (Task 8) matches the rehydration call in `SessionSnapshot.ToDomain`.

## Architecture falsification checks (run before declaring GREEN)

Before claiming complete, verify the plan did NOT:
- leave `BountyLoop` with any reference to `GameSession` — search and confirm zero matches.
- leave `BountyLoop` calling `ProduceEvent`, `EnterActionContext`, `Player.AdjustCash`, `CaseFile.Record*`, `RecordCaseUpdate`, or `CurrentTownVisit.CurrentTownState.Set*` — search and confirm zero matches.
- move business rules into a handler/service/persistence/UI layer — `BountyLoop` is a domain class in `WildBunch.Domain`.
- make `BountyLoop` a separate aggregate root or repository — it has no persistence identity, no `IAggregateRoot`, no repository.
- create a broad service that becomes an alternate mutation authority — `BountyLoop` returns events as data; `GameSession` produces them.
- change clue, wanted-poster, wallet, inventory, horse, or travel state handling — out of scope, untouched.
- change persistence shape, snapshot codec record, or EF entity shape — snapshot record unchanged; only rehydration construction path changes.
- change event payloads, message strings, DTOs, or replay semantics — per-task test gates + Task 9 full suite.
