# Repo Skills Policy

Use this reference when syncing marketplace skills or working with the skill vendoring system.

## Repo Skills

The complete repo skills policy is defined in [`.agents/docs/mesh-policy.md`](.agents/docs/mesh-policy.md) sections 6-7. Key points:

- Marketplace plugin skills are vendored into `.agents/skills/` so any agent working in this repo can invoke and discover them. Devin CLI discovers repo skills only from local `.agents/skills/<name>/SKILL.md` directories; it has no Codex-marketplace plugin install path.
- Source: `HarleyBartles/agent-asset-marketplace` pinned as a git submodule at `.agents/plugins/marketplace-source/`. Provenance (submodule HEAD SHA) is recorded in `.agents/skills/.provenance.json`.
- Do not hand-edit vendored skill files. Edit upstream in `agent-asset-marketplace` and re-sync.
- To refresh vendored skills after upstream plugin updates, run exactly these three commands in order:
  ```powershell
  git submodule update --remote .agents/plugins/marketplace-source
  .\scripts\sync-skills.ps1
  python scripts\generate_index_mesh.py
  ```
  `scripts\sync-skills.ps1` is idempotent: it no-ops when the submodule HEAD matches `.agents/skills/.provenance.json` (pass `-Force` to re-copy regardless). Do not look for or invent other skill-sync paths.
