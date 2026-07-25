---
name: generating-index-mesh
description: Use when the repo-wide INDEX.md mesh is stale or as a CI/pre-commit gate.
metadata:
  source-id: generating-index-mesh
  source-path: sources/first_party/skills/generating-index-mesh/SKILL.md
  provenance-name: Generating Index Mesh first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Run the repository's generate_index_mesh.py command.
  use_when:
  - Use when INDEX.md files are stale after skill, plugin, or source changes.
  - Use when verifying the navigation mesh in CI or as a pre-commit gate.
  do_not_use_when:
  - Do not use when installing or refreshing skills from the plugin source.
  related_skills:
  - refreshing-installed-skills
license: MIT
---

# Generating Index Mesh

Run the repository's index mesh generator.

## When to Use

- After changing skill, plugin, or source files that affect generated `INDEX.md` navigation.
- In CI or as a pre-commit hook to verify the mesh is current.

## Usage

```bash
py -3 .agents/skills/generating-index-mesh/scripts/generate_index_mesh.py
py -3 .agents/skills/generating-index-mesh/scripts/generate_index_mesh.py --check
```

This skill discovers the repo's `tools/generate_index_mesh.py` (source repo) or `scripts/generate_index_mesh.py` (consumer repo) and runs it. It does not commit; the caller decides whether to commit the regenerated mesh.
