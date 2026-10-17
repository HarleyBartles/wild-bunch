# Pointer-Operated Town Selection for 0.1.0

## Status

`live`

## Dated History

- `2026-10-11` - Retained the existing pointer-operated town selection for the 0.1.0 baseline after confirming map-level keyboard/screen-reader selection is not implemented; adding it would expand the release with a new interaction.

## Decision Type

`ui`, `product-scope`

## Related ADRs

- `supersedes`: ADR-0045
- `related to`: ADR-0035
- `related to`: ADR-0039

## Context

Starting-town choice and later travel already use one `WorldMapScene`, one `PhaserMapHost` and one world-map resource, with each interaction supplying its own eligible towns. The map currently accepts pointer selection only. The keyboard fallback in `TownHubSurface` selects buildings inside a town and does not select towns on the map. ADR-0045 proposed a new map-level semantic town selector for 0.1.0.

## Decision

For the 0.1.0 baseline, retain the existing pointer-operated town selection on the shared world map. Do not add map-level keyboard or screen-reader town controls in this release. The starting-town and later-travel interactions continue to apply their own legal eligible-town sets and report selection intent through the existing React owner; the server remains authoritative for accepted state.

This decision concerns only input for selecting towns on the map. Existing keyboard and screen-reader behavior for other supported controls, including town-building navigation, remains governed by its owning contract.

## Rationale and Alternatives

Current code has no map-level semantic town selector in either map context. Completing such a selector would require new controls and behavior, not finishing an existing partial implementation. The user chose to keep the baseline focused on implemented behavior rather than add that feature to 0.1.0. The shared map and server-owned eligibility remain intact.

## Consequences

The feature matrix and release plan describe town selection as pointer-operated. Tests and browser checks for this baseline protect the existing map behavior and legal eligible-town rules; they do not claim keyboard town selection. The historical WB-14 audit finding remains recorded with this explicit release disposition.

## Successors and Surviving Scope

This record supersedes ADR-0045's 0.1.0 requirement for accessible-equivalent map town selection. ADR-0035's React-owned selection state, Phaser intent-only boundary and server-owned legality remain live. ADR-0039's setup, prologue, shared-map and one-shot free first-arrival flow remains live.
