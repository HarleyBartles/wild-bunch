# Repository skills and plugins policy

## Source custody

- `.agents/plugins/marketplace.json` declares the four repository plugin dependencies and their Git sources and paths. Their payload refs intentionally track `main`.
- `.codex/config.toml` binds the Git-backed `wild-bunch` catalog and enables the selected plugin identities. Codex owns installation and cache refresh.
- `.agents/skills/` contains skills authored for Wild Bunch. Each skill's frontmatter name matches its containing directory. Installed plugin skills remain in Codex's cache.
- `py -3 tools/check_plugin_subscriptions.py --check` checks Codex catalog syntax, selectors, paths, matching marketplace registration, and local activations. It does not fetch, install, or establish authentication, access, trust, or runtime availability.
- Devin can read the repository's scoped rules, but this repository does not declare plugin dependencies for Devin. Do not describe Codex plugin activation as Devin support.

## Authoring and validation

Keep reusable game behavior in repository-owned skills when a skill is useful across tasks. Test any executable skill scripts through the same repository-owned behavior-test lane as other Python scripts. Do not require an external validator or a parallel registration inventory for skills without executable scripts.

When changing plugin declarations or authored-skill custody, update this policy and the `repo-plugin-subscriptions` certification in `.agents/contracts/standards-certification.md` in the same change.
