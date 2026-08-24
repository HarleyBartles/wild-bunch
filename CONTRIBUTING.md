# Contributing

This file is the repo's contributor entry point.

## Pre-contribution reading

- Read root [`AGENTS.md`](./AGENTS.md) for source-of-truth and publication rules.
- Read [`.agents/doctrine/repo-runbook-policy.md`](./.agents/doctrine/repo-runbook-policy.md) for this repo's runbook mapping.
- Read [`.agents/runbooks/code-style.md`](./.agents/runbooks/code-style.md) for code and writing conventions.

## PR instructions

- Work in a dedicated linked worktree under `Z:\_agent-worktrees\wild-bunch\<task-name>`.
- Branch from current `origin/main` and keep the branch current.
- Push the branch and open a draft PR.
- Run `\scripts\ci-preflight.ps1` before marking the PR ready for review.
- Do not push directly to `main` without explicit authorization.

## Required skill invocations

Before starting work, invoke:

- `/repo-standards` for repo-shape and guide routing.
- `/repo-worker-base` for worktree, branch, validation, and publication boundaries.

## Stage routing

- Design: `.agents/runbooks/design.md` -> `/brainstorming`
- Planning: `.agents/runbooks/planning.md` -> `/writing-plans`
- Implementation: `.agents/runbooks/implementing.md` -> `/executing-plans` or `/subagent-driven-development`
- Review: `.agents/runbooks/code-review.md` -> `/requesting-code-review`

## Repo-specific contribution notes

- <!-- list repo-specific contribution notes here -->
