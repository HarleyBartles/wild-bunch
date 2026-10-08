# ADR-0014 DDD, Onion Boundaries, CQRS, and Unit of Work

## Status

`live`

## Dated History

- `2026-06-01` - Adopted DDD aggregate boundaries, inward dependency direction, separated command/query handlers, repository ports, and a first-class Unit of Work for writes.
- `2026-10-08` - Editorial clarification: query and command persistence ports remain distinct; implementation inventories and external issue tracking are not part of this decision.

## Decision Type

`architecture`, `persistence`

## Related ADRs

- `depends on`: ADR-0002
- `related to`: ADR-0015, ADR-0017, ADR-0018, ADR-0019, ADR-0028, ADR-0038

## Context

The game needs domain-owned legality, inward dependency direction, distinct read and write responsibilities, and coordinated persistence of one command's changes.

## Decision

Use DDD boundaries with dependencies pointing inward. Keep gameplay rules in the domain, route commands and queries through separate application handlers, expose aggregate-scoped write repositories and query-only read repositories, and coordinate command persistence through an application-facing Unit of Work. CQRS is a strict separation of responsibilities, not a requirement for separate databases or infrastructure.

## Rationale and Alternatives

Putting persistence concerns in the domain would couple gameplay rules to infrastructure. A generic data-access layer with mixed reads and writes would obscure aggregate authority. The selected boundaries keep gameplay, orchestration, persistence, and projections independently understandable.

## Consequences

Domain and application code do not depend on EF Core or persistence implementations. Command writes stage aggregate changes through the Unit of Work; queries use read models without mutation authority. The session consistency boundary is further specified by ADR-0038.
