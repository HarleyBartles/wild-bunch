# Planning runbook

## When

Turning settled Wild Bunch requirements into an executable plan or roadmap.

## Required capabilities

- Author an executable plan from an approved specification for a bounded change.
- Design a roadmap when delivery requires multiple consecutive plans.

## Optional capabilities

- None.

## Required repository-owned skills

- None.

## Optional repository-owned skills

- None.

## Composition

1. Select exactly one owner: spec-to-plan authoring for a bounded change or
   multi-plan roadmap design for multiple consecutive plans.
2. Read the accepted specification and bind the applicable doctrine, contracts,
   repository paths, and validation commands below.
3. Save and commit the active artifact in its declared home before execution.
4. Hand execution exact seams, exclusions, task exits, and evidence without
   copying the planning skill's method into the artifact.

## Doctrine and contracts

- [Coding discipline](../doctrine/coding-discipline.md) and
  [validation doctrine](../doctrine/validation-policy.md) always apply.
- Add [architecture guardrails](../doctrine/architecture-guardrails.md) for
  backend architecture and [frontend standards](../doctrine/frontend-standards.md)
  for browser work.
- [Completed-artifact doctrine](../doctrine/completed-artifacts.md) governs
  custody after execution.

## Local commands and paths

- Plans: `.agents/plans/YYYY-MM-DD-<feature-name>.md`
- Specifications: `.agents/specs/`
- Roadmaps: `.agents/roadmaps/`
- Transient worker material: branch-scoped `_agent-scratch`

## Evidence contract

- [ ] The plan or roadmap is committed in its declared active home.
- [ ] Every task names exact repository paths, constraints, and downstream inputs.
- [ ] Focused and canonical validation lanes are explicit.
- [ ] No unresolved design decision is silently assigned to implementation.

## Prohibited combinations

- Do not run spec-to-plan authoring and multi-plan roadmap design over the same artifact.
- Do not copy their portable planning or handoff workflow into this runbook.

## Playbook routing

- [Security](../playbooks/security.md) - when the plan contains a sensitive mutation, permission, secret, or hidden-truth boundary.
