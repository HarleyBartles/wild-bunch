# Planning runbook

## When

Turning settled Wild Bunch requirements into an executable plan or roadmap.

## Required skills

- `/writing-plans` for one bounded plan.
- `/writing-roadmaps` when the goal requires consecutive plans.

## Composition

Use exactly one planning owner for the artifact scale. It binds the relevant
repository doctrine below and hands execution a committed plan.

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

The committed artifact names exact repository paths, applicable test lanes,
and downstream inputs without inventing unresolved design.

## Prohibited combinations

- Do not run `/writing-plans` and `/writing-roadmaps` over the same artifact.
- Do not copy their portable planning or handoff workflow into this runbook.
