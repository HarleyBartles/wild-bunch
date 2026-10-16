# ADR-0004 PostgreSQL Local Development and Validation Lane

## Status

`live`

## Dated History

- `2026-06-01` - Chose PostgreSQL for local development and real-provider validation.
- `2026-06-22` - Moved the local service to a shared long-lived developer lane.
- `2026-08-24` - Moved the shared PostgreSQL service to `Z:/pg` outside the repository's local cluster directory; the topology changed, not the provider decision.
- `2026-10-08` - Editorial clarification: exact service paths, ports, and commands belong in operational documentation, not this decision record.

## Decision Type

`operations`, `persistence`, `testing`

## Related ADRs

- `depends on`: ADR-0001
- `related to`: ADR-0003, ADR-0014, ADR-0017

## Context

Persistence behavior depends on provider semantics. A local and test lane that uses only a substitute provider would not validate PostgreSQL-specific mapping, migration, or transaction behavior.

## Decision

Use PostgreSQL as the local development database and the real-provider persistence validation lane. Keep persistent developer data separate from temporary test data. Provider setup and exact commands are maintained in operational documentation.

## Rationale and Alternatives

A machine-global cluster without a repository convention makes setup and validation inconsistent. SQLite or an in-memory substitute remains useful for tests only where it preserves the behavior under test; it does not replace real PostgreSQL validation.

## Consequences

Persistence and migration work must be validated against PostgreSQL. Local service ownership and location may change without changing this decision, provided the documented lane continues to distinguish developer data from disposable test data.
