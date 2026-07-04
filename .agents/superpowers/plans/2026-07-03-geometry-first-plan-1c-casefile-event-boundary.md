# Geometry-First Map Generation - Plan 1c: CaseFile Event Boundary

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the generated caseFile event-derived instead of snapshot-only. Add `CaseFileGenerated` domain event to the event stream so the caseFile can be reconstructed by replaying events, not just from the JSON snapshot cache. This completes the event-sourcing contract by ensuring all domain state is in the event stream.

**Architecture:** The current flow passes the `CaseFile` as a constructor argument to `GameSession` and stores it only in the JSON snapshot — it's not in the event stream, which violates the event-sourcing contract (ADR-0028). This plan adds a `CaseFileGenerated` event (carrying suspects, true culprit, opening lead, clues) emitted during setup, after `WorldGenerated`. `Apply(CaseFileGenerated)` sets `CaseFile` from the event. The snapshot continues to cache the caseFile, but the event is now the source of truth — replay without a snapshot produces the same caseFile.

**Incremental Approach:** This plan is broken into phases to reduce risk. Each phase can be independently tested and committed. If a phase hits blockers, we can pause and reassess without losing progress.

**Greenfield Context:** This is a greenfield project with no backward compatibility requirements. Database can be dropped/rebuilt as needed. No migration for existing sessions is required.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3, existing event-sourced GameSession aggregate

## Prerequisites

- Plan 0 (Clean Slate) must be complete.
- Plan 1a (Core Pipeline) must be complete.
- Plan 1b (Event Boundary) must be complete — `WorldGenerated` and `StartingTownSelected` events must exist.

## Current event flow (what we're changing)

```
CompletePlayerSetupHandler
  → _newGameFactory.ResolveWorld(...) → (world, caseFile, seedCodeText, saltSource)
  → GameSession.StartSetup(playerName, world, caseFile, difficulty, entropy, seedCode, saltSource)
    → constructor sets CaseFile = caseFile directly (NOT from event)
    → emits PlayerSetupCompleted { PlayerName, GameDifficulty, GameEntropy, SeedCode }
    → emits WorldGenerated { SeedCode, SaltSource, GameEntropy, World }
    → Apply(PlayerSetupCompleted) sets SeedCode, Difficulty, Entropy, Player
    → Apply(WorldGenerated) sets World from Towns/Trails
    → CaseFile is NOT set from any event — it's a constructor argument

RehydrateFromEvents(sessionId, world, caseFile, events)
  → caseFile is passed as external reference (NOT from events)
  → placeholder session constructed with caseFile from parameter
```

## Target event flow (after this plan)

```
CompletePlayerSetupHandler
  → _newGameFactory.ResolveWorld(...) → (world, caseFile, seedCodeText, saltSource)
  → GameSession.StartSetup(playerName, world, caseFile, difficulty, entropy, seedCode, saltSource)
    → emits PlayerSetupCompleted { PlayerName, GameDifficulty, GameEntropy, SeedCode }
    → emits WorldGenerated { SeedCode, SaltSource, GameEntropy, World }
    → emits CaseFileGenerated { Suspects, TrueCulpritId, OpeningLead, Clues } ← NEW
    → Apply(PlayerSetupCompleted) sets SeedCode, Difficulty, Entropy, Player
    → Apply(WorldGenerated) sets World from Towns/Trails
    → Apply(CaseFileGenerated) sets CaseFile from snapshot ← NEW

RehydrateFromEvents(sessionId, world, events) ← caseFile parameter removed
  → placeholder session constructed with empty caseFile
  → events replayed through Apply
  → Apply(CaseFileGenerated) sets CaseFile from event
```

## Files

**New files:**
- `src/WildBunch.Domain/Events/CaseFileGenerated.cs` — domain event carrying the generated caseFile
- `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs` — public snapshot record for caseFile (the event carries this)
- `tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs` — tests for event round-trip

**Modified files:**
- `src/WildBunch.Domain/Game/GameSession.cs` — emit `CaseFileGenerated` in `StartSetup`, add `Apply(CaseFileGenerated)`, make `CaseFile` property mutable
- `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` — add `CaseFileGenerated` to `ApplyEvent` dispatch, remove `caseFile` parameter from `RehydrateFromEvents`
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs` — add `CaseFileGenerated` to `ResolveEventType`
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs` — update to use public `CaseFileSnapshot` if needed
- `tests/WildBunch.Domain.Tests/Events/StartFlowEventSourcingTests.cs` — update event expectations, remove caseFile from RehydrateFromEvents calls
- `tests/WildBunch.Domain.Tests/TestSessionFactory.cs` — update to remove caseFile from RehydrateFromEvents calls

---

## Phase 1: Foundation (Event + Snapshot Type)

**Goal:** Create the event type and snapshot record needed for event-sourcing the caseFile. This phase is low-risk and doesn't change any existing behavior.

### Task 1: Create Public CaseFile Snapshot Type

**Files:**
- Create: `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs`

**Interfaces:**
- Produces: `CaseFileSnapshot` as a public record in `WildBunch.Domain.Cases`. Used by Task 2 (CaseFileGenerated event).

The snapshot record should carry all caseFile state needed for reconstruction:
- Suspects (list of suspect snapshots)
- TrueCulpritId
- OpeningLead
- Clues (list of clue snapshots)

- [ ] **Step 1: Create public snapshot record in the domain**

Create `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs`:

```csharp
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Domain.Cases;

/// <summary>
/// Immutable snapshot of a generated caseFile for event storage and replay.
/// Carried by the CaseFileGenerated domain event.
/// </summary>
public sealed record CaseFileSnapshot(
    IReadOnlyList<SuspectSnapshot> Suspects,
    string TrueCulpritId,
    CaseOpeningLead OpeningLead,
    IReadOnlyList<ClueSnapshot> Clues)
{
    public static CaseFileSnapshot FromDomain(CaseFile caseFile)
        => new(
            caseFile.Suspects.Select(SuspectSnapshot.FromDomain).ToArray(),
            caseFile.TrueCulpritId.Value,
            caseFile.OpeningLead,
            caseFile.KnownClues.Select(ClueSnapshot.FromDomain).ToArray());

    public CaseFile ToDomain()
        => new(
            null,
            Suspects.Select(SuspectSnapshot.ToDomain).ToArray(),
            new SuspectId(TrueCulpritId),
            OpeningLead,
            Clues.Select(ClueSnapshot.ToDomain).ToArray());
}

public sealed record SuspectSnapshot(
    string Id,
    string Name,
    SuspectProfileSnapshot Profile,
    string TraitsTags,
    string Status)
{
    public static SuspectSnapshot FromDomain(Suspect suspect)
        => new(
            suspect.Id.Value,
            suspect.Name,
            SuspectProfileSnapshot.FromDomain(suspect.Profile),
            suspect.Traits.Tags,
            suspect.Status.ToString());

    public Suspect ToDomain()
        => new(
            new SuspectId(Id),
            Name,
            Profile.ToDomain(),
            SuspectTraits.FromTags(TraitsTags),
            Enum.Parse<SuspectStatus>(Status));
}

public sealed record SuspectProfileSnapshot(
    IReadOnlyList<SuspectAliasSnapshot> Aliases,
    IReadOnlyList<SuspectIdentityFactSnapshot> IdentifyingFacts)
{
    public static SuspectProfileSnapshot FromDomain(SuspectProfile profile)
        => new(
            profile.Aliases.Select(SuspectAliasSnapshot.FromDomain).ToArray(),
            profile.IdentifyingFacts.Select(SuspectIdentityFactSnapshot.FromDomain).ToArray());

    public SuspectProfile ToDomain()
        => new(
            Aliases.Select(SuspectAliasSnapshot.ToDomain).ToArray(),
            IdentifyingFacts.Select(SuspectIdentityFactSnapshot.ToDomain).ToArray());
}

public sealed record SuspectAliasSnapshot(string Alias, string AliasKind)
{
    public static SuspectAliasSnapshot FromDomain(SuspectAlias alias)
        => new(alias.Alias, alias.AliasKind.ToString());

    public SuspectAlias ToDomain()
        => new(Alias, Enum.Parse<AliasKind>(AliasKind));
}

public sealed record SuspectIdentityFactSnapshot(string Raw, string ThirdPerson, string FirstPerson)
{
    public static SuspectIdentityFactSnapshot FromDomain(SuspectIdentityFact fact)
        => new(fact.Raw.ToString(), fact.ThirdPerson, fact.FirstPerson);

    public SuspectIdentityFact ToDomain()
        => new(FeatureLanguage.Raw(Raw), ThirdPerson, FirstPerson);
}

public sealed record ClueSnapshot(
    string Id,
    string Kind,
    string Description,
    string[] LinkedSuspectIds,
    string TargetKind,
    string? SourceKind,
    string? Source,
    string? Context,
    ClueAnchorsSnapshot Anchors)
{
    public static ClueSnapshot FromDomain(Clue clue)
        => new(
            clue.Id.Value,
            clue.Kind.ToString(),
            clue.Description,
            clue.LinkedSuspectIds.Select(id => id.Value).ToArray(),
            clue.TargetKind.ToString(),
            clue.SourceKind?.ToString(),
            clue.Source,
            clue.Context,
            ClueAnchorsSnapshot.FromDomain(clue.Anchors));

    public Clue ToDomain()
        => new(
            new ClueId(Id),
            Enum.Parse<ClueKind>(Kind),
            Description,
            LinkedSuspectIds.Select(id => new SuspectId(id)),
            Enum.Parse<InvestigationTargetKind>(TargetKind),
            SourceKind is null ? null : Enum.Parse<InvestigationSourceKind>(SourceKind),
            Source,
            Context,
            Anchors.ToDomain());
}

public sealed record ClueAnchorsSnapshot(
    ClueSubjectAnchorSnapshot[] Subjects,
    ClueLocationAnchorSnapshot[] Locations,
    ClueTimeAnchorSnapshot[] Times,
    ClueDirectionAnchorSnapshot[] Directions)
{
    public static ClueAnchorsSnapshot FromDomain(ClueAnchors anchors)
        => new(
            anchors.Subjects.Select(ClueSubjectAnchorSnapshot.FromDomain).ToArray(),
            anchors.Locations.Select(ClueLocationAnchorSnapshot.FromDomain).ToArray(),
            anchors.Times.Select(ClueTimeAnchorSnapshot.FromDomain).ToArray(),
            anchors.Directions.Select(ClueDirectionAnchorSnapshot.FromDomain).ToArray());

    public ClueAnchors ToDomain()
        => new(
            Subjects.Select(s => s.ToDomain()),
            Locations.Select(l => l.ToDomain()),
            Times.Select(t => t.ToDomain()),
            Directions.Select(d => d.ToDomain()));
}

public sealed record ClueSubjectAnchorSnapshot(
    string Label,
    string? SuspectId,
    string? Alias,
    string? Feature,
    string? Fact)
{
    public static ClueSubjectAnchorSnapshot FromDomain(ClueSubjectAnchor anchor)
        => new(anchor.Label, anchor.SuspectId?.Value, anchor.Alias, anchor.Feature, anchor.Fact);

    public ClueSubjectAnchor ToDomain()
        => new(Label, SuspectId is null ? null : new SuspectId(SuspectId), Alias, Feature, Fact);
}

public sealed record ClueLocationAnchorSnapshot(string Label, string? TownId, string? Place, string? Route)
{
    public static ClueLocationAnchorSnapshot FromDomain(ClueLocationAnchor anchor)
        => new(anchor.Label, anchor.TownId?.Value, anchor.Place, anchor.Route);

    public ClueLocationAnchor ToDomain()
        => new(Label, TownId is null ? null : new TownId(TownId), Place, Route);
}

public sealed record ClueTimeAnchorSnapshot(string Recency, int? Day, int? Turn)
{
    public static ClueTimeAnchorSnapshot FromDomain(ClueTimeAnchor anchor)
        => new(anchor.Recency.ToString(), anchor.Day, anchor.Turn);

    public ClueTimeAnchor ToDomain()
        => new(Enum.Parse<ClueRecency>(Recency), Day, Turn);
}

public sealed record ClueDirectionAnchorSnapshot(string Label, string? Movement, string? DestinationTownId, string? Route)
{
    public static ClueDirectionAnchorSnapshot FromDomain(ClueDirectionAnchor anchor)
        => new(anchor.Label, anchor.Movement, anchor.DestinationTownId?.Value, anchor.Route);

    public ClueDirectionAnchor ToDomain()
        => new(Label, Movement, DestinationTownId is null ? null : new TownId(DestinationTownId), Route);
}
```

**Verification:**
- Build passes
- No existing tests broken

---

### Task 2: Create CaseFileGenerated Domain Event

**Files:**
- Create: `src/WildBunch.Domain/Events/CaseFileGenerated.cs`

**Interfaces:**
- Produces: `CaseFileGenerated` domain event carrying the caseFile snapshot.

- [ ] **Step 1: Create the domain event**

Create `src/WildBunch.Domain/Events/CaseFileGenerated.cs`:

```csharp
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the caseFile was generated from the seed code, salt source, and entropy.
/// Carries the full caseFile snapshot (suspects, true culprit, opening lead, clues)
/// so the caseFile can be reconstructed by replaying this event without re-running
/// the generation pipeline.
/// This is the event-sourced source of truth for the caseFile — the JSON snapshot
/// is a cache of this event's payload.
/// </summary>
public sealed record CaseFileGenerated : IDomainEvent
{
    public required CaseFileSnapshot CaseFile { get; init; }
    public DateTimeOffset OccurredAt => DateTimeOffset.UtcNow;
}
```

**Verification:**
- Build passes
- No existing tests broken

---

## Phase 2: GameSession Changes (Emission + Apply)

**Goal:** Update GameSession to emit `CaseFileGenerated` during setup and apply it during replay. This phase will break existing tests that call `StartSetup` or `RehydrateFromEvents`.

### Task 1: Emit CaseFileGenerated in StartSetup

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`

**Interfaces:**
- Modifies: `GameSession.StartSetup` to emit `CaseFileGenerated` event.
- Modifies: `GameSession.CaseFile` property to be mutable (currently readonly).

- [ ] **Step 1: Make CaseFile property mutable**

Change `public CaseFile CaseFile { get; }` to `public CaseFile CaseFile { get; private set; } = null!;`

- [ ] **Step 2: Emit CaseFileGenerated event in StartSetup**

After emitting `WorldGenerated`, emit `CaseFileGenerated`:

```csharp
var caseFileEvent = new CaseFileGenerated
{
    CaseFile = CaseFileSnapshot.FromDomain(caseFile)
};

session.Apply(caseFileEvent);
session._uncommittedEvents.Add(caseFileEvent);
```

**Verification:**
- Build fails (expected - callers need updates)
- No existing tests broken yet

---

### Task 2: Add Apply(CaseFileGenerated) Method

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`

**Interfaces:**
- Modifies: GameSession to have `Apply(CaseFileGenerated)` method.

- [ ] **Step 1: Add Apply method**

```csharp
private void Apply(CaseFileGenerated e)
{
    CaseFile = e.CaseFile.ToDomain();
    _version++;
}
```

**Verification:**
- Build fails (expected - callers need updates)

---

### Task 3: Update RehydrateFromEvents to Remove CaseFile Parameter

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`

**Interfaces:**
- Modifies: `RehydrateFromEvents` signature to remove `caseFile` parameter.
- Modifies: placeholder construction to use empty caseFile.

- [ ] **Step 1: Remove caseFile parameter from RehydrateFromEvents**

Change signature from:
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

- [ ] **Step 2: Update placeholder construction to use empty caseFile**

Construct an empty caseFile for the placeholder:
```csharp
var caseFile = new CaseFile(
    null,
    Array.Empty<Suspect>(),
    new SuspectId("placeholder"),
    CaseOpeningLead.Create("placeholder"),
    Array.Empty<Clue>());
```

**Verification:**
- Build fails (expected - callers need updates)

---

### Task 4: Add CaseFileGenerated to Event Replay Dispatch

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`

**Interfaces:**
- Modifies: `ApplyEvent` switch to handle `CaseFileGenerated`.

- [ ] **Step 1: Add case to ApplyEvent switch**

```csharp
case CaseFileGenerated cfg:
    session.Apply(cfg);
    break;
```

**Verification:**
- Build fails (expected - callers need updates)

---

## Phase 3: Update Callers (Handlers + Factory)

**Goal:** Update production code that calls `StartSetup` or `RehydrateFromEvents` to match new signatures. This phase will fix the build failures from Phase 2.

### Task 1: Update CompletePlayerSetupHandler

**Files:**
- Modify: `src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs`

**Interfaces:**
- Modifies: Handler to pass caseFile to `StartSetup` (no change - already passes it).

**Note:** No changes needed here - the handler already passes caseFile to StartSetup. The change is internal to StartSetup (emitting the event).

- [ ] **Step 1: Verify no changes needed**

Confirm that `CompletePlayerSetupHandler` already passes caseFile to `StartSetup`.

**Verification:**
- Build passes for Application project

---

### Task 2: Update INewGameFactory.ResolveWorld (if needed)

**Files:**
- Modify: `src/WildBunch.GameContent/Abstractions/INewGameFactory.cs`
- Modify: `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`

**Interfaces:**
- Modifies: Factory to return caseFile (already does).

**Note:** No changes needed here - the factory already returns caseFile.

- [ ] **Step 1: Verify no changes needed**

Confirm that `ResolveWorld` already returns caseFile.

**Verification:**
- Build passes for GameContent project

---

## Phase 4: Test Updates (Factories + Test Files)

**Goal:** Update test code that calls `StartSetup` or `RehydrateFromEvents` to match new signatures. This phase will fix the build failures from Phase 2.

### Task 1: Update StartFlowEventSourcingTests

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/Events/StartFlowEventSourcingTests.cs`

**Interfaces:**
- Modifies: Tests to expect `CaseFileGenerated` event.
- Modifies: Tests to remove caseFile from `RehydrateFromEvents` calls.

- [ ] **Step 1: Update StartSetup_Produces_PlayerSetupCompleted_AsUncommitted**

Change from expecting 2 events to 3 events, add assertion for `CaseFileGenerated`.

- [ ] **Step 2: Add test for StartSetup_Produces_CaseFileGenerated_AsUncommitted**

Verify that `CaseFileGenerated` is emitted and carries the correct snapshot.

- [ ] **Step 3: Update all RehydrateFromEvents calls**

Remove caseFile parameter from all `RehydrateFromEvents` calls in tests.

**Verification:**
- Build passes for Domain.Tests project
- Tests pass

---

### Task 2: Update TestSessionFactory

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TestSessionFactory.cs`

**Interfaces:**
- Modifies: Factory to remove caseFile from `RehydrateFromEvents` calls.

- [ ] **Step 1: Update RehydrateFromEvents calls**

Remove caseFile parameter from all `RehydrateFromEvents` calls.

**Verification:**
- Build passes for Domain.Tests project
- Tests pass

---

### Task 3: Update StubNewGameFactory (if needed)

**Files:**
- Modify: `tests/WildBunch.Application.Tests/TestDoubles/StubNewGameFactory.cs`

**Interfaces:**
- Modifies: Stub to return caseFile (already does).

**Note:** No changes needed here - the stub already returns caseFile.

- [ ] **Step 1: Verify no changes needed**

Confirm that `ResolveWorld` already returns caseFile.

**Verification:**
- Build passes for Application.Tests project

---

## Phase 5: Event Round-Trip Tests

**Goal:** Add tests to verify that `CaseFileGenerated` event can serialize/deserialize correctly and reconstruct the caseFile.

### Task 1: Create CaseFileGeneratedEventTests

**Files:**
- Create: `tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs`

**Interfaces:**
- Produces: Tests for event round-trip.

- [ ] **Step 1: Create test file**

Create `tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs` with tests:
- `CaseFileGenerated_CarriesCaseFileSnapshotThatReconstructsToIdenticalCaseFile`
- `CaseFileGenerated_PreservesTrueCulpritId`
- `CaseFileGenerated_PreservesOpeningLead`

**Verification:**
- Build passes
- Tests pass

---

## Phase 6: Persistence Serializer Update

**Goal:** Update the JSON serializer to handle `CaseFileGenerated` event.

### Task 1: Add CaseFileGenerated to ResolveEventType

**Files:**
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`

**Interfaces:**
- Modifies: `ResolveEventType` to handle `CaseFileGenerated`.

- [ ] **Step 1: Add case to ResolveEventType**

```csharp
nameof(CaseFileGenerated) => typeof(CaseFileGenerated),
```

**Verification:**
- Build passes

---

## Completion Criteria

- [ ] All phases complete
- [ ] Build passes for all projects
- [ ] All tests pass (GameContent, Domain, Application)
- [ ] Event round-trip tests pass
- [ ] No caseFile parameter in `RehydrateFromEvents` signature
- [ ] `CaseFileGenerated` event is emitted during `StartSetup`
- [ ] `Apply(CaseFileGenerated)` sets CaseFile from event
- [ ] Event stream is self-contained (no external references needed for replay)

## Post-Plan State

After this plan:
- The event stream is fully self-contained for replay
- World → `WorldGenerated` ✓
- CaseFile → `CaseFileGenerated` ✓
- Player → `PlayerSetupCompleted` + `GameStarted` ✓
- StartingTown → `StartingTownSelected` ✓

The event-sourcing contract is complete. Plan 2 (Integration) can proceed to replace the old map generation pipeline with the new geometry-first pipeline.
