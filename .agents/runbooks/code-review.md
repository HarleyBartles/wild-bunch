# Code review runbook

## When

Reviewing a Wild Bunch diff, branch, or pull request.

## Required capabilities

- Provide independent review of a committed diff and select relevant domain and quality lenses.
- Analyze review feedback, resolve findings with evidence, and verify the resulting head.

## Optional capabilities

- None.

## Required repository-owned skills

- None.

## Optional repository-owned skills

- None.

## Composition

1. Use independent code review to review the actual committed diff and select
   the applicable Wild Bunch capability and anti-slop lenses.
2. Check the diff against the doctrine, contracts, local commands, and evidence
   obligations below rather than against the PR summary.
3. Route every accepted finding through review finding analysis and correction; re-check the
   repaired diff rather than trusting the response.
4. Use evidence-based result verification to bind the final verdict to the
   current local head and, when a PR exists, its remote head and checks.

## Doctrine and contracts

- Backend changes: [architecture guardrails](../doctrine/architecture-guardrails.md)
  and [event-sourcing integrity](../doctrine/event-sourcing-integrity.md).
- Frontend changes: [frontend standards](../doctrine/frontend-standards.md).
- Tests: [validation doctrine](../doctrine/validation-policy.md).
- Apply relevant binding profiles from `../contracts/unslop/` and scoped
  contract homes; use an available review capability for portable profiles.

## Local commands and paths

Use `py -3 tools/run.py ci --check` for deliberate CI-parity proof. Apply the
canonical command when selected standards or plugin subscriptions change.

## Evidence contract

- [ ] The reviewed commit range and current head are explicit.
- [ ] Applicable backend, frontend, validation, and anti-slop authorities were checked.
- [ ] Each finding is resolved, rejected with evidence, or reported as open.
- [ ] Local proof and applicable GitHub checks match the reviewed head.
- [ ] Required decision records and repository-shape changes are present.

## Prohibited combinations

- Do not substitute test volume for behavioral review.
- Do not copy portable review lenses or feedback choreography here.

## Playbook routing

- [Code style](../playbooks/code-style.md) - when reviewing source or technical prose.
- [Testing](../playbooks/testing.md) - when reviewing behavior or validation evidence.
- [Security](../playbooks/security.md) - when the diff affects sensitive side effects, permissions, secrets, hidden truth, or mutation boundaries.
- [UI browser check](../playbooks/ui-browser-check.md) - when review needs browser behavior, layout, interaction, or player-flow evidence.
