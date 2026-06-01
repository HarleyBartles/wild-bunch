# ADR-0014 Use DDD, Onion dependency direction, CQRS handlers, repositories, and first-class Unit of Work

## Status

live

## Dated Status History

- 2026-06-01 - live: the repo records DDD aggregate boundaries, Onion dependency direction, CQRS-style handlers, repository roles, and first-class Unit of Work as the current architecture posture.
- 2026-06-01 - implemented: command handlers stage aggregate changes through the repository boundary and commit them through an application-facing Unit of Work.

## Decision Type

architecture, persistence, process

## Related ADRs

- `depends on`: ADR-0002, ADR-0003, ADR-0004, ADR-0013
- `informs`: ADR-0015, ADR-0017, ADR-0018, ADR-0019

## Context

Wild Bunch already has a visible command and persistence shape in source: `GameSession` is the live-play aggregate root, application code is organized around command and query handlers, and persistence lives in a separate project that adapts the domain instead of owning it. The repo also needs to record the now-decided Unit of Work posture so architecture guidance does not lag behind the current direction.

## Decision Drivers

- DDD aggregate authority should remain explicit.
- Onion dependency direction must keep Domain and Application free of Persistence and EF implementation details.
- CQRS-style handler separation is already the application shape and should remain the default.
- Command-side repositories must represent aggregate persistence boundaries, not generic data access.
- Read repositories should stay query-only.
- Unit of Work is now a first-class decided coordination boundary, and the codebase implements that boundary in the application/persistence split.

## Decision Summary

Use DDD with Onion dependency direction, CQRS-style command/query handlers, aggregate-scoped repositories, and a first-class Unit of Work for command-side persistence coordination. `GameSession` remains the current top-level command aggregate root, with session-owned aggregate/subaggregate boundaries beneath it unless a later ADR creates another root.

## Detailed Decision Breakdown

The domain model keeps the authority for live-play mutation inside `GameSession`. Application code routes commands through handler classes, and the persistence layer adapts that model through repository abstractions rather than leaking EF concerns upward.

Command-side repositories are the unit of aggregate persistence ownership. Read repositories are query-only projections and should not gain mutation behavior. This keeps mutation and query roles distinct without forcing a framework-first CQRS implementation.

Unit of Work is no longer a maybe-later nice-to-have. It is the chosen persistence coordination boundary for command work, and the code now implements that coordination explicitly in the application and persistence layers. Issue #40 remains the implementation track for the repository boundary cleanup and any future refinements.

## Options Considered and Rejected

- Introduce a generic repository/UoW framework layer before the repo needs it.
- Let service-layer mutation bypass the aggregate boundary.
- Blend read and write repository responsibilities into one abstraction.
- Depend on EF or persistence details from Domain/Application.
- Wait for multiple command repositories or aggregates before treating Unit of Work as a real decision.
- Move to framework-first CQRS before the current command model needs it.

## When a Rejected Option Would Have Been Better

A framework-first repository layer would only be better for a tiny CRUD application with little domain authority. Delaying Unit of Work would only be better if the repo still treated it as speculative, which it no longer does.

## Benefits

- The command model stays legible and source-backed.
- Domain code remains independent of persistence technology.
- Repository roles stay bounded and easier to test.
- Future aggregate splits have a clearer coordination model.

## Accepted Tradeoffs

- `GameSession` remains substantial because it owns live-play orchestration.
- The current Unit of Work implementation is intentionally small and session-specific rather than a generic framework layer.
- Repository and handler boundaries have to stay disciplined so the architecture does not drift into generic boilerplate.

## Risks

- `GameSession` could grow too large if future work stops extracting pure helpers around it.
- Unit of Work could be described too abstractly if issue #40 does not finish the implementation cleanup.
- A future change could accidentally reintroduce read/write repository blending.

## Consequences for Future Work

New command-side work should assume aggregate-scoped repositories, explicit handler boundaries, and first-class Unit of Work coordination unless a later ADR replaces that posture. Issue #40 is the implementation track for the remaining repository and UoW cleanup.

## Implementation Status or Plan

Live as architecture doctrine, with application-facing Unit of Work and repository staging in source.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/WildBunch.Domain.csproj`
- `src/WildBunch.Application/WildBunch.Application.csproj`
- `src/WildBunch.Persistence/WildBunch.Persistence.csproj`
- `src/WildBunch.Api/WildBunch.Api.csproj`
- `src/WildBunch.Application/Abstractions/IGameSessionRepository.cs`
- `src/WildBunch.Application/Abstractions/IGameSessionReadRepository.cs`
- `src/WildBunch.Application/Games/Commands/`
- `src/WildBunch.Application/Games/Queries/`
- `src/WildBunch.Persistence/DependencyInjection.cs`
- `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`
- `.agents/architecture-hygiene.md`
- `docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md`
- `docs/adr/ADR-0003-composed-jsonb-session-persistence.md`
- `docs/adr/ADR-0004-postgresql-local-development-and-validation-lane.md`
- `docs/adr/ADR-0013-travel-journey-is-a-session-owned-aggregate-subtree.md`

## Proof of Implementation or Explicit Non-Implementation

The repo shows the aggregate root, handler structure, repository split, persistence adapter boundaries, and an application-facing Unit of Work implementation in source. Issue #40 remains the traceable follow-up for any future repository boundary refinement.

## Review Triggers

- When a second command aggregate root becomes concrete.
- When read and write repository roles start drifting together.
- When issue #40 lands and the repository boundary cleanup changes shape.
