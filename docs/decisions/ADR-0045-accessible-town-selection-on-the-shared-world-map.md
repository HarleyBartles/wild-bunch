# Accessible Town Selection on the Shared World Map

## Status

`superseded`

## Dated History

- `2026-10-11` - Restored equivalent keyboard and screen-reader town selection for the shared world map after confirming the earlier DOM town-button path had been removed without a deliberate decision.
- `2026-10-11` - Superseded by ADR-0046 after current-code inspection confirmed map-level keyboard/screen-reader town selection is absent; adding it would be new 0.1.0 work, which the user chose to exclude.

## Decision Type

`ui`, `architecture`

## Related ADRs

- `partially supersedes`: ADR-0035
- `related to`: ADR-0039
- `supersedes`: ADR-0044
- `superseded by`: ADR-0046

## Context

The starting-town and later-travel interactions already use one `WorldMapScene` and one world-map resource, with legal selectable towns supplied by each interaction. The Phaser map currently accepts pointer selection only. An earlier starting-town interface exposed town choices as semantic DOM buttons, and no durable decision justified removing that accessible path. The town hub's separate keyboard building navigation does not provide town selection.

## Decision

The shared world map supports pointer selection and an equivalent semantic keyboard- and screen-reader-accessible town selection interaction. Both interactions report the same town-selection intent to React and use the same selected-town state and eligible-town rules. React owns local selection and presentation state; the server remains authoritative for legal choices and accepted game state. Phaser remains a renderer and pointer-intent adapter and does not own legality or game state.

Starting-town selection and later travel remain distinct game interactions over the same map. The initial selection can choose among towns legal for a free first arrival; later travel exposes only destinations legal from the current town. Accessible controls must represent the same legal choices as the pointer map, including disabled or current towns where the map already presents them as unavailable.

## Rationale and Alternatives

The map renderer and its two legal contexts are substantially implemented, and the earlier semantic selection path demonstrates an established interaction rather than a speculative new game capability. One shared accessible town control surface restores access without creating a second map, a second legality model, or a second selection state. Pointer-only selection was rejected because the reason the earlier alternative disappeared was unknown and no product decision justified the exclusion.

## Consequences

The shared map feature must be tested through both pointer and semantic keyboard selection in starting-town and travel contexts. Tests must establish that the same eligible-town contract applies to both inputs and that a selected destination drives the existing preview or first-arrival command. The accessible control may use ordinary focusable DOM buttons; the decision does not require keyboard navigation inside the Phaser canvas.

## Successors and Surviving Scope

This record superseded ADR-0044 when made. ADR-0046 later supersedes this release-scope decision. ADR-0035's React-owned selection state, Phaser intent-only boundary and server-owned legality remain live. ADR-0039's setup, prologue, shared-map and one-shot free first-arrival flow remains live.
