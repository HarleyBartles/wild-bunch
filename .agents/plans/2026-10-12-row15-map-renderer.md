# Row 15 Shared World Map Renderer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep the one shared pointer-operated world map legible at narrow widths and update its rendered state without tearing down the Phaser game whenever React-owned selection or eligibility changes.

**Architecture:** React remains the owner of map data, selected destination, and legal selectable-town IDs; Phaser remains a renderer and pointer-intent adapter. Keep one Phaser game for the host lifetime, synchronize current React props into its scene, redraw when data or the Phaser viewport changes, and let Phaser resize its canvas to the responsive host. Keep desktop map proportions and give narrow screens enough canvas height to retain readable map labels and town targets.

**Tech Stack:** React, TypeScript, Phaser 3, styled-components, Vitest/Testing Library, the existing web command-bus lane.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, section E and the Web resources and interactions proof family; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 15.

**Execution Strategy:** `executing-plans`. The renderer, React prop synchronization, resize handling, and their behavior proof share one Phaser lifecycle and must be implemented and reviewed as one coupled slice.

## Global Constraints

- Preserve one `WorldMapScene` and one `PhaserMapHost` for opening town selection and later travel, with each caller's existing legal eligible-town set.
- Keep React authoritative for selection state and Phaser limited to displaying supplied data and returning pointer intent; keep server-owned legality unchanged.
- Retain pointer-only map town selection for 0.1.0 under ADR-0046. Do not add map-level keyboard or screen-reader town controls.
- Preserve town names, trails, journey-day labels, selected/current-town distinction, non-selectable appearance, and existing callback semantics.
- Preserve the 800-by-500 desktop map proportion. At viewport widths of 520px or less, keep the canvas within its parent, give it a square viewport, keep text and town targets at readable CSS-pixel sizes rather than shrinking the entire desktop canvas, and place map labels with visible separation so they remain legible on clustered maps.
- Replace source-shape tests with behavior coverage. Do not assert exact class names, CSS strings, constructor counts by themselves, or Phaser config spelling as the only proof.
- This slice changes no game capability, product promise, map legality, or durable architecture decision. The PR author and reviewer must independently compare ADR-0035/0039/0046 and `docs/features.md` and state that those records remain accurate.
- Publish one PR to `develop`; retire the completed `.96` plan in this successor PR; advance the single authoritative version in `Directory.Build.props` from `0.1.0-dev.96` to `0.1.0-dev.97`.
- After the PR is merged and hosted checks pass, fast-forward the main checkout, remove this plan worktree with the canonical helper, then remove and verify the matching local and upstream branch refs.

## Review Focus

- After a React selection update, the visible selected marker and pointer callback use the latest selection without losing the map or its renderer state.
- After travel eligibility changes, towns that became illegal cannot emit selection intent and newly eligible destinations can; the same scene displays the latest current-town and eligibility state.
- A real viewport resize redraws town/trail geometry inside the current canvas bounds, and a narrow viewport keeps town and trail labels inside the canvas without collisions while preserving pointer targets.
- The renderer remains API-free and cannot independently accept a town or write game state.

---

## Tasks

### Task 1: Synchronize React map state with the live Phaser scene

**Files:**
- Modify: `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`
- Modify: `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`

**Interfaces:**
- Consumes: the existing `PhaserMapHostProps` contract and `WorldMapScene.selectTown` intent callback.
- Produces: a scene update operation accepting current map data, selected town ID, current town ID, and nullable selectable town IDs; React prop changes update the live scene's rendered state and eligibility.

- [x] Add a behavior test that mounts the map, changes the selected town through the parent props, and proves the selected marker updates while the previous selection clears.
- [x] Add a behavior test that changes travel eligibility after mount, then proves a formerly legal town emits no intent and a newly legal town emits exactly its own ID.
- [x] Run the focused test and confirm both new tests fail against the constructor-captured scene state.
- [x] Update the scene's render state and redraw its Phaser-owned display objects when those props change, while keeping the latest callback in a React ref and preserving the host's Phaser instance until unmount.
- [x] Run the focused test again and confirm selected-marker, legal-selection, and unmount behavior pass; remove the redundant constructor-count-only test.

### Task 2: Resize the map to its responsive host and redraw geometry

**Files:**
- Modify: `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`
- Modify: `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`

**Interfaces:**
- Consumes: Task 1's live scene render-state operation.
- Produces: viewport-resize handling that redraws town and trail geometry against current Phaser scale dimensions; desktop and narrow viewport canvas sizing remain owned by `MapCanvas`.

- [x] Add a behavior test using a seven-town clustered map that resizes from an 800-by-500 viewport to a 320-by-320 viewport and proves town targets and labels remain in bounds, labels retain readable size and spacing, trails still connect their towns, and pointer selection still returns the selected town ID.
- [x] Temporarily remove the resize-event subscription, run the focused map-host test, and confirm the resize scenario fails because town targets remain laid out for the previous dimensions; restore the subscription.
- [x] Configure Phaser to resize with its parent, redraw map geometry on the Phaser resize event, and give `MapCanvas` a square aspect ratio at viewport widths of 520px or less while retaining the current 8:5 desktop ratio.
- [x] Run the focused map-host test and confirm resize, selection and narrow-label behavior passes without adding map keyboard/screen-reader selection.
- [x] Ensure this worktree's PostgreSQL service and dev-server helper; inspect the existing active playtest without archiving it or creating another game.
- [x] Verify the current playtest map at 375px and desktop width, including canvas bounds, readable labels and map visibility. Focused interaction tests prove pointer eligibility and selection; no travel command was submitted. Confirm the map API request returned 200 and the current browser console had no errors, then stop only this worktree's servers.

### Task 3: Retire the predecessor plan and publish the bounded slice

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-12-row15-shared-display-contracts.md`
- Modify: `Directory.Build.props`
- Keep through this PR: `.agents/plans/2026-10-12-row15-map-renderer.md`

- [x] Update row 15 with the merged PR #281 history, its merge identity, hosted gate results, and verified paired worktree/branch cleanup; make this map-renderer plan the active row-15 plan and leave town-hub renderer work as a remaining row-15 exit.
- [x] Retire the completed `.96` predecessor plan and advance `Directory.Build.props` to `0.1.0-dev.97`.
- [x] Run the focused web tests, changed-file format and lint checks, typecheck, `git diff --check`, inspect the complete diff, verify roadmap links and confirm the generated web identity is `0.1.0-dev.97`.
- [ ] Stage the intended files and commit normally; the installed hook runs the canonical fail-fast check on the staged snapshot without changing or staging files. Repair any reported failure, then retry the normal commit.
- [ ] Complete the author's ADR and feature-matrix comparisons in the PR body, obtain a fresh whole-branch review, publish the PR, and verify its head and exact-head hosted gate before requesting merge.
- [ ] After merge, verify the exact PR head and merge commit, wait for the hosted `develop` gate to pass, fast-forward `Z:\wild-bunch`, then remove this worktree before deleting and verifying its matching local and upstream branch refs.

**Validation:** `npm --prefix src/WildBunch.Web test -- src/tests/PhaserMapHost.test.tsx` (11 tests passed); `npm --prefix src/WildBunch.Web run typecheck`; manual browser viewport evidence under `.agents/playbooks/ui-browser-check.md` plus automated pointer eligibility and selection behavior; changed-file format/lint checks; `git diff --check`; generated web identity check; normal staged-snapshot commit hook, fresh whole-branch review, exact-head hosted CI and develop-push CI remain.
