# ADR-0010 Lawman Evidence Is Event-Derived, Not Seeded

## Status

planned

## Dated Status History

- 2026-06-01 - planned: the repo should treat future lawman-evidence systems as
  derived from events, visibility, timing, witnesses, ledgers, and movement
  rather than as a directly seeded clue pool.

## Decision Type

gameplay, future-domain

## Related ADRs

- `depends on`: ADR-0006, ADR-0007, ADR-0009

## Context

The current repo already has seeded clues, public warrants, town-visit source
refresh, and structured anchors. A future lawman-evidence system should build on
actual game events instead of becoming another static seeded list.

## Decision Drivers

- Evidence should come from what happened in the world.
- Lawman-facing information should reflect timing and visibility.
- Seeded content should not become a substitute for event derivation.
- The planned system must stay clearly separate from the current clue pools.

## Decision Summary

Future lawman evidence should be derived from gameplay events and state changes,
not seeded as a static evidence roster.

## Detailed Decision Breakdown

This planned rule means future lawman evidence would be created from things such
as player actions, witness timing, ledgers, location changes, and route movement.
It should not be assembled as a seeded list that merely mirrors clue content.

The intent is to keep lawman evidence reactive to the game rather than turning
it into another authored content pool.

## Options Considered and Rejected

- Seed lawman evidence exactly like public clues.
- Treat lawman evidence as a generic reuse of the clue/warrant content pool.
- Hide the derivation rule inside UI text only.

## When a Rejected Option Would Have Been Better

Seeded evidence would only be better for a temporary debug surface or a very
early prototype. It would not be appropriate once the product expects lawman
evidence to reflect what the player actually did.

## Benefits

- Evidence can react to the player’s actual trail through the world.
- The system avoids false certainty from static seed content.
- The mechanic stays aligned with investigative fiction.

## Accepted Tradeoffs

- This introduces a future derivation system instead of an immediate simple
  content list.
- The implementation will need clear event boundaries and careful source rules.

## Risks

- If built too early, the system could become overcomplicated.
- If built too late, seeded clues might get mistaken for lawman evidence.

## Consequences for Future Work

When a lawman-evidence feature is started, it should be specified as an
event-derived system and should not reuse this ADR as evidence that it is
already implemented.

## Implementation Status or Plan

Planned only. No lawman-evidence system exists in the current repo evidence set.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Game/GameSession.cs`
- `src/WildBunch.Domain/Cases/CaseFile.cs`
- `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`
- `src/WildBunch.Application/Games/Mapping/CaseBoardMapper.cs`

## Proof of Implementation or Explicit Non-Implementation

The current repo has seeded clues and warrants, but no separate lawman-evidence
derivation model. This ADR intentionally documents the future constraint rather
than claiming implementation.

## Review Triggers

- When a lawman-facing evidence feature is added.
- When the game gains event-tracking surfaces that can drive evidence creation.
