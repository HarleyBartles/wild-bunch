# Pointer-Operated World Map Selection for the 0.1.0 Baseline

## Status

`live`

## Dated History

- `2026-10-11` - Set the initial 0.1.0 town-selection input scope to pointer operation; the reason the earlier keyboard alternative disappeared is unknown.

## Decision Type

`ui`, `product-scope`

## Related ADRs

- `partially supersedes`: ADR-0035
- `partially supersedes`: ADR-0039

## Context

ADR-0035 records the React-owned playfield and Phaser-as-renderer/input-adapter boundary and originally requires an equivalent keyboard and screen-reader town-selection interaction. The current map exposes pointer selection only. The investigation found no evidence explaining when or why the earlier alternative disappeared. This cleanup is intended to make the 0.1.0 baseline truthful, not to restore an absent feature.

## Decision

For the initial 0.1.0 baseline, the player selects a starting town or later travel destination through pointer operation on the shared world map. An equivalent keyboard town-selection interaction is outside this baseline. React owns map presentation and local selection intent; the server owns legal choices and accepted game state. Phaser only renders the supplied map and reports pointer intent. This decision is limited to world-map town selection and does not change keyboard or focus expectations for other supported controls.

## Rationale and Alternatives

Implementing the missing keyboard path would add a capability that the current game does not ship, rather than repair a map authority or selection-legality defect. Keeping the earlier requirement active would tell later work to implement a feature deliberately excluded from this cleanup. The reason for the historical removal is unknown and is not inferred here.

## Consequences

The same world map supports the starting-town choice and later travel under their respective legal rules. Town selection remains pointer-operated in this baseline, and the renderer cannot establish or override legality. This decision does not establish a permanent input policy beyond the 0.1.0 baseline.

## Successors and Surviving Scope

This record partially supersedes ADR-0035's equivalent keyboard town-selection requirement and ADR-0039's statement that this requirement survives. ADR-0035's React-owned state, Phaser's intent-only role and server-owned legality survive. ADR-0039's setup, prologue, shared-map and one-shot free first-arrival flow remains live.
