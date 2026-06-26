# Step 8 — Tests, Validation, and Campaign Closeout

> Parent plan: `../2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing.md`
> Acceptance criteria covered: **AC-006** (representative flows prove the seam end-to-end), and final validation for **all ACs (AC-001 through AC-007)**.

## Goal

Consolidate the test coverage from Steps 1–7 into a coherent end-to-end proof that Event Sourcing is **materially true** for the migrated slice, run the full repo validation lanes, capture the campaign return evidence, and produce follow-up issue recommendations. This step adds the final integration tests that prove the full seam (command → event → apply → append → replay → project → safe API) and runs every validation command required by AGENTS.md.

After this step passes, ADR-0028 is promoted from `planned` to `live`.

## Files

- Add: `tests/WildBunch.Domain.Tests/Events/EventSourcedReplayProofTests.cs` — the core material proof: command path and replay path produce identical state.
- Add: `tests/WildBunch.Persistence.Tests/EventStore/EndToEndEventSourcingTests.cs` — integration proof (PostgreSQL): store via command → load events → replay → state matches.
- Add: `tests/WildBunch.Application.Tests/Games/Commands/EndToEndHandlerProjectionTests.cs` — handler produces safe projections from real events.
- Add: `tests/WildBunch.Api.Tests/SafeApiBridgeEndToEndTests.cs` — API response has diary + HUD, no raw events/audit.
- Modify: `docs/adr/ADR-0028-*.md` — promote to `live`; finalize Implementation Status and Proof of Implementation.
- Modify: `.agents/superpowers/plans/2026-06-22-bunch-77-align-backend-ddd-cqrs-event-sourcing.md` — mark step checkboxes complete.

## The three core proofs

### Proof 1: Replay reconstructs state (material Event Sourcing)

```
// Domain-level
var sessionA = GameSession.StartNew("Doc", world, caseFile, townId, null, null, TravelDifficulty.Normal);
sessionA.Purchase(offer, 3);

var events = sessionA.UncommittedEvents;
var sessionB = GameSession.RehydrateFromEvents(sessionA.Id, world, caseFile, events);

Assert.Equal(sessionA.Player.Wallet.Amount, sessionB.Player.Wallet.Amount);
Assert.Equal(sessionA.Player.Inventory, sessionB.Player.Inventory);
Assert.Equal(sessionA.Status, sessionB.Status);
Assert.Equal(sessionA.TravelDifficulty, sessionB.TravelDifficulty);
```

```
// Persistence-level (PostgreSQL) — uses repository path, no separate event store
var session = GameSession.StartNew(...);
session.Purchase(offer, 3);
await repository.StoreAsync(session, correlationId, ct);  // stages snapshot + events
await uow.CommitAsync(ct);                                 // single save + transaction

var loadedEvents = await repository.GetEventStreamAsync(session.Id, ct); // typed IDomainEvent list
var replayed = GameSession.RehydrateFromEvents(session.Id, world, caseFile, loadedEvents);

Assert.Equal(session.Player.Wallet.Amount, replayed.Player.Wallet.Amount);
Assert.Equal(session.Player.Inventory, replayed.Player.Inventory);
```

This is the proof that Event Sourcing is materially true: the event stream (read via `GetEventStreamAsync`) reconstructs state without the snapshot. No separate event-store interface is used — the repository is the single persistence port.

### Proof 2: Optimistic concurrency works

```
// Two handlers load the same session concurrently
var session1 = await repository.GetByIdAsync(id, ct);
var session2 = await repository.GetByIdAsync(id, ct);

session1.Purchase(offerA, 1);
await repository.StoreAsync(session1, correlationId, ct);  // stages with expected version
await uow.CommitAsync(ct);                                 // commits — stream version advances

session2.Purchase(offerB, 1);
// session2's expected version is stale — StoreAsync throws ConcurrencyException
await Assert.ThrowsAsync<ConcurrencyException>(() =>
    repository.StoreAsync(session2, correlationId, ct));
```

This is the proof that optimistic concurrency prevents lost updates. The concurrency check is inside `StoreAsync` (the single persistence path), not a separate event-store append.

### Proof 3: API is safe

```
var response = await client.PostAsync("/games/start", jsonContent);
var body = await response.Content.ReadFromJsonAsync<StartNewGameResponse>();

Assert.NotNull(body.DiaryEntries);
Assert.NotNull(body.HudEntries);
Assert.NotEmpty(body.DiaryEntries);
Assert.NotEmpty(body.HudEntries);

// Safety: no raw events, no audit, no payloads
Assert.True(body.GetType().GetProperty("Events") is null);
Assert.True(body.GetType().GetProperty("AuditEntries") is null);
Assert.True(body.GetType().GetProperty("CaseFileEntries") is null);
```

This is the proof that the player-facing API exposes only safe projections.

## Validation commands (per AGENTS.md)

- [ ] **V1: `dotnet build`** — clean build.
- [ ] **V2: `dotnet test`** — full suite passes.
- [ ] **V3: `dotnet tool restore`** — before EF commands.
- [ ] **V4: `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`** — `AddEventStore` migration registered.
- [ ] **V5: `.\scripts\postgres-dev.ps1 ensure`** — PostgreSQL healthy.
- [ ] **V6: `.\scripts\postgres-dev.ps1 validate`** — primary validation lane (provisions cluster, exports connection string, restores tools, runs EF + test checks).
- [ ] **V7: `git status`** — clean worktree.
- [ ] **V8: `git diff --check`** — whitespace/conflict check.

## Cleanup proof (per AGENTS.md GREEN standard)

- [ ] **C1–C6:** List/stop/scan worker-owned helpers, ports, file locks. Do not stop shared PostgreSQL service (`localhost:5434`).

## ADR-0028 promotion

- [ ] **A1: Promote to `live`** with Dated Status History entry.
- [ ] **A2: Finalize Implementation Status** with actual PR numbers.
- [ ] **A3: Finalize Proof of Implementation** with source surfaces and test evidence.
- [ ] **A4: Finalize Related Stable Source Surfaces** with actual paths.

## Campaign return evidence

- [ ] **R1: Branch, head commit, PR URL(s).**
- [ ] **R2: Changed file list grouped by Domain, Application, Infrastructure, API, Web, tests, docs.**
- [ ] **R3: AC summary** (AC-001 through AC-007: fully / partially / deferred + follow-up recommendations).
- [ ] **R4: Validation commands and results.**
- [ ] **R5: Explicit note: no Linear delegation, no `!`-prefixed labels.**
- [ ] **R6: Cleanup proof** (C1–C6).
- [ ] **R7: Issue-goal conformance notes.**
- [ ] **R8: Known caveats / next recommended slice.**

## Follow-up issue recommendations

- **Follow-up 1:** Migrate wanted poster read (`WantedPosterRead` event + `Apply` + replay).
- **Follow-up 2:** Migrate clue discovery/investigation (`ClueDiscovered` event).
- **Follow-up 3:** Migrate wrong saloon wanted declaration (`WantedDeclarationRejected`/`Accepted`).
- **Follow-up 4:** Migrate travel start/day/arrival (`JourneyStarted`, `TravelDayAdvanced`, `JourneyArrived`).
- **Follow-up 5:** Full production replay-from-events load path (BUNCH-3 child).
- **Follow-up 6:** Remove legacy `GameLogEntry` pathway; switch `LogEntries` DTO to projection-derived.
- **Follow-up 7:** Projection persistence store.
- **Follow-up 8:** SignalR transport for projected events.
- **BUNCH-67:** Sub-aggregate splits consume the typed event vocabulary and `Apply`/replay mechanism.

## Tasks

- [ ] **Task 1: Write the four consolidated end-to-end test files** (replay proof, concurrency proof, handler projection proof, API safety proof).
- [ ] **Task 2: Run V1–V8 validation.**
- [ ] **Task 3: Run cleanup proof C1–C6.**
- [ ] **Task 4: Promote ADR-0028 to `live` (A1–A4).**
- [ ] **Task 5: Assemble return evidence (R1–R8).**
- [ ] **Task 6: Update master plan with final step status.**
- [ ] **Task 7: Verify no Linear delegation or `!`-prefixed labels.**

## Acceptance mapping (final)

- **AC-001:** ADR-0028 `live` with full implementation status and proof. ✅
- **AC-002:** Typed domain events (no envelope), `Apply` methods, event store with optimistic concurrency, replay proven. ✅
- **AC-003:** `GameSession` remains aggregate root; `Apply` is the single mutation path for migrated state; handlers orchestrate ports. ✅
- **AC-004:** Four separate projections, pattern-matched, with safety boundaries and tense proof. ✅
- **AC-005:** `GameLogEntry` `[Obsolete]`, `#pragma`-confined, `LegacyLogProjector` replacement. ✅
- **AC-006:** Two flows (start game, purchase) fully event-sourced end-to-end: command → event → apply → append → replay → project → safe API. Other flows deferred with follow-up issues. ✅ (partial — two of six flows; breadth adjusted per Harley's feedback).
- **AC-007:** API exposes diary + HUD only. No raw events, no audit, no payloads. Source truth is event stream + projections. ✅

## Non-goals for this step

- No new production code beyond consolidated test files.
- No frontend changes.
- No SignalR transport.
- No replay-from-events production load path (BUNCH-3 follow-up).
- No Linear issue closure.
- No `!`-prefixed labels.

## Self-Review

**Spec coverage:** Step 8 closes out all seven ACs with three core proofs (replay, concurrency, safety), full validation, ADR promotion, and return evidence.

**Event Sourcing materiality:** The replay proof demonstrates that events reconstruct state without snapshot. The concurrency proof demonstrates optimistic concurrency. The safety proof demonstrates the API is safe. This is materially true Event Sourcing for the migrated slice.

**Non-goals:** All six non-goals preserved.

---

## Campaign complete

Once Step 8 passes and return evidence is assembled, the BUNCH-77 campaign is ready for Harley's review. The worker returns the evidence block and does not close any Linear issue.
