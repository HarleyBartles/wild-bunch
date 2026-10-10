# Preserve Unknown Setup State in Player Responses

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Ensure player-facing setup and prologue responses do not report health, inventory, location, or a HUD as gameplay facts before `GameStarted` establishes them.

**Architecture:** `PlayerSetupCompleted` establishes the player name and setup options, while `WorldGenerated` establishes the world. `GameStarted` establishes gameplay resources and the player's initial location. Preserve this event boundary in `GameSessionDto`, the HUD route, and the browser types: setup-known fields remain available, gameplay-only values are nullable/absent, and the HUD is absent until the game starts. The projector remains a non-null projector for streams containing `GameStarted`; invalid pre-start requests return no HUD instead of receiving placeholder values.

**Execution Strategy:** Execute inline and sequentially. The API wire contract, application mapping and web consumers form one player-read boundary and require one coherent plan, while tests provide independently verifiable task exits.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially the player/read/display contract: unknown fields stay unknown until events establish them, and phase-inapplicable views remain absent rather than fake.

**Roadmap:** Row 08, `Restore strict read ownership and honest projections`.

**Scope:** Make setup/prologue `HudProjection` absent; make `PlayerDto.Health` nullable and `GameSessionDto.Inventory` absent until `GameStarted`; preserve setup-known name, world, case, options and start-flow phase; return HTTP 204 for an existing setup-phase HUD request, 404 for a missing session and 200 with the event-derived HUD after start; update TypeScript types and player surfaces to handle absent gameplay state.

**Out of scope:** Changing start/prologue/town-selection semantics, domain placeholder state, gameplay defaults, account ownership, adding gameplay features, or changing journal/case projections.

**Review Focus:** Inspect the actual serialized setup, prologue, selected-town and started responses for invented facts; verify missing-session and pre-start HUD responses remain distinct; check all browser consumers handle absent inventory/health without rendering zero values; confirm started-game projection values still come from committed events.

## Global Constraints

- Follow `.agents/playbooks/unslop.md` and select the Application, API, web and code-review profiles relevant to changed files.
- Follow `.agents/playbooks/decision-records.md`; this implements the existing spec and makes no new durable decision, so update an ADR only if source inspection reveals a decision divergence that must be resolved.
- Follow `.agents/playbooks/feature-matrix.md`; product capability truth is unchanged by correcting its phase-aware response contract.
- Preserve the setup-known player name and `StartFlowPhase`; do not represent the player as dead or invent an empty starting inventory.
- Treat `GameStarted` as the sole event establishing initial health, wallet, inventory and current town for these player projections.
- Add behavior tests only where the current test suite has the documented AP-12 gap; demonstrate each new contract test fails for the intended missing behavior, restore the implementation and verify green.
- Use `develop` as the base and PR target. This is the next row 08 plan, advances the single authored version to `0.1.0-dev.59`, and its PR must pass hosted CI before merge.
- The predecessor `.agents/plans/2026-10-10-audit-occurrence-metadata.md` is retired in the plan-handoff commit because PR #243 shipped its full scope at `.58`; keep the parent specification and roadmap live.

## Task 1: Make the Application DTO preserve pre-start unknowns

**Files:** `src/WildBunch.Application/Games/Models/GameDtos.cs`, `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`, `src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs`, `src/WildBunch.Application/Games/Commands/ViewPrologueHandler.cs`, `tests/WildBunch.Application.Tests/Handlers/CompletePlayerSetupHandlerTests.cs`, and the existing started-game handler tests requiring null-forgiving annotations after the DTO contract becomes nullable.

- [x] Strengthen the real setup-handler response test to prove setup name/world/phase are present while health and inventory are absent; first confirm it fails because current mapping exposes non-null placeholder health and an empty-inventory state.
- [x] Make the DTO represent pre-start health and inventory as nullable, and have `GameSessionMapper` emit them only once `StartFlowPhase` is `GameStarted` or later.
- [x] Remove setup/prologue HUD construction from `CompletePlayerSetupHandler` and `ViewPrologueHandler`; their responses use the DTO's absent HUD value and no longer reload/project a stream for an inapplicable view.
- [x] Replace the setup handler assertion that requires a non-null HUD with assertions on the actual known/unknown fields and phase; verify prologue response behavior in the real API scenario in Task 2.
- [x] Run focused Application tests and inspect their output; confirm a deliberate mapper mutation that exposes placeholder inventory makes the new setup behavior test fail, then restore it.

## Task 2: Make the HTTP HUD route phase-aware

**Files:** `src/WildBunch.Api/Games/ProjectionEndpoints.cs`, `tests/WildBunch.Integration.Tests/ProjectionEndpointTests.cs`, `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`, and any route metadata needed for the 204 response.

- [x] Extend the real PostgreSQL-backed setup-to-start scenario to assert the setup response keeps setup-known facts and returns no gameplay resources or HUD.
- [x] Before posting the start command, request the HUD route for that existing setup session and assert HTTP 204 with no body; keep the missing-session 404 test and started-session 200 test.
- [x] Gate the route on the committed event stream containing `GameStarted`; do not treat `GameStatus.Active` as proof that setup completed.
- [x] Make direct `HudProjector.Project` use fail explicitly when no `GameStarted` event exists, and replace the empty-stream default-projection test with a negative test for that contract.
- [x] Demonstrate the HTTP pre-start test fails if the route returns a fabricated HUD, restore the code, and run the focused Integration tests.

## Task 3: Carry the nullable response contract through the browser

**Files:** `src/WildBunch.Web/src/api/types.ts`, `src/WildBunch.Web/src/shell/Hud.tsx`, `src/WildBunch.Web/src/components/FieldReportPanel.tsx`, `src/WildBunch.Web/src/components/InventoryPanel.tsx`, `src/WildBunch.Web/src/components/travel/TravelActions.tsx`, `src/WildBunch.Web/src/flow/TravelPrepSurface.tsx`, `src/WildBunch.Web/src/flow/places/StorePlace.tsx`, `src/WildBunch.Web/src/tests/Hud.test.tsx`, and focused tests or call sites revealed by type-checking.

- [x] Update `PlayerDto.currentTownId`, `PlayerDto.health` and `GameSessionDto.inventory` types to match the server's nullable response.
- [x] Ensure the HUD and resource panels do not render before gameplay resources exist; do not convert unknown health or inventory into displayed zero/empty values.
- [x] Add focused behavior tests proving pre-start HUD and inventory surfaces stay absent while started-game HUD values still render.
- [x] Run the web type-check, focused and complete web tests, and production build; fix every consumer that assumes pre-start resources exist.

## Task 4: Verify and publish the slice

- [x] Run focused tests for Application, Integration and Web, then run `py -3 tools/run.py ci --check` from the repository command bus.
- [ ] Review the complete diff against this plan, the baseline spec, AP-12 and the current row 08 roadmap text; confirm no feature-matrix or ADR update is required unless implementation reveals a durable divergence.
- [ ] Commit the implementation in coherent task commits, push the branch and open a PR against `develop` with current validation evidence.
- [ ] Confirm hosted CI passes on the exact PR head before merging; merge the PR into `develop` and verify the hosted develop gate passes on the merge commit.
- [ ] Update the roadmap with the merged PR, source/merge SHAs, exact hosted PR and develop gate runs, and next row 08 target; the next successor plan will retire this plan after verifying its full scope.
