# Town Hub Ground Style Bible

The town-hub ground set is the shared western terrain language for the play
surface. It is the target contract for dirt, road, spur, path, and prop art we
are generating in this work, and it needs to read like one consistent world.

For seam and tessellation rules, use
`src/WildBunch.Assets/docs/bibles/tiling-bible.md`. For canonical-side
and compass facing rules, use
`src/WildBunch.Assets/docs/bibles/directional-mirroring-bible.md`.

## Contract map

| Document | Owns |
| --- | --- |
| `tiling-bible.md` | Seam behavior, tessellation, edge continuity, and mirror-safe tile contracts |
| `directional-mirroring-bible.md` | Canonical-side custody, compass-facing rules, and source-side mirroring |
| Family bibles | Visual identity, family-specific exceptions, and prompt-ready guardrails |

Use this map before writing or updating a family bible so the rule lives in one
place only.

## Visual contract

- Camera: top-down with a slight oblique tilt.
- Presentation: pixel art with crisp, readable edges.
- Palette: one consistent western dirt palette for this task; keep it dusty,
  muted, sun-baked, and practical.
- Texture: compacted dirt, rough stone, sparse scrub, worn track marks, and
  weathering that reads at town scale.
- Readability: the tile or sprite must stay legible when scaled down for the
  game surface.
- Source art: if a master is meant to be processed later, keep it large enough
  to scale down cleanly instead of drawing directly at the final 80x50 tile
  size.
- Full bleed: tiled source art must fill the canvas edge-to-edge. Do not leave
  white borders, margins, or frame lines around any tile family.

## Shared ground rules

- Ground assets are about seam behavior and surface identity, not prosperity
  tiers.
- Mirrored copies must still tile cleanly.
- Tile edges should blend naturally into adjacent tiles without a seam line,
  center stripe, or material break.
- The canonical dirt palette is shared across the dirt family and should anchor
  to the approved road-adjacent dirt tone rather than drifting per variant.
- Keep the surface grounded and practical, not cinematic, painterly, or
  over-stylized.
- Keep props separate from ground plates unless a family bible explicitly says
  otherwise.

## Source size and pipeline scaling

- Source files should be generated at large size (1024x1024) to enable future scaling to different output file sizes.
- The asset pipeline handles scaling from large source files to the target canvas size.
- Dirt tiles: scaled to 80x50 canvas (full tile grid)
- Props: scaled to 80x50 canvas (to match dirt tile grid)
- Always generate source files at large size first, then use the asset pipeline for normalization and scaling.

## Prompt-ready guardrails

- Do: Keep the art rooted in the same dusty western terrain language across all
  ground families, and make edge matching, mirror safety, and readable surface
  texture the first priority.
- Do not: Do not change palette families, camera angle, or rendering style
  between dirt, road, spur, path, and prop work; do not introduce fantasy
  surfaces, modern materials, or visible seam lines.

- Do: Keep the ground surface full of practical small-scale detail like pebbles,
  compacted dust, worn stone, and sparse scrub where appropriate.
- Do not: Do not turn the ground into a decorative illustration, a lush biome,
  or a cluttered scene with dominant landmarks unless the family bible asks for
  it.

- Do: Treat props as standalone western objects when the family bible calls for
  them.
- Do not: Do not bake props into a ground plate in new work, and do not let
  prop shapes break the tile seam contract.

## Family map

- `dirt`: base terrain tiles and the shared dirt surface language
- `road`: the main road half-tile set
- `spur`: the spur road set
- `path`: the thin connector set
- `props`: standalone transparent prop sprites

Use the family bible for the asset you are generating, and keep this master
document as the shared constraint layer.
