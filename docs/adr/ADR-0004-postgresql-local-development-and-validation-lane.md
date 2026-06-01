# ADR-0004 PostgreSQL Local Development and Validation Lane

## Status

live

## Dated Status History

- 2026-06-01 - live: the repo documents PostgreSQL as the local development
  database and the provider/storage validation lane.

## Decision Type

operations, persistence, testing

## Related ADRs

- `depends on`: ADR-0001
- `informs`: ADR-0003, ADR-0012

## Context

Wild Bunch needs a repo-local PostgreSQL convention so local development and
provider/storage validation do not depend on an unspecified machine-global
cluster or on an ad hoc database setup.

## Decision Drivers

- Local development must be reproducible.
- Provider/storage validation needs a real PostgreSQL lane.
- The persistent local app database must be separate from temporary test
  databases.
- Repo-local tooling and validation instructions should be explicit.

## Decision Summary

Use PostgreSQL as the live local development database and provider/storage lane,
with a repo-local persistent app database and a documented validation path for
test databases.

## Detailed Decision Breakdown

The repo documents a persistent local app database at `wildbunch_dev` on
`localhost:5434`, with repo-local tooling under `.local/` and explicit setup and
reset commands.

The validation guidance distinguishes the persistent app database from
temporary test-created databases so test cleanup does not silently target the
developer’s live local data.

## Options Considered and Rejected

- Depend on a machine-global PostgreSQL installation with no repo-local
  convention.
- Reintroduce SQLite as the primary local path.
- Treat provider/storage testing as optional and ad hoc.

## When a Rejected Option Would Have Been Better

A machine-global cluster would only be better if the project deliberately chose
to avoid repo-local setup, which it has not. SQLite would only be better if the
project’s current store topology had a source-backed reason to change.

## Benefits

- Local setup is explicit and reproducible.
- The provider/storage lane can exercise the real provider instead of a stub.
- Test and app databases stay conceptually separate.

## Accepted Tradeoffs

- Local setup has a documented provisioning step.
- Provider/storage validation is intentionally heavier than pure unit tests.

## Risks

- If the documented connection-string path changes, validation guidance must be
  updated with it.
- Misusing the persistent database as a test database would cause avoidable
  cleanup risk.

## Consequences for Future Work

Docs, integration tests, and persistence slices should continue to treat
PostgreSQL as the live provider choice unless a new source-backed decision says
otherwise.

## Implementation Status or Plan

Live. The local PostgreSQL docs and testing-lane docs already describe the
convention and the validation posture.

## Related Stable Source Surfaces

- `docs/local-postgresql.md`
- `docs/testing-lanes.md`
- `.agents/architecture-hygiene.md`
- `scripts/postgres-dev.ps1`
- `tests/WildBunch.Integration.Tests/PostgreSqlPersistenceTests.cs`

## Proof of Implementation or Explicit Non-Implementation

The repo documents the persistent local PostgreSQL setup, the repo-local
connection string, and the provider/storage lane that targets PostgreSQL-backed
tests.

## Review Triggers

- When the local database port, database name, or provisioning convention
  changes.
- When the provider/storage lane changes provider or store topology.
