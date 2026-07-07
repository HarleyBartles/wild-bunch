# BUNCH-142 Town Hub Filler Assets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate the filler buildings, road tiles, ground tiles, and standalone props needed to make town hubs feel inhabited, while keeping the asset custody tree honest and tile-safe.

**Architecture:** Split the work into a contract/tooling pass, a custody migration pass, then two independent asset families: buildings and tiles. Buildings stay on the existing sprite promotion path; roads and ground use an explicit tile-safe staging/promotion path that preserves 80x50 canvases and seam edges. `source/` holds authored assets, `staging/` holds cut or normalized intermediates, and `sprites/` holds the final promoted outputs.

**Tech Stack:** Markdown docs, Python 3.11+, Pillow, `scripts/image_asset_pipeline.py`, `scripts/generate_index_mesh.py`, and image generation using the approved style-bible prompts.

## Global Constraints

- `src/WildBunch.Assets/source/`, `src/WildBunch.Assets/staging/`, and `src/WildBunch.Assets/sprites/` are the custody homes for this work.
- Buildings are sprites and remain non-jittered.
- Tiles are 80x50 and must keep their full canvas; do not trim or rescale tile art during promotion.
- Roads and ground do not get prosperity variants.
- Standalone props are transparent sprites and may use placement jitter later, but the prop art itself stays separate from dirt tiles.
- The building sprite path may keep the existing 60x50 canvas contract where the current sprite pipeline expects it.
- The tile path must use an explicit 80x50 contract and a tile-safe promotion mode.
- Tile staging is a copy-plus-cut step into `staging/`; it does not rename canonical source files, and it does not rescale tile art.
- Tile promotion is a straight promotion from `staging/` to `sprites/` with the same filenames and the same `80x50` canvas.
- Building promotion remains the existing sprite path and may still use the current sprite canvas rules.
- `python scripts/generate_index_mesh.py --check` must pass after any file move, rename, or doc update.
- Keep the work narrow to the BUNCH-142 asset slice and the docs that define it.

---

## File Structure

### New files

- `src/WildBunch.Assets/source/town-hub-buildings/README.md`
- `src/WildBunch.Assets/source/town-hub-buildings/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-roads/README.md`
- `src/WildBunch.Assets/source/town-hub-roads/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-ground/README.md`
- `src/WildBunch.Assets/source/town-hub-ground/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-buildings/{boomtown,prosperous,poor,destitute}/background-house/{front,profile,rear,front-oblique,rear-oblique}.png`
- `src/WildBunch.Assets/source/town-hub-buildings/{boomtown,prosperous,poor,destitute}/background-shop/{front,profile,rear,front-oblique,rear-oblique}.png`
- `src/WildBunch.Assets/source/town-hub-roads/main-road/{flat-edge-right,flat-edge-left,path-edge-right,path-edge-left,spur-cross-right,spur-cross-left,end-top,end-bottom}.png`
- `src/WildBunch.Assets/source/town-hub-roads/spur-road/{straight,path-above,end-right,end-left}.png`
- `src/WildBunch.Assets/source/town-hub-ground/base/{dirt-a,dirt-b,dirt-c}.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/{cactus,tumbleweed,scrub-clump,broken-fence-post,small-rock-cluster}.png`
- `src/WildBunch.Assets/source/town-hub-ground/landforms/{hill-nw,hill-ne,hill-sw,hill-se}.png`

### Moved files

- Move the current `src/WildBunch.Assets/town-buildings/` custody tree into `src/WildBunch.Assets/source/town-hub-buildings/`.
- Preserve the existing canonical building tier/view filenames during the move.

### Modified files

- `scripts/image_asset_pipeline.py`
- `docs/art/town-buildings/style-bible.md`
- `docs/art/town-buildings/asset-spec.md`
- `docs/art/town-buildings/pipeline-overview.md`
- `.agents/art/town-buildings/DOCTRINE.md`
- `src/WildBunch.Assets/README.md`
- `src/WildBunch.Assets/AGENTS.md`
- `src/WildBunch.Assets/INDEX.md`
- any generated `INDEX.md` files under `src/WildBunch.Assets/`

---

### Task 1: Make the pipeline and docs explicit about the mixed sprite/tile contract

**Files:**
- Modify: `scripts/image_asset_pipeline.py`
- Modify: `docs/art/town-buildings/style-bible.md`
- Modify: `docs/art/town-buildings/asset-spec.md`
- Modify: `docs/art/town-buildings/pipeline-overview.md`
- Modify: `.agents/art/town-buildings/DOCTRINE.md`
- Modify: `src/WildBunch.Assets/README.md`
- Modify: `src/WildBunch.Assets/AGENTS.md`
- Create or modify: `src/WildBunch.Assets/source/town-hub-buildings/README.md`
- Create or modify: `src/WildBunch.Assets/source/town-hub-buildings/AGENTS.md`
- Create or modify: `src/WildBunch.Assets/source/town-hub-roads/README.md`
- Create or modify: `src/WildBunch.Assets/source/town-hub-roads/AGENTS.md`
- Create or modify: `src/WildBunch.Assets/source/town-hub-ground/README.md`
- Create or modify: `src/WildBunch.Assets/source/town-hub-ground/AGENTS.md`

**Interfaces:**
- Consumes: the current BUNCH-142 design spec and the live `image_asset_pipeline.py` surface
- Produces: an explicit building-vs-tile contract, a deterministic staging story, and a tile-safe pipeline command surface

- [ ] **Step 0: Add a preflight source inventory**

Record the actual source tree before any generation:
```powershell
Get-ChildItem -Recurse src/WildBunch.Assets\source\town-hub-buildings,src/WildBunch.Assets\source\town-hub-roads,src/WildBunch.Assets\source\town-hub-ground | Select-Object FullName
```

Expected:
- the building family tree still matches the intended view names
- no stale `60x50` tile assets remain in the tile families
- no unexpected files are hiding under the new family roots

- [ ] **Step 1: Add a tile-safe pipeline mode**

Add a new `stage-tiles` and `promote-tiles` path to `scripts/image_asset_pipeline.py`.

Expected command shape:
```powershell
python scripts/image_asset_pipeline.py stage-tiles --input-root <source-root> --out-root <staging-root> --canvas-width 80 --canvas-height 50
python scripts/image_asset_pipeline.py promote-tiles --input-root <staging-root> --out-root <sprites-root> --canvas-width 80 --canvas-height 50
```

The tile path must:
- cut only edge-connected background
- preserve the full 80x50 canvas
- avoid trim-and-rescale behavior
- keep mirrored and tessellating edges unchanged

The tile path must also be deterministic:
- `stage-tiles` writes to `staging/` only
- `promote-tiles` copies the staged filenames into `sprites/`
- neither command may invent new filenames
- neither command may alter the canonical left/right or top/bottom mirror mapping

- [ ] **Step 2: Rewrite the repo docs around the new custody tree**

Update the style bible, asset spec, and pipeline overview so they say:
- source is authored art
- staging is the cut/normalized review surface
- sprites are the final promoted outputs
- buildings use the sprite promotion path
- roads and ground use the tile-safe path
- `town-hub-buildings`, `town-hub-roads`, and `town-hub-ground` are the three asset families for this slice

- [ ] **Step 3: Add source-root guidance**

Create or update the source-root `README.md` and `AGENTS.md` files so workers know:
- what belongs in the family root
- which style bible controls generation
- where the AGENTS mesh points for further guidance
- that source asset family roots must not be treated as scratch space

- [ ] **Step 4: Verify the contract text is clean**

Run:
```powershell
git diff --check
python scripts/image_asset_pipeline.py --help
```

Expected:
- no whitespace errors
- the help text includes `normalize`, `slice-sheet`, `promote-sprites`, `stage-tiles`, and `promote-tiles`
- the docs state the tile staging and promotion contract without implying that tiles use the building sprite path

- [ ] **Step 5: Commit**

```powershell
git add scripts/image_asset_pipeline.py docs/art/town-buildings/style-bible.md docs/art/town-buildings/asset-spec.md docs/art/town-buildings/pipeline-overview.md .agents/art/town-buildings/DOCTRINE.md src/WildBunch.Assets/README.md src/WildBunch.Assets/AGENTS.md src/WildBunch.Assets/source/town-hub-buildings/README.md src/WildBunch.Assets/source/town-hub-buildings/AGENTS.md src/WildBunch.Assets/source/town-hub-roads/README.md src/WildBunch.Assets/source/town-hub-roads/AGENTS.md src/WildBunch.Assets/source/town-hub-ground/README.md src/WildBunch.Assets/source/town-hub-ground/AGENTS.md
git commit -m "docs: define tile-safe town-hub asset pipeline"
```

### Task 2: Rehome the existing building custody tree and scaffold the new family roots

**Files:**
- Move: `src/WildBunch.Assets/town-buildings/`
- Create: `src/WildBunch.Assets/source/town-hub-roads/`
- Create: `src/WildBunch.Assets/source/town-hub-ground/`
- Create: `src/WildBunch.Assets/staging/town-hub-buildings/`
- Create: `src/WildBunch.Assets/staging/town-hub-roads/`
- Create: `src/WildBunch.Assets/staging/town-hub-ground/`
- Create: `src/WildBunch.Assets/sprites/town-hub-buildings/`
- Create: `src/WildBunch.Assets/sprites/town-hub-roads/`
- Create: `src/WildBunch.Assets/sprites/town-hub-ground/`
- Modify: `src/WildBunch.Assets/INDEX.md`

**Interfaces:**
- Consumes: the revised docs and the tile-safe pipeline surface from Task 1
- Produces: a real `source/staging/sprites` custody tree with the building family rehomed and the road/ground roots ready for generation

- [ ] **Step 1: Move the current town-building tree**

Re-home the current town-building asset content out of `src/WildBunch.Assets/town-buildings/` and into `src/WildBunch.Assets/source/town-hub-buildings/`.

- [ ] **Step 2: Scaffold the new family roots**

Create the road and ground roots even if they start empty so later generation has a stable home.

- [ ] **Step 3: Regenerate the mesh**

Run:
```powershell
python scripts/generate_index_mesh.py
python scripts/generate_index_mesh.py --check
```

Expected:
- the generated `INDEX.md` mesh reflects the new `source/staging/sprites` layout
- the asset-root AGENTS and README files are visible in the mesh

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "chore: rehome town-hub asset custody tree"
```

### Task 3: Generate the two filler-building families

**Files:**
- Create and stage/promote:
  - `src/WildBunch.Assets/source/town-hub-buildings/{boomtown,prosperous,poor,destitute}/background-house/{front,profile,rear,front-oblique,rear-oblique}.png`
  - `src/WildBunch.Assets/source/town-hub-buildings/{boomtown,prosperous,poor,destitute}/background-shop/{front,profile,rear,front-oblique,rear-oblique}.png`
  - matching staged outputs under `src/WildBunch.Assets/staging/town-hub-buildings/...`
  - matching final outputs under `src/WildBunch.Assets/sprites/town-hub-buildings/...`

**Interfaces:**
- Consumes: the approved style bible and the building family scaffold
- Produces: 40 total filler-building images, 20 per family, each with the canonical five-view turnaround and four prosperity tiers

- [ ] **Step 1: Generate `background-house`**

Generate the first filler family across `boomtown`, `prosperous`, `poor`, and `destitute`.

If the generator emits a 5-up turnaround sheet, slice it into staging with:
```powershell
python scripts/image_asset_pipeline.py slice-sheet --input <sheet.png> --out-dir <staging-tier-dir> --names front,profile,rear,front-oblique,rear-oblique
```

If the generator emits separate view images, normalize them into staging with:
```powershell
python scripts/image_asset_pipeline.py normalize --input <view.png> --out <staging-view.png>
```

- [ ] **Step 2: Generate `background-shop`**

Use the same prompt contract, but keep the silhouette distinct enough that the second family reads as a different supporting building without becoming the dominant town feature.

- [ ] **Step 3: Promote the building sprites**

Run:
```powershell
python scripts/image_asset_pipeline.py promote-sprites --input-root src/WildBunch.Assets/staging/town-hub-buildings --out-root src/WildBunch.Assets/sprites/town-hub-buildings --canvas-width 60 --canvas-height 50
```

Expected:
- the building outputs stay on the existing sprite canvas
- the five views remain named correctly after the earlier view-label corrections

- [ ] **Step 4: Run a read and count pass**

Check that each family and tier has all five views and that the visuals still read as filler buildings at town scale.

- [ ] **Step 5: Commit**

```powershell
git add src/WildBunch.Assets/source/town-hub-buildings src/WildBunch.Assets/staging/town-hub-buildings src/WildBunch.Assets/sprites/town-hub-buildings
git commit -m "feat: add filler-building families for town hubs"
```

### Task 4: Generate the road-network and ground-fill tile families

**Files:**
- Create and stage/promote:
  - `src/WildBunch.Assets/source/town-hub-roads/main-road/{flat-edge-right,flat-edge-left,path-edge-right,path-edge-left,spur-cross-right,spur-cross-left,end-top,end-bottom}.png`
  - `src/WildBunch.Assets/source/town-hub-roads/spur-road/{straight,path-above,end-right,end-left}.png`
  - `src/WildBunch.Assets/source/town-hub-ground/base/{dirt-a,dirt-b,dirt-c}.png`
  - `src/WildBunch.Assets/source/town-hub-ground/props/{cactus,tumbleweed,scrub-clump,broken-fence-post,small-rock-cluster}.png`
  - `src/WildBunch.Assets/source/town-hub-ground/landforms/{hill-nw,hill-ne,hill-sw,hill-se}.png`
  - matching staged outputs under `src/WildBunch.Assets/staging/town-hub-roads/...`
  - matching staged outputs under `src/WildBunch.Assets/staging/town-hub-ground/...`
  - matching final outputs under `src/WildBunch.Assets/sprites/town-hub-roads/...`
  - matching final outputs under `src/WildBunch.Assets/sprites/town-hub-ground/...`

**Interfaces:**
- Consumes: the tile-safe pipeline path from Task 1 and the scaffolded road/ground roots from Task 2
- Produces: 80x50 road and ground assets that tessellate correctly and keep the town hub visually varied without prosperity tiers

- [ ] **Step 1: Generate the road tiles**

Generate the main-road and spur-road families first so the seam and attachment rules are settled before the dirt variants are tuned against them.

Stage them with:
```powershell
python scripts/image_asset_pipeline.py stage-tiles --input-root src/WildBunch.Assets/source/town-hub-roads --out-root src/WildBunch.Assets/staging/town-hub-roads --canvas-width 80 --canvas-height 50
```

- [ ] **Step 2: Generate the ground tiles and props**

Generate the 3 base dirt textures, the 5 standalone prop sprites, and the 4 landform tiles. Keep the props separate from dirt tiles so the terrain can vary without baking every prop into every tile.

Stage them with:
```powershell
python scripts/image_asset_pipeline.py stage-tiles --input-root src/WildBunch.Assets/source/town-hub-ground --out-root src/WildBunch.Assets/staging/town-hub-ground --canvas-width 80 --canvas-height 50
```

- [ ] **Step 3: Promote the tile families**

Run:
```powershell
python scripts/image_asset_pipeline.py promote-tiles --input-root src/WildBunch.Assets/staging/town-hub-roads --out-root src/WildBunch.Assets/sprites/town-hub-roads --canvas-width 80 --canvas-height 50
python scripts/image_asset_pipeline.py promote-tiles --input-root src/WildBunch.Assets/staging/town-hub-ground --out-root src/WildBunch.Assets/sprites/town-hub-ground --canvas-width 80 --canvas-height 50
```

Expected:
- the final tile PNGs remain exactly 80x50
- no road or dirt tile is trimmed or rescaled
- mirror pairs still read as mirror pairs

- [ ] **Step 4: Run the tile validation pass**

Check:
- every tile is 80x50
- the main-road left/right pieces are mirrored correctly
- the top/bottom road-end pieces are mirrored correctly
- the spur-road left/right end pieces are mirrored correctly
- the main-road canonical source is the right-hand piece
- the spur-road canonical source is the right-hand piece
- the road-end canonical source is the bottom piece
- the dirt textures tile cleanly on all edges
- the props remain transparent and do not reintroduce baked-in dirt props
- there are no tile files still carrying 60x50 assumptions
- the staged files and final files have the same relative paths
- every prop remains a transparent sprite, not a baked dirt tile

- [ ] **Step 5: Commit**

```powershell
git add src/WildBunch.Assets/source/town-hub-roads src/WildBunch.Assets/staging/town-hub-roads src/WildBunch.Assets/sprites/town-hub-roads src/WildBunch.Assets/source/town-hub-ground src/WildBunch.Assets/staging/town-hub-ground src/WildBunch.Assets/sprites/town-hub-ground
git commit -m "feat: add town-hub road and ground tile families"
```

### Task 5: Final promotion, mesh refresh, and verification

**Files:**
- Modify: any remaining `INDEX.md` files touched by the new custody tree
- Modify: any doc links that still point at the old `town-buildings/` home after the migration

**Interfaces:**
- Consumes: the generated source/staging/sprites assets from Tasks 2-4
- Produces: a clean final tree, a validated mesh, and a PR-ready diff

- [ ] **Step 1: Run the final mesh refresh**

Run:
```powershell
python scripts/generate_index_mesh.py
python scripts/generate_index_mesh.py --check
```

- [ ] **Step 2: Run the final asset sanity checks**

Run:
```powershell
git status --short
```

Expected:
- only the intended plan, doc, and asset changes remain

- [ ] **Step 3: Commit the final verification state**

```powershell
git add -A
git commit -m "chore: finalize town-hub filler asset pipeline"
```

---

## Self-Review

### Spec coverage

| Spec requirement | Task |
|---|---|
| Define the tile-safe asset pipeline | Task 1 |
| Keep source/staging/sprites custody honest | Tasks 1 and 2 |
| Rehome the existing building custody tree | Task 2 |
| Add source-root guidance for each family | Task 1 and 2 |
| Generate 2 filler-building families | Task 3 |
| Keep filler buildings visually unobtrusive | Task 1 and 3 |
| Generate main-road and spur-road tiles | Task 4 |
| Generate 3 base dirt textures | Task 4 |
| Generate 5 standalone props | Task 4 |
| Generate a 4-tile landform set | Task 4 |
| Keep roads and dirt off prosperity tiers | Tasks 1 and 4 |
| Keep tiles at 80x50 without trim/rescale | Tasks 1 and 4 |
| Promote through the repo image pipeline | Tasks 1, 3, and 4 |
| Refresh `INDEX.md` mesh and validate it | Tasks 2 and 5 |

### Placeholder scan

No placeholders remain. The plan uses concrete family names, concrete file paths, concrete commands, and concrete validation steps.

### Type consistency

- `background-house` and `background-shop` are the only filler-building families in the plan.
- `town-hub-buildings`, `town-hub-roads`, and `town-hub-ground` are the only new asset-family roots in the plan.
- `stage-tiles` and `promote-tiles` are the new tile-safe pipeline commands introduced by the plan.
- `normalize`, `slice-sheet`, and `promote-sprites` remain the building-side pipeline commands.

### Confidence

- Confidence rating: 9.0/10
- Direct execution confidence: 9.1/10
- SDD confidence: 8.8/10

### Gap closure summary

- The staging path is now explicit instead of implied.
- The tile-safe 80x50 contract is explicit instead of borrowing the building sprite canvas.
- The asset family names and counts are fixed.
- The validation commands are concrete.
- The canonical mirror sources and expected path stability are explicit.
- A source inventory preflight now catches stale filenames before generation starts.

### Open questions

- None. The remaining work is execution, not design.
