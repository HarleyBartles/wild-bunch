# N-67: Reconcile Map Accessibility Test Inventory

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the stale 0.1.0 map-level keyboard/screen-reader selection obligation from the current test inventory while preserving the accepted pointer-selection behavior and renderer lifecycle evidence.

**Architecture:** This is a documentation-only correction to the current test inventory. The baseline specification and ADR-0046 remain authoritative: the shared map uses pointer selection for 0.1.0, with each context applying its legal town set, and map-level keyboard/screen-reader selection is excluded. No source, test, product capability, feature-matrix entry, or durable decision changes.

**Tech Stack:** Markdown, Git, the repository command bus, GitHub pull requests.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [whole-test investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [ADR-0046](../../docs/decisions/ADR-0046-pointer-operated-town-selection-for-0.1.0.md).

**Execution Strategy:** `executing-plans` (Native inline). The slice is one dependent documentation correction with successor-plan retirement, version advancement, and publication closeout; inline execution keeps those changes and their evidence together, followed by one independent whole-branch review.

## Global Constraints

- Start from refreshed `origin/develop` in this fresh canonical worktree and target the PR to `develop`.
- Promote N-66 PR #369, retire its completed plan, and advance the sole authored application version once from `0.1.0-dev.184` to `0.1.0-dev.185` in `Directory.Build.props`.
- Preserve the single shared pointer-operated world map and each context's legal-town rules; map-level keyboard/screen-reader town selection remains excluded from 0.1.0.
- Change no production source, tests, ADR, product promise, feature-matrix entry, or user-owned primary-checkout edit.
- Keep planning artifacts in `.agents/plans/`; scratch review artifacts belong under the branch-specific scratch root.
- The pre-commit hook and hosted CI validate without mutating or staging the candidate; run canonical checks fail-fast and cheapest-first.
- Before the next plan, verify merge and hosted gates, fast-forward the primary checkout while preserving its existing user edit, then remove this worktree and its exact local/upstream refs together.

## Review Focus

- Confirm the only changed inventory row describes existing pointer selection, eligible-town rejection, React selection state, and renderer lifecycle without implying keyboard or screen-reader map selection is a 0.1.0 obligation.
- Confirm Town Hub keyboard controls and dialog/focus accessibility remain in scope where supported; the correction applies only to selecting towns on the map.
- Confirm the successor retires N-66 only after matching its merged PR, exact-head and develop-push gates, and worktree/ref cleanup evidence.

---

### Task 1: Promote N-66 and prepare N-67 delivery

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-15-row17-n66-start-map-data-handoff.md`

**Interfaces:**
- Consumes: PR #369 merged to `develop` from reviewed head `1124bc2bf66a9d1a327eab3f8e5ef23fface2764`, merge `3457931a27cc5b60899f9a2745628c194ca3bd37`, exact-head hosted gate `38660968150`, develop-push gate `38661360213`, clean independent review, and verified N-66 worktree plus paired local/upstream ref retirement.
- Produces: promoted N-66 delivery evidence, the already committed N-67 plan as the active roadmap slice at `0.1.0-dev.185`, and retirement of the completed N-66 plan.

- [ ] Verify PR #369 is merged to `develop`; its head and merge identities match the values above; both hosted gates passed; the merge is in refreshed `origin/develop`; the primary checkout preserves the user's local note; and the N-66 worktree plus exact local/upstream refs are absent.
- [ ] Add PR #369 and both hosted gate links to row 17's delivery references, make the committed N-67 plan the active roadmap slice, and record N-66's disposition and paired cleanup in a dated roadmap delivery note.
- [ ] Advance only `Directory.Build.props` from `0.1.0-dev.184` to `0.1.0-dev.185`; delete the N-66 plan and commit the promotion, version update, and retirement together before changing the web-test inventory.
- [ ] Review the staged planning diff, confirm no generated version copies are tracked or edited, run `git diff --check`, and commit through the normal staged-candidate hook.

### Task 2: Correct the current map-selection test inventory

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`

**Interfaces:**
- Consumes: the 0.1.0 map and accessibility scope in the baseline specification, ADR-0046, existing `PhaserMapHost.test.tsx` pointer/eligibility/selection/lifecycle behavior, and the N-55 and N-63 dated dispositions.
- Produces: a truthful current disposition for `PhaserMapHost.test.tsx` and a dated explanation of the corrected stale map-accessibility phrase.

- [ ] Replace the current PhaserMapHost inventory phrase “cover renderer lifetime under the shared pointer and accessible-selection contract” with its actual retained owners: eligible and ineligible pointer intent, React-owned selection state, callback freshness, resize behavior, and cleanup; explicitly state that map-level keyboard/screen-reader town selection is excluded for 0.1.0.
- [ ] Add a dated N-67 disposition explaining that the prior inventory phrase conflicted with the settled ADR-0046/spec scope; retain town-hub keyboard controls and applicable dialog/focus behavior as separate supported contracts.
- [ ] Leave the historical 2026-10-11 accessibility proposal and later ADR-0046 correction intact; do not add map accessibility code or tests, change existing pointer tests, or imply no keyboard access is required outside map town selection.
- [ ] Compare the changed inventory entry with the live `PhaserMapHost.test.tsx` and its implementation; verify the wording makes no claim beyond tests that actually execute.

### Task 3: Validate and commit the inventory correction

**Files:**
- Review: the complete candidate from Tasks 1-2.

**Interfaces:**
- Consumes: the committed N-67 plan, roadmap promotion, version identity, and corrected web-test inventory.
- Produces: a reviewed documentation-only change whose staged-candidate hook is green and which contains no generated or runtime behavior changes.

- [ ] Inspect the complete diff and verify it contains only N-66 promotion/retirement, the single version increment, the N-67 plan, and the narrow stale-inventory correction.
- [ ] Run `git diff --check`; stage the intended files explicitly and inspect the staged diff.
- [ ] Commit through the repository's check-only staged-candidate hook; use its fail-fast canonical gate as the validation result and do not run a duplicate full gate immediately before or after a successful hooked commit.
- [ ] Verify the committed tree and working tree are clean, and verify `Directory.Build.props` is the only authored application-version change.

### Task 4: Review and deliver N-67

**Files:**
- Review: the complete N-67 branch against `origin/develop`.

- [ ] Prepare a whole-branch review package against the plan, spec, N-55/N-63/N-66 dispositions, ADR-0046, the web-test inventory, feature matrix, and code-review/play-surface unslop profiles. Obtain an independent fresh-context whole-branch review; resolve any actionable finding and request a fresh review of the corrected head.
- [ ] Run `git diff --check`, confirm the plan's feature-matrix and ADR checks remain no-change decisions, push the branch, and create a Draft PR to `develop` with the exact scope and validation evidence.
- [ ] Verify PR body, base, head SHA, and Draft state; mark ready after clean review and require the exact-head hosted canonical gate to pass.
- [ ] Squash-merge under the active goal authorization and require the develop-push gate to pass on the merge SHA.
- [ ] Fast-forward the primary checkout without disturbing the user's existing investigation edit. Verify the merged tree, then remove this clean worktree with the worktree-removal helper, delete only its exact local/upstream refs, and verify all three absences before the next plan.

## Acceptance

The current test inventory no longer implies map-level keyboard/screen-reader town selection is required for 0.1.0. Existing pointer selection, legal eligibility, current selection, renderer resize, and cleanup evidence remains accurately described. N-66 is promoted and retired only after verified delivery and paired cleanup, and N-67 merges as `0.1.0-dev.185` with exact-head and develop-push hosted gates green.
