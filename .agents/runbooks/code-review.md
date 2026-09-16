# Code review runbook

## When

Reviewing a Wild Bunch diff, branch, or pull request.

## Required skills

- `/requesting-code-review` for review construction.
- `/receiving-code-review` when resolving feedback.
- `/verification-before-completion` before approval or completion claims.

## Composition

1. Use `/requesting-code-review` to review the actual committed diff and select
   the applicable Wild Bunch capability and anti-slop lenses.
2. Check the diff against the doctrine, contracts, local commands, and evidence
   obligations below rather than against the PR summary.
3. Route every accepted finding through `/receiving-code-review`; re-check the
   repaired diff rather than trusting the response.
4. Use `/verification-before-completion` to bind the final verdict to the
   current local head and, when a PR exists, its remote head and checks.

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

- [ ] The reviewed commit range and current head are explicit.
- [ ] Applicable backend, frontend, validation, and anti-slop authorities were checked.
- [ ] Each finding is resolved, rejected with evidence, or reported as open.
- [ ] Local proof and applicable GitHub checks match the reviewed head.
- [ ] Required ADR and mesh changes are present.

## Prohibited combinations

- Do not substitute test volume for behavioral review.
- Do not copy portable review lenses or feedback choreography here.
