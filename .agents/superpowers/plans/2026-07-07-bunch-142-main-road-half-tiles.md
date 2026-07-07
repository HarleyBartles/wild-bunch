# BUNCH-142 Main Road Half-Tiles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep the canonical main-road source custody aligned with the three existing tile files, remove stale left-hand/end-cap custody from this slice, and rewrite the road guidance docs so image-generation agents can copy reusable `Do` / `Do not` guardrails directly.

**Architecture:** This is an asset-custody and documentation slice, not a gameplay or placement slice. The source tree keeps the canonical road halves only; staging and sprites stay aligned to that canonical set and may only materialize mirror companions if a later pipeline step explicitly needs them. The docs become the durable contract: the asset spec owns the file set, the pipeline overview owns the promotion story, the style bible owns the prompt blocks, and the doctrine gives the terse agent-facing version.

**Tech Stack:** Python 3, Pillow, the repo-local asset pipeline helper, generated index mesh, Markdown docs.

## Global Constraints

- Road source custody is canonical right-hand half tiles only: `flat-edge.png`, `spur-edge.png`, and `path-edge.png`.
- The road slice uses a full `80x50` canvas.
- The main road runs north-to-south with no visible cap ends in this slice.
- Rotation-based road tiling is out of scope for this slice.
- Prompt guidance must include composable positive and negative `Do` / `Do not` blocks for road generation.

---

### Task 1: Keep the main-road custody set canonical

**Files:**
- Modify or delete generated PNGs under `src/WildBunch.Assets/source/town-hub-roads/main-road/`
- Modify or delete generated PNGs under `src/WildBunch.Assets/staging/town-hub-roads/main-road/`
- Modify or delete generated PNGs under `src/WildBunch.Assets/sprites/town-hub-roads/main-road/`

**Interfaces:**
- Consumes: the existing source custody layout in `src/WildBunch.Assets/source/town-hub-roads/main-road/`
- Produces: only the canonical road-half source files and their derived staging/sprites copies

- [ ] **Step 1: Write the failing validation command**

```powershell
@'
from pathlib import Path
from PIL import Image

root = Path(r"src/WildBunch.Assets/source/town-hub-roads/main-road")
expected = {
    "flat-edge.png",
    "spur-edge.png",
    "path-edge.png",
}

actual = {p.name for p in root.glob("*.png")}
assert actual == expected, (sorted(actual), sorted(expected))

for p in root.glob("*.png"):
    assert Image.open(p).size == (80, 50), p
'@ | python -
```

Expected: fail until the source custody only contains the canonical three files.

- [ ] **Step 2: Keep the canonical source set aligned**

If any stale road-half files remain, remove or de-canonize them so the source tree contains only these three files:

```text
flat-edge.png
spur-edge.png
path-edge.png
```

Do not reintroduce cap ends or rotation-based road handling.

- [ ] **Step 3: Verify the road assets stay promoted correctly**

Run:

```powershell
python scripts/image_asset_pipeline.py stage-tiles --input-root src/WildBunch.Assets/source/town-hub-roads --out-root src/WildBunch.Assets/staging/town-hub-roads --canvas-width 80 --canvas-height 50
python scripts/image_asset_pipeline.py promote-tiles --input-root src/WildBunch.Assets/staging/town-hub-roads --out-root src/WildBunch.Assets/sprites/town-hub-roads
```

Expected: the road track remains seam-safe, and the three canonical main-road files exist in source, staging, and sprites at `80x50`.

- [ ] **Step 4: Verify the mirror behavior and file set**

Run:

```powershell
@'
from pathlib import Path
from PIL import Image

root = Path(r"src/WildBunch.Assets/source/town-hub-roads/main-road")
image = Image.open(root / "flat-edge.png").convert("RGBA")
mirror = image.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
combo = Image.new("RGBA", (160, 50), (0, 0, 0, 0))
combo.paste(mirror, (0, 0))
combo.paste(image, (80, 0))
assert combo.size == (160, 50)
assert combo.crop((0, 0, 8, 50)).getbbox() is not None
assert combo.crop((76, 0, 84, 50)).getbbox() is not None
assert combo.crop((152, 0, 160, 50)).getbbox() is not None
'@ | python -
```

Then confirm the source/staging/sprites main-road directories contain only the intended three canonical files.

- [ ] **Step 5: Commit**

```powershell
git add src/WildBunch.Assets/source/town-hub-roads src/WildBunch.Assets/staging/town-hub-roads src/WildBunch.Assets/sprites/town-hub-roads
git commit -m "feat: canonicalize main road half tiles"
```

### Task 2: Rewrite the road guidance docs with composable prompt blocks

**Files:**
- Modify: `docs/art/town-buildings/style-bible.md`
- Modify: `docs/art/town-buildings/asset-spec.md`
- Modify: `docs/art/town-buildings/pipeline-overview.md`
- Modify: `.agents/art/town-buildings/DOCTRINE.md`

**Interfaces:**
- Consumes: the canonical road contract from Task 1
- Produces: copyable road prompt blocks and matching repo-facing contract text

- [ ] **Step 1: Write the failing doc-content check**

```powershell
rg -n "flat-edge|spur-edge|path-edge|Do: Keep the image as a single vertical main-road half tile|Do not: Do not add cap ends" docs/art/town-buildings .agents/art/town-buildings
```

Expected: fail until the road blocks exist in the docs.

- [ ] **Step 2: Update `docs/art/town-buildings/style-bible.md`**

Add a shared road block and three variant blocks. Use copyable positive and negative paragraphs like these:

```markdown
- Do: Keep the image as a single vertical main-road half tile at 80x50, with road on the left edge, dirt/spur/path on the right edge, and clean top/bottom seams for vertical repetition.
- Do not: Do not add cap ends, rotation-only tiling, prosperity language, or any ground/building content to the road prompt.

- Do: `flat-edge` is the canonical straight main-road half tile: road seam on the left, dirt on the right, seam-safe top and bottom edges, and a right-hand dirt edge that mirrors cleanly.
- Do not: Do not add a cap end, a spur junction, or a path lead-in to the flat road tile.

- Do: `spur-edge` is the canonical main-road half with a right-edge spur junction: road seam on the left, spur mouth on the right, seam-safe top and bottom edges, and a mirrored partner that keeps the road band intact.
- Do not: Do not turn the spur-cross tile into an end cap, a full intersection scene, or a rotated road tile.

- Do: `path-edge` is the canonical main-road half with a right-edge thin path connector: road seam on the left, path lead-in on the right, seam-safe top and bottom edges, and a mirrored partner that keeps the road band intact.
- Do not: Do not turn the path tile into a cap end or a full road junction.
```

- [ ] **Step 3: Update `docs/art/town-buildings/asset-spec.md` and `docs/art/town-buildings/pipeline-overview.md`**

Replace the current road text so it says:

- the road track is the main-road half-tile set
- the canonical custody files are the three right-hand files from Task 1
- the canvas is `80x50`
- there are no cap ends in this slice
- road variation comes from mirror-safe topology, not rotation or prosperity

Remove wording that implies road end pieces, left-hand canonical files, or a rotation-based road contract.

- [ ] **Step 4: Update `.agents/art/town-buildings/DOCTRINE.md`**

Replace the old road wording with the condensed agent-facing version of the same contract:

- canonical right-hand custody set
- `80x50` canvas
- no cap ends
- no rotation
- mirror-safe road seam on the left edge
- right-edge variants for flat dirt, spur junction, and path connector

- [ ] **Step 5: Commit**

```powershell
git add docs/art/town-buildings/style-bible.md docs/art/town-buildings/asset-spec.md docs/art/town-buildings/pipeline-overview.md .agents/art/town-buildings/DOCTRINE.md
git commit -m "docs: tighten main road tile guardrails"
```

### Task 3: Refresh the mesh and run the final validation pass

**Files:**
- Modify: `.agents/superpowers/plans/INDEX.md` if the new plan file needs mesh refresh
- Modify: any generated index files touched by `scripts/generate_index_mesh.py`

**Interfaces:**
- Consumes: the updated road assets and docs from Tasks 1 and 2
- Produces: a clean mesh and proof that the road slice is consistent end-to-end

- [ ] **Step 1: Regenerate the index mesh**

Run:

```powershell
python scripts/generate_index_mesh.py
python scripts/generate_index_mesh.py --check
git diff --check
```

Expected: `--check` passes and there are no whitespace or mesh regressions.

- [ ] **Step 2: Run the final road-custody verification**

Run:

```powershell
@'
from pathlib import Path

roots = [
    Path(r"src/WildBunch.Assets/source/town-hub-roads/main-road"),
    Path(r"src/WildBunch.Assets/staging/town-hub-roads/main-road"),
    Path(r"src/WildBunch.Assets/sprites/town-hub-roads/main-road"),
]
expected = {
    "flat-edge.png",
    "spur-edge.png",
    "path-edge.png",
}
for root in roots:
    actual = {p.name for p in root.glob("*.png")}
    assert actual == expected, (root, sorted(actual), sorted(expected))
'@ | python -
```

Expected: every road-main folder contains only the three canonical files.

## Execution Confidence Assessment

- Confidence rating: **8.6/10**
- Direct execution confidence: **8.8/10**
- SDD confidence: **8.4/10**

## Gap Closure Summary

- The custody rule is now explicit: the three right-hand files are canonical source custody, and any mirrored left-hand files are derived only if the pipeline ever needs them.
- The cap-end ambiguity is closed by naming cap ends out of scope for this slice and by telling the docs to remove cap-end language.
- The docs task now has concrete prompt blocks instead of vague prose, so image-generation agents can copy the guardrails directly.

## Open Questions

- None. The remaining work is implementation, regeneration, and validation.
