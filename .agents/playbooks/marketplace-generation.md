# Marketplace generation playbook

## When

Changing plugin subscriptions, the pinned marketplace source, a local plugin,
or a registered repository-local skill.

## Required skills

- `/refreshing-installed-skills`
- `/repo-standards`

## Composition

1. Edit only the authored subscription, pinned marketplace source, local plugin,
   or registered local skill that owns the change.
2. Use `/refreshing-installed-skills` to rebuild installed projections from
   those sources; never patch projected skill content as the fix.
3. Use `/repo-standards` through `py -3 tools/run.py ci --apply` to reconcile
   repository shape, hook custody, provenance, and generated mesh.
4. Stage source and generated outputs together and verify their agreement at
   the normal hooked commit.

## Doctrine and contracts

[Repository skills policy](../doctrine/repo-skills-policy.md) owns source
custody. Exact `repo.local_skills` names identify local skills; every other
installed skill directory is a projection.

## Local commands and paths

- Authored subscription: `.agents/plugins/marketplace.json`
- Pinned source: `.agents/plugins/marketplace-source`
- Apply: `py -3 tools\run.py ci --apply`
- Check: `py -3 tools\run.py ci --check`

## Evidence contract

- [ ] Authored marketplace configuration and submodule gitlink are staged.
- [ ] Installed skill bytes and `.provenance.json` resolve from those sources.
- [ ] Every local skill is registered by exact name and survives refresh.
- [ ] Generated indexes and the committed projection agree.

## Prohibited combinations

- Do not hand-edit marketplace-projected skill files.
- Do not infer local custody from a name or prefix.

## Runbook routing

- [Implementing](../runbooks/implementing.md)
