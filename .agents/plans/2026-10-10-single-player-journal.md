# Consolidate the Player Journal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make the event-derived `/journal` the only semantic player history, preserve meaningful recorded actions in its entries, and remove the duplicate Diary projection contract.

**Architecture:** `JournalLogProjector` remains the Application projection for the player's curated event history, served by the existing journal query. The separate `DiaryProjector`, `/projections/diary` route, and `DiaryProjection` response field duplicate that history with different coverage and are removed. Add the missing player-facing sheriff turn-in fact from its existing event message; keep HUD projection, developer audit, and travel-day resource/state projections under their current distinct purposes.

**Tech Stack:** .NET 10, C#, ASP.NET Core, xUnit, PostgreSQL integration tests, TypeScript.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md` (event-derived journal decision and row 08); `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; AP-15 in `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline and sequentially. The event-catalogue test correction, application response removal, API retirement, and their investigation/unslop updates form one player-history boundary.

## Global Constraints

- Start from `develop` at PR #241 merge `f5ed5e4c8fc3ce4adbd76718004fb401adc958e5`; target `develop`.
- Record PR #241 source `38b991b07eddebe87d2a91ffdb27fb9b96694ebe`, merge `f5ed5e4c8fc3ce4adbd76718004fb401adc958e5`, exact-head gate `38025527579`, develop push gate `38025824748`, and delivered `0.1.0-dev.56`.
- Retire the completed `.56` event-payload plan only after verifying its exact merged output; update row 07 as complete and row 08 as executing in this successor handoff.
- Advance the single authored application version in `Directory.Build.props` to `0.1.0-dev.57` in the committed plan handoff.
- One semantic player journal includes meaningful town and travel actions in event order; a journey-scoped presentation is a view over that same history, not a second history authority.
- Use the existing `SheriffTurnInSettled.Message` and recorded `Day`/`Turn` for a journal entry. Do not invent narration or expose `ArchiveReason` as player copy; archiving is lifecycle state, not authored player diary prose.
- Retain the `/journal` read path and its known-fact boundary. Preserve HUD, developer full-audit, `TravelDiaryDay` state and its persistence watermark; they serve separate purposes.
- Do not add endpoints, event types, migrations, persisted fields, account ownership, or new game behavior. Do not add a route-absence test or structural file/registration detector.
- Preserve ADR-0028; update the PG-008 assessment/evidence in `docs/features.md` because the current duplicate-history claim becomes stale. This implements its existing one-event-authority and journal decisions and changes no product promise.
- Publish a PR to `develop`; require hosted canonical CI success on the exact PR head and exact develop merge commit before retiring the worktree and branch.

## Review Focus

- **Journal coverage:** The production journal currently omits `SheriffTurnInSettled`, while the duplicate Diary projector includes it. Prove the event's established player message and event time appear once in the journal.
- **Distinct saloon outcomes:** A citizen or wrong-identity take-in, or a rejected attempt such as a missing wanted notice, can be represented only by `SaloonPersonOfInterestConfronted`; retain that player-facing result while avoiding duplicate summaries for wanted outcomes already represented by `WantedSuspectConfronted` and sheriff settlement events.
- **Saloon discovery:** Preserve the event's `RecordLog` choice: a recorded sighting appears in the journal, while an unrecorded citizen sighting does not.
- **History duplication:** Remove only the duplicate player Diary projection. Keep the current HUD and the detailed travel-day state projection, which are separate outputs with distinct consumers.
- **History safety:** The journal remains a curated player-safe projection. Do not expose raw event payloads, hidden case truth, or system lifecycle reason strings.
- **Test ownership:** Retain behavior proof through `JournalLogProjector` and the `/journal` read path; remove tests that only preserve the obsolete Diary contract or optional DTO wiring.

---

### Task 1: Commit the `.57` successor handoff

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-10-event-fact-payload-boundary.md`; create this plan.

- [x] Verify PR #241 is merged to `develop` at `f5ed5e4c8fc3ce4adbd76718004fb401adc958e5`, source `38b991b07eddebe87d2a91ffdb27fb9b96694ebe`, and exact-head/develop gates `38025527579`/`38025824748` succeeded.
- [x] Record PR #241 and its gate evidence; close row 07 based on its recovery/replay exit evidence, assign remaining read-ownership work to row 08 and deployment-operator ownership to row 18, and state that no local pre-0.1 playthrough retention is required while the migration chain remains the compatibility strategy.
- [x] Mark row 08 executing, point it to this plan, retire the completed `.56` plan, and set `Directory.Build.props` to `0.1.0-dev.57`.
- [x] Commit the planning handoff before changing production source or tests.

**Expected:** The roadmap has an evidence-based row 07 exit and a JIT row 08 successor; the single version authority and plan custody are current.

### Task 2: Protect one player-history behavior

**Files:** Modify `src/WildBunch.Application/Projections/JournalLogProjector.cs`, `tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs`, `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs`, `tests/WildBunch.Integration.Tests/ProjectionEndpointTests.cs`, and `tests/WildBunch.Integration.Tests/GameApiJournalTests.cs`.

- [x] Replace `SheriffTurnInSettled_ProducesNoLogEntry_MatchingLegacyApply` with a positive journal behavior test: the event's `Message` appears once as a case update at its recorded day and turn after the opening entry.
- [x] Run the focused projector test before implementation and confirm it fails because the journal omits the settlement event; then add the minimal event mapping and rerun it.
- [x] Add journal behavior tests for unique citizen-fine and rejected `SaloonPersonOfInterestConfronted` messages, duplicate suppression when detailed wanted/settlement events already describe an outcome, a `RecordLog` saloon sighting and its false case, and a named wanted-suspect confrontation.
- [x] Correct existing saloon tests that asserted a rejected confrontation or citizen fine must not appear in the journal; retain their independent state, fine, security, and unrecorded-citizen-sighting assertions.
- [x] Move the existing persisted investigation-event proof from `/projections/diary` to `/journal`; assert the known investigation message is present in the returned `JournalDto`.
- [x] Keep existing `/journal` purchase and travel integration behavior intact. Do not create another endpoint or an absence test.
- [x] Review the old Diary projector cases against existing journal tests. Keep or strengthen only behavior that represents a player-visible fact; do not preserve generic store prose or administrative lifecycle strings merely to match the retired projector.

**Expected:** Meaningful recorded town, investigation, turn-in, and travel facts remain represented by one ordered player journal, with a PostgreSQL-backed read path proving persisted events reach `/journal`.

### Task 3: Retire the duplicate Diary contract

**Files:** Modify `src/WildBunch.Api/DependencyInjection.cs`, `src/WildBunch.Api/Games/ProjectionEndpoints.cs`, `src/WildBunch.Application/Games/Commands/AcknowledgeJourneyArrivalHandler.cs`, `AdvanceTravelDayHandler.cs`, `CompleteGameStartHandler.cs`, `CompletePlayerSetupHandler.cs`, `PurchaseStoreItemHandler.cs`, `ResolveJourneyEncounterHandler.cs`, `TravelToTownHandler.cs`, and `ViewPrologueHandler.cs`; modify `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`, `src/WildBunch.Application/Games/Models/GameDtos.cs`, and `src/WildBunch.Web/src/api/types.ts`; update `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`, `GameLogEntryLegacyProjectionTests.cs`, `Mappers/GameSessionDtoProjectionFieldsTests.cs`, `Handlers/CompletePlayerSetupHandlerTests.cs`, `Handlers/CompletePlayerSetupOneActivePlaythroughTests.cs`, `Handlers/AdvanceTravelDayHandlerTests.cs`, `Handlers/PurchaseStoreItemHandlerTests.cs`, `Handlers/ResolveJourneyEncounterHandlerTests.cs`, `Handlers/TravelToTownHandlerTests.cs`, `tests/WildBunch.Integration.Tests/EventSourcingEndToEndTests.cs`, `FullReplayEqualityTests.cs`, and `ProjectionEndpointTests.cs`; delete `src/WildBunch.Application/Projections/DiaryProjector.cs` and `DiaryProjection.cs`.

- [x] Remove `DiaryProjection` from `GameSessionDto`, `GameSessionMapper`, command-handler constructor dependencies/results, and the TypeScript API contract; preserve the independently consumed HUD projection and command/session facts.
- [x] Remove the `/projections/diary` route and its DI registration. Keep `/journal` as the player history route and keep `/projections/hud` unchanged.
- [x] Remove DiaryProjector-specific tests and optional-property mapper tests that only freeze the removed DTO shape. Preserve command, HUD, travel, and real journal behavior assertions; move any required event-history expectation to `JournalLogProjectorTests` or the `/journal` integration test.
- [x] Remove stale projection registrations and comments in integration test composition. Do not remove `TravelDiaryDayProjector`, its cache watermark, `HudProjector`, or `FullAuditProjector`.

**Expected:** Application command responses and the player API no longer offer two differently curated records of the same event history; the web contract contains only the journal resource used by the player UI.

### Task 4: Reconcile evidence and guidance

**Files:** Modify `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`, `.agents/unslop/backend-architecture.md`, `.agents/unslop/observations.md`, and `docs/features.md`.

- [x] Add a dated AP-15 disposition describing the retired Diary surface, the surviving journal projector, the sheriff turn-in coverage correction, and the unchanged travel-day state projection.
- [x] Update the AP-15 test follow-up to distinguish removed duplicate Diary expectations from retained behavior proof; state exactly which integration and projector tests now protect journal entries.
- [x] Add a scoped backend unslop guard that one semantic player journal may have full-playthrough and journey-scoped views but must not become overlapping Diary/Journal histories with different event coverage. Preserve the false-positive boundaries for developer audit and travel-day state.
- [x] Extend U-005 with the concrete source evidence that the API/web consumed `/journal` while command DTOs and an unused endpoint carried a divergent Diary projector; keep historical agent intent and recurrence unknown.
- [x] Re-read ADR-0028 and leave it unchanged because the source conforms to its accepted event authority. Update PG-008's current assessment and append dated evidence so the feature matrix no longer describes duplicate player histories after their retirement.

**Expected:** Current implementation, test ownership, roadmap, and anti-slop guidance all describe one curated player journal without claiming unrelated travel state or developer diagnostics were removed.

### Task 5: Validate, review, and publish

**Files:** All changed application, API, web contract, test, investigation, roadmap and unslop paths in Tasks 1–4.

- [x] Review the complete diff against the baseline spec, ADR-0028, AP-15, event-sourcing integrity, the feature matrix and completed-artifact custody.
- [x] Run the focused projector and PostgreSQL journal integration tests, then rely on the check-only pre-commit hook for the canonical fail-fast gate; do not run the canonical gate immediately before or after a successful hooked commit.
- [x] Verify no `DiaryProjector`, player API `DiaryProjection`, `diaryProjection` DTO field, or `/projections/diary` consumer remains, while `TravelDiaryDay` persistence, its `TravelDiaryProjection*` watermark, and HUD/full-audit behavior remain covered through their actual consumers.
- [ ] Push and open a PR to `develop`; verify the exact PR head SHA and hosted canonical gate. Merge as authorized by the goal and verify the exact develop push gate.
- [ ] Record actual source/merge SHAs and hosted gate evidence in the next successor handoff; sync the shared `Z:\wild-bunch` checkout and retire only this verified merged worktree and branch.

**Expected:** `.57` leaves a single player journal contract on `develop`, with meaningful event facts tested through the owned projection/read paths and no competing Diary history surface.
