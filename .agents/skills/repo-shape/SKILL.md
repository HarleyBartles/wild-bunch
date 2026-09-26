---
name: repo-shape
description: Use when checking, creating, or repairing the required agent-facing files, directories, manifests, pointers, and structural contracts of a repository.
metadata:
  source-id: repo-shape
  source-path: codex-marketplace/plugins/agent-operating-model/skills/repo-shape/SKILL.md
  provenance-name: Repo Shape first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Portable repository surface shape, scaffolding, and structural validation.
  use_when:
    - checking or creating required repository files and directories.
    - aligning structural manifests, routing pointers, or scaffolded surfaces.
    - the repository shape coordinator reports missing or drifted surfaces.
  do_not_use_when:
    - generic repo hygiene such as worktree, branch, source custody, or publication boundaries — defer to repo-worker-base for those.
license: MIT
---

# Repo Shape

This skill owns the portable repository surface model and the check/apply coordinator. Use `repo-standards` to route broad operating-model work and `repo-composition` for runbook or playbook semantics.

Each repo supplies a thin overlay at `.agents/doctrine/repo-runbook-policy.md` that records local mappings and shape exceptions. `repo-composition` owns the meaning of runbooks, playbooks, and their graph.

## Read when

| Need                                                     | Read                                                                                                                                                                          |
| -------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| How a repo's runbooks should be laid out                 | [references/repository-runbook-standard.md](references/repository-runbook-standard.md)                                                                                        |
| How a repo's shape should be checked/applied             | [references/repository-shape-standard.md](references/repository-shape-standard.md) and [references/repository-shape-manifest.json](references/repository-shape-manifest.json) |
| How preflight, pre-commit, and CI relate                 | [references/ci-validation-pipeline.md](references/ci-validation-pipeline.md)                                                                                                  |
| The repo's local runbook mappings                        | `.agents/doctrine/repo-runbook-policy.md` in the consuming repo                                                                                                               |
| Repo hygiene (worktree, branch, validation, publication) | `repo-worker-base`                                                                                                                                                            |
| Scratch workspace layout and cleanup                     | [references/scratch-workspace-policy.md](references/scratch-workspace-policy.md)                                                                                              |
| Skill-bundled script CLI contract failures               | [references/skill-script-contract-validator.md](references/skill-script-contract-validator.md)                                                                                |
| Vendor subagent profile deployment                       | [references/vendor-profile-deployment.md](references/vendor-profile-deployment.md)                                                                                            |

## Workflow order

`repo-shape` is a check-and-align tool, not a first-turn workflow router. Do not invoke it before `using-superpowers-plus`.

After the owning workflow has routed you, invoke `repo-shape` when:

- the stage skill explicitly tells you to verify or apply repo shape,
- the repo's `AGENTS.md` or local runbook points you to `repo-standards`,
- the task involves scaffolds, runbook layout, or the `repository-shape-manifest.json`.

After routing, the `repo-shape` workflow is:

1. Read `references/repository-runbook-standard.md` and `references/repository-shape-standard.md`.
2. Read the repo's `.agents/doctrine/repo-runbook-policy.md`.
3. Apply or check the surfaces the routed owner needs.

## Script usage notes

- Every Python script and wrapper accepts `--help`. Run it before reading the implementation.
- `--check` is always a safe, read-only drift report.
- Normal apply creates missing starter surfaces and preserves repository-owned customizations.
- `--force <surface-id>` is a targeted template deployment, never a generic repair switch. It requires `--confirm-local-customisations-will-be-overwritten`; bare `--force` and `--apply --force` are invalid.
- `repo-standards --check` is the read-only migration audit. It reports contract/schema issues, dead links, unsafe customizations, and available force-reset surfaces without mutating.

For the full list of required surfaces, runbook and playbook sets, scaffold helpers, and exceptions, see [references/repository-shape-standard.md](references/repository-shape-standard.md) and [references/repository-runbook-standard.md](references/repository-runbook-standard.md).
