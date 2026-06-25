---
name: crew
description: gpt-wide doctrine for the Crew thinking roles. use when another skill or user request needs route, authority, fallback, or receipt reasoning, when a plan or claimed green needs a dependency check, or when the task needs a clean Crew lens before action. do not use as the execution surface for linear dispatch, github proof, validation, reporting, or skill packaging; specialist skills own those actions.
metadata:
  source-id: crew
  source-path: sources/first_party/skills/crew/SKILL.md
  provenance-name: "MARK-9 chunk ledger \xC3\xA2\xE2\u201A\xAC\xE2\u20AC\x9D base and control plane"
license: "MIT"
---
# Crew

## Purpose

`crew` is the GPT-wide doctrine source for the Crew thinking model. The Crew are named role-lenses GPT wears to interrogate a task before action. They are not autonomous agents, worker identities, execution actors, or authorities.

This skill defines the role model, dependency order, TPS handoff discipline, and golden-plan contract. It does not run crew-buster, write dispatches, mutate Linear or GitHub, validate packages, publish PRs, or replace specialist skills.

## Current workflow boundary

Use Crew to prepare reasoning for the correct downstream owner, not to choose a new control plane by itself.

For normal coding workflow, the control plane is Linear/Codex: Linear issue as task contract, Codex worker where the golden gate says the task is executable, human Create PR gate, and GitHub PR/main verification.

For GPT-native skillwork, route to `skill-creator` for authored skill content and `writing-skills` for cross-repo wording and doctrine checks when relevant. Do not let Crew reasoning make GPT-native installed-skill edits look like Codex Cloud repo tasks unless the editable skill source is proven repo-backed and accessible to the worker.

For GitHub evidence, route to the repo/GitHub proof surface. For validation choice, route to the validation decision surface. For report language, route to `tps-reporting`. For old chat/YAML worker packets, use the legacy dispatch skills only when Linear/Codex is unavailable, unsuitable, or explicitly rejected.

## Canonical terms

`Crew` means the six-role thinking system: Index, Silk, Writ, Klause, Rollback, and Receipt.

The role names are proper nouns. Capitalized role names refer to Crew roles. Lowercase words keep ordinary meanings unless the surrounding context clearly invokes the Crew model.

Examples:

- `Index` is the starting-terrain role; `index the repo` is an ordinary action.
- `Rollback` is the fallback/resilience role; `rollback plan` is ordinary recovery strategy.
- `Receipt` is the integrity and filing role; `give me a receipt` is an ordinary durable record.

## Reference loading

Read `references/crew-roles.md` when role boundaries, dependency order, or proper-noun disambiguation matter.

Read `references/tps-handoffs.md` when a Crew output must be handed from one role to another, consumed by a buster, used in a worker/issue plan, or inspected after a false-green concern.

Read `references/golden-plan-contract.md` when a plan or claimed GREEN must be prepared for golden-gate falsification.

Do not load every reference by default. Stop once the unresolved decision is owned by an already-read surface or a specialist skill.

## Non-goals

Do not import Adventures lore, character material, visual design, or story canon. Crew names are operational handles only.

Do not store domain-specific dispatch law, issue closure law, validation selection, GitHub operations, package validation, reporting hygiene, project doctrine, or artifact workflows here. Specialist skills own those domains.

Do not turn this doctrine into a verbose checklist for ordinary chat. Use `crew-buster` when the user wants Crew applied to a concrete task and expects a plan, repair, blocker, or route result.
