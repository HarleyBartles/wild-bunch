# Review entry point

This file is the repo's review entry point. Code-review agents discover it automatically.

## Pre-review reading

- Read root [`AGENTS.md`](./AGENTS.md) for source-of-truth and publication rules.
- Read [`.agents/doctrine/repo-runbook-policy.md`](./.agents/doctrine/repo-runbook-policy.md) for the local runbook mapping.
- Read [`.agents/runbooks/code-review.md`](./.agents/runbooks/code-review.md) for the Wild Bunch review delta.

## Workflow routing

Invoke `using-superpowers-plus` once and follow its review-stage handoff.

## First-class review concerns

- Preserve GameSession ownership, DDD/CQRS/event-sourcing boundaries, replay,
  projections, upcasting, and hidden-state privacy.
- Protect the player-facing play surface and the repo's deterministic content
  guarantees.
- Use real PostgreSQL integration evidence for persistence and HTTP-pipeline
  claims.
- Confirm the current GitHub PR state and checks before approval.

Use `repo-standards` only when the review itself touches repository shape,
runbook layout, or scaffolds.
