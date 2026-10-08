# ADR-0023 Difficulty and Entropy Vocabulary and Fairness Contract

## Status

`partially superseded`

## Dated History

- `2026-06-09` - Chose separate challenge and world-variation axes and required both to preserve established mystery truth and fair solvability.
- `2026-10-08` - ADR-0040 replaces the original `Normal` and `Hard` difficulty labels with the current player-facing vocabulary. The axis separation and fairness boundary remain.

## Decision Type

`gameplay`, `content`

## Related ADRs

- `partially superseded by`: ADR-0040
- `related to`: ADR-0021, ADR-0024, ADR-0039

## Context

The game has two replay controls with different meanings: challenge pressure and variation in the generated world or events. Random variation must not rewrite facts that define the mystery.

## Decision

Difficulty and randomness are separate axes. Difficulty governs challenge and assistance; randomness governs variation and volatility. Neither may rewrite settled culprit identity or other established mystery truth, make the case unknowable, or create impossible facts.

## Rationale and Alternatives

A single blended slider would obscure whether a change makes play harder or simply less predictable. Allowing randomness to change the culprit after setup would invalidate the player's investigation rather than vary the world fairly.

## Consequences

Feature decisions may define how each axis affects that feature when it is designed. This ADR does not prescribe unimplemented source mechanics or require a feature to use both controls. Current names and defaults are defined by ADR-0040.

## Successors and Surviving Scope

ADR-0040 partially supersedes this record's difficulty labels and records the current defaults. This record remains authoritative for axis separation and protection of established mystery truth.
