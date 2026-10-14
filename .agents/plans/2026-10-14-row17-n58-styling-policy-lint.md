# N-58 Enforce the Web Styling Policy Through ESLint

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Replace styling migration detectors and regex self-tests with the existing ESLint lane enforcing the repository's current authored TSX styling boundaries.

**Architecture:** `.agents/doctrine/frontend-standards.md` remains the authority: component-owned styling uses styled-components, Sass is limited to global concerns, and component styling does not use plain CSS class names. Use core ESLint selectors rather than a custom plugin or a test-only source scanner. Test the effective ESLint configuration against actual TSX snippets.

**Tech Stack:** ESLint 10 flat config, TypeScript ESLint parser, Vitest, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [whole-test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [frontend standards](../doctrine/frontend-standards.md).

**Execution Strategy:** `executing-plans` (Native inline). One style-policy boundary spans the existing ESLint config and one policy behavior suite. Retire scanner implementation self-tests and stale migration assertions together; preserve the authored styling policy using the lint command already required by local and hosted gates.

## Global Constraints

- Start from `origin/develop` in a fresh worktree created with the bundled `using-git-worktrees/scripts/new_worktree.py`; target the PR to `develop`.
- Promote N-57 and retire its completed plan in N-58's first substantive commit; advance only the authored version in `Directory.Build.props` from `0.1.0-dev.175` to `0.1.0-dev.176`.
- Keep the existing frontend standards: styled-components own component styling, global Sass owns only reset/tokens/base defaults, and no plain class styling or inline style props appear in TSX.
- Do not add a custom ESLint plugin, test-only filesystem scanner, source-file inventory, or historical filename/deletion detector.
- Do not rework styling architecture, migrate components, change CSS/Sass assets, or alter application behavior.
- Use a Vitest policy suite against ESLint's effective project configuration and `py -3 tools/run.py web --check`; normal commits pass the staged check-only hook.

## Review Focus

- The validator must reject representative forbidden TSX constructs and permit styled-component composition.
- Tests must exercise the actual ESLint config/rule output, not repeat selector construction or parser helper logic.
- Historic stylesheet absence/reference assertions and the hard-coded retired-class inventory are removed; they are not replaced by a new path or class inventory.
- No ADR or feature-matrix change is expected because the current styling decision and product behavior remain unchanged.
- Follow the scoped [unslop playbook](../playbooks/unslop.md), [code-style playbook](../playbooks/code-style.md), [testing playbook](../playbooks/testing.md), [decision-record playbook](../playbooks/decision-records.md), [PR runbook](../runbooks/pr.md), and [code-review runbook](../runbooks/code-review.md). The review remains a fresh whole-branch review.

---

### Task 1: Promote N-57 and establish N-58

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n58-styling-policy-lint.md`
- Delete: `.agents/plans/2026-10-14-row17-n57-player-projection-test-truth.md`

**Interfaces:**
- Consumes: merged PR #360 into `develop`, reviewed source `ad11db9dfe024fedced898b6d5a930210c65a3c6`, squash merge `f7e32240d3ebda73f3fed692433588378ac599de`, PR-head gate run `38614538978`, develop-push gate run `38615382727`, clean independent review, primary checkout fast-forward, preserved user investigation edit, and verified removal of N-57's worktree and both branch refs.
- Produces: promoted N-57 delivery evidence, active N-58 at `0.1.0-dev.176`, and retirement of the completed N-57 plan.

- [ ] **Step 1: Revalidate N-57 integration and cleanup.** Confirm PR #360 is merged into `develop`, its source and merge SHAs match, both hosted gates succeeded on those commits, the merge SHA is contained in `origin/develop`, the primary checkout is at the merge, and N-57's worktree plus local/upstream refs are absent. Confirm the primary checkout's pre-existing investigation edit remains intact.
- [ ] **Step 2: Promote N-57, activate N-58, and advance the version.** Add PR #360, source and merge SHAs, clean independent review, exact-head and develop-push CI runs, and paired worktree/ref cleanup evidence to row 17. Set the active plan/version to N-58 and `0.1.0-dev.176`. Change only the authored version in `Directory.Build.props`.
- [ ] **Step 3: Retire the completed predecessor.** Remove the N-57 plan only after its delivery evidence has been promoted. Keep the baseline specification and investigation records.
- [ ] **Step 4: Commit successor planning first.** Run `git diff --check`, inspect the staged planning diff, and commit the roadmap, version, N-58 plan, and N-57 plan retirement as `docs: plan N-58 web styling policy lint` before editing ESLint or tests.

### Task 2: Move the declared styling policy into the lint gate

**Files:**
- Modify: `src/WildBunch.Web/eslint.config.js`
- Modify: `src/WildBunch.Web/src/tests/stylingEnforcement.test.ts`
- Inspect only: `.agents/doctrine/frontend-standards.md`, `.agents/playbooks/code-style.md`, `tools/README.md`, and the web command target.

**Interfaces:**
- Consumes: the existing frontend styling decision and ESLint invocation in the canonical web/style lanes.
- Produces: real lint errors for explicit `className` and `style` JSX attributes; styled-components composition remains clean; a behavior suite validates both outcomes through ESLint.

- [ ] **Step 1: Establish the linter behavior baseline.** Inspect the current config and direct web lint target. Confirm the authored TSX tree has no explicit `className` or `style` attributes. Run the existing styling enforcement test once to establish its baseline.
- [ ] **Step 2: Add focused core-rule restrictions.** In the existing flat config, configure `no-restricted-syntax` selectors for explicit JSX `className` and explicit JSX `style`, each with an actionable message that points to styled-components. Avoid banning attribute-name text in strings or comments.
- [ ] **Step 3: Replace the scanner tests with effective-config behavior cases.** Delete the legacy class list, deleted-file/reference checks, helper matcher tests, recursive directory scanner tests, and their filesystem setup. Add focused Vitest cases that invoke the project's actual ESLint configuration on in-memory TSX snippets: each forbidden construct returns its intended diagnostic, while a styled-component using supported props produces no policy diagnostic. Keep test output scoped to the policy rule under assertion.
- [ ] **Step 4: Prove the negative gate and positive cases.** Verify each invalid fixture fails for its named styling diagnostic and the allowed examples pass. Temporarily disable each configured selector and confirm the corresponding negative case fails, restoring config after each mutation. No production component or style asset is mutated.
- [ ] **Step 5: Update the audit disposition.** Add a dated N-58 disposition to the test-quality and web test follow-up records: old migration/name scanners are retired, the existing styling authority is now executable in ESLint, and valid/invalid examples exercise the actual configured lint boundary. No ADR or PG evidence changes.
- [ ] **Step 6: Validate and commit the intended diff.** Run the focused new Vitest file, `py -3 tools/run.py lint --check src/WildBunch.Web/eslint.config.js src/WildBunch.Web/src/tests/stylingEnforcement.test.ts`, then `py -3 tools/run.py web --check`. Inspect `git diff --check`, ensure only the ESLint config, policy test, and dated investigation records changed, and commit as `test: enforce web styling policy in eslint`.

### Task 3: Review, publish, integrate, and retire N-58

**Files:**
- Review: complete merge-base diff, N-57 promotion, styling policy and test evidence, current frontend standards, and exact validation results.
- Modify: the roadmap with N-58 delivery evidence only in its next successor's first substantive commit.

**Interfaces:**
- Consumes: committed ESLint policy enforcement, allowed/rejected behavior cases, and normal check-only hook validation.
- Produces: N-58 integrated into `develop` at `0.1.0-dev.176` with independent whole-branch review and passing hosted gates.

- [ ] **Step 1: Check the exact worktree and diff.** Verify version `0.1.0-dev.176`, clean committed state, effective ESLint behavior cases, web lint, and hooked canonical validation on the final implementation head.
- [ ] **Step 2: Obtain independent review.** Dispatch a fresh-context reviewer for the full merge-base diff, including independent unslop and frontend-styling-policy review. Resolve actionable findings with behavior evidence and repeat review on the changed head.
- [ ] **Step 3: Publish and verify.** Push the branch, create a Draft PR to `develop`, verify body/base/head and exact source SHA, attach it when supported, mark it ready after review, and require the exact-head hosted gate to pass.
- [ ] **Step 4: Integrate and clean up.** Squash-merge to `develop`, require the develop-push gate on the merge SHA to pass, fast-forward the primary checkout while preserving its pre-existing investigation edit, then remove this clean worktree with the `finishing-a-development-branch` helper. Delete the exact local and upstream refs after worktree removal and verify all three absences.

## Acceptance

- The canonical ESLint gate enforces the existing no-plain-class/no-inline-style rules in authored TSX without a new custom plugin or source scanner.
- Policy tests reject each prohibited construct through the real ESLint config and accept styled-components composition.
- Historic migration detectors, class-name inventory and scanner implementation self-tests are retired; no source/asset/application behavior changes.
- The frontend styling decision remains unchanged, with no new ADR or product feature claim.
- N-57 is promoted and retired only after its exact PR, review, hosted gates, fast-forward and paired cleanup facts are verified.
- N-58 is independently reviewed, hosted CI passes on PR head and develop merge SHA, and its worktree plus local/upstream refs are removed together.
