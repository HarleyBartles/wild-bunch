# N-71: Retire the Unmounted Travel Routes Shell

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the unmounted `TravelRoutesPanel` travel-list shell and its component tests, leaving the single routed world-map travel interaction and its behavioral coverage as the player path.

**Architecture:** `TravelPrepSurface` is the live travel preparation and dispatch surface. `TravelRoutesPanel` has no non-test consumer and represents the separate route-list flow rejected by ADR-0039 and the one-playthrough/one-map product decision. Retire the dead component and its tests without adding a test whose only purpose is to assert that a deleted file stays deleted.

**Tech Stack:** React, TypeScript, Vitest, React Testing Library, Python command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [web layer investigation](../investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [test quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [ADR-0039](../../docs/decisions/ADR-0039-seeded-game-setup-and-free-first-arrival.md); [feature matrix](../../docs/features.md).

**Execution Strategy:** `executing-plans` (Native inline execution). The slice is a bounded removal plus truthful inventory reconciliation; one implementation context can verify reachability, preserve the routed behavior proof, run the canonical gate, and prepare the required independent review.

## Global Constraints

- Start from the latest `origin/develop` in a fresh worktree created by the `using-git-worktrees` helper; commit this plan before implementation.
- Promote N-70 and retire its completed plan in N-71's first substantive commit, and advance the sole application version authority from `0.1.0-dev.188` to `0.1.0-dev.189` in that same commit.
- Preserve the single world-map travel flow in `TravelPrepSurface`; do not add a new route-selection flow or change player-facing travel behavior.
- Delete only the unmounted `TravelRoutesPanel` component and its tests unless inspection proves another live consumer or behavior obligation.
- Keep useful preview, failure, retry, stale-response, and dispatch obligations on the routed travel surface; do not expand coverage solely to preserve discarded component behavior.
- Do not add an absence/change-detector test for the retired component.
- Compare the actual diff with ADR-0039 and `docs/features.md`; no durable decision, product promise, or dependency is expected to change, and document the comparison in the PR.
- Keep Markdown paragraphs and list items on one physical line, with regular hyphens only.

## Review Focus

- Confirm the removed component had no production caller and that the retained `TravelPrepSurface` remains the sole player travel route.
- Confirm the deletion removes only obsolete list-shell behavior and its shape-protecting tests, preserving behavioral coverage on the routed map flow.
- Confirm investigation inventories distinguish historical observations from current disposition and no ADR or feature-matrix claim is made stale.

## Tasks

### Task 1: Promote N-70 and establish N-71 version identity

**Files:** `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/plans/2026-10-15-row17-n70-modal-browser-proof.md`, `Directory.Build.props`.

- [ ] Verify PR #373 merged to `develop` from reviewed source `cac0688a2c00c9b672fdd0775389c9a5503cc614` as squash merge `12425d837169afc5bdf6c84fcc64eac0e8c53193`, and confirm exact-head gate `38678530494` plus develop-push gate `38678934634` passed.
- [ ] Update row 17 to name N-71 as active at `0.1.0-dev.189`; append dated N-70 delivery evidence with PR, source SHA, merge SHA, review result, both hosted gate runs, primary fast-forward, and paired worktree/local/upstream branch cleanup.
- [ ] Retire `.agents/plans/2026-10-15-row17-n70-modal-browser-proof.md` in the successor commit while retaining the row-17 roadmap and investigation inventories.
- [ ] Change only `Directory.Build.props` to `0.1.0-dev.189` for the application version advancement, then verify the first substantive commit contains only the promotion, retirement, roadmap, and version files.

### Task 2: Confirm and retire the unmounted route-list shell

**Files:** `src/WildBunch.Web/src/components/TravelRoutesPanel.tsx`, `src/WildBunch.Web/src/tests/TravelRoutesPanel.test.tsx`.

- [ ] Reconfirm with source search that `TravelRoutesPanel` has no production consumer and inspect the live `TravelPrepSurface` and its tests to verify that map preview, selection, dispatch, rejection, and retry behavior remain represented there.
- [ ] Delete `TravelRoutesPanel.tsx` and `TravelRoutesPanel.test.tsx`; do not create a test that checks their absence.
- [ ] Run the focused routed travel behavior tests and verify they pass with the old component removed.

### Task 3: Reconcile the finding inventories and deliver N-71

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`, `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`, `docs/features.md`, PR description, and all N-71 changes.

- [ ] Add dated N-71 disposition to the owning investigations and update current file-disposition rows to record the component and tests as retired; preserve original dated observations as historical evidence.
- [ ] Compare ADR-0039 and PG-003 with the final diff, keep both unchanged if they remain truthful, and record in the PR why this removal changes no feature promise, dependency, or durable decision.
- [ ] Run the focused travel tests and `py -3 tools/run.py ci --check`; inspect the complete fail-fast gate result.
- [ ] Stage the intended tree, inspect the staged diff and version, and let the normal check-only pre-commit hook validate the exact staged candidate.
- [ ] Dispatch a fresh-context whole-branch reviewer to inspect the committed diff and repeat the ADR and feature-matrix comparisons; resolve findings and request a fresh review after any correction.
- [ ] Publish the PR to `develop`, wait for the exact-head hosted gate to pass before merge, merge it, verify the develop-push hosted gate, fast-forward the primary checkout without disturbing the user's existing investigation edit, and remove the merged worktree with its exact local and upstream branch refs together.
