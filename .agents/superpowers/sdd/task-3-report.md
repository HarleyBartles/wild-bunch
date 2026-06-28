# Task 3 Report: Phaser-backed world map host for starting-town selection

## What I implemented

### Phaser dependency
- Installed `phaser@^3.90.0` (stable Phaser 3) via `npm install phaser@^3.80.0` from `src/WildBunch.Web/`.
- Added to `package.json` dependencies.

### Map DTO types (`src/api/types.ts`)
- Added `StartingTownMapTownDto` (id, name, services, x, y, selectable)
- Added `StartingTownMapTrailDto` (id, fromTownId, toTownId, rideDayDistance)
- Added `StartingTownMapDto` (towns, trails)

### API function (`src/api/wildBunchApi.ts`)
- Added `getStartingTownMap()` → `GET /api/games/starting-town-map` → `StartingTownMapDto`

### PhaserMapHost component (`src/components/start-flow/PhaserMapHost.tsx`)

**Phaser scene structure:**
- `StartingTownMapScene` extends `Phaser.Scene` with key `"starting-town-map"`.
- Constructor receives `mapData`, `selectedTownId`, and `onTownSelected` callback.
- `selectTown(townId)` — validates the town is `selectable` before calling `onTownSelected(townId)`. This is the adapter seam method that the scene's click handlers call.
- `create()` — renders the map:
  - Computes a bounding box from town x/y coordinates and scales to fit the 800×500 canvas with 70px padding.
  - Draws trail edges as lines (`this.add.graphics`) between connected towns with ride-day distance labels (`this.add.text`).
  - Draws town markers as circles (`this.add.circle`): selectable towns in gold (0xc9a84c, radius 14), non-selectable in muted brown (0x5a4d3f, radius 9).
  - Highlights the selected town with a thick stroke (0xf0e6d2).
  - Makes selectable towns interactive (`setInteractive`, `pointerover`/`pointerout` scale, `pointerdown` → `selectTown`).
  - Non-selectable towns are rendered but not clickable.
  - Town name labels below each marker.

**React host component:**
- `PhaserMapHost` props: `mapData`, `selectedTownId`, `onTownSelected`.
- Creates the Phaser game in `useEffect` with `Phaser.Scale.FIT` + `CENTER_BOTH`.
- `onTownSelected` stored in a ref to avoid stale closures and prevent game recreation on callback identity changes.
- Cleanup function calls `game.destroy(true)` — explicit mount/unmount lifecycle.
- `useEffect` deps: `[mapData, selectedTownId]` — recreates the game if data changes (stable in practice with `staleTime: Infinity`).
- Wrapper is a `styled.div` (`MapCanvas`) with `role="img"` and `aria-label="Trail map of starting towns"` for accessibility.
- Phaser does NOT call `POST /api/games`, does NOT decide eligibility, does NOT store selected-town truth.

### StartingTownStep changes (`src/components/start-flow/StartingTownStep.tsx`)
- Switched data source from `getStartingTowns` to `getStartingTownMap` (queryKey: `["starting-town-map"]`).
- Renders `PhaserMapHost` with the map data when loaded.
- Keeps the `StepHeading` "Pick a starting town" and both `StepLead` paragraphs (existing copy unchanged).
- Keeps the loading state "Saddling up the map…" while fetching or when towns list is empty.
- Added a `MapLegend` paragraph below the map: "Click a town on the map to ride out from there." (in-world Western copy per unslop profile).
- **Kept the existing button list** (`TownList` of `TownCard` with "Start in {name}" buttons) as an accessible fallback below the map. Both the map and buttons call `onSelectTown`.

### Confirmation-path choice: immediate-select-then-start

I kept the existing immediate-select-then-start behavior. Clicking a town on the map (or a fallback button) calls `onSelectTown(townId)`, which triggers the same `handleStartWithTown` → `flow.setSelectedTownId` → `flow.goToStep("creating")` → `startNewGame(request)` path in `PreSessionSurface.tsx`.

**Why:** This is the smallest honest path that preserves the existing flow. React still owns the confirm action — React calls the API (`startNewGame`), not Phaser. Phaser only raises selection intent. The brief explicitly allows this: "The smallest honest path that preserves the existing flow: clicking a map town calls `onSelectTown(townId)` which triggers the same `handleStartWithTown` → `startNewGame` path. That IS React-owned confirmation."

I kept the button list as an accessible fallback rather than replacing it entirely, which the brief also allows: "You may keep the existing button list as a fallback/secondary control." This ensures keyboard/screen-reader users can still select a town without the Phaser canvas.

## What I tested + actual command output

### `npm run typecheck`
```
> wildbunch-web@0.0.0 typecheck
> tsc --noEmit
```
Exit code: 0 — pass.

### `npm run build`
```
> wildbunch-web@0.0.0 build
> tsc --noEmit && vite build

vite v6.4.3 building for production...
✓ 230 modules transformed.
dist/index.html                     0.42 kB │ gzip:   0.28 kB
dist/assets/index-CzqsXKvX.css      1.19 kB │ gzip:   0.67 kB
dist/assets/index-DDskXgRV.js   1,915.56 kB │ gzip: 471.78 kB

(!) Some chunks are larger than 500 kB after minification.
✓ built in 6.33s
```
Exit code: 0 — pass. (Chunk size warning expected with Phaser bundled.)

### `npm test`
```
 Test Files  21 passed (21)
      Tests  166 passed (166)
   Duration  9.12s
```
Exit code: 0 — all 21 test files, 166 tests pass.

### Test files created/updated

**`PhaserMapHost.test.tsx`** (7 tests):
- Creates a Phaser game on mount
- Destroys the Phaser game on unmount
- Emits onTownSelected when a selectable town is selected through the scene
- Does not emit onTownSelected for a non-selectable town
- Does not emit onTownSelected for an unknown town id
- Passes the selectedTownId to the scene for highlighting
- Creates exactly one game per mount

**`StartingTownStep.test.tsx`** (10 tests):
- Renders towns fetched from the backend
- Renders the Phaser map host (aria-label assertion)
- Calls onSelectTown with the town id when a town button is selected
- Shows the loading state copy while the map is fetching
- Shows the loading state copy when the fetch resolves to an empty town list
- Renders the heading copy from the copy doc
- Renders the body copy from the copy doc
- Does not render a Back button
- Does not render town buttons while loading
- Renders the map legend copy below the map

**`StartFlow.test.tsx`** (7 tests, all pre-existing — updated mocks only):
- Added `getStartingTownMap: vi.fn()` to the api mock and primed it with map data
- Added `vi.mock("phaser", ...)` to prevent Phaser from loading in jsdom
- All existing flow tests still pass (select town → createGame called with startingTownId)

**`test-utils/setup.ts`**:
- Added a global `vi.mock("phaser", ...)` to the test setup so any test that transitively imports Phaser (via the router/component tree) gets the mock. Test files with their own `vi.mock("phaser")` override this.

## Files changed

- `src/WildBunch.Web/package.json` — added `phaser@^3.90.0` dependency
- `src/WildBunch.Web/package-lock.json` — lockfile updated
- `src/WildBunch.Web/src/api/types.ts` — added `StartingTownMapTownDto`, `StartingTownMapTrailDto`, `StartingTownMapDto`
- `src/WildBunch.Web/src/api/wildBunchApi.ts` — added `getStartingTownMap()` function and import
- `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx` — **created** (Phaser host + scene)
- `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx` — switched to `getStartingTownMap`, renders `PhaserMapHost`, kept button fallback, added map legend
- `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx` — **created** (adapter seam tests)
- `src/WildBunch.Web/src/tests/StartingTownStep.test.tsx` — updated mocks for `getStartingTownMap` + `phaser`, added map host and legend assertions
- `src/WildBunch.Web/src/tests/StartFlow.test.tsx` — added `getStartingTownMap` mock + `phaser` mock
- `src/WildBunch.Web/src/tests/test-utils/setup.ts` — added global `phaser` mock
- `.gitignore` — un-ignored `.agents/superpowers/sdd/` (from prior task setup, committed with this change)

## How Phaser is mocked in tests

The `phaser` module is mocked at two levels:

1. **Global setup** (`src/tests/test-utils/setup.ts`): A basic `vi.mock("phaser", ...)` provides mock `Game` (with `destroy`), `Scene`, and `Scale` classes. This prevents the real Phaser from loading and trying to use WebGL/canvas in jsdom for any test that transitively imports it (e.g., `AppShell.test.tsx`, `StartOverConfirmation.test.tsx`).

2. **Test-file level** (`PhaserMapHost.test.tsx`): A more specific mock using `vi.hoisted` to share a `mockState.games` array that tracks created game instances. The mock `Game` constructor pushes itself to this array, and the test accesses `mockState.games[0].config.scene` to get the `StartingTownMapScene` instance and call `selectTown()` directly. This overrides the global setup mock for that test file.

3. **Test-file level** (`StartingTownStep.test.tsx`, `StartFlow.test.tsx`): Each has its own `vi.mock("phaser", ...)` with the same basic mock structure, overriding the global setup.

The mock `Scene` class is a minimal base class that `StartingTownMapScene` extends. The mock `Game` class stores its config (including the scene instance) and tracks `destroyed` state. The scene's `create()` method is never called by the mock (no Phaser lifecycle), so the rendering logic doesn't run — the tests focus on the adapter seam (mount/unmount cleanup + intent flow via `selectTown`).

## Self-review findings

- **Phaser is presentation/input only**: The scene receives `mapData` and `selectedTownId` as props and emits `onTownSelected`. It does not call any API, does not calculate legal moves, does not store truth. ✅
- **React owns confirmation**: `onSelectTown` → `handleStartWithTown` → `startNewGame`. React calls the API. ✅
- **styled-components**: All layout uses styled-components (`MapCanvas`, `MapLegend`, `StepCard`, etc.). No plain CSS classes in `className`. Design tokens referenced via `var(--token-name)`. ✅
- **No inline styles**: No `style={{}}` props in any component file. ✅ (verified by `stylingEnforcement.test.ts`)
- **No comments**: No comments added to any file. ✅
- **In-world copy**: "Click a town on the map to ride out from there." — concrete Western copy, no dashboard/product chrome drift. ✅
- **Loading state preserved**: "Saddling up the map…" shows while fetching or when towns list is empty. ✅
- **Heading/body copy preserved**: Unchanged from original. ✅
- **No Back button**: Not rendered on the town step. ✅
- **Explicit cleanup**: `game.destroy(true)` in `useEffect` cleanup. ✅
- **Callback ref pattern**: `onTownSelectedRef` prevents stale closures without causing game recreation. ✅
- **selectable validation**: `selectTown` checks `town.selectable` before emitting intent — non-selectable and unknown towns are silently ignored. ✅
- **Build chunk size**: Phaser adds ~1.9MB to the bundle. This is expected for a Phaser-based surface. Future work could code-split the Phaser chunk.

## Concerns

1. **Bundle size**: Phaser 3.90.0 adds ~1.9MB (471KB gzipped) to the production bundle. The build warns about chunks >500KB. This is inherent to including Phaser. A future task could use dynamic `import()` to code-split the Phaser map host so it only loads on the starting-town step.

2. **Game recreation on data change**: The `useEffect` deps include `mapData` and `selectedTownId`. If `mapData` gets a new reference (e.g., from a React Query refetch), the Phaser game is destroyed and recreated. With `staleTime: Infinity` and `retry: false`, this shouldn't happen in practice, but it's worth noting. A future optimization could diff the data and update the scene in place rather than recreating.

3. **Button list retained**: I kept the button list as an accessible fallback rather than replacing it entirely with the map. The brief allows this ("You may keep the existing button list as a fallback/secondary control") and says "replacing the list with the map is the goal." A future task could remove the button list once the map is confirmed to be accessible enough, or replace it with a screen-reader-only control.

## Review fix pass — Minor findings

### Finding 1: Non-selectable fallback buttons

**File:** `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx`
**Issue:** The fallback button list mapped over `mapData.towns` without filtering `selectable`, so non-selectable towns got "Start in {name}" buttons. The Phaser layer correctly guards non-selectable towns (no interactive marker, `selectTown` validates `selectable`), but the React fallback did not.
**Fix:** Filtered the button list to only selectable towns: `mapData.towns.filter((town) => town.selectable)`. This keeps the fallback consistent with the map's clickable markers.

### Finding 2: Weak selectedTownId test

**File:** `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`
**Issue:** The test "passes the selectedTownId to the scene for highlighting" only asserted `scene instanceof StartingTownMapScene` — it did not verify the `selectedTownId` was actually passed to the scene.
**Fix:** Made `selectedTownId` on `StartingTownMapScene` a `public readonly` field (was `private readonly`) so the test can read it without exposing a mutable setter. Added the assertion `expect(scene.selectedTownId).toBe("dust-fork")` to the test. The field remains readonly — no mutation path was introduced.

### Validation output

#### `npm run typecheck`
```
> wildbunch-web@0.0.0 typecheck
> tsc --noEmit
```
Exit code: 0 — pass.

#### `npm test`
```
 Test Files  21 passed (21)
      Tests  166 passed (166)
   Duration  9.21s
```
Exit code: 0 — all 21 test files, 166 tests pass.
