# Agent Operating Model Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Adopt the refreshed marketplace operating model, split lifecycle runbooks from topical playbooks, and restore a convergent Wild Bunch validation gate.

**Architecture:** Subscribe Wild Bunch to the focused `agent-operating-model` capability pack and treat `.agents/skills/` as its generated projection. Keep only design, planning, implementation, review, and publication as lifecycle runbooks; move conditional repository workflows to playbooks, bind reciprocal routing explicitly, and encode consumer-specific shape and command ownership in contracts.

**Tech Stack:** Python 3 repository tooling, JSON contracts, Markdown doctrine/runbooks/playbooks, Git submodules, tracked Git hooks.

**Spec:** `.agents/skills/repo-shape/references/repository-shape-standard.md` and `.agents/skills/repo-shape/references/repository-runbook-standard.md`

**Status:** completed-awaiting-retirement

**Execution Strategy:** `executing-plans` because subscription, generated projection, taxonomy migration, mesh generation, and validation are sequential and share one repository state.

## Global Constraints

- Canonical marketplace content remains in `.agents/plugins/marketplace-source`; `.agents/skills/` is generated and must not be hand-authored.
- Lifecycle runbooks are exactly `design.md`, `planning.md`, `implementing.md`, `code-review.md`, and `pr.md`.
- Conditional repository workflows live under `.agents/playbooks/` and remain directly discoverable as well as routable from lifecycle stages.
- Doctrine states durable truth but does not orchestrate workflows; contracts state machine-checkable shapes.
- The tracked hook and hosted CI must continue to execute the same consumer-declared apply/check vectors against the candidate tree.
- Existing Wild Bunch commands, domain doctrine, local skills, `writing-pack`, and `unslop-plus` subscriptions remain intact unless the refreshed contract requires a narrow correction.
- Do not edit the marketplace submodule's authored files in this consumer PR.

## Review Focus

- Direct playbook availability: every moved topical workflow remains reachable from `.agents/playbooks/INDEX.md` without first choosing a lifecycle stage.
- Reciprocal routing: every declared runbook-to-playbook edge agrees with the playbook's runbook routing and the graph remains acyclic.
- Projection custody: a clean refresh reproduces the installed skills and provenance without orphaning local skills or writing outside declared generated paths.
- Candidate-tree parity: the tracked hook and hosted workflow use the command contract without relying on the caller's unstaged marketplace state.
- Lifecycle custody: completed artifacts follow the two-slice `completed-awaiting-retirement` process and abandoned artifacts promote durable content before removal.

---

### Task 1: Subscribe and project the focused operating-model capabilities

**Files:**
- Modify: `.agents/plugins/marketplace.json`
- Modify: `.agents/plugins/marketplace-source`
- Regenerate: `.agents/skills/**`
- Regenerate: `.agents/skills/.provenance.json`
- Create: `.agents/contracts/agent-operating-model.json`
- Modify: `.agents/contracts/repo-standards-commands.json`

**Interfaces:**
- Consumes: the pinned marketplace plugin at `codex-marketplace/plugins/agent-operating-model` and Wild Bunch's exact `repo.local_skills` list.
- Produces: installed `repo-standards`, `repo-shape`, `repo-composition`, `repo-agent-assets`, `repository-validation`, `tracked-repo-hooks`, `command-bus`, `markdown-formatting`, and `python` capabilities plus valid consumer contracts.

- [x] **Step 1: Add `agent-operating-model` as an `INSTALLED_BY_DEFAULT` GitHub-source plugin without changing existing subscriptions or local-skill registrations.**

- [x] **Step 2: Refresh the generated projection from the pinned marketplace source.**

```powershell
py -3 .agents/skills/refreshing-installed-skills/scripts/refresh_installed_skills.py --apply
```

- [x] **Step 3: Scaffold the missing consumer conformance contract, then record only Wild Bunch-specific exceptions or unslop roots if the repository actually has them.**

```powershell
py -3 .agents/skills/repo-shape/scripts/scaffold_operating_model_contract.py --apply
```

- [x] **Step 4: Add the exact generated paths owned by the canonical apply command to `.agents/contracts/repo-standards-commands.json`, preserving the existing `tools/run.py ci --apply` and `ci --check --diagnostics` vectors.**

- [x] **Step 5: Check projection and contract convergence.**

```powershell
py -3 .agents/skills/refreshing-installed-skills/scripts/refresh_installed_skills.py --check
py -3 .agents/skills/repo-shape/scripts/repo_standards.py --check
```

- [x] **Step 6: Commit the subscription, pin, generated projection, provenance, and contracts together.**

```powershell
git add .agents/plugins .agents/skills .agents/contracts
git commit -m "chore: adopt focused agent operating model"
```

### Task 2: Split lifecycle runbooks from topical playbooks

**Files:**
- Modify: `.agents/doctrine/repo-runbook-policy.md`
- Modify: `.agents/doctrine/completed-artifacts.md`
- Modify: `AGENTS.md`
- Keep and modify: `.agents/runbooks/design.md`
- Keep and modify: `.agents/runbooks/planning.md`
- Keep and modify: `.agents/runbooks/implementing.md`
- Keep and modify: `.agents/runbooks/code-review.md`
- Keep and modify: `.agents/runbooks/pr.md`
- Move and modify: `.agents/runbooks/asset-selection-cut-normalization.md` to `.agents/playbooks/asset-selection-cut-normalization.md`
- Move and modify: `.agents/runbooks/code-style.md` to `.agents/playbooks/code-style.md`
- Move and modify: `.agents/runbooks/completing-plans.md` to `.agents/playbooks/completing-plans.md`
- Move and modify: `.agents/runbooks/dev-overlay.md` to `.agents/playbooks/dev-overlay.md`
- Move and modify: `.agents/runbooks/marketplace-generation.md` to `.agents/playbooks/marketplace-generation.md`
- Move and modify: `.agents/runbooks/security.md` to `.agents/playbooks/security.md`
- Move and modify: `.agents/runbooks/seeded-game-setup.md` to `.agents/playbooks/seeded-game-setup.md`
- Move and modify: `.agents/runbooks/skill-authoring.md` to `.agents/playbooks/skill-authoring.md`
- Move and modify: `.agents/runbooks/testing.md` to `.agents/playbooks/testing.md`
- Move and modify: `.agents/runbooks/town-hub-asset-production.md` to `.agents/playbooks/town-hub-asset-production.md`
- Move and modify: `.agents/runbooks/ui-browser-check.md` to `.agents/playbooks/ui-browser-check.md`
- Regenerate: `.agents/runbooks/INDEX.md`
- Generate: `.agents/playbooks/INDEX.md`
- Update: repository links that still target moved topical files.

**Interfaces:**
- Consumes: Task 1's installed `repo-composition` and `repo-shape` contracts plus current Wild Bunch workflow content.
- Produces: five lifecycle roots with `Playbook routing`, eleven directly discoverable topical playbooks with `Runbook routing`, a non-cyclic reciprocal composition graph, and a policy that maps both standard categories separately.

- [x] **Step 1: Move each topical workflow with `git mv`, preserving its repository-specific commands, paths, doctrine bindings, and evidence.**

- [x] **Step 2: Replace each moved file's runbook title with a playbook title and append `## Runbook routing` containing only the lifecycle stages that commonly compose it; use `None.` where no lifecycle edge is justified.**

- [x] **Step 3: Add `## Playbook routing` to all five lifecycle runbooks and declare each applicable topical edge with its activation condition.**

- [x] **Step 4: Rewrite `.agents/doctrine/repo-runbook-policy.md` with separate `Standard runbooks` and `Standard playbooks` tables, repo-specific playbooks in the playbook section, and no workflow procedure in doctrine.**

- [x] **Step 5: Update completed-artifact doctrine to state the successor-slice, `completed-awaiting-retirement`, and explicit-abandonment invariants while leaving procedural sequencing to `/completing-planning-artifacts` and the completion playbook.**

- [x] **Step 6: Point root `AGENTS.md` directly to both generated inventories and update all tracked links to the moved playbook paths.**

- [x] **Step 7: Regenerate the navigation mesh and validate routing edges.**

```powershell
py -3 .agents/skills/generating-agent-mesh/scripts/generate_index_mesh.py --apply
py -3 .agents/skills/generating-agent-mesh/scripts/validate_agent_mesh.py --check
py -3 .agents/skills/repo-shape/scripts/repo_standards.py --check
```

- [x] **Step 8: Commit the taxonomy migration and regenerated indexes.**

```powershell
git add AGENTS.md .agents/doctrine .agents/runbooks .agents/playbooks
git commit -m "docs: separate lifecycle runbooks from playbooks"
```

### Task 3: Align executable validation and prove the migration

**Files:**
- Modify as required by failed evidence: `tools/run.py`
- Modify as required by failed evidence: `scripts/tests/test_repo_guidance_contracts.py`
- Modify as required by failed evidence: `scripts/tests/test_ci_workflow.py`
- Modify as required by failed evidence: `.github/workflows/ci.yml`
- Modify as required by failed evidence: `githooks/pre-commit`
- Modify as required by generated drift: repository-owned generated `INDEX.md` files.

**Interfaces:**
- Consumes: Task 1's command and conformance contracts and Task 2's final composition graph.
- Produces: one canonical apply/check path, tracked-hook and hosted-CI parity, focused behavioral coverage for Wild Bunch's local guidance contract, and a clean full repository gate.

- [x] **Step 1: Run focused repository-guidance and CI-workflow tests to expose stale path, taxonomy, or command assumptions.**

```powershell
py -3 -m pytest scripts/tests/test_repo_guidance_contracts.py scripts/tests/test_ci_workflow.py -q
```

- [x] **Step 2: Update only consumer-owned code or behavioral tests that encode obsolete runbook paths, plugin ownership, generated-path declarations, or hosted-hook parity. Do not add snapshot-like change detectors for prose.**

- [x] **Step 3: Apply and check the complete uncommitted repository gate once to prove convergence before the final commit.**

```powershell
py -3 tools/run.py ci --apply
py -3 tools/run.py ci --check --diagnostics
```

- [x] **Step 4: Stage the complete intended tree and make the normal hooked commit so the tracked hook proves the exact candidate tree.**

```powershell
git add -A
git commit -m "test: align operating model validation"
```

- [x] **Step 5: Verify the committed head is clean and the read-only audits converge without mutation.**

```powershell
git status --short
py -3 .agents/skills/refreshing-installed-skills/scripts/refresh_installed_skills.py --check
py -3 .agents/skills/repo-shape/scripts/repo_standards.py --check
```
