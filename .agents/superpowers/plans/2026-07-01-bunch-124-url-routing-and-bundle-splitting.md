# BUNCH-124: URL Routing + Vite Bundle Splitting

**Linear:** [BUNCH-124](https://linear.app/harleys-workspace/issue/BUNCH-124/introduce-url-routing-split-vite-bundle-lazy-load-surfaces-isolate)
**Branch:** `harleydbartles/bunch-124-introduce-url-routing-split-vite-bundle-lazy-load-surfaces`
**Date:** 2026-07-01

## Problem

The Wild Bunch web app has two related problems:

1. **Single-route pattern.** The entire app runs under a single URL route (`/`). `GameFlowRouter` switches surfaces internally via `useGamePhase()` + local `activePlace` state. There's no deep-linking, no back-button support, no URL reflection of game state, and no route-level code splitting.

2. **Single 1.9 MB JS chunk.** The Vite production build emits one ~1.9 MB JS chunk (~474 kB gzipped) with a `chunkSizeWarningLimit` warning. Phaser (~1 MB+) is eagerly imported even though it's only needed for map surfaces. No vendor chunk splitting.

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

A `usePhaseRouteSync` hook runs in `AppShell` (the root route component):

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

Phaser isolation: `PhaserMapHost` is imported by `StartingTownStep` (inside `PreSessionSurface`) and `TravelPrepSurface` (the trailhead route). Both are now behind lazy imports, so Phaser naturally lands in its own chunk. No special handling needed — Rollup splits it because it's only reachable through dynamic imports.

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

Phaser is deliberately NOT in `manualChunks` — it isolates naturally via the lazy import.

`defaultPreload: "intent"` is already set — hovering over a town place card will preload that route's chunk before the click.

### Expected chunk shape

- `vendor.js` — React + ReactDOM (~130 kB)
- `router.js` — TanStack Router + Query (~80 kB)
- `styled.js` — styled-components (~45 kB)
- `phaser.js` — Phaser (~1 MB, only loaded when map surfaces are visited)
- `index.js` — app shell + root route (~small)
- `start-flow.js` — PreSessionSurface + setup steps (~medium, pulls phaser.js)
- `town-hub.js` — TownHubSurface (~small)
- `town-place-*.js` — each town place (~small each)
- `trail.js` — TrailFlowSurface (~small)

The main `index.js` chunk should be well under 500 kB.

## File changes

### New files

- `src/shell/usePhaseRouteSync.ts` — phase <-> URL reconciliation hook
- `src/shell/RouteLoading.tsx` — minimal Suspense fallback (or inline in AppShell)

### Removed files

- `src/flow/GameFlowRouter.tsx` — routing moves to TanStack Router route tree
- `src/flow/ArrivalSurface.tsx` — arrival content moves into TrailFlowSurface's completed-journey view

### Modified — router & shell

- `src/shell/router.tsx` — expand route tree: `/` (start flow), `/town` + children (`/store`, `/sheriff`, `/saloon`, `/trailhead`), `/trail`. All lazy-loaded with Suspense.
- `src/shell/AppShell.tsx` — call `usePhaseRouteSync()` in `ShellChrome`.

### Modified — flow surfaces

- `src/flow/TownHubSurface.tsx` — remove `activePlace`/`onPlaceChange` props. Place cards use `useNavigate()`. Add arrival notice rendering when `?arrived=1` query param present.
- `src/flow/TrailFlowSurface.tsx` — add completed-journey view: when `journey.status === Completed`, render last day's resolved content + arrival summary + "Step into town" button.
- `src/flow/PreSessionSurface.tsx` — no functional change (lazy-loaded only).
- `src/flow/places/StorePlace.tsx` — `onLeave` -> `useNavigate()` to `/town`.
- `src/flow/places/SheriffPlace.tsx` — `onLeave` -> `useNavigate()` to `/town`.
- `src/flow/places/SaloonPlace.tsx` — `onLeave` -> `useNavigate()` to `/town`.
- `src/flow/TravelPrepSurface.tsx` — `onBack` -> `useNavigate()` to `/town`.

### Modified — hooks

- `src/hooks/useGamePhase.ts` — remove `"arrival"` from `GamePhase`. `journey.status === Completed` maps to `"on-trail"`. Remove `isArrivalPending`.

### Modified — build config

- `vite.config.ts` — add `build.rollupOptions.output.manualChunks`.

### Modified — tests

- 5 `RouterProvider` test files (`StorePlaceFeedback`, `StartOverConfirmation`, `StartOverRegression`, `GameSettingsOverlay`, `AppShell`) — update for new route tree.
- `SheriffPlace.test.tsx`, `TravelPrepSurface.test.tsx` — mock `useNavigate` or adjust for removed props.

### New tests

- `usePhaseRouteSync` test — verify redirect on mismatch, no-op on match, no redirect during session load.
- `TrailFlowSurface` completed-journey test — verify arrival content shows when journey completed.

### Untouched

- `GameSessionProvider.tsx` — `handleAcknowledgeArrival` stays as-is; the sync hook handles the route change after session updates.
- `PhaserMapHost.tsx` — no change (lazy-loaded via its parents).
- `useTownStoreOffers.ts`, `useGameSession.ts`, API layer — no change.
- Backend — no changes. This is frontend-only.

## Out of scope

- Backend changes.
- Splitting `PreSessionSurface` sub-steps into separate URL routes.
- Memoization / render optimization beyond code splitting.

## Validation

- `npm test -- --run` — all tests pass (updated + new).
- `npm run build` — passes without chunk-size warning.
- Manual playtest: deep-link to `/town/store` works, back button works, trail -> town transition shows arrival content, Phaser only loads when map surfaces are visited.
