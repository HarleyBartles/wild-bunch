# ADR-0006 Investigation Reveals Knowledge, Not Gang Pressure

## Status

`live`

## Dated History

- `2026-06-01` - Chose investigation as a player-knowledge reveal, not a gang-pressure or disruption mechanic.
- `2026-10-08` - Editorial clarification: knowledge-only describes the investigation outcome, not every session effect of entering or using a source; time and other established game rules may also change.

## Decision Type

`gameplay`

## Related ADRs

- `depends on`: ADR-0005
- `related to`: ADR-0007, ADR-0008, ADR-0009, ADR-0010

## Context

The case loop gives the player clues and public knowledge to support an investigation. A separate gang-pressure or disruption score would be a different mechanic and must not be inferred from knowledge discovery.

## Decision

Investigation reveals knowledge to the player. It does not, by itself, advance a gang-pressure or disruption system. This boundary does not assert that source visits have no other game effects, such as advancing time or applying an already-defined town rule.

## Rationale and Alternatives

Keeping knowledge separate from an unimplemented pressure track prevents player-facing actions from implying a hidden system that has not been designed. A pressure or disruption mechanic would require its own player meaning and decision.

## Consequences

Public case information represents what the player has learned; hidden progress remains governed by ADR-0007. No gang-pressure feature is implied by this record.
