# ADR-0035 React Owns Playfield State; Phaser Renders and Returns Intent

## Status

`partially superseded`

## Dated History

- `2026-06-28` - Chose React as owner of playfield presentation and selection state, with Phaser limited to spatial rendering and player input intent.
- `2026-10-08` - The map transport changed from a global map and one-step confirmation to a session-derived map with staged setup and start; those endpoint and DTO shapes are historical. ADR-0039 supersedes the original setup-flow and initial-map assumptions. Accessible equivalent town selection remains an accepted decision, but the current implementation lacks it; the reason for that loss is unknown and no decision retires the requirement.
- `2026-10-11` - ADR-0044 withdraws equivalent keyboard town selection from the 0.1.0 baseline while preserving this record's React/Phaser ownership and server-authority decisions. The reason the earlier interaction disappeared remains unknown.
- `2026-10-11` - ADR-0045 restores equivalent keyboard and screen-reader town selection for 0.1.0 through the shared world-map interaction; React/Phaser ownership and server-authority decisions remain unchanged.

## Decision Type

`ui`, `architecture`, `accessibility`

## Related ADRs

- `depends on`: ADR-0016, ADR-0027
- `partially superseded by`: ADR-0039
- `partially superseded by`: ADR-0044
- `partially superseded by`: ADR-0045
- `related to`: ADR-0022

## Context

A spatial playfield benefits from map rendering and pointer interaction, while game truth, accepted selection, and accessible controls must remain in the React and server-owned flow. The initial town-selection flow included a DOM alternative to the canvas interaction.

## Decision

React owns the playfield's server-state coordination, player-facing display, and selection state. Phaser is a renderer and input adapter: it displays supplied data and returns player intent, but does not call the game API, determine legal choices, or own accepted game state. The same capability must remain available through an equivalent keyboard- and screen-reader-accessible interaction.

At the time this decision was made, the equivalent keyboard and screen-reader interaction remained part of the accepted scope. ADR-0039 governs the current meaning of town selection: after the prologue the player chooses one town on the shared world map, and that selection performs the free first arrival.

ADR-0045 supersedes ADR-0044's temporary pointer-only input scope and restores the equivalent keyboard and screen-reader interaction for the 0.1.0 baseline. React-owned selection state, Phaser's intent-only role and server-owned legality remain.

## Rationale and Alternatives

Letting the canvas decide legality or call mutations would create a second client-owned game model. Replacing the whole React shell with Phaser would move routing, API state, forms, and game truth into a rendering framework. A DOM alternative preserves the accepted interaction for people who cannot use pointer input.

## Consequences

Renderer changes may replace Phaser without changing state ownership or selection legality. The 2026-10-08 audit recorded that the town-selection UI lacked the then-accepted equivalent keyboard path and that its removal reason was unknown. ADR-0044 temporarily withdrew that input requirement; ADR-0045 restores it through shared semantic town controls.

## Successors and Surviving Scope

ADR-0039 supersedes the original map and setup-flow assumptions. React ownership, intent-only renderer input, server-owned selection legality and accessible-equivalent town selection remain in force. ADR-0044 is superseded by ADR-0045.
