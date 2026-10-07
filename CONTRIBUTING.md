# Contributing

This file is the repo's contributor entry point.

## Pre-contribution reading

- Before investigating, designing, implementing or reviewing, read [unslop selection and observations](./.agents/unslop/README.md) and the applicable profiles in full.
- Read root [`AGENTS.md`](./AGENTS.md) for source-of-truth and publication routing.
- Read [AOM subscriptions](./.agents/contracts/operating-standards.json) and
  [their certification](./.agents/contracts/standards-certification.md) when
  changing an adopted surface; maintain the affected assessment in that change.
- Read [`.agents/doctrine/repo-runbook-policy.md`](./.agents/doctrine/repo-runbook-policy.md) for this repo's mapping to the cross-repo runbook standard.
- Read [`.agents/playbooks/code-style.md`](./.agents/playbooks/code-style.md) for
  source conventions and [the writing profile](./.agents/unslop/writing.md)
  for authored prose.

## Workflow routing

Invoke `using-superpowers-plus` once. Follow its handoff to the applicable
owners; those owners read the matching local runbook.

## Repo-specific contribution notes

- For the stable 0.1.0 epic, read the [develop delivery and version contract](.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md#develop-integration-and-per-plan-delivery) before planning or implementing: each JIT plan uses a fresh develop-based worktree and a PR to develop, assesses eligible predecessor-artifact retirement, and advances the shared development version.

- Work on a task branch in a dedicated linked worktree; direct pushes to
  `main` require explicit authorization.
- Use the focused validation lane while constructing the change, then the
  canonical staged-snapshot hook and pull-request checks for delivery proof.
- Keep the repository-owned checks passing through
  `py -3 tools/run.py ci --check`. The normal hook checks the staged candidate
  and refreshes decision freshness output.
