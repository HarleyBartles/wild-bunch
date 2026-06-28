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

## Global Constraints (binding for this task)

- `GameSession` remains the live-play aggregate root; Phaser must not own gameplay truth.
- The Phaser layer is presentation/input only. It may emit `townSelected` intent, but it must not calculate legal moves, start eligibility, or route truth.
- Keep the backend/application/domain route authoritative for towns, routes, distances, selected starting town validity, and game creation.
- Do not rewrite the whole frontend in Phaser.
- Do not implement freeform movement, terrain pathfinding, full travel animation, encounter scenes, or procedural map art.
- Do not expose hidden culprit truth or other internal game-state facts through the map surface.
- Keep any new browser surface inside the existing React shell and HUD model.
- React owns the selected town, the detail panel, and the confirm action. Phaser only raises selection intent.
- The Phaser canvas should not own buttons, validation, or final confirmation.
- Use `styled-components` for component-owned layout (per `src/WildBunch.Web/AGENTS.md`). Do NOT use plain CSS classes in `className`. Reference design tokens via `var(--token-name)`.
- Re-use shared primitives from `src/components/ui/sharedStyled.tsx` for genuine cross-surface patterns (e.g. `Button`).
- Apply the play-surface-ui unslop profile: the map should feel like an in-world trail map, not a SaaS dashboard. No decorative counters, no product chrome copy, no forced modal titles. Plain concrete Western copy.
- Do not add comments unless asked. Follow existing code style.

## Task 1+2 output (already landed on this branch — consume it)

The backend now exposes `GET /api/games/starting-town-map` returning `StartingTownMapDto`:
```typescript
interface StartingTownMapDto {
  towns: StartingTownMapTownDto[];
  trails: StartingTownMapTrailDto[];
}
interface StartingTownMapTownDto {
  id: string;
  name: string;
  services: number;
  x: number;
  y: number;
  selectable: boolean;
}
interface StartingTownMapTrailDto {
  id: string;
  fromTownId: string;
  toTownId: string;
  rideDayDistance: number;
}
```

## Existing frontend seams (verified on this checkout)

- `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx` — current component. Props: `selectedTownId: string | null`, `onSelectTown: (townId: string) => void`. Uses `useQuery({ queryKey: ["starting-towns"], queryFn: () => getStartingTowns() })`. Renders a `TownList` of `TownCard` items with "Start in {name}" buttons. Has `StepHeading` "Pick a starting town", two `StepLead` paragraphs, and a `TownLoading` state ("Saddling up the map…").
- `src/WildBunch.Web/src/flow/PreSessionSurface.tsx` — orchestrates the flow. `handleStartWithTown(townId)` calls `flow.setSelectedTownId(townId)` then `flow.goToStep("creating")` then `startNewGame(request)`. So `onSelectTown` IS the confirm action today — selecting a town immediately starts the game.
- `src/WildBunch.Web/src/api/wildBunchApi.ts` — `getStartingTowns()` returns `StartingTownDto[]` from `/api/games/starting-towns`. Add `getStartingTownMap()` returning `StartingTownMapDto` from `/api/games/starting-town-map`.
- `src/WildBunch.Web/src/api/types.ts` — has `StartingTownDto { id, name, services }`. Add the map DTO types above.
- `src/WildBunch.Web/src/tests/StartingTownStep.test.tsx` — existing tests asserting towns render, onSelectTown fires, loading state, heading/body copy, no Back button, no buttons while loading. These tests mock `getStartingTowns`. You will need to update them if you change the component's data source or shape.
- `src/WildBunch.Web/src/tests/StartFlow.test.tsx` — integration test for the full flow. Mocks `getStartingTowns` returning `[{ id: "t-town", name: "Tumbleweed", services: 0 }, { id: "dust-fork", name: "Dust Fork", services: 0 }]` and clicks "Start in Tumbleweed". If you change `StartingTownStep` to use `getStartingTownMap`, you MUST update the mock in `StartFlow.test.tsx` too (add `getStartingTownMap: vi.fn()` to the mock and prime it).
- `src/WildBunch.Web/package.json` — does NOT yet have `phaser`. You must add it as a dependency. Use a stable Phaser 3 version (e.g. `phaser@^3.80.0` or similar — check npm for a stable release published >7 days ago). Run `npm install phaser` from `src/WildBunch.Web/`.
- `src/WildBunch.Web/vite.config.ts` — vitest config with `environment: "jsdom"`, `setupFiles: ["./src/tests/test-utils/setup.ts"]`, `css: true`, `globals: false`.
- `src/WildBunch.Web/src/tests/test-utils/setup.ts` — read this to understand the test setup (jsdom, matchMedia mocks, etc.).

## Design direction

The goal is a vertical player-facing slice where the player sees towns and trails spatially, clicks a starting town on the map, and confirms through the normal start flow.

### PhaserMapHost component

- Props: `mapData: StartingTownMapDto`, `selectedTownId: string | null`, `onTownSelected: (townId: string) => void`.
- Creates a Phaser game in `useEffect` with a `Phaser.Scene` that:
  - Renders town markers as circles/shapes at their x/y coordinates (scaled to fit the canvas).
  - Renders trail edges as lines between connected towns, with ride-day distance labels.
  - Highlights the selected town.
  - Makes `selectable` towns interactive (pointer hover/click); non-selectable towns are rendered but not clickable.
  - Emits `onTownSelected(townId)` when a selectable town is clicked — NOT game-state mutation.
- Destroys the Phaser game in the cleanup function.
- The canvas should fill a reasonable area (e.g. 800x500 or responsive). Use a styled wrapper div.
- Phaser must NOT call `POST /api/games`, must NOT decide eligibility, must NOT store selected-town truth. It receives `mapData` and `selectedTownId` as props and emits `onTownSelected`.

### StartingTownStep changes

- Switch the data source from `getStartingTowns` to `getStartingTownMap` (or use both — but the map needs the coordinate data, so use `getStartingTownMap`).
- Render `PhaserMapHost` with the map data.
- Keep the `StepHeading` "Pick a starting town" and the two `StepLead` paragraphs (existing copy).
- Keep a React-owned detail/confirmation path. Today `onSelectTown` immediately starts the game. You may keep that behavior (clicking a town on the map selects AND confirms, same as clicking a "Start in X" button did), OR introduce a two-step select-then-confirm. The plan says "React-owned detail panel and confirmation path" and "Phaser emits townSelected intent to React, not game-state mutation" and "React/API command starts the game only after the existing start flow confirms the selected town." The smallest honest path that preserves the existing flow: clicking a map town calls `onSelectTown(townId)` which triggers the same `handleStartWithTown` → `startNewGame` path. That IS React-owned confirmation (React calls the API, not Phaser). Document your choice in the report.
- Show a small React-owned legend or detail text below the map (e.g. "Click a town to ride out from there." or showing the selected town name + ride-day context). Keep copy concrete and in-world per the unslop profile.
- Keep the loading state ("Saddling up the map…") while the map data is fetching.
- You may keep the existing button list as a fallback/secondary control, or replace it entirely with the map. The plan says "present a spatial map rather than only a list" — replacing the list with the map is the goal, but keep a non-canvas confirmation control in React if you introduce select-then-confirm.

### Test strategy

- `PhaserMapHost.test.tsx` — test the adapter seam in jsdom. Phaser may not fully render in jsdom (no WebGL). You may need to mock Phaser or test that: (a) the host creates and destroys a game instance on mount/unmount, (b) `onTownSelected` is called when a town is selected. Consider mocking the `phaser` module to capture game create/destroy calls and emit synthetic pointer events. The key proof is mount/unmount cleanup and intent flow, not visual rendering.
- `StartingTownStep.test.tsx` — update to mock `getStartingTownMap` instead of `getStartingTowns`. Assert the map host renders, loading state, heading/copy, and that selecting a town calls `onSelectTown`.
- `StartFlow.test.tsx` — update the mock to include `getStartingTownMap` and prime it with map data. The flow test should still pass (select town → createGame called with startingTownId).

## Validation

Run from `src/WildBunch.Web/`:
- `npm install` (to install phaser)
- `npm run typecheck` — must pass
- `npm run build` — must pass
- `npm test` — must pass (all tests, including updated ones)

## Architecture rules (binding)

- Phaser is a renderer/input adapter only. It must not own Wild Bunch truth.
- React owns the selected town and the confirm action.
- Use styled-components, not plain CSS classes. Reference design tokens via `var(--token-name)`.
- Apply the play-surface-ui unslop profile: in-world trail map feel, no dashboard drift, no product chrome.
- Do not add comments. Follow existing code style.
- Do not break the existing start flow — the game must still start through `handleStartWithTown` → `startNewGame`.
