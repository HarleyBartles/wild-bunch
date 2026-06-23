# BUNCH-78 Phase 2: Investigation/Case-Update Event Migration

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.
>
> **Prerequisite:** Phase 1 (`2026-06-22-bunch-78-phase1-response-bridge.md`) must be complete. Read `2026-06-22-bunch-78-overview.md` for preflight context.

**Goal:** Migrate 5 investigation methods to typed domain events + `Apply` mutation + orchestration normalization. Move 5 handlers onto `GameSessionCommandHandler`. Update projectors and replay dispatcher.

**Architecture:** Introduce `InvestigationPerformed` event (single event covers all investigation outcomes). Add peek methods to `CaseFile` for non-mutating clue/warrant discovery, and reveal methods for the `Apply` mutation path. Refactor 5 investigation methods to: peek → produce event → `Apply` → record event. Move 5 handlers to `ExecuteWithRetryAsync`. Update `DiaryProjector` and `GameSessionEventReplay` to handle the new event.

**Source-level API corrections (Harley's amendments):**
- `IsSpent(InvestigationSourceKind)` and `WantedPostersSpent` live on `CurrentTownVisit` (`TownVisitState`), NOT on `CurrentTown` (`TownAggregate`). Use `CurrentTownVisit.IsSpent(...)` and `CurrentTownVisit.WantedPostersSpent` for non-mutating spent checks. `CurrentTown.CheckSource(...)` and `CurrentTown.CheckWantedPosters()` remain the mutating calls used inside `Apply`.
- The old `RevealNextPublicClue`/`RevealNextPublicWarrant` methods remove stale already-known entries from the public pool, remove the revealed item, and then discover it. The new peek + event + apply path must end with the same known/public clue and warrant state. `Apply` calls new `CaseFile.RevealClue(Clue)` / `CaseFile.RevealWarrant(Warrant)` methods that handle removal + discovery + stale cleanup.
- No `ApplyInvestigationPerformedForTest` test hook. Tests prove behavior through public command methods, replay tests with fresh baseline `CaseFile`, and projector tests.

**Tech Stack:** C# 14 / .NET, xUnit

---

## Event Design

### `InvestigationPerformed` (new typed domain event)

```csharp
// src/WildBunch.Domain/Events/InvestigationPerformed.cs
using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: an investigation source was checked, possibly revealing a public clue and/or warrant.
/// Carries only public data — hidden culprit truth is never in this event.
/// See ADR-0028.
/// </summary>
public sealed record InvestigationPerformed : IDomainEvent
{
    public required InvestigationSourceKind SourceKind { get; init; }
    public required TownId TownId { get; init; }
    public required string Message { get; init; }
    public Clue? Clue { get; init; }
    public Warrant? Warrant { get; init; }
    public bool AdvanceClock { get; init; } = true;
}
```

**Why a single event:** `ReadWantedPosters` can reveal both a warrant AND a clue in one call. Separate events would require two clock-advance decisions. A single event with optional `Clue` and `Warrant` handles all 5 methods cleanly. Matches the existing pragmatic event style (`GameStarted` carries lots of data).

**Hidden-truth safety:** `Clue` and `Warrant` are public records. `CaseFile.RevealNextPublicClue` only reveals from `_publicClues`. The hidden culprit `SuspectId` is never in the event. Verified at `CaseFile.cs` lines 287-371.

---

## File Structure

| File | Action |
|------|--------|
| `src/WildBunch.Domain/Events/InvestigationPerformed.cs` | Create: new typed domain event |
| `src/WildBunch.Domain/Cases/CaseFile.cs` | Add `PeekNextPublicClue`, `PeekNextPublicWarrant` (non-mutating) and `RevealClue`, `RevealWarrant` (mutating, for Apply path) |
| `src/WildBunch.Domain/Game/GameSession.cs` | Refactor 5 investigation methods to event-sourced pattern; add `Apply(InvestigationPerformed)` |
| `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` | Add `InvestigationPerformed` to `ApplyEvent` dispatcher |
| `src/WildBunch.Application/Projections/DiaryProjector.cs` | Handle `InvestigationPerformed` events |
| `src/WildBunch.Application/Games/Commands/GatherLocalGossipHandler.cs` | Move to `GameSessionCommandHandler` base |
| `src/WildBunch.Application/Games/Commands/FollowTelegraphLeadsHandler.cs` | Move to `GameSessionCommandHandler` base |
| `src/WildBunch.Application/Games/Commands/InspectNoticeBoardHandler.cs` | Move to `GameSessionCommandHandler` base |
| `src/WildBunch.Application/Games/Commands/CheckSheriffRecordsHandler.cs` | Move to `GameSessionCommandHandler` base |
| `src/WildBunch.Application/Games/Commands/ReadWantedPostersHandler.cs` | Move to `GameSessionCommandHandler` base |
| `tests/WildBunch.Application.Tests/InvestigationEventMigrationTests.cs` | New: characterization + migration tests |
| `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs` | Update `KnownLegacyAddLogEntryCallSiteCount` |
| `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs` | Add investigation replay test |

---

## Task 1: Add peek and reveal methods to CaseFile

**Files:**
- Modify: `src/WildBunch.Domain/Cases/CaseFile.cs`

**Interfaces:**
- Produces: `CaseFile.PeekNextPublicClue(Func<Clue, bool>)` and `CaseFile.PeekNextPublicWarrant(InvestigationSourceKind?)` — non-mutating read methods.
- Produces: `CaseFile.RevealClue(Clue)` and `CaseFile.RevealWarrant(Warrant)` — mutating methods for the `Apply` path that preserve the exact behavior of the old `RevealNextPublicClue`/`RevealNextPublicWarrant` (remove from public pool, discover into known).

- [ ] **Step 1: Write the failing test**

Create `tests/WildBunch.Domain.Tests/CaseFilePeekTests.cs`:

```csharp
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests;

public sealed class CaseFilePeekTests
{
    [Fact]
    public void PeekNextPublicClueReturnsMatchingClueWithoutRemovingIt()
    {
        var clue = new Clue(new ClueId("clue-1"), ClueKind.PhysicalEvidence, "A dusty boot print.");
        var caseFile = new CaseFile(
            null,
            Array.Empty<Suspect>(),
            null,
            publicClues: new[] { clue });

        var peeked = caseFile.PeekNextPublicClue(c => c.SourceKind == InvestigationSourceKind.LocalGossip);

        Assert.Null(peeked); // clue has no source kind set, doesn't match

        var peekedAny = caseFile.PeekNextPublicClue(_ => true);
        Assert.NotNull(peekedAny);
        Assert.Equal(clue.Id, peekedAny!.Id);

        // Verify it was NOT removed
        Assert.Single(caseFile.PublicClues);
    }

    [Fact]
    public void PeekNextPublicClueSkipsAlreadyKnownClues()
    {
        var clue = new Clue(new ClueId("clue-1"), ClueKind.PhysicalEvidence, "A dusty boot print.");
        var caseFile = new CaseFile(
            null, Array.Empty<Suspect>(), null,
            publicClues: new[] { clue },
            knownClues: new[] { clue });

        var peeked = caseFile.PeekNextPublicClue(_ => true);

        Assert.Null(peeked);
    }

    [Fact]
    public void RevealClueRemovesFromPublicPoolAndDiscoversIt()
    {
        var clue = new Clue(new ClueId("clue-1"), ClueKind.PhysicalEvidence, "A dusty boot print.");
        var caseFile = new CaseFile(
            null, Array.Empty<Suspect>(), null,
            publicClues: new[] { clue });

        caseFile.RevealClue(clue);

        Assert.Contains(clue, caseFile.KnownClues);
        Assert.DoesNotContain(clue, caseFile.PublicClues);
    }

    [Fact]
    public void RevealClueAlsoCleansStaleKnownEntriesFromPublicPool()
    {
        // The old RevealNextPublicClue removes already-known entries from the public pool
        // as it scans. RevealClue must preserve this cleanup behavior.
        var clue1 = new Clue(new ClueId("clue-1"), ClueKind.PhysicalEvidence, "Boot print.");
        var clue2 = new Clue(new ClueId("clue-2"), ClueKind.WitnessStatement, "A stranger asked about the sheriff.");
        var caseFile = new CaseFile(
            null, Array.Empty<Suspect>(), null,
            publicClues: new[] { clue1, clue2 },
            knownClues: new[] { clue1 }); // clue1 is already known but still in public pool (stale)

        caseFile.RevealClue(clue2);

        Assert.Contains(clue2, caseFile.KnownClues);
        Assert.DoesNotContain(clue2, caseFile.PublicClues);
        // Stale clue1 should also be cleaned from public pool
        Assert.DoesNotContain(clue1, caseFile.PublicClues);
    }

    [Fact]
    public void RevealWarrantRemovesFromPublicPoolAndDiscoversIt()
    {
        var warrant = new Warrant(new WarrantId("w-1"), "Bill the Outlaw",
            new WarrantTerms(WarrantDisposition.DeadOrAlive, 500m, Array.Empty<string>(),
                sourceKind: InvestigationSourceKind.SheriffWarrants));
        var caseFile = new CaseFile(
            null, Array.Empty<Suspect>(), null,
            publicWarrants: new[] { warrant });

        caseFile.RevealWarrant(warrant);

        Assert.Contains(warrant, caseFile.KnownWarrants);
        Assert.DoesNotContain(warrant, caseFile.PublicWarrants);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "CaseFilePeekTests"`
Expected: FAIL — `PeekNextPublicClue` does not exist.

- [ ] **Step 3: Add peek and reveal methods to CaseFile**

In `src/WildBunch.Domain/Cases/CaseFile.cs`, add after the existing `RevealNextPublicClue(Func<Clue, bool>, ...)` method:

```csharp
/// <summary>
/// Non-mutating peek: finds the next revealable public clue matching the predicate,
/// without removing it from the public pool or adding it to known clues.
/// Used by event-sourced investigation methods to decide before producing an event.
/// </summary>
public Clue? PeekNextPublicClue(Func<Clue, bool> canReveal)
{
    ArgumentNullException.ThrowIfNull(canReveal);

    foreach (var clue in _publicClues)
    {
        if (!canReveal(clue)) continue;
        if (_knownClues.Any(existing => existing.Id.Equals(clue.Id))) continue;
        return clue;
    }
    return null;
}

/// <summary>
/// Non-mutating peek: finds the next revealable public warrant matching the source kind,
/// without removing it from the public pool or adding it to known warrants.
/// </summary>
public Warrant? PeekNextPublicWarrant(InvestigationSourceKind? sourceKind = null)
{
    foreach (var warrant in _publicWarrants)
    {
        if (sourceKind.HasValue && warrant.Terms.SourceKind != sourceKind) continue;
        if (_knownWarrants.Any(existing => existing.Id.Equals(warrant.Id))) continue;
        return warrant;
    }
    return null;
}

/// <summary>
/// Mutating reveal: removes the clue from the public pool and discovers it.
/// Also cleans stale already-known entries from the public pool, preserving
/// the behavior of the old RevealNextPublicClue scan path.
/// Called by Apply(InvestigationPerformed) — the single mutation path.
/// </summary>
public void RevealClue(Clue clue)
{
    ArgumentNullException.ThrowIfNull(clue);

    // Clean stale already-known entries from public pool (same as old reveal path)
    for (var i = _publicClues.Count - 1; i >= 0; i--)
    {
        if (_knownClues.Any(existing => existing.Id.Equals(_publicClues[i].Id)))
        {
            _publicClues.RemoveAt(i);
        }
    }

    // Remove the revealed clue from the public pool
    var index = _publicClues.FindIndex(c => c.Id.Equals(clue.Id));
    if (index >= 0)
    {
        _publicClues.RemoveAt(index);
    }

    // Discover it (idempotent — DiscoverClue checks for duplicates)
    DiscoverClue(clue);
}

/// <summary>
/// Mutating reveal: removes the warrant from the public pool and discovers it.
/// Also cleans stale already-known entries from the public pool.
/// Called by Apply(InvestigationPerformed) — the single mutation path.
/// </summary>
public void RevealWarrant(Warrant warrant)
{
    ArgumentNullException.ThrowIfNull(warrant);

    // Clean stale already-known entries from public pool
    for (var i = _publicWarrants.Count - 1; i >= 0; i--)
    {
        if (_knownWarrants.Any(existing => existing.Id.Equals(_publicWarrants[i].Id)))
        {
            _publicWarrants.RemoveAt(i);
        }
    }

    // Remove the revealed warrant from the public pool
    var index = _publicWarrants.FindIndex(w => w.Id.Equals(warrant.Id));
    if (index >= 0)
    {
        _publicWarrants.RemoveAt(index);
    }

    // Discover it (idempotent — DiscoverWarrant checks for duplicates)
    DiscoverWarrant(warrant);
}
```

**Behavior preservation note:** The old `RevealNextPublicClue` scans `_publicClues`, removes stale known entries, removes the matching clue, and calls `DiscoverClue`. `RevealClue` does the same cleanup + removal + discovery, just with the clue already chosen by the peek step. The end state (known/public clue lists) must be identical.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "CaseFilePeekTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/WildBunch.Domain/Cases/CaseFile.cs tests/WildBunch.Domain.Tests/CaseFilePeekTests.cs
git commit -m "BUNCH-78: add peek and reveal methods to CaseFile for event-sourced investigation"
```

---

## Task 2: Create InvestigationPerformed event

**Files:**
- Create: `src/WildBunch.Domain/Events/InvestigationPerformed.cs`

- [ ] **Step 1: Create the event file**

```csharp
using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: an investigation source was checked, possibly revealing a public clue and/or warrant.
/// Carries only public data — hidden culprit truth is never in this event.
/// See ADR-0028.
/// </summary>
public sealed record InvestigationPerformed : IDomainEvent
{
    public required InvestigationSourceKind SourceKind { get; init; }
    public required TownId TownId { get; init; }
    public required string Message { get; init; }
    public Clue? Clue { get; init; }
    public Warrant? Warrant { get; init; }
    public bool AdvanceClock { get; init; } = true;
}
```

- [ ] **Step 2: Run build to verify it compiles**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 3: Commit**

```powershell
git add src/WildBunch.Domain/Events/InvestigationPerformed.cs
git commit -m "BUNCH-78: add InvestigationPerformed typed domain event"
```

---

## Task 3: Add Apply(InvestigationPerformed) and refactor investigation methods

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`

This is the core domain refactoring task. It adds the `Apply` method and refactors 5 investigation methods to the event-sourced pattern.

**Interfaces:**
- Produces: `Apply(InvestigationPerformed)` mutation method; 5 investigation methods produce events instead of calling `RecordCaseUpdate`.

- [ ] **Step 1: Write characterization tests first**

Create `tests/WildBunch.Domain.Tests/InvestigationEventSourcingTests.cs`:

```csharp
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;

namespace WildBunch.Domain.Tests;

public sealed class InvestigationEventSourcingTests
{
    [Fact]
    public void GatherLocalGossipProducesInvestigationPerformedEvent()
    {
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.LocalGossip, "A dusty boot print.");

        var result = session.GatherLocalGossip();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Single(session.UncommittedEvents);
        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents.Single());
        Assert.Equal(InvestigationSourceKind.LocalGossip, e.SourceKind);
        Assert.NotNull(e.Clue);
    }

    [Fact]
    public void GatherLocalGossipNoNewInfoProducesEventWithoutClue()
    {
        var session = TestSessionFactory.CreateWithSpentSource(InvestigationSourceKind.LocalGossip);

        var result = session.GatherLocalGossip();

        Assert.True(result.Success);
        Assert.Single(session.UncommittedEvents);
        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents.Single());
        Assert.Null(e.Clue);
        Assert.Null(e.Warrant);
    }

    [Fact]
    public void ReadWantedPostersProducesEventWithWarrantAndClue()
    {
        var session = TestSessionFactory.CreateWithPublicWarrantAndClue(
            InvestigationSourceKind.SheriffWarrants);

        var result = session.ReadWantedPosters();

        Assert.True(result.Success);
        Assert.Single(session.UncommittedEvents);
        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents.Single());
        Assert.NotNull(e.Warrant);
        Assert.NotNull(e.Clue);
    }

    [Fact]
    public void InvestigationFailedDoesNotProduceEvent()
    {
        var session = TestSessionFactory.CreateWithActiveJourney();

        var result = session.GatherLocalGossip();

        Assert.False(result.Success);
        Assert.Empty(session.UncommittedEvents);
    }

    [Fact]
    public void GatherLocalGossipAdvancesClockAndDiscoversClueViaApply()
    {
        // Prove Apply mutation path works through the public command method.
        // No test hook — the public method produces the event and applies it.
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.LocalGossip, "A dusty boot print.");
        var clue = session.CaseFile.PeekNextPublicClue(_ => true)!;
        var clockBefore = session.Clock.Day;

        var result = session.GatherLocalGossip();

        Assert.True(result.Success);
        Assert.Contains(clue, session.CaseFile.KnownClues);
        Assert.DoesNotContain(clue, session.CaseFile.PublicClues);
        Assert.True(session.Clock.Day > clockBefore);
    }
}
```

Note: `TestSessionFactory` is a test helper that creates sessions with specific clue/warrant/source setups. No `Apply` test hook is needed — Apply behavior is proven through public command methods (this test), replay tests with fresh baseline CaseFile (Task 4), and projector tests (Task 5).

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "InvestigationEventSourcingTests"`
Expected: FAIL — methods don't produce events yet.

- [ ] **Step 3: Add Apply(InvestigationPerformed) method**

In `GameSession.cs`, add after the existing `Apply(StoreItemPurchased)` method (around line 261):

```csharp
private void Apply(InvestigationPerformed e)
{
    // Mark the source as checked (mutating — uses CurrentTown delegation)
    if (e.SourceKind == InvestigationSourceKind.SheriffWarrants)
    {
        CurrentTown.CheckWantedPosters();
    }
    else
    {
        CurrentTown.CheckSource(e.SourceKind);
    }

    // Advance the game clock
    if (e.AdvanceClock)
    {
        Clock.Advance();
    }

    // Reveal clue if present (RevealClue removes from public pool + discovers + cleans stale)
    if (e.Clue is not null)
    {
        CaseFile.RevealClue(e.Clue);
    }

    // Reveal warrant if present (RevealWarrant removes from public pool + discovers + cleans stale)
    if (e.Warrant is not null)
    {
        CaseFile.RevealWarrant(e.Warrant);
    }

    // Legacy log entry (transitional per ADR-0028 — will be removed in deprecation follow-up)
    AddLogEntry(GameLogEntryKind.CaseUpdate, e.Message);

    _version++;
}
```

**No test hook.** The `Apply` method is private, called through the public command methods and through `ApplyEvent` in replay. Tests prove behavior through public command methods, replay tests (Task 4), and projector tests (Task 5).

- [ ] **Step 4: Refactor GatherLocalGossip to event-sourced pattern**

Replace the current `GatherLocalGossip()` method (lines 1769-1794) with:

```csharp
public CaseInvestigationResult GatherLocalGossip()
{
    if (IsJourneyModal())
        return CaseInvestigationResult.Failed(JourneyModalBlockMessage);

    var sourceDefinition = CurrentTown.GetRequiredSourceDefinition(InvestigationSourceKind.LocalGossip);
    if (!CurrentTown.IsAvailable(InvestigationSourceKind.LocalGossip))
        return CaseInvestigationResult.Failed("There is no one to ask here.");

    // Peek: is the source already spent? (non-mutating — uses CurrentTownVisit, not CurrentTown)
    if (CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalGossip))
    {
        var msg = "You ask around again, but hear nothing new.";
        var e = new InvestigationPerformed
        {
            SourceKind = InvestigationSourceKind.LocalGossip,
            TownId = CurrentTown.TownId,
            Message = msg
        };
        Apply(e);
        _uncommittedEvents.Add(e);
        return CaseInvestigationResult.Succeeded(msg, sessionChanged: true);
    }

    // Peek: is there a revealable clue? (non-mutating)
    var clue = CaseFile.PeekNextPublicClue(c => IsPlayerKnownClue(c) && c.SourceKind == InvestigationSourceKind.LocalGossip);

    if (clue is null)
    {
        var msg = "You ask around for local gossip, but hear nothing new.";
        var e = new InvestigationPerformed
        {
            SourceKind = InvestigationSourceKind.LocalGossip,
            TownId = CurrentTown.TownId,
            Message = msg
        };
        Apply(e);
        _uncommittedEvents.Add(e);
        return CaseInvestigationResult.Succeeded(msg, sessionChanged: true);
    }

    var foundMsg = $"You ask around for local gossip and uncover a public lead: {DescribeClueLead(clue.Description)}.";
    var foundEvent = new InvestigationPerformed
    {
        SourceKind = InvestigationSourceKind.LocalGossip,
        TownId = CurrentTown.TownId,
        Message = foundMsg,
        Clue = clue
    };
    Apply(foundEvent);
    _uncommittedEvents.Add(foundEvent);
    return CaseInvestigationResult.Succeeded("You ask around for local gossip and uncover a public lead.", sessionChanged: true);
}
```

- [ ] **Step 5: Refactor the other 4 investigation methods**

Apply the same pattern to `FollowTelegraphLeads`, `InspectNoticeBoard`, `CheckSheriffRecords`, and `ReadWantedPosters`. Each method:

1. Keeps its existing guards (journey modal, source availability).
2. Peeks using `CurrentTownVisit.IsSpent(...)` (or `CurrentTownVisit.WantedPostersSpent` for `ReadWantedPosters`) instead of the mutating `CurrentTown.CheckSource(...)`/`CurrentTown.CheckWantedPosters()`. **Important:** `IsSpent` and `WantedPostersSpent` live on `CurrentTownVisit` (`TownVisitState`), NOT on `CurrentTown` (`TownAggregate`).
3. Peeks using `CaseFile.PeekNextPublicClue(...)`/`CaseFile.PeekNextPublicWarrant(...)` instead of the mutating `RevealNextPublicClue`/`RevealNextPublicWarrant`.
4. Produces an `InvestigationPerformed` event with the appropriate `Clue`/`Warrant`/`Message`.
5. Calls `Apply(e)` + `_uncommittedEvents.Add(e)`. The `Apply` method handles source marking (via `CurrentTown.CheckSource`/`CheckWantedPosters`), clock advance, and `CaseFile.RevealClue`/`RevealWarrant`.
6. Returns the same `CaseInvestigationResult`/`ReadWantedPostersResult` as before.

For `ReadWantedPosters`, the event can carry both `Clue` and `Warrant` when both are found (line 1654-1657 case). The `Apply` method reveals both.

**Key behavior preservation:** The return values (`Success`, `Message`, `SessionChanged`) must be identical to the current implementation. The `SessionChanged: true` flag is always set when an event is produced (which is every success case). The end state of `CaseFile.KnownClues`/`PublicClues`/`KnownWarrants`/`PublicWarrants` must be identical to what the old `RevealNextPublicClue`/`RevealNextPublicWarrant` path produced.

**Key behavior preservation:** The return values (`Success`, `Message`, `SessionChanged`) must be identical to the current implementation. The `SessionChanged: true` flag is always set when an event is produced (which is every success case).

- [ ] **Step 6: Run characterization tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "InvestigationEventSourcingTests"`
Expected: PASS.

- [ ] **Step 7: Run existing investigation handler tests to verify no regression**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "InvestigationSourceHandlerTests|ReadWantedPostersHandlerTests|CheckSheriffRecordsHandlerTests|InspectNoticeBoardHandlerTests"`
Expected: PASS — the domain methods still return the same results; only the internal mutation path changed.

- [ ] **Step 8: Commit**

```powershell
git add src/WildBunch.Domain/Game/GameSession.cs tests/WildBunch.Domain.Tests/InvestigationEventSourcingTests.cs
git commit -m "BUNCH-78: migrate investigation methods to typed events + Apply mutation"
```

---

## Task 4: Update GameSessionEventReplay dispatcher

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs:87-100`

- [ ] **Step 1: Write the failing test**

Add to `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs`:

```csharp
[Fact]
public async Task ReplayFromEvents_ReconstructsInvestigationState()
{
    // Start a session, perform an investigation, store it, then replay from events only.
    // CRITICAL: Replay into a FRESH baseline CaseFile (not the already-mutated session.CaseFile).
    // If we replay into session.CaseFile, the clue is already known there and the test
    // passes trivially without proving the event replay discovers it.
    var session = TestSessionFactory.CreateWithPublicClue(
        InvestigationSourceKind.LocalGossip, "A dusty boot print.");
    var freshBaselineCaseFile = TestSessionFactory.CreateBaselineCaseFileFor(session);
    await _repository.StoreAsync(session, cancellationToken: CancellationToken.None);
    await _unitOfWork.CommitAsync(CancellationToken.None);
    session.MarkEventsCommitted();

    // Perform investigation (produces InvestigationPerformed event)
    session.GatherLocalGossip();
    await _repository.StoreAsync(session, cancellationToken: CancellationToken.None);
    await _unitOfWork.CommitAsync(CancellationToken.None);
    session.MarkEventsCommitted();

    // Replay from events into the FRESH baseline CaseFile
    var events = await _repository.GetEventStreamAsync(session.Id, 0, CancellationToken.None);
    var replayed = GameSession.RehydrateFromEvents(session.Id, session.World, freshBaselineCaseFile, events);

    // The replayed session must have discovered the clue from the event, not from the baseline.
    Assert.Equal(session.CaseFile.KnownClues.Count, replayed.CaseFile.KnownClues.Count);
    Assert.Equal(session.CaseFile.PublicClues.Count, replayed.CaseFile.PublicClues.Count);
    // Verify the specific clue was discovered
    var revealedClueId = events.OfType<InvestigationPerformed>().Single().Clue!.Id;
    Assert.Contains(replayed.CaseFile.KnownClues, c => c.Id.Equals(revealedClueId));
    Assert.DoesNotContain(replayed.CaseFile.PublicClues, c => c.Id.Equals(revealedClueId));
}
```

**Why fresh baseline:** `RehydrateFromEvents` takes a `CaseFile` parameter as the external reference (world/case template not stored in events). If we pass the already-mutated `session.CaseFile`, the clue is already in `KnownClues` and absent from `PublicClues` — the test passes trivially without proving the event replay path works. The fresh baseline `CaseFile` starts with the clue in `PublicClues` and NOT in `KnownClues`, so the test only passes if `Apply(InvestigationPerformed)` correctly reveals it. `TestSessionFactory.CreateBaselineCaseFileFor(session)` creates a fresh `CaseFile` from the same template used to create the session, before any investigation mutations.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Integration.Tests --filter "ReplayFromEvents_ReconstructsInvestigationState"`
Expected: FAIL — `ApplyEvent` throws `InvalidOperationException` for `InvestigationPerformed`.

- [ ] **Step 3: Add InvestigationPerformed to the dispatcher**

In `GameSessionEventReplay.cs`, add to the `ApplyEvent` switch:

```csharp
case InvestigationPerformed ip:
    session.Apply(ip);
    break;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Integration.Tests --filter "ReplayFromEvents_ReconstructsInvestigationState"`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/WildBunch.Domain/Game/GameSessionEventReplay.cs tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs
git commit -m "BUNCH-78: add InvestigationPerformed to event replay dispatcher"
```

---

## Task 5: Update DiaryProjector for InvestigationPerformed

**Files:**
- Modify: `src/WildBunch.Application/Projections/DiaryProjector.cs`

- [ ] **Step 1: Write the failing test**

Add to `tests/WildBunch.Application.Tests` (or a new `DiaryProjectorTests.cs`):

```csharp
[Fact]
public void DiaryProjectorHandlesInvestigationPerformed()
{
    var events = new IDomainEvent[]
    {
        new GameStarted { /* ... minimal setup ... */ },
        new InvestigationPerformed
        {
            SourceKind = InvestigationSourceKind.LocalGossip,
            TownId = new TownId("dustvale"),
            Message = "You uncover a public lead: a dusty boot print."
        }
    };
    var projector = new DiaryProjector();

    var projection = projector.Project(events);

    Assert.Contains(projection.Entries, e => e.Summary.Contains("public lead"));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "DiaryProjectorHandlesInvestigationPerformed"`
Expected: FAIL — `DiaryProjector` doesn't handle `InvestigationPerformed`.

- [ ] **Step 3: Add InvestigationPerformed handling to DiaryProjector**

In `DiaryProjector.cs`, add to the switch:

```csharp
case InvestigationPerformed ip:
    turn++;
    entries.Add(new DiaryEntry(day, turn, ip.Message));
    break;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "DiaryProjectorHandlesInvestigationPerformed"`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/WildBunch.Application/Projections/DiaryProjector.cs tests/WildBunch.Application.Tests/DiaryProjectorTests.cs
git commit -m "BUNCH-78: handle InvestigationPerformed in DiaryProjector"
```

---

## Task 6: Move 5 investigation handlers to GameSessionCommandHandler

**Files:**
- Modify: `GatherLocalGossipHandler.cs`, `FollowTelegraphLeadsHandler.cs`, `InspectNoticeBoardHandler.cs`, `CheckSheriffRecordsHandler.cs`, `ReadWantedPostersHandler.cs`

Now that the domain methods produce events, the handlers can use `ExecuteWithRetryAsync` (which gates on `UncommittedEvents.Count > 0`).

- [ ] **Step 1: Write a failing test for orchestration**

Add to `tests/WildBunch.Application.Tests/InvestigationSourceHandlerTests.cs`:

```csharp
[Fact]
public async Task GatherLocalGossipUsesOrchestrationAndProducesEvents()
{
    var repository = new InMemoryGameSessionRepository();
    var session = TestSessionFactory.CreateWithPublicClue(/* ... */);
    session.MarkEventsCommitted();
    repository.Seed(session);
    var handler = new GatherLocalGossipHandler(repository, repository, new JournalResolver());

    var result = await handler.HandleAsync(new GatherLocalGossipCommand(session.Id.Value));

    Assert.True(result.Success);
    Assert.Equal(1, repository.StoreCalls);
    Assert.Equal(1, repository.CommitCalls);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "GatherLocalGossipUsesOrchestration"`
Expected: FAIL — handler doesn't use `ExecuteWithRetryAsync`.

- [ ] **Step 3: Refactor GatherLocalGossipHandler**

```csharp
public sealed class GatherLocalGossipHandler : GameSessionCommandHandler
{
    private readonly JournalResolver _journalResolver;

    public GatherLocalGossipHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        JournalResolver journalResolver)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _journalResolver = journalResolver;
    }

    public async Task<InvestigationActionResultDto> HandleAsync(
        GatherLocalGossipCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var actionResult = session.GatherLocalGossip();
            return new InvestigationActionResultDto(
                actionResult.Success,
                actionResult.Message,
                JournalMapper.ToDto(_journalResolver.Resolve(session)));
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 4: Refactor the other 4 handlers**

Apply the same pattern to `FollowTelegraphLeadsHandler`, `InspectNoticeBoardHandler`, `CheckSheriffRecordsHandler`, and `ReadWantedPostersHandler`. Each handler:
- Extends `GameSessionCommandHandler`
- Passes `gameSessionRepository` and `gameSessionUnitOfWork` to the base constructor
- Uses `ExecuteWithRetryAsync` instead of manual load/store/commit
- Keeps its existing return type and DTO construction

For `ReadWantedPostersHandler`, the return type is `WantedPostersResultDto` (not `InvestigationActionResultDto`). The pattern is the same — only the return DTO construction differs.

- [ ] **Step 5: Run all investigation handler tests**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "InvestigationSourceHandlerTests|ReadWantedPostersHandlerTests|CheckSheriffRecordsHandlerTests|InspectNoticeBoardHandlerTests"`
Expected: PASS — update constructor calls in existing tests if needed (the handler constructors now take the same dependencies but in the base class pattern).

- [ ] **Step 6: Commit**

```powershell
git add src/WildBunch.Application/Games/Commands/GatherLocalGossipHandler.cs src/WildBunch.Application/Games/Commands/FollowTelegraphLeadsHandler.cs src/WildBunch.Application/Games/Commands/InspectNoticeBoardHandler.cs src/WildBunch.Application/Games/Commands/CheckSheriffRecordsHandler.cs src/WildBunch.Application/Games/Commands/ReadWantedPostersHandler.cs tests/WildBunch.Application.Tests/InvestigationSourceHandlerTests.cs
git commit -m "BUNCH-78: move 5 investigation handlers to GameSessionCommandHandler orchestration"
```

---

## Task 7: Update AddLogEntry guardrail count

**Files:**
- Modify: `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs`

The 5 migrated methods no longer call `RecordCaseUpdate` (which calls `AddLogEntry`). Instead, `Apply(InvestigationPerformed)` calls `AddLogEntry` once. The net change in call sites:

- `RecordCaseUpdate` still exists (called by `LookAroundSaloon` which is not migrated) — its `AddLogEntry` at line 1857 remains.
- New `AddLogEntry` call in `Apply(InvestigationPerformed)` — 1 new call site.
- The 5 migrated methods no longer call `RecordCaseUpdate`, but `RecordCaseUpdate`'s `AddLogEntry` call site still exists (it's inside the method, not at each call site).

So the count goes from 18 to 19 (one new call in `Apply(InvestigationPerformed)`).

- [ ] **Step 1: Update the guardrail constant**

```csharp
// Updated after Phase 2 investigation migration:
// - Apply(InvestigationPerformed) adds 1 new transitional AddLogEntry call site
// - RecordCaseUpdate's AddLogEntry remains (still called by LookAroundSaloon)
private const int KnownLegacyAddLogEntryCallSiteCount = 19;
```

- [ ] **Step 2: Run guardrail test**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "AddLogEntryGuardrailTests"`
Expected: PASS — count is now 19.

- [ ] **Step 3: Commit**

```powershell
git add tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs
git commit -m "BUNCH-78: update AddLogEntry guardrail count after investigation migration"
```

---

## Task 8: Full validation

- [ ] **Step 1: Run dotnet build**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 2: Run dotnet test (Application + Domain)**

Run: `dotnet test tests/WildBunch.Application.Tests tests/WildBunch.Domain.Tests`
Expected: PASS.

- [ ] **Step 3: Run PostgreSQL-backed validation**

Run: `.\scripts\postgres-dev.ps1 validate`
Expected: PASS — integration tests (GameApiTests, EventStorePersistenceTests, ProjectionEndpointTests) pass with investigation events in the stream.

- [ ] **Step 4: Verify clean worktree**

Run: `git status --short`
Expected: clean.

- [ ] **Step 5: Report return evidence**

Report branch, PR URL, starting main SHA, final head SHA, changed files grouped by layer, tests added/changed, validation results, architecture-gap table, follow-up issue IDs, cleanup proof.

---

## Architecture-gap table (Phase 2)

| Gap | Action taken | Remaining follow-up |
|-----|-------------|---------------------|
| 5 investigation methods use direct mutation + AddLogEntry | Migrated to `InvestigationPerformed` event + `Apply` + transitional `AddLogEntry` in `Apply` | Remove transitional `AddLogEntry` from `Apply` (deprecation follow-up) |
| 5 investigation handlers manually load/store/commit | Moved to `GameSessionCommandHandler.ExecuteWithRetryAsync` | — |
| `CaseFile.RevealNextPublicClue` interleaves decide and mutate | Added `PeekNextPublicClue`/`PeekNextPublicWarrant` non-mutating methods | — |
| `DiaryProjector` doesn't handle investigation events | Added `InvestigationPerformed` case | — |
| `GameSessionEventReplay` doesn't handle investigation events | Added `InvestigationPerformed` to dispatcher | — |
| `LookAroundSaloon` still uses direct mutation | Deferred — citizen edge case + BountyLoopCoordinator coupling | Bounty-loop follow-up |
| Travel/encounter flows still use direct mutation | Deferred — too broad for this PR | Travel/journey follow-up issue |

---

## Non-goals for Phase 2

1. `LookAroundSaloon` migration — citizen edge case (no clock advance, no `RecordCaseUpdate`), coupled to `BountyLoopCoordinator`.
2. Bounty-loop handler migration (`TurnInToSheriff`, `ConfrontWantedSuspect`, etc.) — separate seam.
3. Travel/encounter event migration — too broad, separate follow-up.
4. Removing `AddLogEntry` from `Apply(InvestigationPerformed)` — transitional, deferred to deprecation follow-up.
5. Adding `InvestigationPerformed` to `HudProjector` — HUD projection doesn't track clues/warrants; diary projection is the player-facing output for investigation.
6. Frontend changes — Phase 2 doesn't change frontend types (investigation handlers already return projection-safe DTOs via `JournalMapper`).
