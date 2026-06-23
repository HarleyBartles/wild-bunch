# BUNCH-80 Phase 3: Projections + Persistence + Handlers + Tests

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire the 5 new events (4 bounty/saloon gameplay events + `TownActionContextEntered`) into projections, persistence deserialization, handler orchestration, and the full validation/test suite. Update ADR-0028 and guardrail tests. Update DTOs and frontend for `TimeOfDay` display.

**Architecture:** Projections are pure functions over the event stream. Persistence deserialization maps event-type strings to CLR types. Handlers migrate from manual load/store/commit to `GameSessionCommandHandler` base class orchestration. ADR-0028 is updated to reflect implemented state.

**Tech Stack:** C#/.NET 10, xUnit, PostgreSQL (via `postgres-dev.ps1`), EF Core

## Global Constraints

- Projections must not expose hidden culprit truth (`TrueCulpritId`, `LinkedSuspectIds`, `TargetKind`)
- Persistence deserialization must map every new event type or `GetEventStreamAsync` throws
- Handlers must use `ExecuteWithRetryAsync` (which gates on `UncommittedEvents.Count > 0`)
- `LogEntries` stays in DTOs for backward compatibility — projections are additive
- No frontend changes expected (projections are additive; LogEntries remains)
- Follow exact patterns from BUNCH-78 Phase 1/Phase 2

---

## Task 6: DiaryProjector — Bounty/Saloon Event Cases

**Files:**
- Modify: `src/WildBunch.Application/Projections/DiaryProjector.cs:26-48` (switch statement)
- Test: `tests/WildBunch.Application.Tests/DiaryProjectorTests.cs` (or create if not exists)

**Interfaces:**
- Consumes: `SaloonPersonOfInterestSpotted`, `WantedSuspectConfronted`, `SheriffTurnInSettled`, `SaloonPersonOfInterestConfronted` (from Phase 1)

- [ ] **Step 1: Write failing projection tests**

```csharp
[Fact]
public void Project_SaloonPersonOfInterestSpotted_AppendsDiaryEntry()
{
    var events = new IDomainEvent[]
    {
        TestEvents.GameStarted(),
        TestEvents.SaloonPersonOfInterestSpotted("You spot a shady figure.")
    };
    var projection = new DiaryProjector().Project(events);

    var entry = projection.Entries.Last();
    Assert.Contains("shady figure", entry.Message);
}

[Fact]
public void Project_WantedSuspectConfronted_AppendsDiaryEntry()
{
    var events = new IDomainEvent[]
    {
        TestEvents.GameStarted(),
        TestEvents.WantedSuspectConfronted("You confront Cole Tanner. He surrenders.")
    };
    var projection = new DiaryProjector().Project(events);

    var entry = projection.Entries.Last();
    Assert.Contains("Cole Tanner", entry.Message);
}

[Fact]
public void Project_SheriffTurnInSettled_AppendsDiaryEntry()
{
    var events = new IDomainEvent[]
    {
        TestEvents.GameStarted(),
        TestEvents.SheriffTurnInSettled("The sheriff pays you $50.00.")
    };
    var projection = new DiaryProjector().Project(events);

    var entry = projection.Entries.Last();
    Assert.Contains("sheriff pays", entry.Message);
}

[Fact]
public void Project_SaloonPersonOfInterestConfronted_DoesNotAppendDiaryEntry()
{
    var events = new IDomainEvent[]
    {
        TestEvents.GameStarted(),
        TestEvents.SaloonPersonOfInterestConfronted("Wrong declaration.")
    };
    var projection = new DiaryProjector().Project(events);

    // SaloonPersonOfInterestConfronted never produces a diary entry —
    // log entries come from delegated WantedSuspectConfronted/SheriffTurnInSettled events
    Assert.DoesNotContain(projection.Entries, e => e.Message.Contains("Wrong declaration"));
}
```

Note: Check if `TestEvents` helper exists in the Application test project. If not, create a small helper or construct events inline. Check `tests/WildBunch.Application.Tests/` for existing projection test patterns.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "DiaryProjector"`
Expected: FAIL — events not handled in projector switch

- [ ] **Step 3: Add 4 cases to DiaryProjector switch**

In `DiaryProjector.cs:26-48`, the projector must track time from `TownActionContextEntered` events (and `GameStarted`), NOT from a local `turn++` counter. Add the context event case and update existing cases:

```csharp
// Track time from events, not from a local counter
int day = 1, turn = 0;

foreach (var e in events)
{
    switch (e)
    {
        case GameStarted gs:
            day = 1; turn = 0;
            entries.Add(new DiaryEntry(day, turn, gs.Message));
            break;

        case TownActionContextEntered tc:
            // Update tracked time from the context event — this is the
            // event-sourced clock state, not a local counter
            day = tc.Day;
            turn = tc.Turn;
            break;

        case StoreItemPurchased sp:
            entries.Add(new DiaryEntry(day, turn, sp.Message));
            break;

        case InvestigationPerformed ip:
            entries.Add(new DiaryEntry(day, turn, ip.Message));
            break;

        case SaloonPersonOfInterestSpotted sp:
            if (sp.RecordLog)
                entries.Add(new DiaryEntry(day, turn, sp.Message));
            break;

        case WantedSuspectConfronted wc:
            entries.Add(new DiaryEntry(day, turn, wc.Message));
            break;

        case SheriffTurnInSettled st:
            entries.Add(new DiaryEntry(day, turn, st.Message));
            break;

        case SaloonPersonOfInterestConfronted sc:
            // No diary entry from this event — log entries come from delegated
            // WantedSuspectConfronted/SheriffTurnInSettled events.
            break;
    }
}
```

**Key change:** The projector no longer increments a local `turn++` counter. It tracks `day`/`turn` from `TownActionContextEntered` events (the event-sourced clock state). Diary entries use the tracked time. This ensures projections derive time from the event stream, not from invented local state.

Note: `SaloonPersonOfInterestSpotted` has a `RecordLog` flag (controls whether a diary entry is added). `SaloonPersonOfInterestConfronted` never produces a diary entry (log entries come from delegated `WantedSuspectConfronted`/`SheriffTurnInSettled` events). `WantedSuspectConfronted` and `SheriffTurnInSettled` always produce diary entries (they always call `RecordCaseUpdate` in their Apply methods). No event carries `AdvanceClock` — clock advancement is handled by `EnterActionContext` in the domain methods, not by events or projections.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "DiaryProjector"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Projections/DiaryProjector.cs tests/WildBunch.Application.Tests/DiaryProjectorTests.cs
git commit -m "BUNCH-80: add bounty/saloon event cases to DiaryProjector"
```

---

## Task 7: HudProjector — Wallet Changes from Bounty/Saloon Events

**Files:**
- Modify: `src/WildBunch.Application/Projections/HudProjector.cs:29-53` (switch statement)
- Test: `tests/WildBunch.Application.Tests/HudProjectorTests.cs` (or create if not exists)

**Interfaces:**
- Consumes: `SheriffTurnInSettled` (bounty added to wallet), `SaloonPersonOfInterestConfronted` (fine subtracted from wallet)

The HudProjector currently tracks `walletCash` from `GameStarted.StartingWallet` and `StoreItemPurchased.WalletAfter`. For bounty/saloon, the wallet changes come from:
- `SheriffTurnInSettled.BountyAmount` — ADD to wallet
- `SaloonPersonOfInterestConfronted.FineAmount` + `WalletBefore`/`WalletAfter` — SET wallet to `WalletAfter` if present

- [ ] **Step 1: Write failing projection tests**

```csharp
[Fact]
public void Project_SheriffTurnInSettled_AddsBountyToWallet()
{
    var events = new IDomainEvent[]
    {
        TestEvents.GameStarted(startingWallet: 10m),
        TestEvents.SheriffTurnInSettled(bountyAmount: 50m)
    };
    var projection = new HudProjector().Project(events);

    Assert.Equal(60m, projection.WalletCash);
}

[Fact]
public void Project_SaloonPersonOfInterestConfronted_WithFine_SetsWalletAfter()
{
    var events = new IDomainEvent[]
    {
        TestEvents.GameStarted(startingWallet: 100m),
        TestEvents.SaloonPersonOfInterestConfronted(fineAmount: 25m, walletBefore: 100m, walletAfter: 75m)
    };
    var projection = new HudProjector().Project(events);

    Assert.Equal(75m, projection.WalletCash);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "HudProjector"`
Expected: FAIL — events not handled

- [ ] **Step 3: Add cases to HudProjector switch**

In `HudProjector.cs:29-53`, add after the `StoreItemPurchased` case:

```csharp
case SheriffTurnInSettled st:
    walletCash += st.BountyAmount;
    break;

case SaloonPersonOfInterestConfronted sc:
    if (sc.WalletAfter is { } walletAfter)
    {
        walletCash = walletAfter;
    }
    else if (sc.FineAmount is { } fine)
    {
        walletCash -= fine;
    }
    break;
```

Note: Prefer `WalletAfter` when available (it's the exact post-mutation value). Fall back to subtracting `FineAmount` if `WalletAfter` is not set (defensive — should always be set when `FineAmount` is set).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "HudProjector"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Projections/HudProjector.cs tests/WildBunch.Application.Tests/HudProjectorTests.cs
git commit -m "BUNCH-80: add bounty/saloon wallet changes to HudProjector"
```

---

## Task 8: CaseFileViewProjector — Confrontation and Settlement State

**Files:**
- Modify: `src/WildBunch.Application/Projections/CaseFileViewProjector.cs:35-40` (event loop)
- Test: `tests/WildBunch.Application.Tests/CaseFileViewProjectorTests.cs` (or create if not exists)

**Interfaces:**
- Consumes: `WantedSuspectConfronted`, `SheriffTurnInSettled`, `InvestigationPerformed` (clue/warrant reveals)

The `CaseFileViewProjector` currently starts from a seed `CaseFile` and applies no event mutations (the event loop is a no-op placeholder at line 35-40). For bounty/saloon, it needs to:
- Track discovered confrontation states from `WantedSuspectConfronted`
- Track settlement states from `SheriffTurnInSettled`
- Track revealed clues/warrants from `InvestigationPerformed`

This projector is developer-facing (not exposed to player API), but it should still respect hidden-truth boundaries — it exposes only public confrontation/settlement data, never `TrueCulpritId` or `TargetKind`.

- [ ] **Step 1: Write failing projection tests**

```csharp
[Fact]
public void Project_WantedSuspectConfronted_AddsConfrontationToProjection()
{
    var seedCaseFile = TestCaseFileFactory.CreateWithWarrantedSuspect(out var suspectId);
    var events = new IDomainEvent[]
    {
        TestEvents.GameStarted(),
        TestEvents.WantedSuspectConfronted(targetSuspectId: suspectId, outcome: WantedSuspectConfrontationOutcome.Surrendered)
    };
    var projection = new CaseFileViewProjector().Project(Guid.NewGuid(), seedCaseFile, events);

    Assert.Contains(projection.Confrontations, c => c.TargetSuspectId == suspectId);
}

[Fact]
public void Project_SheriffTurnInSettled_AddsSettlementToProjection()
{
    var seedCaseFile = TestCaseFileFactory.CreateWithWarrantedSuspect(out var suspectId);
    var events = new IDomainEvent[]
    {
        TestEvents.GameStarted(),
        TestEvents.WantedSuspectConfronted(targetSuspectId: suspectId, outcome: WantedSuspectConfrontationOutcome.Surrendered),
        TestEvents.SheriffTurnInSettled(targetSuspectId: suspectId, bountyAmount: 50m)
    };
    var projection = new CaseFileViewProjector().Project(Guid.NewGuid(), seedCaseFile, events);

    Assert.Contains(projection.Settlements, s => s.TargetSuspectId == suspectId);
    Assert.Equal(50m, projection.Settlements.Single(s => s.TargetSuspectId == suspectId).BountyAmount);
}
```

Note: Check if `CaseFileViewProjection` has `Confrontations` and `Settlements` collections. If not, they need to be added to the projection type. Check `src/WildBunch.Application/Projections/CaseFileViewProjection.cs` (or wherever the projection record is defined). If the projection type doesn't have these fields, add them as part of this task.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "CaseFileViewProjector"`
Expected: FAIL — events not handled

- [ ] **Step 3: Add confrontation/settlement tracking to CaseFileViewProjector**

Replace the no-op event loop (lines 35-40) with:

```csharp
var confrontations = new List<WantedSuspectConfrontationState>();
var settlements = new List<SheriffTurnInSettlementState>();
var revealedClueIds = new HashSet<ClueId>();
var revealedWarrantIds = new HashSet<WarrantId>();

foreach (var e in events)
{
    switch (e)
    {
        case InvestigationPerformed ip:
            if (ip.ClueId is { } clueId) revealedClueIds.Add(clueId);
            if (ip.WarrantId is { } warrantId) revealedWarrantIds.Add(warrantId);
            break;

        case WantedSuspectConfronted wc:
            if (wc.Outcome is not WantedSuspectConfrontationOutcome.Abandoned)
            {
                confrontations.Add(new WantedSuspectConfrontationState(
                    wc.TargetSuspectId, wc.TargetName, wc.Disposition,
                    wc.Outcome, wc.IsAlive, wc.IsSecured, 0, 0));
            }
            break;

        case SheriffTurnInSettled st:
            settlements.Add(new SheriffTurnInSettlementState(
                st.TargetSuspectId, st.TargetName, st.Disposition,
                st.IsAlive, st.BountyAmount, st.Day, st.Turn));
            break;
    }
}

// Filter seed clues/warrants by revealed IDs
knownClues = seedCaseFile.KnownClues
    .Where(c => revealedClueIds.Contains(c.Id))
    .ToList();
knownWarrants = seedCaseFile.KnownWarrants
    .Where(w => revealedWarrantIds.Contains(w.Id))
    .ToList();
```

Note: The `CaseFileViewProjection` constructor may need to be extended to accept `confrontations` and `settlements`. Check the current constructor and add the new parameters. Also verify `SheriffTurnInSettlementState` constructor signature.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "CaseFileViewProjector"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Projections/CaseFileViewProjector.cs tests/WildBunch.Application.Tests/CaseFileViewProjectorTests.cs
git commit -m "BUNCH-80: add confrontation/settlement tracking to CaseFileViewProjector"
```

---

## Task 9: Persistence Event Deserializer + Snapshot — Register 5 New Event Types + Persist CurrentActionContext

**Files:**
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs:34-40` (ResolveEventType switch)
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` (add `CurrentActionContext` to snapshot)
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs` (set `CurrentActionContext` on rehydrate)
- Test: `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs` (add bounty/saloon persistence tests)

**Interfaces:**
- Consumes: all 5 new event types from Phase 1 (including `TownActionContextEntered`)

Without these registrations, `GetEventStreamAsync` throws `Unknown domain event type` when reading persisted bounty/saloon events. Without `CurrentActionContext` in the snapshot, loading from snapshot resets the context to `None`, causing divergence between snapshot-loaded and replay-loaded sessions.

- [ ] **Step 1: Write failing persistence test**

```csharp
[Fact]
public async Task GetEventStreamAsync_ReturnsBountySaloonEvents_AfterPersisted()
{
    using var database = new PostgreSqlTestDatabase();
    var services = CreateServices(database.ConnectionString);
    using var scope = services.CreateScope();
    var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
    var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

    // 1. Create + store + commit
    var session = CreateSessionWithBountySaloonSetup();
    await repo.StoreAsync(session);
    await uow.CommitAsync();
    session.MarkEventsCommitted();

    // 2. Reload + LookAroundSaloon + store + commit
    var loaded = await repo.GetByIdAsync(session.Id);
    loaded!.LookAroundSaloon();
    await repo.StoreAsync(loaded);
    await uow.CommitAsync();

    // 3. Verify stored event type
    var events = await repo.GetEventStreamAsync(session.Id);
    Assert.Equal(2, events.Count);
    Assert.IsType<GameStarted>(events[0]);
    Assert.IsType<SaloonPersonOfInterestSpotted>(events[1]);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Integration.Tests --filter "GetEventStreamAsync_ReturnsBountySaloonEvents"`
Expected: FAIL — `Unknown domain event type: SaloonPersonOfInterestSpotted`

- [ ] **Step 3: Add 4 cases to ResolveEventType**

In `GameSessionJsonSerializer.Events.cs:34-40`, add:

```csharp
private static Type ResolveEventType(string eventType) => eventType switch
{
    nameof(GameStarted) => typeof(GameStarted),
    nameof(StoreItemPurchased) => typeof(StoreItemPurchased),
    nameof(InvestigationPerformed) => typeof(InvestigationPerformed),
    nameof(TownActionContextEntered) => typeof(TownActionContextEntered),
    nameof(SaloonPersonOfInterestSpotted) => typeof(SaloonPersonOfInterestSpotted),
    nameof(WantedSuspectConfronted) => typeof(WantedSuspectConfronted),
    nameof(SheriffTurnInSettled) => typeof(SheriffTurnInSettled),
    nameof(SaloonPersonOfInterestConfronted) => typeof(SaloonPersonOfInterestConfronted),
    _ => throw new InvalidOperationException($"Unknown domain event type: {eventType}")
};
```

Also add `CurrentActionContext` to the session snapshot:

```csharp
// GameSessionJsonSerializer.SessionSnapshot.cs — add to GameSessionSnapshot record:
private sealed record GameSessionSnapshot(
    Guid Id,
    GameStatus Status,
    TravelDifficulty TravelDifficulty,
    AdventureRandomnessPolicy? Entropy,
    TravelRandomnessSnapshot? TravelRandomness,
    TownVisitStateSnapshot? CurrentTownVisit,
    PlayerSnapshot Player,
    WorldSnapshot World,
    CaseFileSnapshot CaseFile,
    PursuitStateSnapshot PursuitState,
    GameClockSnapshot Clock,
    TownActionContext CurrentActionContext,  // NEW
    JourneySnapshot? Journey,
    // ... rest unchanged
{
    public static GameSessionSnapshot FromDomain(GameSession session)
        => new(
            // ... existing fields ...
            GameClockSnapshot.FromDomain(session.Clock),
            session.CurrentActionContext,  // NEW
            // ... rest unchanged ...
        );

    public GameSession ToDomain()
    {
        // ... existing rehydration ...
        // After creating session, set CurrentActionContext:
        GameSessionRehydrator.SetCurrentActionContext(session, CurrentActionContext);
        return session;
    }
}
```

Add to `GameSessionRehydrator.cs`:
```csharp
public static void SetCurrentActionContext(GameSession session, TownActionContext context)
{
    SetBackingField(session, "<CurrentActionContext>k__BackingField", context);
}
```

Note: `CurrentActionContext` is also reconstructed from event replay via `Apply(TownActionContextEntered)`. The snapshot persists it for efficiency when loading from snapshot. Both paths (snapshot load + event replay) must produce the same `CurrentActionContext` value. The persistence test in Step 5 verifies this.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Integration.Tests --filter "GetEventStreamAsync_ReturnsBountySaloonEvents"`
Expected: PASS

- [ ] **Step 5: Write full persistence + replay test for composite bounty/saloon flow**

```csharp
[Fact]
public async Task ReplayFromEvents_ReconstructsBountySaloonState()
{
    using var database = new PostgreSqlTestDatabase();
    var services = CreateServices(database.ConnectionString);
    using var scope = services.CreateScope();
    var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
    var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

    // Create session with bounty/saloon setup
    var session = CreateSessionWithBountySaloonSetup();
    await repo.StoreAsync(session);
    await uow.CommitAsync();
    session.MarkEventsCommitted();

    // Perform full bounty/saloon flow: LookAround → Confront → TurnIn
    var loaded = await repo.GetByIdAsync(session.Id);
    loaded!.LookAroundSaloon();
    // ... setup confrontation ...
    loaded.ResolveWantedSuspectConfrontation(suspectId, WantedSuspectConfrontationChoice.Surrendered);
    loaded.SettleSheriffTurnIn(suspectId, isAlive: true);
    await repo.StoreAsync(loaded);
    await uow.CommitAsync();

    // Load from snapshot
    var fromSnapshot = await repo.GetByIdAsync(session.Id);

    // Load from event stream (full replay)
    var events = await repo.GetEventStreamAsync(session.Id);
    var fromEvents = GameSession.RehydrateFromEvents(
        session.Id, fromSnapshot!.World, fromSnapshot.CaseFile, events);

    // State equality proof — including clock/context state
    Assert.Equal(fromSnapshot.Player.Wallet.Cash, fromEvents.Player.Wallet.Cash);
    Assert.Equal(fromSnapshot.Clock.Day, fromEvents.Clock.Day);
    Assert.Equal(fromSnapshot.Clock.Turn, fromEvents.Clock.Turn);
    Assert.Equal(fromSnapshot.CurrentActionContext, fromEvents.CurrentActionContext);
    Assert.Equal(
        fromSnapshot.CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out _),
        fromEvents.CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out _));
    Assert.Equal(
        fromSnapshot.CaseFile.SheriffTurnInSettlements.Count,
        fromEvents.CaseFile.SheriffTurnInSettlements.Count);
}
```

- [ ] **Step 6: Run full persistence test suite**

Run: `dotnet test tests/WildBunch.Integration.Tests --filter "EventStorePersistence"`
Expected: All PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs
git commit -m "BUNCH-80: register 5 new event types in persistence deserializer + persist CurrentActionContext in snapshot"
```

---

## Task 10: Migrate 5 Bounty/Saloon Handlers to GameSessionCommandHandler Orchestration

**Files:**
- Modify: `src/WildBunch.Application/Games/Commands/LookAroundSaloonHandler.cs`
- Modify: `src/WildBunch.Application/Games/Commands/ConfrontSaloonPersonOfInterestHandler.cs`
- Modify: `src/WildBunch.Application/Games/Commands/ConfrontSaloonWantedSuspectHandler.cs`
- Modify: `src/WildBunch.Application/Games/Commands/ConfrontWantedSuspectHandler.cs`
- Modify: `src/WildBunch.Application/Games/Commands/TurnInToSheriffHandler.cs`
- Test: existing handler tests in `tests/WildBunch.Application.Tests/`

**Interfaces:**
- Consumes: `GameSessionCommandHandler` base class (`ExecuteWithRetryAsync`)
- Produces: all handlers inherit from `GameSessionCommandHandler` instead of manual load/store/commit

Now that bounty/saloon methods produce typed events, `UncommittedEvents.Count > 0` will be true after mutations, so `ExecuteWithRetryAsync` will store and commit. Rejections that don't enter a new action context produce no events, so the base class correctly skips the store step. Rejections that DO enter a new action context (e.g., a rejected sheriff turn-in) produce a `TownActionContextEntered` event, so the base class will store and commit the context change.

- [ ] **Step 1: Migrate LookAroundSaloonHandler**

Current pattern (manual load/store/commit):
```csharp
public sealed class LookAroundSaloonHandler
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameSessionUnitOfWork _gameSessionUnitOfWork;
    private readonly JournalResolver _journalResolver;
    // ... manual LoadRequiredAsync, LookAroundSaloon, if SessionChanged: StoreAsync+CommitAsync
}
```

Migrated pattern (base class orchestration):
```csharp
public sealed class LookAroundSaloonHandler : GameSessionCommandHandler
{
    private readonly JournalResolver _journalResolver;

    public LookAroundSaloonHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        JournalResolver journalResolver)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _journalResolver = journalResolver;
    }

    public async Task<InvestigationActionResultDto> HandleAsync(
        LookAroundSaloonCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sessionId = new GameSessionId(command.GameSessionId);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            var actionResult = session.LookAroundSaloon();
            return new InvestigationActionResultDto(
                actionResult.Success,
                actionResult.Message,
                JournalMapper.ToDto(_journalResolver.Resolve(session)));
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

Key change: inherits `GameSessionCommandHandler`, uses `ExecuteWithRetryAsync` which handles load/store/commit/retry. The `if (actionResult.SessionChanged)` check is no longer needed — the base class gates on `UncommittedEvents.Count > 0`.

- [ ] **Step 2: Migrate ConfrontSaloonPersonOfInterestHandler**

Same pattern. Check the current handler at `ConfrontSaloonPersonOfInterestHandler.cs` and migrate to inherit `GameSessionCommandHandler`. The return type is `SaloonPersonOfInterestConfrontationResultDto`.

- [ ] **Step 3: Migrate ConfrontSaloonWantedSuspectHandler**

Same pattern. Return type is `WantedSuspectConfrontationResultDto`.

- [ ] **Step 4: Migrate ConfrontWantedSuspectHandler**

Same pattern. Return type is `WantedSuspectConfrontationResultDto`. This handler currently does NOT inherit from the base class (it has manual load/store/commit at lines 29-36).

- [ ] **Step 5: Migrate TurnInToSheriffHandler**

Same pattern. Return type is `SheriffTurnInResultDto`.

- [ ] **Step 6: Run existing handler tests to verify no regressions**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "LookAroundSaloon|ConfrontSaloon|ConfrontWanted|TurnInToSheriff"`
Expected: All PASS

Note: Some handler tests may mock the repository and unit of work directly. The base class `ExecuteWithRetryAsync` calls `GameSessionRepository.LoadRequiredAsync` and `GameSessionRepository.StoreAsync`. Verify that existing test mocks support this call pattern. If tests use a different mock setup, update them to match the base class pattern. Check how `ReadWantedPostersHandlerTests` mocks the handler dependencies — that's the established pattern for base-class handlers.

- [ ] **Step 7: Write handler event-production test**

```csharp
[Fact]
public async Task HandleAsync_LookAroundSaloon_PersistsEventViaBaseClass()
{
    // Use the same mock pattern as ReadWantedPostersHandlerTests
    var handler = CreateLookAroundSaloonHandler();
    var result = await handler.HandleAsync(new LookAroundSaloonCommand(sessionId));

    Assert.True(result.Success);
    // Verify event was stored via the base class (check mock repository was called)
    _mockRepo.Verify(r => r.StoreAsync(It.IsAny<GameSession>(), It.IsAny<Guid>(), default), Times.Once);
}
```

- [ ] **Step 8: Commit**

```bash
git add src/WildBunch.Application/Games/Commands/LookAroundSaloonHandler.cs src/WildBunch.Application/Games/Commands/ConfrontSaloonPersonOfInterestHandler.cs src/WildBunch.Application/Games/Commands/ConfrontSaloonWantedSuspectHandler.cs src/WildBunch.Application/Games/Commands/ConfrontWantedSuspectHandler.cs src/WildBunch.Application/Games/Commands/TurnInToSheriffHandler.cs tests/WildBunch.Application.Tests/
git commit -m "BUNCH-80: migrate 5 bounty/saloon handlers to GameSessionCommandHandler orchestration"
```

---

## Task 10.5: TimeOfDay DTO + Frontend Display

**Files:**
- Modify: `src/WildBunch.Application/Games/Models/GameDtos.cs` (add `TimeOfDay` to `GameClockDto`)
- Modify: `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs` (map `TimeOfDay`)
- Modify: `src/WildBunch.Application/Games/Mapping/JournalMapper.cs` (map `TimeOfDay`)
- Modify: `src/WildBunch.Web/src/api/types.ts` (add `timeOfDay` to `GameClockDto`)
- Modify: `src/WildBunch.Web/src/shell/Hud.tsx` (display `TimeOfDay` name)
- Modify: `src/WildBunch.Web/src/routes/DebugCockpitRoute.tsx` (display `TimeOfDay` name)
- Modify: `src/WildBunch.Web/src/components/CaseFileSurface.tsx` (display `TimeOfDay` name)
- Modify: `src/WildBunch.Web/src/tests/test-utils/factories.ts` (add `timeOfDay` to clock fixtures)

- [ ] **Step 1: Update GameClockDto**

```csharp
// src/WildBunch.Application/Games/Models/GameDtos.cs
// BEFORE:
public sealed record GameClockDto(int Day, int Turn);

// AFTER:
public sealed record GameClockDto(int Day, int Turn, string TimeOfDay);
```

The `TimeOfDay` is a string ("Morning", "Afternoon", "Evening", "Night") for JSON serialization simplicity. The numeric `Turn` stays for backward compatibility.

- [ ] **Step 2: Update mappers**

```csharp
// GameSessionMapper.cs:97
new GameClockDto(clock.Day, clock.Turn, clock.TimeOfDay.ToString()),

// JournalMapper.cs:23
new GameClockDto(snapshot.Day, snapshot.Turn, ((TimeOfDay)snapshot.Turn).ToString()),
```

Note: `JournalSnapshot` may not carry `TimeOfDay` directly — it carries `Day` and `Turn` as ints. Derive `TimeOfDay` from `Turn` via cast.

- [ ] **Step 3: Update frontend types**

```typescript
// src/api/types.ts
export interface GameClockDto {
  day: number;
  turn: number;
  timeOfDay: string;  // "Morning" | "Afternoon" | "Evening" | "Night"
}
```

- [ ] **Step 4: Update frontend display**

```tsx
// Hud.tsx:33 — BEFORE:
<strong>{`Day ${session.clock.day}, Turn ${session.clock.turn}`}</strong>
// AFTER:
<strong>{`Day ${session.clock.day}, ${session.clock.timeOfDay}`}</strong>

// DebugCockpitRoute.tsx:60 — same pattern

// CaseFileSurface.tsx:202 — BEFORE:
return `Day ${journal.clock.day}, turn ${journal.clock.turn} in ${journal.currentTown.name}`;
// AFTER:
return `Day ${journal.clock.day}, ${journal.clock.timeOfDay} in ${journal.currentTown.name}`;

// CaseFileSurface.tsx:409 — BEFORE:
Day {caseJournal.clock.day}, turn {caseJournal.clock.turn}
// AFTER:
Day {caseJournal.clock.day}, {caseJournal.clock.timeOfDay}
```

- [ ] **Step 5: Update test factories**

Add `timeOfDay: "Morning"` (or appropriate value) to all clock fixtures in:
- `src/tests/test-utils/factories.ts` (lines 52, 69)
- `src/tests/TravelRoutesPanel.test.tsx` (line 87)
- `src/tests/TravelPanel.test.tsx` (line 85)
- `src/tests/StartGamePanel.test.tsx` (line 59)
- `src/tests/AppShell.test.tsx` (lines 125, 138)
- `src/tests/App.test.tsx` (lines 156, 169)

- [ ] **Step 6: Run frontend build and tests**

```powershell
cd src\WildBunch.Web
npm run build
npm test
```
Expected: Build and tests pass

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Application/Games/Models/GameDtos.cs src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs src/WildBunch.Application/Games/Mapping/JournalMapper.cs src/WildBunch.Web/src/api/types.ts src/WildBunch.Web/src/shell/Hud.tsx src/WildBunch.Web/src/routes/DebugCockpitRoute.tsx src/WildBunch.Web/src/components/CaseFileSurface.tsx src/WildBunch.Web/src/tests/
git commit -m "BUNCH-80: add TimeOfDay to GameClockDto and frontend display"
```

---

## Task 11: Update AddLogEntry Guardrail Test

**Files:**
- Modify: `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs`

The guardrail test prevents new `AddLogEntry` call sites. After migrating bounty/saloon, the `RecordCaseUpdate` calls in `BountyLoopCoordinator` are removed (moved to Apply methods). The guardrail count should decrease. The `Apply` methods still call `RecordCaseUpdate` as a transitional bridge (same as `Apply(InvestigationPerformed)`), so some call sites remain in Apply methods.

- [ ] **Step 1: Read current guardrail test**

Read `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs` to understand the current assertion (likely counts `AddLogEntry` call sites or asserts no new sites beyond a known set).

- [ ] **Step 2: Update the guardrail count/assertion**

After migration:
- `BountyLoopCoordinator.ResolveWantedSuspectConfrontation` no longer calls `RecordCaseUpdate` directly (moved to `Apply(WantedSuspectConfronted)`)
- `BountyLoopCoordinator.SettleSheriffTurnIn` no longer calls `RecordCaseUpdate` directly (moved to `Apply(SheriffTurnInSettled)`)
- `GameSession.LookAroundSaloon` no longer calls `RecordCaseUpdate` directly (moved to `Apply(SaloonPersonOfInterestSpotted)`)
- The Apply methods call `RecordCaseUpdate` as a bridge — these are the same transitional pattern as `Apply(InvestigationPerformed)`

Update the guardrail to reflect the reduced direct-mutation call count. The exact number depends on the current test assertion — read it and adjust.

- [ ] **Step 3: Run guardrail test**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "AddLogEntryGuardrail"`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs
git commit -m "BUNCH-80: update AddLogEntry guardrail after bounty/saloon migration"
```

---

## Task 12: Hidden-Truth Boundary Tests

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs` (or create domain-level hidden-truth tests for events)

The existing `GameApiHiddenTruthTests` validates that API responses don't leak hidden state. After migration, we need to verify that:
1. The 5 new event types don't carry hidden state (`TrueCulpritId`, `LinkedSuspectIds`, `TargetKind`, `KillerReleaseState`)
2. The projections don't expose hidden state
3. The existing API hidden-truth tests still pass

- [ ] **Step 1: Write event payload hidden-truth tests**

```csharp
[Fact]
public void BountySaloonEvents_DoNotCarryHiddenTruthFields()
{
    // Verify that none of the 5 new event types have properties named
    // TrueCulpritId, LinkedSuspectIds, TargetKind, or KillerReleaseState
    var eventTypes = new[]
    {
        typeof(TownActionContextEntered),
        typeof(SaloonPersonOfInterestSpotted),
        typeof(WantedSuspectConfronted),
        typeof(SheriffTurnInSettled),
        typeof(SaloonPersonOfInterestConfronted)
    };

    var forbiddenNames = new[] { "TrueCulpritId", "LinkedSuspectIds", "TargetKind", "KillerReleaseState" };

    foreach (var type in eventTypes)
    {
        foreach (var prop in type.GetProperties())
        {
            Assert.DoesNotContain(prop.Name, forbiddenNames);
        }
    }
}
```

- [ ] **Step 2: Run existing API hidden-truth tests**

Run: `dotnet test tests/WildBunch.Integration.Tests --filter "HiddenTruth"`
Expected: All PASS (bounty/saloon endpoints still return same DTO shapes; no new hidden-state exposure)

- [ ] **Step 3: Commit**

```bash
git add tests/WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs
git commit -m "BUNCH-80: add hidden-truth boundary tests for bounty/saloon events"
```

---

## Task 13: Update ADR-0028

**Files:**
- Modify: `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`

Update ADR-0028 to reflect:
- Bounty/saloon flows are now migrated to typed events + Apply
- Clock/turn correction: `RecordCaseUpdate` decoupled from clock, `TimeOfDay` enum added, `TownActionContext`-based turn advancement
- 5 new event types: `TownActionContextEntered`, `SaloonPersonOfInterestSpotted`, `WantedSuspectConfronted`, `SheriffTurnInSettled`, `SaloonPersonOfInterestConfronted`
- 5 new event types registered in persistence deserializer
- 5 handlers migrated to `GameSessionCommandHandler` orchestration
- DiaryProjector, HudProjector, CaseFileViewProjector updated for bounty/saloon events
- `InvestigationPerformed` event no longer carries `AdvanceClock`
- Remaining non-migrated flows: travel/journey (12+ AddLogEntry sites), case completion (1)
- `LegacyLogProjector` still not implemented (deferred to follow-up)
- `LogEntries` still in DTOs for backward compatibility

- [ ] **Step 1: Read current ADR-0028**

Read `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md` to find the sections about migrated flows (§13), remaining work (§148-152), and `LegacyLogProjector` (§12).

- [ ] **Step 2: Update migrated-flow list**

Add bounty/saloon to the list of migrated flows in §13. List the 5 new event types and 5 migrated methods.

- [ ] **Step 3: Update remaining-work notes**

Update §148-152 to reflect:
- Travel/journey is the largest remaining seam (follow-up issue)
- Legacy log deprecation is the final follow-up (after travel migration)
- Bounty/saloon is no longer in the remaining-work list

- [ ] **Step 4: Commit**

```bash
git add docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md
git commit -m "BUNCH-80: update ADR-0028 to mark bounty/saloon as migrated"
```

---

## Task 14: Full Validation

- [ ] **Step 1: Run dotnet build**

```powershell
dotnet build
```
Expected: Build succeeds with no errors

- [ ] **Step 2: Run PostgreSQL-backed validation**

```powershell
.\scripts\postgres-dev.ps1 ensure
.\scripts\postgres-dev.ps1 validate
```
Expected: All tests pass, EF migrations list succeeds

- [ ] **Step 3: Run targeted bounty/saloon test suites**

```powershell
dotnet test tests/WildBunch.Domain.Tests --filter "Bounty|Saloon|Confront|Sheriff|Wanted"
dotnet test tests/WildBunch.Application.Tests --filter "Bounty|Saloon|Confront|Sheriff|Wanted|DiaryProjector|HudProjector|CaseFileView|AddLogEntry"
dotnet test tests/WildBunch.Integration.Tests --filter "EventStore|HiddenTruth|Bounty|Saloon"
```
- [ ] **Step 3: Run targeted test suites**

```powershell
dotnet test tests/WildBunch.Domain.Tests --filter "Bounty|Saloon|Confront|Sheriff|Wanted|ClockTurn"
dotnet test tests/WildBunch.Application.Tests --filter "Bounty|Saloon|Confront|Sheriff|Wanted|DiaryProjector|HudProjector|CaseFileView|AddLogEntry"
dotnet test tests/WildBunch.Integration.Tests --filter "EventStore|HiddenTruth|Bounty|Saloon"
```
Expected: All PASS

- [ ] **Step 4: Run full test suite via PostgreSQL lane**

```powershell
.\scripts\postgres-dev.ps1 validate
```
Expected: All PASS

- [ ] **Step 5: Run frontend build and tests**

```powershell
cd src\WildBunch.Web
npm run build
npm test
```
Expected: Build and tests pass (TimeOfDay display changes + test factory updates)

- [ ] **Step 6: Final commit if any remaining changes**

```bash
git add -A
git status
# If clean, no commit needed. If there are remaining changes, commit them.
```

---

## Notes for Implementation

### Test helper patterns

- **Domain tests:** Use `TestSessionFactory` in `tests/WildBunch.Domain.Tests/`. Add new factory methods for bounty/saloon setups. Follow the pattern from `InvestigationEventSourcingTests.cs`.
- **Application tests:** Check `ReadWantedPostersHandlerTests.cs` for the base-class handler mock pattern. Projection tests may use inline event arrays.
- **Integration tests:** Use `PostgreSqlPersistenceFixture` and `PostgreSqlTestDatabase` from `EventStorePersistenceTests.cs`. Follow the `CreateSession` helper pattern.

### CaseFileViewProjection type

Check if `CaseFileViewProjection` already has `Confrontations` and `Settlements` fields. If not, add them as `IReadOnlyList<WantedSuspectConfrontationState>` and `IReadOnlyList<SheriffTurnInSettlementState>`. This is an additive change to a developer-facing projection (not player-facing), so no API/DTO changes are needed.

### SheriffTurnInSettlementState and WantedSuspectConfrontationState constructors

Verify exact constructor signatures before using them in Apply methods and projection tests:
- `WantedSuspectConfrontationState` — check `src/WildBunch.Domain/Cases/WantedSuspectConfrontation.cs`
- `SheriffTurnInSettlementState` — check `src/WildBunch.Domain/Cases/` for the settlement state type

### Handler test mock updates

When migrating handlers to `GameSessionCommandHandler` base class, the test mocks need to support `LoadRequiredAsync` and `StoreAsync` calls from the base class. Check `ReadWantedPostersHandlerTests.cs` for the established mock pattern. If existing bounty/saloon handler tests use a different mock setup, update them.

### Composite event ordering

For `ConfrontSaloonPersonOfInterest` armed+correct path, the event stream order is:
1. `TownActionContextEntered(Saloon)` (from `LookAroundSaloon` — if not already in Saloon)
2. `SaloonPersonOfInterestSpotted` (from `LookAroundSaloon`)
3. `WantedSuspectConfronted` (from `ResolveWantedSuspectConfrontation`)
4. `TownActionContextEntered(SheriffOffice)` (from `SettleSheriffTurnIn` — context change from Saloon)
5. `SheriffTurnInSettled` (from `SettleSheriffTurnIn`)
6. `SaloonPersonOfInterestConfronted` (clears saloon person)

The replay dispatcher handles events in order, so Apply methods must be idempotent and not depend on state that hasn't been set yet. The `TownActionContextEntered` Apply sets the clock and context — subsequent Apply methods for gameplay events use the already-set clock state (e.g., `WantedSuspectConfronted` Apply records `Clock.Turn` which was set by the preceding context event).

### Projection time tracking

Projections (DiaryProjector, HudProjector) must NOT invent time by incrementing a local `turn++` counter. They track `day`/`turn` from `TownActionContextEntered` events (the event-sourced clock state) and use those tracked values for diary entries. This ensures projections derive time from the event stream, not from invented local state.

### Validation cleanup

After validation, stop any worker-owned processes:
- No long-running servers should be started (no API server, no Vite dev server needed for this campaign)
- PostgreSQL service is shared and should NOT be stopped
- Report cleanup proof in the return evidence
