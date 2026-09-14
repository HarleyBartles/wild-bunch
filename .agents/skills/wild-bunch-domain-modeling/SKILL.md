---
name: wild-bunch-domain-modeling
description: Use when Wild Bunch work changes GameSession boundaries, gameplay invariants, player state, investigation truth, or trail-day travel rules.
metadata:
  status: active
  scope: Wild Bunch gameplay domain decisions.
  use_when:
    - Use when a task changes live-play domain rules or aggregate ownership.
  do_not_use_when:
    - Do not use for generic C# structure without Wild Bunch gameplay rules.
---

# Wild Bunch Domain Modeling

## Owned decision

Decide which Wild Bunch domain owner holds a proposed gameplay invariant or
state transition.

## Method

1. Read the relevant live source, [gameplay invariants](../../doctrine/gameplay-invariants.md),
   and the applicable sections of
   [architecture guardrails](../../doctrine/architecture-guardrails.md).
2. Name the invariant and the state it protects.
3. Assign it to `GameSession` when it coordinates session-level or
   cross-component behavior; otherwise assign it to the cohesive child that
   owns both the state and rule.
4. Return the owner, boundary rationale, forbidden sibling mutations, and the
   command/replay parity that must be proved.

## Boundary

This skill does not choose application, persistence, API, or UI placement. Use
`wild-bunch-dotnet-architecture` when the decision crosses those layers. Use
`seed-ownership` for seed, pressure, entropy, or player-setup ownership.
