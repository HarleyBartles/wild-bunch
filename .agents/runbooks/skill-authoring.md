# Skill authoring runbook

## When

Creating or changing a repository-local Wild Bunch skill.

## Required skills

- `/writing-skills`
- `/refreshing-installed-skills`
- `/repo-standards`

## Composition

`/writing-skills` owns test-first skill design. Register the accepted local
skill by exact name, refresh projections, and validate repository shape.

## Doctrine and contracts

[Repository skills policy](../doctrine/repo-skills-policy.md) owns custody. A
local skill owns one recurring Wild Bunch-specific judgment that no doctrine,
contract, runbook, script, or portable skill already owns.

## Local commands and paths

- Source: `.agents/skills/<exact-name>/`
- Registration: `.agents/plugins/marketplace.json` `repo.local_skills`
- Validation: [marketplace generation](marketplace-generation.md) followed by
  [testing](testing.md)

## Evidence contract

The skill's directory and frontmatter names match registration, focused tests
exercise its boundary, and refresh preserves it outside marketplace provenance.

## Prohibited combinations

- Do not add marketplace provenance fields or `agents/openai.yaml` to a local
  skill unless a separate publication task requires them.
- Do not encode a repository lifecycle in a capability skill.
