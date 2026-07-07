# Task 4 Ground Report

## What I implemented

- Regenerated the full ground-track source set under `src/WildBunch.Assets/source/town-hub-ground/`.
- Replaced the old baked `dirt-*` prop set with the canonical transparent prop sprites:
  - `cactus.png`
  - `tumbleweed.png`
  - `scrub-clump.png`
  - `broken-fence-post.png`
  - `small-rock-cluster.png`
- Rebuilt the three base dirt masters as exact `80x50` opaque tiles.
- Rebuilt the four landform tiles as a true 2x2 set so each quadrant carries a real quarter of the combined landform.
- Staged the ground track into `src/WildBunch.Assets/staging/town-hub-ground/` and promoted it into `src/WildBunch.Assets/sprites/town-hub-ground/`.
- Updated the ground-only `props/INDEX.md` files in `source/`, `staging/`, and `sprites/` to match the canonical filenames.

## What I tested and the results

- `py -3 scripts/image_asset_pipeline.py stage-tiles --input-root src/WildBunch.Assets/source/town-hub-ground --out-root src/WildBunch.Assets/staging/town-hub-ground --canvas-width 80 --canvas-height 50`
  - Ran successfully.
  - Preserved the transparent prop sprites correctly.
  - Did not preserve seam-critical full-canvas base and landform masters, so I restored those staged files byte-for-byte from `source/` before final promotion.
- `py -3 scripts/image_asset_pipeline.py promote-tiles --input-root src/WildBunch.Assets/staging/town-hub-ground --out-root src/WildBunch.Assets/sprites/town-hub-ground --canvas-width 80 --canvas-height 50`
  - Ran successfully after the source-to-staging restore for `base/` and `landforms/`.
- Python/Pillow validation:
  - Confirmed every source, staged, and final ground asset is exactly `80x50`.
  - Confirmed the five prop sprites keep transparent backgrounds in `source/`, `staging/`, and `sprites/`.
  - Confirmed the three base dirt tiles are opaque and preserve full-tile coverage in `source/`, `staging/`, and `sprites/`.
  - Confirmed the four landform tiles are opaque and compose into a real 2x2 landform set.
  - Confirmed the staged and final trees carry the same relative file paths.
  - Confirmed the dirt base textures preserve matching opposite edges for mirror-safe tiling.

## Files changed

- `.agents/superpowers/sdd/2026-07-06-bunch-142-town-hub-filler-assets-implementation/task-4-ground-report.md`
- `src/WildBunch.Assets/source/town-hub-ground/base/dirt-a.png`
- `src/WildBunch.Assets/source/town-hub-ground/base/dirt-b.png`
- `src/WildBunch.Assets/source/town-hub-ground/base/dirt-c.png`
- `src/WildBunch.Assets/source/town-hub-ground/landforms/hill-ne.png`
- `src/WildBunch.Assets/source/town-hub-ground/landforms/hill-nw.png`
- `src/WildBunch.Assets/source/town-hub-ground/landforms/hill-se.png`
- `src/WildBunch.Assets/source/town-hub-ground/landforms/hill-sw.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/INDEX.md`
- `src/WildBunch.Assets/source/town-hub-ground/props/broken-fence-post.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/cactus.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/scrub-clump.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/small-rock-cluster.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/tumbleweed.png`
- deleted stale `src/WildBunch.Assets/source/town-hub-ground/props/dirt-*.png`
- `src/WildBunch.Assets/staging/town-hub-ground/base/dirt-a.png`
- `src/WildBunch.Assets/staging/town-hub-ground/base/dirt-b.png`
- `src/WildBunch.Assets/staging/town-hub-ground/base/dirt-c.png`
- `src/WildBunch.Assets/staging/town-hub-ground/landforms/hill-ne.png`
- `src/WildBunch.Assets/staging/town-hub-ground/landforms/hill-nw.png`
- `src/WildBunch.Assets/staging/town-hub-ground/landforms/hill-se.png`
- `src/WildBunch.Assets/staging/town-hub-ground/landforms/hill-sw.png`
- `src/WildBunch.Assets/staging/town-hub-ground/props/INDEX.md`
- `src/WildBunch.Assets/staging/town-hub-ground/props/broken-fence-post.png`
- `src/WildBunch.Assets/staging/town-hub-ground/props/cactus.png`
- `src/WildBunch.Assets/staging/town-hub-ground/props/scrub-clump.png`
- `src/WildBunch.Assets/staging/town-hub-ground/props/small-rock-cluster.png`
- `src/WildBunch.Assets/staging/town-hub-ground/props/tumbleweed.png`
- deleted stale `src/WildBunch.Assets/staging/town-hub-ground/props/dirt-*.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/base/dirt-a.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/base/dirt-b.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/base/dirt-c.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/landforms/hill-ne.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/landforms/hill-nw.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/landforms/hill-se.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/landforms/hill-sw.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/props/INDEX.md`
- `src/WildBunch.Assets/sprites/town-hub-ground/props/broken-fence-post.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/props/cactus.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/props/scrub-clump.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/props/small-rock-cluster.png`
- `src/WildBunch.Assets/sprites/town-hub-ground/props/tumbleweed.png`
- deleted stale `src/WildBunch.Assets/sprites/town-hub-ground/props/dirt-*.png`

## Concerns

- `stage-tiles` is safe for transparent prop sprites, but for full-canvas opaque ground masters it currently cuts edge-connected pixels. I worked around that by restoring `base/` and `landforms/` from `source/` into `staging/` before the final promote so the shipped files preserve exact seam edges.
