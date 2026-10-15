# N-75: Prove shared session cache coherence on the trail

**Goal:** Prove that one successful trail mutation updates the authoritative session observed by both the persistent player HUD and the mounted trail view before the follow-up session refresh completes.

**Architecture:** The web shell and `TravelPanel` read the same session resource through the canonical TanStack Query cache. The mutation response updates that cache, and invalidation refreshes server-backed resources. This is a single-tab client observation contract; it does not add cross-tab synchronization or change server authority.

**Tech Stack:** React, TanStack Query, Vitest, Testing Library, TypeScript, Wild Bunch command bus.

**Specification:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 17 inventory `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`.

**Execution Strategy:** Execute this bounded test and evidence slice inline. Commit this plan before implementation. Use `executing-plans` and test-driven development. Prove the test fails when the mutation-result cache write is removed, restore the implementation, and rerun green. The first substantive successor commit retires N-74, records its merged delivery, advances the development version once, and commits this N-75 plan. A fresh whole-branch reviewer independently compares ADRs and the feature matrix before publication.

## Global Constraints

- Preserve the existing player behavior and server authority. Do not add SignalR, browser storage authority, a second session cache, cross-tab behavior, or a product feature.
- Keep the test behavior-first. Assert updated visible session state in both the real HUD and trail panel while the refetch remains pending; do not assert cache keys, mock call counts, component wiring, or source shape.
- Retain `useTravelPanelState` fresher-data behavior and verify the shared-reader coherence behavior from the inventory.
- Consult `.agents/playbooks/unslop.md` with the relevant UI/test profiles, `.agents/playbooks/testing.md`, `.agents/playbooks/decision-records.md`, and `.agents/playbooks/feature-matrix.md`.
- Compare the diff with ADR-0027, ADR-0039, ADR-0047, PG-003, and any successor records selected from the catalogue. Leave durable records and the feature matrix unchanged if no durable decision or player promise changes, and state the comparison in the PR.
- Do not add browser-only coverage: this is a mounted React Query integration contract, not native browser behavior.
- Retire only the wholly completed N-74 plan in this successor. Preserve the active roadmap, specification, and live investigation inventories.
- Advance only the authored application version from `0.1.0-dev.192` to `0.1.0-dev.193` for this PR.
- Use the canonical fail-fast command bus and check-only hook. Never bypass validation or let a hook mutate the staged candidate.
- Publish a Draft PR to `develop`; require a fresh whole-branch review, passing exact-head hosted CI, and passing develop-push CI after merge.
- After verified merge, fast-forward the primary checkout while preserving unrelated user edits, then remove this clean worktree and its exact local/upstream branch refs as a paired closeout.

## Review Focus

- Does the test fail if the mutation response is no longer written to the session observed by the shell and trail view, before invalidation refetch finishes?
- Does it assert observable state in both real readers rather than implementation details or identical outcomes from multiple routes?
- Does its deferred refetch avoid timing-dependent sleeps and prove the mutation result is visible before refreshed data arrives?
- Does the dated inventory entry describe exactly what this proof establishes and avoid implying multi-tab or deployed evidence?
- Are ADR and feature-matrix comparisons independently resolved against the committed diff?

### Task 1: Commit the JIT plan and retire N-74

**Files:** `.agents/plans/2026-10-15-row17-n75-shared-session-cache.md`, `.agents/plans/2026-10-15-row17-n74-prologue-session-authority.md`, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `Directory.Build.props`.

- [x] Confirm this worktree is clean on `codex/row17-n75-shared-session-cache`, based on current `origin/develop` at `46b577b6ea3875f714bddc901efc4db0fe10aeef`, and has the predecessor plan from the merged base; a fresh fetch confirmed `origin/develop` remains at the same commit.
- [x] Record N-74 PR #377, reviewed source `1ee6e5d6a9e5a12c49e63bf9c253943d20de3503`, squash merge `46b577b6ea3875f714bddc901efc4db0fe10aeef`, fresh review approval, exact-head hosted gate `38698402348`, develop-push gate `38699028842`, and verified primary/worktree/branch cleanup in the roadmap.
- [x] Point row 17 to this active N-75 plan at `0.1.0-dev.193`; retain prior deliveries and append the dated N-74 delivery note.
- [x] Delete the completed N-74 plan in this substantive successor commit and set `Directory.Build.props` to `0.1.0-dev.193`.
- [ ] Run the staged-candidate check-only hook and commit the plan, successor retirement, roadmap, and version before implementation.

### Task 2: Add a mutation-proven shell/trail coherence scenario

**Files:** `src/WildBunch.Web/src/tests/AppShell.test.tsx`.

- [ ] Run the focused `AppShell.test.tsx` baseline and inspect the current on-trail shell, travel mutation result, and session resource refresh behavior.
- [ ] Add a real `AppShell` scenario that hydrates an active journey, clicks “Advance travel day,” receives a session with visibly changed clock and remaining distance, and keeps the subsequent `getGame` refresh deferred.
- [ ] While that refresh is pending, assert the HUD shows the changed day and the trail summary shows the changed remaining distance; then resolve the refresh with the same authoritative session and assert the mounted route remains coherent.
- [ ] Run the scenario red by temporarily removing the mutation result's session-cache update from `useTravelPanelState`; confirm at least one visible assertion fails before the deferred refresh is released. Restore production code exactly and rerun the test green.
- [ ] Avoid sleeps and count-based assertions. Ensure the deferred promise is always resolved in test cleanup to prevent a stranded query.

### Task 3: Reconcile the finding inventory and decision boundaries

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`.

- [ ] Add a dated N-75 disposition that the shared shell/trail reader coherence gap is now behavior-tested across the real mounted shell and travel panel, with the command-result state visible before the session refetch completes.
- [ ] Explicitly limit the evidence to the same tab and QueryClient; do not claim cross-tab synchronization, SignalR, or deployed behavior.
- [ ] Read the relevant ADRs and PG-003 matrix entry against the actual change. Keep those documents unchanged if no architectural decision, product promise, dependency, release disposition, or evidence assessment changes; record that comparison in the PR body.

### Task 4: Validate, review, publish, and close out N-75

**Files:** The N-75 committed diff and review package.

- [ ] Run the focused test and its deliberate mutation proof; run the production web test lane as appropriate, then `py -3 tools/run.py ci --check` on the intended committed tree.
- [ ] Inspect the exact committed diff, validate the roadmap version against `Directory.Build.props`, and check the N-75 finding is closed without overstating its evidence.
- [ ] Dispatch a fresh-context whole-branch reviewer at GPT-6 Luna with exact base/head and diff. Require independent ADR and feature-matrix comparison; resolve Critical/Important findings and request a fresh review after any correction.
- [ ] Publish/update the Draft PR to `develop`, include focused and canonical local proof plus reviewer outcome, then mark ready and require exact-head hosted CI to pass before merge.
- [ ] Verify develop-push CI for the squash merge, fast-forward the primary checkout without disturbing its unrelated edit, and use `remove_worktree.py --check` then `--apply` before deleting/verifying the exact local and upstream branch refs.
