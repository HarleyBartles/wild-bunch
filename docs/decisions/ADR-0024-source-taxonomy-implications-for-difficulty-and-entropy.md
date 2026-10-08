# ADR-0024 Fair Variation Must Preserve Source Meaning

## Status

`live`

## Dated History

- `2026-06-10` - Recorded that source variation must preserve the two-axis fairness boundary in ADR-0023.
- `2026-10-08` - Editorial clarification: the earlier source-by-source forecast is historical design exploration, not a claim that the listed sources or variation mechanics exist in the current game.

## Decision Type

`gameplay`, `content`

## Related ADRs

- `depends on`: ADR-0023
- `related to`: ADR-0008, ADR-0009, ADR-0010, ADR-0021, ADR-0040

## Context

Clues and investigative sources can vary in presentation or availability, but the player must be able to reason from what the game has established. The earlier record expanded this principle into forecasts for sources and behaviors that are not all current features.

## Decision

When a feature uses difficulty or randomness to vary investigative information, it must preserve the authored meaning of that source and the case's settled truth. Difficulty and randomness remain distinct as stated in ADR-0023 and ADR-0040. This record does not establish behavior for an unimplemented source or promise any particular source taxonomy.

## Rationale and Alternatives

Variation can make repeated play less predictable, but changing the meaning of a clue or contradicting settled truth would make the investigation unfair. A source's player meaning should remain stable even when its presentation changes.

## Consequences

The effect of either axis is decided within the feature that uses it. Earlier source-by-source forecasts and implementation assignments are historical context, not current feature claims or backlog authority.
