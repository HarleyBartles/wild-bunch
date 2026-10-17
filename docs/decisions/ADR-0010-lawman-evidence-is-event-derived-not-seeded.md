# ADR-0010 Lawman Evidence Is Event-Derived, Not Seeded

## Status

`planned`

## Dated History

- `2026-06-01` - Chose that a future lawman evidence system should derive its claims from game events and established facts rather than a static seeded evidence list.
- `2026-10-08` - Clarified the initial telegraph scope: its first use is lawman intelligence. This record does not authorize other telegraph clue types.
- `2026-10-10` - Clarified current scope: stable 0.1.0 removes the pre-alpha public-notice clue and local-record interaction. This does not change the planned event-derived lawman intelligence decision.

## Decision Type

`gameplay`

## Related ADRs

- `depends on`: ADR-0006, ADR-0007, ADR-0009
- `related to`: ADR-0013, ADR-0029

## Context

The current case uses seeded clues, public notices, and structured anchors. Lawman intelligence has a different meaning: it describes what the lawman could learn from events and where those events became knowable.

**Editorial clarification (2026-10-10):** The first sentence describes the pre-alpha game when this decision was recorded. Stable 0.1.0 removes the public-notice clue and local-record interaction; the event-derived lawman intelligence decision remains planned.

## Decision

Lawman evidence derives from actual game events and their relevant timing, visibility, and location. Do not create a static seeded evidence roster that merely mirrors the player's clue content. The first telegraph use is limited to lawman intelligence; additional telegraph content is not decided here.

## Rationale and Alternatives

A seeded evidence list could claim that the lawman knows something the playthrough never established. Deriving evidence from event facts preserves provenance and keeps lawman knowledge distinct from the player's seeded case clues.

## Consequences

This is an accepted design constraint, not a claim that lawman evidence or telegraph intelligence exists in the current game. Any implementation must preserve the difference between the lawman's event-derived knowledge and the player's clues.
