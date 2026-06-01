# ADR-0017 Use xUnit, Vitest, Testing Library, and explicit PostgreSQL validation lanes

## Status

live

## Dated Status History

- 2026-06-01 - live: the repo records xUnit, Vitest, Testing Library, jsdom, and explicit PostgreSQL validation lanes as the current testing posture.

## Decision Type

testing, operations

## Related ADRs

- `depends on`: ADR-0014, ADR-0015, ADR-0016, ADR-0018
- `informs`: ADR-0004

## Context

The repository has multiple test layers: .NET domain/application/game-content/integration projects, ASP.NET Core integration infrastructure, and a frontend test stack built around Vitest and Testing Library. The PostgreSQL lane is explicit and should remain distinct from pure unit testing.

## Decision Drivers

- Backend, domain, application, and integration tests need a consistent .NET test stack.
- Integration validation needs ASP.NET Core test infrastructure where appropriate.
- PostgreSQL-specific validation should be explicit rather than hidden behind a generic harness.
- Frontend tests need a TypeScript-aware browser-like environment.
- Generated or randomized travel tests should remain deterministic.

## Decision Summary

Use xUnit with Microsoft.NET.Test.Sdk and coverlet for .NET test projects, ASP.NET Core testing infrastructure for integration coverage where needed, Vitest with Testing Library and jsdom for the web client, and an explicit PostgreSQL validation lane that is documented and skippable when the required connection string is absent.

## Detailed Decision Breakdown

The backend test projects all target `net10.0` and use xUnit as the shared test framework. The integration test project adds ASP.NET Core test support and Npgsql/EF-based validation for persistence work. The PostgreSQL lane is intentionally explicit so absence of the repo-local connection string is treated as lane setup evidence rather than a product success claim.

On the frontend, Vitest provides the test runner, Testing Library covers React interaction, and jsdom supplies the browser-like runtime. The project also keeps TypeScript build and typecheck scripts as part of the validation surface.

Deterministic travel tests should use explicit seeds or direct setup, not repeated sampling or "usually passes" behavior.

## Options Considered and Rejected

- Use a random-sampling style of test and accept flaky passes.
- Treat SQLite as a substitute for PostgreSQL-specific persistence behavior.
- Rely on browser-only manual validation as the only test strategy.
- Skip the PostgreSQL lane silently when the connection string is not configured.

## When a Rejected Option Would Have Been Better

Random sampling would only be better for throwaway exploration, not for durable repo tests. SQLite would only be better if the persistence adapter intentionally targeted SQLite behavior, which it does not.

## Benefits

- Test intent stays explicit across backend and frontend layers.
- The PostgreSQL lane can exercise the real provider.
- Deterministic tests are easier to trust and debug.

## Accepted Tradeoffs

- The repo maintains several validation surfaces instead of a single universal test command.
- PostgreSQL lane execution requires local setup.

## Risks

- If the documented connection-string path changes, the PostgreSQL lane docs must be updated.
- Frontend test coverage can drift if typecheck and runtime tests are not both run.

## Consequences for Future Work

New behavior should normally include tests in the same slice, and persistence or travel changes should continue to use deterministic setup rather than flaky sampling. The PostgreSQL lane should stay explicit and documented.

## Implementation Status or Plan

Live. The existing project files and tests already show the current stack.

## Related Stable Source Surfaces

- `tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`
- `tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`
- `tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj`
- `tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj`
- `tests/WildBunch.Integration.Tests/PostgreSqlPersistenceTests.cs`
- `src/WildBunch.Web/package.json`
- `src/WildBunch.Web/src/api/wildBunchApi.test.ts`
- `src/WildBunch.Web/src/components/TravelPanel.test.tsx`
- `src/WildBunch.Web/src/components/TravelRoutesPanel.test.tsx`
- `.agents/architecture-hygiene.md`

## Proof of Implementation or Explicit Non-Implementation

The test project files declare xUnit, coverlet, Microsoft.NET.Test.Sdk, and integration-test dependencies, while the frontend package and test files show Vitest, Testing Library, jsdom, and explicit API/request assertions. The PostgreSQL lane test is skippable only when its connection string is not configured.

## Review Triggers

- When the test framework changes.
- When PostgreSQL validation stops being a distinct lane.
- When travel tests start depending on nondeterministic sampling.
