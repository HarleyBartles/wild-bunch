# ADR-0015 Use ASP.NET Core Minimal APIs as the game HTTP boundary

## Status

live

## Dated Status History

- 2026-06-01 - live: the repo uses ASP.NET Core Minimal API endpoint groups as the HTTP boundary for game features.

## Decision Type

architecture, api

## Related ADRs

- `depends on`: ADR-0014
- `informs`: ADR-0016, ADR-0017, ADR-0019

## Context

The API project is a .NET Web SDK app that composes the game HTTP boundary from endpoint groups under `/api/games`. Source shows separate endpoint modules by game capability, a shared dependency injection setup, and OpenAPI support for development-time discovery.

## Decision Drivers

- HTTP endpoints should stay thin and delegate into application handlers.
- The game API should be organized by capability rather than by controller ceremony.
- Development-time OpenAPI metadata should remain available.
- Game mutation logic should not live directly inside endpoint lambdas.
- Public API contracts should stay decoupled from raw domain objects.

## Decision Summary

Use ASP.NET Core Minimal APIs as the game HTTP boundary, grouped under `/api/games`, with thin endpoint modules that delegate to application handlers and expose OpenAPI metadata in development.

## Detailed Decision Breakdown

The API bootstrap registers OpenAPI, configures services, and maps the endpoint group tree instead of using MVC controllers. Individual feature modules own the HTTP mapping for game, action, investigation, journal, town store, wanted poster, and travel capabilities.

The endpoint layer should remain a boundary, not a second domain model. It is responsible for request routing, response shaping, and wiring, while application handlers keep the actual game logic.

## Options Considered and Rejected

- Build the game API around MVC controllers by default.
- Put mutation logic directly into endpoint lambdas.
- Expose raw domain objects as public HTTP contracts.
- Choose GraphQL as the game API boundary before the repo has a source-backed need for it.

## When a Rejected Option Would Have Been Better

MVC would be better if the API had a large amount of controller-centric REST history, which it does not. GraphQL would only be better if the product needed a graph-shaped API contract more than the current game flow does.

## Benefits

- The API stays lightweight and easy to follow.
- Capability grouping keeps the surface area organized.
- Application handlers keep domain behavior out of the transport layer.

## Accepted Tradeoffs

- The endpoint modules have to stay disciplined so the surface does not drift into ad hoc route code.
- Minimal APIs are explicit code rather than declarative controller metadata.

## Risks

- Endpoint files could become too large if responsibilities are not kept narrow.
- The API layer could accidentally become logic-heavy if handler delegation slips.

## Consequences for Future Work

New game HTTP surfaces should follow the same grouped Minimal API pattern unless a later ADR replaces it. OpenAPI metadata should continue to support local development and discovery.

## Implementation Status or Plan

Live. The source tree already maps grouped Minimal APIs and OpenAPI support.

## Related Stable Source Surfaces

- `src/WildBunch.Api/WildBunch.Api.csproj`
- `src/WildBunch.Api/Program.cs`
- `src/WildBunch.Api/DependencyInjection.cs`
- `src/WildBunch.Api/Games/GameEndpoints.cs`
- `src/WildBunch.Api/Games/ActionEndpoints.cs`
- `src/WildBunch.Api/Games/GameSessionEndpoints.cs`
- `src/WildBunch.Api/Games/InvestigationEndpoints.cs`
- `src/WildBunch.Api/Games/JournalEndpoints.cs`
- `src/WildBunch.Api/Games/TownStoreEndpoints.cs`
- `src/WildBunch.Api/Games/TravelEndpoints.cs`
- `src/WildBunch.Api/Games/WantedPosterEndpoints.cs`

## Proof of Implementation or Explicit Non-Implementation

`Program.cs` registers OpenAPI and maps the app's API group tree, and the endpoint files under `src/WildBunch.Api/Games/` provide the grouped Minimal API surface.

## Review Triggers

- When the API moves to controller-style routing.
- When a new non-HTTP transport becomes the primary boundary.
- When endpoint modules begin to own game mutation logic instead of delegating it.
