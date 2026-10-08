# ADR-0039 Seeded Game Setup and Free First Arrival

## Status

`live`

## Dated History

- `2026-10-08` - Settled game-start ownership and the one-shot free arrival into the player's selected starting town.

## Decision Type

`gameplay`, `architecture`, `persistence`, `ui`

## Related ADRs

- `partially supersedes`: ADR-0016
- `partially supersedes`: ADR-0021
- `partially supersedes`: ADR-0027
- `partially supersedes`: ADR-0035
- `partially supersedes`: ADR-0036
- `related to`: ADR-0023, ADR-0028

## Context

The original setup and map decisions evolved as the game-start flow and seed ownership became clearer. The durable player flow is a new game, its prologue, then a choice of starting town that is also the first travel-map interaction.

## Decision

The player supplies a character name. Difficulty and randomness have defaults, and a new seed is generated for each visit to the new-game setup so quick play does not repeat one fixed world; a player may edit the seed. Confirming Go creates the `GameSession` and settles the seeded world and case truth, including gang identities and the culprit, before the prologue is read. The prologue presents the case lead from that settled truth. Player location remains unset until the player selects one starting town on the world map. That selection causes one free first arrival: the player is then in that town, with no second town-selection command. The same world map supports this first choice and later travel. The UUID-shaped seed is a public input contract resolved by a versioned, reversible codec; the codec's bit layout is not a stable contract. Event history records resolved facts so state reconstruction does not reroll random choices.

## Rationale and Alternatives

This keeps the original semantic sequence of setup, prologue, and starting-town arrival while making a quick start possible with the name alone. A seed-owned starting town and a separate town-selection map would either remove the player's initial choice or split one playable map concept into two flows.

## Consequences

World and mystery truth exist before the prologue is shown, while the initial player location is established by the one free arrival. Difficulty and randomness remain separate axes as described by ADR-0040. The game event stream records facts and is sufficient to reconstruct state; ordinary reads may use a current cache, and replay rebuilds an invalid cache from ordered history without rolling randomness again. No snapshot-only preparation path establishes a player game outside this flow.

## Successors and Surviving Scope

This record partially supersedes ADR-0016's cockpit-era setup assumption, ADR-0021's seed-owned starting town and bundled setup descriptor, ADR-0027's setup-flow shape, ADR-0035's initial map-flow assumption, and ADR-0036's exclusive snapshot-only preparation/start pattern. The UUID public input contract and hidden-truth boundary in ADR-0021 survive. ADR-0027's routed shell, query ownership, and server authority survive. ADR-0035's React-owned UI state and accessible-equivalent selection requirement survive. ADR-0036's clean player API boundary and backend ownership of dev overrides survive.
