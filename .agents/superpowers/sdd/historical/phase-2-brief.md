# Phase 2: GameSession Changes (Emission + Apply)

## Task Description

Update GameSession to emit `CaseFileGenerated` during setup and apply it during replay. This phase will break existing tests that call `StartSetup` or `RehydrateFromEvents`.

## Files

- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`

## Implementation

### Task 1: Emit CaseFileGenerated in StartSetup

In `GameSession.StartSetup`, after emitting `WorldGenerated`, emit `CaseFileGenerated`:

```csharp
var caseFileEvent = new CaseFileGenerated
{
    CaseFile = CaseFileSnapshot.FromDomain(caseFile)
};

session.Apply(caseFileEvent);
session._uncommittedEvents.Add(caseFileEvent);
```

Also make `CaseFile` property mutable (currently readonly):
- Change `public CaseFile CaseFile { get; }` to `public CaseFile CaseFile { get; private set; } = null!;`

### Task 2: Add Apply(CaseFileGenerated) Method

Add to `GameSession.cs`:

```csharp
private void Apply(CaseFileGenerated e)
{
    CaseFile = e.CaseFile.ToDomain();
    _version++;
}
```

### Task 3: Update RehydrateFromEvents to Remove CaseFile Parameter

In `GameSessionEventReplay.cs`, change signature from:
```csharp
public static GameSession RehydrateFromEvents(
    GameSessionId id,
    DomainWorld world,
    CaseFile caseFile,
    IReadOnlyList<IDomainEvent> events)
```

To:
```csharp
public static GameSession RehydrateFromEvents(
    GameSessionId id,
    DomainWorld world,
    IReadOnlyList<IDomainEvent> events)
```

Update placeholder construction to use empty caseFile:
```csharp
var caseFile = new CaseFile(
    null,
    Array.Empty<Suspect>(),
    new SuspectId("placeholder"),
    CaseOpeningLead.Create("placeholder"),
    Array.Empty<Clue>());
```

### Task 4: Add CaseFileGenerated to Event Replay Dispatch

In `GameSessionEventReplay.cs`, add to `ApplyEvent` switch:

```csharp
case CaseFileGenerated cfg:
    session.Apply(cfg);
    break;
```

## Verification

- Build will fail (expected - callers need updates in Phase 3)
- No existing tests broken yet (they break in Phase 4)

## Context

This is Phase 2 of Plan 1c. The changes follow the same pattern as WorldGenerated from Plan 1b.
