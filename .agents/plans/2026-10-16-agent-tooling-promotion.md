# Agent and repository tooling promotion implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prepare and deliver an agent/repository tooling baseline to `main`, with completed planning retired on `develop`, live planning preserved on `develop`, and application behavior changes left for a fresh release candidate.

**Architecture:** Perform semantic planning retirement before cutting the promotion. Extract a coherent tooling change onto current `main`, promote it without a product release, and reconcile it into `develop` while restoring exactly the live planning tree from immediately before reconciliation. The subsequent application candidate consumes that tooling baseline.

**Tech stack:** Git/GitHub, Python command bus and repository checks, tracked Bash commit hook, .NET/MSBuild, Node/npm/Vite, existing language formatters and browser-test infrastructure.

**Spec and authority:** The user's approved decisions in this conversation are the specification for this bounded change. The approved artifact assessment is recorded below so execution does not depend on chat access. Governing repository decisions are ADR-0037 (repository standards and custody), ADR-0043 (command-bus ownership and gate order), and ADR-0048 (Gitflow and version identity). Read them through `docs/decisions/README.md` and the decision-record playbook before implementation.

**Execution strategy:** `executing-plans`. Retirement, extraction, promotion and reconciliation depend on the same source snapshots and must proceed sequentially. Parallel implementation would increase the risk of losing live planning or importing application changes. Follow `.agents/runbooks/implementing.md`; use the repository's review and publication runbooks at each handoff.

## Global constraints

- This plan authorizes no work by its mere existence. Execute only under a subsequent implementation instruction. Merge/promotion operations require the user's applicable authorization; prepare fully reviewable Draft PRs before requesting any missing authorization.
- No named bootstrap release, `0.0.1`, stable tag, GitHub Release, deployment or server mutation belongs to this work.
- No planning artifacts in the current tree of `main`, including specifications, roadmaps, plans and investigations. Existing Git history is retained; no history rewriting.
- Keep live planning on `develop`. Preserve the existing pinned completed-artifact-custody standard and semantic next-slice retirement lifecycle.
- Before a cut to `main`, an agent reads planning artifacts, compares their whole scope with implementation/delivery evidence, promotes enduring knowledge and commits completed-artifact retirement to `develop`. Ask the user about each artifact whose completion is unclear; never substitute age, checkboxes or merged-PR status for this judgment.
- Reconciliation restores the exact live planning tree from `develop` immediately before the merge, including additions, edits and deletions made since the cut. It does not resurrect planning from the cut or reassess completion during mergeback.
- Command bus, its helpers and tests, hooks, CI, formatting/lint tooling, editor/runtime adapters and their required build configuration belong in the first substantive tooling commit to `main`. A plan-only preparation commit or the preflight commit to `develop` is not that tooling commit.
- Keep gameplay, application architecture, persistence, player UI behavior and their behavioral tests in the application change. Classify by responsibility rather than directory: build/test configuration and scoped agent routers can live under `src/`.
- No opportunistic product repairs, broad dependency upgrades, copied plugin payloads, replacement planning archives, committed test results or development receipts.
- Use canonical worktrees and preserve unrelated working changes. The shared checkout currently has an uncommitted edit in `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`. Preserve its exact patch in branch-scoped off-repo scratch before any checkout update, assess its useful content during promotion, and ask the user only if its intended custody remains unclear. Do not overwrite, stash or discard it as incidental cleanup.
- Markdown paragraphs and list items remain on one physical source line. Use no emojis or em dashes.

## Source snapshots and delivery boundaries

At planning time, remote `develop` is `6ab221b9d7616d6f549fa5e1a026acda82cf0fc3`, remote `main` is `f771aef70091945a6926a7399e41247f101b46bc`, and draft PR #399 proposes `release/0.1.0` at `d9de34b58a2ed6c6e54e30116a15af0ed82c731a` into `main`. These are inspection anchors, not permission to ignore later changes. Refresh all owning refs and PR metadata before execution.

Deliver three sequential changes: (A) preflight retirement and custody policy into `develop`; (B) one coherent agent/tooling promotion commit based on `main`; (C) reconciliation into `develop`. Then prepare a fresh application release candidate, replacing #399 only after its successor is reviewable. Each change gets its own exact-source verification and review. Do not squash all three into one mixed change or merge all of `develop` into `main` for the tooling promotion.

## Approved artifact assessment

Retire the following 29 existing files in preflight after content promotion and link repair:

| Home | Approved retirement scope |
| --- | --- |
| `.agents/plans/` | `2026-10-16-row18-n95-release-qualification.md` |
| `.agents/roadmaps/` | `2026-10-07-stable-0.1.0-cleanup.md` |
| `.agents/specs/` | `2026-09-29-ambient-opt-in-standards-and-decisions-home.md`, `2026-10-02-aom-self-cert-migration.md`, `2026-10-07-stable-0.1.0-baseline.md` |
| `.agents/investigations/stable-0.1.0/` | All 24 currently tracked files, including its README, the source/test audits, spikes, product contracts and `2026-10-07-unrelated-criminal-addition.md` |

The cleanup programme ended at a qualified local development baseline in PR #398. A stable publication, completed hunt, account system or deployed environment was outside that programme's exit. Do not reopen it on those grounds. The two migration specs describe delivered or superseded work, not outstanding migrations to redo.

Keep `.agents/roadmaps/2026-10-06-cloud-playability.md` and `.agents/specs/2026-10-06-cloud-playability-and-releases.md` live on `develop`: authentication, user ownership, hosted capability isolation, container runtime, VPS adoption and deployment remain outside the completed baseline. Reconcile their completed baseline references without deleting their future scope. This new promotion plan also remains live on `develop` through its completing delivery.

Deferred features belong in `docs/features.md`, with clear deferred status, useful stories, integration requirements and unresolved decisions. Consolidate PG-009-A there before retiring its standalone investigation. Apply the same treatment to future service/vendor work, Rifle capability, gunfights, sheriff recognition, lawman/telegraph work, casebook assistance and developer layout preview described in the retiring records. Preserve supplied intent without claiming implementation or inventing missing product decisions. The matrix may describe deferred capabilities on `main`; do not create a second planning archive beneath `docs/`.

## Review focus

- A completed programme contains future feature notes: preserve their meaning in deferred matrix entries before deleting the historical wrappers (Task 1).
- New planning arrives after the cut, or a live file is deleted meanwhile: restore the exact pre-merge `develop` tree, including absence, rather than the older cut snapshot (Task 4).
- A PR branch has an arbitrary name but targets `main`: the target branch and proposed tree must control the exclusion check (Task 2).
- A tooling dependency lives under `src/`: include the configuration needed for runnable tooling without importing newer gameplay or player-interface implementations (Task 3).
- A passing gate masks scope loss: inspect the actual tooling diff and the residual application diff independently of CI results (Tasks 3 and 5).

## Task 1: Commit semantic retirement and content promotion to develop

**Files:** The 29 approved artifacts; `docs/features.md`; relevant current files under `docs/decisions/`, `.agents/doctrine/`, `.agents/playbooks/`, `.agents/runbooks/`, `.agents/unslop/` and `.agents/contracts/`; the two retained cloud artifacts. Do not alter application source or product behavior.

**Consumes:** Approved assessment above and freshly read artifact contents at current `origin/develop`.

**Produces:** The semantic retirement portion of change A, prepared for Task 2 to complete and publish into `develop`.

- [ ] Refresh remote default branch and fast-forward it before editing; use a fresh canonical task worktree from current `origin/develop`. Bring this plan into that branch from its committed planning branch, `codex/separate-agent-tooling-plan`, so it remains an execution input and is preserved in develop history. Reconcile new artifact changes since the assessment, asking the user about each unclear whole-scope classification.
- [ ] Read the full retiring artifacts for knowledge promotion. Inspect callers with `rg -n 'stable-0.1.0|2026-09-29-ambient|2026-10-02-aom|row18-n95' .agents docs README.md CONTRIBUTING.md REVIEW.md tools scripts`. This is link discovery, not an automatic deletion decision.
- [ ] Consolidate current product rules into the owning matrix entries, existing ADRs and focused current product explanations where detail needs a separate explanatory home. Put deferred feature stories and open choices into the matrix. Keep historical evidence links immutable at an existing full commit SHA, not `develop` or `main` paths to deleted files. Preserve unslop observations and their incident provenance; repair their investigation links without deleting them as planning artifacts.
- [ ] Reconcile the two cloud documents: the baseline prerequisite is complete, later cloud scope is live, and the completed programme is no longer a required working document. Promote reusable release rules into `.agents/runbooks/release.md` and ADR-0048 rather than making that runbook depend on a transient spec.
- [ ] Delete only the 29 approved tracked artifacts after their durable/future knowledge and references are handled. Preserve and assess the unrelated shared-checkout patch before that checkout is updated; ask the user if its content creates an unresolved custody or product decision. Independent promotion work may continue while a necessary question is pending.
- [ ] Update `.agents/doctrine/artifact-custody.md`, `.agents/playbooks/completing-plans.md`, `.agents/runbooks/release.md`, `.agents/runbooks/pr.md` and relevant certification to establish branch custody and semantic preflight. Preserve next-slice retirement and its subscription pin. Record this new durable branch-custody decision in a new numbered ADR selected from the current catalogue; amend ADR-0048 only where its promotion/reconciliation wording needs the link. The one-time tooling promotion is not a product release and must be documented accurately against the ordinary PR routing rule.
- [ ] Compare `docs/features.md` with the original records for lost requirements, false completion claims and invented decisions. Review the actual deletions and repaired inbound links. No document-shape or deletion-detector test is required for this editorial work.
- [ ] Assign one next unused `0.1.0-dev.N` in `Directory.Build.props` for change A, reconciling it against current integration before merge. Stage only intended retirement/content/policy changes and commit normally through the tracked hook. Continue Task 2 in this same preflight worktree before publishing the combined change A; one PR consumes one development checkpoint even if it contains multiple commits.

**Exit:** The prepared preflight branch retains the two cloud artifacts and this live plan, plus any newly identified live work. The 29 approved artifacts are in Git history and absent from its current tree. There is no tracked completed-artifact archive and no current rule points at a retired planning document as live authority.

## Task 2: Enforce the main-tree planning boundary

**Files:** Create `tools/check_planning_custody.py` and `tools/tests/test_planning_custody.py`; integrate through `tools/run.py`, `.github/workflows/ci.yml`, `tools/README.md` and `.agents/runbooks/pr.md`.

**Consumes:** Task 1's declared branch policy.

**Produces:** A check-only guard on the proposed tree for promotion to `main`, with semantic completion assessment remaining agent-owned.

- [ ] Implement `py -3 tools/check_planning_custody.py --check --target main` to inspect the tracked candidate/index and reject files under `.agents/plans/`, `.agents/specs/`, `.agents/roadmaps/` or `.agents/investigations/`. Report offending paths and a nonzero status. It must not delete files, stage changes, decide completion or inspect only the PR's changed filenames. `--target develop` permits live planning. Reject missing/unknown arguments before inspecting the tree.
- [ ] Route the check through the existing command bus and canonical gate. In hosted PR validation, pass the actual PR base branch using `WILD_BUNCH_TARGET_BRANCH`; for pushes use the pushed branch. An explicitly supplied `main` target must also work in a local promotion worktree whose branch is named `codex/...`. Do not infer PR destination solely from the checked-out branch name. Keep the existing canonical check name, full staged-candidate checks and fail-fast ordering.
- [ ] Exercise the checker in temporary Git repositories: an otherwise valid candidate containing a tracked plan is rejected for `main`; a tracked spec, roadmap and investigation are each rejected; the same live planning is permitted for `develop`; durable doctrine/ADRs and deferred matrix entries are permitted; an offending file already in the base is still rejected; a staged deletion removes the violation despite the old `HEAD` containing the file. Assert process status and meaningful diagnostics, not implementation strings.
- [ ] Prove the new negative tests can fail: temporarily bypass the relevant rejection in the checker, confirm the owning test fails, restore the behavior and rerun the focused suite. For staged-tree coverage, temporarily inspect `HEAD` instead of the index, confirm that test fails, then restore. Do not commit mutations or test-result receipts. Run `py -3 -m pytest tools/tests/test_planning_custody.py -q` and relevant existing command-bus behavior tests.
- [ ] Commit the guard through the normal hook on Task 1's preflight branch. Publish the combined change A as a Draft PR to `develop`; obtain fresh review and exact-head hosted validation under the repo runbooks. Merge only with applicable authorization and read back its merged identity before cutting the tooling promotion. Include this guard in Task 3's first substantive tooling commit to main; do not publish another intermediate main tooling change.

**Exit:** Change A is integrated into develop before the cut. Proposed `main` trees cannot carry planning in the declared homes; review additionally catches planning disguised elsewhere. A green result says nothing about whether retained develop artifacts are complete.

## Task 3: Prepare the first agent/tooling promotion commit on main

**Files:** `AGENTS.md` and scoped routers; non-planning `.agents/` guidance/contracts/skills; `.codex/`; obsolete `.devin/` rules; `tools/`; repository-owned `scripts/` and their tests; `githooks/pre-commit`; `.github/workflows/ci.yml`; `.editorconfig`, `.gitignore`, `.vscode/`, `pyproject.toml`, `global.json`; build/test configuration identified below; relevant contributor and decision documentation.

**Consumes:** Current `main`, Task 1's merged cleaned integration source, and Task 2's guard.

**Produces:** One independently usable agent/repository tooling commit proposed to `main`, without product release publication or the develop application implementation.

- [ ] Create a fresh canonical `codex/` worktree from refreshed `origin/main`. Preserve the cleaned develop source and original main SHA as immutable extraction anchors in off-repo scratch. Use selective paths and hunks, not wholesale cherry-picks of mixed commits and not a merge of `develop`.
- [ ] Bring the command bus and its complete supporting modules, tooling behavior tests, native service scripts, hooks, style tooling, editor/Codex adapters, CI and declarations together. Resolve wrapper retirement and test moves in the same commit. Preserve source-first plugin declarations and authored skills; do not copy installed runtime caches. Remove the five old planning files already tracked on main and introduce no new ones there, including this plan or the live cloud pair.
- [ ] Treat `.NET` SDK/MSBuild version metadata and web `package.json`/lockfile tooling dependencies, ESLint/Prettier/TypeScript/Vite/Playwright configuration, and browser test harness support as build/test infrastructure. Split mixed `vite.config.ts` and package hunks: retain existing main application entry points and runtime dependencies unless the tooling actually requires a change. Do not transfer new player routes, domain behavior, persistence implementation, product fixtures or application behavior tests merely because tooling refers to them.
- [ ] Make the transferred tools runnable against main's existing application. Generic browser harness checks may establish build/serve and artifact-identity behavior; the newer player-journey specifications remain with the application changes. Preserve all required existing main validation. Do not add silent skips, pretend a missing target passed, or import a game fix to make a tooling check green. If exact latest tooling depends on a new application interface, split that dependency explicitly and return to the plan with the concrete failing interface before changing the agreed boundary.
- [ ] Assess initial style normalization separately from product changes. Do not bulk-copy develop source to satisfy style gates. If adopting the mandatory tooling requires production-source edits beyond build metadata/configuration, show the exact mechanical or semantic scope to the user and settle it before including those edits. Source normalization is not permission to import the application rewrite.
- [ ] Adapt transferred guidance and certification to the implementation actually on main. Tooling decisions and custody rules can land now; changed gameplay/persistence decisions, feature assessments and implementation claims accompany the application source they describe. Retain truthful existing main product guidance rather than claiming its older application already implements develop's baseline.
- [ ] Configure hooks explicitly with `py -3 tools/run.py setup-hooks --apply`. Install the declared Python and locked web dependencies as needed, and ensure PostgreSQL through the existing native lifecycle script. Run focused bus/hook/style/checker tests and explicit `py -3 tools/check_planning_custody.py --check --target main`. Stage intended changes, then commit through the canonical hook with `WILD_BUNCH_TARGET_BRANCH=main` in the process environment. Do not bypass the hook or run an identical full gate immediately before or after a successful hooked commit.
- [ ] Review the entire `origin/main...HEAD` diff for application leakage, missing dependencies, obsolete declarations and planning files. Publish one Draft tooling PR targeting `main`, explicitly describing its non-release scope. Ensure its exact-head hosted canonical validation runs with the real base target when eligible. Obtain fresh review after corrections and read back the PR head/base/state and checks.

**Exit:** A fully reviewable tooling PR exists. Its first substantive commit contains the command bus and the coherent repository-tooling boundary. The existing main application still builds/tests, no planning files are present, and no stable product identity or published release has been fabricated. Missing merge authorization is a final handoff gate, not a reason to leave preparation incomplete.

## Task 4: Promote and reconcile without losing live planning

**Files:** The promoted tooling tree; four planning homes on `develop`; `Directory.Build.props`; guidance/certification whose main-compatible extraction required reconciliation.

**Consumes:** Task 3's approved exact-head tooling PR and current `develop`.

**Produces:** Verified tooling baseline on `main` and a reviewed merge-commit reconciliation into `develop` preserving current application and live planning.

- [ ] With applicable merge authorization, promote the reviewed tooling PR using a merge commit and read back its actual merge SHA and tree. Confirm the main-tree exclusion check on that exact result. Create no tag, GitHub Release or deployment.
- [ ] Refresh `develop` immediately before reconciliation. Capture its full commit SHA as `developBeforeMerge`. The planning snapshot is the four declared homes at this commit, not the release cut, an old list of filenames or a working-copy inventory. Preserve newer application work as well.
- [ ] In an isolated reconciliation worktree based on that exact develop commit, merge the promoted main commit with `--no-commit --no-ff`. Resolve tooling conflicts deliberately; preserve develop's application implementation and reconcile any main-compatible tooling changes into one shared implementation. Never use a blanket `ours` or `theirs` strategy.
- [ ] Restore the four planning homes exactly from `developBeforeMerge` into the index/worktree: restore every snapshot path and remove only planning paths absent from that snapshot. Use the union of snapshot and merge-result paths to handle additions/deletions safely. Verify `git diff --cached --exit-code <developBeforeMerge> -- .agents/plans .agents/specs .agents/roadmaps .agents/investigations` returns no differences. This is an operational equality check for an owned snapshot, not a committed change-detector test.
- [ ] Preserve the ongoing develop release core and allocate its next unique development checkpoint for the reconciliation PR, as ADR-0048 requires. Stage and commit normally with the develop target. Validate applicable tooling and the current application through the hook; review the entire reconciliation diff and exact planning equality.
- [ ] Publish a Draft reconciliation PR to `develop`, obtain fresh review and exact-head hosted checks, and merge by merge commit only under applicable authorization. If develop advances before merge, refresh the merge candidate and use the newly current develop planning snapshot, repeating equality checks and affected validation. Verify the merged result preserves that snapshot and ongoing application work.

**Exit:** Main has the reviewed tooling baseline and no planning. Develop has the same tooling implementation, its application work, and exactly the live planning that existed immediately before reconciliation. Completed artifacts remain deleted.

## Task 5: Hand off a clean application candidate

**Files:** A fresh release candidate from reconciled `develop`; `Directory.Build.props`; any release-only qualifications under `.agents/runbooks/release.md`. Do not reopen the completed cleanup programme or invent a new cloud milestone.

**Consumes:** Task 4's reconciled develop and promoted main identities.

**Produces:** A fresh, reviewable application candidate against the tooling baseline and a clear disposition for draft PR #399.

- [ ] Refresh the refs and follow the release runbook to prepare a fresh `release/*` candidate. Select the next unused/requalified `0.1.0-rc.N` after inspecting #399 and existing candidate identities. Do not relabel changed source as the old qualified candidate or infer a number from this plan alone.
- [ ] Perform semantic planning preflight on current develop before this cut. The two cloud documents remain live; this plan remains through its completing work. Strip all live planning from the candidate tree before main promotion without marking that work abandoned on develop. Repair candidate-side references to the stripped documents: current rules point to durable owners, historical context uses immutable source links, and retained live planning remains available on develop. Do not leave broken relative links in main-bound product documentation.
- [ ] Review the residual diff against current main. It should describe application/source behavior, its tests, corresponding product/decision documentation and release identity. The command bus, tooling guard and agent infrastructure have already landed. Resolve accidental tooling drift in its owner before declaring this separation complete; explain necessary application-specific test/configuration changes rather than silently labelling them agent tooling.
- [ ] Qualify the exact changed source with the canonical gate and `py -3 tools/run.py release-verify --check` under the release runbook. Bind local bundles to source and artifact identity; preserve the distinction between local qualification, hosted checks and deployed behavior. Publish a Draft successor PR with fresh review and accurate qualification limits. Once the replacement is reviewable, close #399 as superseded with its replacement link; preserve its existing branch/worktree until ownership and unmerged contents are verified.
- [ ] Hand off the successor PR, verified tooling/reconciliation identities, remaining live cloud work and any external action still requiring authorization. The scope of this plan ends at a reviewable fresh candidate, not a stable release merge or publication. After completing this scope, retire this plan on develop through the ordinary next-slice process; no permanent completion report belongs in the repo.

**Exit:** The next candidate's review is focused on the application rather than an accumulated agent/tooling migration. Neither creating the Draft nor completing this plan claims a stable release or authorizes public deployment.
