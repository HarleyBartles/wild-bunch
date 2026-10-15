# N-70: Prove the Nested Modal Contract in Chromium

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a repeatable Chromium behavior test for the real nested settings and confirmation dialogs, proving focus containment/restoration, background inactivity, topmost dismissal, and visible controls at a narrow viewport.

**Architecture:** Use Playwright Test against a Vite-served test-only page that mounts the production `GlobalOverlays`, `CockpitOverlayFrame`, and `ConfirmDialog` with a small deterministic `GameSessionContext` value. Keep the fixture outside production build inputs, run one Chromium project, and route the browser test through the existing command bus and canonical hosted gate. ADR-0047 will define the durable browser-test lane and partially supersede ADR-0017's earlier Vitest/jsdom browser-client boundary. Repair production code only if the browser test exposes a failure in the accepted contract.

**Tech Stack:** React, TypeScript, styled-components, Vite, Playwright Test 1.64.1, Chromium, Python command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [ADR-0017](../../docs/decisions/ADR-0017-use-xunit-vitest-testing-library-and-explicit-postgresql-validation-lanes.md); [ADR-0022](../../docs/decisions/ADR-0022-ui-browser-checks-are-a-manual-evidence-lane.md); [ADR catalogue](../../docs/decisions/README.md); [feature matrix](../../docs/features.md).

**Execution Strategy:** `executing-plans` (Native inline execution). Runner setup, the shared fixture, behavior assertions, and command-bus/CI integration form one dependent path; one inline implementation context keeps the browser evidence and its gate wiring coherent, followed by the required independent whole-branch review.

## Global Constraints

- Start from the latest `origin/develop` in a fresh worktree created by the `using-git-worktrees` helper; commit this plan before implementation.
- Promote N-69 and retire its completed plan in N-70's first substantive commit, and advance the sole application version authority from `0.1.0-dev.187` to `0.1.0-dev.188` in that same commit.
- Keep the test page out of production build inputs and do not add a second production dialog implementation or API-backed gameplay fixture.
- Keep this slice to one Chromium browser project and the modal contract; do not add map keyboard selection, a general browser matrix, accessibility conformance claims, snapshots, or screenshot baselines.
- Prefer accessible role and name locators; use browser-computed element rectangles only for actual visible geometry assertions, not serialized CSS values.
- Preserve the current product behavior and wording unless a focused browser test witnesses a failure in the accepted modal contract.
- Compare the actual diff with applicable ADRs and `docs/features.md`; record why neither changes unless the implementation evidence shows otherwise.
- Keep the canonical gate fail-fast and cheapest-first, and place browser tests after ordinary web behavior tests.
- Keep Markdown paragraphs and list items on one physical line, with regular hyphens only.

## Review Focus

- A modal opened with `showModal()` must make background controls unfocusable while it is active, including when a confirmation is stacked over Settings.
- Escape must dismiss only the top confirmation, return focus to its Start Over opener, then dismiss Settings and return focus to its HUD trigger.
- The confirmation's named controls must remain within a 320-pixel viewport without horizontal document overflow.

## Tasks

### Task 1: Promote N-69 and establish N-70 version identity

**Files:** `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/plans/2026-10-15-row17-n69-overlay-behavior.md`, `Directory.Build.props`.

- [ ] Verify PR #372 merged to `develop` from reviewed source `b01f7b43fc3599f9eafbdbae20fd9dfb33d9a6b7` as `cacdca1bc8776e2a4e291768569b154a268cfe4f`, and confirm exact-head gate `38671884485` plus develop-push gate `38672399394` passed.
- [ ] Update row 17 to name N-70 as active at `0.1.0-dev.188`; append dated N-69 delivery evidence with PR, source SHA, merge SHA, review result, both hosted gate runs, primary fast-forward, and paired worktree/local/upstream branch cleanup.
- [ ] Retire `.agents/plans/2026-10-15-row17-n69-overlay-behavior.md` in the successor commit while retaining the row-17 roadmap and web-test inventory.
- [ ] Change only `Directory.Build.props` to `0.1.0-dev.188` for the application version advancement, then verify the first substantive commit contains only the promotion, retirement, roadmap, and version files.

### Task 2: Add the Chromium modal composition behavior proof

**Files:** `src/WildBunch.Web/package.json`, `src/WildBunch.Web/package-lock.json`, `src/WildBunch.Web/tsconfig.json`, `src/WildBunch.Web/playwright.config.ts`, `src/WildBunch.Web/browser-tests/modal-stack.html`, `src/WildBunch.Web/browser-tests/modal-stack.tsx`, `src/WildBunch.Web/browser-tests/modal-stack.spec.ts`, `docs/decisions/ADR-0017-use-xunit-vitest-testing-library-and-explicit-postgresql-validation-lanes.md`, `docs/decisions/ADR-0047-add-playwright-for-native-browser-contracts.md`, `docs/decisions/README.md`.

- [ ] Add exact dev dependency `@playwright/test` version `1.64.1`, an npm script named `test:browser`, a Chromium-only Playwright config with one worker in CI and a Vite `webServer` on strict port `4173`, and TypeScript coverage for the config and browser tests.
- [ ] Create ADR-0047 defining Chromium/Playwright as the automated lane for native modal focus and rendered-geometry behavior, then partially supersede ADR-0017 while retaining Vitest/Testing Library for component and route behavior; keep ADR-0022's separate manual evidence lane unchanged.
- [ ] Create a test-only Vite page that imports the production global stylesheet and mounts `GlobalOverlays` inside `GameSessionContext.Provider`; expose one external Game Settings trigger and use a deterministic context value with no API or PostgreSQL dependency.
- [ ] Add a Playwright scenario using `getByRole` locators that opens Settings and Start Over, proves initial focus enters each dialog, proves programmatic focus cannot escape the top modal, proves Tab and Shift+Tab remain in the confirmation, and proves Escape closes only the top dialog before restoring focus through both levels.
- [ ] Add a narrow-viewport scenario at 320 by 600 pixels that opens the confirmation, checks the heading and both named actions have browser-measured rectangles fully inside the viewport, and verifies the document has no horizontal overflow.
- [ ] Run `npm run test:browser -- --list` to verify discovery, then `npm run test:browser` and inspect the browser output; do not replace real browser observations with JSDOM or CSS string assertions.
- [ ] Prove the test oracle with two temporary production mutations, restoring each source file byte-for-byte after its RED result: replace `showModal()` with non-modal `open`-attribute behavior and verify background-focus containment fails; remove the explicit confirmation focus assignment and verify initial-focus behavior fails.
- [ ] If a current behavior assertion fails before mutation, use that witnessed failure as the RED case, repair only the owned dialog behavior, and rerun the focused browser test before continuing.

### Task 3: Put the browser lane in the canonical command bus and hosted gate

**Files:** `tools/run.py`, `tools/README.md`, `.github/workflows/ci.yml`, `.agents/doctrine/validation-policy.md`, `.agents/playbooks/testing.md`.

- [ ] Add `web-browser-tests` as a check-only command-bus lane invoking `npm run test:browser` in the web project; call it from both the complete `web --check` target and canonical `ci --check` immediately after `web-tests` and before `web-build`.
- [ ] Update command-bus help, validation doctrine, and the testing playbook with the browser lane's owner, local command, Chromium prerequisite `npx playwright install chromium`, and the hosted browser-install step.
- [ ] Add `npm ci --prefix src/WildBunch.Web` followed by `npx playwright install --with-deps chromium` to the hosted workflow's dependency setup before the tracked-gate hook, leaving fail-fast ordering of checks within the hook unchanged.
- [ ] Run the focused browser tests after all required behavior and gate wiring is present, then run `py -3 tools/run.py web --check` and read its complete results.
- [ ] Stage the intended tree, inspect the staged diff and `Directory.Build.props`, and let the normal check-only pre-commit hook run `py -3 tools/run.py ci --check` on the exact staged candidate.

### Task 4: Reconcile evidence and deliver N-70

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`, PR description, and all N-70 changes.

- [ ] Update the overlay coverage inventory with the exact real-browser outcomes proven by the new tests and retain explicit limits: Chromium coverage is not cross-browser certification or a complete WCAG conformance audit.
- [ ] Confirm ADR-0047 and the partial supersession of ADR-0017 truthfully describe the new test boundary, ADR-0022 remains accurate because manual browser evidence remains a separate lane, ADR-0030/0041 remain accurate, and `docs/features.md` remains unchanged; record the author comparison and rationale in the PR body.
- [ ] Complete the author ADR and feature-matrix checks before review, then dispatch a fresh whole-branch reviewer and require the reviewer to repeat both comparisons against the committed diff.
- [ ] Publish a Draft PR to `develop` at `0.1.0-dev.188`, verify its head equals the reviewed commit, make it ready only after self-review and the check-only commit gate pass, and require the exact-head hosted gate to pass before merge.
- [ ] Merge the PR to `develop`, verify the develop-push gate passes on the merge commit, fast-forward the primary checkout while preserving its user-owned inventory edit, remove this clean worktree, then delete and verify absence of this exact local/upstream branch pair.

## Acceptance Criteria

- A real Chromium run tests the production nested overlay components in a deterministic test-only page.
- The browser assertions fail when native modal activation or the explicit confirmation initial focus behavior is deliberately broken.
- The narrow-viewport test observes real element geometry and fails on horizontal overflow or an offscreen required action.
- The browser suite is reachable through both `py -3 tools/run.py web --check` and `py -3 tools/run.py ci --check`, and hosted PR/develop gates execute it.
- The N-69 plan is retired only after its merged delivery evidence is recorded in the successor, and N-70 merges as `0.1.0-dev.188` with required hosted gates green.
