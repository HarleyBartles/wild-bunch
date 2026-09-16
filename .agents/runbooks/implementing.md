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

1. Read the approved plan and invoke only the focused Wild Bunch capability
   skills whose declared boundaries the change crosses.
2. Bind those decisions to the applicable doctrine and contracts below.
3. Use `/test-driven-development` to construct each observable behavior through
   the focused test lane named by validation doctrine.
4. Apply generated agent surfaces with `py -3 tools/run.py ci --apply`, stage
   the intended tree, and use the normal hooked commit.
5. Use `/verification-before-completion` to reconcile the committed head with
   focused behavior proof and the hook's canonical result.

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

- [ ] The implemented behavior has focused automated or browser proof.
- [ ] Applicable doctrine and contracts are satisfied at the committed head.
- [ ] Generated surfaces are staged and current.
- [ ] The normal hooked commit passed the canonical staged-snapshot gate.

## Prohibited combinations

- Do not use a capability skill to sequence the repository delivery lifecycle.
- Do not bypass the hooked commit.
