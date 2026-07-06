# Poor Prosperity Town-Building Asset Generation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate the missing `poor` prosperity-tier town-building assets, starting with a style-bible rewrite that makes `poor` a clear midpoint between `destitute` and `prosperous`, then produce, cut, and promote the poor-tier sprite set.

**Architecture:** Keep the style guidance, prompt guidance, and asset outputs in one ladder. The human-facing bible and the agent-facing doctrine must agree that `poor` is a bridge tier, not a new silhouette family. The asset generation pass then uses that guidance to produce one consistent five-view turnaround per canonical building family and push the results through the existing image pipeline into `src/WildBunch.Assets/town-buildings/`.

**Tech Stack:** Markdown, `image_gen`, `python scripts/image_asset_pipeline.py`, `python scripts/generate_index_mesh.py`, Git worktrees.

## Global Constraints

- `poor` is a midpoint between `destitute` and `prosperous`.
- Keep the top-down slight oblique camera, pixel-art presentation, 60x50 footprint, and five-view turnaround contract unchanged.
- Keep the four canonical families only: general store, sheriff office, saloon, telegraph office.
- Preserve family identity across all tiers; only maintenance, ornament, and material finish should change by prosperity.
- Keep all working and final asset files under `src/WildBunch.Assets/town-buildings/`.
- Do not move working assets into the web public tree.
- Use the repository asset pipeline for cutting, normalization, promotion, and mesh regeneration.
- Work in the fresh worktree created from current `origin/main`.

---

### Task 1: Rewrite the style bible and doctrine to define the poor-tier midpoint

**Files:**
- Modify: `docs/art/town-buildings/style-bible.md`
- Modify: `.agents/art/town-buildings/DOCTRINE.md`
- Modify: `docs/art/town-buildings/pipeline-overview.md`
- Modify: `docs/art/town-buildings/asset-spec.md` if the tier ladder wording needs a small consistency pass

**Interfaces:**
- Consumes: the inspected boomtown, prosperous, and destitute sheets and the current town-building guidance.
- Produces: the final prose that will steer image generation and keep the poor tier visually aligned with the existing ladder.

- [ ] **Step 1: Replace the tier ladder wording in the human style bible**

Use this text as the core replacement for the shared visual contract:

```md
## Prosperity ladder

The town-building prosperity ladder reads as:

- destitute: roughest end of the ladder, sparse, worn, and incomplete
- poor: midpoint bridge tier, repaired and modest, less ornate than prosperous, more complete than destitute
- prosperous: polished middle-high tier, cleaner and richer than poor
- boomtown: richest and most ornate end of the ladder

`poor` is not a new silhouette family. It keeps the same camera, footprint, and five-view turnaround contract as the rest of the set.
```

Keep the existing camera / footprint / turnaround wording, but make sure `poor` is explicitly called out as the bridge tier.

- [ ] **Step 2: Mirror the same ladder language in the agent doctrine**

Use this text as the core poor-tier prompting rule:

```md
## Prosperity-tier prompt ladder

- Treat `poor` as the midpoint between `destitute` and `prosperous`.
- Keep the same top-down slight oblique camera, 60x50 footprint, and five-view turnaround contract across tiers.
- Preserve the family silhouette and identity; only shift maintenance, ornament, paint, trim, and signage.
- Do not let `poor` drift into a new architecture or a new camera contract.
```

If the doctrine already has a prompt style section, fold this language into that section instead of duplicating it.

- [ ] **Step 3: Align the pipeline overview wording**

Add one short paragraph stating that `poor` is a first-class tier in the same ladder as `destitute`, `prosperous`, and `boomtown`, and that the visual rule is midpoint consistency rather than a separate silhouette family.

- [ ] **Step 4: Verify the prose against the inspected assets**

Read the rewritten docs alongside the visual sheets and confirm the wording matches the observed ladder:

- destitute = roughest
- poor = midpoint bridge
- prosperous = upgraded / polished
- boomtown = richest / most ornate

- [ ] **Step 5: Commit**

```powershell
git add docs/art/town-buildings/style-bible.md .agents/art/town-buildings/DOCTRINE.md docs/art/town-buildings/pipeline-overview.md docs/art/town-buildings/asset-spec.md
git commit -m "docs: define poor prosperity style ladder"
```

**Expected interim state:** no runtime impact. This task only changes guidance text.

---

### Task 2: Generate poor-tier turnaround sheets for the four canonical families

**Files:**
- Create temporary sheets in the worktree-local scratch area, then stage the canonical poor-tier outputs under `src/WildBunch.Assets/town-buildings/_pipeline/poor/`
- Families:
  - `src/WildBunch.Assets/town-buildings/_pipeline/poor/general-store/`
  - `src/WildBunch.Assets/town-buildings/_pipeline/poor/sheriff-office/`
  - `src/WildBunch.Assets/town-buildings/_pipeline/poor/saloon/`
  - `src/WildBunch.Assets/town-buildings/_pipeline/poor/telegraph-office/`

**Interfaces:**
- Consumes: the rewritten style bible and doctrine from Task 1.
- Produces: one five-view poor-tier turnaround sheet per family, ready to be sliced into per-view PNGs.

- [ ] **Step 1: Generate the poor-tier sheet for `general-store`**

Use this prompt shape for the sheet:

```text
Top-down slight oblique pixel-art western general store in the poor prosperity tier. The building should sit midway between destitute and prosperous: repaired and modest, less ornate than prosperous, more complete than destitute. Preserve the family silhouette, roof massing, porch logic, and five-view turnaround contract. Keep the same building read across front, profile, rear, front-oblique, and rear-oblique views. Use pixels, not labels, as the source of truth for which panel is which.
```

Keep the sheet in the same five-view order used by the existing tier sheets.

- [ ] **Step 2: Generate the poor-tier sheet for `sheriff-office`**

Use the same shared prompt shape, with the family-specific cue that the building should read authoritative but modest, not like a store and not like a boomtown showpiece.

- [ ] **Step 3: Generate the poor-tier sheet for `saloon`**

Use the same shared prompt shape, with the family-specific cue that the building should read social and public-facing, but with restrained ornament and fewer upscale details than prosperous.

- [ ] **Step 4: Generate the poor-tier sheet for `telegraph-office`**

Use the same shared prompt shape, with the family-specific cue that the building should read compact, practical, and service-oriented, with plain maintenance rather than rich decoration.

- [ ] **Step 5: Visually inspect each sheet before slicing**

Confirm that the pixels, not the sheet order, establish the panel identity. If a panel is visually wrong, regenerate before slicing.

- [ ] **Step 6: Commit**

```powershell
git add -A .agents/superpowers/output src/WildBunch.Assets/town-buildings/_pipeline/poor
git commit -m "feat: generate poor prosperity turnaround sheets"
```

**Expected interim state:** temporary sheet outputs may exist in the scratch area while the team inspects them, but the canonical repo tree should still only contain the staged poor-tier outputs after this task is done.

---

### Task 3: Slice, normalize, and promote the poor-tier assets into sprites

**Files:**
- Update: `src/WildBunch.Assets/town-buildings/_pipeline/poor/**/front.png`
- Update: `src/WildBunch.Assets/town-buildings/_pipeline/poor/**/profile.png`
- Update: `src/WildBunch.Assets/town-buildings/_pipeline/poor/**/rear.png`
- Update: `src/WildBunch.Assets/town-buildings/_pipeline/poor/**/front-oblique.png`
- Update: `src/WildBunch.Assets/town-buildings/_pipeline/poor/**/rear-oblique.png`
- Create: `src/WildBunch.Assets/town-buildings/sprites/poor/**/front.png`
- Create: `src/WildBunch.Assets/town-buildings/sprites/poor/**/profile.png`
- Create: `src/WildBunch.Assets/town-buildings/sprites/poor/**/rear.png`
- Create: `src/WildBunch.Assets/town-buildings/sprites/poor/**/front-oblique.png`
- Create: `src/WildBunch.Assets/town-buildings/sprites/poor/**/rear-oblique.png`

**Interfaces:**
- Consumes: the staged poor-tier sheets from Task 2.
- Produces: transparent poor-tier pipeline cutouts and the promoted sprite tree.

- [ ] **Step 1: Slice each poor-tier sheet with the repo helper**

Use the repository script so the per-view files land in the canonical names:

```powershell
py -3 scripts/image_asset_pipeline.py slice-sheet --input <sheet.png> --out-dir src/WildBunch.Assets/town-buildings/_pipeline/poor/<family> --names front,profile,rear,front-oblique,rear-oblique
```

- [ ] **Step 2: Promote the poor-tier assets into `sprites/`**

Run the existing promotion path after the poor-tier staging tree is complete:

```powershell
py -3 scripts/image_asset_pipeline.py promote-sprites --input-root src/WildBunch.Assets/town-buildings/_pipeline --out-root src/WildBunch.Assets/town-buildings/sprites
```

- [ ] **Step 3: Check alpha and layout on the promoted poor-tier files**

Verify that the promoted poor-tier PNGs are transparent cutouts with the same folder layout as the other tiers.

- [ ] **Step 4: Commit**

```powershell
git add src/WildBunch.Assets/town-buildings/_pipeline/poor src/WildBunch.Assets/town-buildings/sprites/poor
git commit -m "feat: promote poor prosperity town-building sprites"
```

**Expected interim state:** `_pipeline/poor/` and `sprites/poor/` both exist, and the promoted sprite tree mirrors the staged layout.

---

### Task 4: Regenerate the mesh and validate the whole poor-tier ladder

**Files:**
- Regenerate: `docs/INDEX.md`
- Regenerate: `docs/art/INDEX.md`
- Regenerate: `docs/art/town-buildings/INDEX.md`
- Regenerate: `.agents/INDEX.md`
- Regenerate: `.agents/art/INDEX.md`
- Regenerate: `.agents/art/town-buildings/INDEX.md`
- Regenerate: `src/WildBunch.Assets/INDEX.md`
- Regenerate: `src/WildBunch.Assets/town-buildings/INDEX.md`
- Regenerate: `src/WildBunch.Assets/town-buildings/_pipeline/INDEX.md`
- Regenerate: `src/WildBunch.Assets/town-buildings/sprites/INDEX.md`
- Regenerate: any other affected `INDEX.md` files reported by the generator

**Interfaces:**
- Consumes: all docs and image outputs from Tasks 1 to 3.
- Produces: a clean repo mesh and a validated poor-tier asset ladder.

- [ ] **Step 1: Regenerate the index mesh**

Run from the repo root:

```powershell
py -3 scripts/generate_index_mesh.py
```

- [ ] **Step 2: Validate the mesh**

Run from the repo root:

```powershell
py -3 scripts/generate_index_mesh.py --check
```

- [ ] **Step 3: Perform the final visual sanity pass**

Open the poor-tier outputs alongside the inspected `destitute` and `prosperous` sheets and confirm the poor tier reads as the midpoint bridge.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "chore: finalize poor prosperity town-building assets"
```

**Expected interim state:** none. The final tree should be clean, mesh-current, and ready for PR publication.

---

## Self-Review

### Spec coverage

| Spec requirement | Task |
|---|---|
| Poor is a midpoint between destitute and prosperous | Task 1 |
| Style bible / doctrine rewrite | Task 1 |
| Generate poor-tier turnaround sheets | Task 2 |
| Slice and promote poor assets | Task 3 |
| Regenerate mesh and validate | Task 4 |

### Placeholder scan

No TBDs, TODOs, or vague implementation phrases remain in the plan tasks.

### Type consistency

The file targets, commands, and folder names are consistent across tasks:

- `poor` is always the new tier name
- the four canonical families stay the same
- `_pipeline/poor/` is the staging home
- `sprites/poor/` is the promoted home

### Confidence rating

- **Direct execution confidence:** 8/10
- **SDD confidence:** 7/10

### Gap closure summary

The main design gap was how to keep `poor` coherent without inventing a new silhouette family. The plan closes that gap by rewriting the style bible first, then generating the poor-tier sheets against that midpoint contract, then slicing and promoting through the existing asset pipeline.

### Open questions

None remain. The midpoint decision is fixed: `poor` sits between `destitute` and `prosperous`.
