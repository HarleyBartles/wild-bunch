---
name: repo-standards
description: Use when reading, creating, updating, or aligning repo standards; when determining repo shape, runbook layout, workflow order, and handoff requirements. Do not use when the task is generic repo hygiene such as worktree, branch, source custody, or publication boundaries.
metadata:
  source-id: repo-standards
  source-path: codex-marketplace/plugins/repo-worker-pack/skills/repo-standards/SKILL.md
  provenance-name: Repo Standards first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Cross-repo runbook layout, invocation, workflow order, and handoff requirements.
  use_when:
  - Use when reading, creating, updating, or aligning any repo-local runbook.
  - Use when determining the workflow order for repo-backed design, planning, implementation, or review.
  - Use when a repo's runbook set is missing or misaligned with the standard.
  do_not_use_when:
  - Do not use for generic repo hygiene such as worktree, branch, source custody, or publication boundaries — defer to repo-worker-base for those.
  use_with:
  - repo-worker-base
  - inspecting-the-environment
  - brainstorming
  - writing-plans
  - executing-plans
  - subagent-driven-development
  - requesting-code-review
license: MIT
---

# Repo Standards

This skill is the portable baseline for repo-local runbooks and agent-facing routing surfaces. It defines the cross-repo layout of root `AGENTS.md`, pointer files, the `.agents/runbooks/` set, and the workflow order for each stage.

Each repo supplies a thin overlay at `.agents/doctrine/repo-runbook-policy.md` that maps the standard to local files and records any exceptions. Local runbooks in `.agents/runbooks/` contain repo-specific paths, commands, exclusions, CI, and exceptions.

## Read when

| Need | Read |
| --- | --- |
| How a repo's runbooks should be laid out | [references/repository-runbook-standard.md](references/repository-runbook-standard.md) |
| How a repo's shape should be checked/applied | [references/repository-shape-standard.md](references/repository-shape-standard.md) and [references/repository-shape-manifest.json](references/repository-shape-manifest.json) |
| How preflight, pre-commit, and CI relate | [references/ci-validation-pipeline.md](references/ci-validation-pipeline.md) |
| The repo's local runbook mappings | `.agents/doctrine/repo-runbook-policy.md` in the consuming repo |
| Repo hygiene (worktree, branch, validation, publication) | `/repo-worker-base` |
| Scratch workspace layout and cleanup | [references/scratch-workspace-policy.md](references/scratch-workspace-policy.md) |
| Skill-bundled script CLI contract failures | [references/skill-script-contract-validator.md](references/skill-script-contract-validator.md) |
| Vendor subagent profile deployment | [references/vendor-profile-deployment.md](references/vendor-profile-deployment.md) |

## Composition contract

For any runbook work, use:

```text
repo-standards -> repo-worker-base -> local runbook -> selected Superpowers lane
```

`repo-standards` supplies the universal runbook standard and workflow order. `repo-worker-base` supplies worktree, branch, validation, and publication boundaries. The local runbook supplies repo-specific details. The Superpowers lane supplies stage technique.

## Workflow order

The canonical repo-backed workflow is:

```text
design -> planning -> implementing -> review
```

`repo-standards` is a check-and-align tool for repo shape and runbook layout, not a first-turn router. Do not invoke it before `/using-superpowers-plus`.

After the owning Superpowers stage skill has routed you (e.g., `/writing-plans` for planning), invoke `/repo-standards` when:
- the stage skill explicitly tells you to verify or apply repo shape,
- the repo's `AGENTS.md` or local runbook points you to `repo-standards`,
- the task involves scaffolds, runbook layout, or the `repository-shape-manifest.json`.

The typical `repo-standards` workflow is:
1. Read `references/repository-runbook-standard.md` and `references/repository-shape-standard.md`.
2. Invoke `/repo-worker-base` if the work touches worktree, branch, validation, or publication.
3. Read the repo's `.agents/doctrine/repo-runbook-policy.md`.
4. Apply or check the surfaces the stage skill needs.

## Script usage notes

- Every Python script and wrapper accepts `--help`. Run it before reading the implementation.
- `--check` is always a safe, read-only drift report.
- Use `--force` to overwrite an existing scaffolded surface. Without `--force`, the scaffolds create missing files and leave existing ones alone.
- `repo-standards` supports `--apply --yes` to create missing surfaces and `--apply --yes --force` to overwrite drifted surfaces.

For the full list of required surfaces, runbook set, scaffold helpers, and exceptions, see [references/repository-shape-standard.md](references/repository-shape-standard.md) and [references/repository-runbook-standard.md](references/repository-runbook-standard.md).
