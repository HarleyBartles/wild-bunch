---
name: repo-worker-base
description: Use when beginning or reviewing repo-backed work that needs portable worktree, source-custody, layout, validation, publication, or stage-composition guidance.
metadata:
  source-id: repo-worker-base
  source-path: sources/first_party/skills/repo-worker-base/SKILL.md
  provenance-name: Repo Worker Base first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Portable repo-worker routing, hygiene, stage composition, and publication boundaries.
  use_when:
  - Use when repo work needs worktree, branch, scratch, source, layout, validation, evidence, review, closeout, or publication guidance.
  - Use when a repo-backed Superpowers lane needs its matching baseline and repository-local guide.
  do_not_use_when:
  - Do not use when work is not repo-backed or a repository-specific policy alone owns the decision.
  use_with:
  - work-mode-router
  - brainstorming
  - writing-plans
  - executing-plans
  - subagent-driven-development
  - requesting-code-review
license: MIT
---

# Repo Worker Base

This is the thin portable control plane for repo-backed work. It supplies
repeatable hygiene and composition boundaries; the consuming repository owns
its paths, commands, exclusions, CI, and exceptions through its local
hygiene/layout policy and stage guides.

## Read when

| Need | Read |
| --- | --- |
| Repo work, worktree, branch, scratch, PR, or publication | [worktree-and-branch-policy.md](references/worktree-and-branch-policy.md) |
| Running or changing a mutation script | [mutation-script-safety.md](references/mutation-script-safety.md) |
| Creating an agent-facing script | [script-entrypoint-contract.md](references/script-entrypoint-contract.md) |
| Changing README, AGENTS.md, INDEX.md, doctrine, docs, plans, or mesh | [repository-layout-and-mesh.md](references/repository-layout-and-mesh.md) |
| Finding or creating a repository-local stage guide | [stage-guide-contract.md](references/stage-guide-contract.md) |
| Repo-backed design | [design-baseline.md](references/design-baseline.md) |
| Repo-backed planning | [planning-baseline.md](references/planning-baseline.md) |
| Repo-backed implementation, evidence, closeout, or publication | [implementation-baseline.md](references/implementation-baseline.md) |
| Repo-backed review | [code-review-baseline.md](references/code-review-baseline.md) |
| Selecting a Superpowers lane for a repo-backed stage | [superpowers-composition.md](references/superpowers-composition.md) |

Read the consuming repository's local hygiene/layout policy whenever it
exists. That local policy is the authority for repository-specific paths,
commands, exclusions, CI, and exceptions; this skill does not replace it.

## Composition contract

For repo-backed design, planning, implementation, or review, use:

~~~text
repo-worker-base -> matching baseline -> local guide -> selected Superpowers lane
~~~

The matching lanes are brainstorming, writing-plans, either executing-plans or
subagent-driven-development, and requesting-code-review. Keep this entrypoint
thin: it routes to those owners instead of duplicating their stage technique.

## Supporting owners

- work-mode-router owns route classification.
- linear-issue-shaping owns the Linear control plane.
- verification-before-completion owns evidence-before-assertions.
- connector-safety owns sensitive or blocked connector writes.
- github-operations owns GitHub proof.
- base-doctrine owns cross-project source-truth and doctrine routing.

Do not treat an installed skill, generated projection, local cache, or worker
report as authored source custody. Use the first-party source and the
repository's declared local policy, then regenerate or install through their
respective owners.
