# Refreshed Runbook Composition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring Wild Bunch into warning-free compliance with the refreshed repository surface taxonomy and runbook composition contract.

**Architecture:** Preserve the marketplace refresh as generated source, move binding repo-specific anti-slop profiles into contract custody, and express every runbook as a seven-section composition manifest. Restore the standard completed-artifact doctrine and add a local completion runbook without duplicating the portable `cleanup-custody` method.

**Tech Stack:** Markdown authority surfaces, JSON marketplace provenance, Python repository-standard validation, generated index mesh.

**Spec:** `repo-standards` at marketplace source `c4fb0619cc7f0237d875285c7c77325ce673357c`.

**Execution Strategy:** `manual` — the runbook edits share one taxonomy and must remain mutually consistent; no delegation is needed.

## Global Constraints

- Doctrine records stable Wild Bunch truths and never orchestrates.
- Contracts record exact binding shapes, including repo-specific anti-slop profiles.
- Runbooks name and sequence skill owners without copying their internals.
- Local skills retain one focused judgment each.
- Generated skill projections and indexes are changed only through the canonical refresh/apply capability.
- Completed plan creation and removal are both committed.

---

### Task 1: Adopt completed-artifact composition

**Files:**
- Create: `.agents/doctrine/completed-artifacts.md`
- Create: `.agents/runbooks/completing-plans.md`
- Modify: `.agents/doctrine/artifact-custody.md`
- Modify: `.agents/doctrine/repo-runbook-policy.md`

**Interfaces:**
- Consumes: refreshed `cleanup-custody` promotion-before-removal ownership.
- Produces: repository custody truth plus a Wild Bunch completion composition.

- [ ] Add the standard doctrine with concrete Wild Bunch paths and no portable cleanup method.
- [ ] Move duplicate completed-artifact truth out of `artifact-custody.md`.
- [ ] Add and map `completing-plans.md` with all seven runbook sections.
- [ ] Remove the obsolete `completed-artifacts-doctrine` exception.

### Task 2: Move repo-specific anti-slop profiles into contracts

**Files:**
- Move: `.agents/unslop/*.md` to `.agents/contracts/unslop/*.md`
- Move: `src/WildBunch.Web/.agents/unslop/*.md` to `src/WildBunch.Web/.agents/contracts/unslop/*.md`
- Modify: `.agents/doctrine/artifact-custody.md`
- Modify: `.agents/doctrine/frontend-standards.md`
- Modify: `.agents/runbooks/code-review.md`
- Modify: `.devin/rules/src-wildbunch-web.md`
- Modify: all other current consumers found by repository search

**Interfaces:**
- Consumes: refreshed contract custody rule.
- Produces: binding anti-slop profiles under repository and scoped contract homes.

- [ ] Move the profiles without changing their substantive constraints.
- [ ] Repair all current references and remove the retired live paths.
- [ ] Regenerate the complete index mesh.

### Task 3: Upgrade runbooks to composition manifests

**Files:**
- Modify: every authored `.agents/runbooks/*.md` except generated indexes and routers
- Test: refreshed `repo_standards.py --check`

**Interfaces:**
- Consumes: the seven-section runbook contract and existing Wild Bunch workflow content.
- Produces: one warning-free composition manifest per registered runbook.

- [ ] Give every runbook `When`, `Required skills`, `Composition`, `Doctrine and contracts`, `Local commands and paths`, `Evidence contract`, and `Prohibited combinations` sections.
- [ ] Preserve only repo-specific order, paths, commands, constraints, exceptions, and evidence.
- [ ] Remove any portable skill internals exposed while restructuring.
- [ ] Run the refreshed checker and resolve all warnings.

### Task 4: Validate and publish

**Files:**
- Modify: generated `.agents/skills/` projection and index mesh as produced by the canonical apply capability
- Delete: `.agents/plans/2026-09-14-refreshed-runbook-composition.md` after implementation is proven

**Interfaces:**
- Consumes: Tasks 1-3.
- Produces: validated branch and Draft pull request targeting `main`.

- [ ] Scan for old anti-slop paths, missing runbook sections, duplicated workflow ownership, and broken links.
- [ ] Run `py -3 tools\run.py ci --apply` and `py -3 tools\run.py ci --check` through normal hooked commits.
- [ ] Commit the implementation, remove the completed plan, and commit its removal.
- [ ] Push the branch, open a Draft PR, and verify its remote head and body.
