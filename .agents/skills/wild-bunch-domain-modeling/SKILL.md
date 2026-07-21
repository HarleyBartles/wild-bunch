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

Place Wild Bunch gameplay state and invariants inside the `GameSession` aggregate boundary without flattening them into handlers, persistence services, or UI code.

## Current model

- `GameSession` is the live-play aggregate root and external command boundary.
- It owns `BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`, and `ActionContextTracker`.
- Child components own cohesive state and rules. They receive narrow context, return outcomes or events-to-produce, and do not mutate sibling owners.
- Wallet and inventory remain concrete player state. Horse and saddle remain separate concepts; water is not a generic stackable good.
- Hidden culprit truth stays internal. Expose only player-known clues, journal entries, warrants, and investigation results.
- Travel is an active `JourneyLoop` that advances one trail day at a time and pauses for player choice. Do not replace it with an immediate multi-day town jump.
- Gameplay mutations follow the aggregate's typed-event route; do not add direct state changes outside event production and `Apply`.

## Workflow

1. Inspect the live aggregate, event, and test source for the rule being changed.
2. Identify the owning aggregate or child component.
3. Keep cross-component orchestration in `GameSession`; keep cohesive rules with their child owner.
4. Verify command-path and replay-path state converge.

## Reference

Read [Domain model](references/domain-model.md) for ownership and travel checks. For world setup, difficulty, or entropy, also read the project doctrine's difficulty and seeded-setup reference.

## Stop conditions

- Do not invent a new aggregate, service, or generic abstraction without an owned recurring invariant.
- Do not expose hidden investigation truth to read models or browser state.
- Use `wild-bunch-dotnet-architecture` when the decision crosses application or persistence boundaries.
