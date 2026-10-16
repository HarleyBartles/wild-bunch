---
name: wild-bunch-browser-game
description: Use when Wild Bunch browser work needs a decision about client state authority or presentation ownership.
metadata:
  status: active
  scope: Wild Bunch browser state-authority decisions.
  use_when:
    - Use when a browser task could put authoritative game state in React, Phaser, or another client owner.
  do_not_use_when:
    - Do not use for backend-only gameplay or persistence work.
---

# Wild Bunch Browser Game

Before using this skill, follow the [unslop playbook](../../playbooks/unslop.md) for browser presentation and any developer boundary it touches and read the selected profiles in full; consult the [decision-record playbook](../../playbooks/decision-records.md) when work makes, changes, corrects, or materially removes a durable decision.

## Owned decision

Decide whether browser state belongs to React presentation, Phaser playfield
rendering/input, or the backend-authoritative game model.

## Method

1. Read the live browser/API path and [frontend standards](../../doctrine/frontend-standards.md).
2. Classify each fact as authoritative game state, player-known presentation
   state, ephemeral rendering/input state, or developer-only state.
3. Return the owner, command boundary, visibility boundary, and browser proof
   needed to falsify the choice. Route developer-control semantics to
   `dev-control-boundary`.

## Boundary

This skill does not implement React, Phaser, styling, or browser tests. A
runbook may compose it with `react`, `phaser-2d-game`, `game-ui-frontend`,
`game-playtest`, `playwright-testing`, or `web-styling` when those capabilities
are actually required.
