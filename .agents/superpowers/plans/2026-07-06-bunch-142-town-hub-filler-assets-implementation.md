# BUNCH-142 Town Hub Filler Assets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rehome town-hub asset custody into `source/staging/sprites`, rewrite the town-building style contract, and generate the filler buildings, road-network tiles, and ground-fill tiles needed to make town hubs read as complete places.

**Architecture:** This plan splits the work into a doc-and-custody pass, then three independent asset families: filler buildings, road tiles, and ground tiles. The existing `src/WildBunch.Assets/town-buildings/` tree is the old custody shape and gets migrated into the new `src/WildBunch.Assets/source/`, `src/WildBunch.Assets/staging/`, and `src/WildBunch.Assets/sprites/` homes before new art is accepted. Promotion stays boring and mechanical through `scripts/image_asset_pipeline.py`, and file-mesh freshness is enforced with `scripts/generate_index_mesh.py`.

**Tech Stack:** Markdown docs, Python 3.11+, Pillow, `scripts/image_asset_pipeline.py`, `scripts/generate_index_mesh.py`, image generation with the repo-approved style bible prompts.

## Global Constraints

- `src/WildBunch.Assets/source/`, `src/WildBunch.Assets/staging/`, and `src/WildBunch.Assets/sprites/` are the new asset custody homes.
- `src/WildBunch.Assets/town-buildings/` is the old custody shape and must be migrated into the new tree.
- Do not move anything into the web public tree as source custody.
- Do not introduce prosperity variants for road or dirt tiles.
- The road families should gain expressive range through mirroring and topology variants, not through prosperity tiers.
- Variation belongs in the dirt layer as texture, props, and occasional larger features. It does not belong in prosperity variants.
- The filler-building set is intentionally small because repeated view selection and mirroring can make one building family cover more of the hub without requiring many new assets.
- The filler-building set is exactly two families, not one-and-a-half or a stretch goal.
- `python scripts/image_asset_pipeline.py` requires Python 3.11+ with Pillow installed in the active environment.
- Building promotion may use `promote-sprites`, but road and ground tiles must stay tile-safe and never go through the sprite cutter because seam alignment matters more than trimming.
- Road and ground tiles must keep their full 60x50 canvas through source, staging, and sprites; copy promotion is the contract for those families.
- `python scripts/generate_index_mesh.py --check` must pass after any file moves, renames, or doc updates.
- Keep the work narrow to the town-hub asset generation slice and the docs that define it.

---

## File Structure

### New files

- `src/WildBunch.Assets/source/town-hub-buildings/README.md`
- `src/WildBunch.Assets/source/town-hub-buildings/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-roads/README.md`
- `src/WildBunch.Assets/source/town-hub-roads/AGENTS.md`
- `src/WildBunch.Assets/source/town-hub-ground/README.md`
- `src/WildBunch.Assets/source/town-hub-ground/AGENTS.md`
- `src/WildBunch.Assets/staging/town-hub-buildings/README.md`
- `src/WildBunch.Assets/staging/town-hub-roads/README.md`
- `src/WildBunch.Assets/staging/town-hub-ground/README.md`
- `src/WildBunch.Assets/sprites/town-hub-buildings/README.md`
- `src/WildBunch.Assets/sprites/town-hub-roads/README.md`
- `src/WildBunch.Assets/sprites/town-hub-ground/README.md`
- `src/WildBunch.Assets/source/town-hub-buildings/boomtown/background-house/front.png`
- `src/WildBunch.Assets/source/town-hub-buildings/boomtown/background-house/profile.png`
- `src/WildBunch.Assets/source/town-hub-buildings/boomtown/background-house/rear.png`
- `src/WildBunch.Assets/source/town-hub-buildings/boomtown/background-house/front-oblique.png`
- `src/WildBunch.Assets/source/town-hub-buildings/boomtown/background-house/rear-oblique.png`
- `src/WildBunch.Assets/source/town-hub-buildings/boomtown/background-shop/front.png`
- `src/WildBunch.Assets/source/town-hub-buildings/boomtown/background-shop/profile.png`
- `src/WildBunch.Assets/source/town-hub-buildings/boomtown/background-shop/rear.png`
- `src/WildBunch.Assets/source/town-hub-buildings/boomtown/background-shop/front-oblique.png`
- `src/WildBunch.Assets/source/town-hub-buildings/boomtown/background-shop/rear-oblique.png`
- Repeat the same five-view file set for `prosperous/`, `poor/`, and `destitute/` for both filler-building families.
- `src/WildBunch.Assets/source/town-hub-roads/main-road/flat-edge-right.png`
- `src/WildBunch.Assets/source/town-hub-roads/main-road/flat-edge-left.png`
- `src/WildBunch.Assets/source/town-hub-roads/main-road/path-edge-right.png`
- `src/WildBunch.Assets/source/town-hub-roads/main-road/path-edge-left.png`
- `src/WildBunch.Assets/source/town-hub-roads/main-road/spur-cross-right.png`
- `src/WildBunch.Assets/source/town-hub-roads/main-road/spur-cross-left.png`
- `src/WildBunch.Assets/source/town-hub-roads/main-road/end-top.png`
- `src/WildBunch.Assets/source/town-hub-roads/main-road/end-bottom.png`
- `src/WildBunch.Assets/source/town-hub-roads/spur-road/straight.png`
- `src/WildBunch.Assets/source/town-hub-roads/spur-road/path-above.png`
- `src/WildBunch.Assets/source/town-hub-roads/spur-road/end-right.png`
- `src/WildBunch.Assets/source/town-hub-roads/spur-road/end-left.png`
- `src/WildBunch.Assets/source/town-hub-ground/base/dirt-a.png`
- `src/WildBunch.Assets/source/town-hub-ground/base/dirt-b.png`
- `src/WildBunch.Assets/source/town-hub-ground/base/dirt-c.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/dirt-cactus.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/dirt-tumbleweed.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/dirt-scrub.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/dirt-post.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/dirt-ruts.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/dirt-trampled.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/dirt-grass.png`
- `src/WildBunch.Assets/source/town-hub-ground/props/dirt-rocks.png`
- `src/WildBunch.Assets/source/town-hub-ground/landforms/hill-nw.png`
- `src/WildBunch.Assets/source/town-hub-ground/landforms/hill-ne.png`
- `src/WildBunch.Assets/source/town-hub-ground/landforms/hill-sw.png`
- `src/WildBunch.Assets/source/town-hub-ground/landforms/hill-se.png`

### Moved files

- Move the current `src/WildBunch.Assets/town-buildings/` custody tree into the new `src/WildBunch.Assets/source/town-hub-buildings/` / `staging/town-hub-buildings/` / `sprites/town-hub-buildings/` layout.
- Preserve the existing canonical building tier/view filenames during the move so the new filler-building family can sit beside them without changing the established view vocabulary.

### Deterministic staging contract

- Buildings: copy `src/WildBunch.Assets/source/town-hub-buildings/` into `src/WildBunch.Assets/staging/town-hub-buildings/`, then run `promote-sprites` from staging into `src/WildBunch.Assets/sprites/town-hub-buildings/`.
- Road tiles: copy `src/WildBunch.Assets/source/town-hub-roads/` into `src/WildBunch.Assets/staging/town-hub-roads/`, validate the tile dimensions and mirror pairs there, then copy staging into `src/WildBunch.Assets/sprites/town-hub-roads/`.
- Ground tiles: copy `src/WildBunch.Assets/source/town-hub-ground/` into `src/WildBunch.Assets/staging/town-hub-ground/`, validate the tile dimensions and seam-safe variants there, then copy staging into `src/WildBunch.Assets/sprites/town-hub-ground/`.
- All three custody homes remain part of the permanent contract; staging is not optional scratch space.

### Modified files

- `docs/art/town-buildings/style-bible.md`
- `docs/art/town-buildings/asset-spec.md`
- `docs/art/town-buildings/pipeline-overview.md`
- `.agents/docs/town-buildings-doctrine.md`
- `src/WildBunch.Assets/README.md`
- `src/WildBunch.Assets/AGENTS.md`
- `src/WildBunch.Assets/INDEX.md`
- `src/WildBunch.Assets/town-buildings/INDEX.md` if the migration leaves the old tree in place during a step boundary
- `scripts/image_asset_pipeline.py` only if the new custody layout exposes a real command-path mismatch during implementation

---

### Task 1: Rewrite the asset contract docs first

**Files:**
- Modify: `docs/art/town-buildings/style-bible.md`
- Modify: `docs/art/town-buildings/asset-spec.md`
- Modify: `docs/art/town-buildings/pipeline-overview.md`
- Modify: `.agents/docs/town-buildings-doctrine.md`
- Modify: `src/WildBunch.Assets/README.md`
- Modify: `src/WildBunch.Assets/AGENTS.md`

**Interfaces:**
- Consumes: the current town-building docs and the approved BUNCH-142 design spec
- Produces: explicit docs for the three asset tracks, the new source/staging/sprites custody rule, the filler-building visibility rule, and the tile-tessellation contract

- [ ] **Step 1: Rewrite the style bible to carry the final prompt contract**

Write the docs so they say, in plain language:
- filler buildings are visually unobtrusive supporting buildings, not the dominant town feature
- the two filler-building families use the same 5-view turnaround and 4 prosperity tiers as the named buildings
- roads and dirt do not use prosperity tiers
- road tiles are about mirroring and topology, not tier variation
- dirt variation comes from base textures, prop-baked tiles, and a single larger landform set

- [ ] **Step 2: Rewrite the asset spec and pipeline overview for the new custody tree**

Update the human-facing docs so they name the actual homes:
- `src/WildBunch.Assets/source/`
- `src/WildBunch.Assets/staging/`
- `src/WildBunch.Assets/sprites/`

Make the docs explicitly describe:
- `town-hub-buildings`
- `town-hub-roads`
- `town-hub-ground`

- [ ] **Step 3: Update the asset-root AGENTS files**

Add the repo-local guidance that points workers at the style bible, asset spec, and doctrine documents for the town-hub asset family split.

- [ ] **Step 4: Verify the doc diff is clean**

Run:
```powershell
git diff --check
```
Expected: no whitespace errors, no malformed markdown blocks, no accidental trailing whitespace.

- [ ] **Step 5: Commit**

```powershell
git add docs/art/town-buildings/style-bible.md docs/art/town-buildings/asset-spec.md docs/art/town-buildings/pipeline-overview.md .agents/docs/town-buildings-doctrine.md src/WildBunch.Assets/README.md src/WildBunch.Assets/AGENTS.md
git commit -m "docs: rewrite town-hub asset contract for source staging sprites"
```

### Task 2: Rehome the custody tree and scaffold the new family roots

**Files:**
- Move: `src/WildBunch.Assets/town-buildings/` into `src/WildBunch.Assets/source/town-hub-buildings/`, `src/WildBunch.Assets/staging/town-hub-buildings/`, and `src/WildBunch.Assets/sprites/town-hub-buildings/`
- Create: `src/WildBunch.Assets/source/town-hub-buildings/README.md`
- Create: `src/WildBunch.Assets/source/town-hub-buildings/AGENTS.md`
- Create: `src/WildBunch.Assets/source/town-hub-roads/README.md`
- Create: `src/WildBunch.Assets/source/town-hub-roads/AGENTS.md`
- Create: `src/WildBunch.Assets/source/town-hub-ground/README.md`
- Create: `src/WildBunch.Assets/source/town-hub-ground/AGENTS.md`
- Create: `src/WildBunch.Assets/staging/town-hub-buildings/README.md`
- Create: `src/WildBunch.Assets/staging/town-hub-roads/README.md`
- Create: `src/WildBunch.Assets/staging/town-hub-ground/README.md`
- Create: `src/WildBunch.Assets/sprites/town-hub-buildings/README.md`
- Create: `src/WildBunch.Assets/sprites/town-hub-roads/README.md`
- Create: `src/WildBunch.Assets/sprites/town-hub-ground/README.md`
- Modify: `src/WildBunch.Assets/INDEX.md`

**Interfaces:**
- Consumes: the rewritten docs from Task 1
- Produces: a real `source/staging/sprites` custody tree that the rest of the asset work can target

- [ ] **Step 1: Move the existing town-building custody tree**

Re-home the current town-building asset content out of `src/WildBunch.Assets/town-buildings/` and into the new `source/staging/sprites` layout so the source assets stop looking like pipeline scratch.

- [ ] **Step 2: Scaffold the three family roots**

Create the road and ground family roots even if they start empty so later generations have a stable home:
- `town-hub-roads`
- `town-hub-ground`

- [ ] **Step 3: Regenerate the repo mesh**

Run:
```powershell
python scripts/generate_index_mesh.py
python scripts/generate_index_mesh.py --check
```
Expected: the generated `INDEX.md` mesh reflects the new source/staging/sprites layout.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "chore: rehome town-hub asset custody tree"
```

### Task 3: Generate the two filler-building families

**Files:**
- Create and promote:
  - `src/WildBunch.Assets/source/town-hub-buildings/{boomtown,prosperous,poor,destitute}/background-house/{front,profile,rear,front-oblique,rear-oblique}.png`
  - `src/WildBunch.Assets/source/town-hub-buildings/{boomtown,prosperous,poor,destitute}/background-shop/{front,profile,rear,front-oblique,rear-oblique}.png`
  - matching staging outputs under `src/WildBunch.Assets/staging/town-hub-buildings/...`
  - matching sprite outputs under `src/WildBunch.Assets/sprites/town-hub-buildings/...`

**Interfaces:**
- Consumes: the rewritten style bible and the `town-hub-buildings` family scaffold
- Produces: 40 total filler-building images, 20 per family, each with the canonical five-view turnaround and four prosperity tiers

- [ ] **Step 1: Generate the first filler family**

Use the approved style bible prompt language to generate `background-house` across all four prosperity tiers.

If the generator returns a 5-up turnaround sheet for a tier, slice it with:
```powershell
python scripts/image_asset_pipeline.py slice-sheet --input <sheet.png> --out-dir <tier-family-dir> --names front,profile,rear,front-oblique,rear-oblique
```

If the generator returns separate view images, normalize them into the source tree with:
```powershell
python scripts/image_asset_pipeline.py normalize --input <view.png> --out <out.png>
```

The worker must end this step with exactly 20 images for `background-house`: 4 prosperity tiers times 5 views.

- [ ] **Step 2: Generate the second filler family**

Use the same prompt contract for `background-shop`, but keep the silhouette distinct enough that the two families read as different supporting buildings without making either one the dominant town feature.

The worker must end this step with exactly 20 images for `background-shop`: 4 prosperity tiers times 5 views.

- [ ] **Step 3: Make the filler buildings transparently cut and promotion-ready**

Verify the output has clean transparent backgrounds and that the normalized sprites sit cleanly on the 60x50 canvas with the bottom anchor preserved.

- [ ] **Step 4: Copy source into staging**

Run:
```powershell
if (Test-Path src/WildBunch.Assets/staging/town-hub-buildings) { Remove-Item -Recurse -Force src/WildBunch.Assets/staging/town-hub-buildings }
Copy-Item -Recurse src/WildBunch.Assets/source/town-hub-buildings src/WildBunch.Assets/staging/town-hub-buildings
```

- [ ] **Step 5: Promote staging into sprites**

Run:
```powershell
if (Test-Path src/WildBunch.Assets/sprites/town-hub-buildings) { Remove-Item -Recurse -Force src/WildBunch.Assets/sprites/town-hub-buildings }
python scripts/image_asset_pipeline.py promote-sprites --input-root src/WildBunch.Assets/staging/town-hub-buildings --out-root src/WildBunch.Assets/sprites/town-hub-buildings
```

This promotion step is required even if the source tree already contains normalized outputs; the final sprites tree is the shippable home.

- [ ] **Step 6: Commit**

```powershell
git add src/WildBunch.Assets/source/town-hub-buildings src/WildBunch.Assets/staging/town-hub-buildings src/WildBunch.Assets/sprites/town-hub-buildings
git commit -m "feat: add filler-building families for town hubs"
```

### Task 4: Generate the road-network and ground-fill tile families

**Files:**
- Create and promote:
  - `src/WildBunch.Assets/source/town-hub-roads/main-road/{flat-edge-right,flat-edge-left,path-edge-right,path-edge-left,spur-cross-right,spur-cross-left,end-top,end-bottom}.png`
  - `src/WildBunch.Assets/source/town-hub-roads/spur-road/{straight,path-above,end-right,end-left}.png`
  - `src/WildBunch.Assets/source/town-hub-ground/base/{dirt-a,dirt-b,dirt-c}.png`
  - `src/WildBunch.Assets/source/town-hub-ground/props/{dirt-cactus,dirt-tumbleweed,dirt-scrub,dirt-post,dirt-ruts,dirt-trampled,dirt-grass,dirt-rocks}.png`
  - `src/WildBunch.Assets/source/town-hub-ground/landforms/{hill-nw,hill-ne,hill-sw,hill-se}.png`
  - matching staging and sprite outputs under the same family roots

**Interfaces:**
- Consumes: the approved style bible and the new road/ground family roots
- Produces: road and dirt tiles that tessellate on the intended edges and keep the town hub visually alive without prosperity tiers

- [ ] **Step 1: Generate the main-road tile set**

Create the 2-tile-wide main road family first so the edge, path, spur-cross, and end-piece contracts are settled before the spur and dirt tiles are tuned against them.

The worker must end this step with exactly 8 main-road tiles:
- 4 right-side canonical variants: `flat-edge-right`, `path-edge-right`, `spur-cross-right`, `end-bottom`
- 4 mirrored derivatives: `flat-edge-left`, `path-edge-left`, `spur-cross-left`, `end-top`

- [ ] **Step 2: Generate the spur-road tile set**

Create the 1-tile-tall spur family with a clear horizontal read, a building-attachment variant, and mirrored end pieces.

The worker must end this step with exactly 4 spur-road tiles:
- `straight`
- `path-above`
- `end-right`
- `end-left`

- [ ] **Step 3: Generate the ground-fill tile set**

Create the 3 base dirt textures, the 8 prop-baked dirt tiles, and the 4-tile landform set. Keep the dirt edges compatible with the road dirt edges and with each other.

The worker must end this step with exactly 15 ground tiles:
- 3 base dirt textures
- 8 prop dirt tiles
- 4 landform tiles

- [ ] **Step 4: Copy source into staging**

```powershell
if (Test-Path src/WildBunch.Assets/staging/town-hub-roads) { Remove-Item -Recurse -Force src/WildBunch.Assets/staging/town-hub-roads }
if (Test-Path src/WildBunch.Assets/staging/town-hub-ground) { Remove-Item -Recurse -Force src/WildBunch.Assets/staging/town-hub-ground }
Copy-Item -Recurse src/WildBunch.Assets/source/town-hub-roads src/WildBunch.Assets/staging/town-hub-roads
Copy-Item -Recurse src/WildBunch.Assets/source/town-hub-ground src/WildBunch.Assets/staging/town-hub-ground
```

- [ ] **Step 5: Validate tile dimensions and seam-safe mirror pairs**

Run:
```powershell
@'
from pathlib import Path
from PIL import Image, ImageChops, ImageOps

expected_size = (60, 50)
staging_roots = [
    Path("src/WildBunch.Assets/staging/town-hub-roads"),
    Path("src/WildBunch.Assets/staging/town-hub-ground"),
]

mirror_checks = [
    ("src/WildBunch.Assets/staging/town-hub-roads/main-road/flat-edge-right.png", "src/WildBunch.Assets/staging/town-hub-roads/main-road/flat-edge-left.png", "horizontal"),
    ("src/WildBunch.Assets/staging/town-hub-roads/main-road/path-edge-right.png", "src/WildBunch.Assets/staging/town-hub-roads/main-road/path-edge-left.png", "horizontal"),
    ("src/WildBunch.Assets/staging/town-hub-roads/main-road/spur-cross-right.png", "src/WildBunch.Assets/staging/town-hub-roads/main-road/spur-cross-left.png", "horizontal"),
    ("src/WildBunch.Assets/staging/town-hub-roads/main-road/end-bottom.png", "src/WildBunch.Assets/staging/town-hub-roads/main-road/end-top.png", "vertical"),
    ("src/WildBunch.Assets/staging/town-hub-roads/spur-road/end-right.png", "src/WildBunch.Assets/staging/town-hub-roads/spur-road/end-left.png", "horizontal"),
]

failures = []

for root in staging_roots:
    for path in sorted(root.rglob("*.png")):
        with Image.open(path) as image:
            if image.size != expected_size:
                failures.append(f"{path}: expected {expected_size}, got {image.size}")

for canonical_path, mirror_path, axis in mirror_checks:
    with Image.open(canonical_path) as canonical, Image.open(mirror_path) as mirror:
        transformed = ImageOps.mirror(canonical) if axis == "horizontal" else ImageOps.flip(canonical)
        diff = ImageChops.difference(transformed.convert("RGBA"), mirror.convert("RGBA"))
        if diff.getbbox() is not None:
            failures.append(f"{mirror_path}: does not exactly match mirrored {canonical_path}")

if failures:
    raise SystemExit("\n".join(failures))

print("Road and ground staging checks passed")
'@ | python -
```

After the automated check passes, do a visual seam pass on the staged road edges before copying them forward.

- [ ] **Step 6: Promote staging into sprites by copy**

Run:
```powershell
if (Test-Path src/WildBunch.Assets/sprites/town-hub-roads) { Remove-Item -Recurse -Force src/WildBunch.Assets/sprites/town-hub-roads }
if (Test-Path src/WildBunch.Assets/sprites/town-hub-ground) { Remove-Item -Recurse -Force src/WildBunch.Assets/sprites/town-hub-ground }
Copy-Item -Recurse src/WildBunch.Assets/staging/town-hub-roads src/WildBunch.Assets/sprites/town-hub-roads
Copy-Item -Recurse src/WildBunch.Assets/staging/town-hub-ground src/WildBunch.Assets/sprites/town-hub-ground
```

- [ ] **Step 7: Commit**

```powershell
git add src/WildBunch.Assets/source/town-hub-roads src/WildBunch.Assets/staging/town-hub-roads src/WildBunch.Assets/sprites/town-hub-roads src/WildBunch.Assets/source/town-hub-ground src/WildBunch.Assets/staging/town-hub-ground src/WildBunch.Assets/sprites/town-hub-ground
git commit -m "feat: add road and ground tile families for town hubs"
```

### Task 5: Final promotion, mesh refresh, and verification

**Files:**
- Modify: any remaining `INDEX.md` files touched by the new custody tree
- Modify: any doc links that still point at the old `town-buildings/` home after the migration

**Interfaces:**
- Consumes: all generated source/staging/sprites assets from Tasks 2-4
- Produces: a clean final tree, validated index mesh, and a reviewable PR-ready diff

- [ ] **Step 1: Run the final building promotion pass**

Run the shared promotion surface for the building family from staging into sprites so the shippable building tree is current:
```powershell
python scripts/image_asset_pipeline.py promote-sprites --input-root src/WildBunch.Assets/staging/town-hub-buildings --out-root src/WildBunch.Assets/sprites/town-hub-buildings
```

The road and ground families must already be present in `src/WildBunch.Assets/sprites/` from the Task 4 copy promotion path; do not run the sprite cutter over them.

- [ ] **Step 2: Regenerate the index mesh**

Run:
```powershell
python scripts/generate_index_mesh.py
python scripts/generate_index_mesh.py --check
```

- [ ] **Step 3: Run the final repo sanity checks**

Run:
```powershell
git status --short
```
Expected: only the intended plan, doc, and asset changes remain, with no stray scratch files outside the branch-scoped worktree.

- [ ] **Step 4: Commit the final verification state**

```powershell
git add -A
git commit -m "chore: finalize town-hub filler asset pipeline"
```

---

## Self-Review

### Spec coverage

| Spec requirement | Task |
|---|---|
| Rewrite the asset contract docs | Task 1 |
| Use explicit source/staging/sprites custody homes | Task 1, Task 2 |
| Migrate the old `town-buildings` custody tree | Task 2 |
| Add AGENTS/README files at each family root | Task 2 |
| Generate 2 filler-building families | Task 3 |
| Each filler family has 5-turnaround + 4 prosperity tiers | Task 3 |
| Fillers stay visually unobtrusive and supporting | Task 1, Task 3 |
| Generate main-road tiles with mirror/end variants | Task 4 |
| Generate spur-road tiles with path/end variants | Task 4 |
| Generate 3 base dirt textures | Task 4 |
| Generate prop dirt tiles and a landform set | Task 4 |
| Roads/dirt do not get prosperity variants | Task 1, Task 4 |
| Tiles tessellate on the intended edges | Task 4 |
| Promote building assets through `image_asset_pipeline.py`; copy tile assets deterministically | Tasks 3-5 |
| Refresh `INDEX.md` mesh and validate it | Tasks 2 and 5 |

### Placeholder scan

No placeholders remain. The plan uses concrete family names, concrete file paths, and concrete commands.

### Type consistency

- `background-house` and `background-shop` are the two required filler-building families throughout the plan.
- `town-hub-buildings`, `town-hub-roads`, and `town-hub-ground` are the only new asset-family root names used throughout the plan.
- `normalize`, `slice-sheet`, and `promote-sprites` are the only pipeline helper commands used in the plan, and `promote-sprites` is only used for the building family.
- The final validation always uses `python scripts/generate_index_mesh.py --check` after any move or rename.
