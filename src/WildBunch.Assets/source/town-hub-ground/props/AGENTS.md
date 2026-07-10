# Town Hub Props Asset Routing

This subtree is the source-custody root for town-hub prop sprites.

Before editing, generating, or promoting any town-hub prop assets in this
subtree, read the controlling style bible set first:

- `src/WildBunch.Assets/docs/bibles/AGENTS.md`
- `src/WildBunch.Assets/docs/bibles/ground/ground-bible-master.md`
- `src/WildBunch.Assets/docs/bibles/ground/props-bible.md`
- `.agents/art/town-hub-ground/DOCTRINE.md`
- `src/WildBunch.Assets/AGENTS.md`
- `.agents/docs/asset-pipeline/selection-cut-normalization.md`

If a style bible looks stale, misleading, incomplete, or wrong while you are
working, fix the bible as part of the same task instead of deferring the
correction.
Do not add new naming branches here; keep prop naming inside the existing
ground bible family and routing tables.

Keep source custody here (full-size 1024x1024 for future scaling), intermediate work in
`src/WildBunch.Assets/staging/town-hub-ground/props/`, and promoted props
in `src/WildBunch.Assets/production/sprites/town-hub-ground/props/` (normalized to 80x50 canvas).
