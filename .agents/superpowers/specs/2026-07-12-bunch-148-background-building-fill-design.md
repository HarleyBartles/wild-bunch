# BUNCH-148 Background Building Fill Design

## Problem

The town hub now renders the grounded main road, spurs, paths, and the foreground buildings. The remaining empty town plots still read as too sparse, especially in higher-prosperity towns. We need a deterministic background-building fill pass that uses the existing building-placement rules, prosperity-based density, and the current prosperity-matched sprite families.

## Goal

Add decorative background houses and shops to fill empty eligible town plots while preserving the existing foreground-building layout rules.

The new behavior must:
- Use the same prosperity-matched sprite selection family as the foreground town buildings
- Fill only eligible empty building plots beside roads or above/below spurs
- Respect prosperity-based density targets
- Support below-spur placement for background buildings only in addition to the current above-spur placement
- Use deterministic side/view/mirroring choices so the town stays stable for a given layout
- Treat background buildings as decorative only, not clickable
- Render a spur-path-cross tile when two background buildings face each other across a spur

## Architecture

The town hub scene keeps the tile grid and building layout as the source of truth.

Placement should work in two passes over the same eligible-slot model:
1. Resolve foreground buildings and mark the occupied tiles they claim.
2. Resolve background buildings into the remaining eligible plots using the prosperity budget.

Both building sets remain part of the same conceptual sprite layer. The distinction is semantic and behavioral, not a renderer z-layer distinction. They do not overlap because they occupy different tiles.

Data flow:

`TownLayout` -> eligible plot scan -> foreground occupancy -> background budget and selection -> tile underlays -> building sprites -> spur cross tile when needed

## Components

### Eligible Plot Scanner

Add a shared helper that identifies all legal building plots from the tile grid.

Eligible plots include:
- Tiles beside the main road
- Tiles above spurs
- Tiles below spurs

The scanner should ignore already occupied building tiles and should only consider spaces that the current town topology actually exposes.
Foreground buildings claim the canonical action-bearing eligible plots first. Background buildings only consume the remaining eligible plots after foreground occupancy has been resolved.
Foreground buildings today stay above spurs; below-spur slots are reserved for background buildings in this phase.

### Foreground Occupancy

Foreground buildings keep priority on the canonical action-bearing slots.

The occupancy model should:
- Resolve foreground building positions first
- Mark their claimed tiles
- Prevent background buildings from taking the same tile
- Preserve the existing path-underlay rules for any building that sits beside a road or above/below a spur

### Background Prosperity Budget

Background building count should vary by prosperity and stay within these deterministic ranges:

- Destitute: 0 to 2 filler buildings
- Poor: 20% to 40% of eligible filler slots
- Prosperous: 60% to 80% of eligible filler slots
- Boomtown: leave 0 to 2 eligible spaces empty

Interpret the percentage tiers as inclusive count bands after rounding to whole buildings. Pick a deterministic count within the band from the seeded town state, then clamp it to the available eligible slots.

### Background Sprite Selection

Background buildings should use the prosperity-matched background sprite families:
- `background-house`
- `background-shop`

The planner should assume the canonical asset paths already established by the filler-assets contract:
- `src/WildBunch.Assets/source/town-hub-buildings/{boomtown,prosperous,poor,destitute}/background-house/{front,profile,rear,front-oblique,rear-oblique}.png`
- `src/WildBunch.Assets/source/town-hub-buildings/{boomtown,prosperous,poor,destitute}/background-shop/{front,profile,rear,front-oblique,rear-oblique}.png`

The selection contract should mirror the main building sprite logic:
- match prosperity tier
- choose a plausible direction/view based on attachment side and tile geometry
- use mirrored variants where needed to keep the sprite facing the road or spur

The design should prefer a small variety of deterministic turnaround choices rather than expanding the asset set immediately. If the current pool looks too repetitive, new background assets can be added later without changing the placement contract.
For this phase, only background buildings may occupy below-spur slots.

### Spur Cross Tile

When two background buildings occupy the paired plots across the same spur, the spur tile between them should become a spur-path-cross tile.

Rules:
- If only one side of the spur is occupied, keep the normal spur/path tile behavior for that side
- If both sides are occupied, render the spur-path-cross tile in the spur cell between them
- The cross tile is a special spur tile, not a new road system
- The cross tile must preserve the existing spur path connection rules

For this spec, a spur-path-cross is only produced when the paired background buildings occupy the tiles immediately above and below the same spur row during the same layout pass. The cross tile replaces the intermediate spur tile only for that paired case.

### Path Underlays

Every background building must still sit on a path underlay tile that matches the existing building-underlay rules:
- east side of a road uses the horizontal path variants
- west side of a road uses the same variants mirrored around the vertical axis
- rear-oblique views use the mirrored/turned path variant that matches the building face direction
- below-spur placements should use the same path-underlay family with the updated view/mirroring rules

Below-spur background placement keeps the same east/west path-side mirroring as above-spur placement, but the building face and path direction are flipped vertically:
- above-spur background buildings use `front` or `front-oblique`
- below-spur background buildings use `rear` or `rear-oblique`
- below-spur path underlays mirror the above-spur path direction rules
- `rear-oblique` below-spur placements use the same side-based horizontal mirror choice as above-spur placements, then apply the vertical mirror so the sprite reads as the underside companion of the above-spur case

## Rendering Rules

- Background buildings are decorative only
- Background buildings do not receive click handlers
- Foreground buildings still own the action-bearing interactions
- The renderer should stay deterministic for a given layout seed

## Validation

Add focused regression coverage for:
- prosperity-based background counts
- foreground-first occupancy with background fill into remaining eligible plots
- below-spur eligibility
- sprite selection across different views and mirrored sides
- spur-path-cross placement when two background buildings face each other across a spur
- existing town-hub tile rendering and build checks remain green

## Deterministic Selection Contract

To keep planning and implementation aligned, the fill algorithm should use this fixed precedence:

1. Build the eligible plot list from the foreground placement rules first.
2. Remove any plot already claimed by a foreground building.
3. Apply the prosperity budget to the remaining plots, using the phase-specific range for the current prosperity tier.
4. Choose the plots in deterministic seeded order.
5. Assign `background-house` and `background-shop` using the seeded family selector.
6. Assign the directional variant from the same attachment-side logic used for foreground buildings, with below-spur placements restricted to the below-spur view set.
7. If a paired above/below spur background placement exists, emit the spur-path-cross tile between the pair.

## Non-Goals

- New building interior logic
- Interactive background buildings
- New prosperity tiers
- New gameplay actions for background buildings
- Expanding the asset set unless the current variety proves insufficient
