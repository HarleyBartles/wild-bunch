# Skill authoring playbook

## When

Creating or changing a repository-local Wild Bunch skill.

## Required skills

- `/writing-skills`
- `/refreshing-installed-skills`
- `/repo-standards`

## Composition

1. Use `/writing-skills` to define and test one focused Wild Bunch capability or
   judgment; keep repository lifecycle sequencing in runbooks.
2. Create the authored source under `.agents/skills/<exact-name>/` and register
   that exact name in the marketplace manifest's local-skill list without
   relying on a prefix.
3. Use `/refreshing-installed-skills` to prove refresh preserves local custody.
4. Use `/repo-standards` through the marketplace-generation and testing
   runbooks to validate projection, provenance, mesh, and committed shape.

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

- [ ] Directory, frontmatter, and `repo.local_skills` names match exactly.
- [ ] Focused tests exercise the skill's declared capability boundary.
- [ ] Refresh preserves the skill without marketplace provenance.
- [ ] Generated mesh and repository standards checks pass.

## Prohibited combinations

- Do not add marketplace provenance fields or `agents/openai.yaml` to a local
  skill unless a separate publication task requires them.
- Do not encode a repository lifecycle in a capability skill.

## Runbook routing

- [Implementing](../runbooks/implementing.md)
