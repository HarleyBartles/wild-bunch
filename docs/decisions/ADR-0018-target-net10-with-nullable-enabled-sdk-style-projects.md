# ADR-0018 Target .NET 10 with nullable-enabled SDK-style projects

## Status

live

## Dated Status History

- 2026-06-01 - live: the repo standardizes on .NET 10 SDK-style projects with nullable enabled.

## Decision Type

architecture, tooling

## Related ADRs

- `depends on`: ADR-0001
- `informs`: ADR-0014, ADR-0015, ADR-0017, ADR-0019

## Context

The source tree shows a consistent project baseline across the app, domain, persistence, and test projects: `TargetFramework` is `net10.0`, nullable reference types are enabled, and implicit usings are enabled. That baseline is broad enough to deserve its own record.

## Decision Drivers

- The repo should keep a uniform modern SDK-style project baseline.
- Nullable reference types should stay enabled across the solution.
- The baseline should be explicit in source rather than assumed from convention.
- New architecture and testing records should not have to restate the same platform baseline over and over.

## Decision Summary

Target .NET 10 across the repo, with nullable-enabled SDK-style projects and implicit usings turned on as the current baseline.

## Detailed Decision Breakdown

Every visible project file in the main source and test trees uses the same modern SDK-style shape with `TargetFramework` set to `net10.0`, `Nullable` enabled, and `ImplicitUsings` enabled. That gives the repo a clean, consistent base for both application code and tests.

This ADR records the current baseline only. It does not claim a long-term support policy beyond what the source itself shows.

## Options Considered and Rejected

- Keep older target frameworks or mixed language baselines across projects.
- Disable nullable annotations and let null-handling drift back into convention.
- Revert to older non-SDK csproj styles.

## When a Rejected Option Would Have Been Better

An older baseline would only be better if the repository still needed legacy compatibility with older runtime targets, which the current source does not show.

## Benefits

- The repo stays aligned on one modern target framework.
- Nullable annotations provide more explicit intent in code and tests.
- Project files remain simpler and more uniform.

## Accepted Tradeoffs

- The repo is intentionally tied to a current-generation .NET toolchain.
- Any future framework upgrade will need a deliberate source-backed decision.

## Risks

- A mixed-target project sneaking back in would weaken the baseline.
- Claiming policy beyond the source evidence would overstate what is actually decided.

## Consequences for Future Work

New projects should follow the same `net10.0`, nullable-enabled SDK-style baseline unless a later ADR changes the repo standard.

## Implementation Status or Plan

Live. The project files already show the consistent baseline.

## Related Stable Source Surfaces

- `src/WildBunch.Api/WildBunch.Api.csproj`
- `src/WildBunch.Application/WildBunch.Application.csproj`
- `src/WildBunch.Domain/WildBunch.Domain.csproj`
- `src/WildBunch.GameContent/WildBunch.GameContent.csproj`
- `src/WildBunch.Persistence/WildBunch.Persistence.csproj`
- `src/WildBunch.Web/package.json`
- `tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`
- `tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`
- `tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj`
- `tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj`

## Proof of Implementation or Explicit Non-Implementation

The repo's project files already target `net10.0` and enable nullable reference types and implicit usings consistently.

## Review Triggers

- When a project file diverges from the baseline.
- When the repo intentionally adopts a different framework target.
