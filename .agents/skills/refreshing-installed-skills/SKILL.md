---
name: refreshing-installed-skills
description: Use when a worktree is initialized or .agents/skills/ is stale from the plugin source.
metadata:
  source-id: refreshing-installed-skills
  source-path: sources/first_party/skills/refreshing-installed-skills/SKILL.md
  provenance-name: Refreshing Installed Skills first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Install or refresh .agents/skills/ from the plugin source and regenerate the agent mesh.
  use_when:
  - Use when creating a new worktree.
  - Use after updating the marketplace-source submodule.
  - Use when .agents/skills/ appears stale.
  do_not_use_when:
  - Do not use when only the INDEX.md mesh is stale without any skill changes; use generating-agent-mesh instead.
  related_skills:
  - generating-agent-mesh
  - using-git-worktrees
license: MIT
---

# Refreshing Installed Skills

Install or refresh `.agents/skills/` from the plugin source, then regenerate the agent mesh.

## When to Use

- After creating a new worktree.
- After updating the `marketplace-source` submodule in a consumer repo.
- When `.agents/skills/` appears stale.

## Usage

```bash
py -3 .agents/skills/refreshing-installed-skills/scripts/refresh_installed_skills.py
py -3 .agents/skills/refreshing-installed-skills/scripts/refresh_installed_skills.py --check
```

This skill runs the bundled `refresh_installed_skills.py` core, which installs/refreshes `.agents/skills/` from the plugins declared in `.agents/plugins/marketplace.json`, rolls the optional `marketplace-source` submodule to `origin/main`, and regenerates the agent mesh. If changes were made, it commits them with the message `chore: refresh installed skills and regenerate agent mesh`.

## Plugin source types

Each plugin entry in `.agents/plugins/marketplace.json` declares its source with `source.source`:

- `local`: `source.path` is relative to the consumer repo root.
- `github`: `source.path` is relative to the `marketplace-source` submodule root (`.agents/plugins/marketplace-source`).

Both `local` and `github` sources are normalized and must resolve to a directory under their respective roots.

## Local skill validation extension

After the bundled core validates that local skill directories match their frontmatter names and do not collide with reserved marketplace prefixes, it calls a repo-supplied extension script if one exists:

- `scripts/validate_local_skills_extra.sh` — bash script; receives `--check` followed by the skills root and any local skill prefixes.
- `scripts/validate_local_skills_extra.ps1` — PowerShell script; must declare `param([switch]$Check, [Parameter(ValueFromRemainingArguments=$true)][string[]]$Remaining)`.

Example invocation:

```text
scripts/validate_local_skills_extra.sh .agents/skills wild-bunch-
```

In `--check` mode the script must report errors and exit non-zero if invalid. In write mode it may also auto-fix or just validate. The skill fails with a clear error if the hook exits non-zero.

## Provenance

`.agents/skills/.provenance.json` records the marketplace-source version that was installed. When the `marketplace-source` submodule is present, `manifestSha` tracks the submodule HEAD; otherwise it falls back to the consumer repo HEAD. `syncedPlugins` lists every plugin configured as `INSTALLED_BY_DEFAULT`, in order, regardless of whether its skills needed copying on this run.
