# WildBunch.Assets AGENTS.md

This project is the canonical repository home for generated asset work.

## Home layout

- `town-buildings/_pipeline/` holds reviewable staging outputs and other working files
- `town-buildings/sprites/` holds the final promoted sprite assets
- `src/WildBunch.Web/public/assets/` is shipping output only, not the working area for assets

## Required reading

Before editing or promoting assets in this project, read:

- `docs/art/town-buildings/style-bible.md`
- `docs/art/town-buildings/asset-spec.md`
- `.agents/art/town-buildings/DOCTRINE.md`
- `.agents/art/asset-pipeline/selection-cut-normalization.md`

## Rules

- Keep intermediate work in `_pipeline/`.
- Promote into `sprites/` only when the asset is ready to ship.
- Do not add work-in-progress assets to the web public tree as the canonical home.
