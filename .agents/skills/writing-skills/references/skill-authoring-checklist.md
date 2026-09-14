# Skill Authoring Checklist

When the consuming repository provides field-language contracts, apply them to
frontmatter and `agents/openai.yaml`. Do not copy a standalone `Use when`
description into `scope`, trigger-list values, short descriptions, or default
prompts.

Use this checklist when creating, reviewing, or refreshing a skill.

## 1. Choose custody and lane

- [ ] Local `.agents/skills/mark-*` skill, or marketplace `codex-marketplace/plugins/<lane>/skills/<name>/` source?
- [ ] Lane: `first_party`, `skills-with-source`, `skills-with-citation`, or `skills-with-mixed-source`?
- [ ] Read `local-and-marketplace-custody.md` if unsure.

## 2. Author with source-grounded authority

- [ ] Is the skill source-grounded? Read `source-grounded-authoring.md`.
- [ ] Authority evidence in `assets/authority/` where required.
- [ ] `SKILL.md` body is free of inline citations.

## 3. Scaffold

- [ ] Run `scripts/new_skill.py --name <name> --custody <local|marketplace> --lane <lane>` to scaffold.
- [ ] Review generated files before adding authority evidence.

## 4. Validate

- [ ] Frontmatter passes `.agents/contracts/skill-frontmatter.md`.
- [ ] `agents/openai.yaml` passes `.agents/contracts/openai-agent-yaml.md`.
- [ ] Bundled scripts support `--help` and `--check` per the skill-bundled CLI contract.
- [ ] The consuming repository's canonical marketplace-generation check passes.
- [ ] The consuming repository's canonical integration gate passes.
