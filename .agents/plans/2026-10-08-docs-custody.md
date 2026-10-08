# Non-ADR Documentation Custody Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reconcile the six non-ADR documents under `docs/` so each retained document serves an identifiable reader and each engineering obligation has one current routed owner.

**Architecture:** Keep `docs/local-postgresql.md` as a human local-development guide because both root README and `scripts/README.md` route developers to it. Retire the five documents whose content duplicates agent doctrine/profiles or presents an unaccepted issue taxonomy, after confirming every inbound repo link and transferring any unique current guidance to its existing owner. Keep this documentation-only; do not add structural tests or checkers.

**Tech Stack:** Markdown, Wild Bunch documentation gate, `Directory.Build.props` as the sole authored application version.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially the authority split and consolidation rules; [documentation custody investigation](../investigations/stable-0.1.0/2026-10-06-docs-custody-investigation.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 02.

**Execution Strategy:** `executing-plans` — user-authorized Native inline execution; the first commit performs successor retirement and version/roadmap bookkeeping, then one tightly coupled documentation edit is reviewed and validated as a whole.

## Global Constraints

- Target `develop`; start from merged PR #189 at `17b3a8a72c71fb86e6643767758712b88bc8b50c` and advance `Directory.Build.props` from `0.1.0-dev.4` to `0.1.0-dev.5` exactly once for this PR.
- Retire `.agents/plans/2026-10-08-adr-dispositions.md` in this successor slice only after classifying its complete scope against PR #189; keep the current plan and baseline specification through this completing PR.
- Update roadmap row 02 with verified PR #189 merge evidence and this active plan; keep the row executing until this documentation outcome is delivered.
- Keep `docs/local-postgresql.md` because README and `scripts/README.md` use it as the human setup route; preserve accurate service ownership, commands, and database boundaries.
- Retire `docs/frontend-styling.md`, `docs/testing-posture.md`, `docs/testing-lanes.md`, `docs/product-roadmap.md`, and `docs/unslop-style-guide.md` after checking repository inbound links and current owners.
- Preserve current test-lane and manual-browser guidance only in `.agents/doctrine/validation-policy.md`, `.agents/playbooks/testing.md`, and `.agents/playbooks/ui-browser-check.md` where each rule has an existing owner; do not add duplicate doctrine or a new playbook.
- In `.agents/doctrine/frontend-standards.md`, remove the retired document route and the inaccurate claim that one test enforces all styling standards; retain the tested boundaries accurately if mentioned, and state that components own their display contracts while parents compose them through supported layout/props rather than styling reach-through.
- Do not modify application code, tests, ADRs, Linear, hosted services, or unrelated guidance; do not add heading-presence, empty-section, file-inventory, or link-existence tests.
- The check-only pre-commit hook validates the staged candidate and does not mutate or stage corrections; use explicit commands for any generated or formatting changes.

## Review Focus

1. Deleting a document leaves a repo-local inbound link or an agent obligation without a current owner; verify all inbound references and the destination owner before removal.
2. Retaining the local PostgreSQL guide accidentally preserves duplicated worker procedure or asserts an unsafe database boundary; compare every operational claim with `tools/postgres-dev.ps1` and the existing testing playbook.
3. Frontend doctrine continues to overstate what `stylingEnforcement.test.ts` proves or permits parent components to reach into child styling; review the claim against the test and the settled component-owned display contract.

---

## Task 1: Bootstrap this successor slice and retire the completed ADR plan

**Files:** Create `.agents/plans/2026-10-08-docs-custody.md`; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-08-adr-dispositions.md`.

- [ ] Confirm PR #189 is merged to `develop` at `17b3a8a72c71fb86e6643767758712b88bc8b50c` and classify the predecessor's whole scope: ADR disposition work and its PR/review/hosted validation are complete in merged history; the six non-ADR document outcome is explicitly successor scope in roadmap row 02.
- [ ] Update roadmap row 02 to record PR #188 and PR #189 as merged, include PR #189's merge commit and delivered ADR/guidance outcome, and point to this plan as the active document-custody successor; leave row 02 executing.
- [ ] Set `Directory.Build.props` to `0.1.0-dev.5` and confirm it remains the only authored application version.
- [ ] Remove the completed ADR-dispositions plan and its stale roadmap path after the full-scope classification above; preserve the baseline spec, roadmap, investigation evidence, and this plan.
- [ ] Run `git diff --check`, inspect the staged candidate, and make a normal hooked commit before editing the six documents.

## Task 2: Reconcile document owners and repo routes

**Files:** Modify `.agents/doctrine/frontend-standards.md` and `docs/local-postgresql.md`; delete `docs/frontend-styling.md`, `docs/testing-posture.md`, `docs/testing-lanes.md`, `docs/product-roadmap.md`, and `docs/unslop-style-guide.md`.

- [ ] Search the repository for every inbound reference to all six documents, including Markdown links and plain-text routes; resolve each reference before deleting its target. Preserve README and `scripts/README.md` links to `docs/local-postgresql.md`.
- [ ] In frontend standards, remove the stale `docs/frontend-styling.md` route and remove any whole-doctrine enforcement claim. If describing existing enforcement, state only the specific legacy stylesheet/class/inline-style conditions the test actually checks; do not claim it proves token discipline or component ownership.
- [ ] Add the settled styling ownership boundary: each component owns its display contract; parents arrange composition using supported props and layout, not selectors that reach through into child internals.
- [ ] Compare local PostgreSQL guide commands and database claims with `tools/postgres-dev.ps1`, `scripts/README.md`, and the testing playbook; retain the helpful human setup and safety explanation, removing or rewriting worker-only policy that duplicates the agent owner.
- [ ] Retire the five documents only after their useful guidance is represented at existing current owners: testing policy/execution in validation doctrine and testing/browser playbooks; player copy/visual guidance in routed unslop profiles; proposed issue taxonomy retired as an unaccepted, unreferenced repo proposal without making claims about external Linear configuration.
- [ ] Review the full diff for unsupported claims, duplicate current authority, stale links, and empty sections; run `git diff --check` and commit this documentation outcome normally.

## Task 3: Validate and publish the row-02 documentation outcome

**Files:** Review the complete branch and any corrections limited to the files above, this plan, roadmap, and version source.

- [ ] Verify `rg` finds no remaining inbound references to the five retired docs, and that the retained PostgreSQL guide remains linked from root README and `scripts/README.md`.
- [ ] Manually follow the surviving routes to validation doctrine/testing/browser playbooks, frontend standards, and unslop profiles; verify each moved obligation has one current owner and each retained doc has a distinct reader purpose.
- [ ] Run `git diff --check` and `py -3 tools/run.py ci --check`; read the actual output and verify the check-only hook did not mutate the tree.
- [ ] Review the final diff against the plan, spec, documentation-custody investigation, ADR/unslop routing, and actual local PostgreSQL implementation; fix material findings with a focused edit and normal hooked commit.
- [ ] Obtain one fresh whole-branch review, resolve every Critical or Important finding, and defer only Minor findings with a recorded reason.
- [ ] Publish a Draft PR targeting `develop`, attach it to the current task, and advance it to Ready only after local gate and review pass; verify successful hosted checks on the exact PR head before merge.
- [ ] Verify the merge on refreshed `origin/develop`, update row 02 to done only when this PR's outcome is delivered, and retain this plan/spec/roadmap through the completing PR.
- [ ] After merge proof, remove only this plan's verified branch, managed worktree, and branch-scoped scratch; preserve any path the host reports as locked.

## Acceptance Evidence

- Row 02 records both merged ADR outcomes and this completed document-custody outcome with actual PR evidence.
- The only retained non-ADR document in this slice is the locally useful PostgreSQL guide, routed from both human entrypoints and consistent with the service implementation.
- Retired docs have no remaining repo-local inbound routes, and their useful current obligations have a single existing owner.
- Frontend standards accurately describe test enforcement and prevent parent styling reach-through while preserving the component-owned display contract.
- No application or test source changed; no structural document checker was added.
- The canonical repository gate, fresh whole-branch review, exact-head hosted checks, and develop merge are verified before completion.
