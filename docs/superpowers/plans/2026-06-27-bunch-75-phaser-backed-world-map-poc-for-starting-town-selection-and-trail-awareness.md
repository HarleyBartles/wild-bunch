# BUNCH-75: Phaser-Backed World Map POC for Starting-Town Selection and Trail Awareness — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a minimal React-hosted Phaser map POC that shows starting towns and trails spatially, lets the player pick a starting town by clicking the map, and confirms that choice through the normal start flow without moving game truth into the frontend.

**Architecture:** React stays the shell and owns the setup/start flow state. Phaser is a renderer/input adapter only: it receives map read data, draws towns and trail edges, and emits selection intent back to React. Backend/application code remains authoritative for town identity, route distance, and start eligibility.

**Tech Stack:** C#/.NET 10, ASP.NET Core Minimal APIs, React 18, TanStack Query, styled-components, Phaser 2D, Vitest, xUnit.

## Global Constraints

- `GameSession` remains the live-play aggregate root; Phaser must not own gameplay truth.
- Current `main` does **not** yet have the BUNCH-102 starting-town selection seam. This slice is blocked until BUNCH-102 lands or an equivalent seam appears on refreshed `main`.
- The Phaser layer is presentation/input only. It may emit `townSelected` intent, but it must not calculate legal moves, start eligibility, or route truth.
- Keep the backend/application/domain route authoritative for towns, trails, distances, selected starting town validity, and game creation.
- Do not rewrite the whole frontend in Phaser.
- Do not implement freeform movement, terrain pathfinding, full travel animation, encounter scenes, or procedural map art.
- Do not expose hidden culprit truth or other internal game-state facts through the map surface.
- Keep any new browser surface inside the existing React shell and HUD model.
- Do not normalize runtime session state into new tables for this slice.
- If BUNCH-102 is still absent when execution begins, stop AMBER instead of inventing a parallel start flow.

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
- `BUNCH-102` is still `Todo` in Linear, so the starting-town selection seam this issue depends on has not landed yet.

That means the execution plan must stay gated on BUNCH-102 and should not assume a separate start-flow component already exists on current `main`.

## File Structure

If execution proceeds after the BUNCH-102 gate opens, the intended touchpoints are:

- `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs` and a new `SeedWorldMapLayout.cs` or equivalent to keep deterministic map coordinates and trail graph data next to the existing seeded world catalog.
- `src/WildBunch.Application/Games/Models/StartingTownMapDto.cs` and a new read-model DTO for map towns and trail edges.
- `src/WildBunch.Application/Games/Queries/GetStartingTownMapQuery.cs` and `GetStartingTownMapHandler.cs` for the map read endpoint.
- `src/WildBunch.Api/Games/StartingTownMapEndpoints.cs` or a narrowly extended `GameSessionEndpoints.cs` for the public read route.
- `src/WildBunch.Web/src/api/types.ts` and `src/WildBunch.Web/src/api/wildBunchApi.ts` for the map DTO and client call.
- `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx` for the mount/unmount seam.
- `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx` or the BUNCH-102 host component for React-owned confirmation, detail text, and selection state.
- `src/WildBunch.Web/src/tests/*` for the adapter and start-flow tests.
- `tests/WildBunch.Application.Tests/*` and `tests/WildBunch.Api.Tests/*` for the map read-model and endpoint tests.
- `docs/adr/*` only if the Phaser playfield becomes a durable frontend architecture decision rather than a one-off POC seam.

---

### Task 0: Revalidate the dependency gate and current start-flow seam

**Files:**
- Inspect: `src/WildBunch.Web/src/flow/PreSessionSurface.tsx`
- Inspect: `src/WildBunch.Web/src/components/StartGamePanel.tsx`
- Inspect: `src/WildBunch.Web/src/shell/AppShell.tsx`
- Inspect: `src/WildBunch.Web/src/api/types.ts`
- Inspect: `src/WildBunch.Api/Games/GameSessionEndpoints.cs`
- Inspect: `docs/superpowers/plans/2026-06-27-bunch-102-start-over-settings-and-prologue-start-loop.md`

**Interfaces:**
- Consumes: current `main`, Linear BUNCH-102 status, and the existing pre-session/start-route seam.
- Produces: a go/no-go decision for execution. If the BUNCH-102 seam is still absent, stop AMBER and do not start the code slice.

- [ ] **Step 1: Confirm BUNCH-102 is landed or the equivalent starting-town seam exists on refreshed `main`.**

Run: `rg -n "starting-town|prologue|StorySoFar|StartFlow|PhaserMapHost" src/WildBunch.Web src/WildBunch.Api src/WildBunch.Application`

Expected: the new start-flow seam is present only if BUNCH-102 has landed.

- [ ] **Step 2: Confirm current pre-session code still owns the old setup screen.**

Run: `Get-Content src/WildBunch.Web/src/flow/PreSessionSurface.tsx`

Expected: `PreSessionSurface` still renders `StartGamePanel` on the pre-session route.

- [ ] **Step 3: Stop if the dependency gate is still closed.**

If BUNCH-102 is still missing, return AMBER and do not continue into implementation tasks.

### Task 1: Define the map read model from authoritative world data

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs`
- Create: `src/WildBunch.Application/Games/Models/StartingTownMapDto.cs`
- Create: `src/WildBunch.Application/Games/Models/StartingTownMapTownDto.cs`
- Create: `src/WildBunch.Application/Games/Models/StartingTownMapTrailDto.cs`
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs`
- Modify: `src/WildBunch.Domain/World/WorldModels.cs` only if the smallest honest representation needs a domain-level coordinate value object instead of a GameContent-local layout table

**Interfaces:**
- Consumes: seeded town ids/names/services and trail distance truth from current world generation.
- Produces: a deterministic map layout model with town coordinates, route edges, and ride-day distance labels for the map host.

- [ ] **Step 1: Add a deterministic coordinate layout for the seeded towns.**

The layout should stay static and modest. Use coordinates that make the trail graph readable; do not generate procedural map art.

- [ ] **Step 2: Add a map DTO that carries the minimum data the map needs.**

The DTO should include town id, town name, x/y coordinate, selection eligibility, and trail edge data with ride-day distance labels.

- [ ] **Step 3: Keep the map source next to the existing seeded world catalog.**

Do not move map truth into the web project. The frontend should consume read data only.

### Task 2: Add the backend read endpoint for the map POC

**Files:**
- Create: `src/WildBunch.Application/Games/Queries/GetStartingTownMapQuery.cs`
- Create: `src/WildBunch.Application/Games/Queries/GetStartingTownMapHandler.cs`
- Modify: `src/WildBunch.Application/DependencyInjection.cs` or the existing game-handler registration surface
- Create: `src/WildBunch.Api/Games/StartingTownMapEndpoints.cs` or extend `src/WildBunch.Api/Games/GameSessionEndpoints.cs`
- Create or modify: `tests/WildBunch.Application.Tests/Games/Queries/GetStartingTownMapHandlerTests.cs`
- Create or modify: `tests/WildBunch.Api.Tests/Games/StartingTownMapEndpointsTests.cs`

**Interfaces:**
- Consumes: the new map read model and the seeded world catalog.
- Produces: a public read endpoint that returns town coordinates and trail edges before session creation.

- [ ] **Step 1: Write the query and handler against the authoritative seeded world data.**

The handler should return only player-visible map data. It must not depend on a live `GameSession`.

- [ ] **Step 2: Add the API route.**

Use a setup-scoped map route rather than forcing the caller to create a session first.

- [ ] **Step 3: Add tests that prove the map data is deterministic and sourced from the backend.**

The tests should assert town ids, coordinates, and trail distances without depending on frontend state.

### Task 3: Host Phaser inside React and keep React in charge of selection

**Files:**
- Create: `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`
- Create or modify: `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx`
- Modify: `src/WildBunch.Web/src/api/types.ts`
- Modify: `src/WildBunch.Web/src/api/wildBunchApi.ts`
- Create or modify: `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`
- Create or modify: `src/WildBunch.Web/src/tests/StartingTownStep.test.tsx`

**Interfaces:**
- Consumes: the map read endpoint response and the current React-owned selected-town state.
- Produces: a mounted Phaser scene that emits `townSelected` intent and unmounts cleanly on route change.

- [ ] **Step 1: Add the Phaser host component with explicit mount/unmount cleanup.**

The host should create the Phaser game in `useEffect`, destroy it on cleanup, and respond to resize without taking over React state.

- [ ] **Step 2: Wire the host into the starting-town step.**

React owns the selected town, the detail panel, and the confirm action. Phaser only raises selection intent.

- [ ] **Step 3: Keep explanatory copy and confirmation controls in DOM/React.**

The Phaser canvas should not own buttons, validation, or final confirmation.

- [ ] **Step 4: Add component tests for the adapter seam.**

Tests should prove the host mounts and unmounts cleanly and that selection intent flows back into React state.

### Task 4: Confirm the selected town through the normal start flow

**Files:**
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/api/types.ts`
- Modify: `src/WildBunch.Web/src/api/wildBunchApi.ts`
- Modify: `src/WildBunch.Web/src/flow/PreSessionSurface.tsx` if the existing setup surface still hosts the start flow after BUNCH-102 lands
- Create or modify: `src/WildBunch.Web/src/tests/StartFlow.test.tsx` or the BUNCH-102 start-flow test surface

**Interfaces:**
- Consumes: the React-selected starting town and the existing `startNewGame` mutation.
- Produces: a final `POST /api/games` call that carries the selected town only after the normal start flow confirms it.

- [ ] **Step 1: Extend the start request model to carry the selected starting town.**

The request should still preserve existing difficulty/seed/entropy behavior.

- [ ] **Step 2: Pass the selected town from React into the existing game-creation path.**

Do not let Phaser call the game-creation endpoint directly.

- [ ] **Step 3: Add tests proving the final confirmation still happens through the normal start flow.**

The map can select a town, but the game should only start after React-owned confirmation.

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

