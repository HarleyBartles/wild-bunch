# Skill authoring playbook

## When

Creating or changing a repository-local Wild Bunch skill.

## Required capabilities

- Design and author a focused repository capability and its behavior tests.
- Validate authored skill custody and test executable script behavior.

## Unslop before work
Before work in this scope, follow the [unslop playbook](unslop.md) and its scoped profile selection. Consult the [decision-record playbook](decision-records.md) when work makes, changes, corrects, or materially removes a durable decision.

## Composition

1. Use capability skill design and authoring to define and test one focused Wild Bunch capability or
   judgment; keep repository lifecycle sequencing in runbooks.
2. Create the authored source under `.agents/skills/<skill-name>/` and set the
   frontmatter `name` to the directory name.
3. If the skill owns executable scripts, cover their behavior in
   `scripts/tests/` and include those tests in the repository check lane.
4. Update the repository skill policy and its certification when source
   custody or validation behavior changes.

## Doctrine and contracts

[Repository skills policy](../doctrine/repo-skills-policy.md) owns custody. A
local skill owns one recurring Wild Bunch-specific judgment that no doctrine,
contract, runbook, script, or portable skill already owns.

## Local commands and paths

- Source: `.agents/skills/<exact-name>/`
- Plugin dependencies: `.agents/plugins/marketplace.json`
- Authored skill policy: `../doctrine/repo-skills-policy.md`
- Validation: [marketplace generation](marketplace-generation.md) followed by
  [testing](testing.md)

## Evidence contract

- [ ] Directory and frontmatter names match exactly.
- [ ] Focused tests exercise the skill's declared capability boundary.
- [ ] Executable scripts have behavior coverage in the repository test lane.

## Prohibited combinations

- Do not add marketplace provenance fields or `agents/openai.yaml` to a local
  skill unless a separate publication task requires them.
- Do not encode a repository lifecycle in a capability skill.

## Runbook routing

- [Implementing](../runbooks/implementing.md)
