# Security playbook

## When

A Wild Bunch change affects tool side effects, permissions, secrets, hidden
truth, or a sensitive mutation boundary.

## Required capabilities

- Evaluate connector and tool mutations for authority and side effects.
- Apply scope and safety gates when authority, source truth, or impact requires them.

## Unslop before work

Before assessing game-data or API boundaries, read [backend guards](../unslop/backend-architecture.md) in full; browser visibility also requires [play-surface UI](../unslop/play-surface-ui.md), and developer capabilities require [dev overlay](../unslop/dev-overlay.md). Follow [selection and observations](../unslop/README.md) for newly encountered patterns.

## Composition

1. Use scope, authority, source-truth, and consequence review to identify the authority, scope, hidden truth, and
   irreversible consequence before any sensitive mutation.
2. Bind the approved action to the architecture and gameplay doctrine below.
3. Use safe connector and tool mutation for the external tool or connector mutation and its
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
- [Decision records](decision-records.md) - when authority, privacy, or hidden-truth policy changes durably.
