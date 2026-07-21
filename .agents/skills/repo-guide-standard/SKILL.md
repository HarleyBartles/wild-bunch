---
name: repo-guide-standard
description: Use when reading, creating, updating, or aligning repo guides; when determining guide workflow order and handoffs.
metadata:
  source-id: repo-guide-standard
  source-path: sources/first_party/skills/repo-guide-standard/SKILL.md
  provenance-name: Repo Guide Standard first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Cross-repo guide layout, invocation, workflow order, and handoff requirements.
  use_when:
  - Use when reading, creating, updating, or aligning any repo-local guide.
  - Use when determining the workflow order for repo-backed design, planning, implementation, or review.
  - Use when a repo's guide set is missing or misaligned with the standard.
  do_not_use_when:
  - Do not use for generic repo hygiene such as worktree, branch, source custody, or publication boundaries — defer to repo-worker-base for those.
  use_with:
  - repo-worker-base
  - work-mode-router
  - brainstorming
  - writing-plans
  - executing-plans
  - subagent-driven-development
  - requesting-code-review
license: MIT
---

# Repo Guide Standard

This skill is the portable baseline for repo-local guides. It defines the cross-repo layout of root `AGENTS.md` headings, root pointer files, the `.agents/guides/` set, and the workflow order and Superpowers routing for each stage.

Each repo supplies a thin overlay at `.agents/docs/repo-guide-policy.md` that maps the standard to local files and records any exceptions. Local guides in `.agents/guides/` contain only repo-specific paths, commands, exclusions, CI, and exceptions.

## Read when

| Need | Read |
| --- | --- |
| How a repo's guides should be laid out | [references/repository-guide-standard.md](references/repository-guide-standard.md) |
| The repo's local guide mappings | `.agents/docs/repo-guide-policy.md` in the consuming repo |
| Repo hygiene (worktree, branch, validation, publication) | `/repo-worker-base` |

## Composition contract

For any guide work, use:

```text
repo-guide-standard -> repo-worker-base -> local guide -> selected Superpowers lane
```

`repo-guide-standard` supplies the universal guide standard and workflow order. `repo-worker-base` supplies worktree, branch, validation, and publication boundaries. The local guide supplies repo-specific details. The Superpowers lane supplies stage technique.

## Required root surfaces

- Root `AGENTS.md` contains canonical headings in order:
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
  11. `## Routing pointers`
  12. `## Maintenance responsibility`
- Root `REVIEW.md` is the review entry point. It contains first-class review concerns and routes to `.agents/guides/code-review-guide.md` for detailed review methodology and to `/requesting-code-review` for execution.
- Root `CONTRIBUTING.md` is the contributor entry point. It routes to the design, planning, implementation, and review guides and to the relevant repo-worker-pack and Superpowers skills. It may be a thin pointer to `.agents/guides/contributing-guide.md` when a repo keeps detailed guidance there.

## Core guide set

`.agents/guides/` must contain these stage guides:

- `design-guide.md`
- `planning-guide.md`
- `implementing-guide.md`
- `code-review-guide.md`

## Allowed additional guides

A repo may declare additional `<topic>-guide.md` files in `.agents/guides/`. Each must be a repo-specific overlay, not a repeat of portable doctrine. Examples:

- `security-guide.md`
- `testing-guide.md`
- `contributing-guide.md`
- `pr-guide.md`
- `code-style-guide.md`
- `marketplace-generation-guide.md`
- `skill-authoring-guide.md`

## Workflow order

The canonical repo-backed workflow is:

```text
design -> planning -> implementing -> review
```

For each stage:

1. Invoke `/repo-guide-standard` and read `references/repository-guide-standard.md`.
2. Invoke `/repo-worker-base` for worktree, branch, validation, and publication boundaries.
3. Read the repo's `.agents/docs/repo-guide-policy.md` to find the local guide path.
4. Read the repo-local guide for that stage.
5. Route to the correct Superpowers skill:
   - design -> `/brainstorming`
   - planning -> `/writing-plans`
   - implementation -> `/executing-plans` or `/subagent-driven-development`
   - review -> `/requesting-code-review`
