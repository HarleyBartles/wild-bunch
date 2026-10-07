# Town-hub asset production playbook

## When

Producing or revising a town-hub building, road, ground, or prop asset.

## Required capabilities

- Generate raster assets when the task requires new imagery.

## Optional capabilities

- None.

## Required repository-owned skills

- town-hub-asset-judgment

## Optional repository-owned skills

- None.

## Unslop before work

Before authoring asset briefs or reviewing browser integration, read [writing](../unslop/writing.md) and the relevant [play-surface UI guards](../unslop/play-surface-ui.md) in full. Use [selection and observations](../unslop/README.md) when work crosses into backend or developer concerns.

## Composition

1. Read the asset operations/spec, matching art doctrine, and master/family bible.
2. Use raster image generation only when a new raster candidate is required; retain source
   and staging custody declared below.
3. Use `/town-hub-asset-judgment` to accept, retry, or reject the candidate at
   required views and game scale.
4. Send only accepted candidates through the asset cut/normalization runbook,
   then promote the accepted processed result to its declared production path.

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

- [ ] Candidate judgment records accept, retry, or reject.
- [ ] Required views and game-scale read match the family bible.
- [ ] Seam, canvas, footprint, and camera checks match doctrine and asset spec.
- [ ] Only the accepted processed asset exists at the production path.

## Prohibited combinations

- Do not promote rejected candidate sheets or discarded renders.
- Do not trim or rescale road and ground tile canvases.

## Runbook routing

- [Design](../runbooks/design.md)
- [Implementing](../runbooks/implementing.md)
