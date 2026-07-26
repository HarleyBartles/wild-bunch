# Repo Skills Policy

Use this reference when syncing marketplace skills or working with the Wild
Bunch repo-local skill boundary.

## Repo Skills

The complete repo skills policy is defined in [`mesh-policy.md`](mesh-policy.md)
sections 6-7.

## Repo-local Wild Bunch skills

Use [`skill-authoring-policy.md`](skill-authoring-policy.md) before creating,
renaming, reviewing, migrating, or retiring any `wild-bunch-*` skill under
`.agents/skills/`.

- `wild-bunch-*` is the permanent repo-local namespace.
- Discover repo-local Wild Bunch skills from the matching directories under
  `.agents/skills/`; do not maintain a second inventory.
- Local Wild Bunch skills must stay outside marketplace provenance and survive
  refreshes from `.agents/plugins/marketplace-source/`.

## Marketplace skill sync

For vendored marketplace projections, follow the refresh boundary below:

- Marketplace plugin skills are vendored into `.agents/skills/` so any agent working in this repo can invoke and discover them. Devin CLI discovers repo skills only from local `.agents/skills/<name>/SKILL.md` directories; it has no Codex-marketplace plugin install path.
- Source: `HarleyBartles/agent-asset-marketplace` pinned as a git submodule at `.agents/plugins/marketplace-source/`. Provenance (submodule HEAD SHA) is recorded in `.agents/skills/.provenance.json`.
- A plugin may also be vendored locally under `.agents/plugins/<name>/` and declared in `.agents/plugins/marketplace.json` with `"source": "local"`. Local plugin skills are copied into `.agents/skills/<name>/` using their original names and are not marketplace-derived.
- Do not hand-edit vendored skill files. Edit upstream in `agent-asset-marketplace` and re-sync. Edit local plugin skills in `.agents/plugins/<name>/skills/` and re-run the refresh skill.
- To refresh vendored skills after upstream plugin updates or local plugin changes, run:
  ```powershell
  git submodule update --remote .agents/plugins/marketplace-source
  py -3 .agents/skills/refreshing-installed-skills/scripts/refresh-installed-skills.py
  # or: .agents/skills/refreshing-installed-skills/scripts/refresh-installed-skills.ps1
  ```
  The bundled `refreshing-installed-skills` skill is idempotent: it no-ops only when the source, ordered default-plugin names, generated skill metadata, and byte-for-byte vendored skill contents match `.agents/skills/.provenance.json` (pass `--force` to re-copy regardless). Do not look for or invent other skill-sync paths.
- Regenerate the `INDEX.md` mesh afterwards by running:
  ```powershell
  py -3 .agents/skills/generating-agent-mesh/scripts/generate-index-mesh.py
  # or: .agents/skills/generating-agent-mesh/scripts/generate-index-mesh.ps1
  ```
