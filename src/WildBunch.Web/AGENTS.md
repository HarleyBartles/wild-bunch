# Wild Bunch Web

Use [`.agents/unslop/play-surface-ui.md`](.agents/unslop/play-surface-ui.md) before designing, implementing, or reviewing player-facing game surfaces, HUD placement, overlays, modal surfaces, or related reference UI.

Keep player-facing game surfaces as in-world, player-usable surfaces, not cockpit dashboards or product chrome. If a label, counter, callout, modal title, or overlay header does not help the player read or use the surface, cut it.

Durable play surfaces belong in the HUD/shell or another player-facing route, not in `DebugCockpitRoute`. The debug cockpit can remain utilitarian scaffolding.

React should render backend/player-known state rather than inventing canonical game facts or hidden internal interpretations.

## Styling Stack

- Use `styled-components` for component-owned layout and appearance.
- Use SASS (`src/styles/`) for global concerns: design tokens (`_variables.scss`), reset (`_reset.scss`), and base element defaults (`_base.scss`).
- Do NOT use plain CSS classes in `className`. All component styling must be handled via styled components.
- Reference design tokens via `var(--token-name)` to stay on-palette.
- Re-use shared primitives from `src/components/ui/sharedStyled.tsx` for genuine cross-surface patterns (Panel, StatusCard, Button, Grid, ItemCard, etc.).
- Feature-specific styled components should stay local in the component file that uses them.
- Durable guidance: [`docs/frontend-styling.md`](../../docs/frontend-styling.md).
- Enforced by `src/tests/stylingEnforcement.test.ts`.

## Dev overlay work

Required reading: [`.agents/dev-overlay/DOCTRINE.md`](../../.agents/dev-overlay/DOCTRINE.md) — binding doctrine for dev overlay state/action boundary, panel ownership, related panel visibility, layout, hidden truth, backend authority, and closeout proof.

Also apply [`.agents/unslop/dev-overlay.md`](../../.agents/unslop/dev-overlay.md) — the dev overlay unslop drift-prevention profile — together with this doctrine when designing, implementing, or reviewing dev overlay work.

Dev panels are contextual to the current gameplay surface. Each panel deeply owns one domain node/surface and only lightly manipulates related nodes. Dev mutations go through backend commands — the frontend never fakes player progress or injects final results.

The dev overlay is a developer tool, not a player-facing surface. It lives under `src/dev/` and is toggled from the shell chrome bar, not from player routes.
