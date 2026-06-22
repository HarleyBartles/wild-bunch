# Step 1 — Typed Domain Events (No Envelope in Domain)

> Parent plan: `../2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing.md`
> Acceptance criteria covered: **AC-002** (event sourcing seam visible in code, domain half).

## Goal

Introduce typed domain events as **plain sealed records** in `WildBunch.Domain/Events/`. These are domain facts — structured, typed, carrying only the data that represents the decision that happened. **No envelope fields** (EventId, Sequence, OccurredAtUtc, SchemaVersion, CorrelationId, CausationId). Those are infrastructure concerns introduced at the persistence boundary in Step 3.

This is the critical correction from the rejected draft. Domain owns typed facts, not a generic `GameEvent` envelope with `string EventType` and `object Payload`. There is no string dispatch, no generic payload casting, no storage vocabulary leaking inward.

## Files

- Add: `src/WildBunch.Domain/Events/GameStarted.cs`
- Add: `src/WildBunch.Domain/Events/StoreItemPurchased.cs`
- Add: `src/WildBunch.Domain/Events/IDomainEvent.cs` — a marker interface implemented by all typed domain events, enabling the aggregate and projectors to accept a list of events without `object`.
- Add: `tests/WildBunch.Domain.Tests/Events/TypedDomainEventTests.cs`
- No changes to `GameSession.cs` (Step 2), persistence (Step 3), handlers (Step 5), or API (Step 6).

## Marker interface

```csharp
namespace WildBunch.Domain.Events;

/// <summary>
/// Marker interface for typed domain events — immutable facts produced by aggregate command methods.
/// Domain events carry only decision data. Envelope metadata (event id, sequence, timestamp, etc.)
/// is infrastructure and lives in WildBunch.Persistence, not here. See ADR-0028.
/// </summary>
public interface IDomainEvent { }
```

Every typed domain event implements `IDomainEvent`. This lets `GameSession` hold `List<IDomainEvent>` for uncommitted events and projectors accept `IReadOnlyList<IDomainEvent>` without falling back to `object`. Pattern matching dispatches on the concrete type.

## Typed domain events for the migrated slice

Only two events in this step — one per migrated flow.

### `GameStarted`

```csharp
namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a new game session was started with the given player and world configuration.
/// </summary>
public sealed record GameStarted : IDomainEvent
{
    public required string PlayerName { get; init; }
    public required TownId StartingTownId { get; init; }
    public required string StartingTownName { get; init; }
    public required int StartingHealth { get; init; }
    public required decimal StartingWallet { get; init; }
    public required TravelDifficulty Difficulty { get; init; }
    public required TravelRandomnessState TravelRandomness { get; init; }
    public required AdventureRandomnessPolicy Entropy { get; init; }
}
```

This captures every decision `GameSession.StartNew` makes today: player name, starting town, starting health (derived from difficulty), starting wallet, difficulty, travel randomness, and entropy/seed policy. `World` and `CaseFile` are external references provided at load time — they are content/templates, not decisions, so they are not in the event.

### `StoreItemPurchased`

```csharp
namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player purchased a quantity of a store item at the current town.
/// </summary>
public sealed record StoreItemPurchased : IDomainEvent
{
    public required TownId TownId { get; init; }
    public required ItemKind ItemKind { get; init; }
    public required string DisplayName { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal TotalPrice { get; init; }
    public required decimal WalletAfter { get; init; }
}
```

This captures every decision `GameSession.Purchase` makes: what was bought, where, how many, at what price, and the wallet state after. The `Apply` method in Step 2 uses `WalletAfter` to set the wallet and `ItemKind`/`Quantity` to add the item.

## What is NOT in Domain

- No `GameEvent` envelope type.
- No `EventType` string constants.
- No `EventId`, `Sequence`, `OccurredAtUtc`, `SchemaVersion`, `CorrelationId`, `CausationId`.
- No `GameSessionEventStream` class (the aggregate owns its uncommitted events list in Step 2).
- No `object Payload` field.
- No generic `IProjection<TEvent, TEntry>` with envelope types.

The envelope and event stream infrastructure live in `WildBunch.Persistence` (Step 3). Domain events are typed facts, period.

## Why typed records, not a generic envelope

The rejected draft's `GameEvent` with `string EventType` and `object Payload` would force:
- Stringly-typed dispatch in projectors: `switch (event.EventType) { case "GameStarted": var p = (GameStarted)event.Payload; ... }` — fragile, no compiler support.
- Generic payload casting: `(GameStarted)event.Payload` — runtime failures, no type safety.
- Storage vocabulary in Domain: `EventId`, `Sequence`, `OccurredAtUtc` are persistence concerns that Domain should not know about.

Typed records with `IDomainEvent` give:
- Pattern matching in projectors: `switch (e) { case GameStarted gs: ...; case StoreItemPurchased p: ...; }` — compiler-checked, exhaustive.
- No casting: the event IS the typed fact.
- Domain purity: no envelope fields on domain events.

## Tasks

- [ ] **Task 1: Add `IDomainEvent` marker interface** in `src/WildBunch.Domain/Events/IDomainEvent.cs` with the XML doc comment referencing ADR-0028.
- [ ] **Task 2: Add `GameStarted` record** with the fields above. Verify field types against existing `WildBunch.Domain` types (`TownId`, `TravelDifficulty`, `TravelRandomnessState`, `AdventureRandomnessPolicy`). Use `required` properties with `init` setters.
- [ ] **Task 3: Add `StoreItemPurchased` record** with the fields above. Verify `ItemKind` and `TownId` types.
- [ ] **Task 4: Write `TypedDomainEventTests`.** Assert: both events implement `IDomainEvent`; all required fields are settable via init; records are immutable (init-only); equality is value-based (record semantics).
- [ ] **Task 5: Verify no envelope fields exist on domain events.** The test asserts that `GameStarted` and `StoreItemPurchased` do NOT have properties named `EventId`, `Sequence`, `OccurredAtUtc`, `SchemaVersion`, `CorrelationId`, or `CausationId` (reflection check). This prevents envelope leakage.

## Validation

- [ ] **V1: `dotnet build`** passes.
- [ ] **V2: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~TypedDomainEventTests"`** passes.
- [ ] **V3: `dotnet test`** (full domain suite) passes — no existing behavior touched.
- [ ] **V4: No envelope fields.** The reflection test in Task 5 confirms no storage vocabulary on domain events.
- [ ] **V5: No persistence/handler/API/`GameSession` changes.** `git status` shows only `src/WildBunch.Domain/Events/**` and `tests/WildBunch.Domain.Tests/Events/TypedDomainEventTests.cs` added.

## Acceptance mapping

- **AC-002 (event sourcing seam visible in code):** partially satisfied. The typed domain events are the domain half of the seam. Step 2 (`Apply` + command-produces-event) and Step 3 (persistence event store) complete it.

## Non-goals for this step

- No `GameSession` changes (Step 2).
- No `Apply` methods (Step 2).
- No persistence/envelope (Step 3).
- No projections (Step 4).
- No handlers (Step 5).
- No API (Step 6).
- No events for non-migrated flows (follow-up issues).
- No replay (Step 2 adds `RehydrateFromEvents`; Step 3 persists events for replay).

## Self-Review

**Spec coverage:** Step 1 delivers typed domain events as plain records with `IDomainEvent` marker, no envelope fields. This is the domain-pure half of AC-002.

**Placeholder scan:** Field types reference existing `WildBunch.Domain` types; the worker verifies during implementation. No TBDs.

**Type consistency:** Sealed records with `required init` properties match the existing `WildBunch.Domain` record style (e.g. `StorePurchaseResult`, `TravelJourneySnapshot`).

**Non-goals:** All eight non-goals preserved.
