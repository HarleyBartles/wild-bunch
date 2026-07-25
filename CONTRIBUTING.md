# Contributing

This file is the repo's contributor entry point.

## Pre-contribution reading

- Read root [`AGENTS.md`](./AGENTS.md) for source-of-truth and publication rules.
- Read [`.agents/docs/repo-guide-policy.md`](./.agents/docs/repo-guide-policy.md) for this repo's mapping to the cross-repo guide standard.
- Read [`.agents/guides/code-style-guide.md`](./.agents/guides/code-style-guide.md) for code and writing conventions.

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

- Design: `.agents/guides/design-guide.md` -> `/brainstorming`
- Planning: `.agents/guides/planning-guide.md` -> `/writing-plans`
- Implementation: `.agents/guides/implementing-guide.md` -> `/executing-plans` or `/subagent-driven-development`
- Review: `.agents/guides/code-review-guide.md` -> `/requesting-code-review`

## Repo-specific contribution notes

- <!-- list repo-specific contribution notes here -->
