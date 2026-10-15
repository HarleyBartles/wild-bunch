# N-76: Prove town-building hover with the real Phaser renderer

**Goal:** Verify that an available non-square town-building sprite grows proportionally on pointer hover and returns to its configured fitted dimensions in the real Phaser browser renderer. Reconcile the old WB-15 claim with current implementation and evidence.

**Architecture:** `TownHubScene` captures the fitted sprite `scaleX`/`scaleY` after `setDisplaySize`, applies a proportional 1.05 hover scale, and restores both captured axes on pointerout. The existing Vitest scene double exercises that callback logic; a Chromium test must establish the same contract through actual Phaser rendering and a loaded production sprite. This adds test evidence only unless the real renderer exposes a behavioral defect.

**Tech Stack:** Phaser 3.90, Vite, Playwright, Chromium, Vitest, TypeScript.

**Specification:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 17 inventory `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md` and `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`.

**Execution Strategy:** Execute inline in the fresh canonical worktree. Commit this plan before implementation. Add a small browser fixture around the production `TownHubScene` and real non-square production building texture. In Chromium, capture the canvas before hover, while hovered, and after pointerout; prove visible change on hover and pixel-identical restoration after pointerout. Assert the sprite texture loaded so the rectangle fallback cannot satisfy the result. Deliberately restore the old absolute `1.05`/`1` scaling in production code and confirm the browser test fails, then restore the source and rerun green. Do not add a production fix unless the real-browser proof demonstrates one is required.

## Global Constraints

- Preserve current pointer-only interaction and available-action rules; this plan does not add map keyboard selection or change ADR-0046.
- Use the real Phaser 3.90 runtime and a production non-square building asset. Do not reproduce Phaser's scale implementation in the browser oracle.
- Assert user-visible canvas output and separately establish that the sprite branch loaded. Do not assert implementation property names as the behavior result.
- Avoid sleeps, screenshot golden files, broad rendering changes, test-only behavior in production, and unrelated accessibility or asset-pipeline work.
- Retain the focused unit test if it remains useful as callback-level coverage; do not remove it merely because browser proof is added.
- Consult `.agents/playbooks/unslop.md` with the relevant UI and test profiles, `.agents/playbooks/testing.md`, `.agents/playbooks/decision-records.md`, and `.agents/playbooks/feature-matrix.md`.
- Compare against ADR-0046 and PG-003. Leave the ADR and feature matrix unchanged if the change only corrects evidence and test coverage; state the comparison in the PR.
- Retire only the completed N-75 plan in this successor. Preserve the active roadmap, specification, and current inventories.
- Advance only the authored application version from `0.1.0-dev.193` to `0.1.0-dev.194` for this PR.
- Use the canonical fail-fast command bus and check-only hook. Never bypass validation or let a hook mutate the staged candidate.
- Publish a Draft PR to `develop`; require a fresh whole-branch review, passing exact-head hosted CI, and passing develop-push CI after merge.
- After verified merge, fast-forward the primary checkout while preserving unrelated user edits, then use `remove_worktree.py --check` and `--apply` before deleting and verifying the exact local/upstream branch refs.

## Review Focus

- Does the test run through real Phaser and confirm the production sprite loaded before it evaluates hover behavior?
- Does the visible result change while hovered and return exactly to its baseline after pointerout?
- Does the deliberate absolute-scale mutation fail for the intended restoration assertion?
- Does the dated WB-15 disposition preserve the historical observation while describing only evidence observed in the current source and browser test?
- Are ADR-0046 and PG-003 still accurate, with no unrequested product or accessibility promise added?

### Task 1: Commit the JIT plan and retire N-75

**Files:** `.agents/plans/2026-10-15-row17-n76-phaser-hover-browser-proof.md`, `.agents/plans/2026-10-15-row17-n75-shared-session-cache.md`, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `Directory.Build.props`.

- [x] Confirm this worktree is clean on `codex/row17-n76-session-identity-stale-results`, based on refreshed `origin/develop` at `aed37561cb08fd5c570bf6c3f8e77e300189be4b`.
- [x] Record N-75 PR #378, reviewed source `b8060e2d09bba901d0e216ac8049666a7f082547`, squash merge `aed37561cb08fd5c570bf6c3f8e77e300189be4b`, clean independent review, exact-head hosted gate `38703144397`, develop-push gate `38703943112`, and verified primary/worktree/local/upstream cleanup in the roadmap.
- [x] Point row 17 to this active N-76 plan at `0.1.0-dev.194`; include merged PR #376, #377 and #378 links and append the dated N-75 delivery.
- [x] Delete the completed N-75 plan in this substantive successor commit and set `Directory.Build.props` to `0.1.0-dev.194`.
- [x] Run the staged-candidate check-only hook and commit the plan, successor retirement, roadmap, and version before implementation.

### Task 2: Add real-browser hover and restoration proof

**Files:** `src/WildBunch.Web/browser-tests/town-hub-hover.html`, `src/WildBunch.Web/browser-tests/town-hub-hover.ts`, `src/WildBunch.Web/browser-tests/town-hub-hover.spec.ts`, and `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts` only if the real behavior is defective.

- [x] Run the existing focused `TownHubScene.test.ts` baseline and inspect the production sprite asset dimensions and current browser test harness.
- [x] Add the smallest deterministic harness that instantiates the real `TownHubScene` in Phaser 3.90 with an available Store and a production non-square Store texture.
- [x] Serve the production asset through a test-local Playwright route; provide harmless local responses for unrelated scene textures so the browser test has no external service dependency.
- [x] In Chromium, wait for the actual Store texture and canvas to be ready, capture the baseline canvas, move over the Store sprite and prove the rendered canvas changes, then move away and prove the rendered canvas returns to the exact baseline.
- [x] Ensure missing texture or rectangle fallback cannot pass the sprite-specific scenario. Do not add fixed sleeps or committed golden images.
- [x] Temporarily restore the old absolute sprite hover scales (`1.05` and `1`) and require the browser test to fail because the post-hover canvas differs from baseline; restore production source and rerun the focused browser scenario green.
- [x] Inspect the resulting screenshots locally to verify the test is observing the intended Store sprite and not unrelated scene movement. Retain only the behavior assertions, not screenshot receipts.

### Task 3: Reconcile the WB-15 investigation and test inventory

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`.

- [x] Preserve WB-15's original dated observation as historical truth and add a dated N-76 disposition describing current fitted-scale restoration and the real browser evidence.
- [x] Correct the test follow-up so the current scene double is not described as wholly unable to detect hover scale behavior; distinguish callback-level unit coverage from real renderer/browser evidence.
- [x] Update the file disposition for `TownHubScene.ts` to state its current hover behavior and retained test owner, without carrying a stale active fix recommendation.
- [x] Keep the conclusion bounded to a production asset exercised by the local real Phaser browser harness; do not claim deployed or cross-browser proof.
- [x] Read ADR-0046 and PG-003 against the actual diff; leave them unchanged because this is evidence reconciliation with no product promise change.

### Task 4: Validate, review, publish, and close out N-76

**Files:** The N-76 committed diff and review package.

- [x] Run the focused Vitest suite and browser scenario, including the deliberate mutation proof; run `py -3 tools/run.py web --check` and then `py -3 tools/run.py ci --check` on the intended committed tree.
- [ ] Inspect the exact committed diff, confirm roadmap and `Directory.Build.props` agree on `0.1.0-dev.194`, and verify the dated disposition matches the browser evidence.
- [ ] Dispatch a fresh-context whole-branch reviewer at GPT-6 Luna with exact base/head and diff. Require independent ADR and feature-matrix comparison; resolve Critical/Important findings and obtain a fresh review after any correction.
- [ ] Publish the Draft PR to `develop`, include focused and canonical local proof plus reviewer outcome, then mark ready and require exact-head hosted CI to pass before merge.
- [ ] Verify develop-push CI for the squash merge, fast-forward the primary checkout without disturbing its unrelated edit, and use `remove_worktree.py --check` then `--apply` before deleting and verifying the exact local and upstream branch refs.
