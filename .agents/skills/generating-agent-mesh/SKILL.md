---
name: generating-agent-mesh
description: Use when the repo-wide INDEX.md mesh or agent-mesh validation is stale, or as a CI/pre-commit gate.
metadata:
  source-id: generating-agent-mesh
  source-path: codex-marketplace/plugins/repo-worker-pack/skills/generating-agent-mesh/SKILL.md
  provenance-name: Generating Agent Mesh first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Run the repository's generate_index_mesh.py and validate_agent_mesh.py commands.
  use_when:
  - Use when INDEX.md files are stale after skill, plugin, or source changes.
  - Use when verifying the navigation mesh, local markdown links, and doctrine routing in CI or as a pre-commit gate.
  do_not_use_when:
  - Do not use when installing or refreshing skills from the plugin source.
  related_skills:
  - repo-standards
  - refreshing-installed-skills
license: MIT
---

# Generating Agent Mesh

Run the repository's index mesh generator and agent mesh validator.

## When to Use

- After changing skill, plugin, or source files that affect generated `INDEX.md` navigation.
- In CI or as a pre-commit hook to verify the mesh and local markdown links are current.

## Usage

```bash
# Generate or check the INDEX.md mesh
py -3 .agents/skills/generating-agent-mesh/scripts/generate_index_mesh.py --apply
py -3 .agents/skills/generating-agent-mesh/scripts/generate_index_mesh.py --check

# Validate the agent mesh (local links + doctrine routing)
py -3 .agents/skills/generating-agent-mesh/scripts/validate_agent_mesh.py --check
py -3 .agents/skills/generating-agent-mesh/scripts/validate_agent_mesh.py --check --changed-from HEAD
```

The wrapper commands `generate-index-mesh` and `validate-agent-mesh` in the same directory call these bundled Python cores and are the form used by `repo-standards` preflight.

`generate-index-mesh` writes the repo-wide `INDEX.md` mesh from `git ls-files` when passed `--apply`; without `--apply` it defaults to `--check`. `validate-agent-mesh` checks local markdown links and doctrine routing. It does not commit; the caller decides whether to commit regenerated or validated state. When running from a linked worktree, add `--apply --allow-shared-checkout`.

## Repo-specific generation extensions

`generate-index-mesh` runs an optional extra hook if one exists, after writing all `INDEX.md` files and before link validation:
- `scripts/generate_index_mesh_extra.sh` -- bash script; receives `--check` followed by `<repo-root>`.
- `scripts/generate_index_mesh_extra.ps1` -- PowerShell script; must declare `param([switch]$Check, [string]$RepoRoot)`.

In write mode the script can post-process or append content to specific `INDEX.md` files (e.g., an ADR freshness table in `docs/adr/INDEX.md`). In `--check` mode it must verify its generated content is current and exit non-zero if not. The skill fails with a clear error if the hook exits non-zero.

## Repo-specific validation extensions

`validate-agent-mesh` runs an optional extra hook if one exists:
- `scripts/validate_agent_mesh_extra.sh` -- bash script; receives `--check` and optional `--changed-from <ref>`.
- `scripts/validate_agent_mesh_extra.ps1` -- PowerShell script; must declare `param([switch]$Check, [string]$ChangedFrom)`.

The hook should print findings as `DRIFT: <message>` and exit non-zero on failure. Any stdout/stderr not prefixed with `DRIFT:` is reported as an extra-hook error.
