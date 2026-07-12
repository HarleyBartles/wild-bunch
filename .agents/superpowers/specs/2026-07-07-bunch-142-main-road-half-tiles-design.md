# BUNCH-142 Main Road Half-Tile and Prompt-Guardrail Design

**Linear issue:** [BUNCH-142](https://linear.app/harleys-workspace/issue/BUNCH-142/generate-town-hub-filler-assets-for-buildings-and-road-tiles)
**Date:** 2026-07-07

## Goal

Define the canonical main-road tile slice for the town hub so the road reads as one continuous north-to-south route without cap ends, while still allowing mirror placement to produce a full two-tile-wide road. The design also needs to tighten the style guidance so future image-generation agents can copy composable positive and negative prompt blocks directly from the repo.

## Scope

This issue slice covers only the main-road half tiles and the repo docs that describe how to constrain and validate them.

### In scope

1. Canonical road-half art for the main road.
2. Style-bible and doctrine updates that describe the road-half contract in copyable `Do` / `Do not` paragraphs.
3. Asset-spec and pipeline wording updates so the repo no longer describes road caps or rotation-based road handling for this slice.
4. Removal or deprecation of any non-canonical road-half files that are only present because of earlier over-generation.

### Out of scope

1. Spur-road end caps.
2. North/south major-road cap ends.
3. Rotation-based road tiling.
4. Ground tiles, dirt props, landforms, or building assets.
5. Phaser placement logic or town layout code.

## Canonical Road Set

The main-road source custody is the canonical right-hand half tiles only:

- `src/WildBunch.Assets/source/town-hub-roads/main-road/road-flat-edge.png`
- `src/WildBunch.Assets/source/town-hub-roads/main-road/road-spur-edge.png`
- `src/WildBunch.Assets/source/town-hub-roads/main-road/road-path-edge.png`

The left-hand read is the horizontal mirror of each canonical tile and is not part of the canonical source custody set for this slice. These source files already exist and are the truth the pipeline must preserve.

Derived outputs may still materialize the mirrored left-hand companions in `staging/` and `sprites/` when the pipeline or review workflow needs them, but those files are derived artifacts, not canonical custody. The implementation must not treat them as primary inputs or as an expanded source set.

The following files are not part of the canonical road slice and should be removed, ignored, or otherwise de-canonized as part of implementation if they currently exist:

- `flat-edge-left.png`
- `spur-edge-left.png`
- `path-edge-left.png`
- `end-top.png`
- `end-bottom.png`

## Road Contract

The main road is a two-tile-wide route that runs north-to-south through the town hub.

Each canonical tile is a full `80x50` road-half tile with this edge contract:

- **Left edge:** road seam
  - This is the canonical road side.
  - When the tile is mirrored horizontally and placed adjacent to the original, the two tiles form the full two-tile-wide road band.
- **Right edge:** variant-specific outer seam
  - `flat-edge`: dirt edge only.
  - `spur-edge`: spur side-trail junction.
  - `path-edge`: thin path connector that can meet a later dirt/path tile.
- **Top edge:** tiles cleanly with the bottom edge so the road can repeat vertically.
- **Bottom edge:** tiles cleanly with the top edge for the same reason.

The road should read as a continuous vertical corridor. There are no visible cap ends in this slice. The road extends north-to-south and any trail-head or terminal treatment is deferred to a later asset pass.

## Variant Intent

### `flat-edge`

- Default main-road half tile.
- Left edge reads as the road band.
- Right edge reads as dirt.
- This is the cleanest version for straight runs of road beside dirt terrain.

### `spur-edge`

- Main-road half tile with a right-edge spur junction.
- Left edge still reads as the road band.
- Right edge shows the spur side-trail connection where a spur meets the major road.
- This is the only road-half variant that needs to imply the spur mouth.

### `path-edge`

- Main-road half tile with a right-edge thin path connector.
- Left edge still reads as the road band.
- Right edge shows a path lead-in that can join to a later dirt-with-path tile.
- This is the bridge between the road and building-adjacent dirt work.

## Style Guide / Bible Requirements

The repo's road guidance needs to become directly reusable by image-generation agents instead of being only descriptive prose.

Update the road-facing guidance in:

- `docs/art/town-buildings/style-bible.md`
- `docs/art/town-buildings/asset-spec.md`
- `docs/art/town-buildings/pipeline-overview.md`
- `.agents/art/town-buildings/DOCTRINE.md`

The update should add composable prompt blocks with explicit positive and negative guardrails. Each block should be copyable as a whole paragraph set into image generation prompts.

### Required prompt-block shape

The road guidance should include:

- one shared road contract block
- one positive / negative block for `flat-edge`
- one positive / negative block for `spur-edge`
- one positive / negative block for `path-edge`

Each block should be written so an agent can paste it into a prompt without having to reconstruct the intent from surrounding prose. The blocks should state:

- the road-half geometry
- the `80x50` canvas contract
- the canonical right-hand orientation
- the mirrored partner behavior
- the top/bottom seam behavior
- the absence of cap ends in this slice
- the prohibition on rotation-based tiling for this slice

The negative guardrails should explicitly forbid:

- adding prosperity language to road tiles
- inventing cap ends
- broadening the slice into ground or building work
- turning the road-half tiles into full scene compositions

## Documentation Contract

The docs should say the same thing in all three places:

1. The asset spec defines the road slice and canonical files.
2. The pipeline overview explains how the road slice is promoted.
3. The doctrine gives agents a short, actionable version of the same contract.

If any of those documents still imply end caps, rotation-based tiling, or a canonical left-hand file set, they should be repaired in the same pass.

## Validation

The implementation should verify all of the following:

1. The canonical road slice is reduced to the three right-hand files above.
2. The source tree keeps only the canonical right-hand custody files, while staging and sprites may materialize exact mirrored left-hand companions as derived outputs only if the pipeline requires them.
3. Mirroring any canonical road half horizontally produces the matching left-hand read.
4. The mirrored pair forms a full two-tile-wide major road with the road in the middle and dirt on the outside edges.
5. The top and bottom edges still tile cleanly when road halves are stacked vertically.
6. The updated style bible and doctrine contain copyable `Do` / `Do not` blocks, not just generic prose.
7. `python scripts/generate_index_mesh.py --check` still passes after any file removals or renames.

## Success Criteria

- The road slice is small and mechanical: three canonical right-hand road-half assets only.
- The main road reads as one continuous north-to-south corridor with no cap ends.
- The mirrored partner of each road half is obvious and exact.
- The repo documents are composable enough that a future image-generation agent can lift the positive and negative guardrails directly into prompts.

## Handoff Confidence

Handoff confidence for planning: **8.5/10**.

The remaining work is concrete and narrow: write the plan, then implement the three-file road slice and the associated doc updates without broadening into cap-end, spur-end, or ground work.
