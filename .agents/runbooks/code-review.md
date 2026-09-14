# Code review runbook

## When

Reviewing a Wild Bunch diff, branch, or pull request.

## Required skills

- `/requesting-code-review` for review construction.
- `/receiving-code-review` when resolving feedback.
- `/verification-before-completion` before approval or completion claims.

## Composition

The review owner selects lenses; applicable Wild Bunch capability skills judge
domain-specific boundaries. Feedback resolution returns through
`/receiving-code-review`, then current verification gates the verdict.

## Doctrine and contracts

- Backend changes: [architecture guardrails](../doctrine/architecture-guardrails.md)
  and [event-sourcing integrity](../doctrine/event-sourcing-integrity.md).
- Frontend changes: [frontend standards](../doctrine/frontend-standards.md).
- Tests: [validation doctrine](../doctrine/validation-policy.md).
- Apply relevant binding profiles from `../contracts/unslop/` and scoped
  contract homes; portable profiles remain owned by `/unslop-profiles`.

## Local commands and paths

Use `py -3 tools/run.py ci --check` for deliberate CI-parity proof. Regenerate
the mesh through the canonical apply capability when routed files change.

## Evidence contract

The verdict names current local proof and current GitHub checks, and identifies
any applicable ADR or mesh change.

## Prohibited combinations

- Do not substitute test volume for behavioral review.
- Do not copy portable review lenses or feedback choreography here.
