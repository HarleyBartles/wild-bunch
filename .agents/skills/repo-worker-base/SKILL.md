---
name: repo-worker-base
description: Use when beginning or reviewing repo-backed work that needs portable worktree, source-custody, layout, validation, or publication guidance.
metadata:
  source-id: repo-worker-base
  source-path: codex-marketplace/plugins/repo-worker-pack/skills/repo-worker-base/SKILL.md
  provenance-name: Repo Worker Base first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Portable repo-worker routing, hygiene, and publication boundaries.
  use_when:
  - Use when repo work needs worktree, branch, scratch, source, layout, validation, evidence, review, closeout, or publication guidance.
  do_not_use_when:
  - Do not use when work is not repo-backed or a repository-specific policy alone owns the decision.
  use_with:
  - using-superpowers-plus
  - brainstorming
  - writing-plans
  - executing-plans
  - subagent-driven-development
  - requesting-code-review
license: MIT
---

# Repo Worker Base

This is the thin portable control plane for repo-backed work. It supplies
repeatable hygiene and publication boundaries; the consuming repository owns
its paths, commands, exclusions, CI, and exceptions through its local
hygiene/layout policy and stage runbooks. Superpowers lane composition is owned
by `using-superpowers-plus`; each stage skill owns its own baseline.

## Read when

| Need | Read |
| --- | --- |
| Repo work, worktree, branch, scratch, PR, or publication | [worktree-and-branch-policy.md](references/worktree-and-branch-policy.md) |
| Running or changing a mutation script | [mutation-script-safety.md](references/mutation-script-safety.md) |
| Creating an agent-facing script | [script-entrypoint-contract.md](references/script-entrypoint-contract.md) |
| Changing README, AGENTS.md, INDEX.md, doctrine, docs, plans, or mesh | [repository-layout-and-mesh.md](references/repository-layout-and-mesh.md) |
| Finding or creating a repository-local stage runbook | [stage-guide-contract.md](references/stage-guide-contract.md) |

Read the consuming repository's local hygiene/layout policy whenever it
exists. That local policy is the authority for repository-specific paths,
commands, exclusions, CI, and exceptions; this skill does not replace it.

For Superpowers lane composition and stage routing, see
[`using-superpowers-plus/references/bootstrap-routing.md`](/.agents/skills/using-superpowers-plus/references/bootstrap-routing.md)
and
[`using-superpowers-plus/references/superpowers-composition.md`](/.agents/skills/using-superpowers-plus/references/superpowers-composition.md).
Each stage skill owns its own baseline reference and reads it as part of its
own first step.

## Supporting owners

- using-superpowers-plus owns session bootstrap and request classification.
- linear-issue-shaping owns the Linear control plane.
- verification-before-completion owns evidence-before-assertions.
- connector-safety owns sensitive or blocked connector writes.
- using-github-mcp owns GitHub proof.
- base-doctrine owns cross-project source-truth and doctrine routing.

Do not treat an installed skill, generated bundle, local cache, or worker
report as authored source custody. Use the first-party source and the
repository's declared local policy, then regenerate or install through their
respective owners.
