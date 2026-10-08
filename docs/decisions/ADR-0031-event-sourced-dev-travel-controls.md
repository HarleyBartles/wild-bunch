# ADR-0031 Developer Travel Overrides Are Event-Backed and One-Shot

## Status

`live`

## Dated History

- `2026-06-25` - Added developer controls for forcing and clearing the next travel encounter, with consumption recorded so replay reconstructs whether the override was used.

## Decision Type

`architecture`, `gameplay`, `persistence`

## Related ADRs

- `depends on`: ADR-0028, ADR-0030
- `related to`: ADR-0007, ADR-0032, ADR-0036, ADR-0041

## Context

Playtesting needs a way to choose the next travel encounter without changing ordinary player commands or making the result disappear during event replay. A pending override must not be silently cleared outside the event history.

## Decision

Developer travel overrides are recorded as domain events. Forcing an override, clearing a pending override, and consuming it are distinct recorded facts. The next travel-day generation consumes a pending override once; later days use ordinary generation unless another override is forced. The generated result then follows the normal travel event and state path.

The override is a developer capability under ADR-0030 and ADR-0041. It does not alter the ordinary player command contract or disclose hidden case truth through player reads.

## Rationale and Alternatives

A transient flag set and cleared outside event history could produce different results after replay. Treating the override as event-backed lets the developer action and its one-time consumption be reconstructed without making it a second game authority.

## Consequences

Replay must preserve the pending, cleared, and consumed states in order. Developer controls may select a test result, but they do not waive ordinary travel legality or change the player-facing meaning of the resulting encounter.
