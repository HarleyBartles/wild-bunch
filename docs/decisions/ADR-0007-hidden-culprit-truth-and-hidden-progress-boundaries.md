# ADR-0007 Hidden Culprit Truth and Hidden Progress Boundaries

## Status

`live`

## Dated History

- `2026-06-01` - Chose to keep culprit identity and hidden case progress out of player-facing DTOs and read surfaces.
- `2026-10-08` - Editorial clarification: developer diagnostics are an explicit, separately controlled exception; they do not authorize disclosure through player APIs or UI.

## Decision Type

`gameplay`, `architecture`

## Related ADRs

- `depends on`: ADR-0002, ADR-0005
- `related to`: ADR-0006, ADR-0009, ADR-0030, ADR-0032

## Context

The mystery depends on separating what is true in the world from what the player has discovered. Read surfaces must not reveal hidden culprit identity or internal progress merely because those values exist in domain state.

## Decision

Keep hidden culprit truth and hidden progress internal to the domain. Player-facing APIs and projections expose what the player has legitimately learned, not raw hidden state. Developer diagnostics may expose explicit truth only through the controlled developer surface described by ADR-0030 and ADR-0032.

## Rationale and Alternatives

Exposing hidden values in ordinary read models would collapse the mystery's knowledge boundary. Relying on the browser to hide fields would leave the public API authoritative over information the player should not know.

## Consequences

Each player read surface must be shaped around player knowledge. A diagnostic exception remains distinguishable from player information and must not become an implicit allowance for general API disclosure.
