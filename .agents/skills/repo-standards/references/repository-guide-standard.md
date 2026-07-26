# Repo Standards

This file is the portable cross-repo standard for repo-local guides and agent-facing routing surfaces.

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

- `REVIEW.md` — review entry point. It contains first-class review concerns and routes to `.agents/guides/code-review-guide.md` for detailed review methodology and to `/requesting-code-review` for execution.
- `CONTRIBUTING.md` — contributor entry point. It routes to the design, planning, implementation, and review guides and to the relevant repo-worker-pack and Superpowers skills. It may be a thin pointer to `.agents/guides/contributing-guide.md` when the repo keeps detailed guidance there.

## Core guide set

`.agents/guides/` must contain:

- `design-guide.md`
- `planning-guide.md`
- `implementing-guide.md`
- `code-review-guide.md`
- `pr-guide.md`

## Pull request guide policy

Every repo using this standard must define a PR workflow in `.agents/guides/pr-guide.md` that includes the following policy:

- Open pull requests as **draft**.
- Keep a PR in draft while iterating, running local validation, and performing self-review.
- Only flip a PR out of draft when:
  - self-review is complete,
  - the relevant validation commands pass,
  - the branch is ready for review or merge.
- The repo's CI must not run on draft pull requests. For GitHub Actions, gate `pull_request` workflows so they run only when `github.event.pull_request.draft == false` or on `ready_for_review` activity.
- After flipping a PR to ready, monitor CI and address failures before requesting human review.
- The PR body must include publication proof per the repo's `AGENTS.md`.

This policy reduces wasted CI minutes while a branch is still being iterated on and ensures CI only runs on PRs the author believes are ready.

## Allowed additional guides

Additional `<topic>-guide.md` files may live in `.agents/guides/`. They must be thin repo-specific overlays, not repeats of portable doctrine. Common additional guides include:

- `security-guide.md`
- `testing-guide.md`
- `contributing-guide.md`
- `code-style-guide.md`
- `marketplace-generation-guide.md`
- `skill-authoring-guide.md`

## Local overlay policy

Each repo keeps `.agents/docs/repo-guide-policy.md`. It must:

- State that the repo follows `repo-standards`.
- Map standard guide names to local paths.
- List existing and missing guides.
- Note any repo-specific exceptions.

## Workflow order

The canonical stage order is:

```text
design -> planning -> implementing -> review
```

At each stage:

1. Read this standard.
2. Read the repo's `.agents/docs/repo-guide-policy.md`.
3. Invoke `/repo-worker-base` for worktree, branch, validation, and publication boundaries.
4. Read the repo-local guide for the stage.
5. Route to the matching Superpowers skill:
   - design -> `/brainstorming`
   - planning -> `/writing-plans`
   - implementation -> `/executing-plans` or `/subagent-driven-development`
   - review -> `/requesting-code-review`

## Relationship to repo-worker-base

`repo-standards` owns guide layout, invocation, and workflow order. `repo-worker-base` owns repo-worker hygiene, stage baselines, and publication boundaries. Use both together for every repo-backed stage.
