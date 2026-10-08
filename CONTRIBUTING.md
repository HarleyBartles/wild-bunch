# Contributing

This file is the repo's contributor entry point.

## Pre-contribution reading

- Before investigating, designing, implementing or reviewing, read the [unslop playbook](./.agents/playbooks/unslop.md) and follow its scoped profile selection.
- Read root [`AGENTS.md`](./AGENTS.md) for source-of-truth and publication routing.
- Read [AOM subscriptions](./.agents/contracts/operating-standards.json) and
  [their certification](./.agents/contracts/standards-certification.md) when
  changing an adopted surface; maintain the affected assessment in that change.
- Read [`.agents/doctrine/repo-runbook-policy.md`](./.agents/doctrine/repo-runbook-policy.md) for this repo's mapping to the cross-repo runbook standard.
- Read the [decision-record playbook](./.agents/playbooks/decision-records.md) when work makes, changes, corrects, or materially removes a durable decision; the lifecycle runbooks carry the author and reviewer obligations.
- For work that changes a product promise, disposition, capability boundary or dependency, follow the [feature-matrix playbook](./.agents/playbooks/feature-matrix.md) and update the [authoritative matrix](./docs/features.md) with the same change.
- Use the [repository command bus guide](./tools/README.md) to discover supported build, test, and validation targets.
- Read [`.agents/playbooks/code-style.md`](./.agents/playbooks/code-style.md) for
  source conventions; use the unslop playbook's scoped selection for authored prose.

## Workflow routing

Invoke `using-superpowers-plus` once. Follow its handoff to the applicable
owners; those owners read the matching local runbook.

## Repo-specific contribution notes

- Work on a task branch in a dedicated linked worktree; direct pushes to
  `main` require explicit authorization.
- Use the focused validation lane while constructing the change, then the
  canonical staged-snapshot hook and pull-request checks for delivery proof.
- Keep the repository-owned checks passing through
  `py -3 tools/run.py ci --check`. The normal hook checks the staged candidate
  without mutating tracked files. Update the authored ADR catalogue deliberately when the relevant decision changes.
