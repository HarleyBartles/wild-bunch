# N-63 Correct W-11 presentation-only test claims

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove test assertions that mistake CSS/DOM construction and historical copy absence for player behavior, while keeping actual setup, town navigation, map selection and travel-preparation evidence intact.

**Architecture:** This is a test-quality correction only. React and styled-components source remain unchanged. Existing user-facing behavior belongs to its current component and routing owners; jsdom assertions do not certify visual appearance or exact CSS composition.

**Tech Stack:** TypeScript, React Testing Library, Vitest, Prettier, ESLint, and the Wild Bunch web validation command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [whole-test quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web-test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [frontend standards](../doctrine/frontend-standards.md); [validation policy](../doctrine/validation-policy.md).

**Execution Strategy:** `executing-plans` (Native inline). One presentation-only test finding spans three focused web test owners and their shared dated investigation disposition; no production change or independent deliverable warrants separate plans.

## Global Constraints

- Start from refreshed `origin/develop` in a fresh worktree created with `using-git-worktrees/scripts/new_worktree.py`; target the PR to `develop`.
- Promote N-62 delivery evidence in the first commit, retire its completed plan, and advance only the authored version in `Directory.Build.props` from `0.1.0-dev.180` to `0.1.0-dev.181`.
- Remove only the exact toggle DOM/CSS assertions, redundant legacy-label exclusions, and historic destination-copy absence assertions identified by W-11. Preserve accessible labels, option selection, form validation, visible navigation, route dispatch, town eligibility, focusability, tab-order, map-selection, preview and departure behavior.
- Rename the focus test so its claim matches its assertions. It verifies focus and tab order, not the visual styling of a focus indicator.
- Do not add browser/screenshot infrastructure for this cleanup. No visual requirement or product behavior is being changed.
- Do not change React source, product copy, map accessibility scope, ADRs, or the feature matrix. If current source reveals a real durable-decision or product-scope mismatch, stop and route that decision to brainstorming rather than silently widening this test correction.
- Run focused Vitest, focused Prettier/ESLint, and the canonical fail-fast `py -3 tools/run.py ci --check` on the committed candidate.

## Review Focus

- Removed assertions only fixed incidental DOM/CSS details or checked old copy absence; retained assertions continue to exercise actual user choices, navigation and supported keyboard and travel behavior.
- Removed legacy-label exclusions are independently implied by exact ordered label equality; that positive option contract remains covered.
- The renamed focus test no longer claims that jsdom proves a visible focus treatment.
- W-11 dated dispositions describe the exact current source/test scope and do not claim a new visual, accessibility, product, or architecture guarantee.
- N-62's merge, hosted gates, review, fast-forward and paired worktree/ref cleanup evidence is accurately promoted before retiring its plan.

---

### Task 1: Promote N-62 and establish N-63

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n63-w11-presentation-proof.md`
- Delete: `.agents/plans/2026-10-14-row17-n62-action-predicate-negatives.md`

**Interfaces:**
- Consumes: merged PR #365, reviewed source `b4abead934c084acd183f3a8b11790d2312641c6`, squash merge `a2cd422c8c8625b6739eddf3b6f1912db7c472af`, exact-head gate `38644300134`, develop-push gate `38644876867`, clean independent review, and verified paired N-62 worktree/local/upstream-ref cleanup.
- Produces: promoted N-62 delivery evidence, active N-63 at `0.1.0-dev.181`, and retirement of the completed N-62 plan.

- [x] Verify PR #365 is merged to `develop`, its recorded head and merge SHAs match, both hosted gates passed on those exact commits, the merge is contained in refreshed `origin/develop`, the primary checkout is current with the user's edit preserved, and the N-62 worktree plus exact local/upstream refs are absent.
- [x] Update row 17's active plan/version and append PR #364/#365 to its delivery references. Add a dated N-62 delivery note with review, exact hosted-gate and paired-cleanup evidence.
- [x] Advance only `Directory.Build.props` to `0.1.0-dev.181`; create this plan and delete the completed N-62 plan.
- [x] Inspect the staged planning diff and `git diff --check`, then commit roadmap, version, plan and predecessor retirement before changing tests.

### Task 2: Remove presentation-shape assertions while retaining behavior owners

**Files:**
- Modify: `src/WildBunch.Web/src/tests/SetupHuntStep.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/TownHubSurface.test.tsx`

**Interfaces:**
- Consumes: the setup component's accessible difficulty/entropy selectors and TownHubSurface's Phaser host plus visible fallback navigation.
- Produces: tests whose claims observe user behavior and do not pin incidental styling or historical markup.

- [x] In `SetupHuntStep.test.tsx`, remove the test that queries the single `aria-hidden` thumb and asserts transparent button computed backgrounds; remove the test that asserts the segmented controls' computed `flexWrap` value. Keep the real selection, accessible labels, name validation, busy/retry, seed and start-action tests.
- [x] In `TownHubSurface.test.tsx`, rename the Phaser host smoke to claim only that the town hub creates a Phaser game, and remove its comments/assertions about old card descriptions being absent. Keep route-selection and visible keyboard-fallback behavior.
- [x] Rename the focus test to state that fallback buttons receive focus and Tab advances to the next building. Remove wording that claims visual focus-indicator verification; preserve the actual `.toHaveFocus()` assertions and nav visibility check.
- [x] Run `npm exec -- vitest run src/tests/SetupHuntStep.test.tsx src/tests/TownHubSurface.test.tsx` from `src/WildBunch.Web`. Confirm the remaining cases still pass without changing production source.

### Task 3: Retire the remaining TravelPrepSurface copy detector

**Files:**
- Modify: `src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx`

**Interfaces:**
- Consumes: the current Phaser map rendering, destination selection, preview and travel-preparation behavior owners.
- Produces: no test that only checks absence of a retired text-list phrase.

- [x] Remove the `does not render the old text destination list` test and its historical-copy comment. Keep the adjacent Phaser map-rendering proof and the existing destination selection, preview, error/retry and departure behavior tests.
- [x] Run `npm exec -- vitest run src/tests/TravelPrepSurface.test.tsx` from `src/WildBunch.Web` and confirm all remaining current behavior cases pass.

### Task 4: Remove redundant legacy-label exclusions

**Files:**
- Modify: `src/WildBunch.Web/src/tests/SetupHuntStep.test.tsx`

- [x] Remove only the five redundant `not.toContain` checks for legacy difficulty/entropy labels; the existing exact ordered label arrays independently reject those values. Keep the exact positive label/order expectations and the value-selection behavior tests.
- [x] Run `npm exec -- vitest run src/tests/SetupHuntStep.test.tsx` from `src/WildBunch.Web` and confirm the real setup interactions remain green.

### Task 5: Date the W-11 disposition and validate the full candidate

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Review: `docs/decisions/README.md`, applicable frontend/map ADRs, `docs/features.md`

**Interfaces:**
- Consumes: the actual three-file test diff and focused Vitest results.
- Produces: dated W-11 disposition at the original finding and web follow-up owner; no new product, ADR or visual requirement.

- [x] Record exactly which toggle construction, redundant label exclusions, and historic-copy assertions were removed from `SetupHuntStep`, `TownHubSurface` and `TravelPrepSurface`, which interaction/keyboard/map/travel behaviors remain, and why the jsdom tests do not establish visual treatment. Do not create a test-result receipt or claim browser visual proof.
- [x] Compare the test-only diff with current frontend/map decisions and PG-003. State in the PR that the slice preserves the current capability and map-accessibility boundary; do not alter an ADR or feature entry without evidence of a real mismatch.
- [x] Run focused Prettier and ESLint on all three changed test files, then from the repository root run `git diff --check` and `py -3 tools/run.py ci --check`. Keep any transient logs in branch-scoped `_agent-scratch`.

### Task 6: Independent review and delivery

- [ ] Prepare the branch review package and obtain an independent whole-branch review against this plan, W-11, the play-surface unslop profile, relevant ADRs, and the feature matrix. Fix actionable findings with behavior evidence and request a fresh review.
- [ ] Push the branch, create a Draft PR to `develop`, and verify PR body, base, head and SHA. Mark ready after the clean review; require exact-head hosted CI to pass.
- [ ] Squash-merge to `develop` under the active goal authorization and require the develop-push gate to pass on its merge SHA.
- [ ] Fast-forward the primary checkout while preserving the user's existing investigation edit. Verify the merged tree, then remove this clean worktree with the finishing helper, delete only its exact local/upstream branch refs, and verify all three are absent before the next plan.

## Acceptance

- The two setup-toggle DOM/CSS shape checks, five redundant legacy-label exclusions, and stale TownHubSurface/TravelPrepSurface copy-absence checks are gone.
- Actual setup choices, validation, town routes, eligible fallback controls, visible navigation, focusability, tab order, map selection, preview, and departure remain behavior-tested.
- Test names and dated W-11 dispositions state only what their assertions establish.
- Production source, game behavior, map-accessibility scope, ADR decisions and feature promises are unchanged.
- N-62 is promoted and its plan retired only after the merged delivery and cleanup evidence is verified.
- N-63 receives independent review, passes exact-head and develop-push hosted CI, and its worktree plus exact branch refs are removed together.
