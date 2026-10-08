# ADR-0021 UUID-Shaped Setup Seeds Resolve to Legal Starting Worlds

## Status

`partially superseded`

## Dated History

- `2026-06-01` - Chose a UUID-shaped player seed input that resolves deterministically to legal setup data while keeping hidden culprit truth private.
- `2026-06-29` - The seed pipeline was refactored into `SeedWorld` in commit `a2a88e9`; the resolver now uses a versioned direct-bit codec rather than the labeled mixer described in this record. The commit establishes the implementation change; it does not establish a new decision to preserve the old mixer.
- `2026-10-08` - ADR-0039 supersedes seed ownership of starting-town selection and the single bundled setup descriptor. ADR-0040 records current difficulty and randomness vocabulary.

## Decision Type

`gameplay`, `architecture`

## Related ADRs

- `depends on`: ADR-0007, ADR-0012
- `partially superseded by`: ADR-0039, ADR-0040

## Context

New-game setup needs a player-editable deterministic seed without exposing the hidden case solution. The initial decision described a broader descriptor than the settled flow requires.

## Decision

The public seed input is UUID-shaped and resolves deterministically to legal world variation through a versioned, reversible codec. The codec's bit layout is not a stable contract. It does not expose culprit identity or hidden solution facts. Starting-town choice and the current setup sequence are governed by ADR-0039; difficulty and randomness are separate controls governed by ADR-0040.

## Rationale and Alternatives

The UUID-shaped input is convenient to edit and replay while keeping setup meaning out of a bespoke public token format. Hidden truth remains owned by case generation rather than encoded for the player to inspect.

## Consequences

An identical seed and compatible game rules can reconstruct the same generated world facts. The seed does not choose the player's starting town, own difficulty, or collapse all setup choices into one descriptor. The codec remains versioned and reversible without freezing its bit layout. Historical mixer and codec details explain the original decision but are not current implementation guidance.

## Successors and Surviving Scope

ADR-0039 partially supersedes the seed-owned starting-town and bundled descriptor scope. ADR-0040 supersedes the original difficulty and randomness names. The UUID-shaped input and hidden-truth boundary survive.
