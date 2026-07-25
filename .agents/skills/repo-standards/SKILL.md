---
name: repo-standards
description: Use when reading, creating, updating, or aligning repo standards; when determining repo shape, guide layout, workflow order, and handoff requirements. Do not use when the task is generic repo hygiene such as worktree, branch, source custody, or publication boundaries.
metadata:
  source-id: repo-standards
  source-path: sources/first_party/skills/repo-standards/SKILL.md
  provenance-name: Repo Standards first-party skill
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
  - inspecting-the-environment
  - work-mode-router
  - brainstorming
  - writing-plans
  - executing-plans
  - subagent-driven-development
  - requesting-code-review
license: MIT
---

# Repo Standards

This skill is the portable baseline for repo-local guides and agent-facing routing surfaces. It defines the cross-repo layout of root `AGENTS.md`, pointer files, the `.agents/guides/` set, and the workflow order for each stage.

Each repo supplies a thin overlay at `.agents/docs/repo-guide-policy.md` that maps the standard to local files and records any exceptions. Local guides in `.agents/guides/` contain repo-specific paths, commands, exclusions, CI, and exceptions.

## Read when

| Need | Read |
| --- | --- |
| How a repo's guides should be laid out | [references/repository-guide-standard.md](references/repository-guide-standard.md) |
| How a repo's shape should be checked/applied | [references/repository-shape-standard.md](references/repository-shape-standard.md) and [references/repository-shape-manifest.json](references/repository-shape-manifest.json) |
| The repo's local guide mappings | `.agents/docs/repo-guide-policy.md` in the consuming repo |
| Repo hygiene (worktree, branch, validation, publication) | `/repo-worker-base` |

## Composition contract

For any guide work, use:

```text
repo-standards -> repo-worker-base -> local guide -> selected Superpowers lane
```

`repo-standards` supplies the universal guide standard and workflow order. `repo-worker-base` supplies worktree, branch, validation, and publication boundaries. The local guide supplies repo-specific details. The Superpowers lane supplies stage technique.

## Workflow order

The canonical repo-backed workflow is:

```text
design -> planning -> implementing -> review
```

For each stage, invoke `/repo-standards`, read `references/repository-guide-standard.md`, invoke `/repo-worker-base`, read the repo's `.agents/docs/repo-guide-policy.md`, read the repo-local stage guide, and route to the matching Superpowers skill (`/brainstorming`, `/writing-plans`, `/executing-plans` or `/subagent-driven-development`, `/requesting-code-review`).

## Script usage notes

- Every Python script and wrapper accepts `--help`. Run it before reading the implementation.
- `--check` is always a safe, read-only drift report.
- Use `--force` to overwrite an existing scaffolded surface. Without `--force`, the scaffolds create missing files and leave existing ones alone.
- `repo-standards` supports `--apply --yes` to create missing surfaces and `--apply --yes --force` to overwrite drifted surfaces.

For the full list of required surfaces, guide set, scaffold helpers, and exceptions, see [references/repository-shape-standard.md](references/repository-shape-standard.md) and [references/repository-guide-standard.md](references/repository-guide-standard.md).
