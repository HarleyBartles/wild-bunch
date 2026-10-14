# N-62 Prove action predicate routing boundaries

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove each retained web action predicate accepts its own action kind and rejects a different live action kind, so a false-positive route cannot pass only on positive examples.

**Architecture:** Action predicates remain small presentation-routing adapters over the backend-provided `AvailableActionDto.kind`. This slice strengthens behavior tests only; it does not add predicates, action kinds, route policy, or validation of malformed DTOs.

**Tech Stack:** TypeScript, Vitest, and the Wild Bunch web validation commands.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [whole-test quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [frontend standards](../doctrine/frontend-standards.md); [validation policy](../doctrine/validation-policy.md).

**Execution Strategy:** `executing-plans` (Native inline). This is one compact test owner and one dated investigation correction. The same three predicate contracts and test fixture are shared across the whole slice; a fresh implementer per step would duplicate context without creating independent deliverables.

## Global Constraints

- Start from refreshed `origin/develop` in a fresh worktree created with the bundled `using-git-worktrees/scripts/new_worktree.py`; target the PR to `develop`.
- Promote N-61 delivery evidence in the first commit, retire its completed plan, and advance only the authored version in `Directory.Build.props` from `0.1.0-dev.179` to `0.1.0-dev.180`.
- Test only the three currently retained predicates: wanted posters (`ReadWantedPosters`), local gossip (`GatherLocalGossip`), and saloon observation (`LookAroundSaloon`).
- Use other currently defined action kinds as mismatches; do not restore predicates for retired local-record or telegraph actions.
- Preserve production behavior, public action kinds, player features, ADR truth, and feature-matrix claims.
- Run focused Vitest and the canonical fail-fast `py -3 tools/run.py ci --check`; do not wait for downstream tests after an earlier formatting or lint failure.

## Review Focus

- Each predicate's negative case uses a distinct currently valid `AvailableActionKind`, so replacing that predicate with an always-true implementation fails its own test.
- Investigation wording must describe current repository predicates and must not retain the removed local-record, telegraph-lead, or nonexistent hook-test findings as live scope.

---

### Task 1: Promote N-61 and establish N-62

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n62-action-predicate-negatives.md`
- Delete: `.agents/plans/2026-10-14-row17-n61-dev-panel-recovery.md`

**Interfaces:**
- Consumes: merged PR #364, reviewed source `5d576cf5d3b1d0b81596f1968847dc6146d671d6`, squash merge `1faf6ea56524eb39e76b64aa584dc2771452c4c5`, exact-head gate `38640086991`, develop-push gate `38640935823`, clean independent review, and verified paired N-61 worktree/local/upstream-ref cleanup.
- Produces: promoted N-61 delivery evidence, active N-62 at `0.1.0-dev.180`, and retirement of the completed N-61 plan.

- [ ] **Step 1: Revalidate N-61 integration and cleanup.** Verify PR #364 is merged to `develop`, its recorded head and merge SHAs match, both hosted gates passed on those commits, the merge is contained in `origin/develop`, the primary checkout is at the merge with the pre-existing user edit intact, and the N-61 worktree plus exact local/upstream refs are absent.
- [ ] **Step 2: Promote N-61, activate N-62, and advance the version.** Add N-61's PR, source and merge SHAs, independent review, exact-head and develop-push gate runs, and paired worktree/ref cleanup evidence to row 17. Set the active plan/version to N-62 and `0.1.0-dev.180`. Change only the authored version in `Directory.Build.props`.
- [ ] **Step 3: Retire the completed predecessor.** Remove the N-61 plan only after its delivery evidence is promoted; keep the specification and investigation records.
- [ ] **Step 4: Commit successor planning first.** Check whitespace and inspect the staged planning diff, then commit the roadmap, version, N-62 plan, and N-61 plan retirement as `docs: plan N-62 action predicate negatives` before editing tests.

### Task 2: Prove retained action predicate routing

**Files:**
- Modify: `src/WildBunch.Web/src/tests/actionTypePredicates.test.ts`
- Review and change only if a behavior test finds a defect: `src/WildBunch.Web/src/utils/actionTypePredicates.ts`

**Interfaces:**
- Consumes: `AvailableActionDto.kind`, `AvailableActionKind`, `actionIsWantedPosters`, `actionIsGatherLocalGossip`, and `actionIsLookAroundSaloon`.
- Produces: one positive and one negative routing assertion for each retained predicate.

- [ ] **Step 1: Replace isolated positive-only examples with a compact behavior table.** For each retained predicate, provide its matching kind and a different currently defined kind: `ReadWantedPosters`/`BuySupplies` for wanted posters, `GatherLocalGossip`/`ReadWantedPosters` for local gossip, and `LookAroundSaloon`/`GatherLocalGossip` for saloon observation. Assert that the predicate returns true for its matching action and false for the mismatch; do not add arbitrary malformed-object cases.
- [ ] **Step 2: Run and mutation-prove the predicate cases.** From `src/WildBunch.Web`, run `npm exec -- vitest run src/tests/actionTypePredicates.test.ts`. Temporarily make each predicate always return true, one predicate at a time; verify that its own mismatching case fails, restore the source byte-for-byte, and rerun the focused test file.

### Task 3: Reconcile W-12 with current source and complete delivery

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Review: `docs/decisions/README.md`, applicable web/action-routing ADRs and `docs/features.md`

**Interfaces:**
- Consumes: the current three-predicate source inventory and passing mutation-proven behavior tests from Task 2.
- Produces: dated W-12 disposition describing the delivered negative coverage and corrected live scope.

- [ ] **Step 1: Correct the W-12 finding against current source.** State that three predicates remain and that the previously described local-record and telegraph predicates are retired; remove the nonexistent `beatNarrationHook.test.tsx` item after confirming no current file/test has that owner. Preserve unrelated behavior-test inventory rows.
- [ ] **Step 2: Record the dated W-12 disposition and inventory update.** Record the exact matching/mismatching cases and that each always-true mutation failed, with no production fix. Update the `actionTypePredicates.test.ts` inventory row. Do not create a development-receipt artifact or expand the finding into speculative malformed-object behavior.
- [ ] **Step 3: Resolve author decision and feature checks.** Compare the actual diff with current action-routing decisions and the relevant feature-matrix entries. State in the PR that this test-only correction changes no durable decision, 0.1.0 capability, or deployment boundary; do not edit ADRs or the feature matrix unless the diff reveals a real current mismatch.
- [ ] **Step 4: Validate the final tree.** Run `npm exec -- prettier --check src/tests/actionTypePredicates.test.ts` and `npm exec -- eslint --config eslint.config.js src/tests/actionTypePredicates.test.ts`, then from the repository root run `git diff --check` and `py -3 tools/run.py ci --check`. Preserve mutation logs only in the branch-scoped `_agent-scratch` task ledger.
- [ ] **Step 5: Obtain an independent whole-branch review.** Review against the testing and action-routing scope, applicable unslop profile, relevant ADRs, and feature matrix. Fix actionable issues with behavior proof and obtain a fresh clean review.
- [ ] **Step 6: Publish and verify.** Push the branch and create a Draft PR to `develop`; verify PR body, base, head and SHA. Mark ready after clean review and require exact-head hosted CI to pass.
- [ ] **Step 7: Integrate and close out.** Squash-merge to `develop`, require the develop-push gate on its merge SHA, fast-forward the primary checkout while preserving the user's edit, remove the clean worktree with the `finishing-a-development-branch` helper, delete the exact local/upstream refs, and verify all are absent.

## Acceptance

- All three retained action predicates accept their matching action and reject a different valid action kind.
- Each negative assertion fails when its owning predicate is temporarily changed to always true; production source is restored byte-for-byte afterward.
- W-12 and the relevant test inventory describe current source and no longer present removed predicates or absent test files as live scope.
- No player behavior, action contract, event, durable decision, feature promise, or deployment boundary changes.
- N-61 delivery is promoted and its plan retired only after PR #364, both hosted gates, independent review, primary fast-forward, and paired cleanup are verified.
- N-62 is independently reviewed, both hosted gates pass on their exact commits, and its worktree plus exact branch refs are removed together.
