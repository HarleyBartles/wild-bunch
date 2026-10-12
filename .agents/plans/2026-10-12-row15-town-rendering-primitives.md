# Row 15 Town Rendering Primitives Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Share the town renderer's duplicated grid and seeded-choice primitives while preserving existing coordinates, asset resolution, and salt-derived visual results.

**Architecture:** Keep rendering behavior inside the town-hub capability and extract only the grid geometry and stable seed/hash functions already used by its scene, background planner, and ground renderer. Reuse one prosperity-directory mapping for foreground and background sprite assets; do not introduce a generic Phaser framework or change town layout ownership.

**Tech Stack:** React, TypeScript, Phaser 3, Vitest, the existing web command-bus lane.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, Town-service scope and developer salt-control direction; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 15; `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, WB-16.

**Execution Strategy:** `executing-plans`. This is one bounded extraction across tightly coupled town renderer consumers; preserving seeded output requires a single test-first pass across the shared helper and its planner/scene/asset callers, while splitting it would repeat the same baseline proof and review surface.

## Global Constraints

- Preserve town identity, geography, layout determinism and prosperity variation.
- Keep rendering-derived visual choices deterministic from the recorded layout salts; do not reroll, alter gameplay state, or change server/event ownership.
- Keep the fixed town tile grid 10 columns by 10 rows, logical-to-cell flooring by ten, and missing tile-cell fallback to zero.
- Preserve existing foreground/background sprite URLs, keys, and all visual output for the same layout and salts.
- Keep current Phaser host teardown, latest callback, map-selection and keyboard behavior unchanged; PR #282 already delivered live world-map update semantics.
- Compare applicable decision records and PG-003 with the actual diff; record why no durable decision or feature-matrix assessment changes.
- In the first substantive commit, retire the completed `.98` predecessor plan, record PR #283 merge/check/paired-ref cleanup evidence in row 15, and advance the single authored product version from `0.1.0-dev.98` to `0.1.0-dev.99`.
- Deliver one PR to `develop`; after exact-head hosted and develop-push checks pass, fast-forward the primary checkout, remove this worktree first, then remove and verify its matching local/upstream branch refs.

## Review Focus

- Fixed salt and coordinate inputs retain their current dirt variant, mirroring, prop eligibility/kind/offset, background placement, and prosperity-specific sprite output; tests fail when a seeded output or coordinate boundary changes.
- Planner and scene use the same 10-by-10 grid, logical-coordinate conversion, and missing-cell fallback without changing current rendering.
- The extraction does not widen into a renderer framework, new town behavior, town-layout generation changes, or player-visible capability claims.

---

## Tasks

### Task 1: Lock seeded rendering and grid behavior before extraction

**Files:**
- Modify: `src/WildBunch.Web/src/components/town-hub/ground-loader.test.ts`
- Modify: `src/WildBunch.Web/src/components/town-hub/background-building-planner.test.ts`
- Create: `src/WildBunch.Web/src/components/town-hub/town-hub-geometry.test.ts`

**Interfaces:**
- Consumes: current ground-loader and planner functions with their existing seed, coordinate, and DTO inputs.
- Produces: meaningful behavior assertions for stable seeded visual outputs, the exact logical-cell boundary, and missing-cell fallback; no assertions about private file shape or duplicated function presence.

- [ ] Replace repeated-call equality tests for dirt choice, mirroring, prop kind and placement with concrete current outputs: `dirt-salt` at row 1/column 2 gives variant index 1 and `{ flipX: true, flipY: true }`; `prop-salt` at row 4/column 5 gives `cactus` and `{ offsetX: 2, offsetY: 7, scale: 0.6 }`.
- [ ] Replace the blocked-prop-only check with a real eligible cell and its blocked form: `shouldPlaceProp("prop-salt", 5, 4)` is true, and `shouldPlaceProp("prop-salt", 5, 4, true)` is false.
- [ ] Add a planner behavior case proving logical coordinates exactly on `x=20`, `y=30` map to row 3/column 2 under floor-by-ten semantics.
- [ ] Add geometry behavior cases proving the first cell, the 10-unit boundary, the last supported row/column, and a missing tile grid return the current cell/fallback results.
- [ ] Run `npm --prefix src/WildBunch.Web exec -- vitest run src/components/town-hub/ground-loader.test.ts src/components/town-hub/background-building-planner.test.ts src/tests/TownHubScene.tiles.test.ts` against the untouched implementation and confirm the characterization assertions pass; temporarily change the ground-loader FNV multiplier and the planner's coordinate divisor separately, confirm each relevant test fails, then restore source before continuing.

### Task 2: Extract the shared town geometry, seed and prosperity mappings

**Files:**
- Create: `src/WildBunch.Web/src/components/town-hub/town-hub-geometry.ts`
- Create: `src/WildBunch.Web/src/components/town-hub/town-hub-seed.ts`
- Modify: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`
- Modify: `src/WildBunch.Web/src/components/town-hub/background-building-planner.ts`
- Modify: `src/WildBunch.Web/src/components/town-hub/ground-loader.ts`
- Modify: `src/WildBunch.Web/src/components/town-hub/sprite-loader.ts`

**Interfaces:**
- Consumes: `TownLayoutDto` and logical X/Y coordinates; seed string plus row, column and semantic suffix; `TownProsperity` for sprite path resolution.
- Produces: one `TownTileColumns`, `TownTileRows`, `TownTileCell`, `logicalToTileCell(x, y)`, and `getTownTileCell(layout, row, col)` owner; one `hashTownVisualSeed(value)` and `townVisualCellSeed(seed, row, col, suffix)` owner; one prosperity-directory mapping for foreground and background sprites.

- [ ] Implement and keep Task 1's characterization tests green while moving duplicated geometry into `town-hub-geometry.ts` and replacing both scene/planner copies with the shared functions and constants.
- [ ] Move the identical FNV-style hash and cell-seed formatting into the exact exports `hashTownVisualSeed(value: string)` and `townVisualCellSeed(seed: string, row: number, col: number, suffix: string)` in `town-hub-seed.ts`; update ground-loader and background-building-planner without changing suffix strings or call order.
- [ ] Make foreground and background sprite URL/key resolution use the existing `getProsperityDirectoryName(prosperity)` mapping; keep building/view mappings and returned strings unchanged.
- [ ] Run `npm --prefix src/WildBunch.Web exec -- vitest run src/components/town-hub/ground-loader.test.ts src/components/town-hub/background-building-planner.test.ts src/components/town-hub/sprite-loader.test.ts src/tests/TownHubScene.tiles.test.ts`, then run `npm --prefix src/WildBunch.Web run typecheck`; verify all town-hub planner, ground-loader, sprite-loader, tile-rendering and scene tests pass.
- [ ] Run `py -3 tools/run.py format --check <changed TypeScript paths>` and `py -3 tools/run.py lint --check <changed TypeScript paths>`; run the web production build and verify generated `src/WildBunch.Web/dist/version.json` reports `0.1.0-dev.99`.
- [ ] Review the final code and tests for unchanged seeded outputs, no out-of-scope layout validation, no generic abstraction, and no renderer or gameplay behavior changes.

### Task 3: Retire the predecessor and publish the successor slice

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row15-townhub-sprite-hover.md`
- Keep through this PR: `.agents/plans/2026-10-12-row15-town-rendering-primitives.md`

- [ ] Classify the complete `.98` predecessor against PR #283 source `f400187dc829b0cb65feb39e145e804a755bd8fb`, merge `79e9a4d48d6f365b1144d61aec75c536ad44a3f5`, exact-head gate `38191794517`, develop-push gate `38192227529`, and paired worktree/branch cleanup; retain the live row 15 roadmap and stable 0.1.0 specification.
- [ ] Update row 15 with verified PR #283 identities and checks, record that `.98` is retired in this successor, and state the remaining row-15 scope without asserting that row 15 is complete.
- [ ] Advance `Directory.Build.props` to `0.1.0-dev.99` and delete the completed predecessor plan in this plan's first substantive commit.
- [ ] Compare the actual source diff with the relevant ADR catalogue successors and PG-003; record in the PR body why current durable decisions and feature promises/evidence remain unchanged.
- [ ] Run focused web validation, changed-path format/lint, generated identity verification, links and `git diff --check`; let the normal staged-snapshot hook run the complete fail-fast gate on the intended commit. Do not run the complete canonical gate immediately before a normal commit or after a successful hooked commit.
- [ ] Obtain one fresh whole-branch review, publish the PR to `develop`, and verify exact-head hosted PR and develop-push gates before merge.
- [ ] After merge, verify the PR source/merge identities and both hosted checks, fast-forward `Z:\wild-bunch`, remove the worktree before its local/upstream branch refs, and verify all three are absent.

**Validation:** Ground-loader and background-planner behavioral tests with demonstrated failing mutations; TownHubScene tile/render tests; `py -3 tools/run.py web --check`; changed-path format/lint; generated version identity `0.1.0-dev.99`; normal staged-snapshot hook; fresh whole-branch review; exact-head hosted PR and develop-push checks.
