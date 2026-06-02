---
name: validation-selection
description: Choose and report lawful validation classes for Will-owned changes, including governance, skills, scripts, indexes, publication-boundary work, and vague validation claims. Use when a dispatcher, worker, or reviewer needs to select sufficient validation, classify skipped checks, or challenge "tests passed" claims without duplicating publication or verifier doctrine.
---

# validation-selection

## Purpose

Use this skill to choose the narrowest sufficient validation for a change and to report that choice clearly.

Keep validation selection separate from validation execution, cleanup evidence, publication proof, and verifier closure.

## Required Reads

- `../../Doctrine/Governance/VALIDATION_SELECTION_POLICY.md`
- `../../Doctrine/Contracts/ACTOR_BOUND_WORKER_RETURN_CONTRACT.md`
- `../../Doctrine/Governance/MAINLINE_ONLY_PUBLICATION_POLICY.md`
- `../git-github-worker-operations/SKILL.md`
- `../will-mainline-stack-publication/SKILL.md`

## When To Use

- A worker needs to decide what validation is sufficient for a repo, governance, skill, script, or documentation change.
- A dispatcher needs to state the selected validation class without micromanaging every command.
- A worker return says "tests passed" but does not say what was selected or what ran.
- A reviewer needs to challenge under-validation or overbroad validation.
- An issue-backed dispatch needs validation selected separately from issue-goal conformance, so the worker can prove the chosen checks actually cover the observed goal.

## When Not To Use

- You are writing publication doctrine, worker-return doctrine, or verifier doctrine.
- You are proving remote publication or issue closure.
- You are replacing actor-local authority with a generic validation plan.

## Workflow

1. Identify the changed surface and authority owner.
2. Decide whether any protected surface, gitlink, or publication boundary is involved.
3. Select the narrowest sufficient validation class.
4. List concrete checks or commands when known.
5. Record skipped validations with reasons.
6. Separate planned validation, local validation results, cleanup evidence, publication proof, and verifier evidence.
7. Challenge both under-validation and overbroad validation.
8. For issue-backed work, record the issue goal as observable state and the surfaces that would falsify it separately from the validation class.

## Validation Class Guide

- `none/no-op` - no meaningful tracked change or no lawful validation surface
- `docs-only` - policy text, narrative docs, or prose-only edits
- `structural-markdown/index` - navigation, discovery, or link topology
- `skill/package` - skill bundle contents or metadata
- `script/tooling` - helper scripts, automation, or executable glue
- `data/schema/parse` - parseability, schema, or structured-format integrity
- `ProjectDB-adjacent read-only` - safe read-only inspection near ProjectDB
- `ProjectDB mutation-authorized` - live mutation with explicit authority
- `canon/world` - world/canon-bearing surfaces
- `manuscript` - manuscript/prose-bearing surfaces
- `governed-trash/sentinel` - temp, sentinel, or residue handling
- `publication-boundary` - gitlink or pointer-sensitive work
- `smoke` - minimal end-to-end check
- `focused` - narrow confidence check
- `regression` - wider compatibility check
- `full` - broadest lawful check for the change

## Examples

### Docs-only governance edit

- Changed surface: policy text under `Doctrine/Governance/`
- Validation class: `docs-only`
- Checks: markdown lint, link sanity, diff review
- Skip: heavy runtime checks, because no executable behavior changed

### Source-controlled skill update

- Changed surface: `Skills/<bundle>/SKILL.md` and `agents/openai.yaml`
- Validation class: `skill/package`
- Checks: YAML parse, bundle shape, index link sanity, diff review
- Skip: repo-wide execution checks unless the skill actually changes a script or publish path

### Script/tooling change

- Changed surface: executable helper or validation script
- Validation class: `script/tooling`
- Checks: syntax, targeted test or smoke run, diff review
- Skip: broad regression unless the helper is used across multiple workflows

### ProjectDB-adjacent read-only validation

- Changed surface: read-only ProjectDB-facing query or dossier path
- Validation class: `ProjectDB-adjacent read-only`
- Checks: read-only path verification, query shape, no mutation path, result sanity
- Skip: mutation checks because authority is read-only only

### Governed-trash sentinel or index update

- Changed surface: `_tmp` sentinel, governed trash, or discovery index
- Validation class: `governed-trash/sentinel` or `structural-markdown/index`
- Checks: path hygiene, link sanity, sentinel semantics, diff review
- Skip: product-QA or release ceremony

### Publication-boundary or gitlink-sensitive work

- Changed surface: submodule pointer, parent bump, or mainline publication boundary
- Validation class: `publication-boundary`
- Checks: pointer diff review, helper preflight, remote verification, diff sanity
- Skip: generic QA ceremony that does not prove boundary publication

### Local manual validation evidence

- Changed surface: visible workflow or local end-to-end behavior
- Validation class: `smoke` or `focused`, depending on issue goal
- Checks: local command results plus the observed workflow
- Separate proof: cleanup evidence for worker-started local helpers and workspace residue risk
- Skip: treating the local validation result as overall GREEN until cleanup evidence, publication proof, and issue-goal conformance are separately satisfied

## Output Expectations

When asked to report validation-selection, state:

- selected validation class
- why that class is sufficient
- what was intentionally skipped
- whether the selection is only planned or already executed
- whether cleanup evidence is required or already supplied when local validation used worker-started helpers or touched a local workspace
- whether publication proof or verifier evidence is still needed

## False-Green Risks

- Treating a selected class as proof that checks ran.
- Hiding skipped checks behind vague "reasonable" language.
- Treating local validation as safe closeout when worker-owned helper or workspace-residue evidence is missing.
- Importing product-QA, Figma, browser, device, or release-management ceremony.
- Duplicating publication or verifier doctrine inside this skill.
- Letting validation selection bypass authority routing or protected-surface checks.
- Treating selected validation as proof of issue-goal conformance.
