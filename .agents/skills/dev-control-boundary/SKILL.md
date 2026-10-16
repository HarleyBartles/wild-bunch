---
name: dev-control-boundary
description: Use when deciding whether a proposed Wild Bunch developer control prepares game state or unlawfully forces a normal gameplay action or result.
metadata:
  status: active
  scope: Wild Bunch developer-control semantics and panel ownership.
  use_when:
    - Use for new or changed developer controls, dev panels, hidden-truth views, or forced scenario setup.
  do_not_use_when:
    - Do not use for ordinary player-facing controls with no developer capability.
---

# Dev Control Boundary

Before using this skill, follow the [unslop playbook](../../playbooks/unslop.md) for developer controls and backend commands and read the selected profiles in full; consult the [decision-record playbook](../../playbooks/decision-records.md) when work makes, changes, corrects, or materially removes a durable decision.

## Owned decision

Classify a developer control as lawful state preparation or an unlawful forced
gameplay action/result, and assign it to the panel that owns its primary noun.

## Method

1. Read [dev-overlay doctrine](../../doctrine/dev-overlay.md) and the live
   gameplay command the control is intended to exercise.
2. State the precondition changed by the dev command and the normal player
   action that must still resolve the outcome.
3. Reject controls that fabricate that action or outcome.
4. Return the owning panel, related-panel visibility, hidden-truth boundary,
   backend command/event receipt, and proof scenario.

## Boundary

This skill makes the semantic and ownership decision. The dev-overlay runbook
owns implementation order, skill composition, and closeout evidence.
