# ADR-0026 Turn-In Outcomes, Bounty Settlement, and Case Resolution

## Status

`live`

## Dated History

- `2026-06-10` - Chose to distinguish legal eligibility, turn-in outcome, settlement, murder-case resolution, and hidden culprit truth.
- `2026-10-08` - Factual correction: sheriff turn-in and bounty-cash settlement now exist in the game. Their implementation does not collapse bounty success into murder-case success; the earlier statement that those paths were absent is historical.

## Decision Type

`gameplay`, `architecture`

## Related ADRs

- `depends on`: ADR-0002, ADR-0005, ADR-0007, ADR-0025
- `related to`: ADR-0010

## Context

The initial sheriff design needed to make clear that a wanted person's legal terms, the handoff result, any money or penalty settlement, and the murder investigation have different meanings. Later implementation added turn-in and payout behavior, making the boundary operational as well as conceptual.

## Decision

Preserve the distinction between:

- public legal eligibility under a warrant;
- what happened during a turn-in;
- the money or penalty settled as a result;
- whether the murder case is resolved; and
- the hidden culprit identity.

Accepting a wanted person or paying a bounty does not establish murder-case success. A dead true culprit may satisfy a dead-or-alive bounty term, but killing the person who could clear the player's name is a case failure. A wrong identity or a dead wrong person is not equivalent to a correct lawful turn-in. Player-facing outcomes must not reveal hidden culprit truth.

## Rationale and Alternatives

A single result that combines legal eligibility, payment, and case resolution would make the sheriff's response ambiguous and could let an ordinary bounty satisfy the murder investigation. Separate meanings allow each outcome to remain truthful while the player learns the case from evidence.

## Consequences

Turn-in, settlement, and case resolution may be coordinated by one player action while retaining distinct domain outcomes. Current implementation of turn-in and bounty settlement corrects the record's old absence claim; it does not change the accepted separation.
