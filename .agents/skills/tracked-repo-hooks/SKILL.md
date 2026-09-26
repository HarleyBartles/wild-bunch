---
name: tracked-repo-hooks
description: Use when installing, changing, or validating tracked pre-commit hooks, staged-snapshot execution, linked-worktree activation, or hosted-CI parity.
metadata:
  source-id: tracked-repo-hooks
  source-path: codex-marketplace/plugins/agent-operating-model/skills/tracked-repo-hooks/SKILL.md
  provenance-name: Tracked Repo Hooks first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
license: MIT
---

# Tracked Repo Hooks

The canonical hook is tracked at `githooks/pre-commit` and activated through `core.hooksPath=githooks`. It materializes the staged tree, invokes the consumer-declared apply and check vectors, stages only owned generated surfaces declared in `.agents/contracts/repo-standards-commands.json`, restores unrelated working state, and rejects unresolved contract failures. The tracked hook is a behavioral seed: consumer-owned edits are valid when they preserve the staged-snapshot, apply-before-check, restoration, and hosted-parity contract.

Hosted CI invokes the same hook against the checked-out commit with `REPO_STANDARDS_HOSTED_COMMIT=HEAD`. The portable hook never hard-codes Python, Ruff, or a repository-specific command bus.

`REPO_STANDARDS_STAGED_SNAPSHOT=1` identifies the candidate tree under validation. It does not alter consumer command semantics, marketplace-source rolling, refresh behavior, or any other workflow policy unless the consumer's owning contract explicitly declares otherwise. Tree-selection mechanics do not acquire policy meaning merely because they are visible to a command.

The compatibility hook template remains under `repo-shape/templates/` while the coordinator installs it; this skill owns its contract.
