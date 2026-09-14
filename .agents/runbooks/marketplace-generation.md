# Marketplace generation runbook

## When

Changing plugin subscriptions, the pinned marketplace source, a local plugin,
or a registered repository-local skill.

## Required skills

- `/refreshing-installed-skills`
- `/repo-standards`

## Composition

Refresh the installed projection from authored configuration, then apply and
check repository shape and mesh requirements.

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

The projection provenance, submodule gitlink, registered local skills, and
generated mesh agree at the committed head.

## Prohibited combinations

- Do not hand-edit marketplace-projected skill files.
- Do not infer local custody from a name or prefix.
