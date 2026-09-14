# Agent Workflow Ownership Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Wild Bunch agent guidance consistently distinguish doctrine, contracts, focused skills, and runbook composition while using explicit local-skill registration as the sole custody mechanism.

**Architecture:** Doctrine records stable repository truth, contracts record independently consumed shapes, local skills own one focused judgment, and runbooks compose skills with Wild Bunch paths, commands, gates, and evidence. Marketplace projections remain generated; only registered repository-local skills are authored here.

**Tech Stack:** Markdown agent guidance, JSON marketplace registration, Python validation tooling, generated `INDEX.md` mesh.

**Spec:** Approved ownership model in the 2026-09-14 conversation for PR #177.

**Execution Strategy:** `manual` — sequential edits in the current worktree; no subagents or iterative review.

## Global Constraints

- Local skill custody comes only from exact names in `repo.local_skills`; `wild-bunch-*` is not a protected prefix.
- A skill owns one focused capability or judgment; a runbook composes skills into a legitimate repository workflow.
- Doctrine states what must remain true and must not contain workflow sequencing or skill composition.
- Contracts define exact shapes consumed or verified independently.
- Do not edit marketplace-projected skills; repository-local registered skills are in scope.
- Preserve current game behavior and the Draft state of PR #177.

---

### Task 1: Replace prefix custody with explicit registration

**Files:**
- Modify: `.agents/plugins/marketplace.json`
- Modify: `.agents/doctrine/repo-skills-policy.md`
- Modify: `.agents/doctrine/mesh-policy.md`
- Delete or replace: `scripts/validate_local_skills_extra.py`, `.ps1`, `.sh`
- Modify: `scripts/README.md`, `scripts/tests/test_power_shell_wrappers.py`
- Test: `scripts/tests/test_run.py` or a focused marketplace-policy test

**Interfaces:**
- Consumes: `repo.local_skills` and `refreshing-installed-skills` exact-name validation.
- Produces: exact registration as the sole local-skill custody signal.

- [ ] Add a failing focused test proving local skills are not discovered or validated by prefix.
- [ ] Run it and confirm the old prefix validator makes the test fail for the intended reason.
- [ ] Remove prefix-based validation and all claims that `wild-bunch-*` is protected.
- [ ] Preserve exact-name validation through the installed refresh capability.
- [ ] Run focused Python tests and marketplace refresh check.

### Task 2: Make repository-local skills lean judgment owners

**Files:**
- Delete: `.agents/skills/wild-bunch-project-doctrine/`
- Modify: `.agents/skills/wild-bunch-domain-modeling/`
- Modify: `.agents/skills/wild-bunch-dotnet-architecture/`
- Modify: `.agents/skills/wild-bunch-browser-game/`
- Create: `.agents/skills/seed-ownership/`
- Create: `.agents/skills/dev-control-boundary/`
- Create: `.agents/skills/town-hub-asset-judgment/`
- Modify: `.agents/plugins/marketplace.json`

**Interfaces:**
- Consumes: current doctrine, asset bibles, generic architecture/frontend/image skills.
- Produces: registered skills that each own one decision and return a bounded result to a composing runbook.

- [ ] Record focused baseline failure cases from the current files: copied current truth, broad lifecycle composition, stale paths, and unavailable skill names.
- [ ] Retire the broad project bootstrap skill and register `seed-ownership`.
- [ ] Thin domain-modeling and .NET-architecture skills to workflows that read doctrine rather than copying it.
- [ ] Thin browser-game to the browser state/ownership decision and repair its skill routes.
- [ ] Create `dev-control-boundary` for state-preparation, panel ownership, and hidden-truth judgment.
- [ ] Create `town-hub-asset-judgment` for family/camera/seam/retry/promotion decisions.
- [ ] Validate each skill structurally and against its focused failure cases before moving to the next.

### Task 3: Put repository workflow composition in runbooks and contracts

**Files:**
- Create: `.agents/runbooks/seeded-game-setup.md`
- Create: `.agents/runbooks/dev-overlay.md`
- Create: `.agents/runbooks/town-hub-asset-production.md`
- Modify: `.agents/runbooks/implementing.md`
- Modify: `.agents/runbooks/marketplace-generation.md`
- Modify: `.agents/runbooks/skill-authoring.md`
- Modify: `.agents/runbooks/testing.md`
- Modify: `.agents/runbooks/ui-browser-check.md`
- Modify: `.agents/runbooks/asset-selection-cut-normalization.md`
- Create: `.agents/contracts/dev-overlay-proof.md`
- Modify: `.agents/doctrine/repo-runbook-policy.md`

**Interfaces:**
- Consumes: focused skill outputs and doctrine constraints.
- Produces: legitimate skill combinations, local command/path deltas, and an explicit dev-overlay evidence shape.

- [ ] Write runbooks as compositions of named skills with observable conditions for each combination.
- [ ] Keep skill internals out of runbooks; retain only Wild Bunch paths, commands, order, exceptions, and evidence gates.
- [ ] Move strict dev-overlay proof fields into the contract and have the runbook reference it.
- [ ] Update the runbook mapping with every additional local runbook.
- [ ] Check that no runbook restates a focused skill workflow.

### Task 4: Thin doctrine and repair routing

**Files:**
- Modify: `.agents/doctrine/architecture-guardrails.md`
- Modify: `.agents/doctrine/art/*.md`
- Modify: `.agents/doctrine/artifact-custody.md`
- Modify: `.agents/doctrine/coding-discipline.md`
- Modify: `.agents/doctrine/dev-overlay.md`
- Modify: `.agents/doctrine/event-sourcing-integrity.md`
- Modify: `.agents/doctrine/frontend-standards.md`
- Modify: `.agents/doctrine/game-content-seed-pipeline.md`
- Modify or delete: `.agents/doctrine/skill-authoring-policy.md`
- Modify: `.agents/doctrine/validation-policy.md`
- Modify: `.devin/rules/*.md`, ADR current-reference sections, and other routed consumers

**Interfaces:**
- Consumes: runbook and skill destinations from Tasks 2-3.
- Produces: doctrine containing only live Wild Bunch truths and constraints.

- [ ] Remove commands, read order, retry sequences, skill composition, validation workflow, and closeout choreography from doctrine.
- [ ] Preserve visual, architectural, domain, custody, source-truth, and ownership invariants.
- [ ] Repair all current references; preserve explicitly dated ADR history unchanged.
- [ ] Regenerate the repository mesh and scan for retired paths, unavailable skill names, workflow language in doctrine, and broken links.
- [ ] Run `py -3 tools\run.py ci --check`.
- [ ] Commit normally, remove this completed plan, commit the removal, push, update PR #177, and verify the remote head/body/Draft state.
