# Implementing runbook

## When

Implementing an approved Wild Bunch change in a task worktree.

## Required capabilities

- Develop observable behavior through focused, behavior-first tests.
- Verify claims against current repository and hosted evidence.

## Optional capabilities

- None.

## Required repository-owned skills

- None.

## Optional repository-owned skills

- None.

## Unslop before work

Before implementation, read the applicable [backend](../unslop/backend-architecture.md), [web](../unslop/play-surface-ui.md) and [dev-overlay](../unslop/dev-overlay.md) profiles in full. Authored prose requires [writing](../unslop/writing.md); agent guidance requires [routing](../unslop/routing.md). Recheck applicability at boundary changes and maintain [distinct observations](../unslop/README.md#record-and-improve) while working.

## Composition

1. Read the approved plan and invoke only the focused Wild Bunch capability
   skills whose declared boundaries the change crosses.
2. Bind those decisions to the applicable doctrine and contracts below.
3. Use behavior-focused test development to construct each observable behavior through
   the focused test lane named by validation doctrine.
4. Refresh ADR freshness metadata and run the repository's structural checks
   with `py -3 tools/run.py ci --apply`, stage the intended tree, and use the
   normal hooked commit. This command does not regenerate agent guidance or
   install subscribed assets.
5. Use evidence-based result verification to reconcile the committed head with
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
then use [testing](../playbooks/testing.md). A normal commit uses the installed hook over the
exact staged snapshot.

## Evidence contract

- [ ] The implemented behavior has focused automated or browser proof.
- [ ] Applicable doctrine and contracts are satisfied at the committed head.
- [ ] Generated ADR freshness metadata is current when applicable.
- [ ] The normal hooked commit passed the canonical staged-snapshot gate.

## Prohibited combinations

- Do not use a capability skill to sequence the repository delivery lifecycle.
- Do not bypass the hooked commit.

## Playbook routing

- [Code style](../playbooks/code-style.md) - when source or technical prose changes.
- [Testing](../playbooks/testing.md) - whenever behavior or validation changes.
- [Security](../playbooks/security.md) - when side effects, permissions, secrets, hidden truth, or sensitive mutation boundaries change.
- [Dev overlay](../playbooks/dev-overlay.md) - when developer controls or panels change.
- [Plugin subscriptions and authored skills](../playbooks/marketplace-generation.md) - when native plugin subscriptions, AOM standard subscriptions or certification, or repository-authored skills change.
- [Seeded game setup](../playbooks/seeded-game-setup.md) - when setup ownership or deterministic setup changes.
- [Skill authoring](../playbooks/skill-authoring.md) - when a repository-local skill changes.
- [Town-hub asset production](../playbooks/town-hub-asset-production.md) - when town-hub assets are produced or revised.
- [UI browser check](../playbooks/ui-browser-check.md) - when browser behavior or visual evidence is required.
- [Asset cut and normalization](../playbooks/asset-selection-cut-normalization.md) - when an accepted asset needs deterministic processing.
