# ADR-0030 Dev Overlay and Dev Endpoint Namespace

## Status

`live`

## Dated Status History

- 2026-06-25 - live: Dev overlay foundation implemented. Toggleable DevOverlay shell in AppShell replaces DebugCockpitRoute. Dev endpoints under /api/dev/ with centralized DevRoleGuard. SessionAuditDevPanel as first contextual panel. Panel registry pattern established for future travel/saloon dev panels.

## Decision Type

architecture, ui, process

## Related ADRs

- `depends on`: ADR-0028 (projection posture — FullAuditProjector is the dev audit source)
- `related to`: ADR-0007 (hidden culprit boundaries — the player-vs-dev boundary, not a blanket prohibition on dev truth)

## Context

The previous dev cockpit (`DebugCockpitRoute` at `/debug`) duplicated player-facing functionality (start game, actions, travel, case file) already available in the flow surfaces. It was a separate route that competed with the play surface rather than a contextual overlay that could augment it.

Future dev controls (travel encounter forcing, saloon/POI forcing) need a shared extension point that is clearly dev-only, toggleable, and separated from player-facing APIs. Some future dev panels will need to inspect hidden truth (culprit identity, internal encounter state, seed internals) for debugging and playtesting. The foundation must not establish a convention that prevents that.

## Decision

1. **DevOverlay shell.** A single toggleable `DevOverlay` component mounted in `AppShell` renders as a fixed full-surface panel when open and nothing when closed. Toggle state is shell-local. This replaces the `/debug` route.

2. **Dev endpoint namespace.** Dev-only endpoints live under `/api/dev/`, mapped by `DevEndpoints`, separate from player-facing `/api/games/`. Dev endpoints may return dev-only projections (FullAuditProjection) and dev-only DTOs.

3. **DevRoleGuard seam.** A centralized `DevRoleGuard` with `EnsureDevAccess()` gates every dev endpoint. Currently checks `IHostEnvironment.IsDevelopment()`. Throws `DevAccessDeniedException` which endpoints catch and return as 403. Future auth replaces the method body without changing call sites.

4. **Panel registry.** A `DevPanelRegistry` defines available dev panels as `{ id, label, render }` entries. The DevOverlay renders a sidebar from the registry. Future panels (TravelDevPanel, SaloonDevPanel) add entries without modifying the shell.

5. **Cockpit retirement.** `DebugCockpitRoute` and the `/debug` route are removed. There is one dev surface: the DevOverlay.

6. **Player-vs-dev truth boundary.** The boundary is player-vs-dev, not truth-vs-no-truth. Normal player APIs and read models must not newly leak hidden truth (per ADR-0007 and ADR-0028 §10). Dev-only endpoints MAY expose hidden truth and internal diagnostics when deliberately scoped, guarded, and separated from player DTOs. BUNCH-88 does not itself expose hidden truth, but the `/api/dev/` namespace and `DevRoleGuard` seam establish the route through which later issues can deliberately expose dev-only truth. Dev DTOs are separate types from player DTOs. The existing `GameApiHiddenTruthTests` continue to guard the player boundary.

## Options Considered and Rejected

- **Keep DebugCockpitRoute and add overlay beside it.** Rejected: two dev surfaces creates confusion and duplication.
- **Route-based overlay at /dev.** Rejected: URL-based toggle adds navigation complexity and doesn't coexist cleanly with game phase routing.
- **Generic Modal/Panel abstraction.** Rejected per play-surface-ui.md: avoid generic React infrastructure before demand. The DevOverlay is a specific dev surface, not a reusable abstraction.
- **Blanket prohibition on dev hidden-truth exposure.** Rejected: this would prevent future dev-only truth inspection (culprit identity, encounter internals, seed diagnostics) that playtesting and debugging require. The correct boundary is player-vs-dev with the guard seam, not truth-vs-no-truth.

## Consequences

- Future dev panels register through the registry and fetch from `/api/dev/`.
- Dev endpoint access is centralized through one guard seam with an explicit 403 denial path.
- The play surface is clean when the overlay is closed.
- The FullAuditProjector is now exposed through a dev endpoint, but only in development and only through dev DTOs.
- Later issues may add dev endpoints that expose hidden truth (culprit identity, internal state) through the same guarded `/api/dev/` namespace without violating ADR-0007, because ADR-0007's boundary is player-facing, not dev-facing.
