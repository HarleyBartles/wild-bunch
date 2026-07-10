# Town Hub Directional Mirroring Bible

This document defines the canonical-side, compass-facing, and mirror-on-use
rules for town-hub sprites and tiles. It complements the tiling bible, which
handles seam and tessellation behavior.

## Core rules

- Use compass directions, not left/right, when describing canonical sides.
- Source custody stores one canonical side only.
- The canonical source side is mirrored in use when the target contract requires
  the opposite side.
- Do not store both sides in source unless a family bible explicitly says it is
  a special case.

## Canonical source convention

- When a family is stored as a half-tile or side-tile, define the canonical
  source side in compass terms for that family and keep only that side in
  source.
- When a family is stored as a full tile with directional structure, define the
  canonical source orientation in compass terms for that family and keep only
  that orientation in source.
- The source filename is not the identity; the directional contract is.

## Buildings

- Building masters keep the canonical turnaround set in source.
- The canonical building turnarounds face north, northwest, west, southwest,
  and south.
- Buildings placed on the east side of the town map use the building sprites as
  is.
- Buildings placed on the west side of the town map use the mirrored building
  sprites.
- Building frontage should read as facing inward toward the road when mirrored
  for use.

## Roads and spurs

- The main road canonical source stores the east side of the road, with dirt on
  the east and road on the west in source orientation.
- The road mirrors across its vertical axis to form the full street read.
- The spur canonical source stores the east side of the spur family unless the
  family-specific file says otherwise.
- The canonical spur direction for the path-bearing variant is north.
- The spur-edge main-road attachment is the version where the spur leads east
  in source.
- The spur end cap is the eastern terminus of the spur: the spur connection is
  on the west side of the source image and the dirt fade occupies the east
  side.

## Paths

- Paths are narrow connectors.
- Path source art should preserve the canonical width required at the join edge
  so the path can continue into other tiles cleanly.

## Prompt-ready guardrails

- Do: Describe the exact compass-facing side that must be stored in source.
- Do not: Do not rely on filenames to communicate canonical direction.

- Do: Say whether the asset is mirrored on use or stored in both directions.
- Do not: Do not mix seam/tessellation rules into this document unless they are
  also directional rules.
