# N-69: Reconcile Overlay Behavior Test Claims

> **For agentic workers:** Use `executing-plans` with Native inline execution. Read this committed plan and the baseline specification before implementation.

**Goal:** Remove overlay tests that claim CSS layout behavior from JSDOM, correct the misleading developer-panel test title, and record the actual modal-composition coverage boundary.

**Architecture:** Keep the existing user-visible expand, shrink, Escape, close, and contextual-panel behavior tests. Retire the computed-style height proxy because JSDOM does not render layout, and state precisely what the existing nested-confirmation test proves. Do not add browser infrastructure or claim that JSDOM proves native dialog top-layer focus or background inertness.

**Tech Stack:** React, TypeScript, Vitest, Testing Library, JSDOM, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [ADR catalogue](../../docs/decisions/README.md); [feature matrix](../../docs/features.md).

**Execution:** Fresh linked worktree from `origin/develop`; commit this plan before implementation. Implement inline, then obtain one fresh whole-branch review. Deliver a PR to `develop` at `0.1.0-dev.187`, require the exact-head hosted gate and develop-push gate to pass, fast-forward the primary checkout, and remove this clean worktree and its exact local/upstream branch refs.

**Review Focus:** Confirm the removed assertions did not prove rendered behavior, retained user interactions still have meaningful outcomes, the nested cancellation claim is bounded to current evidence, and ADR/feature-matrix truth is unchanged.

**Scope and exclusions:** This slice makes no production UI change and adds no browser-test dependency or harness. It does not change modal design, keyboard behavior, or player promises. A browser-level proof of native top-layer focus, background inertness, and focus restoration remains outside the existing test harness; do not convert that absence into a JSDOM claim.

## Global Constraints

- Use the current develop integration, versioning, review, fail-fast gate, hosted-check, plan-retirement, and paired worktree/branch cleanup contracts in the roadmap and runbooks.
- Retain only behavior assertions that distinguish a supported user outcome; do not replace the removed CSS proxy with another source-shape or CSS-string assertion.
- Do not create a new regression test because the production behavior is unchanged and the existing nested cancellation scenario already proves its stated callback-level outcome.
- Compare the actual diff with applicable ADRs and `docs/features.md`; record why neither changes.
- Markdown prose and list items remain one physical source line, with regular hyphens only.

## Tasks

### Task 1: Promote N-68 and establish N-69 version identity

**Files:** `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/plans/2026-10-15-row17-n68-player-playtest-composition.md`, `Directory.Build.props`.

- [x] Reconcile N-68 against PR #371, reviewed source `39a3242a8bbe93024b2113202c4e8d55b0594ebb`, squash merge `ef68448aca61a3a7b15c8825f89201920020eea8`, exact-head gate `38668014404`, develop-push gate `38668998186`, independent whole-branch review, primary fast-forward, and paired cleanup recorded in the current execution context.
- [x] Update row 17 to name N-69 as active, advance the proposed version from `0.1.0-dev.186` to `0.1.0-dev.187`, and add PR #371 plus its source, merge, review, hosted-gate, and cleanup evidence to the row notes.
- [x] Delete the completed N-68 plan in this successor's first substantive commit; retain the roadmap and mixed-scope web-test investigation.
- [x] Advance only `Directory.Build.props` to `0.1.0-dev.187` and commit this promotion, retirement, and version advancement together before test or investigation edits.
- [x] Verify the resulting commit contains only the planned promotion, retirement, and version files.

### Task 2: Remove unsupported overlay test oracles

**Files:** `src/WildBunch.Web/src/tests/DevOverlay.test.tsx`.

- [x] Remove the expanded-height test that reads `getComputedStyle` height/overflow in JSDOM and claims the drawer is capped and scrollable.
- [x] Rename the pre-session panel test so it accurately states that Session Audit remains available when no contextual panel matches; retain only assertions that distinguish this behavior from contextual Saloon/Travel panels.
- [x] Keep existing expand/shrink, Escape, close, click-away, surface-specific panel, and default-panel behavior tests unchanged unless the focused run exposes a necessary correction.
- [x] Run the focused DevOverlay test file and inspect the diff to ensure no production source or unrelated test changed.

### Task 3: Reconcile the overlay coverage inventory

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`.

- [x] Update the ConfirmDialog, DevOverlay, and GameSettingsOverlay inventory descriptions to identify their present behavior owners and avoid implying that JSDOM verifies native dialog top-layer focus, background inertness, or focus restoration.
- [x] Add a dated N-69 disposition stating that the JSDOM CSS serialization proxy was retired, the pre-session test title was corrected, and `GameSettingsOverlay.test.tsx` already proves cancellation closes only the nested confirmation while preserving Settings and avoiding archive.
- [x] Record that the repo has no browser test runner, so native top-layer/inert/focus-restoration proof is not added to this 0.1.0 slice; do not claim these behaviors are verified or open a new infrastructure slice here.
- [x] State that no production behavior, ADR, feature promise, or dependency changed; verify ADR-0030/0041 remain accurate for the playtest developer surface and `docs/features.md` remains unchanged.

### Task 4: Validate and reconcile the completed slice

**Files:** All N-69 changes.

- [x] Run `py -3 tools/run.py web --check` and read the complete result, including format, lint, typecheck, all web tests, and production build.
- [x] Run `py -3 tools/run.py ci --check` against the final staged candidate before publication; follow cheapest-first failure behavior and resolve the first failure before continuing. The installed check-only commit hook runs this exact canonical command on the staged snapshot.
- [x] Verify the staged and committed tree, confirm `Directory.Build.props` is the only application-version authority, and confirm there is no generated or ignored output in the diff.
- [ ] Complete the author ADR and feature-matrix checks with the no-change rationale in the PR body; have the independent reviewer repeat both checks against the actual diff.
- [ ] Publish/update the Draft PR against `develop`, verify the remote head equals the reviewed local head, and wait for its exact-head hosted gate to pass before requesting merge.
- [ ] Merge only after whole-branch review and exact-head CI are clear; verify the develop-push gate passes on the merge commit and the PR merged to `develop`.
- [ ] Fast-forward the primary checkout without overwriting the user's existing investigation edit, then remove this clean worktree before deleting and verifying the exact local and upstream branch refs.

## Acceptance Criteria

- The web suite no longer treats JSDOM's serialized CSS values as proof of rendered drawer geometry or scrolling.
- The pre-session Session Audit test title matches the behavior it asserts.
- Existing DevOverlay interaction tests and the composed cancellation behavior remain covered by behavior-focused assertions.
- The test inventory accurately distinguishes current JSDOM evidence from native-browser-only modal behavior.
- N-68 is retired only after its merged delivery and gates are recorded, and N-69 merges as `0.1.0-dev.187` with required hosted checks green.
