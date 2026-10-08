# ADR-0027 Routed Browser Shell and Server-Owned Game State

## Status

`partially superseded`

## Dated History

- `2026-06-21` - Chose a routed player-facing browser shell with shared query state and a separate developer surface, replacing the single-cockpit interaction model.
- `2026-10-08` - Partially superseded the original setup and map-flow assumptions through ADR-0039; the routed shell, query ownership, and server authority remain.

## Decision Type

`ui`, `architecture`

## Related ADRs

- `depends on`: ADR-0016, ADR-0019
- `supersedes`: ADR-0011
- `partially supersedes`: ADR-0016
- `partially superseded by`: ADR-0039
- `related to`: ADR-0011, ADR-0022, ADR-0030, ADR-0035

## Context

The early browser client placed play, case, travel, and developer tools in one cockpit surface and duplicated query and mutation handling. The player-facing client needed clearer navigation and a data boundary that remained a client of server-authoritative game state.

## Decision

Use a routed React shell for player surfaces, with server queries and mutations coordinated through the selected client query layer. Keep game legality and durable state on the server. A developer surface remains separate from ordinary player navigation. React owns player-facing presentation and interaction state; renderer adapters may present the playfield and return player intent without deciding game truth.

The original setup sequence, route catalogue, and map-selection assumptions are historical. ADR-0039 governs the current setup, prologue, starting-town, and shared-map flow.

## Rationale and Alternatives

Routing separates distinct player surfaces and makes navigation explicit. A single cockpit couples every interaction to one dense page; duplicating server state in a client-owned game model risks displaying or acting on facts the server has not accepted.

## Consequences

Player components render and submit intent against server-owned state. Browser route and component structure may evolve without changing the game contract. The original route list and setup-flow diagram are not current navigation authority.

## Successors and Surviving Scope

ADR-0039 supersedes the original setup and map-flow assumptions. Routed player surfaces, client query ownership, server authority, and a separate developer surface remain the decisions recorded here.
