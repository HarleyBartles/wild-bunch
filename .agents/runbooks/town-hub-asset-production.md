# Town-hub asset production runbook

## When

Producing or revising a town-hub building, road, ground, or prop asset.

## Required skills

- `/imagegen` when raster generation is needed.
- `/town-hub-asset-judgment`

## Composition

Read the asset operations/spec and applicable master/family bible, generate only
when needed, judge each candidate, then send accepted candidates through the
deterministic processing runbook.

## Doctrine and contracts

The matching art doctrine under `../doctrine/art/` and
`src/WildBunch.Assets/docs/asset-spec.md` govern family, camera, seam, canvas,
and promotion constraints.

## Local commands and paths

- Sources: matching family under `src/WildBunch.Assets/source/`
- Reviewable intermediates: matching family under `src/WildBunch.Assets/staging/`
- Outputs: matching family under `src/WildBunch.Assets/production/sprites/` or
  `production/tiles/` as declared by the asset spec
- Processing: [asset cut and normalization](asset-selection-cut-normalization.md)

## Evidence contract

Candidate judgment, required views, game-scale read, seam/footprint checks, and
promoted path agree with the applicable bible and asset spec.

## Prohibited combinations

- Do not promote rejected candidate sheets or discarded renders.
- Do not trim or rescale road and ground tile canvases.
