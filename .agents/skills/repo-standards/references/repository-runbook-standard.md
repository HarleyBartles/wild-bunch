# Repo Standards

This file is the portable cross-repo standard for repo-local runbooks and agent-facing routing surfaces.

## Required root surfaces

Every repo using this standard must have:

- `AGENTS.md` with these canonical headings in order:
  1. `## Repository purpose`
  2. `## Source-of-truth split`
  3. `## Publication proof for repo work`
  4. `## Build and test commands`
  5. `## Testing instructions`
  6. `## Code style guidelines`
  7. `## Review guidelines`
  8. `## PR instructions`
  9. `## Contributing`
  10. `## Security considerations`
  11. `## Routing pointers` (repo-specific router table)
  12. `## Maintenance responsibility`

- `REVIEW.md` — review entry point. It contains first-class review concerns and routes to `.agents/runbooks/code-review.md` for detailed review methodology and to `/requesting-code-review` for execution.
- `CONTRIBUTING.md` — contributor entry point. It routes to the design, planning, implementation, and review runbooks and to the relevant repo-worker-pack and Superpowers skills. It may be a thin pointer to `.agents/runbooks/contributing.md` when the repo keeps detailed guidance there.

## Core runbook set

`.agents/runbooks/` must contain:

- `design.md`
- `planning.md`
- `implementing.md`
- `code-review.md`
- `pr.md`

## Pull request runbook policy

Every repo using this standard must define a PR workflow in `.agents/runbooks/pr.md` that includes the following policy:

- Open pull requests as **draft**.
- Keep a PR in draft while iterating, running local validation, and performing self-review.
- Only flip a PR out of draft when:
  - self-review is complete,
  - the relevant validation commands pass,
  - the branch is ready for review or merge.
- The repo's CI must not run on draft pull requests. For GitHub Actions, gate `pull_request` workflows so they run only when `github.event.pull_request.draft == false` or on `ready_for_review` activity.
- After flipping a PR to ready, wait for the remote CI run to finish and pass. Do not report the PR as green or ready based only on a passing local `ci --check`. Address remote failures before requesting human review.
- The PR body must include publication proof per the repo's `AGENTS.md`.
- Each repo's `.agents/runbooks/pr.md` must map this policy to the repo's specific remote CI command (e.g., `gh pr checks`, the repository's status check API, or an external build link).

This policy reduces wasted CI minutes while a branch is still being iterated on and ensures CI only runs on PRs the author believes are ready.

## Allowed additional runbooks

Additional `<topic>.md` files may live in `.agents/runbooks/`. They must be thin repo-specific overlays, not repeats of portable doctrine. Common additional runbooks include:

- `security.md`
- `testing.md`
- `contributing.md`
- `code-style.md`
- `marketplace-generation.md`
- `skill-authoring.md`

## Local overlay policy

Each repo keeps `.agents/doctrine/repo-runbook-policy.md`. It must:

- State that the repo follows `repo-standards`.
- Map standard runbook names to local paths.
- List existing and missing runbooks.
- Note any repo-specific exceptions.

## Workflow order

The canonical stage order is:

```text
design -> planning -> implementing -> review
```

At each stage:

1. Read this standard.
2. Read the repo's `.agents/doctrine/repo-runbook-policy.md`.
3. Invoke `/repo-worker-base` for worktree, branch, validation, and publication boundaries.
4. Read the repo-local runbook for the stage.
5. Route to the matching Superpowers skill:
   - design -> `/brainstorming`
   - planning -> `/writing-plans`
   - implementation -> `/executing-plans` or `/subagent-driven-development`
   - review -> `/requesting-code-review`

## Relationship to repo-worker-base

`repo-standards` owns runbook layout, invocation, and workflow order. `repo-worker-base` owns worktree, branch, scratch, validation, and publication boundaries. Each stage skill owns its own baseline reference and reads it as part of its own first step. Use both together for every repo-backed stage.
