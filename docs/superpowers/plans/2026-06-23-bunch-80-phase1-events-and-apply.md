# BUNCH-80 Phase 1: Clock/Turn Correction + Events + Apply + Domain Method Migration

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Decouple clock advancement from `RecordCaseUpdate`, introduce `TimeOfDay`-named turns and event-sourced action-context-based advancement, then create 5 typed domain events with 5 Apply methods (including `TownActionContextEntered`) and migrate 5 bounty/saloon domain methods to the event-sourced pattern.

**Architecture:** Clock advancement moves from `RecordCaseUpdate(advanceClock: true)` to `EnterActionContext(TownActionContext)`. Events do NOT carry `AdvanceClock`. Apply methods call `RecordCaseUpdate(msg)` (pure log, no clock). Domain methods call `EnterActionContext` before producing events. The `BountyLoopCoordinator` keeps its decision logic but delegates mutation to events.

**Tech Stack:** C#/.NET 10, xUnit, sealed record domain events implementing `IDomainEvent`

## Global Constraints

- Events carry only public data — no `TrueCulpritId`, `LinkedSuspectIds`, `TargetKind`, or `KillerReleaseState`
- Events do NOT carry `AdvanceClock` — clock advancement is handled by `EnterActionContext`, not by events
- `Apply` methods call `RecordCaseUpdate(msg)` as a transitional log bridge — NO clock advancement in Apply
- `RecordCaseUpdate` loses its `advanceClock` parameter — it becomes a pure log append
- `GameSession` remains the aggregate root; `BountyLoopCoordinator` stays internal
- Composite operations may produce multiple events per command
- Non-state-changing rejections produce no gameplay events, but actions that enter a new action context (e.g., going to the sheriff's office) still produce a `TownActionContextEntered` event even if the action itself is rejected — the player went there, time passed
- `TimeOfDay` is a naming layer on the existing int `Turn` (0-3) — no persistence format change
- `TownActionContext` is a simple enum tracked on `GameSession` — no complex location model
- Trail advancement stays day-level (`Clock.AdvanceTravelDay()` unchanged)

---

## Task 1: TimeOfDay + TownActionContext + TownActionContextEntered Event + EnterActionContext + RecordCaseUpdate Decoupling

**Files:**
- Create: `src/WildBunch.Domain/Game/TimeOfDay.cs`
- Create: `src/WildBunch.Domain/Game/TownActionContext.cs`
- Create: `src/WildBunch.Domain/Events/TownActionContextEntered.cs`
- Modify: `src/WildBunch.Domain/Game/GameClock.cs` (add `TimeOfDay` property + `Set` method)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add `CurrentActionContext` field, `EnterActionContext` method that emits event, `Apply(TownActionContextEntered)`, decouple `RecordCaseUpdate`)
- Test: `tests/WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs`

**Key principle:** Every clock/context mutation is event-sourced. `EnterActionContext` emits a `TownActionContextEntered` event. `Apply(TownActionContextEntered)` sets both `CurrentActionContext` and `Clock` from the event. Replay reconstructs the exact same state.

- [ ] **Step 1: Write failing tests for event-sourced clock/context**

```csharp
// tests/WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs
[Fact]
public void RecordCaseUpdate_DoesNotAdvanceClock()
{
    var session = TestSessionFactory.CreateDefault();
    var turnBefore = session.Clock.Turn;
    session.RecordCaseUpdateForTesting("test message");
    Assert.Equal(turnBefore, session.Clock.Turn);
}

[Fact]
public void EnterActionContext_DifferentContext_ProducesEventAndAdvancesTurn()
{
    var session = TestSessionFactory.CreateDefault();
    Assert.Equal(TownActionContext.None, session.CurrentActionContext);
    var turnBefore = session.Clock.Turn;

    session.EnterActionContextForTesting(TownActionContext.Saloon);

    Assert.Equal(TownActionContext.Saloon, session.CurrentActionContext);
    Assert.Equal(turnBefore + 1, session.Clock.Turn);
    // Event-sourced: the context entry produced an event
    var contextEvent = Assert.Single(session.UncommittedEvents.OfType<TownActionContextEntered>());
    Assert.Equal(TownActionContext.Saloon, contextEvent.Context);
    Assert.Equal(turnBefore + 1, contextEvent.Turn);
}

[Fact]
public void EnterActionContext_SameContext_DoesNotProduceEventOrAdvanceTurn()
{
    var session = TestSessionFactory.CreateDefault();
    session.EnterActionContextForTesting(TownActionContext.Saloon);
    session.MarkEventsCommittedForTesting();
    var turnAfterFirstEntry = session.Clock.Turn;

    session.EnterActionContextForTesting(TownActionContext.Saloon);

    Assert.Equal(turnAfterFirstEntry, session.Clock.Turn);
    Assert.Empty(session.UncommittedEvents); // No new event
}

[Fact]
public void EnterActionContext_None_DoesNotProduceEventOrAdvanceTurn()
{
    var session = TestSessionFactory.CreateDefault();
    var turnBefore = session.Clock.Turn;
    session.EnterActionContextForTesting(TownActionContext.None);
    Assert.Equal(turnBefore, session.Clock.Turn);
    Assert.Empty(session.UncommittedEvents);
}

[Fact]
public void Replay_TownActionContextEntered_ReconstructsContextAndClock()
{
    var session = TestSessionFactory.CreateDefault();
    session.EnterActionContextForTesting(TownActionContext.Saloon);
    session.EnterActionContextForTesting(TownActionContext.SheriffOffice);
    var events = session.UncommittedEvents.ToList();
    var contextAfterCommands = session.CurrentActionContext;
    var dayAfterCommands = session.Clock.Day;
    var turnAfterCommands = session.Clock.Turn;

    var replayed = GameSession.RehydrateFromEvents(
        session.Id, session.World, session.CaseFile,
        /* GameStarted + */ events);

    Assert.Equal(contextAfterCommands, replayed.CurrentActionContext);
    Assert.Equal(dayAfterCommands, replayed.Clock.Day);
    Assert.Equal(turnAfterCommands, replayed.Clock.Turn);
}

[Fact]
public void TimeOfDay_MapsFromTurnCorrectly()
{
    var clock = new GameClock();
    Assert.Equal(TimeOfDay.Morning, clock.TimeOfDay);

    clock.Advance();
    Assert.Equal(TimeOfDay.Afternoon, clock.TimeOfDay);

    clock.Advance();
    Assert.Equal(TimeOfDay.Evening, clock.TimeOfDay);

    clock.Advance();
    Assert.Equal(TimeOfDay.Night, clock.TimeOfDay);

    clock.Advance();
    Assert.Equal(TimeOfDay.Morning, clock.TimeOfDay);
    Assert.Equal(2, clock.Day);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "ClockTurnCorrection"`
Expected: FAIL — `TimeOfDay`, `TownActionContext`, `TownActionContextEntered` types not found

- [ ] **Step 3: Create TimeOfDay enum**

```csharp
// src/WildBunch.Domain/Game/TimeOfDay.cs
namespace WildBunch.Domain.Game;

public enum TimeOfDay
{
    Morning = 0,
    Afternoon = 1,
    Evening = 2,
    Night = 3
}
```

- [ ] **Step 4: Create TownActionContext enum**

```csharp
// src/WildBunch.Domain/Game/TownActionContext.cs
namespace WildBunch.Domain.Game;

public enum TownActionContext
{
    None = 0,
    SheriffOffice = 1,
    Saloon = 2,
    Store = 3,
    Stable = 4,
    Jail = 5,
    TelegraphOffice = 6,
    TownSquare = 7
}
```

- [ ] **Step 5: Create TownActionContextEntered event**

```csharp
// src/WildBunch.Domain/Events/TownActionContextEntered.cs
using WildBunch.Domain.Game;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player entered a new action context within the current town,
/// advancing the turn. Carries the resulting context and clock state so that
/// replay can reconstruct both CurrentActionContext and Clock without divergence.
/// See ADR-0028 + BUNCH-80 clock/turn correction.
/// </summary>
public sealed record TownActionContextEntered : IDomainEvent
{
    public required TownActionContext Context { get; init; }
    public required int Day { get; init; }
    public required int Turn { get; init; }
    public required TimeOfDay TimeOfDay { get; init; }
}
```

- [ ] **Step 6: Add TimeOfDay + Set to GameClock**

```csharp
// src/WildBunch.Domain/Game/GameClock.cs
public TimeOfDay TimeOfDay => (TimeOfDay)Turn;

/// <summary>
/// Sets the clock to an exact day/turn. Used by Apply(TownActionContextEntered)
/// during replay to reconstruct clock state from the event.
/// </summary>
public void Set(int day, int turn)
{
    Day = day;
    Turn = turn;
}
```

- [ ] **Step 7: Add CurrentActionContext, EnterActionContext, and Apply to GameSession**

```csharp
public TownActionContext CurrentActionContext { get; private set; } = TownActionContext.None;

/// <summary>
/// Enters an action context within the current town. If the context is different
/// from the current one, emits a TownActionContextEntered event that advances the
/// turn and records the resulting context/clock state. If same context, no event
/// and no turn advance. TownActionContext.None never produces an event.
/// This is event-sourced: the event carries the resulting Day/Turn/TimeOfDay so
/// replay reconstructs the exact same state.
/// </summary>
public bool EnterActionContext(TownActionContext context)
{
    if (context == TownActionContext.None || context == CurrentActionContext)
        return false;

    // Compute resulting clock state (do NOT mutate Clock directly — Apply does that)
    var newTurn = Clock.Turn + 1;
    var newDay = Clock.Day;
    if (newTurn >= 4) { newDay++; newTurn = 0; }

    var e = new TownActionContextEntered
    {
        Context = context,
        Day = newDay,
        Turn = newTurn,
        TimeOfDay = (TimeOfDay)newTurn
    };
    ProduceEvent(e);
    return true;
}

private void Apply(TownActionContextEntered e)
{
    CurrentActionContext = e.Context;
    Clock.Set(e.Day, e.Turn);
    _version++;
}
```

**Critical design point:** `EnterActionContext` does NOT call `Clock.Advance()` directly. It computes the resulting day/turn, creates the event with those values, and calls `ProduceEvent` which calls `Apply`. `Apply` sets the clock from the event via `Clock.Set`. This ensures command execution and replay produce identical state.

- [ ] **Step 8: Add ProduceEvent helper**

```csharp
internal void ProduceEvent<T>(T e) where T : IDomainEvent
{
    Apply(e);
    _uncommittedEvents.Add(e);
}
```

- [ ] **Step 9: Decouple RecordCaseUpdate from clock**

```csharp
// BEFORE (GameSession.cs:2019-2027):
public void RecordCaseUpdate(string message, bool advanceClock = true)
{
    if (advanceClock) { Clock.Advance(); }
    AddLogEntry(GameLogEntryKind.CaseUpdate, message);
}

// AFTER:
public void RecordCaseUpdate(string message)
{
    AddLogEntry(GameLogEntryKind.CaseUpdate, message);
}
```

- [ ] **Step 10: Remove AdvanceClock from InvestigationPerformed event**

In `src/WildBunch.Domain/Events/InvestigationPerformed.cs`, remove:
```csharp
public bool AdvanceClock { get; init; } = true;
```

Update `Apply(InvestigationPerformed)` at `GameSession.cs:281`:
```csharp
// BEFORE:
RecordCaseUpdate(e.Message, advanceClock: e.AdvanceClock);
// AFTER:
RecordCaseUpdate(e.Message);
```

**Note:** Investigation tests will now fail because the clock doesn't advance from `RecordCaseUpdate`. Task 6 adds `EnterActionContext` calls to investigation methods, which will restore clock advancement via the `TownActionContextEntered` event.

- [ ] **Step 11: Run clock decoupling tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "ClockTurnCorrection"`
Expected: PASS

- [ ] **Step 12: Commit**

```bash
git add src/WildBunch.Domain/Game/TimeOfDay.cs src/WildBunch.Domain/Game/TownActionContext.cs src/WildBunch.Domain/Events/TownActionContextEntered.cs src/WildBunch.Domain/Game/GameClock.cs src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Events/InvestigationPerformed.cs tests/WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs
git commit -m "BUNCH-80: event-sourced clock/context via TownActionContextEntered, decouple RecordCaseUpdate from clock"
```

---

## Task 2: SaloonPersonOfInterestSpotted Event + LookAroundSaloon Migration

**Files:**
- Create: `src/WildBunch.Domain/Events/SaloonPersonOfInterestSpotted.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs:1747-1778` (LookAroundSaloon method)
- Test: `tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs`

**Key change from original plan:** The event does NOT carry `AdvanceClock`. The `LookAroundSaloon` method calls `EnterActionContext(TownActionContext.Saloon)` AFTER the saloon-exists check but BEFORE the local action resolution. This emits a `TownActionContextEntered` event (if context changed) that appears in the stream before the `SaloonPersonOfInterestSpotted` event. The Apply method calls `RecordCaseUpdate(e.Message)` (no clock advance). The citizen path now advances the turn (because entering the saloon context advances it), which is a deliberate behavior change — looking around the saloon takes time regardless of outcome. If no saloon exists in town, no context event is produced and no turn advance occurs.

- [ ] **Step 1: Write the event production test**

```csharp
[Fact]
public void LookAroundSaloonWithSuspectProducesSpottedEvent()
{
    var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
    var result = session.LookAroundSaloon();

    Assert.True(result.Success);
    Assert.True(result.SessionChanged);
    // Two events: TownActionContextEntered (context change) + SaloonPersonOfInterestSpotted
    Assert.Equal(2, session.UncommittedEvents.Count);
    Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
    var e = Assert.IsType<SaloonPersonOfInterestSpotted>(session.UncommittedEvents[1]);
    Assert.Equal(InvestigationSourceKind.SaloonLookAround, e.SourceKind);
    Assert.NotNull(e.Descriptor);
    Assert.NotNull(e.SuspectId);
    Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, e.PersonOfInterestKind);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "LookAroundSaloonWithSuspectProducesSpottedEvent"`
Expected: FAIL — `SaloonPersonOfInterestSpotted` type not found

- [ ] **Step 3: Create the event type**

```csharp
// src/WildBunch.Domain/Events/SaloonPersonOfInterestSpotted.cs
using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a saloon look-around revealed a person of interest (wanted suspect or citizen),
/// or found nobody of interest on a repeat visit. Carries only public data.
/// See ADR-0028. Clock advancement is handled by EnterActionContext, not this event.
/// </summary>
public sealed record SaloonPersonOfInterestSpotted : IDomainEvent
{
    public required InvestigationSourceKind SourceKind { get; init; }
    public required TownId TownId { get; init; }
    public required string Message { get; init; }
    public SuspectId? SuspectId { get; init; }
    public string? Descriptor { get; init; }
    public SaloonPersonOfInterestKind? PersonOfInterestKind { get; init; }
    /// <summary>
    /// Whether to append a case-update log entry. The repeat path and suspect path
    /// log a message; the citizen path does not (preserving existing behavior).
    /// This does NOT advance the clock — clock advance comes from EnterActionContext.
    /// </summary>
    public bool RecordLog { get; init; } = true;
}
```

Note: `RecordLog` replaces the old `RecordCaseUpdate` + `AdvanceClock` flags. It controls only whether a log entry is appended, not whether the clock advances.

- [ ] **Step 4: Add Apply method and migrate LookAroundSaloon**

Add to `GameSession.cs` near the existing `Apply(InvestigationPerformed)` method:

```csharp
private void Apply(SaloonPersonOfInterestSpotted e)
{
    CurrentTown.CheckSource(e.SourceKind);

    if (e.RecordLog)
    {
        RecordCaseUpdate(e.Message);
    }

    if (e.SuspectId is not null && e.Descriptor is not null)
    {
        CurrentTownVisit.CurrentTownState.SetActiveSaloonPersonOfInterest(e.SuspectId.Value, e.Descriptor);
    }
    else if (e.Descriptor is not null)
    {
        CurrentTownVisit.CurrentTownState.SetActiveSaloonCitizenPersonOfInterest(e.Descriptor);
    }

    _version++;
}
```

Migrate `LookAroundSaloon` (replace lines 1747-1778):

```csharp
public CaseInvestigationResult LookAroundSaloon()
{
    if (IsJourneyModal())
        return CaseInvestigationResult.Failed(JourneyModalBlockMessage);

    var saloonSource = CurrentTown.GetRequiredSourceDefinition(InvestigationSourceKind.SaloonLookAround);
    if (!CurrentTown.IsAvailable(InvestigationSourceKind.SaloonLookAround))
        return CaseInvestigationResult.Failed("There is no saloon here.");

    // Enter saloon context AFTER availability check, BEFORE local action resolution.
    // Emits TownActionContextEntered event if context changed (advances turn).
    // If no saloon exists, we already returned above — no context event, no turn advance.
    EnterActionContext(TownActionContext.Saloon);

    if (CurrentTown.CheckSource(saloonSource) == TownSourceCheckOutcome.RepeatNoNewInfo)
    {
        var repeatMessage = "You look around the saloon again, but nobody of interest is here.";
        var repeatEvent = new SaloonPersonOfInterestSpotted
        {
            SourceKind = InvestigationSourceKind.SaloonLookAround,
            TownId = CurrentTown.Id,
            Message = repeatMessage,
            RecordLog = true
        };
        Apply(repeatEvent);
        _uncommittedEvents.Add(repeatEvent);
        return CaseInvestigationResult.Succeeded(repeatMessage, sessionChanged: true);
    }

    if (TryGetConfrontableSaloonPersonOfInterestCandidateInTown(out var suspect))
    {
        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, CaseFile);
        var spotMessage = $"You look around the saloon and spot {descriptor}.";
        var spotEvent = new SaloonPersonOfInterestSpotted
        {
            SourceKind = InvestigationSourceKind.SaloonLookAround,
            TownId = CurrentTown.Id,
            Message = spotMessage,
            SuspectId = suspect.Id,
            Descriptor = descriptor,
            PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
            RecordLog = true
        };
        Apply(spotEvent);
        _uncommittedEvents.Add(spotEvent);
        return CaseInvestigationResult.Succeeded(spotMessage, sessionChanged: true);
    }

    var citizenDescriptor = DescribeTownCitizen(CurrentTown);
    var citizenMessage = $"You look around the saloon and spot {citizenDescriptor}.";
    var citizenEvent = new SaloonPersonOfInterestSpotted
    {
        SourceKind = InvestigationSourceKind.SaloonLookAround,
        TownId = CurrentTown.Id,
        Message = citizenMessage,
        Descriptor = citizenDescriptor,
        PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
        RecordLog = false  // Citizen path does not log (preserving existing behavior)
    };
    Apply(citizenEvent);
    _uncommittedEvents.Add(citizenEvent);
    return CaseInvestigationResult.Succeeded(citizenMessage, sessionChanged: true);
}
```

**Key changes:**
1. `EnterActionContext(TownActionContext.Saloon)` at the start — advances turn if context changed
2. All paths now produce events (including citizen path)
3. Citizen path has `RecordLog = false` (no log entry, preserving existing behavior)
4. No `advanceClock` anywhere — clock advance comes from context entry
5. `CheckSource` is called in Apply, not in the method body (to avoid double-checking). Actually, `CheckSource` is called in the method body for the repeat-path check. Move it to Apply only and check the outcome in the method before producing the event. **Wait** — `CheckSource` mutates state (marks source as spent). It should be in Apply, not in the method. But the method needs to know the outcome to decide which event to produce. 

**Resolution:** Call `CurrentTown.PeekSourceOutcome(saloonSource)` (non-mutating check) in the method body to decide which event to produce. Call `CurrentTown.CheckSource(e.SourceKind)` (mutating) in Apply. If `PeekSourceOutcome` doesn't exist, add it as a non-mutating peek that returns the same outcome as `CheckSource` without mutating. Check `TownAggregate` for existing peek methods.

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "LookAroundSaloonWithSuspectProducesSpottedEvent"`
Expected: PASS

- [ ] **Step 6: Write additional event production tests**

```csharp
[Fact]
public void LookAroundSaloonCitizenProducesSpottedEventWithNoLog()
{
    var session = TestSessionFactory.CreateWithNoConfrontableSaloonSuspect();
    var result = session.LookAroundSaloon();

    Assert.True(result.Success);
    // Two events: TownActionContextEntered + SaloonPersonOfInterestSpotted
    var spottedEvent = session.UncommittedEvents.OfType<SaloonPersonOfInterestSpotted>().Single();
    Assert.Null(spottedEvent.SuspectId);
    Assert.Equal(SaloonPersonOfInterestKind.Citizen, spottedEvent.PersonOfInterestKind);
    Assert.False(spottedEvent.RecordLog);
}

[Fact]
public void LookAroundSaloonAdvancesTurnViaContextEvent()
{
    var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
    var turnBefore = session.Clock.Turn;
    session.LookAroundSaloon();
    Assert.Equal(turnBefore + 1, session.Clock.Turn);
    Assert.Equal(TownActionContext.Saloon, session.CurrentActionContext);
    // Context event is in the stream, before the spotted event
    var contextEvent = session.UncommittedEvents.OfType<TownActionContextEntered>().Single();
    Assert.Equal(TownActionContext.Saloon, contextEvent.Context);
    Assert.Equal(turnBefore + 1, contextEvent.Turn);
}

[Fact]
public void LookAroundSaloonCitizenPathAdvancesTurn()
{
    // Citizen path now advances turn (entering saloon context) — behavior change
    var session = TestSessionFactory.CreateWithNoConfrontableSaloonSuspect();
    var turnBefore = session.Clock.Turn;
    session.LookAroundSaloon();
    Assert.Equal(turnBefore + 1, session.Clock.Turn);
    // Context event is in the stream even for citizen path
    Assert.Contains(session.UncommittedEvents, e => e is TownActionContextEntered);
}

[Fact]
public void LookAroundSaloon_NoSaloonInTown_DoesNotProduceContextEvent()
{
    var session = TestSessionFactory.CreateWithNoSaloon();
    var turnBefore = session.Clock.Turn;
    var result = session.LookAroundSaloon();

    Assert.False(result.Success);
    Assert.Equal(turnBefore, session.Clock.Turn);
    Assert.Empty(session.UncommittedEvents); // No context event, no spotted event
}
```

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Domain/Events/SaloonPersonOfInterestSpotted.cs src/WildBunch.Domain/Game/GameSession.cs tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs
git commit -m "BUNCH-80: add SaloonPersonOfInterestSpotted event + migrate LookAroundSaloon with context-based turn advance"
```

---

## Task 3: WantedSuspectConfronted Event + ResolveWantedSuspectConfrontation Migration

**Files:**
- Create: `src/WildBunch.Domain/Events/WantedSuspectConfronted.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs:205-327`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add Apply method + ProduceEvent helper)
- Test: `tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs`

**Key clock change:** The confrontation does NOT advance the turn — the player is already in the Saloon context from `LookAroundSaloon`. The `Clock.Turn + 1` pattern at `BountyLoopCoordinator.cs:261,271,279,295` is removed — confrontation state records `Clock.Turn` directly (no `+1` offset).

- [ ] **Step 1: Write the event production test**

```csharp
[Fact]
public void ResolveWantedSuspectConfrontationSurrenderedProducesEvent()
{
    var session = TestSessionFactory.CreateWithWarrantedSuspect();
    // Pre-enter Saloon context (as LookAroundSaloon would do)
    session.EnterActionContextForTesting(TownActionContext.Saloon);
    session.MarkEventsCommittedForTesting();

    var suspect = session.CaseFile.Suspects.First(s => /* has warrant, not confronted */);
    var result = session.ResolveWantedSuspectConfrontation(suspect.Id, WantedSuspectConfrontationChoice.Surrendered);

    Assert.True(result.Success);
    Assert.True(result.SessionChanged);
    // Single event: WantedSuspectConfronted (no context event — already in Saloon)
    Assert.Single(session.UncommittedEvents);
    var e = Assert.IsType<WantedSuspectConfronted>(session.UncommittedEvents.Single());
    Assert.Equal(suspect.Id, e.TargetSuspectId);
    Assert.Equal(WantedSuspectConfrontationOutcome.Surrendered, e.Outcome);
    Assert.True(e.IsAlive);
    Assert.True(e.IsSecured);
}

[Fact]
public void ResolveWantedSuspectConfrontation_DoesNotAdvanceTurn_WhenAlreadyInSaloonContext()
{
    var session = TestSessionFactory.CreateWithWarrantedSuspect();
    session.LookAroundSaloon(); // enters Saloon context, advances turn
    var turnAfterLookAround = session.Clock.Turn;

    var suspect = session.CaseFile.Suspects.First(s => /* has warrant */);
    session.ResolveWantedSuspectConfrontation(suspect.Id, WantedSuspectConfrontationChoice.Surrendered);

    // Turn does NOT advance — already in Saloon context
    Assert.Equal(turnAfterLookAround, session.Clock.Turn);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "ResolveWantedSuspectConfrontation"`
Expected: FAIL — `WantedSuspectConfronted` type not found

- [ ] **Step 3: Create the event type**

```csharp
// src/WildBunch.Domain/Events/WantedSuspectConfronted.cs
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a wanted suspect was confronted with a specific outcome.
/// Carries only public data — TrueCulpritId is never in this event.
/// Clock advancement is handled by EnterActionContext, not this event.
/// See ADR-0028.
/// </summary>
public sealed record WantedSuspectConfronted : IDomainEvent
{
    public required SuspectId TargetSuspectId { get; init; }
    public required string TargetName { get; init; }
    public required WarrantDisposition Disposition { get; init; }
    public required WantedSuspectConfrontationChoice Choice { get; init; }
    public required WantedSuspectConfrontationOutcome Outcome { get; init; }
    public required bool IsAlive { get; init; }
    public required bool IsSecured { get; init; }
    public required string Message { get; init; }
    public string? DeclaredWantedIdentityHandle { get; init; }
}
```

- [ ] **Step 4: Add ProduceEvent helper to GameSession**

```csharp
internal void ProduceEvent<T>(T e) where T : IDomainEvent
{
    Apply(e);
    _uncommittedEvents.Add(e);
}
```

- [ ] **Step 5: Add Apply method**

```csharp
private void Apply(WantedSuspectConfronted e)
{
    RecordCaseUpdate(e.Message);

    if (e.Outcome is not WantedSuspectConfrontationOutcome.Abandoned)
    {
        var confrontationState = new WantedSuspectConfrontationState(
            e.TargetSuspectId,
            e.TargetName,
            e.Disposition,
            e.Outcome,
            e.IsAlive,
            e.IsSecured,
            Clock.Day,
            Clock.Turn);  // No +1 — clock no longer advances from RecordCaseUpdate
        CaseFile.RecordWantedSuspectConfrontationState(confrontationState);
        UpdateWantedSuspectPresence(e.TargetSuspectId, e.Choice);
    }

    _version++;
}
```

- [ ] **Step 6: Migrate ResolveWantedSuspectConfrontation in BountyLoopCoordinator**

Refactor to produce events. Remove direct `RecordCaseUpdate`, `RecordWantedSuspectConfrontationState`, `UpdateWantedSuspectPresence` calls — these move to Apply. Remove the `Clock.Turn + 1` pattern.

For the Abandoned path:
```csharp
var abandonEvent = new WantedSuspectConfronted
{
    TargetSuspectId = targetSuspectId,
    TargetName = warrant.TargetName,
    Disposition = warrant.Terms.Disposition,
    Choice = WantedSuspectConfrontationChoice.Abandoned,
    Outcome = WantedSuspectConfrontationOutcome.Abandoned,
    IsAlive = true,
    IsSecured = false,
    Message = abandonNarration,
    DeclaredWantedIdentityHandle = declaredWantedIdentityHandle
};
_session.ProduceEvent(abandonEvent);
```

For Surrendered/Fled/Killed:
```csharp
var confrontationEvent = new WantedSuspectConfronted
{
    TargetSuspectId = targetSuspectId,
    TargetName = warrant.TargetName,
    Disposition = warrant.Terms.Disposition,
    Choice = choice,
    Outcome = (WantedSuspectConfrontationOutcome)choice,
    IsAlive = isAlive,
    IsSecured = isSecured,
    Message = narration,
    DeclaredWantedIdentityHandle = declaredWantedIdentityHandle
};
_session.ProduceEvent(confrontationEvent);
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "ResolveWantedSuspectConfrontation"`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add src/WildBunch.Domain/Events/WantedSuspectConfronted.cs src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs
git commit -m "BUNCH-80: add WantedSuspectConfronted event + migrate ResolveWantedSuspectConfrontation, remove Clock.Turn+1 pattern"
```

---

## Task 4: SheriffTurnInSettled Event + SettleSheriffTurnIn Migration

**Files:**
- Create: `src/WildBunch.Domain/Events/SheriffTurnInSettled.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs:413-438`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add Apply method)
- Test: `tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs`

**Key clock change:** `SettleSheriffTurnIn` calls `EnterActionContext(TownActionContext.SheriffOffice)` at the start, which emits a `TownActionContextEntered` event if the context changed. If the player is coming from the Saloon context (after a confrontation), the turn advances and the context event is in the stream. If already at the Sheriff's office, no event and no turn advance. **Rejected turn-ins still produce the context event** — the player went to the sheriff's office, time passed, but the turn-in was rejected. The `TownActionContextEntered` event is in the stream even when no `SheriffTurnInSettled` event follows.

- [ ] **Step 1: Write the event production test**

```csharp
[Fact]
public void SettleSheriffTurnInProducesSettledEvent()
{
    var session = TestSessionFactory.CreateWithSecuredSuspect();
    // Pre-enter Saloon context (as the confrontation flow would do)
    session.EnterActionContextForTesting(TownActionContext.Saloon);
    session.MarkEventsCommittedForTesting();

    var suspect = session.CaseFile.Suspects.First(s => /* secured alive */);
    var result = session.SettleSheriffTurnIn(suspect.Id, isAlive: true);

    Assert.True(result.Success);
    Assert.True(result.SessionChanged);
    // Two events: TownActionContextEntered(SheriffOffice) + SheriffTurnInSettled
    Assert.Equal(2, session.UncommittedEvents.Count);
    Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
    var e = Assert.IsType<SheriffTurnInSettled>(session.UncommittedEvents[1]);
    Assert.Equal(suspect.Id, e.TargetSuspectId);
    Assert.True(e.BountyAmount > 0);
}

[Fact]
public void SettleSheriffTurnIn_AdvancesTurn_WhenEnteringSheriffContextFromSaloon()
{
    var session = TestSessionFactory.CreateWithSecuredSuspect();
    session.LookAroundSaloon(); // enters Saloon (context event + spotted event)
    session.ResolveWantedSuspectConfrontation(suspectId, WantedSuspectConfrontationChoice.Surrendered);
    session.MarkEventsCommittedForTesting();
    var turnAfterConfrontation = session.Clock.Turn;

    session.SettleSheriffTurnIn(suspectId, isAlive: true);

    // Turn advances — context change from Saloon to SheriffOffice
    Assert.Equal(turnAfterConfrontation + 1, session.Clock.Turn);
    Assert.Equal(TownActionContext.SheriffOffice, session.CurrentActionContext);
    // Context event is in the stream (the only new uncommitted event pair)
    var contextEvent = session.UncommittedEvents.OfType<TownActionContextEntered>()
        .Single(); // Only the SheriffOffice context event is uncommitted
    Assert.Equal(TownActionContext.SheriffOffice, contextEvent.Context);
}

[Fact]
public void SettleSheriffTurnIn_Rejected_StillProducesContextEvent()
{
    var session = TestSessionFactory.CreateWithNoSecuredSuspect();
    session.LookAroundSaloon(); // enters Saloon
    session.MarkEventsCommittedForTesting();
    var turnAfterSaloon = session.Clock.Turn;

    var result = session.SettleSheriffTurnIn(nonExistentSuspectId, isAlive: true);

    // Turn-in is rejected, but the player still went to the sheriff's office
    Assert.False(result.Success);
    Assert.Equal(turnAfterSaloon + 1, session.Clock.Turn);
    Assert.Equal(TownActionContext.SheriffOffice, session.CurrentActionContext);
    // Context event is in the stream even though no settlement event follows
    Assert.Contains(session.UncommittedEvents, e => e is TownActionContextEntered);
    Assert.DoesNotContain(session.UncommittedEvents, e => e is SheriffTurnInSettled);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "SettleSheriffTurnIn"`
Expected: FAIL — `SheriffTurnInSettled` type not found

- [ ] **Step 3: Create the event type**

```csharp
// src/WildBunch.Domain/Events/SheriffTurnInSettled.cs
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a wanted suspect was turned in to the sheriff for a bounty.
/// Carries only public data. See ADR-0028.
/// </summary>
public sealed record SheriffTurnInSettled : IDomainEvent
{
    public required SuspectId TargetSuspectId { get; init; }
    public required string TargetName { get; init; }
    public required WarrantDisposition Disposition { get; init; }
    public required bool IsAlive { get; init; }
    public required decimal BountyAmount { get; init; }
    public required string Message { get; init; }
    public required int Day { get; init; }
    public required int Turn { get; init; }
}
```

- [ ] **Step 4: Add Apply method**

```csharp
private void Apply(SheriffTurnInSettled e)
{
    Player.AdjustCash(e.BountyAmount);

    var settlementState = new SheriffTurnInSettlementState(
        e.TargetSuspectId, e.TargetName, e.Disposition,
        e.IsAlive, e.BountyAmount, e.Day, e.Turn);
    CaseFile.RecordSheriffTurnInSettlementState(settlementState);

    _version++;
}
```

- [ ] **Step 5: Migrate SettleSheriffTurnIn in BountyLoopCoordinator**

```csharp
public SheriffTurnInResult SettleSheriffTurnIn(SuspectId targetSuspectId, bool isAlive)
{
    _session.EnterActionContext(TownActionContext.SheriffOffice);

    var assessment = AssessSheriffTurnIn(targetSuspectId, isAlive);
    if (!assessment.Success)
        return assessment;

    if (!BountySettlementPolicy.TryCreateSheriffTurnInSettlementState(
            _session.CaseFile, assessment, targetSuspectId, isAlive,
            _session.Clock.Day, _session.Clock.Turn,
            out var settlementState, out var rejectionResult))
    {
        return rejectionResult;
    }

    var settledEvent = new SheriffTurnInSettled
    {
        TargetSuspectId = targetSuspectId,
        TargetName = assessment.TargetName!,
        Disposition = assessment.Disposition!.Value,
        IsAlive = isAlive,
        BountyAmount = settlementState.BountyAmount,
        Message = assessment.Message!,
        Day = settlementState.Day,
        Turn = settlementState.Turn
    };
    _session.ProduceEvent(settledEvent);

    return assessment with { SessionChanged = true };
}
```

Note: `EnterActionContext` is called BEFORE the assessment. This means even a failed turn-in attempt enters the SheriffOffice context and advances the turn, producing a `TownActionContextEntered` event in the stream. This is intentional — going to the sheriff's office takes time even if the turn-in is rejected. The context event is replayable, so replay reconstructs the correct clock/context state regardless of whether the turn-in succeeded.

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "SettleSheriffTurnIn"`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Domain/Events/SheriffTurnInSettled.cs src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs
git commit -m "BUNCH-80: add SheriffTurnInSettled event + migrate SettleSheriffTurnIn with SheriffOffice context entry"
```

---

## Task 5: SaloonPersonOfInterestConfronted Event + ConfrontSaloonPersonOfInterest Migration

**Files:**
- Create: `src/WildBunch.Domain/Events/SaloonPersonOfInterestConfronted.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs:17-203`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add Apply method)
- Test: `tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs`

**Key clock change:** All `SaloonPersonOfInterestConfronted` events do NOT advance the turn — the player is already in the Saloon context from `LookAroundSaloon`. The Apply method only clears the saloon person and optionally fines the player. No `RecordCaseUpdate` call (no log entry from this event — log entries come from delegated `WantedSuspectConfronted` events).

- [ ] **Step 1: Write event production tests for each path**

```csharp
[Fact]
public void ConfrontCitizenWithWrongDeclarationProducesConfrontedEvent()
{
    var session = TestSessionFactory.CreateWithActiveCitizenSaloonPerson();
    var result = session.ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle: "wrong-handle");

    Assert.True(result.Success);
    var confrontedEvent = session.UncommittedEvents.OfType<SaloonPersonOfInterestConfronted>().Single();
    Assert.True(confrontedEvent.IsCitizen);
    Assert.True(confrontedEvent.FineAmount > 0);
}

[Fact]
public void ConfrontArmedCorrectDeclarationProducesConfrontedAndSettledEvents()
{
    var session = TestSessionFactory.CreateWithArmedCorrectDeclarationSetup();
    var result = session.ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle: "correct-handle");

    Assert.True(result.Success);
    Assert.Contains(session.UncommittedEvents, e => e is WantedSuspectConfronted);
    Assert.Contains(session.UncommittedEvents, e => e is SheriffTurnInSettled);
    Assert.Contains(session.UncommittedEvents, e => e is SaloonPersonOfInterestConfronted);
}

[Fact]
public void ConfrontSaloonPerson_DoesNotAdvanceTurn_WhenAlreadyInSaloonContext()
{
    var session = TestSessionFactory.CreateWithActiveCitizenSaloonPerson();
    session.LookAroundSaloon(); // enters Saloon, advances turn
    var turnAfterLookAround = session.Clock.Turn;

    session.ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle: "wrong-handle");

    Assert.Equal(turnAfterLookAround, session.Clock.Turn);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "Confront"`
Expected: FAIL — `SaloonPersonOfInterestConfronted` type not found

- [ ] **Step 3: Create the event type**

```csharp
// src/WildBunch.Domain/Events/SaloonPersonOfInterestConfronted.cs
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a saloon person of interest was confronted. Covers citizen/wrong-declaration
/// paths and rejection paths that clear the active saloon person. When the confrontation
/// delegates to ResolveWantedSuspectConfrontation or SettleSheriffTurnIn, those produce
/// their own events; this event covers only the saloon-person-level outcome.
/// Carries only public data. See ADR-0028.
/// </summary>
public sealed record SaloonPersonOfInterestConfronted : IDomainEvent
{
    public required string Message { get; init; }
    public SuspectId? TargetSuspectId { get; init; }
    public required string TargetName { get; init; }
    public required SaloonPersonOfInterestKind PersonOfInterestKind { get; init; }
    public required SaloonPersonOfInterestConfrontationOutcome Outcome { get; init; }
    public bool? IsAlive { get; init; }
    public bool? IsSecured { get; init; }
    public decimal? FineAmount { get; init; }
    public decimal? WalletBefore { get; init; }
    public decimal? WalletAfter { get; init; }
    public string? DeclaredWantedIdentityHandle { get; init; }
    public bool IsCitizen { get; init; }
}
```

- [ ] **Step 4: Add Apply method**

```csharp
private void Apply(SaloonPersonOfInterestConfronted e)
{
    CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();

    if (e.FineAmount is { } fine && fine > 0m)
    {
        Player.AdjustCash(-fine);
    }

    _version++;
}
```

- [ ] **Step 5: Migrate ConfrontSaloonPersonOfInterest in BountyLoopCoordinator**

For each path:
1. **Rejection clearing saloon person:** produce `SaloonPersonOfInterestConfronted` with `Outcome = Rejected`
2. **Armed + correct:** delegate to `ResolveWantedSuspectConfrontation` + `SettleSheriffTurnIn` (produce their own events), then produce `SaloonPersonOfInterestConfronted` with `Outcome = Surrendered` to clear saloon person
3. **Armed + wrong declaration:** produce `SaloonPersonOfInterestConfronted` with `Outcome = WrongWantedDeclaration`, `FineAmount`, `WalletBefore`, `WalletAfter`, `IsCitizen = false`
4. **No firearm:** delegate to `ResolveWantedSuspectConfrontation(Fled)`, then produce `SaloonPersonOfInterestConfronted` with `Outcome = Fled`
5. **Citizen wrong declaration:** produce `SaloonPersonOfInterestConfronted` with `Outcome = WrongWantedDeclaration`, `FineAmount`, `WalletBefore`, `WalletAfter`, `IsCitizen = true`
6. **No person of interest:** return rejection WITHOUT producing event

**No `EnterActionContext` call** — the player is already in the Saloon context from `LookAroundSaloon`.

- [ ] **Step 6: Migrate ConfrontSaloonWantedSuspect**

The thin wrapper delegates. The no-warrant rejection path produces `SaloonPersonOfInterestConfronted` with `Outcome = Rejected` to clear the saloon person.

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "Confront"`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add src/WildBunch.Domain/Events/SaloonPersonOfInterestConfronted.cs src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSession.BountyLoopCoordinator.cs tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs
git commit -m "BUNCH-80: add SaloonPersonOfInterestConfronted event + migrate ConfrontSaloonPersonOfInterest"
```

---

## Task 6: Investigation Method Context Entry

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (4 investigation methods: `FollowTelegraphLeads`, `GatherLocalGossip`, `InspectNoticeBoard`, `CheckSheriffRecords`)
- Test: `tests/WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs`

**Goal:** Update the 4 investigation methods to call `EnterActionContext` with the correct context mapping AFTER source-availability checks. This restores clock advancement for investigation actions after `RecordCaseUpdate` was decoupled in Task 1. The context entry emits a `TownActionContextEntered` event that is replayable.

**Context mapping:**
| InvestigationSourceKind | TownActionContext |
|------------------------|-------------------|
| `SheriffWarrants` | `SheriffOffice` |
| `SheriffRecords` | `SheriffOffice` |
| `TelegraphLead` | `TelegraphOffice` |
| `LocalGossip` | `Saloon` |
| `NoticeBoard` | `TownSquare` |

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public void CheckSheriffRecords_EntersSheriffOfficeContext_AndAdvancesTurn()
{
    var session = TestSessionFactory.CreateDefault();
    var turnBefore = session.Clock.Turn;
    session.CheckSheriffRecords();
    Assert.Equal(TownActionContext.SheriffOffice, session.CurrentActionContext);
    Assert.Equal(turnBefore + 1, session.Clock.Turn);
}

[Fact]
public void FollowTelegraphLeads_EntersTelegraphOfficeContext()
{
    var session = TestSessionFactory.CreateDefault();
    session.FollowTelegraphLeads();
    Assert.Equal(TownActionContext.TelegraphOffice, session.CurrentActionContext);
}

[Fact]
public void GatherLocalGossip_EntersSaloonContext()
{
    var session = TestSessionFactory.CreateDefault();
    session.GatherLocalGossip();
    Assert.Equal(TownActionContext.Saloon, session.CurrentActionContext);
}

[Fact]
public void InspectNoticeBoard_EntersTownSquareContext()
{
    var session = TestSessionFactory.CreateDefault();
    session.InspectNoticeBoard();
    Assert.Equal(TownActionContext.TownSquare, session.CurrentActionContext);
}

[Fact]
public void TwoSheriffActionsInSameContext_DoNotAdvanceTurnTwice()
{
    var session = TestSessionFactory.CreateDefault();
    session.CheckSheriffRecords(); // enters SheriffOffice, advances turn
    var turnAfterFirst = session.Clock.Turn;

    // ReadWantedPosters is also a SheriffOffice action — same context, no turn advance
    session.ReadWantedPosters();
    Assert.Equal(turnAfterFirst, session.Clock.Turn);
    Assert.Equal(TownActionContext.SheriffOffice, session.CurrentActionContext);
}
```

Note: `ReadWantedPosters` also needs to enter the SheriffOffice context. Check if it currently advances the clock. If it does (via `RecordCaseUpdate`), it needs the same context entry treatment.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "EntersSheriffOffice|EntersTelegraphOffice|EntersSaloonContext|EntersTownSquare|DoNotAdvanceTurnTwice"`
Expected: FAIL — investigation methods don't call `EnterActionContext` yet

- [ ] **Step 3: Add EnterActionContext calls to investigation methods**

For each of the 4 investigation methods, add `EnterActionContext(mappedContext)` AFTER the journey modal check and source-availability check, but BEFORE the local action resolution. This ensures that attempting an action in a non-existent location does not advance time:

```csharp
// Example: CheckSheriffRecords
public CaseInvestigationResult CheckSheriffRecords()
{
    if (IsJourneyModal())
        return CaseInvestigationResult.Failed(JourneyModalBlockMessage);

    // Check source availability FIRST — no context entry if source doesn't exist
    if (!CurrentTown.IsAvailable(InvestigationSourceKind.SheriffRecords))
        return CaseInvestigationResult.Failed("Sheriff's records are not available here.");

    // Enter context AFTER availability check — emits TownActionContextEntered event
    EnterActionContext(TownActionContext.SheriffOffice);

    // ... rest of method unchanged (produces InvestigationPerformed event, Apply logs without clock advance)
}
```

Also add `EnterActionContext(TownActionContext.SheriffOffice)` to `ReadWantedPosters` if it exists as a separate method, after its availability check.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "ClockTurnCorrection"`
Expected: PASS

- [ ] **Step 5: Run existing investigation tests and update turn assertions**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "Investigation"`
Expected: Some tests may fail because turn advancement behavior changed. Update assertions:
- Tests that expected `Clock.Turn` to advance by 1 per investigation action should still pass (context entry advances the turn)
- Tests that expected `Clock.Turn` to NOT advance for repeat-source checks may need updating (entering the context still advances the turn even if the source is spent)
- Tests that do multiple investigation actions from different sources may need turn-count updates (same-context actions don't advance, different-context actions do)

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSession.cs tests/WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs
git commit -m "BUNCH-80: add EnterActionContext calls to investigation methods with context mapping"
```

---

## Task 7: Replay Dispatcher Update

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs:87-103` (ApplyEvent switch)

- [ ] **Step 1: Write a test that verifies replay dispatches new events**

```csharp
[Fact]
public void RehydrateFromEventsHandlesAllBountySaloonEventTypes()
{
    var session = TestSessionFactory.CreateWithFullBountySaloonSetup();
    session.LookAroundSaloon();
    session.ResolveWantedSuspectConfrontation(suspectId, WantedSuspectConfrontationChoice.Surrendered);
    session.SettleSheriffTurnIn(suspectId, isAlive: true);

    var events = /* all events */;
    var replayed = GameSession.RehydrateFromEvents(session.Id, session.World, session.CaseFile, events);

    // Verify no exception thrown and state matches
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "RehydrateFromEventsHandlesAllBountySaloonEventTypes"`
Expected: FAIL — `Unknown domain event type: SaloonPersonOfInterestSpotted`

- [ ] **Step 3: Add 5 new cases to ApplyEvent switch**

```csharp
case TownActionContextEntered tc:
    session.Apply(tc);
    break;
case SaloonPersonOfInterestSpotted s:
    session.Apply(s);
    break;
case WantedSuspectConfronted w:
    session.Apply(w);
    break;
case SheriffTurnInSettled st:
    session.Apply(st);
    break;
case SaloonPersonOfInterestConfronted c:
    session.Apply(c);
    break;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "RehydrateFromEventsHandlesAllBountySaloonEventTypes"`
Expected: PASS

- [ ] **Step 5: Run all bounty/saloon domain tests to check for regressions**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "Bounty|Saloon|Confront|Sheriff|Wanted"`
Expected: All PASS (update tests that asserted on old clock behavior)

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSessionEventReplay.cs tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs
git commit -m "BUNCH-80: add bounty/saloon events to replay dispatcher"
```

---

## Notes for Implementation

### ProduceEvent helper

`internal void ProduceEvent<T>(T e) where T : IDomainEvent` on `GameSession` — calls `Apply(e)` then `_uncommittedEvents.Add(e)`. Used by `EnterActionContext` and `BountyLoopCoordinator` methods that can't access `_uncommittedEvents` directly.

### Event-sourced clock/context

`EnterActionContext` does NOT call `Clock.Advance()` directly. It computes the resulting day/turn, creates a `TownActionContextEntered` event with those values, and calls `ProduceEvent`. `Apply(TownActionContextEntered)` sets both `CurrentActionContext` and `Clock` (via `Clock.Set`) from the event. This ensures command execution and replay produce identical clock/context state — no divergence.

`CurrentActionContext` is persisted in the session snapshot (see Phase 3 Task 9) and reconstructed from event replay. It does NOT reset to `None` after load.

### PeekSourceOutcome

If `TownAggregate` doesn't have a non-mutating peek for source check outcome, add one:
```csharp
public TownSourceCheckOutcome PeekSourceOutcome(InvestigationSourceKind kind) { /* non-mutating */ }
```
This lets `LookAroundSaloon` decide which event to produce without mutating state. The mutating `CheckSource` call moves to `Apply(SaloonPersonOfInterestSpotted)`.

### TestSessionFactory

Add factory methods for bounty/saloon test setups (see Phase 1 original plan notes).

### Clock.Turn + 1 removal

The `BountyLoopCoordinator.cs:261,271,279,295` pattern of `Clock.Turn + 1` was needed because `RecordCaseUpdate` advanced the clock BEFORE the confrontation state was recorded. After decoupling, the clock no longer advances from `RecordCaseUpdate`, so the state records `Clock.Turn` directly (no `+1`).

### Existing test updates

100+ test sites assert on `Clock.Turn` values. After the clock/turn correction:
- Single investigation actions still advance turn by 1 (via `EnterActionContext`) — most tests pass
- Multiple same-context actions (e.g., `CheckSheriffRecords` + `ReadWantedPosters`) now advance turn by 1, not 2 — tests need updating
- Bounty/saloon confrontation after `LookAroundSaloon` does NOT advance turn (same Saloon context) — tests that expected turn advance from confrontation need updating
- `SettleSheriffTurnIn` after confrontation advances turn (context change) — tests that expected no turn advance need updating

### SheriffTurnInSettlementState constructor

Verify exact constructor signature before writing the Apply method.

### EnterActionContext on rejection paths

For `SettleSheriffTurnIn`, `EnterActionContext(SheriffOffice)` is called before the assessment. This means even rejected turn-ins advance the turn. This is intentional — traveling to the sheriff's office takes time. If this is NOT desired, move `EnterActionContext` after the assessment success check.
