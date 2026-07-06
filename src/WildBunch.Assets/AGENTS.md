# WildBunch.Assets AGENTS.md

This project is the canonical repository home for generated asset work.

## Home layout

- `source/` holds full-size source-custody assets for each family
- `staging/` holds reviewable scratch, cut, and normalization output
- `sprites/` holds the final promoted sprite assets
- `src/WildBunch.Web/public/assets/` is shipping output only, not the working area for assets

## Required reading

Before editing or promoting assets in this project, read:

- `docs/art/town-buildings/style-bible.md`
- `docs/art/town-buildings/asset-spec.md`
- `.agents/docs/town-buildings-doctrine.md`
- `.agents/docs/asset-pipeline/selection-cut-normalization.md`
- `source/town-buildings/README.md`
- `source/town-buildings/AGENTS.md`

## Rules

- Keep source custody in `source/`.
- Keep intermediate work in `staging/`.
- Promote into `sprites/` only when the asset is ready to ship.
- Do not add work-in-progress assets to the web public tree as the canonical home.
- Every asset family root under `source/` must include both a human-facing `README.md` and an `AGENTS.md`; the README should explain what is in the family, and the AGENTS file should point agents at the controlling style bible, asset spec, and any family-specific doctrine before they edit or generate files there.
