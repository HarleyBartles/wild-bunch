---
name: repo-composition
description: Use when creating, changing, or validating repository runbooks, playbooks, their policy mapping, or declared composition edges.
metadata:
  source-id: repo-composition
  source-path: codex-marketplace/plugins/agent-operating-model/skills/repo-composition/SKILL.md
  provenance-name: Repo Composition first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
license: MIT
---

# Repo Composition

Lifecycle stages are runbooks. Topical workflows are playbooks available whenever their concern applies. Runbook routing is optional; declared runbook/playbook edges resolve and agree on both sides. Playbooks may compose other playbooks through their `Composition` section, but composition targets must resolve and the resulting graph must remain acyclic.

The portable contract and scaffolds currently live with `repo-shape` while the coordinator consumes them. This skill owns their semantics and is the trigger for future changes to those assets.
