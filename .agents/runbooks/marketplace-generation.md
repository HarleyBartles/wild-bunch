# Marketplace generation guide

Use `/refreshing-installed-skills` after changing the plugin subscription,
pinned marketplace source, local plugin, or a registered repo-local skill.

The authored subscription is `.agents/plugins/marketplace.json`. Exact entries
under `repo.local_skills` are preserved as local custody; all other installed
skill directories are projections. Run `py -3 tools\run.py ci --apply`, stage
the intended projection and mesh changes, and use the normal hooked commit.

See [repository skills policy](../doctrine/repo-skills-policy.md) for the
custody invariants this workflow must preserve.
