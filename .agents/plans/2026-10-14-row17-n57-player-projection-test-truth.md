# N-57 Player Projection Test Truth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove frontend secrecy assertions whose fixtures contain no secret to protect, while retaining real player display contracts and proving that case summary belongs in the casebook rather than the journal.

**Architecture:** Player-safe field filtering remains owned by the Application/API projection boundary under ADR-0007. The web layer renders supported DTO fields and does not scrub arbitrary server-authored prose. This slice changes tests and evidence only; production behavior remains unchanged.

**Tech Stack:** React, TypeScript, Vitest, Testing Library, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [whole-test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [PG-008](../../docs/features.md#pg-008-keep-a-casebook-and-journal); [ADR-0007](../../docs/decisions/ADR-0007-hidden-culprit-truth-and-hidden-progress-boundaries.md).

**Execution Strategy:** `executing-plans` (Native inline). One concern spans four existing player-surface test owners and their dated evidence. A single inline implementation keeps the distinction between server projection secrecy, casebook display and event-journal display together. Only the new positive case-summary assertion requires a temporary mutation proof.

## Global Constraints

- Start from `origin/develop` in a fresh worktree created with the bundled `using-git-worktrees/scripts/new_worktree.py`; target the PR to `develop`.
- Promote N-56 and retire its completed plan in N-57's first substantive commit; advance only the authored version in `Directory.Build.props` from `0.1.0-dev.174` to `0.1.0-dev.175`.
- Preserve ADR-0007: public API projections expose only learned/player-authorized facts. Keep that secrecy test at the actual Application/API boundary with adversarial source truth; do not assign the React surface an obligation to scrub arbitrary prose.
- Keep the summary's supplied display on the casebook and out of the event journal. Keep the journal's current town name and entry grouping; do not expose the internal town ID.
- Keep the prologue's visible heading/body/action and omit its actual `variantId` metadata from the rendered page. The server-owned template substitution remains tested at its Application/API owner.
- Do not change production source, event history, API contracts, ADR decisions, or unrelated clue/casebook behavior.
- Use the focused Vitest command while constructing the change and `py -3 tools/run.py web --check` for the full web validation lane. Normal commits must pass the staged check-only hook.

## Review Focus

- Every retained negative assertion must name a value actually present in its fixture and protect a user-visible display contract.
- The summary must be positively visible in the CaseFileSurface and absent in the AppShell journal overlay, with distinct content on the two surfaces.
- Journal hidden-marker absence assertions and the prologue placeholder test must be removed because their inputs already omit those alleged secrets; the backend/API tests retain that boundary.
- The story test's `variantId` omission remains because its DTO fixture supplies the metadata and the UI owns that visible omission.
- Compare the final diff with ADR-0007 and PG-008; update evidence narrowly without implying a complete casebook or deployed browser journey.
- Follow the scoped [unslop playbook](../playbooks/unslop.md), [testing playbook](../playbooks/testing.md), [decision-record playbook](../playbooks/decision-records.md), [feature-matrix playbook](../playbooks/feature-matrix.md), [PR runbook](../runbooks/pr.md), and [code-review runbook](../runbooks/code-review.md). The code-review lane remains a fresh whole-branch review.

---

### Task 1: Promote N-56 and establish N-57

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n57-player-projection-test-truth.md`
- Delete: `.agents/plans/2026-10-14-row17-n56-background-placement-assertions.md`

**Interfaces:**
- Consumes: merged PR #359 into `develop`, reviewed source `9fad67d56a9c5eebe0b9e519262ba46d42b95ef9`, squash merge `3107b5e980dea53502401c5c63915ca389a960bf`, PR-head gate run `38610455091`, develop-push gate run `38611023525`, clean independent review, primary checkout fast-forward, and verified removal of N-56's worktree and both branch refs.
- Produces: promoted N-56 delivery evidence, active N-57 at `0.1.0-dev.175`, and retirement of the completed N-56 plan.

- [ ] **Step 1: Revalidate N-56 integration and cleanup.** Confirm PR #359 is merged into `develop`, its source and merge SHAs match, both hosted gates succeeded on those commits, the merge SHA is contained in `origin/develop`, the primary checkout is at the merge, and N-56's worktree plus local/upstream refs are absent. Confirm the existing AppShell investigation edit is still preserved in the primary checkout.
- [ ] **Step 2: Promote N-56, activate N-57, and advance the version.** Add PR #359, source and merge SHAs, clean independent review, exact-head and develop-push CI runs, and paired worktree/ref cleanup evidence to row 17. Set the active plan/version to N-57 and `0.1.0-dev.175`. Change only the authored version in `Directory.Build.props`.
- [ ] **Step 3: Retire the completed predecessor.** Remove the N-56 plan only after its delivery evidence has been promoted. Keep the baseline specification and investigation records.
- [ ] **Step 4: Commit successor planning first.** Run `git diff --check`, inspect the complete staged planning diff, and commit the roadmap, version, N-57 plan, and N-56 plan retirement as `docs: plan N-57 player projection test truth` before editing tests.

### Task 2: Keep only truthful player-surface display assertions

**Files:**
- Modify: `src/WildBunch.Web/src/tests/AppShell.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/CaseFileSurface.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/JournalSurface.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/StorySoFarStep.test.tsx`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Modify: `docs/features.md`
- Inspect only: `src/WildBunch.Web/src/components/CaseFileSurface.tsx`, `src/WildBunch.Web/src/components/JournalSurface.tsx`, `src/WildBunch.Web/src/components/start-flow/StorySoFarStep.tsx`, `src/WildBunch.Application/Games/Queries/GetPrologueHandler.cs`, `tests/WildBunch.Application.Tests/Handlers/PrologueHandlerTests.cs`, `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs`, `tests/WildBunch.Integration.Tests/PrologueHiddenTruthTests.cs`, and `docs/decisions/ADR-0007-hidden-culprit-truth-and-hidden-progress-boundaries.md`.

**Interfaces:**
- Consumes: ADR-0007's player projection boundary; PG-008's casebook/journal distinction; actual event-derived journal and settled-prologue DTOs; successful-response adversarial backend/API secrecy tests.
- Produces: positive case-summary display at the casebook owner, truthful summary separation at the journal composition, current-town and entry rendering, prologue variant metadata omission, and corrected W-06 dated findings.

- [ ] **Step 1: Reconfirm focused baselines before editing.** Run `npm --prefix src/WildBunch.Web test -- src/tests/AppShell.test.tsx src/tests/CaseFileSurface.test.tsx src/tests/JournalSurface.test.tsx src/tests/StorySoFarStep.test.tsx`. The current baseline passed 39 tests across four files. Do not edit production code to make a test pass.
- [ ] **Step 2: Give the case summary its actual display owner.** In the existing `CaseFileSurface` scenario, assert the fixture's `Bring the Wild Bunch to justice.` case summary is visible and name the scenario to reflect the case summary, retained clues and same-name warrant records. Retain the AppShell assertion that the actual supplied summary is absent inside the Journal dialog and rename that scenario to `keeps case-summary content separate from the journal when its overlay opens`; the low-level JournalSurface suite must not duplicate it.
- [ ] **Step 3: Remove frontend secrecy claims without their alleged secret.** In `JournalSurface.test.tsx`, remove the four `trueCulpritId`, `isTrueCulprit`, `linkedSuspectIds` and `killerReleaseState` absence checks and remove the duplicate case-summary absence check. Retain current-town-name, town-ID omission, grouping and event-entry assertions; rename the scenario to describe those observable results. In `StorySoFarStep.test.tsx`, remove the literal-template-placeholder test and unbacked hidden-identifier checks. Keep omission checks for `variantId` and `variant-1`, because the actual DTO fixture contains that metadata, and retain successful API copy/rendering behavior.
- [ ] **Step 4: Prove the new casebook assertion is behavior-sensitive.** Temporarily remove the CaseFileSurface case-summary render and run the focused test; the positive case-summary assertion must fail. Restore the component byte-for-byte and rerun the four-file focused suite successfully. Do not commit production mutations.
- [ ] **Step 5: Record W-06 and feature evidence accurately.** Add dated dispositions to both test investigation records explaining which frontend absence checks were unsupported, which real display contracts remain, and that player secrecy stays owned by ADR-0007's Application/API boundary and its adversarial tests. Update PG-008's evidence to link the real case-summary/casebook and journal-separation proof while retaining its partial assessment and the limits on complete casebook/deployed journey evidence. Confirm ADR-0007 remains true and unchanged.
- [ ] **Step 6: Validate and commit the intended diff.** Run `git diff --check`, inspect the complete diff, confirm production files are byte-for-byte restored and only tests/investigation/evidence changed, then commit as `test: correct player projection assertions`.

### Task 3: Review, publish, integrate, and retire N-57

**Files:**
- Review: complete merge-base diff, N-56 promotion, W-06 dispositions, PG-008 evidence update, ADR-0007 comparison, and exact focused/canonical evidence.
- Modify: the roadmap with N-57 final delivery evidence only in its next successor's first substantive commit.

**Interfaces:**
- Consumes: committed player-surface test corrections and normal check-only hook validation.
- Produces: N-57 integrated into `develop` at `0.1.0-dev.175` with independent whole-branch review and passing hosted gates.

- [ ] **Step 1: Check the exact worktree and diff.** Verify version `0.1.0-dev.175`, clean committed state, focused player-surface suites and hooked canonical validation on the final implementation head.
- [ ] **Step 2: Obtain independent review.** Dispatch a fresh-context reviewer for the full merge-base diff, including the independent ADR and feature-matrix truth comparison. Resolve actionable findings with appropriate behavior evidence and repeat review on the changed head.
- [ ] **Step 3: Publish and verify.** Push the branch, create a Draft PR to `develop`, verify body/base/head and exact source SHA, attach it when supported, mark it ready after review, and require the exact-head hosted gate to pass.
- [ ] **Step 4: Integrate and clean up.** Squash-merge to `develop`, require the develop-push gate on the merge SHA to pass, fast-forward the primary checkout while preserving its pre-existing investigation-note edit, then remove this clean worktree with the `finishing-a-development-branch` helper. Delete the exact local and upstream refs after worktree removal and verify all three absences.

## Acceptance

- The case summary is displayed in the CaseFileSurface and remains absent from the journal overlay; the new positive assertion fails if the case-file display is removed.
- Journal tests retain only observable current-town, event grouping and supported entry behavior, with no claim to prove hidden-data projection secrecy.
- StorySoFar tests retain omission of the DTO's actual variant metadata while the backend/API owners retain template substitution and hidden-truth secrecy.
- W-06's dated dispositions explain the unsupported frontend checks; PG-008 stays partial, ADR-0007 stays accurate, and no product/source behavior changes.
- N-56 is promoted and retired only after exact PR, review, hosted gates, fast-forward, and paired cleanup facts are verified.
- N-57 is independently reviewed, hosted CI passes on PR head and develop merge SHA, and its worktree plus local/upstream refs are removed together.
