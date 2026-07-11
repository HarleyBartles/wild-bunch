# WildBunch.Assets Asset Operations

This project is the canonical repository home for generated town-hub asset work.

## Home layout

- `source/` holds full-size source-custody assets for each track
- `staging/` holds reviewable scratch, cut, and normalization output
- `production/sprites/` holds the final promoted sprite assets
- `production/tiles/` holds the final promoted tile assets
- `scripts/` holds asset-local helper scripts for this project
- `src/WildBunch.Web/public/assets/` is shipping output only, not the working area for assets

The current track split is `town-hub-buildings`, `town-hub-roads`, and
`town-hub-ground`.

## Required reading

Before editing or promoting assets in this project, read:

- `src/WildBunch.Assets/docs/bibles/AGENTS.md`
- `src/WildBunch.Assets/docs/bibles/buildings/buildings-bible-master.md`
- `src/WildBunch.Assets/docs/asset-spec.md`
- `.agents/art/town-buildings/DOCTRINE.md`
- `src/WildBunch.Assets/docs/bibles/ground/ground-bible-master.md`
- the matching family bible under `src/WildBunch.Assets/docs/bibles/ground/` for dirt,
  road, spur, path, or props work
- `.agents/art/town-hub-ground/DOCTRINE.md`
- `.agents/docs/asset-pipeline/selection-cut-normalization.md`
- `src/WildBunch.Assets/scripts/AGENTS.md`

## Rules

- Keep source custody in `source/`.
- Keep intermediate work in `staging/`.
- Promote into `production/sprites/` or `production/tiles/` only when the asset is ready to ship.
- Do not add work-in-progress assets to the web public tree as the canonical home.
- Do not introduce new naming branches or parallel taxonomy paths when an
  existing family, master, or routing table can absorb the rule.
- Every asset family root under `source/` must include both a human-facing `README.md` and an `AGENTS.md`; the README should explain what is in the family, and the AGENTS file should point agents at the controlling style bible, asset spec, and any family-specific doctrine before they edit or generate files there.
- For the town-hub split, keep `town-hub-buildings`, `town-hub-roads`, and `town-hub-ground` separated and follow the track-specific contract in the style bible, asset spec, and doctrine before editing files in those roots.
- If a style bible, asset spec, or family doctrine looks stale, misleading,
  incomplete, or wrong while you are working, fix it as part of the same task
  instead of deferring the correction.
