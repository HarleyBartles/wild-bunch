# BUNCH-142 Town Hub Filler Assets Design

**Linear issue:** [BUNCH-142](https://linear.app/harleys-workspace/issue/BUNCH-142/generate-town-hub-filler-assets-for-buildings-and-road-tiles)
**Date:** 2026-07-06

## Goals

1. **Fill the town hub cleanly** - towns should read as inhabited places, not isolated buildings on an empty plane.
2. **Keep tiles efficient** - road and dirt assets must tile cleanly and remain small in number.
3. **Keep asset families distinct** - filler buildings, road-network tiles, and dirt/ground tiles each need their own contract.
4. **Stay visually consistent** - the new assets must match the existing western town look and the approved style bible direction.
5. **Support future placement work** - the asset shapes should make town-hub assembly mostly mechanical later.

## Scope

This issue covers three asset tracks:

1. **Filler buildings** - generic building shapes used to occupy inactive town-hub slots.
2. **Road-network tiles** - main road pieces and spur pieces that define how a town hub lays out its circulation.
3. **Ground-fill tiles** - dirt, dirt-with-prop, and larger landform tiles that make the hub feel like a place.

The issue also covers the style-bible rewrite needed to keep those three tracks visually aligned before new generation work proceeds.

## Non-goals

- Do not change the existing building prosperity ladder.
- Do not introduce prosperity variants for road or dirt tiles.
- Do not broaden into town placement logic, road graph generation, or Phaser placement code.
- Do not move anything into the web public tree as source custody.
- Do not create new canonical building families beyond the generic filler-building set required here.

## Asset Family Structure

The design uses one level of family separation under the asset root. The new source-tree homes are:

- `src/WildBunch.Assets/source/`
  - `town-hub-buildings/`
  - `town-hub-roads/`
  - `town-hub-ground/`
- `src/WildBunch.Assets/staging/`
  - mirrored family layout for reviewable cutouts and normalized intermediates
- `src/WildBunch.Assets/sprites/`
  - mirrored family layout for final promoted outputs

Each family root should carry its own `README.md` and `AGENTS.md`, and those should point back to the controlling style bible and asset-spec guidance.

The planning agent should treat the current `src/WildBunch.Assets/town-buildings/` layout as the old custody shape and plan the migration into the new `source/staging/sprites` tree as part of this issue.

## Implementation Order

The planning agent should expand the issue into tasks in this order:

1. **Doc rewrite first** - update the style bible, asset spec, pipeline overview, doctrine, and root/family README + AGENTS files so the asset contract is current before generation work starts.
2. **Tree migration second** - define and move the asset custody layout from `src/WildBunch.Assets/town-buildings/` into the new `src/WildBunch.Assets/source/`, `staging/`, and `sprites/` homes.
3. **Filler buildings third** - generate the two required filler-building families across the four prosperity tiers and five-turnaround views.
4. **Road tiles fourth** - generate the main-road and spur-road tile families with the required mirrored and end-piece variants.
5. **Ground tiles fifth** - generate the base dirt, prop dirt, and landform tiles.
6. **Promotion and verification last** - cut, normalize, promote, and regenerate the index mesh after any file moves or renames.

If a task needs to split, split between the doc/tree migration work and the image generation work, not within the core asset family definition.

## V1 Deliverables

The first actionable slice should generate the following minimum set:

### Filler buildings

- 2 required generic filler-building families
- each family gets the canonical 5-view turnaround contract: `front`, `profile`, `rear`, `front-oblique`, `rear-oblique`
- each family gets the 4 prosperity tiers used elsewhere in the town-building stack: `boomtown`, `prosperous`, `poor`, `destitute`
- each family therefore produces 20 images
- the total building output is 40 images
- the filler-building set is intentionally small because repeated view selection and mirroring can make one building family cover more of the hub without requiring many new assets

### Main road

- 5 unique main-road tile variants:
  - flat edge
  - thin path connector
  - spur-crossroad edge
  - bottom end piece
  - top end piece
- canonical art is the right-side version
- left-side and top/bottom counterparts are mirrored derivatives as defined by the tile contract

### Spur road

- 3 unique spur-road tile variants:
  - straight
  - path connector
  - right end piece
- mirrored left-end derivative
- canonical art is the right-side version

### Ground fill

- 3 tessellating base dirt textures
- 8 prop dirt tiles:
  - cactus
  - tumbleweed
  - scrub clump
  - broken post or fence remnant
  - wheel-rut patch
  - trampled patch
  - dry grass tuft
  - small rock cluster
- 1 four-tile dirt hill / berm set

## Style-Bible Contract

The style-bible rewrite must make the following explicit:

- positive and negative guardrails in bullet-paragraph form so prompts can be copied directly into image generation
- separate guidance for buildings, roads, and ground fill
- canonical view / tile naming
- what should never drift: camera, palette, readability, and edge behavior
- how to keep the family visually cohesive while allowing small town-to-town variation

The style bible should not rely on visual inspection as the primary control. Visual inspection is complementary; the bible is the durable prompt contract.

## Doc Targets

The planning agent should update or create the following repository docs as part of the implementation plan:

- `docs/art/town-buildings/style-bible.md`
- `docs/art/town-buildings/asset-spec.md`
- `docs/art/town-buildings/pipeline-overview.md`
- `.agents/art/town-buildings/DOCTRINE.md`
- `src/WildBunch.Assets/README.md`
- `src/WildBunch.Assets/AGENTS.md`
- `src/WildBunch.Assets/INDEX.md`
- `src/WildBunch.Assets/source/town-hub-buildings/README.md`
- `src/WildBunch.Assets/source/town-hub-buildings/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-roads/README.md`
- `src/WildBunch.Assets/source/town-hub-roads/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-ground/README.md`
- `src/WildBunch.Assets/source/town-hub-ground/AGENTS.md`

## Filler-Building Contract

Filler buildings are sprite assets, not tiles.

### Purpose

- occupy town-hub slots that are not one of the canonical named buildings
- give the town a populated silhouette and architectural density
- act as mechanical background pieces that still read as part of the same town family

### Contract

- maintain the same top-down slight oblique western look as the existing town-building set
- remain visually unobtrusive compared with the named buildings
- preserve a readable roof mass and footprint at town scale
- use the same prosperity tiers as the named building families so hub density can match town prosperity without inventing new road or ground tiers
- fit cleanly with the named buildings in the same prosperity tier, but never compete with them for visual dominance
- read as supporting architecture that fills the town out behind the more important main buildings

### Variation

- allow modest roofline / porch / massing differences so the filler buildings do not become clones
- keep them recognizable as town buildings first and filler pieces second
- keep prosperity readable through accumulated detail, materials, and completeness rather than through a new building taxonomy
- use different view selections of the same building family, plus mirroring, to stretch a small source set into a larger hub presence
- rely on reuse and orientation variety to make one filler family feel sufficient unless the hub needs a second family for balance

## Road-Network Contract

Road-network assets are tiles.

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

## Ground-Fill Contract

Ground-fill assets are tiles.

### Base dirt

- create 3 base dirt textures
- all 3 must tessellate on every edge
- they should be interchangeable so towns can vary their ground rhythm without extra topology cost

### Prop dirt

Prop dirt tiles are self-contained dirt tiles with a baked-in prop and edge-compatible framing. They are not overlays.

Recommended prop set:

- cactus
- tumbleweed
- scrub clump
- broken post or fence remnant
- wheel-rut patch
- trampled patch
- dry grass tuft
- small rock cluster

### Landform tiles

- include a 4-tile dirt hill or berm as an occasional larger feature
- the landform should feel like a composition anchor, not a repeating noise tile

### Ground variation rule

Variation belongs in the dirt layer as texture, props, and occasional larger features. It does not belong in prosperity variants.

## Tessellation Rules

The tiles must support three simultaneous composition contracts:

1. **Main-road repetition** - top and bottom of main-road pieces connect perfectly so the road can repeat vertically.
2. **Spur repetition** - left and right of spur pieces connect perfectly so the spur can repeat horizontally.
3. **Ground blending** - dirt connects naturally to dirt and to the dirt-facing edges of roads and spurs.

Additional edge rules:

- spurs should have dirt on the top and bottom edges
- a dirt tile should fit above or below a spur without visible seams
- main-road outer edges should face dirt cleanly
- path and spur variants should cross the dirt edge without breaking the seam contract

## Town Identity and Landscape Variety

Each town should feel like its own place without adding excessive asset families.

Recommended controls:

- vary the weighting of the 3 base dirt textures per town
- vary the density of prop dirt tiles per town
- choose different prop mixes for different town moods
- reserve the 4-tile landform for a few meaningful placements

This keeps the hub visually alive while preserving a small reusable asset set.

## Validation

The implementation should validate:

- the doc rewrite lands before new generation outputs are accepted
- the new style bible and asset-spec language is explicit enough to drive generation prompts directly
- the source/staging/sprites custody tree exists in the new layout and the generated index mesh reflects it
- filler buildings read as the correct architectural family at town scale
- filler building prosperity tiers read correctly at town scale
- road tiles tessellate on their intended edges
- ground tiles tessellate with each other and with road dirt edges
- prop dirt tiles remain fully tile-compatible
- the output homes are correct for source, staging, and promoted sprites
- generated outputs are transparent where expected
- `python scripts/image_asset_pipeline.py slice-sheet` or `normalize` is used where the source material requires it
- `python scripts/image_asset_pipeline.py promote-sprites --input-root src/WildBunch.Assets/source --out-root src/WildBunch.Assets/sprites` is the promotion surface, with family-specific subpaths
- `python scripts/generate_index_mesh.py --check` passes after any file moves, renames, or doc updates

## Success Criteria

- filler buildings, road tiles, and ground tiles are each clearly defined as separate asset tracks
- road and ground tiles tile cleanly without requiring prosperity variants
- filler buildings have the expected prosperity ladder across the five-turnaround set
- the town hub can be assembled from a small reusable set of tiles plus generic filler buildings
- towns can vary their identity through base dirt weighting, props, and occasional landforms
- future placement work can choose tile variants mechanically instead of artistically guessing

## Next Step

After this design is approved, write the implementation plan for the asset-generation work and keep the repo docs honest about the new family split.
