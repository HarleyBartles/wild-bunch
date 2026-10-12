# Prove Salt Changes Clue Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan inline, task by task.

**Goal:** Make the salt-variation test prove that changing only the salt can change which clue is selected for one identical investigation input.

**Architecture:** This is a Domain behavior-test correction. `ClueSurfacingResolver` remains the owner of deterministic clue selection; no production behavior or persistence changes are planned.

**Tech Stack:** .NET 10, xUnit, repository command bus.

**Spec:** [Stable 0.1.0 test-quality investigation, N-09](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md#n-09-salt-and-map-tests-overclaim-countnon-null-assertions)

**Execution Strategy:** `executing-plans` - one falsifiable resolver assertion and its evidence/disposition updates form a single bounded test-quality slice.

## Global Constraints

- Start from current `origin/develop` in this fresh canonical worktree and deliver one PR to `develop`.
- Keep the plan-only commit at `0.1.0-dev.123`; advance the sole authored version in `Directory.Build.props` to `0.1.0-dev.124` in the first substantive implementation commit.
- Retire the N-08 plan in the first substantive implementation commit; retain this N-09 plan through its delivery and retire it in the next successor slice.
- Change the salt test to compare two salts at the same surface, town slot, and visit count, and require two non-null, different clue IDs. Keep deterministic same-input, surface-tag, boring-mode, and already-known behavior tests.
- Prove the new assertion fails when the resolver ignores salt, restore `ClueSurfacingResolver.cs` exactly, and rerun the focused test successfully.
- N-09's dev-override tests were already retired because developer salt controls are outside the 0.1.0 candidate. Do not restore or replace them.
- The map-test concerns are already addressed in merged commit `bf5899b6` (PR #245): `GetWorldMapHandlerTests` compares every projected town/trail field and owns the missing-session negative, while the former split starting-map handlers are gone. Record this current-tree disposition; do not add a duplicate map assertion.
- No production implementation, player feature, ADR decision, or feature-matrix truth changes in this slice.
- Before review, compare the actual diff with ADRs and the feature matrix. After merge, require passing exact-head and develop-push gates, fast-forward the primary checkout, and remove the clean worktree before deleting and verifying its exact local and upstream branch refs.

## Review Focus

- The salt test must hold every input except salt constant and fail if salt is ignored; iterating across town/visit values is not evidence that salt affects selection.
- The map portion of the old N-09 finding is resolved by current mapped-field coverage; do not freeze a count, duplicate map route, or invent a test for an absent handler.
- Confirm no gameplay implementation, ADR, or feature-matrix disposition changes.

---

### Task 1: Prove salt variation at one identical input

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/ClueSurfacingResolverTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row17-compiler-smoke-retirement.md`
- Modify: `.agents/plans/2026-10-12-row17-n09-salt-selection-proof.md`

**Interfaces:**
- Consumes: `ClueSurfacingResolver.Resolve` with a `CaseFile` containing multiple `LocalGossip` clues.
- Produces: a behavior assertion that distinct fixed salts at identical resolver inputs select distinct eligible clue IDs.

- [x] Replace the current salt-variation loops with two calls at identical `LocalGossip`, `townSlotIndex: 0`, and `visitCount: 0` inputs, using the existing fixed `salt-a` and `salt-b` values; assert both results are non-null and their IDs differ.
- [x] Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~SaltMode_DifferentSaltCanSelectDifferentClue` and confirm the focused test passes.
- [x] Temporarily bypass the salt contribution in `ClueSurfacingResolver.Resolve` so identical town/visit inputs choose the same eligible index regardless of salt; rerun the focused test and confirm it fails because the selected IDs match. Restore the production file exactly, confirm its diff is empty, and rerun the focused test successfully.
- [x] Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj` and compare the result with the repository's existing Domain baseline.
- [x] Add a dated N-09 investigation disposition: record that the dev-override orchestration was retired on 2026-10-08, the map projection/count findings were already resolved by PR #245 / commit `bf5899b6`, and this slice replaces only the salt test's cross-input variation with a fixed-input salt comparison. Preserve the unknowns and do not claim this slice changed the map contract.
- [x] Update roadmap row 17 with N-08 delivery facts: reviewed source `a314415c83f6105d8e8fd5407e3ae05acaf5fad6`, merge `da482b5d8b7ec210620d9b86e616e7e1346181bf`, matching tree `b14423143f82a51488f25cf6df30562628215645`, exact-head gate `38307782092` passed, develop-push gate `38308230470` passed, worktree and local/upstream refs removed and verified. Set N-09 active at `0.1.0-dev.124`; keep row 17 incomplete.
- [x] Set the sole authored version to `0.1.0-dev.124`, retire the N-08 plan in this first substantive implementation commit, and retain this plan for retirement in the N-10 successor. Use the normal staged-snapshot hook; do not run a redundant full gate immediately around a successful hooked commit.

### Task 2: Review, publish, merge, and clean the execution unit

**Files:**
- Review the full branch diff against the N-09 investigation, ADRs, the feature matrix, and the Review Focus.

- [ ] Verify the fixed-input salt assertion can fail under the salt-ignored mutation and passes after exact restoration; verify the map coverage and dev-override dispositions against current tests and Git history.
- [ ] Obtain a fresh whole-branch review, resolve actionable findings with focused red/green evidence, and rerun the canonical gate through a normal hooked commit if the tree changes.
- [ ] Publish PR `test: prove salt changes clue selection` to `develop`, independently confirm the author's ADR and feature-matrix checks, mark it ready, and require the exact-head canonical hosted gate to pass.
- [ ] Squash merge under the authorized epic flow; verify the develop-push gate passes and the merged tree equals the reviewed source tree.
- [ ] Fast-forward the primary checkout. Verify the merged PR head, remove the clean worktree first, explicitly delete the exact local and upstream refs, and verify all three are absent before creating the next plan worktree.
- [ ] Keep this plan for the next successor slice to retire; do not claim row 17 complete while N-10 and later findings remain.
