# ADR-0040 Difficulty and Randomness Vocabulary

## Status

`live`

## Dated History

- `2026-10-08` - Retained the implemented player-facing difficulty and randomness names and defaults, and clarified their separate roles.

## Decision Type

`gameplay`, `content`

## Related ADRs

- `partially supersedes`: ADR-0021, ADR-0023
- `related to`: ADR-0021, ADR-0024, ADR-0039

## Context

ADR-0023 locked names that later differed from the implemented player-facing vocabulary. The player needs two distinct replay controls: ordinary challenge and world variability.

## Decision

Difficulty values are `Easy`, `Standard`, `Challenging`, and `Brutal`, with `Standard` as the default. Randomness values are `Boring`, `Classic`, `Adventurous`, and `Wild`, with `Classic` as the default. Difficulty governs ordinary challenge and consequence pressure. Randomness governs the degree of world and event variation. Neither setting changes settled culprit identity or other recorded mystery facts, and neither axis substitutes for the other.

## Rationale and Alternatives

The current labels are already part of the player-facing setup and code contract. Restoring the earlier `Normal` and `Hard` labels would create a vocabulary that disagrees with the current game without changing the underlying separation of challenge and variation.

## Consequences

Defaults allow a player to begin with only a character name, while both controls remain available as replay levers. Randomness affects game generation and choices as they occur; event history records the resolved facts, so replay reconstructs those facts rather than rolling them again.

## Successors and Surviving Scope

This record partially supersedes the `GameEntropy` vocabulary captured in ADR-0021's setup descriptor and the `Normal` and `Hard` difficulty labels in ADR-0023. The UUID-shaped seed contract in ADR-0021 survives. ADR-0023's distinction between challenge pressure and variation, and its protection of established mystery truth, remain authoritative.
