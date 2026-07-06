# Frontend Standards

Use this reference when implementing or reviewing frontend work in the Wild Bunch web app (`src/WildBunch.Web/`). These are the binding standards for the frontend stack — both implementers and reviewers must follow them.

## Styling Stack

- Use `styled-components` for component-owned layout and appearance.
- Use SASS (`src/styles/`) for global concerns: design tokens (`_variables.scss`), reset (`_reset.scss`), and base element defaults (`_base.scss`).
- Do NOT use plain CSS classes in `className`. All component styling must be handled via styled components.
- Reference design tokens via `var(--token-name)` to stay on-palette.
- Re-use shared primitives from `src/components/ui/sharedStyled.tsx` for genuine cross-surface patterns (Panel, StatusCard, Button, Grid, ItemCard, etc.).
- Feature-specific styled components should stay local in the component file that uses them.
- Durable guidance: `docs/frontend-styling.md`.
- Enforced by `src/tests/stylingEnforcement.test.ts`.

## Play-Surface UI

Player-facing surfaces must be in-world, player-usable surfaces, not cockpit dashboards or product chrome. If a label, counter, callout, modal title, or overlay header does not help the player read or use the surface, cut it.

- Durable play surfaces belong in the HUD/shell or another player-facing route, not in `DebugCockpitRoute`. The debug cockpit can remain utilitarian scaffolding.
- Apply `.agents/unslop/play-surface-ui.md` when designing, implementing, or reviewing player-facing game surfaces, HUD placement, overlays, modal surfaces, or related reference UI.

## Source Truth

React renders backend/player-known state rather than inventing canonical game facts or hidden internal interpretations. The frontend is a presentation adapter over authoritative backend game state — it reads what the backend provides and renders it. It does not own complex domain state.

## Dev Overlay

Required reading: `.agents/docs/dev-overlay-doctrine.md` — binding doctrine for dev overlay state/action boundary, panel ownership, related panel visibility, layout, hidden truth, backend authority, and closeout proof.

Also apply `.agents/unslop/dev-overlay.md` — the dev overlay unslop drift-prevention profile.

- Dev panels are contextual to the current gameplay surface. Each panel deeply owns one domain node/surface and only lightly manipulates related nodes.
- Dev mutations go through backend commands — the frontend never fakes player progress or injects final results.
- The dev overlay is a developer tool, not a player-facing surface. It lives under `src/dev/` and is toggled from the shell chrome bar, not from player routes.

## Routing

The app uses TanStack Router with a route tree that reflects game state via URL paths.

- **Route tree:** Defined in `src/shell/router.tsx`. Routes are lazy-loaded via `React.lazy()` to enable Vite bundle splitting.
- **URL reflects game state:** `usePhaseRouteSync` reconciles the URL with the backend-derived game phase. `useDevSurfaceSync` maps phase + URL to the dev surface context.
- **Town place routes are flat siblings under rootRoute, NOT children of townRoute.** `TownHubSurface` renders the hub directly (no `<Outlet />`), so child routes would not render. Place routes (`/town/store`, `/town/sheriff`, `/town/saloon`, `/town/trailhead`) are siblings of `/town` under the root route.
- **Search params:** The `/town` route uses `validateSearch` to parse the optional `arrived` query param (`?arrived=1`). The validateSearch function must return `{}` when the param is absent, not `{ arrived: undefined }` — the latter makes TanStack Router type the param as required, breaking `navigate({ to: "/town" })` calls that don't pass search.
- **Suspense:** Lazy-loaded route components are wrapped in `<Suspense>` with `<RouteLoading>` as the fallback (defined in `src/shell/RouteLoading.tsx`).
- **Testing:** Tests that render through `RouterProvider` must use the `createAppRouter()` factory, not the shared `router` singleton. The shared router retains internal state between tests (TanStack Router doesn't react to `window.history.replaceState`), causing test ordering flakes. See `.agents/docs/validation-policy.md` Test Quality Standards for details.
- **Enforcement:** Routing conventions (lazy-loaded components, flat town place routes, validateSearch returns `{}`, `createAppRouter` factory exists) are enforced by `src/tests/routingConventions.test.ts`. If you add or change a route, this test will catch violations.
