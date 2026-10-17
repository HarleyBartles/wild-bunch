# ADR-0029 Heat Is Lawman Pressure, Not Trail Danger

## Status

`live`

## Dated History

- `2026-06-24` - Reframed heat as future lawman pressure rather than route risk or generic trail danger.
- `2026-06-25` - Removed heat changes from trail events and encounters; retained town-day rollover and journey-start reset behavior.
- `2026-10-08` - Factual clarification: the clock's turn numbering is zero-based. The daily rollover is from the last turn to the first turn of the next day, not the earlier `turn 4` to `turn 1` notation; the heat rule did not change.

## Decision Type

`gameplay`

## Related ADRs

- `related to`: ADR-0013, ADR-0020, ADR-0028

## Context

Heat had been used as a proxy for route risk and private trail hardship despite the intended meaning being attention that a future lawman system could consume. The game has no lawman pursuit behavior in this decision's scope.

## Decision

Heat represents lawman and town attention, not route danger, encounter risk, or reputation. It increases by one when a full in-town day rolls over, resets to zero when a journey begins, and does not change because a route is risky, a private trail event occurs, or a trail encounter is resolved. Its current high or low value has no lawman effect.

## Rationale and Alternatives

Treating private hardship or route danger as lawman attention gives the value a misleading meaning. Connecting heat to pursuit, interception, or encounter difficulty requires a separately designed lawman feature.

## Consequences

Current turn labels use zero-based numbering; this changes only how rollover is described, not when heat changes. A later lawman feature may consume heat under a new decision, but no pursuit capability is established here.
