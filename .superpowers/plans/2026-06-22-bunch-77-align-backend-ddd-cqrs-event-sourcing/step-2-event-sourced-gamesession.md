# Step 2 — Event-Sourced `GameSession` for the Migrated Slice

> Parent plan: `../2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing.md`
> Acceptance criteria covered: **AC-002** (events applied by aggregate), **AC-003** (aggregate root remains the command consistency boundary), **AC-006** (representative flows prove the seam).

## Goal

Make Event Sourcing **materially true** for the migrated slice (start new game, purchase store item). This means:

1. `GameSession` gains `Apply(GameStarted)` and `Apply(StoreItemPurchased)` methods that mutate state.
2. `StartNew` and `Purchase` are refactored: validate intent → produce typed domain event → call `Apply` → record event in uncommitted list. State changes come from `Apply`, not from direct mutation in the command method.
3. `GameSession` owns an uncommitted-events list and a version counter.
4. `GameSession.RehydrateFromEvents` constructs a session from external references + typed events and replays them through `Apply` to reconstruct state.
5. Non-migrated flows keep their existing direct-mutation path, clearly marked as not-yet-migrated.

This is the core domain change. After this step, the migrated flows are truly event-sourced: state changes are driven through event application, and replay reconstructs state.

## Files

- Modify: `src/WildBunch.Domain/Game/GameSession.cs` — add `Apply` methods, uncommitted-events list, version counter, `RehydrateFromEvents`, refactor `StartNew` and `Purchase`.
- Add: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` — a `partial class` helper for `GameSession` containing the `RehydrateFromEvents` static factory and the event dispatch helper. Keeps `GameSession.cs` from growing unwieldy (per AGENTS.md: extract pure helpers before god-object drift).
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs` — the snapshot-based rehydrator continues to work for non-migrated state. Step 3 adds event-store-based rehydration; for Step 2, the rehydrator is unchanged (events are not persisted yet). The worker verifies the existing rehydrator still constructs cleanly with the new `GameSession` shape.
- Add: `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs` — the core proof tests.
- Modify (extend): `tests/WildBunch.Domain.Tests/GameSessionAggregateRootTests.cs` — assert `StartNew` produces a `GameStarted` event and state matches.
- Modify (extend): `tests/WildBunch.Domain.Tests/GameSessionPurchaseTests.cs` — assert `Purchase` produces a `StoreItemPurchased` event and state matches.

## `GameSession` event-sourcing surface

### Uncommitted events and version

```csharp
private readonly List<IDomainEvent> _uncommittedEvents = [];
private int _version; // number of events applied (committed + uncommitted)

public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;
public int Version => _version;

internal void MarkEventsCommitted()
{
    _uncommittedEvents.Clear();
}
```

`_version` counts all events applied (committed + uncommitted). When loaded from the event store (Step 3), `MarkEventsCommitted` is called after replay. When loaded from snapshot, `_version` is set from the snapshot's version field (Step 3).

### `Apply` methods

```csharp
private void Apply(GameStarted e)
{
    // State changes that StartNew currently does directly:
    Player = new Player(
        e.PlayerName,
        e.StartingTownId,
        e.StartingHealth,
        WildBunch.Domain.Economy.Wallet.Starting(e.StartingWallet),
        DomainInventory.Empty());
    Status = GameStatus.Active;
    TravelDifficulty = e.Difficulty;
    TravelRandomness = e.TravelRandomness;
    Entropy = e.Entropy;
    // TownAggregate and CaseFile are external references provided at construction.
    // _currentTown is initialized from Player.CurrentTownId in the constructor.
}

private void Apply(StoreItemPurchased e)
{
    // State changes that Purchase currently does directly:
    Player.SpendCash(e.TotalPrice);
    Player.AddItem(e.ItemKind, e.Quantity);
}
```

`Apply` is the **single mutation path** for migrated state. Command methods do not directly mutate state for migrated flows — they produce the event and call `Apply`.

### Command method refactoring: `StartNew`

Current `StartNew` directly constructs `Player`, sets `Status`, `TravelDifficulty`, etc., then calls `AddLogEntry`.

Refactored:

```csharp
public static GameSession StartNew(
    string playerName, DomainWorld world, CaseFile caseFile, /* ...existing params... */)
{
    // 1. Validate intent (existing validation)
    ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
    ArgumentNullException.ThrowIfNull(world);
    ArgumentNullException.ThrowIfNull(caseFile);

    var resolvedTownId = startingTownId ?? world.Towns.First().Id;
    var startingTown = world.GetTown(resolvedTownId);

    // 2. Produce typed domain event
    var e = new GameStarted
    {
        PlayerName = playerName,
        StartingTownId = startingTown.Id,
        StartingTownName = startingTown.Name,
        StartingHealth = StartingHealthFor(travelDifficulty),
        StartingWallet = wallet?.Amount ?? 25m,
        Difficulty = travelDifficulty,
        TravelRandomness = travelRandomness ?? TravelRandomnessState.CreateRuntimeSalted(),
        Entropy = entropy
    };

    // 3. Construct empty session and apply event
    var session = new GameSession(GameSessionId.New(), world, caseFile, e.TravelRandomness, e.Entropy);
    session.Apply(e);
    session._uncommittedEvents.Add(e);

    // 4. Legacy log entry (Step 7 demotes this; kept for now)
    session.AddLogEntry(GameLogEntryKind.Opening, $"The hunt begins in {startingTown.Name}.");

    return session;
}
```

The key change: `Player` construction and state initialization move from the command method into `Apply(GameStarted)`. The command method validates and produces the event; `Apply` mutates.

### Command method refactoring: `Purchase`

Current `Purchase` validates, then directly calls `Player.SpendCash` and `Player.AddItem`.

Refactored:

```csharp
public StorePurchaseResult Purchase(StoreOffer offer, int quantity)
{
    // 1. Validate intent (existing validation — unchanged)
    ArgumentNullException.ThrowIfNull(offer);
    if (IsJourneyModal()) return StorePurchaseResult.Failed(JourneyModalBlockMessage);
    if (quantity < 1) return StorePurchaseResult.Failed("Quantity must be at least 1.");
    if (offer.ItemKind == ItemKind.Horse && quantity != 1) return StorePurchaseResult.Failed("Horse items must have a quantity of 1.");
    if (quantity != 1 && !IsStackableItemKind(offer.ItemKind)) return StorePurchaseResult.Failed($"{offer.ItemKind} does not stack.");
    var totalPrice = offer.Price * quantity;
    if (!Player.CanAfford(totalPrice)) return StorePurchaseResult.Failed("Not enough cash.");
    if (!CanPurchaseInventoryItem(offer, quantity, out var inventoryFailureMessage)) return StorePurchaseResult.Failed(inventoryFailureMessage);

    // 2. Produce typed domain event
    var e = new StoreItemPurchased
    {
        TownId = CurrentTown.Id,
        ItemKind = offer.ItemKind,
        DisplayName = offer.DisplayName,
        Quantity = quantity,
        UnitPrice = offer.Price,
        TotalPrice = totalPrice,
        WalletAfter = Player.Wallet.Amount - totalPrice
    };

    // 3. Apply event (mutates state)
    Apply(e);
    _uncommittedEvents.Add(e);

    // 4. Legacy log entry (Step 7 demotes this; kept for now)
    var quantityLabel = quantity == 1 ? offer.DisplayName : $"{quantity} {offer.DisplayName}";
    AddLogEntry(GameLogEntryKind.Purchase, $"Purchased {quantityLabel} for ${totalPrice:0.00}.");

    return StorePurchaseResult.Succeeded($"Purchased {quantityLabel} for ${totalPrice:0.00}.");
}
```

The key change: `Player.SpendCash` and `Player.AddItem` move from the command method into `Apply(StoreItemPurchased)`. The command method validates and produces the event; `Apply` mutates.

### `RehydrateFromEvents`

```csharp
public static GameSession RehydrateFromEvents(
    GameSessionId id,
    DomainWorld world,
    CaseFile caseFile,
    IReadOnlyList<IDomainEvent> events)
{
    var session = new GameSession(id, world, caseFile, /* minimal init */);
    foreach (var e in events)
    {
        ApplyEvent(session, e);
    }
    session.MarkEventsCommitted();
    return session;
}

private static void ApplyEvent(GameSession session, IDomainEvent e)
{
    switch (e)
    {
        case GameStarted gs: session.Apply(gs); break;
        case StoreItemPurchased p: session.Apply(p); break;
        default: throw new InvalidOperationException($"Unknown domain event type: {e.GetType().Name}");
    }
}
```

This is the replay path. It constructs a session from external references (`world`, `caseFile`) and replays typed events through `Apply`. After replay, events are marked committed (they came from the store).

### Constructor changes

`GameSession` needs a minimal constructor for `RehydrateFromEvents` that takes `id`, `world`, `caseFile`, and initial `TravelRandomness`/`Entropy` (which may be overwritten by `Apply(GameStarted)`). The existing persistence constructor (used by `GameSessionRehydrator`) continues to work for snapshot-based loading. The worker may add a new internal constructor or adapt the existing one — the key constraint is that `RehydrateFromEvents` produces a session whose migrated state matches what `Apply` would produce.

### Non-migrated flows

All other command methods (`StartJourney`, `AdvanceJourneyDay`, `ReadWantedPosters`, `LookAroundSaloon`, `AssessSheriffTurnIn`, etc.) keep their existing direct-mutation path. They are **not** refactored in this step. The worker adds a comment block near the top of `GameSession.cs`:

```csharp
// Event-sourced flows (migrated): StartNew, Purchase.
// Direct-mutation flows (not-yet-migrated): all others.
// See ADR-0028 and follow-up issues for the migration path.
// Do not add new direct-mutation command methods; use the event-sourced pattern.
```

This makes the transitional coexistence explicit and directs future work to the event-sourced pattern.

## Replay proof (the core AC-006 evidence)

The test must prove:

1. **Command path and replay path produce the same state.**
   - `var sessionA = GameSession.StartNew(...); sessionA.Purchase(offer, 3);`
   - `var events = sessionA.UncommittedEvents;`
   - `var sessionB = GameSession.RehydrateFromEvents(sessionA.Id, world, caseFile, events);`
   - Assert: `sessionB.Player.Wallet.Amount == sessionA.Player.Wallet.Amount`
   - Assert: `sessionB.Player.Inventory` matches `sessionA.Player.Inventory`
   - Assert: `sessionB.Status == sessionA.Status`
   - Assert: `sessionB.TravelDifficulty == sessionA.TravelDifficulty`

2. **Multiple purchases replay correctly.**
   - Start new game, purchase item A, purchase item B.
   - Replay all events.
   - Assert wallet and inventory match.

3. **Uncommitted events are collected in order.**
   - Assert `UncommittedEvents` is `[GameStarted, StoreItemPurchased, ...]` in command order.

4. **`MarkEventsCommitted` clears uncommitted without changing state.**
   - After `MarkEventsCommitted`, `UncommittedEvents` is empty, state is unchanged.

## Tasks

- [ ] **Task 1: Add uncommitted-events list, version counter, and `MarkEventsCommitted` to `GameSession`.**
- [ ] **Task 2: Add `Apply(GameStarted)` method.** Move `Player` construction and state initialization from `StartNew` into `Apply`.
- [ ] **Task 3: Add `Apply(StoreItemPurchased)` method.** Move `Player.SpendCash` and `Player.AddItem` from `Purchase` into `Apply`.
- [ ] **Task 4: Refactor `StartNew`** to validate → produce `GameStarted` → `Apply` → record. Preserve the legacy `AddLogEntry` call for now.
- [ ] **Task 5: Refactor `Purchase`** to validate → produce `StoreItemPurchased` → `Apply` → record. Preserve the legacy `AddLogEntry` call and the existing `StorePurchaseResult` return shape.
- [ ] **Task 6: Add `GameSessionEventReplay.cs` partial class** with `RehydrateFromEvents` and `ApplyEvent` dispatch.
- [ ] **Task 7: Add the minimal constructor** for `RehydrateFromEvents` (or adapt the existing one). Verify the existing `GameSessionRehydrator` still works for snapshot-based loading.
- [ ] **Task 8: Add the non-migrated-flows comment block** directing future work to the event-sourced pattern.
- [ ] **Task 9: Write `GameSessionEventSourcingTests`.** The four replay proofs above.
- [ ] **Task 10: Extend `GameSessionAggregateRootTests`** to assert `StartNew` produces a `GameStarted` event.
- [ ] **Task 11: Extend `GameSessionPurchaseTests`** to assert `Purchase` produces a `StoreItemPurchased` event.
- [ ] **Task 12: Run the full domain test suite.** Existing behavior must be preserved — the refactored `StartNew` and `Purchase` produce the same state and results as before.

## Validation

- [ ] **V1: `dotnet build`** passes.
- [ ] **V2: `dotnet test tests/WildBunch.Domain.Tests`** passes — existing behavior preserved, new event-sourcing tests pass.
- [ ] **V3: `dotnet test`** (full suite) passes.
- [ ] **V4: Replay proof passes.** `GameSessionEventSourcingTests` proves command path and replay path produce the same state.
- [ ] **V5: No persistence/handler/API changes.** `git status` shows only `WildBunch.Domain/**` and test files.
- [ ] **V6: Existing public result objects unchanged.** `StorePurchaseResult` shape and `StartNew` return type are unchanged.

## Acceptance mapping

- **AC-002:** events are applied by the aggregate through `Apply` methods. State changes are driven through event application. This is materially true Event Sourcing for the migrated slice.
- **AC-003:** `Apply` methods live inside `GameSession`. Command methods validate and produce events. The aggregate root remains the command consistency boundary. Handlers do not become domain truth (Step 5).
- **AC-006:** the two migrated flows (start game, purchase) prove the seam: command-produces-event → apply → replay reconstructs state. Other flows deferred with follow-up issues.

## Non-goals for this step

- No persistence of events (Step 3).
- No optimistic concurrency (Step 3 — the version counter is added here but the concurrency check is in the store).
- No handler changes (Step 5).
- No API changes (Step 6).
- No projection contracts (Step 4).
- No removal of `AddLogEntry` calls (Step 7 demotes them).
- No migration of non-migrated flows (follow-up issues).
- No replay-from-events load path in persistence (Step 3 — `RehydrateFromEvents` exists in Domain but is not wired to persistence until Step 3).
- No sub-aggregate splits (BUNCH-67).

## Self-Review

**Spec coverage:** Step 2 makes Event Sourcing materially true for the migrated slice: command-produces-event → `Apply` mutates → replay reconstructs. This is the core correction from the rejected draft.

**Placeholder scan:** The minimal constructor for `RehydrateFromEvents` is a design decision the worker resolves during implementation. No TBDs in the contract.

**Type consistency:** `Apply` methods are private, matching the existing `GameSession` method visibility. `IDomainEvent` list replaces `object` list. Partial class extraction follows AGENTS.md guidance.

**Non-goals:** All nine non-goals preserved.
