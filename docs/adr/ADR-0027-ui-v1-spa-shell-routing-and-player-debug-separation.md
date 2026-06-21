# ADR-0027 UI v1 SPA Shell, Hash Routing, and Player/Debug Separation

## Status

live

## Dated Status History

- 2026-06-21 - live: the web client gained a v1 app shell with a persistent HUD,
  player-facing hash routes, and a separated debug cockpit. Promotes the case
  file from a cockpit-hosted modal to a first canonical player route.

## Decision Type

ui, architecture

## Related ADRs

- `depends on`: ADR-0016, ADR-0019
- `informs`: ADR-0011, ADR-0022
- `related to`: ADR-0007

## Context

Before this decision the web client (`src/WildBunch.Web`) was a single cockpit
page (`App.tsx`) with every panel on one screen and major play surfaces opening
as cockpit-hosted modals (ADR-0011). The UI v1 campaign asked for a first
production-shaped SPA shape: a browser-game shell, a compact HUD, player-facing
navigation, and at least one production-shaped player surface, while keeping the
developer cockpit available and not bulldozing existing journal/case work.

ADR-0011 explicitly anticipated this step: surfaces start as cockpit-hosted
modals and are promoted to canonical routes "only when the product is ready",
with a review trigger "when route promotion becomes the clearer default". This
ADR records that promotion for the case file and the shell it lives in.

## Decision Drivers

- The frontend adapts authoritative game/application state and must not become a
  second gameplay model.
- A v1 shell should ship without adopting a heavy routing/state framework before
  the route model is proven (ADR-0019 posture: stay manual until justified).
- Player-facing surfaces and developer/debug tooling must be clearly separated.
- The persistent HUD must stay compact and protect the main content area.
- Existing cockpit behaviour and tests must keep working.

## Decision Summary

Introduce a v1 SPA shell that renders a persistent HUD, a player-facing
navigation frame, and a routed content outlet driven by a tiny hash router. A
single `GameSessionProvider` is the shared, server-authoritative state source for
every route. The dense cockpit is preserved verbatim as a separated `Dev tools`
route, and the case file is promoted from a cockpit modal into a canonical
player route.

## Detailed Decision Breakdown

- Routing: `src/shell/useHashRoute.ts` is a dependency-free hash router. Routes
  are declared as data in `src/shell/routes.ts` and resolved by `AppShell`.
- Shell chrome: `src/shell/AppShell.tsx` renders `Hud`, the navigation, and the
  active route. `src/shell/Hud.tsx` is a sticky, compact status bar.
- Shared state: `src/state/GameSessionContext.tsx` wraps the existing
  `useCurrentGameSession` hook (plus town store offers / buy) so the HUD and all
  routes read one authoritative session instead of holding per-route copies.
- Player routes: `Camp` (`/`), `Hunt` (`/hunt`), `Case file` (`/case`),
  `Wanted` (`/wanted`), `Trail` (`/trail`). These reuse existing surface
  components (`StartGamePanel`, `FieldReportPanel`, `CaseFileSurface`,
  `WantedPosterSurface`, `TravelRoutesPanel`, `TravelPanel`).
- Debug route: `Dev tools` (`/debug`) renders the former cockpit
  (`src/routes/DebugCockpitRoute.tsx`) unchanged in behaviour, visually marked as
  non-player tooling. The case-file modal stays available here, preserving the
  BUNCH-20 journal/modal context.
- The shared `AvailableActionsPanel` is extracted from the cockpit and reused by
  the Hunt route so action wiring lives in one place.

## Options Considered and Rejected

- Adopt `react-router` and a global store now: rejected as premature weight
  before the route model is proven (ADR-0019 posture).
- Promote every modal surface into a route at once: rejected; only the case file
  is promoted, the rest stay reachable and can be promoted later.
- Replace the cockpit outright: rejected; the cockpit is retained as separated
  developer tooling.

## When a Rejected Option Would Have Been Better

A full router/state library would win once routes need nested layouts, data
loaders, code-splitting, or deep-linkable sub-state beyond a single hash segment.

## Benefits

- A real v1 shell with player navigation and a compact, playfield-protecting HUD.
- One authoritative session source shared across HUD and routes.
- Clear player/debug separation without losing developer tooling.
- No new runtime dependency for routing.

## Accepted Tradeoffs

- The hash router is intentionally minimal (no nested routes, no loaders).
- Two header styles coexist: the player `RouteHeader` and the cockpit hero.

## Risks

- The hash router may need replacement if routing needs grow; routes are kept as
  data to ease that migration.
- Promoting only the case file can read as inconsistent until more surfaces are
  promoted; tracked as follow-up UI issues.

## Consequences for Future Work

Future surface promotions follow the same pattern: add a route entry, mount an
existing surface component, and read state from `GameSessionProvider`. A renderer
(canvas/Phaser) playfield, if added later, slots into the routed content area
under the HUD without changing the state boundary.

## Implementation Status or Plan

Live. Implemented in `src/WildBunch.Web` and covered by frontend tests.

## Related Stable Source Surfaces

- `src/WildBunch.Web/src/App.tsx`
- `src/WildBunch.Web/src/shell/`
- `src/WildBunch.Web/src/state/GameSessionContext.tsx`
- `src/WildBunch.Web/src/routes/`
- `src/WildBunch.Web/src/shell/AppShell.test.tsx`
- `src/WildBunch.Web/src/App.test.tsx`

## Proof of Implementation or Explicit Non-Implementation

`AppShell` mounts the HUD, player navigation, and routed surfaces;
`AppShell.test.tsx` proves the HUD, navigation, the promoted case-file route, and
the separated debug cockpit. `App.test.tsx` proves the relocated cockpit retains
its behaviour.

## Review Triggers

- When a second major surface is promoted from modal/cockpit to a player route.
- When routing needs nested layouts, loaders, or code-splitting.
- When a canvas/Phaser playfield is introduced into the shell.
