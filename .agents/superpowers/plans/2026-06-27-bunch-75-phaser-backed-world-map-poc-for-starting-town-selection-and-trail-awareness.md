# BUNCH-75: Phaser-Backed World Map POC for Starting-Town Selection and Trail Awareness — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a minimal React-hosted Phaser map POC that enhances the BUNCH-102 starting-town step with spatial map selection, lets the player pick a starting town by clicking the map, and confirms that choice through the normal start flow without moving game truth into the frontend.

**Architecture:** React stays the shell and owns the setup/start flow state. Phaser is a renderer/input adapter only: it receives map read data, draws towns and trail edges, and emits selection intent back to React. Backend/application code remains authoritative for town identity, route distance, and start eligibility.

**Tech Stack:** C#/.NET 10, ASP.NET Core Minimal APIs, React 18, TanStack Query, styled-components, Phaser 2D, Vitest, xUnit.

## Global Constraints

- `GameSession` remains the live-play aggregate root; Phaser must not own gameplay truth.
- BUNCH-102 has a checked-in, merged plan that defines the expected upstream start-flow seam. BUNCH-75 composes with that plan and reuses its starting-town selection, request, and confirmation seams.
- The Phaser layer is presentation/input only. It may emit `townSelected` intent, but it must not calculate legal moves, start eligibility, or route truth.
- Keep the backend/application/domain route authoritative for towns, trails, distances, selected starting town validity, and game creation.
- Do not rewrite the whole frontend in Phaser.
- Do not implement freeform movement, terrain pathfinding, full travel animation, encounter scenes, or procedural map art.
- Do not expose hidden culprit truth or other internal game-state facts through the map surface.
- Keep any new browser surface inside the existing React shell and HUD model.
- Do not normalize runtime session state into new tables for this slice.
- If BUNCH-102 implementation has not landed on refreshed `main` at execution time, stop AMBER instead of inventing a parallel start flow.

---

## Preflight Findings

Current source inspection on `main` found these seams:

- `src/WildBunch.Web/src/flow/PreSessionSurface.tsx` and `src/WildBunch.Web/src/components/StartGamePanel.tsx` still own the current pre-session setup screen.
- `src/WildBunch.Web/src/shell/AppShell.tsx` is the React shell host for the routed play surface.
- `src/WildBunch.Web/src/state/useCurrentGameSession.ts` stores the active game id in `localStorage` and drives session queries/mutations.
- `src/WildBunch.Web/src/api/types.ts` exposes `WorldDto` and `TrailDto` only after a session exists; there is no pre-session starting-town map read model.
- `src/WildBunch.Api/Games/GameSessionEndpoints.cs` only creates and fetches sessions today.
- `src/WildBunch.Api/Games/TravelEndpoints.cs` only exposes post-start travel preview and travel actions.
- `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs` and `src/WildBunch.Application/Games/Mapping/TravelMapper.cs` map authoritative world/travel state after session creation.
- `src/WildBunch.Domain/World/WorldModels.cs` and `src/WildBunch.Domain/Travel/TravelRouteModels.cs` already own town/trail and ride-day distance truth.
- `BUNCH-102` has a checked-in, merged plan, but the implementation must still land on refreshed `main` before BUNCH-75 can execute.

That means the execution plan must stay gated on BUNCH-102 and should not assume a separate start-flow component already exists on current `main`.

## File Structure

If execution proceeds after the BUNCH-102 gate opens, the intended touchpoints are:

- `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs` plus a small map-layout companion such as `SeedWorldMapLayout.cs` to keep deterministic coordinates next to the seeded world catalog.
- `src/WildBunch.Application/Games/Models/StartingTownMapDto.cs` and companion town/trail DTOs if the setup read model needs richer map fields than BUNCH-102 already returns.
- `src/WildBunch.Application/Games/Queries/GetStartingTownMapQuery.cs` and `GetStartingTownMapHandler.cs` only if the BUNCH-102 `GET /api/games/starting-towns` read model needs a clearly named companion map endpoint; do not create a second eligibility algorithm.
- `src/WildBunch.Api/Games/StartingTownMapEndpoints.cs` or an extended setup route only if the map read model cannot be kept on the existing setup endpoint.
- `src/WildBunch.Web/src/api/types.ts` and `src/WildBunch.Web/src/api/wildBunchApi.ts` for the shared setup/map DTO and client call.
- `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx` for the mount/unmount seam.
- `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx` or its BUNCH-102 successor component for the React-owned confirmation, detail text, and selection state.
- `src/WildBunch.Web/src/tests/*` for the adapter and start-flow tests.
- `tests/WildBunch.Application.Tests/*` and `tests/WildBunch.Api.Tests/*` for the setup/map read-model and endpoint tests.
- `docs/adr/*` only if the Phaser playfield becomes a durable frontend architecture decision rather than a one-off POC seam.

---

### Task 0: Revalidate the BUNCH-102 seam, then compose with it

**Files:**
- Inspect: `src/WildBunch.Web/src/flow/PreSessionSurface.tsx`
- Inspect: `src/WildBunch.Web/src/components/StartGamePanel.tsx`
- Inspect: `src/WildBunch.Web/src/shell/AppShell.tsx`
- Inspect: `src/WildBunch.Web/src/api/types.ts`
- Inspect: `src/WildBunch.Api/Games/GameSessionEndpoints.cs`
- Inspect: `docs/superpowers/plans/2026-06-27-bunch-102-start-over-settings-and-prologue-start-loop.md`

**Interfaces:**
- Consumes: the checked-in BUNCH-102 plan, the expected upstream start-flow seams, and refreshed `main` at execution time.
- Produces: a go/no-go decision for execution. If the BUNCH-102 implementation is still absent on refreshed `main`, stop AMBER and do not start the code slice.

- [ ] **Step 1: Read the checked-in BUNCH-102 plan and identify the expected upstream seams.**

Use: `docs/superpowers/plans/2026-06-27-bunch-102-start-over-settings-and-prologue-start-loop.md`

Expected: the plan establishes the upstream `StartingTownId` chain, `GET /api/games/starting-towns`, and the React start-flow step that BUNCH-75 must enhance.

- [ ] **Step 2: On execution, revalidate refreshed `main` against the expected BUNCH-102 seams.**

Run: `rg -n "StartingTownId|starting-towns|StartingTownStep|StorySoFar|PhaserMapHost" src/WildBunch.Web src/WildBunch.Api src/WildBunch.Application`

Expected: the upstream seam exists on source, or a landed variant can be reconciled before implementing BUNCH-75.

- [ ] **Step 3: If BUNCH-102 has landed differently from the plan, reconcile against landed source before implementing.**

Do not duplicate start-request, start-command, or setup-endpoint work. Reuse the landed seam or adjust the plan against it before coding.

- [ ] **Step 4: Stop if the BUNCH-102 implementation is still absent on refreshed `main`.**

If the upstream implementation has not landed, return AMBER and do not continue into implementation tasks.

### Task 1: Extend the BUNCH-102 setup read model for map coordinates

**Files:**
- Modify: `src/WildBunch.Application/Games/Models/StartingTownDto.cs` or the existing setup-town DTO surface from BUNCH-102
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs`
- Create: `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs`
- Modify: `src/WildBunch.Domain/World/WorldModels.cs` only if the smallest honest representation needs a domain-level coordinate value object instead of a GameContent-local layout table

**Interfaces:**
- Consumes: the BUNCH-102 setup-town candidate source and the seeded world/trail truth.
- Produces: a deterministic map layout extension with town coordinates and route edges that reuses the same eligibility/candidate source as BUNCH-102.

- [ ] **Step 1: Add a deterministic coordinate layout for the seeded towns.**

The layout should stay static and modest. Use coordinates that make the trail graph readable; do not generate procedural map art.

- [ ] **Step 2: Extend the setup-town read model or add a companion map projection, but keep the candidate source shared.**

The map view may add x/y coordinates and trail-edge labels, but the allowed-town list must come from the same eligibility logic BUNCH-102 already owns.

- [ ] **Step 3: Keep the map source next to the existing seeded world catalog and setup read model.**

Do not move map truth into the web project. The frontend should consume read data only.

### Task 2: Reuse the BUNCH-102 setup endpoint and expose map-ready data

**Files:**
- Modify: `src/WildBunch.Application/Games/Queries/GetStartingTownsHandler.cs` or the BUNCH-102 setup-town query surface
- Modify: `src/WildBunch.Api/Games/GameSessionEndpoints.cs` or the BUNCH-102 setup endpoint surface
- Create or modify: `tests/WildBunch.Application.Tests/Games/Queries/GetStartingTownsHandlerTests.cs`
- Create or modify: `tests/WildBunch.Api.Tests/Games/GameSessionEndpointsTests.cs`

**Interfaces:**
- Consumes: the BUNCH-102 setup-town candidate source and the map coordinate extension.
- Produces: a setup-scoped read endpoint that returns the existing candidate towns plus optional map-ready coordinates and trail edges.

- [ ] **Step 1: Verify and reuse the BUNCH-102 setup-town endpoint rather than creating a second eligibility algorithm.**

If the existing endpoint can carry coordinates and edges, extend it; otherwise add a clearly named companion map endpoint that still uses the same candidate source.

- [ ] **Step 2: Add or extend the API route without duplicating eligibility.**

Use a setup-scoped map route rather than forcing the caller to create a session first.

- [ ] **Step 3: Add tests that prove the map data is deterministic, backend-sourced, and shares the candidate list with BUNCH-102.**

The tests should assert town ids, coordinates, trail distances, and candidate eligibility without depending on frontend state.

### Task 3: Replace the BUNCH-102 `StartingTownStep` body with a Phaser-backed map host

**Files:**
- Create: `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`
- Modify: `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx`
- Modify: `src/WildBunch.Web/src/api/types.ts`
- Modify: `src/WildBunch.Web/src/api/wildBunchApi.ts`
- Create or modify: `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`
- Create or modify: `src/WildBunch.Web/src/tests/StartingTownStep.test.tsx`

**Interfaces:**
- Consumes: the BUNCH-102 setup-town candidate response and the current React-owned selected-town state.
- Produces: a mounted Phaser scene that emits `townSelected` intent and unmounts cleanly on route change.

- [ ] **Step 1: Add the Phaser host component with explicit mount/unmount cleanup.**

The host should create the Phaser game in `useEffect`, destroy it on cleanup, and respond to resize without taking over React state.

- [ ] **Step 2: Wire the host into the BUNCH-102 starting-town step.**

React owns the selected town, the detail panel, and the confirm action. Phaser only raises selection intent.

- [ ] **Step 3: Keep explanatory copy and confirmation controls in DOM/React.**

The Phaser canvas should not own buttons, validation, or final confirmation.

- [ ] **Step 4: Add component tests for the adapter seam.**

Tests should prove the host mounts and unmounts cleanly and that selection intent flows back into React state.

### Task 4: Prove React owns the final confirmation and game creation

**Files:**
- Inspect: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Inspect: `src/WildBunch.Web/src/api/types.ts`
- Inspect: `src/WildBunch.Web/src/api/wildBunchApi.ts`
- Inspect: `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx`
- Create or modify: `src/WildBunch.Web/src/tests/StartFlow.test.tsx` or the BUNCH-102 start-flow test surface
- Create or modify: `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`

**Interfaces:**
- Consumes: the React-selected starting town and the existing `startNewGame` mutation from BUNCH-102.
- Produces: proof that the final `POST /api/games` call still comes from React-owned confirmation, not Phaser.

- [ ] **Step 1: Verify the selected-town request and command seams already exist from BUNCH-102.**

Do not re-add `StartingTownId` to the request chain; just confirm the upstream seam and reuse it.

- [ ] **Step 2: Keep Phaser out of the game-creation path.**

React owns the selection state and the confirm action. Phaser must not call `POST /api/games`.

- [ ] **Step 3: Add tests proving the final confirmation still happens through the normal start flow.**

The map can select a town, but the game should only start after React-owned confirmation.

- [ ] **Step 4: Add falsifiable proof that Phaser does not own game truth.**

Tests should prove Phaser does not call `POST /api/games`, does not decide eligibility, does not store selected-town truth, and does not bypass the React-owned final confirmation.

### Task 5: Validate the slice and capture browser proof

**Files:**
- Inspect or modify: `.agents/superpowers/output/screenshots/` only for local evidence during manual verification
- Modify: `docs/adr/*` only if the Phaser seam is durable and should be recorded

**Interfaces:**
- Consumes: the completed backend read model, Phaser host, and start-flow integration.
- Produces: build/test output, browser screenshots, and any required ADR/doc update.

- [ ] **Step 1: Run backend validation.**

Run: `dotnet build WildBunch.sln`

Run: `dotnet test WildBunch.sln`

- [ ] **Step 2: Run frontend validation.**

Run: `cd src/WildBunch.Web; npm run build`

Run: `cd src/WildBunch.Web; npm test`

- [ ] **Step 3: Perform a browser smoke test.**

Prove the map mounts, towns are clickable, the selected town is reflected in React, and the confirm action completes the normal start flow.

- [ ] **Step 4: Update durable docs if the Phaser seam is now a lasting frontend boundary.**

If the Phaser host becomes a standing architecture decision, update the relevant ADR or frontend docs in the same PR.

---

## Definition of Done

| DOD item | Proof |
| --- | --- |
| Current `main` was checked for the dependency gate | Task 0 output and the BUNCH-102 status |
| Map data comes from authoritative backend source | `GetStartingTownMapHandlerTests.cs` |
| Phaser renders towns and trails without owning game truth | `PhaserMapHost.test.tsx` and the browser smoke test |
| Player can click a town and see the selection in React | `StartingTownStep.test.tsx` and browser proof |
| Final start flow still confirms through React/backend, not Phaser | start-flow test coverage |
| Browser evidence exists for the map selection flow | local screenshot or equivalent artifact outside the repo |
| No pathfinding/freeform-movement scope creep | plan stop conditions and test scope remain narrow |

## Stop Conditions

- If BUNCH-102 is still absent on refreshed `main`, stop AMBER and do not start implementation.
- If the map design turns into pathfinding, freeform movement, or full travel animation, split that work out and stop this slice.
- If the slice needs more than a deterministic coordinate layout plus trail edges, pause and reassess instead of broadening the frontend.
