# Design runbook

## When

Designing a Wild Bunch feature or behavior before implementation planning.

## Required capabilities

- Facilitate structured feature discovery, clarify behavior, and produce an accepted design specification.

## Unslop before work
Before work in this scope, follow the [unslop playbook](../playbooks/unslop.md) and its scoped profile selection. Consult the [decision-record playbook](../playbooks/decision-records.md) when work makes, changes, corrects, or materially removes a durable decision.

## Composition

1. Use structured feature-design facilitation to settle the behavior and produce the design artifact.
2. Bind each affected Wild Bunch surface to the doctrine below and name its
   validation lane before accepting the design.
3. Save the accepted specification under `.agents/specs/` for planning; keep
   transient exploration in branch-scoped scratch.
4. Select governing ADRs through the [decision-record playbook](../playbooks/decision-records.md) and identify any durable decision the design makes or changes.
5. When the design changes a product promise, capability boundary or dependency, read and update the [feature matrix](../../docs/features.md) through its [maintenance playbook](../playbooks/feature-matrix.md).

## Doctrine and contracts

- [Coding discipline](../doctrine/coding-discipline.md) always applies.
- Add [architecture guardrails](../doctrine/architecture-guardrails.md) for
  domain, persistence, command, query, or projection work.
- Add [frontend standards](../doctrine/frontend-standards.md) for browser work.
- Add [validation doctrine](../doctrine/validation-policy.md) to select evidence
  lanes and [artifact custody](../doctrine/artifact-custody.md) for agent-surface changes.

## Local commands and paths

Active design specifications live in `.agents/specs/`.

## Evidence contract

- [ ] The accepted specification is present under `.agents/specs/`.
- [ ] It names every applicable doctrine and contract.
- [ ] It identifies the focused, integration, browser, or repository-shape evidence the
  implementation must produce.

## Prohibited combinations

- Do not reproduce structured feature-design facilitation discovery, self-review, or handoff steps.

## Playbook routing

- [Security](../playbooks/security.md) - when the design introduces sensitive side effects, authority, permissions, secrets, or hidden truth.
- [Dev overlay](../playbooks/dev-overlay.md) - when the design includes a developer control or panel.
- [Seeded game setup](../playbooks/seeded-game-setup.md) - when the design changes setup ownership or deterministic setup.
- [Town-hub asset production](../playbooks/town-hub-asset-production.md) - when the design includes a new or revised town-hub asset family.
