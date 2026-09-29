# Repository skills policy

## Source custody

- `.agents/plugins/marketplace.json` owns the four Git-subdirectory subscriptions. Their payload refs track `main`.
- `.codex/config.toml` registers the Git-backed `wild-bunch` marketplace from this repository's published `main` and enables only its selected plugin identities. Codex owns installation and cache refresh.
- `.agents/skills/` contains only the exact authored names in `repo.local_skills`. Plugin skills remain in Codex's cache and have no repository projection or `.provenance.json`.
- `.agents/plugins/marketplace-source` remains pinned for the Markdown formatter wheel and the skill-script validator. Selected operating-standard implementations are deployed into `.agents/standards/` with their own provenance, so checks run without an ambient plugin. The submodule is not an installed plugin source and is not rolled by a refresh workflow.
- Update a tooling pin deliberately, deploy standards through AOM's `deploy_operating_standards.py`, and prove the repository gates. Native Marketplace Upgrade changes cached plugins, not this tooling pin or deployed standards.

## Registration invariant

Every repository-local skill is named exactly once in `repo.local_skills`, its directory and frontmatter name match that entry, and its authored bytes survive plugin installation and refresh. Names and prefixes alone do not establish custody.

## Native refresh

Run `codex plugin marketplace upgrade wild-bunch` in a trusted Wild Bunch checkout. This upgrades the Git catalog snapshot and its installed Git-subdirectory payloads. Other repositories do not inherit the activation keys. Native installation may also write user-level enablement; keep these four identities disabled or absent at user scope so repository activation remains the owner. Before this migration merges, field tests override the catalog's ref to the published PR branch; payload refs remain `main`. After merge the checked-in catalog ref is sufficient.

Devin activation and field testing remain deferred. The adopted AOM scaffold's empty `.devin/config.json` declares no dependencies.
