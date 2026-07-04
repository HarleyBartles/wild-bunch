# Geometry-First Map Generation - Plan 1d: Remove StartNew — Core (Snapshot Fix + Factory Rewrite)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate the split-brain game-start flows by (1) adding `PublicClues` to the domain `CaseFileSnapshot` so `CaseFileGenerated` events carry the complete case file, and (2) rewriting all test helper factories (`TestSessionFactory`, `TravelTestFactory`, `StubNewGameFactory`) to use the canonical start flow (`StartSetup` → `ViewPrologue` → `SelectStartingTown` → `CompleteGameStart`) instead of `GameSession.StartNew`. This plan covers the core factories and directly-affected event-sourcing tests. Plan 1e handles the remaining 66 direct-test call sites, deletes `StartNew`, and adds the final round-trip proof.

**Architecture:** The repo has two game-start paths: the canonical event-sourced flow (`StartSetup` → `ViewPrologue` → `SelectStartingTown` → `CompleteGameStart`, used by production handlers) and the legacy `StartNew` convenience factory (used by 67 test files and `SeededNewGameFactory.Create`). `StartNew` emits only `GameStarted` — the `world` and `caseFile` are constructor arguments, not event-derived state, so sessions created via `StartNew` cannot be fully rehydrated from events alone. Rather than making `StartNew` event-sourced (which would create a second event chain duplicating the canonical flow), we remove it entirely and push all callers through the canonical flow. Additionally, the domain `CaseFileSnapshot` (carried by `CaseFileGenerated`) only captures `KnownClues`, losing `PublicClues` on replay — the persistence serializer's private `CaseFileSnapshot` already carries `PublicClues`, so this is an event-layer gap only.

**Scope of this plan:** This plan (1d) covers the core changes — `PublicClues` snapshot fix, `TestSessionFactory` rewrite (10 methods), `TravelTestFactory` rewrite (3 methods), `StubNewGameFactory` rewrite, and the directly-affected `GameSessionEventSourcingTests.cs`. The blast-radius cleanup across the remaining 66 test files, the deletion of `StartNew`/`SeededNewGameFactory.Create`, and the final round-trip proof are in **Plan 1e** (`2026-07-03-geometry-first-plan-1e-startnew-cleanup.md`), which depends on this plan.

**Greenfield Context:** This is a greenfield project with no backward compatibility requirements. Database can be dropped/rebuilt as needed. No migration for existing sessions is required.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3, existing event-sourced GameSession aggregate

## Prerequisites

- Plan 0 (Clean Slate) must be complete.
- Plan 1a (Core Pipeline) must be complete.
- Plan 1b (Event Boundary) must be complete — `WorldGenerated` and `StartingTownSelected` events must exist.
- Plan 1c (CaseFile Event Boundary) must be complete — `CaseFileGenerated` event and domain `CaseFileSnapshot` must exist.

## Current state (what we're changing)

### Two start flows (split-brain)

```
CANONICAL (production — event-sourced):
  CompletePlayerSetupHandler → GameSession.StartSetup(playerName, world, caseFile, difficulty, entropy, seedCode, saltSource)
    → emits PlayerSetupCompleted, WorldGenerated, CaseFileGenerated (3 events)
    → StartFlowPhase = SetupComplete
  ViewPrologueHandler → session.ViewPrologue(descriptor)
    → emits PrologueViewed
    → StartFlowPhase = PrologueViewed
  CompleteGameStartHandler → session.SelectStartingTown(townId) → session.CompleteGameStart(wallet, inventory)
    → emits StartingTownSelected, GameStarted (2 events)
    → StartFlowPhase = GameStarted
  TOTAL: 6 events, fully rehydratable from event stream

LEGACY (tests + SeededNewGameFactory.Create — NOT event-sourced):
  GameSession.StartNew(playerName, world, caseFile, startingTownId, wallet, inventory, difficulty, saltSource, entropy, seedCode)
    → emits GameStarted only (1 event)
    → world/caseFile are constructor args, NOT event-derived
    → CaseFile is a placeholder on replay — DATA LOSS
  TOTAL: 1 event, NOT fully rehydratable
```

### CaseFileSnapshot (event layer — data loss)

```
CaseFileSnapshot.FromDomain(caseFile)
  → captures: Suspects, TrueCulpritId, OpeningLead, KnownClues (as "Clues")
  → DOES NOT capture: PublicClues ← DATA LOSS

CaseFileSnapshot.ToDomain()
  → reconstructs: CaseFile with KnownClues only
  → PublicClues is empty ← DATA LOSS
```

## Target state (after this plan + Plan 1e)

### Single start flow (canonical only)

```
ALL callers (tests + production):
  GameSession.StartSetup(playerName, world, caseFile, difficulty, entropy, seedCode, saltSource)
    → emits PlayerSetupCompleted, WorldGenerated, CaseFileGenerated
  session.ViewPrologue(descriptor)
    → emits PrologueViewed
  session.SelectStartingTown(townId)
    → emits StartingTownSelected
  session.CompleteGameStart(wallet, inventory)
    → emits GameStarted
  TOTAL: 6 events, fully rehydratable

GameSession.StartNew — DELETED
SeededNewGameFactory.Create — DELETED (dead in production; only tests used it)
```

### CaseFileSnapshot (event layer — complete)

```
CaseFileSnapshot.FromDomain(caseFile)
  → captures: Suspects, TrueCulpritId, OpeningLead, KnownClues (as "Clues"), PublicClues ← FIXED

CaseFileSnapshot.ToDomain()
  → reconstructs: CaseFile with KnownClues AND PublicClues ← FIXED
```

## Files

**Modified files:**
- `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs` — add `PublicClues` to snapshot record, update `FromDomain`/`ToDomain`
- `tests/WildBunch.Domain.Tests/TestSessionFactory.cs` — add `StartGameCanonical` helper, rewrite all 10 factory methods to use it instead of `StartNew`
- `tests/WildBunch.Domain.Tests/TravelTestFactory.cs` — rewrite 3 factory methods to use `TestSessionFactory.StartGameCanonical` instead of `StartNew`
- `tests/WildBunch.Application.Tests/TestDoubles/StubNewGameFactory.cs` — rewrite `CreateSession` to use canonical flow instead of `StartNew`
- `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs` — migrate 3 `StartNew` calls to canonical flow, update event count assertions (6 events instead of 1, version 6 instead of 1)
- `tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs` — add `PublicClues` round-trip test

**No new files needed.** All event types and snapshot types already exist from Plans 1b/1c.

---

## Phase 1: PublicClues in CaseFileSnapshot

**Goal:** Fix the data loss gap where `CaseFileSnapshot` (the domain event payload) only captures `KnownClues`, not `PublicClues`. The persistence serializer's private `CaseFileSnapshot` already carries `PublicClues` — this aligns the event layer.

### Task 1: Add PublicClues to CaseFileSnapshot

**Files:**
- Modify: `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs:9-28`
- Test: `tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs`

**Interfaces:**
- Produces: `CaseFileSnapshot` record now has `PublicClues` parameter; `FromDomain` captures `caseFile.PublicClues`; `ToDomain` passes `publicClues` to `CaseFile` constructor

- [ ] **Step 1: Write the failing test**

Add a test to `tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs` that verifies `PublicClues` survive the round-trip through `CaseFileSnapshot`:

```csharp
[Fact]
public void CaseFileSnapshot_RoundTrip_Preserves_PublicClues()
{
    var suspects = new[]
    {
        new Suspect(new SuspectId("suspect-1"), "Ira Flint",
            SuspectTraits.FromTags(SuspectTraitTags.Local), SuspectStatus.AtLarge)
    };
    var publicClue = new Clue(
        new ClueId("clue-public-1"),
        ClueKind.Alias,
        "A dusty boot print.",
        new[] { new SuspectId("suspect-1") },
        InvestigationTargetKind.Suspected,
        InvestigationSourceKind.LocalGossip,
        source: "test source",
        context: "test context");

    var caseFile = new CaseFile(
        accusation: null,
        suspects,
        trueCulpritId: new SuspectId("suspect-1"),
        openingLead: CaseOpeningLead.Create("Follow the trail."),
        knownClues: Array.Empty<Clue>(),
        publicClues: new[] { publicClue });

    var snapshot = CaseFileSnapshot.FromDomain(caseFile);
    var restored = snapshot.ToDomain();

    Assert.Single(restored.PublicClues);
    Assert.Equal(publicClue.Id, restored.PublicClues[0].Id);
    Assert.Equal(publicClue.Description, restored.PublicClues[0].Description);
    Assert.Empty(restored.KnownClues);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs --filter "CaseFileSnapshot_RoundTrip_Preserves_PublicClues"`
Expected: FAIL — `Assert.Single()` fails because `restored.PublicClues` is empty (snapshot doesn't carry PublicClues yet)

- [ ] **Step 3: Add PublicClues to CaseFileSnapshot record**

In `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs`, update the record signature and both conversion methods:

```csharp
public sealed record CaseFileSnapshot(
    IReadOnlyList<SuspectSnapshot> Suspects,
    string TrueCulpritId,
    CaseOpeningLead OpeningLead,
    IReadOnlyList<ClueSnapshot> Clues,
    IReadOnlyList<ClueSnapshot> PublicClues)
{
    public static CaseFileSnapshot FromDomain(CaseFile caseFile)
        => new(
            caseFile.Suspects.Select<Suspect, SuspectSnapshot>(SuspectSnapshot.FromDomain).ToArray(),
            caseFile.TrueCulpritId.Value,
            caseFile.OpeningLead,
            caseFile.KnownClues.Select<Clue, ClueSnapshot>(ClueSnapshot.FromDomain).ToArray(),
            caseFile.PublicClues.Select<Clue, ClueSnapshot>(ClueSnapshot.FromDomain).ToArray());

    public CaseFile ToDomain()
        => new(
            accusation: null,
            suspects: Suspects.Select<SuspectSnapshot, Suspect>(s => s.ToDomain()).ToArray(),
            trueCulpritId: new SuspectId(TrueCulpritId),
            openingLead: OpeningLead,
            knownClues: Clues.Select<ClueSnapshot, Clue>(c => c.ToDomain()).ToArray(),
            publicClues: PublicClues.Select<ClueSnapshot, Clue>(c => c.ToDomain()).ToArray());
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs --filter "CaseFileSnapshot_RoundTrip_Preserves_PublicClues"`
Expected: PASS

- [ ] **Step 5: Run full Domain.Tests to check for breakage from the new constructor parameter**

Run: `dotnet test tests/WildBunch.Domain.Tests/ --filter "CaseFileGenerated"`
Expected: All `CaseFileGeneratedEventTests` pass. If any other tests break because they construct `CaseFileSnapshot` directly (without using `FromDomain`), fix them by adding the `PublicClues` argument.

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/Cases/CaseFileSnapshot.cs tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs
git commit -m "fix: add PublicClues to domain CaseFileSnapshot to prevent event replay data loss

The domain CaseFileSnapshot (carried by CaseFileGenerated events) only
captured KnownClues, losing PublicClues on replay. The persistence
serializer's private CaseFileSnapshot already carried PublicClues — this
aligns the event layer. Adds PublicClues parameter to the record, captures
it in FromDomain, and reconstructs it in ToDomain."
```

---

## Phase 2: Rewrite TestSessionFactory to use canonical flow

**Goal:** Replace all 10 `GameSession.StartNew(...)` calls in `TestSessionFactory.cs` with the canonical start flow (`StartSetup` → `ViewPrologue` → `SelectStartingTown` → `CompleteGameStart`). This is the single highest-leverage change — every test that delegates to `TestSessionFactory` automatically gets the canonical flow without any further changes.

**Key insight:** `StartNew` takes `wallet` and `inventory` as parameters and sets them directly on the player. The canonical flow's `CompleteGameStart(wallet, inventory)` also takes these as parameters. The migration is a mechanical replacement: instead of one `StartNew` call, make four calls in sequence. We add a private helper to avoid repeating the 4-step sequence 10 times.

### Task 2: Add StartGameCanonical helper to TestSessionFactory

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TestSessionFactory.cs` (add helper method after the class opening, before `CreateDefault`)

**Interfaces:**
- Produces: `TestSessionFactory.StartGameCanonical(...)` — private static helper that runs the 4-step canonical start flow and returns a fully-started session with `StartFlowPhase = GameStarted`

- [ ] **Step 1: Add the StartGameCanonical helper method**

Add this private static method to `TestSessionFactory` in `tests/WildBunch.Domain.Tests/TestSessionFactory.cs`, immediately after the class opening brace (before `CreateDefault`):

```csharp
/// <summary>
/// Creates a fully-started game session using the canonical start flow
/// (StartSetup → ViewPrologue → SelectStartingTown → CompleteGameStart).
/// This replaces the legacy StartNew convenience factory with the same
/// event-sourced flow used by production handlers, ensuring all test
/// sessions are fully rehydratable from their event stream.
/// </summary>
private static GameSession StartGameCanonical(
    string playerName,
    DomainWorld world,
    CaseFile caseFile,
    TownId startingTownId,
    Wallet? wallet = null,
    DomainInventory? inventory = null,
    GameDifficulty gameDifficulty = GameDifficulty.Easy,
    SaltSource? saltSource = null,
    GameEntropy gameEntropy = GameEntropy.Classic,
    string? seedCode = null)
{
    var resolvedSaltSource = saltSource ?? SaltSource.CreateFixed(string.Empty);
    var resolvedSeedCode = seedCode ?? "test-seed";

    var session = GameSession.StartSetup(
        playerName,
        world,
        caseFile,
        gameDifficulty,
        gameEntropy,
        resolvedSeedCode,
        resolvedSaltSource);

    session.ViewPrologue("test-prologue-descriptor");
    session.SelectStartingTown(startingTownId);
    session.CompleteGameStart(wallet, inventory);

    return session;
}
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build tests/WildBunch.Domain.Tests/`
Expected: Build succeeds (the helper is not called yet, but must compile)

- [ ] **Step 3: Commit**

```bash
git add tests/WildBunch.Domain.Tests/TestSessionFactory.cs
git commit -m "refactor: add StartGameCanonical helper to TestSessionFactory

Private helper that runs the 4-step canonical start flow
(StartSetup → ViewPrologue → SelectStartingTown → CompleteGameStart).
Replaces the legacy StartNew convenience factory with the same
event-sourced flow used by production handlers. Subsequent tasks
migrate each factory method to use this helper."
```

### Task 3: Migrate all 10 TestSessionFactory methods to use StartGameCanonical

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TestSessionFactory.cs` — all 10 methods that call `StartNew`

**Pattern:** Each method currently has a block like:
```csharp
var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
    Wallet.Starting(25m), inventory, GameDifficulty.Easy,
    SaltSource.CreateFixed(string.Empty));
session.MarkEventsCommitted();
```
Replace with:
```csharp
var session = StartGameCanonical("Ranger Vale", world, caseFile, town.Id,
    Wallet.Starting(25m), inventory, GameDifficulty.Easy,
    SaltSource.CreateFixed(string.Empty));
session.MarkEventsCommitted();
```

The only difference is `GameSession.StartNew(` → `StartGameCanonical(`. The parameters are the same. The `MarkEventsCommitted()` call stays — it clears the 6 setup events so tests start with a clean event list.

- [ ] **Step 1: Migrate CreateDefault (line 61)**

Replace:
```csharp
var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
    Wallet.Starting(25m), inventory, GameDifficulty.Easy,
    SaltSource.CreateFixed(string.Empty));
```
With:
```csharp
var session = StartGameCanonical("Ranger Vale", world, caseFile, town.Id,
    Wallet.Starting(25m), inventory, GameDifficulty.Easy,
    SaltSource.CreateFixed(string.Empty));
```

- [ ] **Step 2: Migrate CreateWithConfrontableSaloonSuspect (line 102)**

Replace:
```csharp
var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
    Wallet.Starting(25m), inventory: null, GameDifficulty.Easy,
    SaltSource.CreateFixed(string.Empty));
```
With:
```csharp
var session = StartGameCanonical("Ranger Vale", world, caseFile, town.Id,
    Wallet.Starting(25m), inventory: null, GameDifficulty.Easy,
    SaltSource.CreateFixed(string.Empty));
```

- [ ] **Step 3: Migrate CreateWithKillerReleaseGateOpen (line 146)**

Same pattern: `GameSession.StartNew(` → `StartGameCanonical(`

- [ ] **Step 4: Migrate CreateWithNoConfrontableSaloonSuspect (line 174)**

Same pattern: `GameSession.StartNew(` → `StartGameCanonical(`

- [ ] **Step 5: Migrate CreateWithNoSaloon (line 220)**

Same pattern: `GameSession.StartNew(` → `StartGameCanonical(`

- [ ] **Step 6: Migrate CreateWithWarrantedSuspect (line 269)**

Same pattern: `GameSession.StartNew(` → `StartGameCanonical(`

- [ ] **Step 7: Migrate CreateWithIneligibleWarrantedSuspect (line 320)**

Same pattern: `GameSession.StartNew(` → `StartGameCanonical(`

- [ ] **Step 8: Migrate CreateWithArmedCorrectDeclarationSetup (line 418)**

Same pattern: `GameSession.StartNew(` → `StartGameCanonical(`

- [ ] **Step 9: Migrate CreateWithPublicClue (line 477)**

Same pattern: `GameSession.StartNew(` → `StartGameCanonical(`

- [ ] **Step 10: Migrate CreateWithPublicWarrantAndClue (line 571)**

Same pattern: `GameSession.StartNew(` → `StartGameCanonical(`

- [ ] **Step 11: Run Domain.Tests to verify factory migration**

Run: `dotnet test tests/WildBunch.Domain.Tests/ --filter "TestSessionFactory|BountySaloon|InvestigationEventSourcing|SaloonPersonOfInterest|WantedSuspect|SheriffTurnIn|PublicClue|SpentSource|PublicWarrant"`
Expected: All tests that use `TestSessionFactory` methods pass. The sessions now produce 6 setup events (cleared by `MarkEventsCommitted`) instead of 1, but since all factory methods call `MarkEventsCommitted()` after creation, downstream tests see no difference in `UncommittedEvents`.

- [ ] **Step 12: Commit**

```bash
git add tests/WildBunch.Domain.Tests/TestSessionFactory.cs
git commit -m "refactor: migrate all TestSessionFactory methods to canonical start flow

All 10 factory methods now use StartGameCanonical (StartSetup →
ViewPrologue → SelectStartingTown → CompleteGameStart) instead of
GameSession.StartNew. Each method calls MarkEventsCommitted() after
creation, so downstream tests see no difference in UncommittedEvents.
This is the single highest-leverage change — every test that delegates
to TestSessionFactory automatically gets the canonical flow."
```

---

## Phase 3: Rewrite TravelTestFactory to use canonical flow

**Goal:** Replace the 3 `GameSession.StartNew(...)` calls in `TravelTestFactory.cs` with the canonical flow. `TravelTestFactory` is in the same project as `TestSessionFactory`, so it can call `TestSessionFactory.StartGameCanonical` — but that method is private. We make it `internal static` so `TravelTestFactory` can use it.

### Task 4: Make StartGameCanonical internal and migrate TravelTestFactory

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TestSessionFactory.cs` — change `private static` to `internal static` on `StartGameCanonical`
- Modify: `tests/WildBunch.Domain.Tests/TravelTestFactory.cs` — migrate 3 `StartNew` calls

- [ ] **Step 1: Make StartGameCanonical internal**

In `tests/WildBunch.Domain.Tests/TestSessionFactory.cs`, change:
```csharp
private static GameSession StartGameCanonical(
```
To:
```csharp
internal static GameSession StartGameCanonical(
```

- [ ] **Step 2: Migrate TravelTestFactory.RecaptureGameStartedForReplay (line 71)**

This method currently re-runs `StartNew` to capture the `GameStarted` event for replay tests. After migration, the session already produces the full event stream through the canonical flow. Replace the entire method:

```csharp
internal static GameStarted RecaptureGameStartedForReplay(GameSession session)
{
    // After migration to the canonical flow, the session's event stream
    // already contains GameStarted. We can extract it from the committed
    // events by re-running the canonical flow with the same inputs.
    var seed = TestSessionFactory.StartGameCanonical(
        session.Player.Name,
        session.World,
        TestSessionFactory.CreateBaselineCaseFileFor(session),
        session.Player.CurrentTownId,
        session.Player.Wallet,
        session.Player.Inventory,
        session.GameDifficulty,
        session.SaltSource);
    return Assert.IsType<GameStarted>(seed.UncommittedEvents.OfType<GameStarted>().Single());
}
```

Note: `UncommittedEvents` now has 6 events (PlayerSetupCompleted, WorldGenerated, CaseFileGenerated, PrologueViewed, StartingTownSelected, GameStarted). We use `OfType<GameStarted>().Single()` instead of `.Single()` to extract just the `GameStarted` event.

- [ ] **Step 3: Migrate TravelTestFactory.CreateHighRiskJourney (line 118)**

Replace:
```csharp
var session = GameSession.StartNew("Ranger Vale", world, caseFile,
    pinecross.Id, Wallet.Starting(25m), inventory,
    GameDifficulty.Easy,
    SaltSource.CreateFixed(string.Empty));
```
With:
```csharp
var session = TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile,
    pinecross.Id, Wallet.Starting(25m), inventory,
    GameDifficulty.Easy,
    SaltSource.CreateFixed(string.Empty));
```

- [ ] **Step 4: Migrate TravelTestFactory.CreateSixDayQuietJourney (line 165)**

Replace:
```csharp
var session = GameSession.StartNew("Ranger Vale", world, caseFile,
    origin.Id, Wallet.Starting(25m), inventory,
    GameDifficulty.Easy,
    SaltSource.CreateFixed(string.Empty));
```
With:
```csharp
var session = TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile,
    origin.Id, Wallet.Starting(25m), inventory,
    GameDifficulty.Easy,
    SaltSource.CreateFixed(string.Empty));
```

- [ ] **Step 5: Run travel tests to verify**

Run: `dotnet test tests/WildBunch.Domain.Tests/ --filter "TravelTestFactory|TravelResolver|TravelDayPlan|TravelRules|AdvanceTravel"`
Expected: All travel tests pass.

- [ ] **Step 6: Commit**

```bash
git add tests/WildBunch.Domain.Tests/TestSessionFactory.cs tests/WildBunch.Domain.Tests/TravelTestFactory.cs
git commit -m "refactor: migrate TravelTestFactory to canonical start flow

Made StartGameCanonical internal so TravelTestFactory can use it.
Migrated 3 StartNew calls (RecaptureGameStartedForReplay,
CreateHighRiskJourney, CreateSixDayQuietJourney) to use
StartGameCanonical. Updated RecaptureGameStartedForReplay to use
OfType<GameStarted>().Single() since the canonical flow produces 6
events, not 1."
```

---

## Phase 4: Rewrite StubNewGameFactory to use canonical flow

**Goal:** Replace the single `GameSession.StartNew(...)` call in `StubNewGameFactory.CreateSession` with the canonical flow. `StubNewGameFactory` is in `WildBunch.Application.Tests`, which does not reference `WildBunch.Domain.Tests`. We add a local copy of the canonical flow helper.

### Task 5: Migrate StubNewGameFactory.CreateSession

**Files:**
- Modify: `tests/WildBunch.Application.Tests/TestDoubles/StubNewGameFactory.cs`

- [ ] **Step 1: Migrate CreateSession (line 109)**

Replace:
```csharp
return GameSession.StartNew(
    "Ranger Vale",
    world,
    caseFile,
    dustvale.Id,
    Wallet.Starting(25m),
    inventory,
    saltSource: SaltSource.CreateFixed("application-tests"));
```
With:
```csharp
return StartGameCanonical(
    "Ranger Vale",
    world,
    caseFile,
    dustvale.Id,
    Wallet.Starting(25m),
    inventory,
    saltSource: SaltSource.CreateFixed("application-tests"));
```

- [ ] **Step 2: Add private StartGameCanonical helper to StubNewGameFactory**

Add this private static method to `StubNewGameFactory`:

```csharp
private static GameSession StartGameCanonical(
    string playerName,
    World world,
    CaseFile caseFile,
    TownId startingTownId,
    Wallet? wallet = null,
    Inventory? inventory = null,
    GameDifficulty gameDifficulty = GameDifficulty.Standard,
    SaltSource? saltSource = null,
    GameEntropy gameEntropy = GameEntropy.Classic,
    string? seedCode = null)
{
    var resolvedSaltSource = saltSource ?? SaltSource.CreateFixed("application-tests");
    var resolvedSeedCode = seedCode ?? "stub-seed";

    var session = GameSession.StartSetup(
        playerName,
        world,
        caseFile,
        gameDifficulty,
        gameEntropy,
        resolvedSeedCode,
        resolvedSaltSource);

    session.ViewPrologue("test-prologue-descriptor");
    session.SelectStartingTown(startingTownId);
    session.CompleteGameStart(wallet, inventory);

    return session;
}
```

- [ ] **Step 3: Run Application.Tests to verify**

Run: `dotnet test tests/WildBunch.Application.Tests/ --filter "StubNewGameFactory"`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add tests/WildBunch.Application.Tests/TestDoubles/StubNewGameFactory.cs
git commit -m "refactor: migrate StubNewGameFactory to canonical start flow

Replaced StartNew with a local StartGameCanonical helper that runs
the 4-step canonical flow. Application.Tests does not reference
Domain.Tests, so the helper is duplicated locally."
```

---

## Phase 5: Update GameSessionEventSourcingTests

**Goal:** Migrate the 3 `StartNew` calls in `GameSessionEventSourcingTests.cs` to the canonical flow and update event count assertions. These tests directly assert on event counts and versions, so they need careful updates.

### Task 6: Migrate GameSessionEventSourcingTests to canonical flow

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs`

**Context:** The canonical flow produces 6 events (PlayerSetupCompleted, WorldGenerated, CaseFileGenerated, PrologueViewed, StartingTownSelected, GameStarted) and sets version to 6. The old `StartNew` produced 1 event and version 1. Tests that assert on initial event count or version must be updated. Tests that call `MarkEventsCommitted()` before asserting on operation events are unaffected.

- [ ] **Step 1: Update CreateSession helper (line 370-382)**

Replace the `CreateSession` helper:
```csharp
private static GameSession CreateSession(
    Wallet? wallet = null,
    DomainInventory? inventory = null)
{
    var world = CreateWorld();
    var caseFile = CreateCaseFile();
    var resolvedInventory = inventory ?? new DomainInventory(new[]
    {
        new DomainInventoryItem(DomainItemKind.Food, 1),
        new DomainInventoryItem(DomainItemKind.Canteen, 1)
    });
    return GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), wallet ?? Wallet.Starting(25m), resolvedInventory);
}
```
With:
```csharp
private static GameSession CreateSession(
    Wallet? wallet = null,
    DomainInventory? inventory = null)
{
    var world = CreateWorld();
    var caseFile = CreateCaseFile();
    var resolvedInventory = inventory ?? new DomainInventory(new[]
    {
        new DomainInventoryItem(DomainItemKind.Food, 1),
        new DomainInventoryItem(DomainItemKind.Canteen, 1)
    });
    var session = GameSession.StartSetup(
        "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic,
        "test-seed", SaltSource.CreateFixed("test-salt"));
    session.ViewPrologue("test-prologue-descriptor");
    session.SelectStartingTown(new TownId("pinecross"));
    session.CompleteGameStart(wallet ?? Wallet.Starting(25m), resolvedInventory);
    return session;
}
```

- [ ] **Step 2: Update StartNew_Produces_GameStarted_Event_As_Uncommitted (line 18)**

Rename and update this test. The canonical flow produces 6 events, not 1. Replace:
```csharp
[Fact]
public void StartNew_Produces_GameStarted_Event_As_Uncommitted()
{
    var session = CreateSession();
    var single = Assert.Single(session.UncommittedEvents);
    var gameStarted = Assert.IsType<GameStarted>(single);
    Assert.Equal("Ranger Vale", gameStarted.PlayerName);
    Assert.Equal(new TownId("pinecross"), gameStarted.StartingTownId);
    Assert.Equal("Pinecross", gameStarted.StartingTownName);
    Assert.Equal(1000, gameStarted.StartingHealth);
    Assert.Equal(25m, gameStarted.StartingWallet);
    Assert.Equal(GameDifficulty.Standard, gameStarted.GameDifficulty);
    Assert.Equal(GameEntropy.Classic, gameStarted.GameEntropy);
}
```
With:
```csharp
[Fact]
public void CanonicalStart_Produces_GameStarted_Event_As_Uncommitted()
{
    var session = CreateSession();
    var gameStarted = session.UncommittedEvents.OfType<GameStarted>().Single();
    Assert.Equal("Ranger Vale", gameStarted.PlayerName);
    Assert.Equal(new TownId("pinecross"), gameStarted.StartingTownId);
    Assert.Equal("Pinecross", gameStarted.StartingTownName);
    Assert.Equal(1000, gameStarted.StartingHealth);
    Assert.Equal(25m, gameStarted.StartingWallet);
    Assert.Equal(GameDifficulty.Standard, gameStarted.GameDifficulty);
    Assert.Equal(GameEntropy.Classic, gameStarted.GameEntropy);
}
```

- [ ] **Step 3: Update StartNew_WithSeedCode_Produces_GameStarted_Event_WithSeedCode (line 33)**

This test creates a session directly with a seed code. Migrate to canonical flow. Replace the entire test:
```csharp
[Fact]
public void CanonicalStart_WithSeedCode_Produces_GameStarted_Event_WithSeedCode()
{
    var world = CreateWorld();
    var caseFile = CreateCaseFile();
    var seedCode = "test-seed-code-12345";

    var session = GameSession.StartSetup(
        "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic,
        seedCode, SaltSource.CreateRuntime());
    session.ViewPrologue("test-prologue-descriptor");
    session.SelectStartingTown(new TownId("pinecross"));
    session.CompleteGameStart();

    var gameStarted = session.UncommittedEvents.OfType<GameStarted>().Single();
    Assert.Equal(seedCode, gameStarted.SeedCode);
}
```

- [ ] **Step 4: Update RehydrateFromEvents_Restores_SeedCode_From_GameStarted_Event (line 57)**

Migrate the `StartNew` call to canonical flow. Replace the session creation:
```csharp
var session = GameSession.StartNew(
    "Ranger Vale",
    world,
    caseFile,
    new TownId("pinecross"),
    wallet: null,
    inventory: null,
    GameDifficulty.Standard,
    SaltSource.CreateRuntime(),
    GameEntropy.Classic,
    seedCode);
```
With:
```csharp
var session = GameSession.StartSetup(
    "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic,
    seedCode, SaltSource.CreateRuntime());
session.ViewPrologue("test-prologue-descriptor");
session.SelectStartingTown(new TownId("pinecross"));
session.CompleteGameStart();
```

- [ ] **Step 5: Update StartNew_Increments_Version_To_One (line 115)**

The canonical flow produces version 6, not 1. Rename and update:
```csharp
[Fact]
public void CanonicalStart_Increments_Version_To_Six()
{
    var session = CreateSession();
    Assert.Equal(6, session.Version);
}
```

- [ ] **Step 6: Run GameSessionEventSourcingTests to verify**

Run: `dotnet test tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs`
Expected: All tests pass. Tests that call `MarkEventsCommitted()` before asserting on operation events (Purchase, etc.) are unaffected because the 6 setup events are cleared.

- [ ] **Step 7: Commit**

```bash
git add tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs
git commit -m "refactor: migrate GameSessionEventSourcingTests to canonical start flow

Migrated 3 StartNew calls to StartSetup → ViewPrologue →
SelectStartingTown → CompleteGameStart. Updated event count and
version assertions: canonical flow produces 6 events (version 6)
instead of 1 event (version 1). Tests that call MarkEventsCommitted()
before asserting on operation events are unaffected."
```

---

## Verification

- [ ] **Final verification: Run full Domain.Tests suite**

Run: `dotnet test tests/WildBunch.Domain.Tests/`
Expected: All tests pass. Any failures should be in tests that call `StartNew` directly (not through factories) — those are handled in Plan 1e.

- [ ] **Final verification: Run Application.Tests suite**

Run: `dotnet test tests/WildBunch.Application.Tests/`
Expected: All tests pass. Any failures should be in tests that call `StartNew` directly — those are handled in Plan 1e.

- [ ] **Final verification: Confirm no StartNew calls remain in factories**

Run: `rg "StartNew" tests/WildBunch.Domain.Tests/TestSessionFactory.cs tests/WildBunch.Domain.Tests/TravelTestFactory.cs tests/WildBunch.Application.Tests/TestDoubles/StubNewGameFactory.cs`
Expected: No matches (all factory calls migrated to canonical flow)
