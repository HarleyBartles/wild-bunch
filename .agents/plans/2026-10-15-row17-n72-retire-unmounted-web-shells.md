# N-72: Retire the Unmounted Web Shells

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the confirmed-unmounted legacy web panels and setup components, leaving the routed game and setup surfaces as the only player-facing implementations.

**Architecture:** `SetupHuntStep` is the live setup contract and already owns player name, difficulty, entropy and editable/randomized seed behavior. `FieldReportPanel`, `StartGameOptionsForm`, `SetupSeedSummary` and `SeedCodeEditor` have no production consumers; `AvailableActionsPanel` is already absent. Remove only these dead components and update the current web investigation disposition while retaining historical observations.

**Tech Stack:** React, TypeScript, styled-components, Python command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [web layer investigation](../investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [PG-001 feature contract](../../docs/features.md#pg-001--start-a-hunt-and-choose-the-first-town).

**Execution Strategy:** `executing-plans` (Native inline execution). This is one bounded retirement slice with no replacement behavior; source reachability, current setup behavior tests, and the canonical gate provide the relevant evidence.

## Global Constraints

- Start from the latest `origin/develop` in this fresh worktree created by the `using-git-worktrees` helper; commit this plan before implementation.
- Promote N-71 and retire its completed plan in N-72's first substantive commit; advance `Directory.Build.props` from `0.1.0-dev.189` to `0.1.0-dev.190` in that same commit.
- Preserve `SetupHuntStep` and its live editable/randomized seed, difficulty, entropy, player-name, validation and setup submission behavior.
- Remove only `FieldReportPanel`, `StartGameOptionsForm`, `SetupSeedSummary` and `SeedCodeEditor` after confirming no production consumers; do not remove shared components or the live setup seed codec.
- Do not add file-absence tests or replacement tests for behavior that is not part of the live product path.
- Compare the actual diff with ADRs and `docs/features.md`; record in the PR why no durable decision or current product promise changes.
- Keep Markdown paragraphs and list items on one physical line, with regular hyphens only.

## Review Focus

- Confirm each deleted component has no production consumer and no unique live player behavior.
- Confirm the live setup still supports the PG-001 contract, including editable and randomized seed behavior and name-only defaults.
- Confirm the current investigation no longer presents already-retired or now-retired shells as open recommendations, while preserving dated historical findings.

## Tasks

### Task 1: Promote N-71 and establish N-72 version identity

**Files:** `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/plans/2026-10-15-row17-n71-retire-travel-routes-shell.md`, `Directory.Build.props`.

- [ ] Verify PR #374 merged to `develop` from reviewed source `39eb115f87c688a0a576abbc04ca565c94e00bfd` as squash merge `7f673e4609651200981a93480f7dcab9fcdd8fe3`; verify exact-head gate `38682288662` and develop-push gate `38682747483` passed.
- [ ] Update row 17 to name N-72 as active at `0.1.0-dev.190`; append dated N-71 delivery evidence with PR, source SHA, merge SHA, fresh review outcome, both hosted gates, primary fast-forward, and paired worktree/local/upstream branch cleanup.
- [ ] Retire `.agents/plans/2026-10-15-row17-n71-retire-travel-routes-shell.md` in this successor commit while retaining the row-17 roadmap and investigation inventories.
- [ ] Change only `Directory.Build.props` to `0.1.0-dev.190` for the application version advancement, then verify this first substantive commit contains only the promotion, retirement, roadmap and version files.

### Task 2: Retire confirmed-unmounted legacy web components

**Files:** `src/WildBunch.Web/src/components/FieldReportPanel.tsx`, `src/WildBunch.Web/src/components/StartGameOptionsForm.tsx`, `src/WildBunch.Web/src/components/SetupSeedSummary.tsx`, `src/WildBunch.Web/src/components/SeedCodeEditor.tsx`.

- [ ] Search all production and test sources for each component name and inspect the routed setup composition; confirm `SetupHuntStep` independently owns the live setup fields and submission behavior.
- [ ] Delete the four listed components only after confirming each has no production or test consumer; preserve `gameSetupSeedCodec` and all routed start-flow code.
- [ ] Do not add tests that assert files or exports are absent. Run the focused `StartFlow.test.tsx` behavior suite to verify the retained setup contract still passes.

### Task 3: Reconcile the web inventory and deliver N-72

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`, `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`, `PR description`, and all N-72 changes.

- [ ] Update WB-23 and the current file-disposition rows: record `AvailableActionsPanel` as already retired based on current source, and the four confirmed-unmounted components as retired by N-72. Preserve earlier dated findings as history and add a dated N-72 disposition; do not invent an owning test gap where none exists.
- [ ] Compare the final diff with applicable ADRs and PG-001; keep them unchanged if the live behavior and decisions remain truthful, and state the author comparison in the PR.
- [ ] Run focused `StartFlow.test.tsx` and `py -3 tools/run.py ci --check`; inspect the fail-fast gate result for the tested commit.
- [ ] Stage the intended tree, inspect the staged diff and version, and let the normal check-only pre-commit hook validate the exact staged candidate.
- [ ] Dispatch a fresh-context whole-branch reviewer with the prepared review package; require an independent ADR and feature-matrix comparison, resolve findings and request a fresh review after any correction.
- [ ] Publish the PR to `develop`, keep it Draft until the author comparison, independent review and local gate are resolved, then mark it ready and wait for the exact-head hosted gate to pass before merge. Verify the develop-push gate, fast-forward the primary checkout without disturbing the user's existing investigation edit, and remove the merged worktree with its exact local and upstream branch refs together.
