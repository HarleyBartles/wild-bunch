# ADR-0027 UI v0.1 SPA Shell, Routing, Data Layer, and Player/Debug Separation

## Status

live

## Dated Status History

- 2026-06-21 - live: first v0.1 SPA shell with TanStack Router, React Query for all session data/mutations, persistent HUD, player routes, and a separated Dev tools route landed in `src/WildBunch.Web`. This is an early iteration-shaped shell (v0.1), not a final v1; it leaves clear seams for route, surface, and playfield growth.

## Decision Type

ui, architecture

## Related ADRs

- `depends on`: ADR-0016, ADR-0019
- `related to`: ADR-0011, ADR-0022

## Context

The web client had grown as a single cockpit page (`App.tsx`) that hosted every surface: start/continue hunt, field report, available actions, travel routes, log, and a cockpit-hosted case-file modal (per ADR-0011). The central session hook (`useCurrentGameSession`) hand-rolled all data fetching, mutation state, busy-mode tracking, and cache invalidation with `useState`/`useEffect`, despite the repo already having `@tanstack/react-query` installed and used correctly by `useTravelPanelState`. BUNCH-56 asked for a v0.1 SPA shape that moves beyond cockpit/debug posture toward player-facing navigation and production-shaped play surfaces, while preserving the cockpit as a temporary dev surface. This is deliberately labeled v0.1, not v1, to leave room for iteration on routing, surfaces, and the future playfield.

The repo already uses React, Vite, TanStack React Query, and styled-components (ADR-0016) with a manual typed API client (ADR-0019). There is no canvas/Phaser playfield yet, so the shell has to slot one in later without changing the state boundary.

## Decision Drivers

- The frontend must stay an adapter over server-authoritative game/application state; it must not become a second gameplay model.
- TanStack React Query is the repo's chosen tool for all query and mutation API handling; the central session hook must use it, not hand-rolled state.
- Player-facing navigation should feel like a game, not an admin sidebar, while protecting the playfield.
- The dense developer cockpit is temporary scaffolding, not a permanent fixture; it stays reachable as a dev surface but is clearly separated from player routes and may be replaced by a dev overlay in future work.
- ADR-0011 says new play surfaces start cockpit-hosted and promote to routes only when ready; the case file is now ready.
- ADR-0019 says stay manual on the API client until a generated client is justified; it does not constrain routing or data-fetching tool choices.
- ADR-0022 says browser checks are a manual evidence lane, not an automated CI gate.
- Styled-components is the preferred styling approach; SASS for CSS that cannot be done naturally within styled-components. Vertical slice is the preferred architecture for component grouping.

## Decision Summary

Establish a v0.1 SPA shell with TanStack Router for routing, React Query for all session data fetching and mutations, a persistent compact HUD, player-facing navigation, and routed play surfaces. One `GameSessionProvider` context wraps the refactored `useCurrentGameSession` hook (now fully on React Query) and feeds the HUD and every route. The case file is promoted from a cockpit-hosted modal to a canonical player route. The cockpit is preserved as a separated `Dev tools` route — a temporary dev surface, not a permanent fixture.

## Detailed Decision Breakdown

### Shell shape

```
App
└─ GameSessionProvider              // single authoritative session source (+ town store/buy)
   └─ RouterProvider (TanStack Router)
      └─ AppShell (root route)
         ├─ Hud                     // sticky compact status bar (player, day/turn, town, health, cash, heat, status)
         ├─ shell-nav               // player routes + separated "Dev tools"
         └─ Outlet (TanStack Router)
            ├─ /        Camp        // start / continue hunt (StartGamePanel)
            ├─ /hunt    Hunt        // FieldReportPanel + AvailableActionsPanel
            ├─ /case    Case file   // CaseFileSurface, promoted from modal
            ├─ /wanted  Wanted      // WantedPosterSurface
            ├─ /trail   Trail       // TravelRoutesPanel (+ TravelPanel when a journey is active)
            └─ /debug   Dev tools   // DebugCockpitRoute (temporary dev surface, unchanged behaviour)
```

### Routing: TanStack Router

Routing uses TanStack Router (code-based routes, not file-based). This pairs naturally with the already-installed TanStack React Query ecosystem. Routes are declared as a data array in `shell/router.tsx` using `createRootRoute`, `createRoute`, and `createRouter`. The root route's component is `AppShell`, which renders the HUD, nav, and `<Outlet />`. Nav links use TanStack Router's `<Link>` component. The router uses the browser History API (not hash routing).

This replaces an initial hand-rolled hash router that was carried over from a prior closed PR. The hash router was a questionable call — it overextended ADR-0019 (which covers the API client, not routing) and the react-patterns skill is explicitly router-agnostic, listing TanStack Router as a standard option. TanStack Router is the better choice because it handles nested routes, params, loaders, scroll restoration, and type-safe navigation — all things the hand-rolled router would need to grow into manually.

### Data layer: React Query for all session data and mutations

`useCurrentGameSession` is refactored from hand-rolled `useState`/`useEffect` to React Query:

- **Queries:** `useQuery` for session (`getGame`), actions (`getAvailableActions`), and journal (`getJournal`), keyed by gameId. Queries are `enabled: Boolean(gameId)` and read the game ID from localStorage synchronously via a `useState` lazy initializer.
- **Mutations:** `useMutation` for each game action (start game, travel, read wanted posters, inspect notice board, check local records, follow telegraph leads, gather local gossip, look around saloon, confront saloon person of interest). Each mutation's `onSuccess` sets query data via `queryClient.setQueryData` for immediate updates, then invalidates the relevant queries for refetch.
- **Busy state:** `busyMode` is derived from which mutation is pending, eliminating the hand-rolled state machine that previously required manual `setBusyMode` calls scattered across 8+ async handlers.
- **Cache invalidation:** `reloadCurrentGame` is now `queryClient.invalidateQueries`, replacing manual `hydrateGame` calls.
- **Local persistence:** game ID in localStorage via `useState` initializer, replacing the manual `useEffect` hydration.

This aligns the central session hook with the pattern `useTravelPanelState` already uses. The return shape of `useCurrentGameSession` is unchanged, so `GameSessionProvider` and all consumers work without modification.

### State boundary

`GameSessionProvider` is the single authoritative session source for the shell. It calls `useCurrentGameSession`, wires `useTownStoreOffers`, and owns `handleBuyOffer`. The HUD and every route consume state via `useGameSession()`. No gameplay logic moved into the UI; the provider only composes existing hook output and store-offers wiring that previously lived in `App.tsx`.

### Case file promotion (ADR-0011 trigger)

The case file moves from a cockpit-hosted modal to a canonical player route (`/case`). This is the ADR-0011 review trigger ("when route promotion becomes the clearer default"). The cockpit's case-file modal is preserved inside the `Dev tools` route so BUNCH-20 journal/modal context is not bulldozed.

### Player/debug separation

Player routes (Camp, Hunt, Case file, Wanted, Trail) live in one nav group. `Dev tools` lives in a visually separated group (muted, italic, dashed border) so developers can still reach the full cockpit but players see a game shell, not an admin sidebar.

The Dev tools route is a **temporary dev surface**, not a permanent fixture. It preserves the existing cockpit behaviour unchanged so developer tooling is not lost, but it is expected to be deprecated and replaced by a dev overlay in future work — a surface where a dev can play the game and view/manipulate stats and RNG behind the game. No new dev control surfaces are invented in this slice; the existing cockpit surfaces are the entire scope.

### HUD

The HUD is a sticky, compact, single-row status bar that wraps on narrow viewports. It shows player, clock, location, health, cash, heat, and status. It protects the center and lower-middle playfield; long text lives in DOM routes/drawers/modals, not permanent playfield-covering panels.

### Styling and component architecture

Styled-components is the preferred styling approach for UI components. SASS is the fallback for CSS that cannot be done naturally within styled-components. Vertical slice is the preferred architecture for component grouping — components are organized by feature, not by layer. The existing `styles.css` file contains shared shell styles (HUD, nav, route outlet) that are common to the shell chrome and not naturally owned by a single component; these may be migrated to styled-components or SASS in future cleanup work.

### Renderer seam

There is no canvas/Phaser playfield yet. When one is added, it slots into the routed content area under the HUD without changing the state boundary. DOM owns text-heavy HUD/menus/surface and responsive layout; the future canvas/Phaser layer would own the playfield, camera, sprites, and input plumbing.

## Options Considered and Rejected

- Hand-rolled hash router. Rejected: overextended ADR-0019 (which covers the API client, not routing); TanStack Router is a standard tool that handles nested routes, params, loaders, and scroll restoration without manual maintenance.
- Keep `useCurrentGameSession` hand-rolled. Rejected: the repo already uses React Query for `useTravelPanelState`; the hand-rolled busy-mode state machine was the highest-drift-risk code in the session path. TanStack React Query is the repo's chosen tool for all query and mutation API handling.
- Bulldoze the cockpit. Rejected: the cockpit is still useful developer tooling and hosts the BUNCH-20 modal context; it is preserved as a temporary dev surface.
- Keep the case file as a cockpit modal only. Rejected: it is ready for route promotion (ADR-0011 trigger), and a canonical player route is clearer for a v0.1 SPA.
- Introduce a canvas/Phaser playfield in this slice. Rejected: out of scope for the shell slice; the routed content area is the seam for it.
- Add new dev control surfaces. Rejected: the existing cockpit surfaces are the entire scope of this slice; no new dev control surfaces are invented.

## When a Rejected Option Would Have Been Better

A hand-rolled hash router would be better only if there were a strict zero-dependency constraint, which there is not — the repo already has TanStack React Query. A canvas/Phaser playfield would be better once art and scene work are in scope; until then the DOM shell is the right shape.

## Benefits

- Player-facing navigation and a compact HUD replace the single cockpit page.
- TanStack Router handles nested routes, params, loaders, and scroll restoration without manual maintenance.
- React Query eliminates the hand-rolled busy-mode state machine and aligns the central session hook with the pattern `useTravelPanelState` already uses.
- One authoritative session source keeps the frontend an adapter over server state.
- The case file is a canonical player route while the cockpit modal is preserved.
- Debug/cockpit tooling stays reachable but clearly separated from player routes, as a temporary dev surface.
- A future canvas/Phaser playfield has a clear seam under the HUD.

## Accepted Tradeoffs

- TanStack Router is a new runtime dependency, but it pairs with the already-installed TanStack React Query and replaces hand-rolled routing that would have grown into the same features manually.
- The cockpit is duplicated conceptually (Dev tools route) rather than removed; this is intentional to preserve developer access as a temporary dev surface.
- Inventory/wallet, horse/saddle, and deeper travel presentation reuse existing panels rather than getting dedicated routes in this slice.
- The shared `styles.css` shell styles are not yet migrated to styled-components or SASS; this is deferred to future cleanup.

## Risks

- The router singleton could cause state leakage between tests if not reset properly.
- The cockpit route could accumulate responsibilities if future debug surfaces are added there instead of as player routes.
- The shared `styles.css` file coexists with styled-components without a clear boundary yet.

## Consequences for Future Work

- New player surfaces should be added as routes in the `routes/` directory and registered in the `shell/router.tsx` route tree.
- New debug surfaces may live inside `DebugCockpitRoute` while they remain debug-only; the Dev tools route is a temporary dev surface expected to be replaced by a dev overlay.
- New query/mutation hooks should use React Query, following the `useCurrentGameSession` and `useTravelPanelState` patterns.
- A canvas/Phaser playfield slots into the routed content area under the HUD without changing `GameSessionProvider`.
- Inventory/wallet, horse/saddle, and travel/journey presentation are follow-up child issues under BUNCH-56.
- Shared shell styles in `styles.css` may be migrated to styled-components or SASS in future cleanup.
- The manual API client (ADR-0019) stays in place until the API surface grows enough to justify a generated client; the review trigger is "when hand-maintained transport code becomes a duplication burden."

## Implementation Status or Plan

Live. The v0.1 shell, HUD, routes, provider, TanStack Router integration, React Query refactor, and Dev tools separation are implemented in `src/WildBunch.Web`. Validation: `npm run typecheck` clean, `npm test` 32 passed, `npm run build` succeeds.

## Related Stable Source Surfaces

- `src/WildBunch.Web/src/App.tsx`
- `src/WildBunch.Web/src/state/GameSessionProvider.tsx`
- `src/WildBunch.Web/src/state/useGameSession.ts`
- `src/WildBunch.Web/src/shell/AppShell.tsx`
- `src/WildBunch.Web/src/shell/Hud.tsx`
- `src/WildBunch.Web/src/shell/router.tsx`
- `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- `src/WildBunch.Web/src/routes/`
- `src/WildBunch.Web/src/components/AvailableActionsPanel.tsx`
- `src/WildBunch.Web/src/App.test.tsx`
- `src/WildBunch.Web/src/shell/AppShell.test.tsx`

## Proof of Implementation or Explicit Non-Implementation

The shell renders via `App.tsx` → `GameSessionProvider` → `RouterProvider` → `AppShell` (root route). The HUD, nav, and route outlet are in `src/WildBunch.Web/src/shell/`. Route components are in `src/WildBunch.Web/src/routes/`. The relocated cockpit is `DebugCockpitRoute.tsx`. The refactored session hook is `useCurrentGameSession.ts` using `useQuery`/`useMutation`/`useQueryClient`. Tests: `App.test.tsx` (6 retargeted cockpit behaviours, wrapped in `QueryClientProvider`) and `AppShell.test.tsx` (5 shell/routing tests, wrapped in `QueryClientProvider` + `RouterProvider`).

## Review Triggers

- When the route count grows enough that file-based routes or route-level code splitting is justified.
- When a canvas/Phaser playfield is added and the routed content area needs to host it.
- When inventory/wallet, horse/saddle, or deeper travel presentation promote to dedicated routes.
- When the Dev tools route should be replaced by a dev overlay with stat/RNG manipulation.
- When the shared `styles.css` should be migrated to styled-components or SASS.
- When the manual API client (ADR-0019) should be replaced by a generated client.
