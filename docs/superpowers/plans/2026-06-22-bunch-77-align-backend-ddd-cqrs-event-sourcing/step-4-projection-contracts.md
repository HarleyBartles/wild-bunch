# Step 4 — Projection Contracts and Reference Projectors (Pattern Matching on Typed Events)

> Parent plan: `../2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing.md`
> Acceptance criteria covered: **AC-004** (diary, HUD feed, case file, and audit separated as projections/read models).

## Goal

Define the four first-class projection contracts from ADR-0028 as pure interfaces in `WildBunch.Application.Projections`, plus in-memory reference projectors that derive projection outputs from **typed domain events** (`IDomainEvent`) via **pattern matching** on the concrete event type. No string dispatch, no generic payload casting.

The four projections are **separate**: each has its own interface, output type, and projector. They share the typed event list as input but do not share output types or narration logic. This is the core of AC-004.

**Safety boundary:** the case file view projection exposes only public clues and warrants — never hidden culprit truth (per ADR-0007). The audit projection is exhaustive but is **not exposed to player-facing API** (Step 6 enforces this).

## Files

- Add: `src/WildBunch.Application/Projections/IProjection.cs` — base interface typed over `IDomainEvent` input.
- Add: `src/WildBunch.Application/Projections/IPlayerDiaryProjection.cs`
- Add: `src/WildBunch.Application/Projections/IHudFeedProjection.cs`
- Add: `src/WildBunch.Application/Projections/ICaseFileViewProjection.cs`
- Add: `src/WildBunch.Application/Projections/IAuditProjection.cs`
- Add: `src/WildBunch.Application/Projections/Models/PlayerDiaryEntry.cs`
- Add: `src/WildBunch.Application/Projections/Models/HudFeedEntry.cs`
- Add: `src/WildBunch.Application/Projections/Models/CaseFileViewEntry.cs`
- Add: `src/WildBunch.Application/Projections/Models/AuditEntry.cs`
- Add: `src/WildBunch.Application/Projections/PlayerDiaryProjector.cs`
- Add: `src/WildBunch.Application/Projections/HudFeedProjector.cs`
- Add: `src/WildBunch.Application/Projections/CaseFileViewProjector.cs`
- Add: `src/WildBunch.Application/Projections/AuditProjector.cs`
- Add: `tests/WildBunch.Application.Tests/Projections/` (create the test project if it does not exist)
  - `PlayerDiaryProjectorTests.cs`
  - `HudFeedProjectorTests.cs`
  - `CaseFileViewProjectorTests.cs`
  - `AuditProjectorTests.cs`
  - `ProjectionNarrationTenseTests.cs` — AC-004 separation proof.
  - `ProjectionSafetyTests.cs` — asserts case file view never exposes hidden truth fields; audit is not player-facing.

## Projection contract shape

```csharp
public interface IProjection<TEntry>
{
    IReadOnlyList<TEntry> Project(IReadOnlyList<IDomainEvent> events);
}

public interface IPlayerDiaryProjection : IProjection<PlayerDiaryEntry> { }
public interface IHudFeedProjection  : IProjection<HudFeedEntry> { }
public interface ICaseFileViewProjection : IProjection<CaseFileViewEntry> { }
public interface IAuditProjection    : IProjection<AuditEntry> { }
```

Input is `IReadOnlyList<IDomainEvent>` — typed domain events from the aggregate. Projectors pattern-match on the concrete type. No `object`, no string dispatch, no envelope.

## Output entry shapes

```csharp
public sealed record PlayerDiaryEntry
{
    public required int Sequence { get; init; }      // event order in the stream
    public required int Day { get; init; }            // game day, derived from event or stream context
    public required string Narrative { get; init; }   // first-person past tense
}

public sealed record HudFeedEntry
{
    public required int Sequence { get; init; }
    public required string Notice { get; init; }      // second-person present tense
    public required string Severity { get; init; }    // "info" | "warning" | "danger"
}

public sealed record CaseFileViewEntry
{
    public required int Sequence { get; init; }
    public required string EvidenceKind { get; init; }   // "purchase" | "warrant" | "clue" | "declaration"
    public required string EvidenceSummary { get; init; } // neutral, safe — no hidden truth
    public required IReadOnlyDictionary<string, string> Attributes { get; init; }
}

public sealed record AuditEntry
{
    public required int Sequence { get; init; }
    public required string EventType { get; init; }      // concrete type name
    public required string PayloadJson { get; init; }    // exhaustive technical representation
}
```

Note: projection entries do **not** carry `EventId`, `CorrelationId`, `CausationId`, or `OccurredAtUtc` unless the projection's audience needs them. The diary and HUD entries carry `Sequence` (event order) and `Day` (game day) — what the player sees. The audit entry carries `EventType` and `PayloadJson` — what a developer/replay tool needs. The envelope's infrastructure metadata is not leaked into player-facing projections.

## Narration rules (projection-time, pattern-matched)

Each projector switches on the typed event:

```csharp
public sealed class PlayerDiaryProjector : IPlayerDiaryProjection
{
    public IReadOnlyList<PlayerDiaryEntry> Project(IReadOnlyList<IDomainEvent> events)
    {
        var entries = new List<PlayerDiaryEntry>();
        var sequence = 0;
        foreach (var e in events)
        {
            sequence++;
            switch (e)
            {
                case GameStarted gs:
                    entries.Add(new PlayerDiaryEntry
                    {
                        Sequence = sequence,
                        Day = 1,
                        Narrative = $"I arrived in {gs.StartingTownName} with ${gs.StartingWallet} in my pocket and a hunt on my mind."
                    });
                    break;
                case StoreItemPurchased p:
                    entries.Add(new PlayerDiaryEntry
                    {
                        Sequence = sequence,
                        Day = 1, // or derived from game clock when available
                        Narrative = $"I bought {p.Quantity} {p.DisplayName} for ${p.TotalPrice:0.00}."
                    });
                    break;
                // Non-migrated event types are not yet handled — skipped.
                // As follow-up issues add events, they add cases here.
            }
        }
        return entries;
    }
}
```

Examples for `StoreItemPurchased` across all four projectors:

- **Diary (first-person past):** `"I bought 3 rounds of ammunition for $6.00."`
- **HUD (second-person present):** `"You buy 3 rounds of ammunition for $6.00."` severity `info`.
- **Case file view:** evidenceKind `purchase`, summary `"Store purchase: 3× ammunition, $6.00 total."`, attributes `{ town, itemKind, unitPrice }`. No hidden truth.
- **Audit:** eventType `StoreItemPurchased`, payloadJson (full serialized event). Developer-only, not player-facing.

Narration strings live in the projectors, **not** in `GameSession` or the event payloads. This corrects the "raw strings inside domain/application flow" risk surface from the brief.

## Safety boundary (AC-004 + ADR-0007)

The `CaseFileViewProjector` exposes only **public** evidence:
- For `GameStarted`: no case file entry (starting a game is not case evidence).
- For `StoreItemPurchased`: a `purchase` evidence entry with town/item/price — public transaction record.
- For future `ClueDiscovered` / `WantedPosterRead` events (follow-up issues): only public clue/warrant details, never hidden culprit truth.

The `ProjectionSafetyTests` assert that `CaseFileViewEntry` never contains fields named `HiddenCulprit`, `CulpritId`, `HiddenTruth`, or similar — the projection output model is safe by construction.

The `AuditProjector` is exhaustive but its output (`AuditEntry`) is **not** included in player-facing API responses (Step 6 enforces this). Audit is a developer/replay surface only.

## Tasks

- [ ] **Task 1: Check whether `tests/WildBunch.Application.Tests` exists.** Create it if not, referencing `WildBunch.Application` and `WildBunch.Domain`, matching the existing test project style.
- [ ] **Task 2: Add the `Projections` folder and the four output entry records.** Sealed records, immutable, POCO.
- [ ] **Task 3: Add the `IProjection<TEntry>` base interface and the four specific interfaces.**
- [ ] **Task 4: Implement `AuditProjector`.** Every event becomes one `AuditEntry` with `EventType` and `PayloadJson`. Uses `System.Text.Json`.
- [ ] **Task 5: Implement `PlayerDiaryProjector`.** Pattern-match on `GameStarted` and `StoreItemPurchased`. First-person past tense.
- [ ] **Task 6: Implement `HudFeedProjector`.** Pattern-match on the two event types. Second-person present tense with severity.
- [ ] **Task 7: Implement `CaseFileViewProjector`.** Pattern-match on the two event types. Safe, neutral, no hidden truth.
- [ ] **Task 8: Write the four projector test files.** For `GameStarted` and `StoreItemPurchased`, assert correct entries with correct tense, content, and fields.
- [ ] **Task 9: Write `ProjectionNarrationTenseTests`.** Feed the same `StoreItemPurchased` event to all four projectors; assert each output matches its tense/voice rule. AC-004 separation proof.
- [ ] **Task 10: Write `ProjectionSafetyTests`.** Assert case file view entries never contain hidden-truth field names. Assert audit entries are not player-facing (the test documents the safety contract; Step 6 enforces it at the API).

## Validation

- [ ] **V1: `dotnet build`** passes.
- [ ] **V2: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~Projections"`** passes.
- [ ] **V3: `dotnet test`** (full suite) passes — no existing behavior touched.
- [ ] **V4: No persistence/handler/API/`GameSession` changes.** `git status` shows only `src/WildBunch.Application/Projections/**` and `tests/WildBunch.Application.Tests/Projections/**`.
- [ ] **V5: Pattern matching, not string dispatch.** Grep projectors for `switch (e)` and confirm no `switch (event.EventType)` or string-based dispatch.

## Acceptance mapping

- **AC-004 (diary, HUD feed, case file, and audit separated as projections):** satisfied by four distinct interfaces, output types, projectors, and the narration-tense test. Player diary is curated narrative; full audit is exhaustive technical history. Case file view is safe (no hidden truth). Audit is not player-facing.

## Non-goals for this step

- No persistence of projections (in-memory reference projectors; a projection store is a future step).
- No handler wiring (Step 5).
- No API exposure (Step 6).
- No `GameSession` changes (Step 2 already added events).
- No `GameLogEntry` removal (Step 7).
- No SignalR transport.
- No UI slice.
- No projection rebuild-from-event-store (Step 5 wires projectors to handler-collected events; a projection store is a future step).

## Self-Review

**Spec coverage:** Step 4 covers AC-004 fully with four separate projections, pattern-matched narration, and safety boundaries.

**Placeholder scan:** Narration strings are sketched; the worker writes the full set during implementation. No TBDs.

**Type consistency:** Output entries are sealed records matching the existing `WildBunch.Application` DTO style. Projectors are sealed classes implementing typed interfaces. No `object`, no string dispatch.

**Non-goals:** All eight non-goals preserved.
