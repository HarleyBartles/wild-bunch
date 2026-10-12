# Row 17 N-06 Discovered Suspect Projection Proof Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Application query test prove that a casebook returns only genuinely discovered suspects when undiscovered suspects exist in the same case.

**Architecture:** The Domain `CaseFile` owns its roster and discovered-suspect membership; `GameSessionMapper` projects only `CaseFile.GetDiscoveredSuspects()` into the player DTO. This slice changes no production behavior and strengthens the Application test fixture so an incorrect projection of the full roster becomes observable.

**Tech Stack:** C#/.NET 10, xUnit, `GetGameSessionHandler`, `GameSessionMapper`, and the existing Application test doubles.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; test finding N-06 is recorded in `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`.

**Execution Strategy:** `executing-plans` inline. Keep this test-only slice bounded to the discovered-suspect player query and its investigation/roadmap accounting; do not broaden it into API secrecy, projections generally, or other test-quality findings.

## Global Constraints

- Start from refreshed `origin/develop` in the dedicated `codex/row17-n06-fixture-reality` worktree.
- Keep `Directory.Build.props` as the only authored application version and advance once from `0.1.0-dev.120` to `0.1.0-dev.121` in the first substantive implementation commit, not this plan-only commit.
- In that first substantive commit, retire the completed N-05 `.120` plan and update the row-17 roadmap with PR #305's reviewed source/head, squash merge, matching tree, exact-head and develop-push gate results, and paired worktree/ref cleanup evidence. Preserve this current plan through its completing PR.
- Keep `CaseFile` as the authority for roster membership and discovered knowledge; do not change its behavior, `GameSessionMapper`, API contracts, or player promises.
- Compare the diff with ADR-0007 and ADR-0028. No ADR change is expected because this slice restores proof for existing hidden-truth and projection decisions. The feature matrix is unchanged because no capability or dependency changes.
- Use behavior-focused assertions against two real suspects, one discovered and one undiscovered. Prove the assertions fail if the mapper exposes the full roster; do not rely on the test name, call counts, source text, or fixture shape alone.
- Never use `--no-verify`; commit only through the normal staged-snapshot hook and fail-fast repository gate.

## Task 1: Record and commit the JIT plan

**Files:**
- `.agents/plans/2026-10-12-row17-discovered-suspect-projection-proof.md`

- [x] Confirm N-06 against the current investigation, query test, mapper and available player/dev secrecy tests.
- [x] Commit this plan alone without advancing the application version.

## Task 2: Make the undiscovered-suspect exclusion observable

**Files:**
- `tests/WildBunch.Application.Tests/Handlers/GetGameSessionHandlerTests.cs`
- `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- `.agents/plans/2026-10-12-row17-concurrency-retry-proof.md`
- `Directory.Build.props`

- [x] Replace the one-suspect factory in `GetGameSessionProjectsOnlyExplicitlyDiscoveredSuspects` with a case containing two named suspects and discover exactly one before querying.
- [x] Assert the fixture has two roster members and one discovered member, then assert the DTO contains the discovered suspect and excludes the real undiscovered suspect by name and serialized payload.
- [x] Temporarily change the mapper to project the full case roster and confirm this test fails because the hidden suspect appears; restore the mapper and rerun the focused test.
- [x] Record a dated N-06 disposition for the already-satisfied misleading dev-test half: the dedicated method was removed in commit `02fd3e66`, its positive developer assertions remain in `HandleAsync_ReturnsSaloonContext_WithHiddenTruthAndSuspects`, and player DTO secrecy remains covered at the Application mapping boundary. Record this slice's correction of the remaining fixture defect without rewriting the original finding.
- [x] Update the row-17 roadmap with PR #305 delivery evidence and paired cleanup verification, name N-06 as the active successor at `.121`, and retire the completed N-05 plan in this same first substantive commit.
- [x] Advance the authored version once to `0.1.0-dev.121` and run the focused test plus the complete Application suite before committing through the normal hook.

## Task 3: Review, publish, merge and retire the execution unit

**Files:**
- Review the actual branch diff against ADR-0007, ADR-0028, the feature matrix, N-06 and applicable unslop guards.

- [ ] Independently inspect the actual diff for the player-knowledge boundary, fixture adversariality and truthful investigation/roadmap accounting; resolve any source-truth discrepancy in the same PR.
- [ ] Obtain a fresh whole-branch review and resolve findings with focused re-review.
- [ ] Publish a Draft PR to `develop`, complete author/reviewer ADR and feature-matrix checks, mark ready, and require the exact-head canonical hosted gate to pass.
- [ ] Squash merge under the already-authorized epic flow; verify the develop-push gate passes and the merged tree matches the reviewed tree.
- [ ] Fast-forward the primary checkout. Verify the merged PR head, remove the clean worktree first, explicitly delete its exact local and upstream refs, and verify all three are absent before the next plan worktree.
- [ ] Preserve later row-17 findings for JIT successor plans; do not claim row 17 complete.
