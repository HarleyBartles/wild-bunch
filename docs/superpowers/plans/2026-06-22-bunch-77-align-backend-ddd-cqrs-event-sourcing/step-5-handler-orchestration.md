# Step 5 — Command Handler Orchestration (Load → Command → Store → Commit → Project → Safe Return)

> Parent plan: `../2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing.md`
> Acceptance criteria covered: **AC-003** (handlers orchestrate through ports, not domain truth), **AC-006** (representative flows prove the seam end-to-end), **AC-007** (UI-facing bridge ready, source truth remains event stream/projections).

## Goal

Wire the representative command handlers to the full event-sourced seam: load `GameSession` (snapshot + replay) → command (validate + produce event + apply) → **store via repository** (stages snapshot upsert + event append + concurrency check on the same DbContext) → **commit via UoW** (single `SaveChangesAsync` + transaction) → run synchronous projectors → return **safe** projected player events (diary + HUD only).

The handler uses **one persistence path**: `repository.StoreAsync(session, correlationId, ct)` stages everything, then `uow.CommitAsync(ct)` commits. There is no separate event-store append call from the handler. The handler does not inject `IGameSessionEventStore` — that interface does not exist (Step 3 absorbed the event append into `IGameSessionRepository.StoreAsync`).

Handlers do **not** become domain truth. They orchestrate ports: repository (load/store), UoW (commit), projectors (derive outputs). `GameSession` remains the aggregate root and the only place invariants are enforced and state is mutated through `Apply` (AC-003).

## Files

- Modify: `src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs`
- Modify: `src/WildBunch.Application/Games/Commands/PurchaseStoreItemHandler.cs`
- Add: `src/WildBunch.Application/Games/Commands/CommandProjectionResult.cs` — the bridge result shape carrying **safe** projected player events (diary + HUD only, no raw events, no audit).
- Modify: `src/WildBunch.Persistence/DependencyInjection.cs` — register the four projectors (Step 4) in DI.
- Add: `tests/WildBunch.Application.Tests/Games/Commands/HandlerOrchestrationTests.cs`
- Add: `tests/WildBunch.Application.Tests/Games/Commands/ConcurrencyRetryTests.cs`

No changes to `IGameSessionUnitOfWork` — the existing `CommitAsync` is the single save + transaction commit, which is exactly what we need.

## Handler orchestration shape

Every migrated handler follows the same shape — **one store call, one commit call**:

```csharp
public sealed class StartNewGameHandler
{
    private readonly IGameSessionRepository _repository;
    private readonly IGameSessionUnitOfWork _uow;
    private readonly IPlayerDiaryProjection _diaryProjector;
    private readonly IHudFeedProjection _hudProjector;

    public async Task<CommandProjectionResult> Handle(StartNewGameCommand command, CancellationToken ct)
    {
        var correlationId = command.CorrelationId; // or Guid.NewGuid()

        // 1. Command (validate + produce event + apply) — domain truth lives in GameSession
        var session = GameSession.StartNew(/* ...command params... */);

        // 2. Collect events for projection BEFORE marking committed
        var eventsToProject = session.UncommittedEvents.ToList();

        // 3. Store: stages snapshot upsert + event append + concurrency check on DbContext
        //    Does NOT call SaveChangesAsync — the UoW commits.
        await _repository.StoreAsync(session, correlationId, ct);

        // 4. Commit: single SaveChangesAsync + transaction (snapshot + events atomic)
        await _uow.CommitAsync(ct);

        // 5. Mark events committed (clears uncommitted list; state unchanged)
        session.MarkEventsCommitted();

        // 6. Project safe player-facing outputs from the events produced by this command
        var diary = _diaryProjector.Project(eventsToProject);
        var hud = _hudProjector.Project(eventsToProject);

        // 7. Return safe bridge result (diary + HUD only — no raw events, no audit)
        return new CommandProjectionResult
        {
            SessionId = session.Id.Value,
            Message = "The hunt begins.", // existing message preserved
            DiaryEntries = diary,
            HudEntries = hud
        };
    }
}
```

Key points:
- **No `IGameSessionEventStore` injection.** The handler injects `IGameSessionRepository` and `IGameSessionUnitOfWork` only (plus projectors). The event append is inside `StoreAsync`.
- **One store call, one commit call.** `StoreAsync` stages; `CommitAsync` commits. No double append, no ambiguity.
- **Events collected before `MarkEventsCommitted`.** The handler copies `UncommittedEvents` to a local list before calling `MarkEventsCommitted`, so projectors can run after commit without seeing an empty list.
- **`ConcurrencyException` propagates from `StoreAsync`** (the stage-time version check). For `StartNewGame` (new session), concurrency conflict is unlikely (new stream id). For `PurchaseStoreItem` (existing session), the handler retries.

## `CommandProjectionResult` — safe bridge only

```csharp
public sealed class CommandProjectionResult
{
    public Guid SessionId { get; init; }
    public string Message { get; init; }  // existing message preserved

    // Safe player-facing projections only:
    public IReadOnlyList<PlayerDiaryEntry> DiaryEntries { get; init; }
    public IReadOnlyList<HudFeedEntry> HudEntries { get; init; }

    // NOT included:
    // - No IReadOnlyList<IDomainEvent> Events
    // - No AuditEntries
    // - No CaseFileEntries (or only safe subset, if Harley approves in Step 6)
    // - No raw payloads
}
```

The handler result does **not** expose raw events, audit entries, or case-file internal truth. Step 6 maps this to the API response.

## Optimistic concurrency retry

For `PurchaseStoreItemHandler` (existing session), `StoreAsync` throws `ConcurrencyException` if the stream version changed between load and store. The handler catches it, reloads, and re-executes:

```csharp
public async Task<CommandProjectionResult> Handle(PurchaseStoreItemCommand command, CancellationToken ct)
{
    var correlationId = command.CorrelationId;
    const int maxRetries = 3;

    for (var attempt = 0; attempt < maxRetries; attempt++)
    {
        // 1. Load (snapshot + replay)
        var session = await _repository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null) return CommandProjectionResult.Failed("Session not found.");

        // 2. Command (validate + produce + apply)
        var result = session.Purchase(offer, quantity);
        if (!result.IsSuccess) return CommandProjectionResult.Failed(result.Message);

        // 3. Collect events for projection
        var eventsToProject = session.UncommittedEvents.ToList();

        try
        {
            // 4. Store: stages snapshot + events + concurrency check (throws if stale version)
            await _repository.StoreAsync(session, correlationId, ct);

            // 5. Commit: single SaveChangesAsync + transaction
            await _uow.CommitAsync(ct);

            // 6. Mark committed
            session.MarkEventsCommitted();

            // 7. Project safe outputs
            var diary = _diaryProjector.Project(eventsToProject);
            var hud = _hudProjector.Project(eventsToProject);

            return new CommandProjectionResult
            {
                SessionId = session.Id.Value,
                Message = result.Message,
                DiaryEntries = diary,
                HudEntries = hud
            };
        }
        catch (ConcurrencyException) when (attempt < maxRetries - 1)
        {
            // Retry: reload (gets latest version), re-execute command, re-store
            continue;
        }
    }

    return CommandProjectionResult.Failed("Concurrency conflict — please retry.");
}
```

The retry loop reloads the session (getting the latest version), re-executes the command, and re-stores. If the command's validation still passes on the reloaded state, the retry succeeds. If validation fails (e.g., another purchase drained the wallet), the retry returns a normal failure.

**Important:** the `DbContext` is scoped per request. If `StoreAsync` throws `ConcurrencyException`, the staged changes on the `DbContext` must be discarded before retry. The worker verifies whether the existing `DbContext` needs explicit `ChangeTracker.Clear()` or whether the retry naturally re-stages. If the DbContext state is dirty after a `ConcurrencyException`, the handler may need to reset it or the retry must use a fresh DbContext scope. The worker resolves this during implementation and documents the chosen approach.

## What handlers do **not** do

- Handlers do not mutate `GameSession` state directly. They call `GameSession` command methods.
- Handlers do not invent events. Events come from `session.UncommittedEvents` after the command method.
- Handlers do not call `SaveChangesAsync`. The UoW does that.
- Handlers do not inject or call a separate event store. The repository's `StoreAsync` handles event append.
- Handlers do not write to `GameLogEntry` or `TravelDiaryDayState`. Those are domain-owned.
- Handlers do not read aggregate internals for query results. Query handlers (separate) read projections.
- Handlers do not expose raw events, audit, or case-file internal truth in their results.
- Handlers do not become domain truth. They are orchestration.

## Non-migrated handlers

Handlers for non-migrated flows (`TravelToTownHandler`, `AdvanceTravelDayHandler`, `AcknowledgeJourneyArrivalHandler`, wanted-poster/clue/declaration handlers) keep their existing orchestration: load → mutate → store snapshot → return. They call `StoreAsync` with an empty uncommitted-events list (or the existing `StoreAsync` signature if not yet updated). They do not project. They are clearly marked as not-yet-migrated. Follow-up issues migrate them.

**Note on `StoreAsync` signature change:** Step 3 adds a `correlationId` parameter to `StoreAsync`. Non-migrated handlers pass `Guid.Empty` or a generated correlation id — they have no events to append, so the correlation id is unused for them. The worker updates all existing handler call sites to match the new signature.

## Tasks

- [ ] **Task 1: Add `CommandProjectionResult`** with safe fields only (diary + HUD + existing message). No raw events, no audit.
- [ ] **Task 2: Register the four projectors in DI** (`DependencyInjection.cs`).
- [ ] **Task 3: Refactor `StartNewGameHandler`** to the orchestration shape: command → collect events → `StoreAsync(session, correlationId, ct)` → `CommitAsync(ct)` → `MarkEventsCommitted` → project diary + HUD → return safe result.
- [ ] **Task 4: Refactor `PurchaseStoreItemHandler`** to the orchestration shape with optimistic concurrency retry loop.
- [ ] **Task 5: Update non-migrated handler call sites** to match the new `StoreAsync` signature (pass `Guid.Empty` or generated correlation id).
- [ ] **Task 6: Resolve DbContext state after `ConcurrencyException`.** Verify whether `ChangeTracker.Clear()` is needed before retry, or whether re-staging naturally overwrites. Document the chosen approach.
- [ ] **Task 7: Write `HandlerOrchestrationTests`.** For each migrated flow, assert: (a) events are collected and staged via `StoreAsync`, (b) `CommitAsync` is called once, (c) projectors produce expected diary + HUD entries, (d) result has diary + HUD but no raw events/audit, (e) existing `Message` is preserved.
- [ ] **Task 8: Write `ConcurrencyRetryTests`.** Simulate a `ConcurrencyException` on first `StoreAsync`; assert the handler retries, reloads, and succeeds (or fails gracefully if validation no longer passes).

## Validation

- [ ] **V1: `dotnet build`** passes.
- [ ] **V2: `dotnet test`** (full suite) passes.
- [ ] **V3: `.\scripts\postgres-dev.ps1 test -- dotnet test`** — PostgreSQL-backed tests pass.
- [ ] **V4: No domain changes.** `git status` shows only `WildBunch.Application/**` and `WildBunch.Persistence/DependencyInjection.cs` modified, plus test files. `GameSession.cs` is not touched.
- [ ] **V5: Result safety.** `HandlerOrchestrationTests` asserts the result does not contain raw events or audit entries (reflection or type check).
- [ ] **V6: Single persistence path.** Grep handler code for `_eventStore` — no matches. The handler injects `_repository` and `_uow` only.
- [ ] **V7: No `SaveChangesAsync` in handler code.** Grep handler code for `SaveChangesAsync` — no matches. The UoW does the save.

## Acceptance mapping

- **AC-003:** handlers orchestrate through ports (repository, UoW, projectors). `GameSession` remains the aggregate root. `Apply` methods are the single mutation path for migrated state. No event-store interface in the handler — the repository absorbs the append.
- **AC-006:** the two migrated flows prove the seam end-to-end: command → event → apply → store (stages snapshot + events) → commit → project → safe return.
- **AC-007:** the bridge result shape is ready for Step 6's API exposure. Source truth remains the event stream + projections, not the synchronous response.

## Non-goals for this step

- No API endpoint changes (Step 6).
- No projection persistence (in-memory projectors).
- No SignalR transport.
- No replay-from-events production load path (snapshot + replay is the production path).
- No `GameSession` domain changes (Step 2 already done).
- No migration of non-migrated handlers (follow-up issues).
- No removal of existing result fields (backward compatibility).
- No case-file view in the result (safety; Step 6 may add a safe subset if approved).
- No separate `IGameSessionEventStore` injection — the repository handles event append.

## Self-Review

**Spec coverage:** Step 5 covers AC-003 (orchestration through ports), AC-006 (end-to-end seam), and AC-007 (safe bridge shape). The single persistence path (repository + UoW) and the safe-only result are the key corrections.

**Onion dependency direction:** The handler injects `IGameSessionRepository` and `IGameSessionUnitOfWork` (both in Application.Abstractions) and projector interfaces (in Application.Projections). No Persistence-layer types injected. ✅

**Single append ownership:** `StoreAsync` stages snapshot + events. `CommitAsync` commits. No separate append call. No double append. ✅

**UoW atomicity:** `StoreAsync` stages without saving. `CommitAsync` is the single save + transaction. No independent save in handler or event-store code. ✅

**Non-goals:** All nine non-goals preserved.
