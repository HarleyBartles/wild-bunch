# ADR-0030 Dev Overlay and Dev Endpoint Namespace

## Status

`live`

## Dated Status History

- 2026-06-25 - live: Dev overlay foundation implemented. Toggleable DevOverlay drawer in AppShell replaces DebugCockpitRoute. Dev endpoints under /api/dev/ with centralized DevRoleGuard. SessionAuditDevPanel as first contextual panel. Panel registry pattern established for future travel/saloon dev panels. Shell chrome (AppShell, Hud, GlobalOverlays) migrated to styled-components; global tokens and reset moved to SASS partials.
- 2026-06-25 - live: TravelDevPanel added as second panel via the registry pattern (BUNCH-89, ADR-0031). Dev travel endpoints under /api/dev/sessions/{id}/travel-context, /travel/force-override, /travel/clear-override. The panel registry pattern from point 5 is now exercised by a real second panel.
- 2026-06-26 - live: SaloonDevPanel added as third panel (BUNCH-90, ADR-0032). Dev saloon endpoints under /api/dev/sessions/{id}/saloon-context, /saloon/force-override, /saloon/clear-override. This is the first dev panel to deliberately expose hidden culprit truth (TrueCulpritId, suspect eligibility) through the §7 player-vs-dev boundary. HiddenTruthDevDto is a separate dev-only DTO type, not a player DTO.
- 2026-06-27 - live: BUNCH-90 doctrine pass. Binding dev-overlay doctrine installed at `.agents/docs/dev-overlay-doctrine.md` as agent-facing law, linked from root AGENTS.md, `.agents/INDEX.md`, and `src/WildBunch.Web/AGENTS.md`. Doctrine covers state/action boundary, panel ownership model, related panel visibility/defaults, layout doctrine, hidden truth/API boundary, suspect/warrant/casefile composition, backend authority, context mismatch detection, and closeout proof. Mesh policy installed in root AGENTS.md separating agents mesh (law), index mesh (navigation), and README (human-facing). Index mesh installed across the full folder tree (all-or-nothing with documented exclusions). Default panel selection now prefers surface owner over Session Audit.

## Decision Type

architecture, ui, process

## Related ADRs

- `depends on`: ADR-0028 (projection posture — FullAuditProjector is the dev audit source)
- `related to`: ADR-0007 (hidden culprit boundaries — the player-vs-dev boundary, not a blanket prohibition on dev truth)

## Context

The previous dev cockpit (`DebugCockpitRoute` at `/debug`) duplicated player-facing functionality (start game, actions, travel, case file) already available in the flow surfaces. It was a separate route that competed with the play surface rather than a contextual overlay that could augment it.

Future dev controls (travel encounter forcing, saloon/POI forcing) need a shared extension point that is clearly dev-only, toggleable, and separated from player-facing APIs. Some future dev panels will need to inspect hidden truth (culprit identity, internal encounter state, seed internals) for debugging and playtesting. The foundation must not establish a convention that prevents that.

The dev overlay must support playtesting while playing. The normal loop is: keep playing, expose a dev HUD, inspect current state, tweak current-context setup, collapse it, and continue. The overlay should feel attached to the game chrome and current play surface, not like a separate mode that blacks out the game.

## Decision

1. **DevOverlay drawer.** A single toggleable `DevOverlay` component mounted in `AppShell` renders as a slide-down drawer that partially covers the play surface (45vh by default) when open and nothing when closed. The play surface remains visible below the drawer, preserving game context. Toggle state is shell-local. This replaces the `/debug` route.

2. **Three-state interaction model.** The dev overlay supports three states:
   - **Collapsed:** a small dev affordance (the "Dev" toggle button) in the overlay bar chrome.
   - **Open:** a slide-down drawer at 45vh that partially covers the play surface. The game remains visible and interactive below. This is the default open state.
   - **Expanded:** an optional maximized mode (85vh) for panels that genuinely need more room. Triggered by an "Expand" button in the drawer header. Escape shrinks from expanded to open; Escape again closes the drawer.

   "Can dominate the screen" means the shell is capable of growing when needed via the expanded state. The default open state does not consume the viewport or hide the game. Future panels that only need a HUD-sized panel sit in the default open drawer; panels that need more room use the expanded state.

3. **Dev endpoint namespace.** Dev-only endpoints live under `/api/dev/`, mapped by `DevEndpoints`, separate from player-facing `/api/games/`. Dev endpoints may return dev-only projections (FullAuditProjection) and dev-only DTOs.

4. **DevRoleGuard seam.** A centralized `DevRoleGuard` with `EnsureDevAccess()` gates every dev endpoint. Currently checks `IHostEnvironment.IsDevelopment()`. Throws `DevAccessDeniedException` which endpoints catch and return as 403 (via `Results.StatusCode(403)` to avoid the `IAuthenticationService` dependency that `Results.Forbid()` requires). Future auth replaces the method body without changing call sites.

5. **Panel registry.** A `DevPanelRegistry` defines available dev panels as `{ id, label, render }` entries. The DevOverlay renders a sidebar from the registry. Future panels (TravelDevPanel, SaloonDevPanel) add entries without modifying the shell.

6. **Cockpit retirement.** `DebugCockpitRoute` and the `/debug` route are removed. There is one dev surface: the DevOverlay.

7. **Player-vs-dev truth boundary.** The boundary is player-vs-dev, not truth-vs-no-truth. Normal player APIs and read models must not newly leak hidden truth (per ADR-0007 and ADR-0028 §10). Dev-only endpoints MAY expose hidden truth and internal diagnostics when deliberately scoped, guarded, and separated from player DTOs. BUNCH-88 does not itself expose hidden truth, but the `/api/dev/` namespace and `DevRoleGuard` seam establish the route through which later issues can deliberately expose dev-only truth. Dev DTOs are separate types from player DTOs. The existing `GameApiHiddenTruthTests` continue to guard the player boundary.

8. **Styling convention.** The shell chrome (AppShell, Hud, GlobalOverlays, DevOverlay, SessionAuditDevPanel) uses styled-components for component-level styling. Global concerns (design tokens via CSS custom properties, box-model reset, base element defaults) live in SASS partials under `src/styles/` (`_variables.scss`, `_reset.scss`) imported through `src/styles/index.scss`. Feature styles that have not yet been migrated remain in `styles.css` and are imported by the SASS entry so migration can proceed incrementally.

## Options Considered and Rejected

- **Keep DebugCockpitRoute and add overlay beside it.** Rejected: two dev surfaces creates confusion and duplication.
- **Route-based overlay at /dev.** Rejected: URL-based toggle adds navigation complexity and doesn't coexist cleanly with game phase routing.
- **Full-screen modal takeover as default.** Rejected: this interrupts the play loop by hiding the game. Dev tools should be a contextual playtest HUD, not a separate mode. A full-screen takeover is available via the expanded state for panels that genuinely need it, but it is not the default.
- **Generic Modal/Panel abstraction.** Rejected per play-surface-ui.md: avoid generic React infrastructure before demand. The DevOverlay is a specific dev surface, not a reusable abstraction.
- **Blanket prohibition on dev hidden-truth exposure.** Rejected: this would prevent future dev-only truth inspection (culprit identity, encounter internals, seed diagnostics) that playtesting and debugging require. The correct boundary is player-vs-dev with the guard seam, not truth-vs-no-truth.

## Consequences

- Future dev panels register through the registry and fetch from `/api/dev/`.
- Dev endpoint access is centralized through one guard seam with an explicit 403 denial path.
- The play surface is clean when the overlay is closed, and partially visible when the overlay is open.
- The FullAuditProjector is now exposed through a dev endpoint, but only in development and only through dev DTOs.
- Later issues may add dev endpoints that expose hidden truth (culprit identity, internal state) through the same guarded `/api/dev/` namespace without violating ADR-0007, because ADR-0007's boundary is player-facing, not dev-facing.
- The shell chrome styling convention is styled-components for components and SASS for global concerns. Future shell and feature components should follow this pattern.
