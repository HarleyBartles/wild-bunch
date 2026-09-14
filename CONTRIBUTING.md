# Contributing

This file is the repo's contributor entry point.

## Pre-contribution reading

- Read root [`AGENTS.md`](./AGENTS.md) for source-of-truth and publication routing.
- Read [`.agents/doctrine/repo-runbook-policy.md`](./.agents/doctrine/repo-runbook-policy.md) for this repo's mapping to the cross-repo runbook standard.
- Read [`.agents/runbooks/code-style.md`](./.agents/runbooks/code-style.md) for
  source conventions and [the writing contract](./.agents/contracts/unslop/writing.md)
  for authored prose.

## Workflow routing

Invoke `using-superpowers-plus` once. Follow its handoff to the applicable
owners; those owners read the matching local runbook.

## Repo-specific contribution notes

- Work on a task branch in a dedicated linked worktree; direct pushes to
  `main` require explicit authorization.
- Use the focused validation lane while constructing the change, then the
  canonical staged-snapshot hook and pull-request checks for delivery proof.
- Keep generated agent indexes and installed skill projections synchronized
  through `py -3 tools/run.py ci --apply`; do not hand-edit them.
