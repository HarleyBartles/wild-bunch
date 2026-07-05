# BUNCH-124: URL Routing + Vite Bundle Splitting

**Linear:** [BUNCH-124](https://linear.app/harleys-workspace/issue/BUNCH-124/introduce-url-routing-split-vite-bundle-lazy-load-surfaces-isolate)
**Branch:** `harleydbartles/bunch-124-split-vite-bundle-url-routing`
**Date:** 2026-07-01 (refactored 2026-07-05 against current `main` @ `ecbb3a6`)

## Problem

The Wild Bunch web app has two related problems:

1. **Single-route pattern.** The entire app runs under a single URL route (`/`). `GameFlowRouter` switches surfaces internally via `useGamePhase()` + local `activePlace` state. There's no deep-linking, no back-button support, no URL reflection of game state, and no route-level code splitting.

2. **Single 1.9 MB JS chunk.** The Vite production build emits one ~1.9 MB JS chunk (~474 kB gzipped) with a `chunkSizeWarningLimit` warning. Phaser (~1 MB+) is eagerly imported even though it's only needed for map surfaces. No vendor chunk splitting.

## Current source state (verified against `main` @ `ecbb3a6`)

This section reflects the actual source as of the refactor date. The implementation plan must be read against this state, not the original 2026-07-01 state.

### Router (`src/shell/router.tsx`)

Single route tree — root route (`AppShell`) with one child `/` route whose component is `GameFlowRouter`. No other routes are wired in.

```tsx
const rootRoute = createRootRoute({ component: AppShell });
const indexRoute = createRoute({ getParentRoute: () => rootRoute, path: "/", component: GameFlowRouter });
const routeTree = rootRoute.addChildren([indexRoute]);
export const router = createRouter({ routeTree, defaultPreload: "intent" });
```

### App shell (`src/shell/AppShell.tsx`)

`AppShell` is a thin wrapper around `ShellChrome`. `ShellChrome` renders the chrome bar (Hud + GlobalOverlays + DevOverlay toggle) and a `<RouteOutlet>` containing `<Outlet />`. The sync hook belongs in `ShellChrome` (it already uses `useState`/`useRef`/`useEffect`).

### Flow router (`src/flow/GameFlowRouter.tsx`)

Switches on `useGamePhase().phase`:
- `pre-session` / `setup` / `prologue` / `town-selection` → `<PreSessionSurface />`
- `in-town` → `<TownHubSurface activePlace={activePlace} onPlaceChange={setActivePlace} />`
- `on-trail` → `<TrailFlowSurface />`
- `arrival` → `<ArrivalSurface />`

Also exports `TownPlace` type (imported by `TownHubSurface`). When `GameFlowRouter` is removed, `TownPlace` must move or be inlined.

### Phase hook (`src/hooks/useGamePhase.ts`)

`GamePhase` includes `"arrival"`. `journey.status === JourneyStatus.Completed` maps to `"arrival"` with `isArrivalPending: true`. `StartFlowPhase` is a numeric enum (`0..4`) with `StartingTownSelected: 3` and `GameStarted: 4`. Both `PrologueViewed` (2) and `StartingTownSelected` (3) map to `"town-selection"`.

### Surfaces

- `PreSessionSurface` — statically imports `SetupHuntStep`, `StorySoFarStep`, `StartingTownStep`, `CreatingStep`. Renders based on `effectiveStep` derived from phase + local flow step.
- `TownHubSurface` — takes `activePlace`/`onPlaceChange` props. When a place is active, renders the place surface with a back button; otherwise renders place cards. Imports `TownPlace` from `GameFlowRouter`.
- `TrailFlowSurface` — renders `TravelPanel` with a trail-lock banner. No completed-journey handling.
- `ArrivalSurface` — renders arrival card with "Step into town" button calling `handleAcknowledgeArrival`.
- `TravelPrepSurface` — takes `onBack` prop. Has an internal `TravelMapSelection` component that imports `PhaserMapHost`.
- `StorePlace` / `SheriffPlace` / `SaloonPlace` — each takes `onLeave` prop, renders `BackButton` calling `onLeave`.

### Phaser import chains (verified)

- `PreSessionSurface` → (static) `StartingTownStep` → (static) `PhaserMapHost` → `phaser`
- `TravelPrepSurface` → (internal `TravelMapSelection`) `PhaserMapHost` → `phaser`

### Orphaned `src/routes/` directory

`src/routes/` contains `HuntRoute.tsx`, `TrailRoute.tsx`, `CaseFileRoute.tsx`, `WantedRoute.tsx`. These are **not wired into the router** and **not imported anywhere**. They appear to be from an earlier abandoned routing attempt. Their responsibilities overlap with `GlobalOverlays` (case file, wanted posters as overlays) and `TrailFlowSurface`/`TravelPrepSurface` (trail/travel). This plan removes them as dead code.

### Tests (verified)

- `GameFlowRouter.test.tsx` — tests `GameFlowRouter` directly (not via `RouterProvider`). Will break when `GameFlowRouter` is removed; must be deleted or replaced.
- `AppShell.test.tsx` — uses `RouterProvider` with the real `router`. Will need route tree updates.
- `StorePlaceFeedback.test.tsx`, `StartOverConfirmation.test.tsx`, `StartOverRegression.test.tsx`, `GameSettingsOverlay.test.tsx` — all use `RouterProvider` with the real `router`. Will need route tree updates.
- `SheriffPlace.test.tsx`, `TravelPrepSurface.test.tsx` — test place surfaces directly with `onLeave`/`onBack` props. Will need `useNavigate` mocking or prop adjustment.
- `StartingTownStep.test.tsx` — tests `StartingTownStep` directly. Unaffected by lazy-loading (lazy boundary is at the consumer).
- `PhaserMapHost.test.tsx` — mocks `phaser`. Unaffected.

### Build config (`vite.config.ts`)

Currently minimal — `defineConfig` from `vitest/config`, `@vitejs/plugin-react`, test config, server config. No `build.rollupOptions`. The `manualChunks` addition goes here.

### Dependencies (`package.json`)

- `@tanstack/react-router` `^1.170.16`, `@tanstack/react-query` `^5.100.14`
- `phaser` `^3.90.0`, `react`/`react-dom` `^18.3.1`, `styled-components` `^6.4.2`
- Build: `tsc --noEmit && vite build`. Test: `vitest run`.

## Design

### URL structure

| URL | Phase | Surface | Lazy chunk |
|---|---|---|---|
| `/` | pre-session, setup, prologue, town-selection | `PreSessionSurface` | `start-flow` |
| `/town` | in-town | `TownHubSurface` | `town-hub` |
| `/town/store` | in-town, store | `StorePlace` | `town-place-store` |
| `/town/sheriff` | in-town, sheriff | `SheriffPlace` | `town-place-sheriff` |
| `/town/saloon` | in-town, saloon | `SaloonPlace` | `town-place-saloon` |
| `/town/trailhead` | in-town, trailhead | `TravelPrepSurface` | `town-place-trailhead` |
| `/trail` | on-trail (active or completed) | `TrailFlowSurface` | `trail` |

- `PreSessionSurface` keeps handling its sub-steps (setup -> prologue -> town-selection) internally. Those aren't separate URL routes — they're a single start-flow surface that progresses with backend state.
- Town places are separate routes because they're meaningful navigation targets the player can return to.
- `GameFlowRouter` is removed. Routing is the router's job.

### Arrival flow rework

The current `ArrivalSurface` exists for the wrong reason. The real problem it was patching: on the last trail day, the encounter plays out, the journey completes, and the app immediately jumps to town — skipping the resolution of the last day's content. The player needs to see the last day play out, then explicitly acknowledge the trail is done before transitioning.

Revised design:
- `journey.status === Completed` maps to `on-trail` (not a separate phase). URL stays `/trail`.
- `TrailFlowSurface` checks `journey.status`:
  - `Active` / `Interrupted` -> render the current trail day normally (encounter, choices, next-day button).
  - `Completed` -> render the last day's resolved content PLUS an arrival summary and "Step into town" button. The player sees how the last day played out, then acknowledges.
- On acknowledge (`handleAcknowledgeArrival`) -> session updates -> journey clears -> `useGamePhase` derives `in-town` -> sync navigates `/trail` -> `/town?arrived=1`.
- `TownHubSurface` shows a brief arrival notice when `?arrived=1` is present, clears on dismiss.
- `ArrivalSurface.tsx` is removed.

### Phase-sync logic

A `usePhaseRouteSync` hook runs in `ShellChrome` (inside `AppShell`, the root route component):

1. Read session -> derive phase via `useGamePhase()`.
2. Read current URL via TanStack Router's `useLocation()` / `useRouterState()`.
3. Map phase -> expected URL prefix:
   - pre-session / setup / prologue / town-selection -> `/`
   - in-town -> `/town` (sub-routes OK — `/town/store` etc. all match `in-town`)
   - on-trail -> `/trail`
4. If URL doesn't match expected phase, navigate to the phase's base URL.
5. Skip sync on first render while session is still loading.

Key rules:
- **Phase determines the top-level route.** Within `/town/*`, the player navigates freely between store/sheriff/saloon/trailhead — the sync only cares that they're somewhere under `/town`.
- **Backend transitions drive navigation.** When the player travels (trail -> town), the session updates, `useGamePhase` returns a new phase, and the sync navigates to the new route.
- **Player navigation within a phase is free.** Clicking "Store" in the town hub navigates to `/town/store`. That's a player-initiated route change within the same phase.
- **Stale URLs redirect.** If the player hits `/town/store` but has no session, the sync redirects to `/`.
- **No redirect loops.** The sync only navigates when phase != URL phase. Once they match, it's stable.

### Lazy loading & chunk splitting

Each route component is `React.lazy` + dynamic `import()`, wrapped in `<Suspense>` at the route outlet level in `AppShell`:

```tsx
const StartFlow = lazy(() => import("./flow/PreSessionSurface"));
const TownHub = lazy(() => import("./flow/TownHubSurface"));
const StorePlace = lazy(() => import("./flow/places/StorePlace"));
// ... etc.
```

`RouteLoading` fallback: a minimal styled-components spinner or muted "Loading..." — same visual register as existing `Muted` loading states.

Phaser isolation requires explicit dynamic import boundaries — route-level lazy loading alone is NOT sufficient. The current static import chains are:

- `PreSessionSurface` -> `StartingTownStep` -> `PhaserMapHost` -> `phaser`
- `TravelPrepSurface` -> `TravelMapSelection` (internal) -> `PhaserMapHost` -> `phaser`

Lazy-loading `PreSessionSurface` as a route component would still pull Phaser into the start-flow chunk, because `StartingTownStep` is statically imported by `PreSessionSurface` and statically imports `PhaserMapHost`. The player would download Phaser on the very first page load (name/prologue step) before they ever reach town selection.

Explicit Phaser isolation boundaries:

1. **`StartingTownStep` lazy-loaded inside `PreSessionSurface`.** `PreSessionSurface` renders `StartingTownStep` only when `effectiveStep === "town"`. Replace the static import with `React.lazy(() => import("../components/start-flow/StartingTownStep"))` and wrap in `<Suspense>`. This ensures Phaser is only loaded when the player reaches the town-selection step, not during name/prologue.

2. **`TravelPrepSurface` already lazy-loaded as a route component** (`/town/trailhead`). Since `PhaserMapHost` is statically imported by `TravelPrepSurface`'s internal `TravelMapSelection`, Phaser lands in the trailhead route chunk — which is only loaded when the player navigates to the trailhead. This is sufficient for the trailhead side.

3. **`PhaserMapHost` itself is NOT modified.** The isolation happens at the consumer boundary (`StartingTownStep` lazy import, `TravelPrepSurface` route lazy import), not inside `PhaserMapHost`.

If future start-flow steps need Phaser earlier, the lazy boundary should move to wrap `PhaserMapHost` directly inside `StartingTownStep` instead. The implementation plan should verify the chosen boundary by inspecting the Vite build output (see Acceptance checks below).

Vendor chunk splitting via `manualChunks`:

```ts
build: {
  rollupOptions: {
    output: {
      manualChunks: {
        vendor: ["react", "react-dom"],
        router: ["@tanstack/react-router", "@tanstack/react-query"],
        styled: ["styled-components"],
      },
    },
  },
},
```

Note: `vite.config.ts` currently imports `defineConfig` from `vitest/config`. The `build.rollupOptions` key is valid on the same `defineConfig` call — no import change needed.

Phaser is deliberately NOT in `manualChunks` — it isolates naturally via the lazy import.

`defaultPreload: "intent"` is already set — hovering over a town place card will preload that route's chunk before the click.

### Expected chunk shape

- `vendor.js` — React + ReactDOM (~130 kB)
- `router.js` — TanStack Router + Query (~80 kB)
- `styled.js` — styled-components (~45 kB)
- `phaser.js` — Phaser (~1 MB, only loaded when town-selection or trailhead surfaces are visited)
- `index.js` — app shell + root route (~small)
- `start-flow.js` — PreSessionSurface + SetupHuntStep + StorySoFarStep + CreatingStep (~small, does NOT include Phaser)
- `StartingTownStep.js` — StartingTownStep + PhaserMapHost (~medium, pulls phaser.js — only loaded when effectiveStep === "town")
- `town-hub.js` — TownHubSurface (~small)
- `town-place-*.js` — each town place (~small each)
- `town-place-trailhead.js` — TravelPrepSurface + TravelMapSelection + PhaserMapHost (~medium, pulls phaser.js — only loaded when player navigates to trailhead)
- `trail.js` — TrailFlowSurface (~small)

The main `index.js` chunk should be well under 500 kB. The `start-flow.js` chunk must NOT contain Phaser — Phaser should only appear in `phaser.js`, `StartingTownStep.js`, and `town-place-trailhead.js`.

### Acceptance checks for Phaser isolation

After implementation, inspect the Vite build output to prove Phaser is isolated:

1. Run `npm run build` and examine the chunk list in the output.
2. Verify no chunk loaded on the initial `/` route (before town selection) contains Phaser. The `start-flow.js` chunk (or equivalent) must not import or bundle `phaser`.
3. Verify Phaser appears in a separate chunk that is only referenced by `StartingTownStep` and `TravelPrepSurface` (the trailhead route).
4. If Phaser still appears in the start-flow chunk, the lazy boundary is wrong — move it closer to `PhaserMapHost` (e.g. lazy-load `PhaserMapHost` inside `StartingTownStep` instead of lazy-loading `StartingTownStep` inside `PreSessionSurface`).

## File changes

### New files

- `src/shell/usePhaseRouteSync.ts` — phase <-> URL reconciliation hook
- `src/shell/RouteLoading.tsx` — minimal Suspense fallback (or inline in AppShell)

### Removed files

- `src/flow/GameFlowRouter.tsx` — routing moves to TanStack Router route tree
- `src/flow/ArrivalSurface.tsx` — arrival content moves into TrailFlowSurface's completed-journey view
- `src/routes/HuntRoute.tsx` — orphaned dead code (not wired into router, not imported anywhere)
- `src/routes/TrailRoute.tsx` — orphaned dead code
- `src/routes/CaseFileRoute.tsx` — orphaned dead code (overlaps with `GlobalOverlays` case-file overlay)
- `src/routes/WantedRoute.tsx` — orphaned dead code (overlaps with `GlobalOverlays` wanted overlay)
- `src/routes/INDEX.md` — generated index for removed directory; regenerate index mesh after deletion
- `src/tests/GameFlowRouter.test.tsx` — tests the removed `GameFlowRouter`; replace with route-tree-aware tests in `AppShell.test.tsx` and the new `usePhaseRouteSync` test

### Modified — router & shell

- `src/shell/router.tsx` — expand route tree: `/` (start flow), `/town` + children (`/store`, `/sheriff`, `/saloon`, `/trailhead`), `/trail`. All lazy-loaded with Suspense. Remove `GameFlowRouter` import.
- `src/shell/AppShell.tsx` — call `usePhaseRouteSync()` in `ShellChrome`. Wrap `<Outlet />` in `<Suspense fallback={<RouteLoading />}>` (or place Suspense at the route component level — implementer's choice, but the fallback must be minimal).

### Modified — flow surfaces

- `src/flow/TownHubSurface.tsx` — remove `activePlace`/`onPlaceChange` props. Remove `import type { TownPlace } from "./GameFlowRouter"` — inline the place type or drop it (place cards use `useNavigate()` to `/town/store` etc.). Add arrival notice rendering when `?arrived=1` query param present (use `useSearch()` from TanStack Router).
- `src/flow/TrailFlowSurface.tsx` — add completed-journey view: when `journey.status === Completed`, render last day's resolved content + arrival summary + "Step into town" button (calls `handleAcknowledgeArrival`).
- `src/flow/PreSessionSurface.tsx` — lazy-loaded as route component. Additionally, replace static import of `StartingTownStep` with `React.lazy(() => import(...))` wrapped in `<Suspense>` so Phaser is not loaded for name/prologue steps.
- `src/flow/places/StorePlace.tsx` — remove `onLeave` prop. Replace `BackButton onClick={onLeave}` with `useNavigate()` to `/town`.
- `src/flow/places/SheriffPlace.tsx` — remove `onLeave` prop. Replace `BackButton onClick={onLeave}` with `useNavigate()` to `/town`.
- `src/flow/places/SaloonPlace.tsx` — remove `onLeave` prop. Replace `BackButton onClick={onLeave}` with `useNavigate()` to `/town`.
- `src/flow/TravelPrepSurface.tsx` — remove `onBack` prop. Replace `BackButton onClick={onBack}` with `useNavigate()` to `/town`.

### Modified — hooks

- `src/hooks/useGamePhase.ts` — remove `"arrival"` from `GamePhase`. `journey.status === Completed` maps to `"on-trail"`. Remove `isArrivalPending` from `GamePhaseState`. Update the doc comment.

### Modified — build config

- `vite.config.ts` — add `build.rollupOptions.output.manualChunks`.

### Modified — tests

- `AppShell.test.tsx` — update for new route tree. Tests that assumed `GameFlowRouter` rendered surfaces based on phase now need to either navigate to the right URL or mock the phase-sync hook.
- `StorePlaceFeedback.test.tsx`, `StartOverConfirmation.test.tsx`, `StartOverRegression.test.tsx`, `GameSettingsOverlay.test.tsx` — update for new route tree. These use `RouterProvider` with the real router; the route tree change means they may need to render at a specific URL or adjust navigation expectations.
- `SheriffPlace.test.tsx` — remove `onLeave` prop usage. Mock `useNavigate` from `@tanstack/react-router` or render inside a `RouterProvider` and assert navigation.
- `TravelPrepSurface.test.tsx` — remove `onBack` prop usage. Mock `useNavigate` or render inside a `RouterProvider`.

### New tests

- `usePhaseRouteSync` test (`src/tests/usePhaseRouteSync.test.tsx`) — verify redirect on mismatch, no-op on match, no redirect during session load.
- `TrailFlowSurface` completed-journey test (`src/tests/TrailFlowSurface.test.tsx` or extend existing) — verify arrival content shows when `journey.status === Completed`.

### Untouched

- `GameSessionProvider.tsx` — `handleAcknowledgeArrival` stays as-is; the sync hook handles the route change after session updates.
- `PhaserMapHost.tsx` — no change (lazy-loaded via its parents).
- `StartingTownStep.test.tsx` — tests `StartingTownStep` directly; lazy boundary is at the consumer, so the test is unaffected.
- `PhaserMapHost.test.tsx` — mocks `phaser`; unaffected.
- `useTownStoreOffers.ts`, `useGameSession.ts`, `useCurrentGameSession.ts`, API layer — no change.
- `GlobalOverlays.tsx` — no change (case file / wanted / journal stay as overlays, not routes).
- Backend — no changes. This is frontend-only.

## Out of scope

- Backend changes.
- Splitting `PreSessionSurface` sub-steps into separate URL routes.
- Memoization / render optimization beyond code splitting.
- Promoting `GlobalOverlays` (case file, wanted, journal) to full routes — they stay as overlays.

## Validation

- `npm test -- --run` — all tests pass (updated + new).
- `npm run build` — passes without chunk-size warning.
- **Phaser isolation check:** inspect the Vite build output chunk list. Verify the start-flow chunk (loaded on initial `/` route before town selection) does NOT contain Phaser. Phaser should only appear in chunks reachable via `StartingTownStep` (town-selection step) and `TravelPrepSurface` (trailhead route). See "Acceptance checks for Phaser isolation" above.
- Manual playtest: deep-link to `/town/store` works, back button works, trail -> town transition shows arrival content, Phaser only loads when map surfaces are visited.
