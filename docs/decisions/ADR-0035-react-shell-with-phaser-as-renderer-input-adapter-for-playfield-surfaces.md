# ADR-0035 React Owns Playfield State; Phaser Renders and Returns Intent

## Status

`partially superseded`

## Dated History

- `2026-06-28` - Chose React as owner of playfield presentation and selection state, with Phaser limited to spatial rendering and player input intent.
- `2026-10-08` - The map transport changed from a global map and one-step confirmation to a session-derived map with staged setup and start; those endpoint and DTO shapes are historical. ADR-0039 supersedes the original setup-flow and initial-map assumptions. Accessible equivalent town selection remains an accepted decision, but the current implementation lacks it; the reason for that loss is unknown and no decision retires the requirement.

## Decision Type

`ui`, `architecture`, `accessibility`

## Related ADRs

- `depends on`: ADR-0016, ADR-0027
- `partially superseded by`: ADR-0039
- `related to`: ADR-0022

## Context

A spatial playfield benefits from map rendering and pointer interaction, while game truth, accepted selection, and accessible controls must remain in the React and server-owned flow. The initial town-selection flow included a DOM alternative to the canvas interaction.

## Decision

React owns the playfield's server-state coordination, player-facing display, and selection state. Phaser is a renderer and input adapter: it displays supplied data and returns player intent, but does not call the game API, determine legal choices, or own accepted game state. The same capability must remain available through an equivalent keyboard- and screen-reader-accessible interaction.

ADR-0039 governs the current meaning of town selection: after the prologue the player chooses one town on the shared world map, and that selection performs the free first arrival. This does not remove the accessibility requirement.

## Rationale and Alternatives

Letting the canvas decide legality or call mutations would create a second client-owned game model. Replacing the whole React shell with Phaser would move routing, API state, forms, and game truth into a rendering framework. A DOM alternative preserves the accepted interaction for people who cannot use pointer input.

## Consequences

Renderer changes may replace Phaser without changing state ownership or accessible selection. The current town-selection UI does not provide the accepted equivalent keyboard path. This is an implementation gap for the owning roadmap row; the 2026-10-08 audit found no evidence that its removal was an intentional product decision.

## Successors and Surviving Scope

ADR-0039 supersedes the original map and setup-flow assumptions. React ownership, intent-only renderer input, and accessible-equivalent town selection remain in force.
