# Town-hub asset production runbook

Use this runbook when producing or revising a town-hub building, road, or
ground asset.

## Composition

1. Read `src/WildBunch.Assets/docs/asset-operations.md`, the asset spec, the
   applicable master and family bible under `src/WildBunch.Assets/docs/bibles/`,
   and the matching doctrine under `../doctrine/art/`.
2. Use `/imagegen` for generated raster candidates when generation is needed.
3. Use `/town-hub-asset-judgment` to accept, retry, or reject each candidate.
4. Use the deterministic selection/cut/normalization runbook for processing.

## Repository route

- Masters and family sources: the matching family under
  `src/WildBunch.Assets/source/`.
- Reviewable intermediates: the matching family under
  `src/WildBunch.Assets/staging/`.
- Promoted outputs: the matching family under
  `src/WildBunch.Assets/production/sprites/` or
  `src/WildBunch.Assets/production/tiles/`, as defined by the asset spec.
- Deterministic helper and commands:
  [asset selection, cut, and normalization](asset-selection-cut-normalization.md).

Buildings require the canonical five-view set and game-scale review. Road and
ground tiles use seam-safe copy promotion without trimming or rescaling. Keep
candidate sheets and discarded renders out of promoted output.
