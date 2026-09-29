# Marketplace generation playbook

## When

Changing native plugin subscriptions, deployed tooling resources, or a registered repository-local skill.

## Required capabilities

- Validate native subscriptions and inspect authored local-skill custody.
- Apply and check the consumer-selected repository standards.

## Optional capabilities

- None.

## Required repository-owned skills

- None.

## Optional repository-owned skills

- None.

## Composition

1. Edit `.agents/plugins/marketplace.json` and `.codex/config.toml` for plugin dependencies; keep exact local-skill registrations.
2. Install through `codex plugin add <plugin-name>@wild-bunch` and refresh through `codex plugin marketplace upgrade wild-bunch` in the trusted repository. Keep plugin payloads in Codex's cache.
3. Apply and check consumer-selected standards through `py -3 tools/run.py ci --apply` and `--check`. A tooling-submodule update is a separate deliberate pin change with AOM deployment provenance.
4. Stage the authored configuration and deployed resources together and verify at the normal hooked commit.

## Doctrine and contracts

[Repository skills policy](../doctrine/repo-skills-policy.md) owns source
custody. Exact `repo.local_skills` names identify local skills; every other
skill directory in `.agents/skills/` is an unregistered custody error.

## Local commands and paths

- Authored subscription: `.agents/plugins/marketplace.json`
- Native activation: `.codex/config.toml`
- Pinned tooling resources: `.agents/plugins/marketplace-source`
- Plugin refresh: `codex plugin marketplace upgrade wild-bunch`
- Apply: `py -3 tools\run.py ci --apply`
- Check: `py -3 tools\run.py ci --check`

## Evidence contract

- [ ] Native activation keys resolve to the declared marketplace and selected plugins.
- [ ] Plugin skills are discoverable and invocable in the repo and its worktrees, with unrelated-repo isolation.
- [ ] Every local skill is registered by exact name and survives native refresh.
- [ ] Selected operating standards pass using deployed resources.

## Prohibited combinations

- Do not copy plugin payloads or project their skills into `.agents/skills/`.
- Do not infer local custody from a name or prefix.

## Runbook routing

- [Implementing](../runbooks/implementing.md)
