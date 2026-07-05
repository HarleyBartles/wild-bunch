# BUNCH-138: Town Building Asset Pipeline Docs and Mesh Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire the town-building art workflow into the repo mesh so human docs, agent doctrine, and asset-subtree pointers all point at the same canonical guidance and asset homes.

**Architecture:** Keep the split surface explicit. Human-facing truth lives in `docs/art/town-buildings/`, agent-facing doctrine lives in `.agents/art/town-buildings/`, and the operational pointer lives in `src/WildBunch.Web/public/assets/town-buildings/AGENTS.md` where asset work actually happens. Final sprites ship from `src/WildBunch.Web/public/assets/town-buildings/sprites/`; pipeline intermediates land in `src/WildBunch.Web/public/assets/town-buildings/_pipeline/`. Generated `INDEX.md` files remain generated only.

**Tech Stack:** Markdown, PowerShell, `scripts/generate_index_mesh.py`, git worktree hygiene.

## Global Constraints

- `AGENTS.md` files are routing files only. They point at doctrine; they do not carry doctrine.
- `INDEX.md` files are generated only. Never hand-edit them.
- Human-facing guidance stays in `docs/`.
- Agent-facing guidance stays in `.agents/`.
- Final shipped sprites live under `src/WildBunch.Web/public/assets/town-buildings/sprites/`.
- Pipeline intermediates live under `src/WildBunch.Web/public/assets/town-buildings/_pipeline/`.
- Keep the current town-building art contract intact: top-down slight oblique camera, pixel-art style, 60x50 footprint normalisation, and the 5-view turnaround semantics already established in chat.
- Work in the isolated worktree based on current `origin/main`. Do not mutate the main checkout.
- Do not broaden scope beyond town-building art docs, mesh pointers, and the lawful asset homes needed for the pipeline.

---

### Task 1: Split the canonical human-facing town-building docs into `docs/art/town-buildings/`

**Files:**
- Create: `docs/art/town-buildings/style-bible.md`
- Create: `docs/art/town-buildings/asset-spec.md`
- Create: `docs/art/town-buildings/pipeline-overview.md`
- Generated: `docs/art/INDEX.md`
- Generated: `docs/art/town-buildings/INDEX.md`
- Retire: `.agents/superpowers/specs/2026-07-05-town-building-prosperity-asset-specs.md`

**Interfaces:**
- Consumes: the town-building rules already agreed in chat
- Produces: human-facing reference docs that define the visual contract, asset naming, folder intent, and pipeline overview

- [x] **Step 1: Write `style-bible.md`**

Capture the canonical visual rules for the town-building set:

- top-down, slightly oblique camera
- pixel-art presentation, not painted concept art
- 60x50 normalised footprint
- 5-view turnaround contract
- asymmetry allowed for doors/windows so each town can reuse a building with different side details
- current master set limited to the four active building types already settled in chat: general store, sheriff office, saloon, telegraph office
- prosperity tiers are deferred but the style bible should leave room for them

- [x] **Step 2: Write `asset-spec.md`**

Document the repo-facing asset contract:

- where final sprites will live
- where pipeline intermediates will live
- how files should be named
- which assets are considered source references versus shippable output
- what a worker should check before promoting an image from `_pipeline/` to `sprites/`

- [x] **Step 3: Write `pipeline-overview.md`**

Keep this human-facing and short:

- what the pipeline is for
- what is expected to be tracked in git
- what should never be treated as final art
- how human reviewers should inspect the generated outputs

- [x] **Step 4: Retire the scratch spec file**

Once the canonical docs exist, remove the temporary `.agents/superpowers/specs/...` draft so the repo stops advertising the wrong home for durable guidance.

```powershell
Remove-Item .agents/superpowers/specs/2026-07-05-town-building-prosperity-asset-specs.md
```

- [x] **Step 5: Run the mesh generator**

From the repo root:

```powershell
python scripts/generate_index_mesh.py
```

Expected: `docs/art/INDEX.md` and `docs/art/town-buildings/INDEX.md` appear in the generated mesh.

- [x] **Step 6: Commit**

```powershell
git add -A
git commit -m "docs: add town building style bible and asset spec"
```

**Expected interim state:** none. This task is doc-only and should stay green throughout.

---

### Task 2: Create the agent-facing doctrine in `.agents/art/town-buildings/`

**Files:**
- Create: `.agents/art/town-buildings/DOCTRINE.md`
- Generated: `.agents/art/INDEX.md`
- Generated: `.agents/art/town-buildings/INDEX.md`

**Interfaces:**
- Consumes: the human-facing style bible from Task 1
- Produces: agent-only generation rules, reference-image control rules, and prompt constraints for town-building sprite work

- [x] **Step 1: Write `DOCTRINE.md`**

Keep this file agent-facing and operational. It should define:

- how to choose or reject reference images
- how to keep the camera contract from drifting toward a front-facing view
- how to describe the 45-degree turnaround pair correctly
- how to preserve the pixel-art style in prompts
- how to distinguish master fronts from side/back/diagonal turnarounds
- how to encode building-specific cues so the sheriff office does not collapse into the store read
- how to request retries when an image lands with the wrong facing or wrong oblique angle

- [x] **Step 2: Keep the doctrine linked to the human style bible**

The agent doctrine should point back to the human docs for canonical vocabulary and use the same building names and footprint terms. It should not restate the whole style bible.

- [x] **Step 3: Run the mesh generator**

From the repo root:

```powershell
python scripts/generate_index_mesh.py
```

Expected: `.agents/art/INDEX.md` and `.agents/art/town-buildings/INDEX.md` appear in the generated mesh.

- [x] **Step 4: Commit**

```powershell
git add -A
git commit -m "docs: add town building agent doctrine"
```

**Expected interim state:** none. This task is doc-only and should stay green throughout.

---

### Task 3: Add the local asset-subtree pointer and durable asset homes

**Files:**
- Create: `src/WildBunch.Web/public/assets/town-buildings/AGENTS.md`
- Create: `src/WildBunch.Web/public/assets/town-buildings/README.md`
- Create: `src/WildBunch.Web/public/assets/town-buildings/sprites/.gitkeep`
- Create: `src/WildBunch.Web/public/assets/town-buildings/_pipeline/.gitkeep`

**Interfaces:**
- Consumes: the canonical docs from Tasks 1 and 2
- Produces: the local pointer file that agents will read when working inside the asset tree, plus tracked homes for final sprites and pipeline intermediates

- [x] **Step 1: Write the asset-subtree `AGENTS.md`**

Make it a routing file only. It should point to:

- `docs/art/town-buildings/style-bible.md`
- `docs/art/town-buildings/asset-spec.md`
- `.agents/art/town-buildings/DOCTRINE.md`

The file should explicitly say that anyone editing town-building assets must read those docs before generating or promoting art.

- [x] **Step 2: Write the human `README.md`**

Keep this short and discoverable. It should explain:

- `sprites/` is the shipped asset home
- `_pipeline/` is the intermediate home
- the asset subtree is intentionally split between human guidance and agent guidance

- [x] **Step 3: Create the tracked asset homes**

Add placeholder files so git keeps the folders alive even before the first asset drop:

- `src/WildBunch.Web/public/assets/town-buildings/sprites/.gitkeep`
- `src/WildBunch.Web/public/assets/town-buildings/_pipeline/.gitkeep`

If the first real asset pass lands during this work, replace the placeholders with the actual generated images instead of leaving the keeps behind.

- [x] **Step 4: Commit**

```powershell
git add -A
git commit -m "docs: add town building asset subtree pointers and homes"
```

**Expected interim state:** none. This task should not affect runtime behavior.

---

### Task 4: Regenerate the mesh and validate discoverability

**Files:**
- Regenerate: `docs/INDEX.md`
- Regenerate: `.agents/INDEX.md`
- Regenerate: `src/WildBunch.Web/public/assets/town-buildings/INDEX.md`
- Regenerate: any other affected `INDEX.md` files reported by the generator

**Interfaces:**
- Consumes: all files created in Tasks 1 to 3
- Produces: a repo-wide navigation mesh that exposes the new art docs and the asset subtree pointer

- [x] **Step 1: Regenerate the full index mesh**

From the repo root:

```powershell
python scripts/generate_index_mesh.py
```

- [x] **Step 2: Validate the mesh**

From the repo root:

```powershell
python scripts/generate_index_mesh.py --validate
rg -n "town-buildings|art" .agents/INDEX.md docs/INDEX.md src/WildBunch.Web/public/assets/town-buildings/AGENTS.md
git status --short
```

Expected:

- the new `art` mesh branch is present in both `docs/` and `.agents/`
- the asset subtree `AGENTS.md` points to the correct docs
- there are no unexpected files outside the planned doc, mesh, and asset-home changes

- [x] **Step 3: Commit**

```powershell
git add -A
git commit -m "chore: regenerate mesh for town building art docs"
```

**Expected interim state:** none. The only expected changes are the planned docs, routing, placeholder asset homes, and generated index updates.

---

## Self-Review

### Spec coverage

| Issue requirement | Task |
|---|---|
| Human-facing style/spec docs | Task 1 |
| Agent-facing docs and generation rules | Task 2 |
| Local pointer where asset workers actually read | Task 3 |
| Lawful homes for final sprites and intermediates | Task 3 |
| Mesh discoverability for new art docs | Task 4 |
| Retire the wrong-home scratch spec | Task 1 |

### Placeholder scan

No prose placeholders remain. The tracked `.gitkeep` files are intentional asset homes, not leftover draft placeholders. File paths, commands, and validation steps are explicit.

### Type and path consistency

- `docs/art/town-buildings/` is the human-facing branch point
- `.agents/art/town-buildings/` is the agent-facing branch point
- `src/WildBunch.Web/public/assets/town-buildings/` is the operational asset subtree
- `sprites/` is the final output home
- `_pipeline/` is the intermediate home
- `scripts/generate_index_mesh.py` is the only index-mesh writer

### Confidence rating

- **Direct execution confidence:** 8/10
- **SDD confidence:** 7/10

### Gap closure summary

The main gap was where the guidance should live. That is closed by splitting human docs, agent doctrine, and the asset-subtree pointer into separate surfaces and by making the generated mesh expose all three.

### Open questions

None. The remaining work is execution, not design.

