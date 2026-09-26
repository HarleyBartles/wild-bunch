---
name: repo-standards
description: Use when aligning several repository operating-model concerns or deciding which focused repository standard owns a requested change.
metadata:
  source-id: repo-standards
  source-path: codex-marketplace/plugins/agent-operating-model/skills/repo-standards/SKILL.md
  provenance-name: Repo Standards router first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
license: MIT
---

# Repo Standards

Route the request to the smallest owning capability:

| Concern                                                       | Skill                   |
| ------------------------------------------------------------- | ----------------------- |
| Required repository surfaces and structural checks            | `repo-shape`            |
| Runbooks, playbooks, and their composition graph              | `repo-composition`      |
| Named command targets and dispatch semantics                  | `command-bus`           |
| Focused checks, complete gates, and evidence                  | `repository-validation` |
| Tracked pre-commit and hosted-CI parity                       | `tracked-repo-hooks`    |
| Markdown formatting adoption and enforcement                  | `markdown-formatting`   |
| Plugin subscriptions, local skills, and installed projections | `repo-agent-assets`     |
| Python implementation patterns                                | `python`                |

Read the consumer repository's local policy after selecting the owner. Compose multiple skills only for a migration that genuinely crosses their boundaries. Generic worker hygiene, worktrees, risk, publication, and closeout remain owned by `repo-worker-base` and are not part of this router.
