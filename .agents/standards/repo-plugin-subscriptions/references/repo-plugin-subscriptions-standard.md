# Repo Plugin Subscriptions Standard

This optional AOM standard lets a repository declare plugins that its agent harness loads in that repository and its worktrees. The plugin source remains a Git dependency; the consumer does not vendor its payload or copy plugin skills into `.agents/skills/`.

## Opt in

Add `repo-plugin-subscriptions` to `.agents/contracts/operating-standards.json`. The standard scaffolds missing native config files and validates them. The consumer owns and may edit the resulting files. Applying the scaffold never overwrites an existing config.

## Codex

Declare each plugin in `.agents/plugins/marketplace.json` using `source.source: "git-subdir"`, the Git repository `url`, the plugin-relative `path`, and exactly one selector: a floating `ref` such as `main`, or a fixed `sha`. Codex reads repo activation from `.codex/config.toml`; its `[plugins]` entries use the `plugin-name@marketplace-name` key and `enabled = true`. Every activation key must resolve to a declared plugin and marketplace.

Codex's native `codex plugin marketplace upgrade <marketplace-name>` refreshes the managed marketplace and installed plugin payload. For a consumer tracking `main`, that refresh picks up new commits without changing the consumer declaration. A fixed `sha` remains pinned until the consumer changes it.

## Devin

`.devin/config.json` is scaffolded as a native repo configuration starting point. Repositories may add Devin-native `requiredPlugins`, `optionalPlugins`, or `forbiddenPlugins` declarations when they choose to use Devin. This standard does not install plugins at user scope and does not invoke Devin's global plugin update command.

## Validation and ownership

`repo-standards --check --standard repo-plugin-subscriptions` validates local config syntax and references without network access. Plugin paths must be relative, remain within the declared plugin directory, and contain no parent traversal. A plugin name may be declared only once. Existing `.agents/skills/` and the `repo.local_skills` list remain consumer-owned; the standard neither projects plugin skills nor removes local skills.
