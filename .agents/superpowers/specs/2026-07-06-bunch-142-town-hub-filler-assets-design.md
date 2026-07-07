# BUNCH-142 Town Hub Asset, Terrain, and Prop Contract Design

**Linear issue:** [BUNCH-142](https://linear.app/harleys-workspace/issue/BUNCH-142/generate-town-hub-filler-assets-for-buildings-and-road-tiles)
**Date:** 2026-07-07

## Goals

1. **Fill the town hub cleanly** - towns should read as inhabited places, not isolated buildings on an empty plane.
2. **Keep the terrain readable** - dirt, paths, props, and larger landforms must compose into a varied ground plane without obvious broken seams.
3. **Keep assets efficient** - tiles stay small in number, props stay reusable, and the placement contract stays mechanical.
4. **Keep asset families distinct** - filler buildings, road-network tiles, ground tiles, and standalone props each need their own contract.
5. **Stay visually consistent** - the new assets must match the existing western town look and the approved style-bible direction.
6. **Support future placement work** - the asset shapes should make town-hub assembly mostly mechanical later.

## Scope

This issue covers four asset tracks and the contract that lets them compose into a town hub:

1. **Filler buildings** - generic building shapes used to occupy inactive town-hub slots.
2. **Road-network tiles** - main road pieces and spur pieces that define how a town hub lays out its circulation.
3. **Ground tiles** - base dirt, dirt-with-path, and larger landform tiles that make the hub feel like a place.
4. **Standalone props** - transparent sprites such as cactus and tumbleweed that add variance without breaking tile seams.

The issue also covers the style-bible rewrite needed to keep those tracks visually aligned before new generation work proceeds, and the repo-doc updates needed to keep the new asset-tree contract honest.

### Required documentation surfaces

The planner should treat the following docs as in-scope and update them together:

- `docs/art/town-buildings/style-bible.md`
- `docs/art/town-buildings/asset-spec.md`
- `docs/art/town-buildings/pipeline-overview.md`
- `.agents/art/town-buildings/DOCTRINE.md`
- `src/WildBunch.Assets/README.md`
- `src/WildBunch.Assets/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-buildings/README.md`
- `src/WildBunch.Assets/source/town-hub-buildings/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-roads/README.md`
- `src/WildBunch.Assets/source/town-hub-roads/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-ground/README.md`
- `src/WildBunch.Assets/source/town-hub-ground/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-ground/paths/README.md` and `AGENTS.md` if the path family is split into its own on-disk bucket during the execution plan

## Non-goals

- Do not change the existing building prosperity ladder.
- Do not introduce prosperity variants for road tiles or ground tiles.
- Do not add random jitter to tiles or buildings.
- Do not broaden into town placement logic, road graph generation, or Phaser code changes.
- Do not move anything into the web public tree as source custody.
- Do not keep baked-in prop dirt tiles as the long-term contract when a prop should really be a sprite.
- Do not treat the existing 60x50 outputs in this branch as reusable final assets; they are stale against the current 80x50 tile contract and should be regenerated.

## Asset Family Structure

The design uses one level of family separation under the asset root. The current home is already split into source, staging, and final sprites:

- `src/WildBunch.Assets/source/`
  - `town-hub-buildings/`
  - `town-hub-roads/`
  - `town-hub-ground/`
- `src/WildBunch.Assets/staging/`
  - mirrored family layout for reviewable cutouts and normalized intermediates
- `src/WildBunch.Assets/sprites/`
  - mirrored family layout for final promoted outputs

The ground family is conceptually split into:

- `town-hub-ground/base/` - seam-safe dirt textures
- `town-hub-ground/paths/` - dirt-with-path tiles
- `town-hub-ground/props/` - transparent prop sprites
- `town-hub-ground/landforms/` - larger dirt hill or berm tiles

The planning agent should preserve that conceptual separation and treat `paths/` as a required contract bucket for the execution plan. If the current on-disk tree is missing `paths/`, the plan should create it and move the path tiles there rather than folding them into `base/`.

## Tile Contract

All tile-based art in this slice shares the same base canvas contract:

- Tile canvas: `80x50`
- Tiles are not jittered.
- Tiles are not rotated as a substitute for missing seam work.
- Tiles must tile cleanly on their intended edges before any mirroring or map assembly uses them.

This applies to road tiles, base dirt tiles, dirt-with-path tiles, and landform tiles.

## Building Contract

Filler buildings remain sprites, not tiles.

- Buildings keep the existing 5-view turnaround contract.
- Buildings do not receive positional jitter.
- Buildings remain visually dominant over any filler or terrain detail touching the same hub slot.
- The prosperity tiers remain `boomtown`, `prosperous`, `poor`, and `destitute`.
- The family set remains intentionally small so the town can be populated mechanically without requiring many unique art families.

## Road-Network Contract

Road-network assets are tiles and follow the `80x50` tile canvas contract.

### Main road

- the main road is 2 tiles wide
- the canonical source art is the right-side version
- the left-side version is the mirror of the right-side version
- the top and bottom road edges must tessellate cleanly so the road can repeat vertically

### Main-road variants

The main-road family needs these variants:

- flat edge of the road
- edge of the road with a thin path connector
- spur-crossroad edge where a spur meets the major road
- top end piece
- bottom end piece

The top end piece is the mirror of the bottom end piece.

### Spur road

- spurs are 1 tile tall
- the spur road runs horizontally through the middle of the tile
- the canonical source art is the right-side version
- the left-side version is the mirror of the right-side version
- the left and right edges of spur pieces must tessellate so the spur can repeat horizontally

### Spur variants

The spur-road family needs these variants:

- straight
- path connector for buildings above the spur
- spur end piece on the right
- spur end piece on the left

The left end piece is the mirror of the right end piece. If later placement needs buildings below the spur, the path variant should be mirror-friendly rather than reauthored.

### Tile efficiency rule

The road families should gain expressive range through mirroring and topology variants, not through prosperity tiers.

Rotation may be added later if the town-hub placement system needs north-south spurs or east-west major roads, but this issue should not depend on rotation.

## Ground Contract

Ground assets are tiles unless they are explicitly standalone props.

### Base dirt

- create 3 base dirt textures
- all 3 must tessellate on every edge
- all 3 must remain seam-safe when repeated normally or mirrored horizontally/vertically
- they should be interchangeable so towns can vary their ground rhythm without extra topology cost
- they should be visually varied enough that the play surface can avoid a stamped repeated look when the generator mixes them

### Dirt-with-path tiles

- dirt-with-path is a tile family, not a sprite overlay
- path tiles do not jitter
- path tiles must support buildings facing 8 directions: `N`, `NE`, `E`, `SE`, `S`, `SW`, `W`, `NW`
- the authored path set should be 4 tiles, with these canonical orientations:
  - `north` - path exits toward the top of the tile
  - `east` - path exits toward the right of the tile
  - `south` - path exits toward the bottom of the tile
  - `west` - path exits toward the left of the tile
- the mirrored placement contract should cover the diagonals without requiring separate authored `NE`, `SE`, `SW`, or `NW` art
- path seams must still match the surrounding dirt, including mirrored placement
- the path must read as a deliberate connector from a building front to the road, not as a random stripe on the dirt

The planner may document the exact mirror mapping if needed, but the authored set itself is fixed at the four cardinal directions above.

### Standalone props

Standalone props are transparent sprites, not baked dirt tiles.

- props may have small positional jitter
- props do not receive path-like precision placement; they are decorative variance
- props should not obscure road seams or path seams
- props should fit over the base dirt family cleanly
- props are the correct home for isolated objects that do not need to participate in tile seam logic

Recommended initial prop set:

- cactus
- tumbleweed
- scrub clump
- broken fence post
- small rock cluster

This is a starter set, not a hard ceiling. If a later prop is clearly useful and still fits the contract, it can be added without changing the terrain rules.

### Landform tiles

- include a 4-tile dirt hill or berm set
- landforms are tiles, not props
- landforms do not jitter
- landforms should feel like occasional composition anchors, not a noise pattern

### Ground variation rule

Variation belongs in the dirt layer as texture, path placement, props, and occasional larger features. It does not belong in prosperity variants.

Do not use baked prop dirt as the durable way to get terrain variety when the same result can be achieved with base dirt plus separate prop sprites.

## Composition Rules

The surface should compose in the following order:

1. **Base dirt** - establishes the terrain plane.
2. **Path tiles** - connect building fronts to roads with no jitter and no broken seams.
3. **Props** - add visual variety with small jitter so the surface feels inhabited and less repetitive.
4. **Buildings** - sit cleanly on top without jitter and keep the town silhouette dominant.

Additional edge rules:

- dirt should connect naturally with dirt edges, other dirt pieces, and the dirt edges of roads and spurs
- a dirt tile should be able to sit above or below a spur and connect cleanly
- road pieces should keep the outer edge as dirt and should fit next to adjacent dirt without visible seam breaks
- path tiles should be readable at a glance as connectors, not as decorative noise
- props should never be the thing that makes the seam work; the seam must already be correct without the prop

## Town Identity and Landscape Variety

Each town should feel like its own place without adding excessive asset families.

Recommended controls:

- vary the weighting of the 3 base dirt textures per town
- vary the density of the 5 starter props per town
- place props more sparsely near important path and road junctions
- reserve the 4-tile landform set for a few meaningful placements

This keeps the hub visually alive while preserving a small reusable asset set and avoiding a repeated tiled look.

## Validation

The implementation should validate:

- the doc rewrite lands before new generation outputs are accepted
- the new style bible and asset-spec language is explicit enough to drive generation prompts directly
- the source/staging/sprites custody tree exists in the new layout and the generated index mesh reflects it
- filler buildings read as the correct architectural family at town scale
- filler building prosperity tiers read correctly at town scale
- road tiles tessellate on their intended edges
- base dirt tiles tessellate cleanly on all edges and remain seam-safe when mirrored
- dirt-with-path tiles cover all 8 building-facing directions through the authored set plus mirrors
- standalone props remain transparent, readable, and jittered only at the sprite-placement layer
- landform tiles remain tile-compatible and non-jittered
- generated outputs are transparent where expected
- `python scripts/image_asset_pipeline.py slice-sheet` or `normalize` is used where the source material requires it
- `python scripts/image_asset_pipeline.py promote-sprites --input-root src/WildBunch.Assets/source --out-root src/WildBunch.Assets/sprites` is the promotion surface, with family-specific subpaths
- `python scripts/generate_index_mesh.py --check` passes after any file moves, renames, or doc updates

## Success Criteria

- filler buildings, road tiles, ground tiles, and standalone props are each clearly defined as separate asset tracks
- base dirt can repeat normally and in mirrored placement without the seams falling apart
- the town hub can be assembled from a small reusable set of terrain pieces plus a few transparent props
- dirt-with-path tiles connect buildings to roads in all supported building orientations without needing ad hoc art per building
- towns can vary their identity through dirt weighting, prop density, and occasional landforms without inventing new tile taxonomies
- the play surface can look varied and inhabited without turning into an obviously stamped tiled pattern
- future placement work can choose tile variants mechanically instead of artistically guessing

## Next Step

After this design is approved, write the implementation plan for the asset-generation work and keep the repo docs honest about the tile, prop, and path split.
