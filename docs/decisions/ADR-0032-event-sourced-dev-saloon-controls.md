# ADR-0032 Developer Saloon Overrides Respect Encounter Eligibility

## Status

`live`

## Dated History

- `2026-06-26` - Added event-backed developer controls for inspecting saloon context and forcing the next person of interest, with explicit hidden-truth access on the guarded developer surface.
- `2026-06-27` - Clarified that forced suspects use the same availability and killer-release eligibility rules as ordinary saloon generation; a false-identity outcome comes from the normal player confrontation rather than a separate forced category.

## Decision Type

`architecture`, `gameplay`, `persistence`, `security`

## Related ADRs

- `depends on`: ADR-0007, ADR-0028, ADR-0030
- `follows`: ADR-0031
- `related to`: ADR-0036, ADR-0041

## Context

Playtesting saloon encounters needs controlled inspection and generation. The control must not bypass the ordinary eligibility rules or turn a developer-only observation into player knowledge.

## Decision

Developer saloon controls may inspect deliberately scoped hidden truth and force the next saloon person of interest. Forcing, clearing, and consuming an override are event-backed facts. A forced suspect must satisfy the same availability rules as ordinary generation, including the culprit-release gate. The override is consumed once by the next saloon look-around. Citizen encounters remain a distinct ordinary outcome; a false identity is produced by the normal confrontation flow, not by a special forced false-lead type.

Hidden truth may be returned only by a distinct, guarded developer surface. Player APIs and projections remain constrained by ADR-0007. These developer capabilities are limited to explicitly developer-enabled environments under ADR-0041.

## Rationale and Alternatives

Allowing a developer control to produce an otherwise ineligible suspect would test a different game from normal play and could bypass the mystery's release rule. Hiding truth in a player DTO or ordinary route would make the knowledge boundary depend on the client.

## Consequences

The forced encounter remains subject to the same domain eligibility and event-replay rules as ordinary play. Developer inspection can expose useful truth to the owner without changing player-facing clues, legal status, or confrontation outcomes.
