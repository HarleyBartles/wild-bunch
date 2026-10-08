# ADR-0017 Use xUnit, Vitest, Testing Library, and PostgreSQL Validation

## Status

`live`

## Dated History

- `2026-06-01` - Chose xUnit for .NET tests and Vitest with Testing Library for browser-client behavior.
- `2026-10-08` - Editorial clarification: historical smoke-test skips do not make current real-PostgreSQL integration validation optional.

## Decision Type

`testing`

## Related ADRs

- `depends on`: ADR-0004, ADR-0014, ADR-0016
- `related to`: ADR-0022

## Context

The repository needs test tools aligned with its .NET domain/application boundaries, browser behavior, and PostgreSQL persistence adapter.

## Decision

Use xUnit for .NET tests and Vitest with Testing Library and jsdom for browser-client behavior. Validate persistence behavior that depends on the provider against real PostgreSQL. Each test belongs to the layer and behavior it protects; test-tool choice does not justify tautological or source-shape tests.

## Rationale and Alternatives

These tools support domain and application behavior tests, browser component behavior, and a real-provider integration lane. A substitute database cannot establish PostgreSQL-specific behavior.

## Consequences

Tests should protect observable behavior and negative cases at the owning boundary. PostgreSQL validation remains an explicit lane for provider-dependent behavior rather than a blanket requirement for every test.
