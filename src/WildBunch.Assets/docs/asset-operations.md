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

- `src/WildBunch.Assets/docs/bibles/buildings/buildings-bible-master.md`
- `src/WildBunch.Assets/docs/bibles/ground/ground-bible-master.md`
- the matching family bible under `src/WildBunch.Assets/docs/bibles/buildings/` or `src/WildBunch.Assets/docs/bibles/ground/` for the asset you are working on
- `src/WildBunch.Assets/docs/asset-spec.md`
- `.agents/doctrine/art/town-hub-buildings.md` (for building work)
- `.agents/doctrine/art/town-hub-ground.md` (for ground, road, or prop work)
- `.agents/playbooks/asset-selection-cut-normalization.md`

## Bible custody

- Use `*-bible-master.md` for the umbrella document owning a family set's routing table and shared contract; use `*-bible.md` for family-specific or rule-specific guidance beneath it.
- Keep one master per family set and keep its family bibles in the matching subfolder.
- Before creating or revising a bible, decide whether its rule belongs in the family master, a family bible or a project-level document.
- Extend an existing family and routing table instead of creating an ambiguous naming branch; keep routing tables prominent and current.
- Apply the stale-guidance correction rule below when a bible is misleading, incomplete or wrong.

## Pipeline custody

- Keep asset-pipeline code specific to this project in `src/WildBunch.Assets/scripts/`.
- `src/WildBunch.Assets/scripts/image_asset_pipeline.py` is the asset staging and promotion command; invoke it directly with Python 3.11+ and Pillow.

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
