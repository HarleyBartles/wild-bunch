# Security playbook

## When

A Wild Bunch change affects tool side effects, permissions, secrets, hidden
truth, or a sensitive mutation boundary.

## Required skills

- `/connector-safety` for connector and tool mutations.
- `/risk-gates` when scope, authority, source truth, or safety needs a gate.

## Composition

1. Use `/risk-gates` to identify the authority, scope, hidden truth, and
   irreversible consequence before any sensitive mutation.
2. Bind the approved action to the architecture and gameplay doctrine below.
3. Use `/connector-safety` for the external tool or connector mutation and its
   readback; keep unrelated writes separate.
4. Run the focused security/behavior tests and the testing runbook's delivery gate.

## Doctrine and contracts

[Architecture guardrails](../doctrine/architecture-guardrails.md) and
[gameplay invariants](../doctrine/gameplay-invariants.md) protect backend and
hidden-truth boundaries.

## Local commands and paths

Use the relevant focused tests and [testing](testing.md); never place secrets in
repo files or command output.

## Evidence contract

- [ ] The action's authority and exact scope are recorded.
- [ ] The mutation and independent readback agree.
- [ ] No secret or hidden game truth enters repo files, logs, or player APIs.
- [ ] Any unresolved permission or exposure boundary is reported explicitly.

## Prohibited combinations

- Do not bundle sensitive mutations with unrelated writes.
- Do not expose hidden game truth through player-facing APIs.

## Runbook routing

- [Design](../runbooks/design.md)
- [Planning](../runbooks/planning.md)
- [Implementing](../runbooks/implementing.md)
- [Code review](../runbooks/code-review.md)
