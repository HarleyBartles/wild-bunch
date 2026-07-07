# WildBunch.Assets AGENTS.md

This project is the canonical repository home for generated town-hub asset work.

## Home layout

- `source/` holds full-size source-custody assets for each track
- `staging/` holds reviewable scratch, cut, and normalization output
- `sprites/` holds the final promoted sprite assets
- `src/WildBunch.Web/public/assets/` is shipping output only, not the working area for assets

The current track split is `town-hub-buildings`, `town-hub-roads`, and
`town-hub-ground`.

## Required reading

Before editing or promoting assets in this project, read:

- `docs/art/town-buildings/style-bible.md`
- `docs/art/town-buildings/asset-spec.md`
- `.agents/art/town-buildings/DOCTRINE.md`
- `docs/art/town-hub-ground/style-bible.md`
- the matching family bible under `docs/art/town-hub-ground/` for dirt,
  road, spur, path, or props work
- `.agents/art/town-hub-ground/DOCTRINE.md`
- `.agents/docs/asset-pipeline/selection-cut-normalization.md`

## Rules

- Keep source custody in `source/`.
- Keep intermediate work in `staging/`.
- Promote into `sprites/` only when the asset is ready to ship.
- Do not add work-in-progress assets to the web public tree as the canonical home.
- Every asset family root under `source/` must include both a human-facing `README.md` and an `AGENTS.md`; the README should explain what is in the family, and the AGENTS file should point agents at the controlling style bible, asset spec, and any family-specific doctrine before they edit or generate files there.
- For the town-hub split, keep `town-hub-buildings`, `town-hub-roads`, and `town-hub-ground` separated and follow the track-specific contract in the style bible, asset spec, and doctrine before editing files in those roots.
