# Wild Bunch Web

Use [`docs/unslop/play-surface-ui.md`](docs/unslop/play-surface-ui.md) before designing, implementing, or reviewing player-facing game surfaces, HUD placement, overlays, modal surfaces, or related reference UI.

Keep player-facing game surfaces as in-world, player-usable surfaces, not cockpit dashboards or product chrome. If a label, counter, callout, modal title, or overlay header does not help the player read or use the surface, cut it.

Durable play surfaces belong in the HUD/shell or another player-facing route, not in `DebugCockpitRoute`. The debug cockpit can remain utilitarian scaffolding.

React should render backend/player-known state rather than inventing canonical game facts or hidden internal interpretations.
