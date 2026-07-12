# Town Hub Tiling Bible

This document defines the seam and tessellation contract for the town-hub
ground asset family. It is the master rule layer for dirt, road, spur, path,
and related tile work.

## Core rules

- Tile seams must remain valid under the allowed mirroring rules for the family.
- Edge features must not depend on a hidden orientation that disappears when
  mirrored.
- Use compass directions when describing which edge must connect to which edge.

## Family tiling contracts

### Dirt

- Dirt tiles must tile on all four edges.
- Dirt tiles must remain valid when mirrored horizontally, vertically, or on
  both axes.
- Dirt variation must stay interior-only; edges must stay neutral and
  repeat-safe.
- Dirt edge bands must stay in the shared canonical dirt palette so adjoining
  dirt tiles read as one landscape instead of separate swatches.

### Main road

- The road mirrors across its vertical axis to form the full street read.

### Spur road

- The spur family must remain mirror-safe in both axes.
- The canonical path-bearing spur tile must keep the path leading north.
- The spur edge and end cap must keep their join edge centered and repeat-safe
  with the other spur pieces.
- If a future cross-junction tile is introduced, the horizontal band must
  remain the spur family read and the vertical band must remain the lighter
  path family read. The spur should stay the darker browner track; the path
  should stay the lighter worn connector.

### Path

- Paths are narrow connectors.
- Path tiles must tile cleanly with the other path pieces and with dirt-backed
  compositions.
- Keep path width consistent at the tile edge where another path tile must
  continue it.

## Prompt-ready guardrails

- Do: Describe the exact edge a tile must connect on using compass language.
- Do not: Do not use left/right as the only contract language for new assets.

- Do: Call out whether a family is mirrorable on one axis or both axes.
- Do not: Do not place large landmarks, strong directional gradients, or edge
  objects that would break mirroring.

- Do: Keep seam safety higher priority than interior decoration.
- Do not: Do not let a seam contract depend on source-side custody rules.
