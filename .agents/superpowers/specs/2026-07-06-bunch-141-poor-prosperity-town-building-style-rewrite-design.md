# Poor Prosperity Town-Building Style Rewrite Design

**Linear issue:** [BUNCH-141](https://linear.app/harleys-workspace/issue/BUNCH-141/generate-poor-prosperity-town-building-asset-set)
**Date:** 2026-07-06

## Goal

Rewrite the town-building style guidance so `poor` becomes a first-class prosperity tier between `destitute` and `prosperous`, with one consistent visual language across all canonical building families and all five turnaround views.

## Problem

The current town-building guidance names the shared camera, footprint, and turnaround contract, but it does not yet describe the missing `poor` tier as a deliberate midpoint in the visual ladder. That leaves image generation under-specified for the next prosperity pass:

- `boomtown` already reads as the most ornate end of the ladder.
- `prosperous` reads as the polished middle-high tier.
- `destitute` reads as the rough low tier.
- `poor` needs to sit between `destitute` and `prosperous` without drifting into a new silhouette family or a different camera contract.

Without explicit midpoint rules, generation can overcorrect toward either the roughest or richest look and the family set will stop feeling coherent.

## Design

### Shared contract stays fixed

The style rewrite keeps the existing non-negotiables:

- top-down slight oblique camera
- pixel-art presentation
- 60x50 normalized footprint
- five-view turnaround contract
- the four canonical families: general store, sheriff office, saloon, telegraph office

No new family, camera, or footprint rules are introduced for `poor`.

### Prosperity ladder

The ladder becomes an explicit read order:

`destitute` -> `poor` -> `prosperous` -> `boomtown`

The rewrite should describe `poor` as the bridge tier, not as a separate architecture. The visual differences are in maintenance, ornament density, trim quality, signage, and material finish rather than in massing or layout.

### Poor-tier visual direction

`poor` should read as:

- modest but maintained
- repaired rather than pristine
- slightly upgraded from destitute, not fully finished
- less decorative than prosperous, but not as stripped down as destitute

That means the style bible should steer image generation toward:

- same roof and wall massing as the other tiers
- same family silhouettes and recognizable turnarounds
- cleaner or more complete structure than destitute
- restrained ornament, trim, and signage relative to prosperous
- consistent weathering that still leaves the building usable and readable

The rewrite should make clear that `poor` is a midpoint in the same family ladder, not a separate design language.

### Prompt language

The prompt guidance should emphasize:

- consistent family identity across all tiers
- the tier ladder as a continuum
- `poor` as the bridge between rough and polished
- camera/footprint stability over decorative variation

The rewrite should avoid wording that invites:

- level-eye storefront views
- frontier shack drift
- painterly concept-art treatment
- extra architectural flourish that would make `poor` read like a new class

### Doc surfaces to align

The design should update the current town-building guidance surfaces together so they do not drift apart:

- `docs/art/town-buildings/style-bible.md`
- `.agents/art/town-buildings/DOCTRINE.md`
- `docs/art/town-buildings/pipeline-overview.md` if the workflow wording needs to mention the new midpoint tier explicitly

If the asset spec needs a small wording change to keep the tier ladder consistent, update it in the same pass.

## Validation

- Re-read the rewritten style bible against the inspected boomtown, prosperous, and destitute assets.
- Confirm the wording makes `poor` a midpoint without changing the camera, footprint, or turnaround contract.
- Confirm the agent doctrine and pipeline guidance point at the same ladder and do not contradict the human-facing bible.

## Non-goals

- No new image generation procedure.
- No new asset folders or naming scheme.
- No new gameplay or runtime surface.
- No change to the approved asset pipeline behavior beyond wording and guidance alignment.

## Open Question

If a future poor-tier prompt needs a specific material bias, the current decision is to keep it as a midpoint between destitute and prosperous rather than inventing a separate poor-only silhouette. That is the only unresolved degree of freedom this rewrite leaves open.
