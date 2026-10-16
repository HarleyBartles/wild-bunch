# N-94 Reconcile shipped browser evidence with the feature matrix

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Correct two stale feature-matrix summaries against the already recorded row-18 map and saloon-gossip browser evidence, while keeping their current assessments and limits accurate.

**Architecture:** This is a documentation and release-identity slice. The feature matrix remains the current product assessment, the roadmap remains the ordered implementation and delivery record, and existing tests remain the behavioral evidence; this plan adds no feature, code path, or test.

**Tech Stack:** Markdown, the single authored .NET version property, existing Playwright and PostgreSQL acceptance evidence, and the repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, `.agents/specs/2026-10-06-cloud-playability-and-releases.md`, `docs/features.md`, `.agents/playbooks/feature-matrix.md`, and row 18 of `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`.

**Execution Strategy:** `executing-plans` - the feature rows, delivery record, plan retirement, and version identity are one dependent documentation slice; inline execution preserves the source-to-claim comparison, while per-task delegation would add handoffs without independent implementation boundaries.

## Global Constraints

- Keep the established PG IDs, promises, dependencies, and current `Partial` assessments; correct only the two demonstrably stale summaries in PG-003 and PG-005.
- Do not duplicate N-88 Store evidence already recorded in PG-004 or N-83 through N-86 and N-90 travel evidence already recorded in PG-007.
- Distinguish local browser routes, API/persistence evidence, hosted validation, and deployed behavior; do not infer whole-feature completeness from one route or a passing CI run.
- Do not add product code, behavior tests, structural document tests, new features, new ADR decisions, or release/deployment actions.
- Advance the single authored product version in `Directory.Build.props` from `0.1.0-dev.211` to `0.1.0-dev.212`; generated web identity remains build-owned.
- Retire N-93 only in the first substantive implementation commit of this successor slice; keep N-94 through its completing PR.
- Merge the implementation PR to `develop` only after independent whole-branch review and exact-head hosted CI pass; verify the develop-push gate before paired worktree and branch cleanup.

## Review Focus

- Do not upgrade a feature from `Partial` because one player route passed; retain the already stated unsupported variants and deployment limits.
- Verify PG-003 no longer implies that the earlier Silver Creek town-hub playtest is the only browser evidence for map interaction after checking the existing N-80 and N-83 through N-86 routes.
- Keep the new PG-003 and corrected PG-005 statements distinct from existing N-88 Store evidence in PG-004, travel and starvation evidence in PG-007, and pre-town case-file evidence in PG-008.
- Preserve N-93's explicit manual-browser telemetry limitation; do not imply console or request-failure telemetry was observed.
- Resolve every changed Markdown link against the repository and confirm citations point to existing behavioral tests or the owning specification.

---

### Task 1: Reconcile delivered route evidence into live product assessments

**Files:** Modify `docs/features.md`, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, and `Directory.Build.props`; delete `.agents/plans/2026-10-16-row18-n93-casefile-before-town.md` in the first substantive implementation commit.

**Consumes:** Existing row-18 delivery evidence for N-80 through N-93, the current `src/WildBunch.Web/browser-tests/player-startup.spec.ts` route bodies, the N-90 PostgreSQL acceptance evidence, and the PG-001 through PG-008 promises and disposition rules.

**Produces:** A bounded PG-003 map-evidence statement, a corrected PG-005 scope statement, a current N-94 roadmap pointer, an accurate N-93 completion record, retirement of the completed N-93 plan, and generated build identity for `0.1.0-dev.212`.

- [x] Re-read the feature-matrix playbook and the current PG-001 through PG-008 entries; inspect the named browser route bodies and verify their existing assertions before editing any evidence claim.
- [x] Correct PG-003's stale paragraph so it distinguishes the earlier Silver Creek town-hub observation from the already documented N-80 starting-town map route and N-83 through N-86 travel/arrival routes; retain the pointer-only town-selection boundary, `Partial` assessment, and limits on untested routes and deployed behavior.
- [x] Correct PG-005's stale sentence that leaves saloon gossip outside browser proof; state that N-81 covers one Sheriff Office poster route and N-89 separately covers one saloon-gossip route, while observation, other knowledge sources, hosted behavior and deployment remain outside those bounded browser proofs.
- [x] Confirm PG-002 already contains N-87 confirmed archive evidence, PG-004 already contains N-88 Store evidence, PG-006 already contains N-82 named-take-in evidence, PG-007 already contains N-83 through N-86 and N-90 evidence, PG-008 already contains N-91 through N-93 evidence, and PG-001 already contains N-80 startup evidence; do not duplicate or rewrite accurate rows.
- [x] Record N-93 PR #396, reviewed source `01053f1b9bd8a204e9d28bc34e12054b6cfbd349`, merge `19e525e886c43af07bc8699567ccd638a8be2993`, version `0.1.0-dev.211`, exact-head gate `38853775584`, develop-push gate `38854549122`, no-actionable-findings review, and paired worktree/local/upstream-branch cleanup in the roadmap; state that the manual paired-server browser control did not expose console or request-failure telemetry.
- [x] Change the active row-18 plan link to N-94, replace the stale N-93-current wording with its completed bounded outcome and the feature-evidence reconciliation scope, and keep row 18 open for remaining qualification and release procedure work.
- [x] Set `Directory.Build.props` to `0.1.0-dev.212`, leave generated files untouched, and verify the production build derives the web identity from that one authored value.
- [x] Compare the resulting diff with ADR-0005, ADR-0007, ADR-0012, ADR-0027 and ADR-0047; leave the ADRs unchanged because this slice records evidence and makes no durable decision.
- [x] Resolve every changed relative link and verify the two changed feature claims against the named browser routes and existing row-18 delivery records; do not add tests that assert document shape or file presence.
- [x] Stage only the N-94 plan, matrix, roadmap, version property, and N-93 plan retirement; commit through the normal check-only hook without a preceding or immediately repeated full CI run.

### Task 2: Review, publish, merge, and clean N-94

**Files:** Review the committed N-94 diff, this plan, the affected feature rows, the updated roadmap, and the cited behavioral evidence.

**Consumes:** The source-to-claim inspection, checked document links, build-derived `.212` identity, and canonical hooked commit result.

**Produces:** Independent whole-branch review, a reviewed PR to `develop`, passing exact-head hosted CI and develop-push CI, a verified fast-forwarded primary checkout, and paired retirement of the merged worktree and exact local/upstream branch refs.

- [ ] Build a fresh-context whole-branch review package from current `origin/develop` to `HEAD`, including this plan, affected PG rows, cited route bodies, roadmap delivery facts, version-source comparison, ADR comparison, and hooked validation; use the standing reviewer authorization and repeat review after any correction.
- [ ] Follow the PR runbook, open a Draft PR to `develop`, and verify the exact title, base, head SHA, and body; mark it ready only after the review and local gate pass, and merge only after exact-head hosted CI succeeds.
- [ ] Verify the develop-push gate passes on the exact merge commit, then fast-forward the primary checkout while preserving the unrelated investigation edit.
- [ ] Use the finishing helper in check/apply mode; verify the PR head and merge result, clean worktree, and absence of the exact local and upstream branch refs together with the worktree.

## Validation Boundaries

This slice reconciles documented evidence for already implemented behavior; it does not establish new player behavior, complete any feature assessment, qualify a release candidate, or publish a release.
