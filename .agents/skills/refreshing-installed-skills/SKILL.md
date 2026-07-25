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
