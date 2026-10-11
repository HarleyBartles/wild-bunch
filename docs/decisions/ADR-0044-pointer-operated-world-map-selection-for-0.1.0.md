# Pointer-Operated World Map Selection for the 0.1.0 Baseline

## Status

`superseded`

## Dated History

- `2026-10-11` - Set the initial 0.1.0 town-selection input scope to pointer operation; the reason the earlier keyboard alternative disappeared is unknown.
- `2026-10-11` - Superseded by ADR-0045 after review found the town-selection feature already uses one shared world-map renderer and the earlier semantic town-button alternative had been removed without a deliberate product decision.

## Decision Type

`ui`, `product-scope`

## Related ADRs

- `partially supersedes`: ADR-0035
- `partially supersedes`: ADR-0039
- `superseded by`: ADR-0045

## Context

ADR-0035 records the React-owned playfield and Phaser-as-renderer/input-adapter boundary and originally requires an equivalent keyboard and screen-reader town-selection interaction. The current map exposes pointer selection only. The investigation found no evidence explaining when or why the earlier alternative disappeared. This cleanup is intended to make the 0.1.0 baseline truthful, not to restore an absent feature.

## Decision

This historical decision set pointer-only town selection for the initial 0.1.0 baseline. ADR-0045 supersedes that restriction and restores an equivalent keyboard and screen-reader interaction on the shared world map.

## Rationale and Alternatives

The original rationale treated the absent keyboard path as out of scope. A later review determined that the shared map is already a substantially implemented capability, that an earlier starting-town DOM alternative existed, and that no deliberate decision explained its removal. The exclusion is therefore superseded rather than treated as settled product truth.

## Consequences

The same world map supports starting-town choice and later travel under their respective legal rules. ADR-0045 now requires semantic accessible town controls alongside pointer operation; the renderer cannot establish or override legality.

## Successors and Surviving Scope

This record is superseded by ADR-0045. ADR-0035's React-owned state, Phaser's intent-only role, accessible town selection and server-owned legality remain live. ADR-0039's setup, prologue, shared-map and one-shot free first-arrival flow remains live.
