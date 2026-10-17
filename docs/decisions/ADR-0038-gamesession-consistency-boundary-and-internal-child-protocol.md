# ADR-0038 GameSession Consistency Boundary and Internal Child Protocol

## Status

`live`

## Dated History

- `2026-10-08` - Adopted `GameSession` as the external command consistency and event-production boundary while retaining cohesive rule ownership in its internal child components.

## Decision Type

`architecture`, `persistence`

## Related ADRs

- `partially supersedes`: ADR-0020
- `partially supersedes`: ADR-0028
- `related to`: ADR-0002, ADR-0005, ADR-0013

## Context

The repository chose `GameSession` as the top-level command root, while later domain extraction introduced focused child components that own cohesive rules. Some earlier decision text described children as independently producing events or cross-aggregate protocols that no longer describe the chosen consistency boundary.

## Decision

External gameplay commands are consistent through one `GameSession` command boundary. `GameSession` owns event production, application of resulting facts, and the consistency decision for the command. Internal children may own cohesive rules and return outcomes or proposed facts to the root; they do not independently commit or establish a second command consistency boundary. A child may not mutate another child's authority by reaching through its public API. Persistence layout does not determine domain ownership.

## Rationale and Alternatives

This preserves focused domain rule ownership without splitting one player action across independently committed command roots. Independent child event ownership and cross-aggregate coordination were the competing protocol described in the superseded portions of ADR-0020 and ADR-0028.

## Consequences

Commands produce one coherent result at the session boundary, and persisted facts can be replayed through that boundary. Child extraction remains useful when it clarifies rule ownership, but does not imply a new aggregate or repository. This decision does not require every invariant to be implemented directly on the `GameSession` type.

## Successors and Surviving Scope

This record partially supersedes ADR-0020's autonomous child event-emission and cross-aggregate protocol and ADR-0028's corresponding child-protocol framing. ADR-0020 remains authoritative for cohesive domain legality and the prohibition on reach-through mutation. ADR-0002, ADR-0005, and ADR-0013 remain authoritative for the root's relationship to the case file and journey subtree.
