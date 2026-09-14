# Implementing runbook

## When

Implementing an approved Wild Bunch change in a task worktree.

## Required skills

- `/test-driven-development`
- `/verification-before-completion`
- `/wild-bunch-domain-modeling` for gameplay and aggregate decisions.
- `/wild-bunch-dotnet-architecture` for C# application and persistence boundaries.
- `/wild-bunch-browser-game` for browser state and presentation ownership.
- `/seed-ownership`, `/dev-control-boundary`, or
  `/town-hub-asset-judgment` when that focused judgment is in scope.

## Composition

The focused capability establishes ownership, `/test-driven-development`
constructs the change, and `/verification-before-completion` gates the result.

## Doctrine and contracts

- [Coding discipline](../doctrine/coding-discipline.md) always applies.
- [Architecture guardrails](../doctrine/architecture-guardrails.md) applies to
  GameSession, persistence, domain, command, query, or projection changes.
- [Gameplay invariants](../doctrine/gameplay-invariants.md) applies to player,
  investigation, or travel behavior.
- [Frontend standards](../doctrine/frontend-standards.md) applies to browser work.

## Local commands and paths

Select focused tests from [validation doctrine](../doctrine/validation-policy.md),
then use [testing](testing.md). A normal commit uses the installed hook over the
exact staged snapshot.

## Evidence contract

Focused behavior proof and the canonical staged-snapshot gate both pass.

## Prohibited combinations

- Do not use a capability skill to sequence the repository delivery lifecycle.
- Do not bypass the hooked commit.
