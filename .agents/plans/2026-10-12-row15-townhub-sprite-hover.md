# Row 15 Town-Hub Sprite Hover Restoration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make available town-hub sprites enlarge by five percent on pointer entry and return to their fitted display size on pointer exit without distorting their aspect ratio.

**Architecture:** Phaser sprites receive independent scale values from `setDisplaySize`, while the current hover handlers replace both values with scalar `1.05` and `1`. Capture the fitted scale pair and apply/restore it proportionally; keep rectangle fallback behavior unchanged. Exercise the real event behavior with a sprite test double that models Phaser's display-size and scale semantics, then confirm the interaction in the browser.

**Tech Stack:** React, TypeScript, Phaser 3, Vitest, the existing web command-bus lane, Playwright browser QA.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, Web resources and interactions proof family; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 15; `docs/features.md`, PG-003.

**Execution Strategy:** `executing-plans`. The hover test, fitted-scale correction, and browser verification concern one Phaser sprite interaction and one observable behavior; splitting them would repeat setup and weaken the review boundary.

## Global Constraints

- Keep the sprite's fitted display width and height after pointer exit, including when those dimensions require different X and Y scales.
- On pointer entry, increase both fitted scale axes by 5 percent, preserving the original aspect ratio.
- Keep pointer-down selection and available-action gating unchanged.
- Keep rectangle fallback hover behavior unchanged; it starts at unit scale and its current scalar hover/restore is correct.
- Do not change town layout generation, map ownership, keyboard town selection, game capability, or public product promises.
- Compare ADR-0035, ADR-0039, ADR-0046, and PG-003 against the actual diff; this is a behavior repair and must not silently change durable decisions.
- Publish one PR to `develop`; retire the completed `.97` predecessor plan in the successor's first substantive commit and advance the single authoritative version in `Directory.Build.props` from `0.1.0-dev.97` to `0.1.0-dev.98`.
- After merge and passing hosted checks, fast-forward the primary checkout, remove this worktree with the canonical helper, then remove and verify its local and upstream branch refs.

## Review Focus

- A texture-backed available building fitted to a nonuniform scale grows by 5 percent on pointer entry, restores its exact original X/Y scales and rendered dimensions on pointer exit, and still selects the same building on pointer-down.
- The test fails against the current scalar reset and passes only when fitted scale semantics are preserved.
- Rectangle fallback behavior remains unchanged and is covered separately if existing coverage does not prove entry and restoration.
- Browser observation confirms the real sprite visibly enlarges and returns to its initial fitted dimensions; it does not mutate game state.

---

## Tasks

### Task 1: Prove and repair sprite hover scale restoration

**Files:**
- Modify: `src/WildBunch.Web/src/tests/TownHubScene.test.ts`
- Modify: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`

**Interfaces:**
- Consumes: current layout, availability, sprite texture, and Phaser pointer events.
- Produces: available sprite hover interaction that scales the fitted X/Y pair by 1.05 and restores the captured pair exactly; pointer-down still calls `selectBuilding`.

- [ ] Replace or extend the visual sprite double so it stores event handlers, tracks intrinsic texture dimensions, models `setDisplaySize` as independent X/Y scale assignment, and derives rendered dimensions from those scales.
- [ ] Add a texture-backed available-building behavior test with nonuniform fitted scale. Fire pointer-over, pointer-out, and pointer-down handlers and assert the two scale axes and rendered dimensions at each state, plus the existing selected-building callback.
- [ ] Run that focused test against the unmodified scene and confirm it fails specifically because pointer-over loses the fitted ratio and pointer-out fails to restore fitted dimensions; retain the failure output only in the working session, not in the repository.
- [ ] Capture the sprite's fitted X/Y scales before binding hover events; apply the 1.05 multiplier to each axis on entry and restore the captured pair on exit.
- [ ] Run the focused behavior test and confirm the sprite interaction passes. Keep `setScale(1.05)`/`setScale(1)` on the unit-scale rectangle fallback unchanged; add fallback behavior assertions only if existing tests do not already prove its hover and restore outcomes.
- [ ] Run the TownHubScene-focused web tests and confirm sprite availability/opacity and callback behavior remain correct.

### Task 2: Verify the rendered hover interaction and update owned evidence

**Files:**
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `docs/features.md`
- Evidence location: current conversation/review handoff; do not add a validation receipt file.

**Interfaces:**
- Consumes: Task 1's passing event behavior test and the current browser-game scenario.
- Produces: an explicit baseline invariant and accurate PG-003 evidence assessment, plus real-browser observation of sprite hover and restoration.

- [ ] Add the sprite fitted-size/ratio hover invariant to the Web resources and interactions proof family, preserving the existing pointer-only town map decision.
- [ ] Update PG-003's assessment with the focused sprite-hover behavior test and browser result while retaining its partial status and existing map limitations.
- [ ] Ensure PostgreSQL and the worktree's API/web servers with the repository helpers; prove the browser's web app uses this worktree's API and identify the existing active playthrough without creating, archiving, restarting, or travelling in a game.
- [ ] Hover an available texture-backed building, verify visible enlargement and exact return to the prior fitted size after pointer exit, observe pointer-down selection without submitting a gameplay command, and record actual API/web URLs, console/network errors, and the observation separately from automated test results.
- [ ] Stop only servers started for this worktree; leave shared PostgreSQL running. If no safe existing playthrough or texture-backed rendered building is available, use the least invasive deterministic route and state the remaining browser limitation rather than creating or mutating a playthrough.

### Task 3: Retire the predecessor plan, advance version, and publish

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row15-map-renderer.md`
- Keep through this PR: `.agents/plans/2026-10-12-row15-townhub-sprite-hover.md`

- [ ] Classify the complete `.97` predecessor plan against its behavior, tests, and PR #282 delivery; retain WB-16 town-hub host/lifecycle cleanup as remaining row-15 work and do not mark row 15 complete.
- [ ] Update row 15 to make this `.98` plan active, record PR #282 source/merge and hosted checks from verified evidence, and state that its worktree plus local/upstream branch were removed and verified.
- [ ] Delete the completed predecessor plan in this first substantive commit and advance `Directory.Build.props` to `0.1.0-dev.98`.
- [ ] Run the changed web tests, typecheck/build as required, changed-file formatting/lint checks, generated identity check, and `git diff --check`; inspect the full diff and verify links. Start PostgreSQL and run `py -3 tools/run.py ci --check` when required by the repository gate; do not run the canonical gate immediately before a normal commit or after a successful hooked commit.
- [ ] Stage the intended files and commit normally; the installed pre-commit hook runs the fail-fast staged-snapshot gate without mutation. Repair any failure, then retry the normal commit.
- [ ] Complete the author's ADR and feature-matrix comparison in the PR description, obtain a fresh whole-branch review, publish the PR to `develop`, and verify the exact-head hosted gate before requesting merge.
- [ ] After merge, verify PR head, merge commit, and passing hosted develop gate; fast-forward `Z:\wild-bunch`; remove this worktree before deleting and verifying the paired local and upstream branch refs.

**Validation:** Focused `TownHubScene.test.ts` behavior proof with demonstrated red baseline; full focused TownHubScene suite; web typecheck/build and changed-path style checks; real browser hover observation under `.agents/playbooks/ui-browser-check.md`; generated version identity `0.1.0-dev.98`; normal staged-snapshot commit hook, fresh whole-branch review, exact-head hosted CI, and develop-push CI.
