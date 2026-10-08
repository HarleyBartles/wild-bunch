# Code review runbook

## When

Reviewing a Wild Bunch diff, branch, or pull request.

## Required capabilities

- Provide independent review of a committed diff and select relevant domain and quality lenses.
- Analyze review feedback, resolve findings with evidence, and verify the resulting head.

## Composition

1. Use independent code review to review the actual committed diff and select
   the applicable Wild Bunch capability and anti-slop lenses.
2. Check the diff against the doctrine, contracts, local commands, and evidence
   obligations below rather than against the PR summary.
3. Compare relevant decisions from the [catalogue](../../docs/decisions/README.md) with the diff and check required history updates using the [decision-record playbook](../playbooks/decision-records.md).
4. Route every accepted finding through review finding analysis and correction; re-check the
   repaired diff rather than trusting the response.
5. Use evidence-based result verification to bind the final verdict to the
   current local head and, when a PR exists, its remote head and checks.

## Doctrine and contracts

- Backend changes: [architecture guardrails](../doctrine/architecture-guardrails.md)
  and [event-sourcing integrity](../doctrine/event-sourcing-integrity.md).
- Frontend changes: [frontend standards](../doctrine/frontend-standards.md).
- Tests: [validation doctrine](../doctrine/validation-policy.md).
- Read [code-review guards](../unslop/code-review.md) in full before review, plus [backend](../unslop/backend-architecture.md), [play-surface UI](../unslop/play-surface-ui.md), [dev overlay](../unslop/dev-overlay.md) and [writing](../unslop/writing.md) for touched concerns. Changed agent guidance also requires [routing guards](../unslop/routing.md). Select and maintain observations through the [unslop loop](../unslop/README.md).

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
