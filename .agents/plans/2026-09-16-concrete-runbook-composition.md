# Concrete Runbook Composition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every Wild Bunch runbook a concrete repository workflow that composes declared skills with binding doctrine, contracts, local commands, and independently checkable evidence.

**Architecture:** Keep skills focused on portable capability and judgment. Strengthen repo runbooks as explicit composition manifests with ordered local workflow steps and checklist-shaped evidence, while retaining doctrine as invariant truth and contracts as exact exchange shapes.

**Tech Stack:** Markdown agent guidance, Python `unittest`, repository standards and mesh tooling.

**Spec:** `.agents/skills/repo-standards/references/repository-runbook-standard.md`

**Execution Strategy:** `manual` — the changes share one taxonomy and must be reviewed as a single guidance graph in this session.

## Global Constraints

- Do not copy skill-internal methods into runbooks.
- Every runbook retains the exact seven-section manifest.
- Every composition declares ordered repo workflow steps.
- Every evidence contract uses independently checkable checklist items.
- Generated skills and indexes change only through the canonical refresh/apply path.

---

### Task 1: Move the pre-commit hook into tracked custody

**Files:**
- Create: `githooks/pre-commit`
- Modify: `scripts/tests/test_run.py`
- Modify: `scripts/tests/test_ci_workflow.py`
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: the upstream canonical staged-snapshot hook template and the local apply/check command declaration.
- Produces: a tracked canonical hook, `core.hooksPath=githooks`, and hosted CI execution of that same hook.

- [ ] **Step 1: Add failing hook-custody tests**

Assert that the tracked hook matches the accepted upstream template, `ci --apply` configures hook custody through repo-standards, and hosted CI executes the tracked hook directly.

- [ ] **Step 2: Run the focused tests and confirm failure**

Run: `py -3 -m pytest scripts/tests/test_run.py scripts/tests/test_ci_workflow.py -q`

Expected: FAIL because no tracked hook or hosted parity step exists.

- [ ] **Step 3: Implement tracked hook installation and validation**

Accept the upstream-generated `githooks/pre-commit`; let repo-standards apply and check own `core.hooksPath` and hook validation. Update hosted CI to invoke `githooks/pre-commit` with `REPO_STANDARDS_HOSTED_COMMIT=HEAD`.

- [ ] **Step 4: Run focused hook tests**

Run: `py -3 -m pytest scripts/tests/test_run.py scripts/tests/test_ci_workflow.py -q`

Expected: PASS.

### Task 2: Encode the concrete local runbook contract

**Files:**
- Modify: `scripts/tests/test_repo_guidance_contracts.py`
- Modify: `.agents/runbooks/design.md`
- Modify: `.agents/runbooks/planning.md`
- Modify: `.agents/runbooks/implementing.md`
- Modify: `.agents/runbooks/code-review.md`
- Modify: `.agents/runbooks/pr.md`

**Interfaces:**
- Consumes: the upstream seven-section runbook standard and installed skill names.
- Produces: an executable local test for ordered composition and checklist evidence, plus concrete core-stage runbooks.

- [ ] **Step 1: Extend the guidance contract test**

Assert that every authored runbook has an ordered list in `## Composition`, a checklist in `## Evidence contract`, and at least one explicitly named skill in `## Required skills`.

- [ ] **Step 2: Run the focused test and confirm the current summaries fail**

Run: `py -3 -m unittest scripts.tests.test_repo_guidance_contracts`

Expected: FAIL on runbooks whose composition or evidence remains prose-only.

- [ ] **Step 3: Rewrite the five core runbooks as concrete compositions**

Name the skill-owned capability at each stage, bind the applicable Wild Bunch doctrine/contracts, identify the local commands and paths, and finish with observable evidence. Do not reproduce skill algorithms.

- [ ] **Step 4: Run the focused test**

Run: `py -3 -m unittest scripts.tests.test_repo_guidance_contracts`

Expected: remaining failures identify additional runbooks for Task 2; the five core runbooks pass their structural assertions.

### Task 3: Make every additional runbook concrete

**Files:**
- Modify: `.agents/runbooks/asset-selection-cut-normalization.md`
- Modify: `.agents/runbooks/code-style.md`
- Modify: `.agents/runbooks/completing-plans.md`
- Modify: `.agents/runbooks/dev-overlay.md`
- Modify: `.agents/runbooks/marketplace-generation.md`
- Modify: `.agents/runbooks/security.md`
- Modify: `.agents/runbooks/seeded-game-setup.md`
- Modify: `.agents/runbooks/skill-authoring.md`
- Modify: `.agents/runbooks/testing.md`
- Modify: `.agents/runbooks/town-hub-asset-production.md`
- Modify: `.agents/runbooks/ui-browser-check.md`

**Interfaces:**
- Consumes: focused Wild Bunch capability skills, current doctrine and contracts, and canonical repository commands.
- Produces: concrete specialized workflows with no competing portable workflow ownership.

- [ ] **Step 1: Convert each composition to ordered repository steps**

Each step identifies the capability used and the local doctrine, contract, command, or path it binds. Conditional capabilities remain explicitly conditional.

- [ ] **Step 2: Convert evidence prose to checklists**

Each checklist item names observable proof: file/path state, command result, contract field, browser observation, or Git/GitHub state.

- [ ] **Step 3: Run the focused contract test and standards checker**

Run: `py -3 -m unittest scripts.tests.test_repo_guidance_contracts`

Run: `py -3 .agents/skills/repo-standards/scripts/repo_standards.py --check --yes`

Expected: both pass without warnings.

### Task 4: Reconcile, validate, and publish

**Files:**
- Modify: generated `INDEX.md` files as produced by `ci --apply`
- Delete: `.agents/plans/2026-09-16-concrete-runbook-composition.md` after completion

**Interfaces:**
- Consumes: the tracked hook and complete authored guidance tree from Tasks 1 through 3.
- Produces: a clean committed branch and Draft pull request targeting `main`.

- [ ] **Step 1: Review the full guidance graph**

Scan routers, doctrine, contracts, runbooks, scoped rules, and tests for duplicated skill workflows, stale paths, contradictory ownership, historical narration, and undeclared skill invocations. Repair every finding and repeat until a pass is clean.

- [ ] **Step 2: Restore the declared integration dependency and run focused preflight**

Run: `.\tools\postgres-dev.ps1 ensure`

Run: `py -3 tools\run.py ci --apply`

Run: `py -3 -m unittest scripts.tests.test_repo_guidance_contracts`

Expected: generated surfaces are current and focused guidance tests pass. The
normal commit hook performs the state-bound canonical check for the staged
snapshot.

- [ ] **Step 3: Commit implementation, remove the completed plan, and commit removal**

Use normal hooked commits so the staged snapshot receives the canonical apply
and check gates, including repository standards, mesh, .NET build/tests, and
web typecheck/tests/build. Remove the completed plan and generated plan index
rather than retaining a tracked archive.

- [ ] **Step 4: Publish a Draft pull request and reconcile proof**

Push `codex/repo-standards-concrete-runbooks`, open a Draft PR to `main`, and verify its remote head, body, state, and changed paths against the local committed head.
