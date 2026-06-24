# BUNCH-78: Architecture Stack Refinement — Campaign Overview

> **For agentic workers:** This is the master overview for BUNCH-78. Phase plans are in separate documents:
> - Phase 1: `2026-06-22-bunch-78-phase1-response-bridge.md`
> - Phase 2: `2026-06-22-bunch-78-phase2-investigation-events.md`

**Goal:** Bring the Wild Bunch backend closer to the live architecture stack (ADR-0028) by (1) adding safe projection output to migrated command responses and (2) migrating the investigation/case-update seam to typed domain events with `Apply` mutation and orchestration normalization.

**Base:** `main` at `125e45c` (clean worktree)

**Approval gate:** Approved by Harley with 6 source-level amendments (incorporated into Phase 1 and Phase 2 plans). Proceeding with execution in a single PR.

---

## Preflight Summary (from current `main`, verified against source)

### Architecture stack (A1-A7)

- **ADR-0028** is `live`. Declares Onion + DDD + CQRS + Event Sourcing for migrated flows + projection taxonomy. File: `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`.
- **2 migrated flows:** `StartNew` → `GameStarted` event + `Apply(GameStarted)`; `Purchase` → `StoreItemPurchased` event + `Apply(StoreItemPurchased)`. Both in `GameSession.cs` lines 236-261.
- **18 `AddLogEntry` call sites** remain (all pre-ADR-0028 legacy). No new sites added after ADR-0028. `AddLogEntry` is `[Obsolete]` at line 1987.
- **Event persistence envelope** in `EfGameSessionRepository.StoreAsync` (lines 34-130): appends `StoredEventEntity` rows, updates snapshot cache. Domain never sees envelope. `IGameSessionRepository.GetEventStreamAsync` is on the port interface (line 21).
- **Optimistic concurrency** in `StoreAsync` (lines 54-60): version mismatch throws `ConcurrencyException`. Retry in `GameSessionCommandHandler.ExecuteWithRetryAsync` (max 3). 4 tests prove it.
- **Safe projection endpoints:** `GET /api/games/{id}/projections/hud` and `/diary`. `HudProjector` and `DiaryProjector` are pure functions over `IReadOnlyList<IDomainEvent>`. Raw events, full audit, hidden truth NOT exposed.
- **Command responses are legacy DTOs:** `StartNewGameHandler` returns `GameSessionDto`, `PurchaseStoreItemHandler` returns `GameTurnResultDto`. Both include `LogEntries` (marked `[Obsolete]`). ADR-0028 §10 line 98 explicitly calls for this follow-up.
- **`LegacyLogProjector`** mentioned in ADR-0028 §12 but NOT implemented in source — ADR-to-source gap.

### Player-facing legacy surface (B1-B7)

- `GameSessionDto.LogEntries` (`GameDtos.cs` line 23), `JournalDto.LogEntries` (`JournalDto.cs` line 11).
- Frontend: `GameSessionDto.logEntries` (`types.ts` line 448), `JournalDto.logEntries` (line 495).
- 4 API routes return DTOs with `LogEntries`. 4 UI components consume it (all through `LogPanel`).
- `GameTurnResultDto` (`GameDtos.cs` lines 129-136) has no projection fields. `GameTurnResultFactory` (lines 1-33) doesn't compute projections.
- Additive shape: add optional `HudProjection?` and `DiaryProjection?` to `GameSessionDto`. Backward-compatible.

### AddLogEntry map (C1-C6)

- 18 call sites: 12 pure narration, 6 coupled to state mutation. See Phase 2 plan for the investigation-specific sites.
- Lowest-risk migration candidate: investigation methods (5 methods, coherent pattern, route through `RecordCaseUpdate`).
- Travel/encounter (12 sites): too broad, requires journey event model. Deferred.
- No post-ADR-0028 new `AddLogEntry` sites.

### Handler orchestration (D1-D5)

- 2 handlers use `GameSessionCommandHandler` base (StartNewGame, PurchaseStoreItem).
- 14 handlers manually load/mutate/store/commit.
- **Critical finding:** `ExecuteWithRetryAsync` only stores when `UncommittedEvents.Count > 0` (line 56). Non-migrated flows don't produce events, so they can't use the base class without first being migrated to typed events. **Orchestration normalization is coupled to event migration — can't do one without the other.**
- 6 investigation handlers return projection-safe DTOs (via `JournalMapper`). Good candidates for migration.
- `StoreAsync` updates the snapshot ALWAYS (lines 89-127), even without events. But the base class gates on events.

---

## Phase Selection

### Phase 1: Migrated-flow response bridge

Add optional `HudProjection` and `DiaryProjection` fields to `GameSessionDto`. Populate them in `StartNewGameHandler` and `PurchaseStoreItemHandler` from the committed event stream. Update frontend types. Add guardrail test against new `AddLogEntry` call sites. Update ADR-0028.

**Why first:** Closes the explicit ADR-0028 §10 follow-up. Additive, no domain changes, no new events. Provable with existing test infrastructure. See `2026-06-22-bunch-78-phase1-response-bridge.md`.

### Phase 2: Investigation/case-update event migration

Migrate 5 investigation methods (`GatherLocalGossip`, `FollowTelegraphLeads`, `InspectNoticeBoard`, `CheckSheriffRecords`, `ReadWantedPosters`) to typed domain events + `Apply` mutation + orchestration normalization. Move their 5 handlers onto `GameSessionCommandHandler`. Update projectors and replay dispatcher.

**Why this is the best Phase 2 value/risk tradeoff:**

1. **Closes a real architecture gap:** Investigation is the second-largest coherent behavioral seam after travel. It has 5 methods following the same pattern, all routing through `RecordCaseUpdate` (clock advance + `AddLogEntry`). Migrating them moves 5 of 14 manual handlers onto orchestration and produces real typed events for replay.

2. **Safe hidden-truth boundary:** `RevealNextPublicClue` and `RevealNextPublicWarrant` only reveal PUBLIC clues/warrants. The hidden culprit `SuspectId` in `CaseFile` is never exposed. Events carry only public clue/warrant data. Verified at `CaseFile.cs` lines 287-371.

3. **Bounded refactoring:** The decide-vs-apply separation requires adding peek methods to `CaseFile` (`PeekNextPublicClue`, `PeekNextPublicWarrant`) and using existing `IsSpent`/`WantedPostersSpent` as non-mutating source checks. The Apply method handles source marking, clock advance, clue/warrant discovery, and legacy log entry. One event type (`InvestigationPerformed`) covers all 5 methods.

4. **Orchestration comes for free:** Once the domain methods produce events, the handlers can use `ExecuteWithRetryAsync` (which gates on `UncommittedEvents.Count > 0`). No behavior change — the handlers already check `SessionChanged` which is equivalent.

5. **Lower risk than travel:** Travel has 12+ `AddLogEntry` sites, complex multi-day state machine, encounter resolution with health/wallet/ammo/heat/horse mutations, and rich `TravelDiaryDayState` overlap. Investigation has 5 methods, one mutation pattern, and projection-safe return types.

**Why not other Phase 2 candidates:**

- **Orchestration normalization alone:** Impossible without event migration. The base class gates storage on `UncommittedEvents.Count > 0`. Non-migrated flows don't produce events.
- **Travel/journey event slice:** Highest architecture value but too broad for this PR. 12+ call sites, multi-day state machine, encounter resolution. High mini-rewrite risk. Shaped as bounded follow-up issue.
- **`LegacyLogProjector` implementation:** ADR-0028 §12 mentions it but it's not needed for the response bridge or investigation migration. The guardrail test + transitional `AddLogEntry` in `Apply` methods is sufficient. Deferred to the deprecation follow-up.

---

## Non-goals (still too large or unsafe for this PR)

1. **Travel/journey event sourcing** — 12+ `AddLogEntry` sites, multi-day journey state machine, encounter resolution (run/fight/bribe) with health/wallet/ammo/heat/horse mutations. Requires journey event model design. High mini-rewrite risk. Bounded follow-up issue proposed.
2. **`LookAroundSaloon` migration** — has citizen edge case (no clock advance, no `RecordCaseUpdate`) and is coupled to `BountyLoopCoordinator` via saloon person of interest state. Deferred to the bounty-loop follow-up.
3. **Bounty-loop event migration** (`TurnInToSheriff`, `ConfrontWantedSuspect`, `ConfrontSaloonPersonOfInterest`, `ConfrontSaloonWantedSuspect`) — routes through `BountyLoopCoordinator` with complex confrontation/settlement state. Separate seam, separate follow-up.
4. **Removing `LogEntries` from DTOs** — `LogEntries` stays for backward compatibility. Removal requires UI migration to projection output first. Deferred to deprecation follow-up.
5. **Removing `AddLogEntry` call sites** — `AddLogEntry` remains as transitional legacy in `Apply` methods and non-migrated flows. The guardrail test prevents NEW sites. Removal is the deprecation follow-up.
6. **Implementing `LegacyLogProjector`** — mentioned in ADR-0028 §12 but not needed for this campaign. Deferred.
7. **Case-file-view or full-audit projections** — ADR-0028 declares them but they're separate follow-ups.
8. **New persisted aggregate root** — `GameSession` remains the sole AR.
9. **Runtime-state table fan-out** — no new tables.

---

## Follow-up issues (at most two, rich and bounded)

### Follow-up 1: Travel/journey event sourcing

**Scope:** Migrate travel and encounter flows to typed domain events + `Apply` + projection.

**Methods to migrate:** `StartJourney`, `AdvanceJourneyDayDeterministic`, `HandleInterruptedTravelDay`, `HandleCompletedTravelDay`, `HandleOngoingTravelDay`, `ResolveJourneyEncounterDeterministic` (run/fight/bribe), `ContinueCurrentDayAfterEncounterResolution`, `AcknowledgeJourneyArrival`.

**Candidate event types:** `JourneyStarted`, `TravelDayAdvanced`, `JourneyEncounterInterrupted`, `JourneyEncounterResolved` (with choice: run/fight/bribe), `JourneyArrivalAcknowledged`, `TrailEventEncountered`.

**Key challenges:** Multi-day journey state machine, encounter resolution with health/wallet/ammo/heat/horse mutations, `TravelDiaryDayState` overlap with log entries, pending encounter state.

**Acceptance criteria:** All travel/encounter flows produce typed events, `Apply` is the single mutation path, replay reconstructs journey state, `DiaryProjector` handles travel events, hidden-truth boundaries intact, 4 travel handlers move to orchestration.

**Why deferred:** 12+ call sites, complex state machine, high mini-rewrite risk. Too large for this PR without compromising reviewability.

### Follow-up 2: Legacy log deprecation and UI projection migration

**Scope:** Remove `LogEntries` from player-facing surfaces after all flows are event-sourced.

**Steps:** Implement `LegacyLogProjector` (ADR-0028 §12), migrate `LogPanel`/`FieldReportPanel` to consume `diaryProjection`/`hudProjection` instead of `logEntries`, remove `LogEntries` from `GameSessionDto` and `JournalDto`, remove `AddLogEntry` call sites from `Apply` methods, remove `#pragma warning disable CS0618`, remove `AddLogEntry` method itself.

**Prerequisite:** All flows migrated to typed events (this campaign + Follow-up 1 + bounty-loop migration).

**Acceptance criteria:** No `LogEntries` in any player-facing DTO, no `AddLogEntry` call sites, no `#pragma warning disable CS0618`, UI consumes projections, `GameLogEntry` type removed or internal-only.

**Why deferred:** Cannot remove `LogEntries` until all flows produce projection-compatible events and UI consumers are migrated. Premature removal breaks compatibility.

---

## Validation plan (both phases)

- `dotnet build`
- `dotnet test tests/WildBunch.Application.Tests`
- `.\scripts\postgres-dev.ps1 validate` (integration tests touch API/persistence)
- `npm run build` in `src/WildBunch.Web` (Phase 1 only — Phase 2 doesn't touch frontend)

---

## Confirmation

**Approved by Harley.** Phase 1 and Phase 2 execute in the same PR. Phase 2 depends on Phase 1's guardrail test and DTO changes being in place. Two bounded follow-up issues (travel/journey event sourcing, legacy-log deprecation) are created after the PR lands cleanly and those seams remain deferred.
