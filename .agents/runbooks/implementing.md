# Implementing runbook

Use this local overlay after `using-superpowers-plus` selects the implementation
owner.

## Required local reading

- [Coding discipline](../doctrine/coding-discipline.md) for scope and architecture
  boundaries.
- [Validation doctrine](../doctrine/validation-policy.md) before changing tests.
- [Architecture guardrails](../doctrine/architecture-guardrails.md) before changing
  GameSession, persistence, domain logic, commands, queries, or projections.
- [Gameplay invariants](../doctrine/gameplay-invariants.md) before changing
  player state, investigation truth, or travel behavior.
- [Frontend standards](../doctrine/frontend-standards.md) before browser work.

## Wild Bunch validation

Select focused tests from [validation doctrine](../doctrine/validation-policy.md),
then use the sequence in the [testing runbook](testing.md). For a normal commit,
stage the intended tree and let the installed pre-commit hook check that exact
snapshot.
